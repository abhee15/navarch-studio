# Architecture & Service Boundaries

## Service Topology

```
┌─────────────────────────────────────────────────────────────┐
│                         Frontend                             │
│  (React + TypeScript + MobX + react-three-fiber)            │
│  Port: 3000                                                  │
└────────────────────┬────────────────────────────────────────┘
                     │ HTTP/REST
                     │
┌────────────────────▼────────────────────────────────────────┐
│                      ApiGateway                              │
│  (.NET 8, Port 5002)                                        │
│  Routes:                                                     │
│    /api/v1/identity/*   → IdentityService                   │
│    /api/v1/hydrostatics/* → DataService                     │
│    /api/v1/resistance/* → DataService                       │
│    /api/v1/catalog/*    → DataService                       │
│    /api/v1/hull-sizing/* → HullSizingService (NEW)          │
│  Middleware: JWT validation, CORS, Rate limiting            │
└─────────┬────────────────┬─────────────────┬────────────────┘
          │                │                 │
          │                │                 │
┌─────────▼──────┐  ┌──────▼───────┐  ┌─────▼──────────────┐
│ IdentityService│  │ DataService  │  │ HullSizingService  │
│ (Port 5001)    │  │ (Port 5003)  │  │ (Port 5004) NEW    │
│ Schema:        │  │ Schema: data │  │ Schema: sizing     │
│   identity     │  │              │  │                    │
│                │  │ - Hydrostatics│  │ - Mission cases   │
│ - Users        │  │ - Resistance │  │ - Sizing runs     │
│ - Roles        │  │ - Catalog    │  │ - Candidates      │
│ - JWT          │  │ - Benchmarks │  │ - Hull families   │
└────────────────┘  └──────┬───────┘  └─────┬──────────────┘
                           │                 │
                           │   HTTP (Polly)  │
                           │   ┌─────────────┘
                           │   │ GET /catalog/water-properties
                           │   │ POST /hydrostatics/vessels
                           │   │ (Retry, Timeout, Circuit Breaker)
                           │   │
          ┌────────────────┴───▼──────────────────────┐
          │         PostgreSQL Database                │
          │  (Single instance, multiple schemas)       │
          │                                            │
          │  identity.*   - Users, roles               │
          │  data.*       - Vessels, catalog, etc.     │
          │  sizing.*     - Mission cases, candidates  │
          └────────────────────────────────────────────┘
```

## HullSizingService Details

### Responsibilities
1. **Mission Management** - CRUD for mission cases (cargo, speed, constraints)
2. **Sizing Computation** - First-principles solver (displacement closure, Froude targeting)
3. **Candidate Generation** - Multiple hull families (container, tanker, bulk, fishing, yacht)
4. **Geometry Generation** - Parametric hulls (Wigley, Series 60)
5. **Integration** - Push candidates to DataService (create vessels)

### NOT Responsible For
- ❌ Hydrostatics calculations (delegates to DataService)
- ❌ Resistance curves (calls DataService for Holtrop)
- ❌ User authentication (handled by IdentityService via ApiGateway)
- ❌ Water properties storage (reads from DataService catalog)

### Technology Stack
- **.NET 8** Web API
- **Entity Framework Core** 8 with PostgreSQL
- **Polly** for resilience (retry, circuit breaker, timeout)
- **Serilog** for structured logging
- **OpenTelemetry** for distributed tracing
- **FluentValidation** for DTO validation
- **CsvHelper** for seed data import

### Configuration
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Database=navarch;Username=...;Password=..."
  },
  "Services": {
    "DataService": "http://data-service:8080"
  },
  "Jwt": {
    "SecretKey": "...",
    "Issuer": "navarch-studio",
    "Audience": "navarch-studio-api"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000", "https://app.navarch.studio"]
  },
  "FeatureFlags": {
    "DataDrivenMode": false,
    "DxfExport": false
  }
}
```

## Service-to-Service Communication

### Pattern: Direct HTTP with Polly

**Example: HullSizingService → DataService**

```csharp
// Register HttpClient with policies
builder.Services.AddHttpClient<IDataServiceClient, DataServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:DataService"]);
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(2)))
.AddPolicyHandler(Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromMilliseconds(200 + Random.Shared.Next(0, 400))))
.AddPolicyHandler(Policy
    .Handle<HttpRequestException>()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
```

### Resilience Strategies

#### 1. Timeout (2 seconds)
```csharp
var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(2));
```
- Prevents hanging requests
- Fast failure for user responsiveness

#### 2. Retry (3 attempts, jitter 200-600ms)
```csharp
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .Or<TimeoutException>()
    .WaitAndRetryAsync(3, retryAttempt => 
        TimeSpan.FromMilliseconds(200 + Random.Shared.Next(0, 400)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            Log.Warning("Retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
        });
```
- Handles transient failures (network blips, temporary overload)
- Jitter prevents thundering herd

#### 3. Circuit Breaker (5 failures / 30 seconds)
```csharp
var circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (outcome, duration) =>
        {
            Log.Error("Circuit breaker OPENED for {Duration}s", duration.TotalSeconds);
        },
        onReset: () => Log.Information("Circuit breaker RESET"));
```
- Prevents cascading failures
- Gives downstream service time to recover
- Opens after 5 consecutive failures
- Half-open after 30 seconds to test recovery

### Caching Strategy

#### Water Properties (12-hour TTL)
```csharp
public async Task<WaterPropertiesDto> GetWaterPropertiesAsync(decimal tempC, decimal salinityPsu, CancellationToken ct)
{
    var cacheKey = $"water_props_{tempC}_{salinityPsu}";
    
    // Try cache first
    if (_cache.TryGetValue(cacheKey, out WaterPropertiesDto? cached))
    {
        _logger.LogInformation("Cache HIT for {Key}", cacheKey);
        return cached!;
    }
    
    try
    {
        // Call DataService
        var response = await _httpClient.GetAsync($"/api/v1/catalog/water-properties?temp={tempC}&salinity={salinityPsu}", ct);
        response.EnsureSuccessStatusCode();
        
        var props = await response.Content.ReadFromJsonAsync<WaterPropertiesDto>(cancellationToken: ct);
        
        // Cache for 12 hours
        _cache.Set(cacheKey, props, TimeSpan.FromHours(12));
        
        return props!;
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "DataService call failed, checking for stale cache");
        
        // Fallback to any cached value (even expired)
        if (_cache.TryGetValue(cacheKey, out WaterPropertiesDto? fallback))
        {
            _logger.LogInformation("Serving STALE cached water properties");
            return fallback!;
        }
        
        throw;
    }
}
```

**Why 12 hours?**
- Water properties don't change frequently
- Reduces load on DataService
- Fallback to stale cache if DataService down

### Claims Forwarding (Multi-Tenancy)

**Middleware extracts claims from JWT:**
```csharp
public class ClaimsForwardingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirst("sub")?.Value;
        var tenantId = context.User.FindFirst("tenantId")?.Value;
        var orgId = context.User.FindFirst("orgId")?.Value;
        var roles = context.User.FindAll("role").Select(c => c.Value).ToArray();
        var scopes = context.User.FindFirst("scope")?.Value?.Split(' ') ?? Array.Empty<string>();
        
        // Deny if tenant missing
        if (string.IsNullOrEmpty(tenantId))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant ID required" });
            return;
        }
        
        // Store in HttpContext.Items for controllers
        context.Items["UserId"] = userId;
        context.Items["TenantId"] = tenantId;
        context.Items["OrgId"] = orgId;
        context.Items["Roles"] = roles;
        context.Items["Scopes"] = scopes;
        
        await _next(context);
    }
}
```

**DataServiceClient forwards claims:**
```csharp
private void AddClaimsHeaders(HttpRequestMessage request)
{
    var context = _contextAccessor.HttpContext;
    if (context == null) return;
    
    if (context.Items.TryGetValue("UserId", out var userId))
        request.Headers.Add("X-User-Id", userId?.ToString());
    
    if (context.Items.TryGetValue("TenantId", out var tenantId))
        request.Headers.Add("X-Tenant-Id", tenantId?.ToString());
    
    if (context.Items.TryGetValue("OrgId", out var orgId))
        request.Headers.Add("X-Org-Id", orgId?.ToString());
    
    if (context.Items.TryGetValue("Roles", out var roles) && roles is string[] rolesArray)
        request.Headers.Add("X-Roles", string.Join(",", rolesArray));
}
```

### Idempotency for "Push to Hydrostatics"

**Client generates idempotency key:**
```typescript
// frontend/src/api/hull-sizing-api.ts
export const pushToHydrostatics = async (candidateId: string, vesselName: string) => {
  const idempotencyKey = generateIdempotencyKey(candidateId, vesselName);
  
  const response = await api.post(
    `/hull-sizing/candidates/${candidateId}/push-to-hydrostatics`,
    { vesselName },
    { headers: { 'X-Idempotency-Key': idempotencyKey } }
  );
  
  return response.data;
};

function generateIdempotencyKey(candidateId: string, vesselName: string): string {
  return `push-hydro-${candidateId}-${Date.now()}`;
}
```

**Backend respects idempotency key:**
```csharp
public async Task<Guid> PushToHydrostaticsAsync(Guid candidateId, string vesselName, string idempotencyKey, CancellationToken ct)
{
    // Check if already pushed with this key
    var existing = await _context.PushOperations
        .Where(p => p.IdempotencyKey == idempotencyKey)
        .FirstOrDefaultAsync(ct);
    
    if (existing != null)
    {
        _logger.LogInformation("Idempotency key {Key} already processed, returning existing vessel ID", idempotencyKey);
        return existing.VesselId;
    }
    
    var candidate = await _context.CandidateDesigns.FindAsync(candidateId);
    var vesselDto = MapToVesselDto(candidate, vesselName);
    
    var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/hydrostatics/vessels");
    request.Headers.Add("X-Idempotency-Key", idempotencyKey);
    AddClaimsHeaders(request);
    request.Content = JsonContent.Create(vesselDto);
    
    var response = await _httpClient.SendAsync(request, ct);
    response.EnsureSuccessStatusCode();
    
    var vessel = await response.Content.ReadFromJsonAsync<VesselDto>(cancellationToken: ct);
    
    // Record operation
    _context.PushOperations.Add(new PushOperation
    {
        Id = Guid.NewGuid(),
        IdempotencyKey = idempotencyKey,
        CandidateId = candidateId,
        VesselId = vessel!.Id,
        CreatedAt = DateTime.UtcNow
    });
    await _context.SaveChangesAsync(ct);
    
    return vessel.Id;
}
```

## OpenTelemetry Tracing

### Configuration (All Services)
```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddSource("HullSizingService");
    });
```

### Propagation
ApiGateway → HullSizingService → DataService all propagate `traceparent` header:

```
traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
             │  │                                │                │
             │  │                                │                └─ Sampling flag
             │  │                                └─ Parent span ID
             │  └─ Trace ID (same across all services)
             └─ Version
```

### Example Trace Flow
```
Trace ID: 0af7651916cd43dd8448eb211c80319c

Span 1 (ApiGateway): POST /api/v1/hull-sizing/runs [200ms]
  └─ Span 2 (HullSizingService): POST /runs [180ms]
       ├─ Span 3 (HullSizingService): Solver.SolveAsync [150ms]
       │    ├─ Span 4 (HullSizingService): HTTP GET water-properties [20ms]
       │    │    └─ Span 5 (DataService): GET /catalog/water-properties [15ms]
       │    └─ Span 6 (HullSizingService): DisplacementClosure [100ms]
       └─ Span 7 (HullSizingService): SaveCandidates [10ms]
```

### Custom Instrumentation
```csharp
using var activity = Activity.StartActivity("DisplacementClosure");
activity?.SetTag("mission.type", missionType);
activity?.SetTag("target.displacement", targetDisplacement);

// ... solver logic

activity?.SetTag("result.iterations", iterations);
activity?.SetTag("result.error_pct", errorPct);
```

## Database Schema Ownership

### `identity` Schema (IdentityService)
- `users`
- `roles`
- `user_roles`
- `refresh_tokens`

### `data` Schema (DataService)
- **Hydrostatics:**
  - `vessels`, `stations`, `waterlines`, `offsets`, `loadcases`, `hydro_results`, `curves`
- **Resistance:**
  - `speed_grids`, `speed_points`, `engine_curves`
- **Catalog (Shared Reference Data):**
  - `catalog_propeller_series`, `catalog_propeller_points`
  - `catalog_water_properties` ← HullSizingService reads this
- **Benchmarks:**
  - `benchmark_cases`, `benchmark_geometries`, `benchmark_test_points`

### `sizing` Schema (HullSizingService) - NEW
- `mission_cases` - User mission requirements
- `sizing_runs` - Computation runs
- `candidate_designs` - Generated hull candidates
- `hull_family_presets` - Vessel type presets (container, tanker, etc.)
- `vessel_catalog` - Reference vessels for data-driven mode (Phase 2)
- `kpi_weights` - Scoring weights (user-specific or system default)
- `push_operations` - Idempotency tracking for vessel creation

### Cross-Schema References
**None.** Services communicate via HTTP, not foreign keys.

Example:
- `sizing.candidate_designs` does NOT have FK to `data.vessels`
- Instead: `push_operations.vessel_id` stores the created vessel's UUID
- If vessel deleted in DataService, HullSizingService keeps its own record

## Security

### Authentication Flow
```
1. User logs in → IdentityService issues JWT
2. Frontend stores JWT in httpOnly cookie
3. Frontend makes request → ApiGateway
4. ApiGateway validates JWT (signature, expiration)
5. ApiGateway forwards to HullSizingService with JWT in Authorization header
6. HullSizingService extracts claims (sub, tenantId, etc.)
7. HullSizingService forwards claims to DataService (X-User-Id, X-Tenant-Id headers)
```

### Multi-Tenancy Enforcement
- All tables have `tenant_id` (or implicit via `user_id`)
- Middleware denies requests if `tenantId` claim missing
- EF Core query filters: `.Where(e => e.TenantId == currentTenantId)`
- Prevents cross-tenant data leakage

### Secrets Management
- **Development:** `appsettings.Development.json` (local only)
- **Production:** AWS Secrets Manager
  - `navarch/rds/credentials` - DB connection string
  - `navarch/jwt/secret` - JWT validation key
- **Injection:** App Runner environment variables

```hcl
# terraform/deploy/modules/app-runner/hull-sizing.tf
resource "aws_apprunner_service" "hull_sizing" {
  # ...
  
  environment_variable {
    name  = "ConnectionStrings__DefaultConnection"
    value = data.aws_secretsmanager_secret_version.db_creds.secret_string
  }
  
  environment_variable {
    name  = "Jwt__SecretKey"
    value = data.aws_secretsmanager_secret_version.jwt_key.secret_string
  }
}
```

## API Gateway Routing Logic

```csharp
// ApiGateway/Program.cs
app.MapWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api/v1/identity"),
    appBuilder => appBuilder.RunProxy(new Uri("http://identity-service:8080"))
);

app.MapWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api/v1/hydrostatics") ||
           ctx.Request.Path.StartsWithSegments("/api/v1/resistance") ||
           ctx.Request.Path.StartsWithSegments("/api/v1/catalog"),
    appBuilder => appBuilder.RunProxy(new Uri("http://data-service:8080"))
);

app.MapWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api/v1/hull-sizing"),
    appBuilder => appBuilder.RunProxy(new Uri("http://hull-sizing-service:8080"))
);
```

## Port Allocation

| Service | Docker Port | Container Port | Purpose |
|---------|-------------|----------------|---------|
| PostgreSQL | 5433 | 5432 | Database |
| IdentityService | 5001 | 8080 | User auth |
| ApiGateway | 5002 | 8080 | Routing |
| DataService | 5003 | 8080 | Hydro/Resistance |
| **HullSizingService** | **5004** | **8080** | **Hull sizing (NEW)** |
| Frontend | 3000 | 3000 | React app |

## Health Checks

All services expose `/health` endpoint:

```csharp
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

app.MapHealthChecks("/health").DisableRateLimiting();
```

Docker compose healthcheck:
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

AWS App Runner healthcheck:
```hcl
health_check_configuration {
  path             = "/health"
  interval         = 10
  timeout          = 5
  healthy_threshold   = 1
  unhealthy_threshold = 5
}
```

## Deployment Architecture (AWS)

```
┌─────────────────────────────────────────────────────────┐
│                    CloudFront (CDN)                      │
│  - S3 Origin (Frontend static files)                    │
│  - Custom domain: app.navarch.studio                    │
└──────────────────┬──────────────────────────────────────┘
                   │
                   │ HTTPS
                   │
┌──────────────────▼──────────────────────────────────────┐
│               Application Load Balancer                  │
│  - SSL termination                                      │
│  - Routes to App Runner services                       │
└──────┬────────────┬──────────────┬──────────────────────┘
       │            │              │
       │            │              │
┌──────▼──────┐ ┌──▼──────────┐ ┌─▼──────────────────┐
│IdentityService│ DataService │ │HullSizingService   │
│ (App Runner) │(App Runner) │ │(App Runner) NEW    │
└──────┬──────┘ └──┬──────────┘ └─┬──────────────────┘
       │           │               │
       │           │               │
       └───────────┴───────────────┘
                   │
                   │ VPC Connector
                   │
       ┌───────────▼───────────┐
       │    RDS PostgreSQL     │
       │  (Multi-AZ, encrypted)│
       │  - identity schema    │
       │  - data schema        │
       │  - sizing schema      │
       └───────────────────────┘
```

## Next: Read `02-DATABASE-SCHEMA.md` for detailed DDL
