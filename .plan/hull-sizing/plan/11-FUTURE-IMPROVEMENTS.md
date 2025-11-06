# Hull Sizing Module - Future Improvements & Technical Debt

**Document Purpose:** Track items discovered during Phase 0 implementation that should be addressed in future phases or iterations.

**Last Updated:** 2025-11-02
**Status:** Living document - add items as we discover them

---

## Phase 0 Analysis - Items Identified

### ✅ Completed During Phase 0
1. **OpenTelemetry Packages** - Added all required OpenTelemetry instrumentation packages to `HullSizingService.csproj`
2. **Health Check Package** - Added `AspNetCore.HealthChecks.NpgSql` for PostgreSQL health checks
3. **Comprehensive Logging** - Enhanced migration logging with detailed start/end/error messages
4. **Service Configuration** - Port 5004, proper JWT/CORS/RateLimiting setup
5. **Database Schema** - Complete `sizing` schema with precise numeric types, indexes, constraints

### 🚧 In Progress (Phase 0 Continuation)
1. **docker-compose.yml** - Add `hull-sizing-service` entry
2. **ApiGateway Routing** - Add `/api/v1/hull-sizing/*` proxy routes
3. **GitHub Actions CI/CD** - Add HullSizingService to build/test/deploy workflows

---

## Discovered Gaps & Improvements

### 1. Package Consistency Analysis

**Findings:**
- ✅ `HullSizingService.csproj` matches `DataService.csproj` for core packages (Serilog, EFCore, FluentValidation)
- ⚠️ **Missing:** `AWS.Logger.SeriLog` (version 4.0.2) - used by `IdentityService` and `DataService` for CloudWatch logs
- ✅ OpenTelemetry packages added (1.9.0 for stable, 1.0.0-beta.11 for EFCore instrumentation)
- ✅ Polly resilience packages included

**Recommendation:**
- Add `AWS.Logger.SeriLog` in Phase 1 when setting up CloudWatch integration
- Keep package versions aligned with `DataService` to avoid compatibility issues

---

### 2. Missing XML Documentation in `.csproj`

**Current State:**
- ✅ `DataService.csproj` has `<GenerateDocumentationFile>true</GenerateDocumentationFile>` and suppresses warning 1591
- ✅ `HullSizingService.csproj` also has this configured (added during Phase 0)
- ❌ `IdentityService.csproj` is **missing** XML documentation generation

**Impact:**
- Swagger/OpenAPI documentation won't include XML comments for IdentityService endpoints
- Inconsistent API documentation across services

**Recommendation:**
- Add to `IdentityService.csproj`:
  ```xml
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <NoWarn>$(NoWarn);1591</NoWarn>
  ```

---

### 3. ApiGateway Service URL Configuration

**Current State:**
```json
{
  "Services": {
    "IdentityService": "http://identity-service:8080",
    "DataService": "http://data-service:8080"
  }
}
```

**Missing:**
- `HullSizingService` URL configuration

**Recommendation (Phase 0):**
- Add to `backend/ApiGateway/appsettings.json`:
  ```json
  "Services": {
    "IdentityService": "http://identity-service:8080",
    "DataService": "http://data-service:8080",
    "HullSizingService": "http://hull-sizing-service:8080"
  }
  ```

---

### 4. GitHub Actions Secrets for HullSizingService

**Required New Secrets:**
- `ECR_HULL_SIZING_SERVICE_URL` - ECR repository URL for HullSizingService Docker images

**Workflow Changes Needed:**
1. `.github/workflows/ci-dev.yml`:
   - Add "Build and push Hull Sizing Service" step
   - Add HullSizingService to smoke tests (health check)
   - Update deployment trigger to include HullSizingService

2. `.github/workflows/ci-staging.yml` and `.github/workflows/ci-prod.yml`:
   - Same changes as dev workflow

**Recommendation:**
- Update all 3 environment workflows (`ci-dev.yml`, `ci-staging.yml`, `ci-prod.yml`)
- Add `ECR_HULL_SIZING_SERVICE_URL` secret via Terraform output or manual configuration

---

### 5. Terraform Infrastructure for HullSizingService

**Missing Terraform Resources:**
1. **ECR Repository** (`terraform/setup/main.tf`):
   ```hcl
   resource "aws_ecr_repository" "hull_sizing_service" {
     name                 = "${var.project_name}-hull-sizing-service"
     image_tag_mutability = "MUTABLE"
     # ... lifecycle policy, scanning, etc.
   }
   ```

2. **App Runner Service** (`terraform/deploy/modules/app-runner/`):
   - New App Runner service for HullSizingService
   - VPC connector to RDS (private subnets)
   - Environment variables (DB connection, JWT, DataService URL)
   - Health check endpoint: `/health`
   - CPU: 1024, Memory: 2048 (same as DataService)

3. **API Gateway Route** (if using custom API Gateway module):
   - Route `/api/v1/hull-sizing/*` → HullSizingService

4. **Outputs**:
   - `hull_sizing_service_url` for CI/CD and integration testing

**Recommendation:**
- Add to `terraform/setup/` first (ECR repo creation)
- Then add to `terraform/deploy/` (App Runner service)
- Test locally with `docker-compose` before deploying to AWS

---

### 6. Unit Conversion Configuration Consistency

**Current State:**
- All services copy `unit-systems.xml` to output directory
- ✅ `HullSizingService` follows this pattern:
  ```xml
  <None Include="..\..\packages\unit-conversion\config\unit-systems.xml">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Link>config\unit-systems.xml</Link>
  </None>
  ```

**Note:**
- `IdentityService` uses `<Link>unit-systems.xml</Link>` (root, no `config/` prefix)
- `DataService` and `HullSizingService` use `<Link>config\unit-systems.xml</Link>`

**Recommendation:**
- Standardize on `config\unit-systems.xml` for all services (already consistent for Data/HullSizing)
- Update `IdentityService.csproj` for consistency (non-breaking, but good housekeeping)

---

### 7. Dockerfile Health Check Consistency

**Analysis:**
- ✅ `HullSizingService/Dockerfile` has proper health check:
  ```dockerfile
  HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1
  ```
- ✅ All services use port 8080 internally (mapped to different external ports)
- ✅ All services have consistent health check configuration

**No action needed** - this is already correct!

---

### 8. Polly Resilience Policies for DataService HTTP Client

**Current State (Phase 0):**
- `HullSizingService` has Polly packages installed
- No Polly policies configured yet (Phase 1 task)

**Required (Phase 1):**
```csharp
builder.Services.AddHttpClient<IDataServiceClient, DataServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:DataService"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy())
.AddPolicyHandler(GetTimeoutPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => 
                TimeSpan.FromMilliseconds(200 + Random.Shared.Next(0, 400)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Log.Warning("Retry {RetryCount} after {Delay}ms", retryCount, timespan.TotalMilliseconds);
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30));
}

static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
{
    return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(2));
}
```

**Also needed:**
- Forward `idempotency-key` header on "Push to Hydrostatics" requests
- Forward user claims (sub, tenantId, orgId, roles, scope)

---

### 9. Water Properties Caching (Phase 1)

**Required:**
```csharp
public class WaterPropertiesService
{
    private readonly IMemoryCache _cache;
    private readonly HttpClient _httpClient;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(12);

    public async Task<WaterProperties> GetPropertiesAsync(decimal tempC, decimal salinityPsu)
    {
        var cacheKey = $"water_props_{tempC}_{salinityPsu}";
        
        if (_cache.TryGetValue(cacheKey, out WaterProperties? cached))
            return cached!;

        try
        {
            var result = await _httpClient.GetFromJsonAsync<WaterProperties>(
                $"/api/v1/water-properties?temp={tempC}&salinity={salinityPsu}");
            
            _cache.Set(cacheKey, result, CacheTtl);
            return result!;
        }
        catch (HttpRequestException)
        {
            // If DataService is down, try to serve stale cache
            var staleKey = $"{cacheKey}_stale";
            if (_cache.TryGetValue(staleKey, out WaterProperties? stale))
            {
                Log.Warning("DataService unavailable, serving stale water properties");
                return stale!;
            }
            throw;
        }
    }
}
```

---

### 10. Claims Forwarding Middleware (Phase 1)

**Current State:**
- JWT authentication extracts claims and sets `HttpContext.User`
- Need to forward claims to DataService for tenant isolation and authorization

**Required Middleware:**
```csharp
public class ClaimsForwardingMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var sub = context.User.FindFirst("sub")?.Value;
            var tenantId = context.User.FindFirst("tenantId")?.Value;
            var orgId = context.User.FindFirst("orgId")?.Value;
            var roles = string.Join(",", context.User.FindAll("roles").Select(c => c.Value));
            var scope = context.User.FindFirst("scope")?.Value;

            if (string.IsNullOrEmpty(tenantId))
            {
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { error = "Missing tenantId claim" });
                return;
            }

            // Forward claims in headers for S2S calls
            context.Items["Claims:Sub"] = sub;
            context.Items["Claims:TenantId"] = tenantId;
            context.Items["Claims:OrgId"] = orgId;
            context.Items["Claims:Roles"] = roles;
            context.Items["Claims:Scope"] = scope;
        }

        await next(context);
    }
}
```

**Then in HTTP client:**
```csharp
if (httpContext.Items.TryGetValue("Claims:TenantId", out var tenantId))
    request.Headers.Add("X-Tenant-Id", tenantId?.ToString());
// ... forward other claims
```

---

### 11. Database CHECK Constraints (Currently Missing)

**Current State:**
- EF Core configuration sets column types and relationships
- No database-level CHECK constraints yet

**Recommended Constraints (add in next migration):**
```sql
-- MissionCase
ALTER TABLE sizing.mission_cases
  ADD CONSTRAINT chk_cargo_basis 
    CHECK (cargo_basis IN ('volume', 'weight', 'teu'));
ALTER TABLE sizing.mission_cases
  ADD CONSTRAINT chk_cargo_value_positive 
    CHECK (cargo_value > 0);
ALTER TABLE sizing.mission_cases
  ADD CONSTRAINT chk_speed_positive 
    CHECK (service_speed_kn > 0);

-- SizingRun
ALTER TABLE sizing.sizing_runs
  ADD CONSTRAINT chk_mode 
    CHECK (mode IN ('first_principles', 'data_driven'));
ALTER TABLE sizing.sizing_runs
  ADD CONSTRAINT chk_status 
    CHECK (status IN ('pending', 'computing', 'completed', 'failed'));

-- CandidateDesign
ALTER TABLE sizing.candidate_designs
  ADD CONSTRAINT chk_dimensions_positive 
    CHECK (lpp_m > 0 AND b_m > 0 AND t_m > 0 AND d_m > 0);
ALTER TABLE sizing.candidate_designs
  ADD CONSTRAINT chk_coefficients_range 
    CHECK (cb BETWEEN 0.3 AND 0.95 
       AND cp BETWEEN 0.5 AND 1.0 
       AND cwp BETWEEN 0.5 AND 1.0);
```

**Recommendation:**
- Add these constraints in a separate migration after Phase 0
- Add corresponding FluentValidation rules in the application layer
- Document constraint violations in API responses using `FlagsJson`

---

### 12. Seed Data Strategy

**Current Approach (from plan):**
- One-shot CSV seeder runs after migrations
- Checks if tables are empty before seeding (idempotent)

**Files to Seed:**
1. `hull_family_presets_extended.csv` → `hull_family_presets`
2. `iso_containers.csv` → `iso_containers`
3. `kpi_weights.csv` → `kpi_weights`
4. `vessel_catalog_seed.csv` → `vessel_catalog` (KCS, KVLCC2, Series 60)
5. `water_properties.csv` → to be added to DataService (not sizing schema)

**Recommendation:**
- Create `SeedService` in Phase 3
- Use `CsvHelper` for parsing
- Log seed operations: `[SEED] Seeded N hull families`, etc.
- Skip seeding if tables are not empty (check count first)

---

### 13. Performance Guardrails

**3D Mesh Limits (from plan):**
- Default max: 80,000 triangles
- Use dynamic LOD (Level of Detail) based on camera distance
- Run solver in Web Worker (off main thread)

**Targets:**
- Slider interaction: < 300 ms (API call + re-render)
- Full sizing compute: < 2 s (5 candidates with Holtrop-Mennen)
- 3D viewport: ≥ 45 FPS

**Monitoring:**
- Add performance telemetry to OpenTelemetry spans:
  ```csharp
  using var activity = _activitySource.StartActivity("SizingSolver.Compute");
  activity?.SetTag("candidate.count", candidates.Count);
  activity?.SetTag("compute.time_ms", stopwatch.ElapsedMilliseconds);
  ```

**Recommendation:**
- Add performance assertions in unit tests (solver should complete in < 500ms per candidate)
- Monitor P95/P99 latency in production
- Alert if FPS drops below 30 on majority of clients

---

### 14. Testing Gaps

**Current State (Phase 0):**
- No tests yet (expected - Phase 0 is infrastructure)

**Required Tests (Phase 2-4):**

1. **Unit Tests** (`backend/HullSizingService.Tests/`):
   - `DisplacementClosureService` convergence tests
   - `HoltropResistanceService` accuracy tests (compare with reference data)
   - `StabilityScreenService` calculation tests
   - `WigleyHullGenerator` geometry validation
   - Series 60 hull generator tests

2. **Integration Tests**:
   - Full sizing flow: mission case → run → candidates
   - "Push to Hydrostatics" integration
   - Database constraints and soft delete
   - Multi-tenancy isolation

3. **Performance Tests**:
   - Solver convergence within 100 iterations
   - 5 candidates generated in < 2s
   - Database query performance (indexed queries)

4. **Reference Test Cases** (from `test_matrix.csv`):
   - Container: 6000 TEU @ 24 kn → Lpp ≈ 230-250 m, Cb ≈ 0.65
   - Tanker: 200,000 DWT @ 16 kn → Lpp ≈ 320-340 m, Cb ≈ 0.82
   - KCS data-driven mode (Phase 2): Compare with published dimensions

**Recommendation:**
- Create test project: `backend/HullSizingService.Tests/`
- Add tests incrementally as solver components are implemented
- Use `xUnit`, `Moq`, and `FluentAssertions` (match existing test projects)

---

### 15. API Specification & Typed Clients

**Current State:**
- Swagger/OpenAPI configured in `Program.cs`
- No typed client generation yet

**Recommended (Phase 4):**
1. Generate OpenAPI spec from Swagger:
   ```bash
   dotnet swagger tofile --output swagger.json backend/HullSizingService/bin/Debug/net8.0/HullSizingService.dll v1
   ```

2. Generate TypeScript client for frontend:
   ```bash
   npx openapi-typescript-codegen --input swagger.json --output frontend/src/api/sizing
   ```

3. Add ProblemDetails for error responses:
   ```csharp
   builder.Services.AddProblemDetails();
   ```

**Recommendation:**
- Add OpenAPI spec generation to CI/CD pipeline
- Version API routes: `/api/v1/hull-sizing/*` (already planned)
- Document all DTOs with XML comments

---

### 16. Security Hardening

**Current State:**
- JWT authentication configured
- Rate limiting: 100 req/min per IP
- CORS configured for frontend origins

**Additional Hardening (Phase 1-2):**
1. **Input Validation:**
   - FluentValidation for all request DTOs
   - Validate dimensions (must be positive, within reasonable bounds)
   - Validate coefficients (0.3 ≤ Cb ≤ 0.95, etc.)

2. **Tenant Isolation:**
   - Always filter by `tenantId` in queries
   - Use EF Core query filters
   - Deny requests without `tenantId` claim

3. **Secrets Management:**
   - Store RDS credentials in AWS Secrets Manager
   - Store JWT validation keys in Secrets Manager
   - Inject via App Runner environment variables

4. **SQL Injection Prevention:**
   - ✅ Using EF Core (parameterized queries by default)
   - ✅ No raw SQL queries (currently)

**Recommendation:**
- Add input validation in Phase 1 alongside controllers
- Implement tenant isolation query filters in EF Core
- Migrate secrets to AWS Secrets Manager before production deployment

---

### 17. Logging & Observability Enhancements

**Current State:**
- Serilog configured with JSON output
- OpenTelemetry tracing configured
- Migration logging enhanced during Phase 0

**Additional Recommendations:**
1. **Structured Logging:**
   ```csharp
   Log.Information("Sizing run created: {RunId}, Mode: {Mode}, Candidates: {Count}",
       run.Id, run.Mode, candidates.Count);
   ```

2. **Correlation ID:**
   - ✅ Already configured via `CorrelationIdMiddleware`
   - Ensure propagated to DataService HTTP calls

3. **Metrics:**
   - Solver convergence rate (% of runs that converge)
   - Average compute time per candidate
   - Distribution of hull families selected

4. **Alerting:**
   - Alert if solver convergence rate drops below 95%
   - Alert if P99 compute time exceeds 5 seconds
   - Alert if database query time exceeds 1 second

**Recommendation:**
- Add custom OpenTelemetry metrics in Phase 2
- Set up CloudWatch alarms for critical metrics
- Create Grafana dashboard for hull sizing metrics (optional)

---

### 18. Custom Algorithm Development

**User Request:**
> "Lets see if we could come up with our custom algorithm as we build"

**Current Approach:**
- Phase 1: Implement industry-standard first-principles solver
  - Displacement closure (Newton loop)
  - Geometric ratio constraints (L/B, B/T, D/T)
  - Holtrop-Mennen resistance
  - Stability screen (quick GM/BMt)

**Research Direction:**
- **Reference:** SPS SName paper (mentioned by user)
- **Potential Improvements:**
  1. Multi-objective optimization (Pareto front)
  2. Machine learning for coefficient prediction
  3. Wave resistance correction factors (beyond Holtrop-Mennen)
  4. Seakeeping constraints (RAO-based)
  5. Structural efficiency metrics (lightweight index)

**Recommendation:**
- Implement standard solver first (Phase 1-2)
- Document solver logic in `.plan/hull-sizing/plan/05-SOLVER-ALGORITHM.md`
- Collect performance data (convergence, accuracy, user feedback)
- Research custom algorithm in Phase 3+
- Add custom algorithm as a "mode" option (alongside first_principles and data_driven)

**Action Items:**
- [ ] Review SPS SName paper for algorithm insights
- [ ] Benchmark standard solver against reference cases
- [ ] Identify gaps in standard solver (accuracy, performance, coverage)
- [ ] Prototype custom algorithm in separate branch
- [ ] A/B test custom vs standard solver

---

## Priority Matrix

### 🔴 Critical (Phase 0 Continuation - This Week)
1. Add HullSizingService to `docker-compose.yml`
2. Add ApiGateway routing for `/api/v1/hull-sizing/*`
3. Test end-to-end connectivity (Gateway → HullSizing → DB)

### 🟠 High Priority (Phase 1 - Next 2 Weeks)
1. Implement Polly policies for DataService HTTP client
2. Add water properties caching (12h TTL)
3. Create ClaimsForwardingMiddleware
4. Add HullSizingService to GitHub Actions workflows
5. Create Terraform resources (ECR repo, App Runner service)

### 🟡 Medium Priority (Phase 2-3 - Next Month)
1. Add database CHECK constraints
2. Create SeedService and import CSVs
3. Add unit tests for solver components
4. Generate TypeScript API client
5. Add performance telemetry

### 🟢 Low Priority (Phase 4+ - Future Iterations)
1. Update IdentityService.csproj for XML documentation
2. Standardize unit-conversion path across all services
3. Research custom algorithm
4. Add Grafana dashboards
5. Implement advanced seakeeping constraints

---

## Continuous Improvement Process

**How to Use This Document:**
1. **During Development:** Add new items as they're discovered
2. **During Code Review:** Check if new code introduces technical debt
3. **During Retrospectives:** Prioritize items for next sprint
4. **Before Releases:** Ensure all "Critical" items are addressed

**Document Maintenance:**
- Update status as items are completed
- Move completed items to a "Completed" section (with completion date)
- Re-prioritize quarterly based on user feedback and system metrics

---

## Completed Items (Archive)

### 2025-11-02
- ✅ Added OpenTelemetry packages to HullSizingService.csproj
- ✅ Added AspNetCore.HealthChecks.NpgSql package
- ✅ Enhanced migration logging with detailed messages
- ✅ Configured service on port 5004 with proper JWT/CORS/RateLimiting
- ✅ Created complete `sizing` schema with 8 tables
- ✅ Added XML documentation generation to HullSizingService.csproj
- ✅ Added HullSizingService to navarch-studio.sln
- ✅ Committed and pushed Phase 0 to main branch

---

**Next Review:** After Phase 0 docker-compose + ApiGateway integration
**Owner:** Development Team
**Stakeholders:** Product, DevOps, QA





