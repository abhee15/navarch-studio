# Backend Implementation Phases (Week-by-Week)

## Phase 0: Foundation & Setup (Week 1, Days 1-2)

### Goal
Create HullSizingService project structure, database schema, and verify basic connectivity.

### Tasks

**0.1 Project Structure**
- [x] Create `backend/HullSizingService/` directory
- [x] Create `HullSizingService.csproj` with all NuGet packages
- [x] Create `Program.cs` with Serilog, OpenTelemetry, JWT, CORS
- [x] Create `appsettings.json` and `appsettings.Development.json`
- [x] Create `Properties/launchSettings.json` (port 5004)
- [x] Create `Dockerfile` (multi-stage build)
- [x] Add to solution: `dotnet sln add HullSizingService/HullSizingService.csproj`

**0.2 Database Context**
- [x] Create `Data/SizingDbContext.cs` with schema `sizing`
- [ ] Configure all entities with proper column types and indexes
- [ ] Add query filters (soft delete)

**0.3 Verify Build**
- [ ] `dotnet restore backend/HullSizingService`
- [ ] `dotnet build backend/HullSizingService`
- [ ] Health endpoint `/health` responds

**✓ Completion Checklist:**
- HullSizingService builds without errors
- Program.cs logs startup messages
- Swagger UI accessible at http://localhost:5004/swagger
- Health check returns 200 OK

---

## Phase 1: Models & DTOs (Week 1, Day 3)

### Goal
Create all entity models and data transfer objects in Shared project.

### Tasks

**1.1 Entity Models** (`backend/Shared/Models/Sizing/`)
- [ ] `MissionCase.cs` - All properties with validation attributes
- [ ] `SizingRun.cs` - With navigation to Candidates
- [ ] `CandidateDesign.cs` - All dimensions, coefficients, scores
- [ ] `HullFamilyPreset.cs` - Ratio ranges and coefficient bands
- [ ] `VesselCatalog.cs` - Reference vessel data
- [ ] `KpiWeight.cs` - Scoring weights
- [ ] `IsoContainer.cs` - Standard container types
- [ ] `PushOperation.cs` - Idempotency tracking

**1.2 DTOs** (`backend/Shared/DTOs/Sizing/`)
- [ ] `MissionCaseDto.cs` - For API responses
- [ ] `CreateMissionCaseDto.cs` - For POST/PUT requests
- [ ] `SizingRunDto.cs` - Run details with candidates
- [ ] `CreateSizingRunDto.cs` - Run configuration (mode, locks, options)
- [ ] `CandidateDesignDto.cs` - Candidate details
- [ ] `SizingResultDto.cs` - Response with multiple candidates
- [ ] `RecomputeRequestDto.cs` - For slider adjustments
- [ ] `PushToHydrostaticsRequestDto.cs` - Vessel name input
- [ ] `PushToHydrostaticsResultDto.cs` - Created vessel ID
- [ ] `HullFamilyPresetDto.cs` - Family ranges
- [ ] `LocksDto.cs` - Lock configuration (keep_fn, keep_l_over_b, etc.)
- [ ] `SizingOptionsDto.cs` - Solver options

**1.3 Supporting Classes**
- [ ] `ClosureResult.cs` - Displacement closure output
- [ ] `ResistanceResult.cs` - Holtrop calculation output
- [ ] `StabilityResult.cs` - GM/BMt/KB output
- [ ] `HullGeometry.cs` - Offsets grid representation
- [ ] `ScoreBreakdown.cs` - Individual KPI scores

**✓ Completion Checklist:**
- All models match database schema exactly
- DTOs have FluentValidation rules
- Models compile without errors
- Navigation properties configured

---

## Phase 2: Database Migration (Week 1, Day 3)

### Goal
Create initial EF Core migration and apply to development database.

### Tasks

**2.1 Create Migration**
```bash
dotnet ef migrations add InitialSizingSchema \
  --project backend/HullSizingService \
  --context SizingDbContext \
  --output-dir Migrations
```

**2.2 Review Migration**
- [ ] Verify all tables created in `sizing` schema
- [ ] Verify all CHECK constraints present
- [ ] Verify all indexes present
- [ ] Verify numeric precision correct

**2.3 Apply Migration (Local Dev)**
```bash
dotnet ef database update \
  --project backend/HullSizingService \
  --context SizingDbContext
```

**2.4 Verify Schema**
```sql
-- Connect to PostgreSQL
\c sri_template_dev

-- List tables in sizing schema
\dt sizing.*

-- Check constraints
SELECT * FROM information_schema.table_constraints 
WHERE table_schema = 'sizing';

-- Check indexes
SELECT * FROM pg_indexes WHERE schemaname = 'sizing';
```

**✓ Completion Checklist:**
- `sizing` schema exists in database
- All 8 tables created
- All CHECK constraints active
- All indexes created
- Migration runs without errors

---

## Phase 3: Seed Data Import (Week 1, Days 4-5)

### Goal
Import CSV seed data (hull families, KPI weights, vessel catalog, ISO containers).

### Tasks

**3.1 CSV Models** (`Data/Seeds/CsvModels/`)
- [ ] `HullFamilyPresetCsv.cs` - Maps to hull_family_presets_extended.csv
- [ ] `VesselCatalogCsv.cs` - Maps to vessel_catalog_seed.csv
- [ ] `KpiWeightCsv.cs` - Maps to kpi_weights.csv

**3.2 Seed Service** (`Services/ISeedService.cs`)
```csharp
public interface ISeedService
{
    Task SeedAllAsync(CancellationToken ct);
    Task SeedHullFamiliesAsync(CancellationToken ct);
    Task SeedVesselCatalogAsync(CancellationToken ct);
    Task SeedKpiWeightsAsync(CancellationToken ct);
    Task SeedIsoContainersAsync(CancellationToken ct);
}
```

**3.3 Implementation** (`Services/SeedService.cs`)
- [ ] Use CsvHelper to parse CSVs
- [ ] Check if table already has data (idempotent)
- [ ] Bulk insert records
- [ ] Log import counts

**3.4 Copy CSV Files**
```bash
# Copy from app-docs to HullSizingService
mkdir backend/HullSizingService/Data/Seeds/csv
cp .plan/app-docs/hull-sizing/hull_family_presets_extended.csv backend/HullSizingService/Data/Seeds/csv/
cp .plan/app-docs/hull-sizing/vessel_catalog_seed.csv backend/HullSizingService/Data/Seeds/csv/
cp .plan/app-docs/hull-sizing/kpi_weights.csv backend/HullSizingService/Data/Seeds/csv/
cp .plan/app-docs/hull-sizing/iso_containers.csv backend/HullSizingService/Data/Seeds/csv/
```

**3.5 Register Seeder**
```csharp
// Program.cs - After migrations
builder.Services.AddScoped<ISeedService, SeedService>();

// Run seeder
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<ISeedService>();
    await seeder.SeedAllAsync(CancellationToken.None);
}
```

**✓ Completion Checklist:**
- 13 hull families imported
- 3-4 vessel catalog entries (KCS, KVLCC2, Series 60)
- 5 KPI weights (system defaults)
- 4 ISO container types
- Seeder is idempotent (runs safely multiple times)
- Logs show import counts

---

## Phase 4: Resilient HTTP Client (Week 2, Day 1)

### Goal
Implement Polly resilience patterns for DataService communication.

### Tasks

**4.1 Data Service Client Interface** (`Services/Integration/IDataServiceClient.cs`)
```csharp
public interface IDataServiceClient
{
    Task<WaterPropertiesDto> GetWaterPropertiesAsync(decimal tempC, decimal salinityPsu, CancellationToken ct);
    Task<Guid> CreateVesselAsync(CreateVesselDto dto, string idempotencyKey, CancellationToken ct);
}
```

**4.2 Implementation** (`Services/Integration/DataServiceClient.cs`)
- [ ] Implement GetWaterPropertiesAsync with caching (12h TTL)
- [ ] Implement CreateVesselAsync with idempotency key header
- [ ] Forward claims (X-User-Id, X-Tenant-Id, X-Org-Id, X-Roles)
- [ ] Fallback to stale cache on failure

**4.3 Polly Policies** (`Program.cs`)
```csharp
builder.Services.AddHttpClient<IDataServiceClient, DataServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:DataService"]!);
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

**4.4 Water Properties DTO** (`Shared/DTOs/Catalog/WaterPropertiesDto.cs`)
```csharp
public record WaterPropertiesDto
{
    public decimal TempC { get; init; }
    public decimal SalinityPsu { get; init; }
    public decimal RhoKgM3 { get; init; }
    public decimal NuM2S { get; init; }
}
```

**✓ Completion Checklist:**
- DataServiceClient registered with Polly policies
- Timeout policy: 2 seconds
- Retry policy: 3 attempts, jitter 200-600ms
- Circuit breaker: 5 failures, 30s break
- Water properties cached with 12h TTL
- Fallback to stale cache works
- Idempotency key forwarded

---

## Phase 5: Claims & Multi-Tenancy (Week 2, Day 1)

### Goal
Extract and forward JWT claims for multi-tenancy support.

### Tasks

**5.1 Claims Middleware** (`Middleware/ClaimsForwardingMiddleware.cs`)
```csharp
public class ClaimsForwardingMiddleware
{
    private readonly RequestDelegate _next;
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Extract claims from JWT
        var userId = context.User.FindFirst("sub")?.Value;
        var tenantId = context.User.FindFirst("tenantId")?.Value;
        var orgId = context.User.FindFirst("orgId")?.Value;
        var roles = context.User.FindAll("role").Select(c => c.Value).ToArray();
        var scopes = context.User.FindFirst("scope")?.Value?.Split(' ') ?? Array.Empty<string>();
        
        // Deny if tenant missing
        if (string.IsNullOrEmpty(tenantId))
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant ID required for multi-tenancy" });
            return;
        }
        
        // Store in HttpContext.Items
        context.Items["UserId"] = userId;
        context.Items["TenantId"] = tenantId;
        context.Items["OrgId"] = orgId;
        context.Items["Roles"] = roles;
        context.Items["Scopes"] = scopes;
        
        await _next(context);
    }
}
```

**5.2 Register Middleware** (`Program.cs`)
```csharp
// After JWT authentication
app.UseMiddleware<JwtAuthenticationMiddleware>();
app.UseMiddleware<ClaimsForwardingMiddleware>(); // NEW
```

**5.3 Update DataServiceClient**
- [ ] Add IHttpContextAccessor injection
- [ ] Create AddClaimsHeaders() method
- [ ] Forward claims on all HTTP requests to DataService

**✓ Completion Checklist:**
- Claims extracted from JWT (sub, tenantId, orgId, roles, scopes)
- 403 returned if tenantId missing
- Claims stored in HttpContext.Items
- Claims forwarded to DataService (X-User-Id, X-Tenant-Id, etc.)
- Controllers can access via HttpContext.Items["TenantId"]

---

## Phase 6: First-Principles Solver (Week 2, Days 2-5)

### Goal
Implement core sizing algorithm (displacement closure, Holtrop resistance, stability screen).

### Tasks

**6.1 Solver Interfaces** (`Services/Solvers/`)
- [ ] `IFirstPrinciplesSolver.cs`
- [ ] `IDisplacementClosureService.cs`
- [ ] `IFroudeTargetingService.cs`
- [ ] `IHoltropResistanceService.cs`
- [ ] `IStabilityScreenService.cs`
- [ ] `IHullFamilyService.cs`

**6.2 Displacement Closure** (`Services/Solvers/DisplacementClosureService.cs`)

Algorithm:
```
1. Start with target displacement (from payload + lightship estimates)
2. Pick initial Fn from family band
3. Solve LWL = V² / (g·Fn²)
4. Apply ratios (L/B, B/T, D/T) from family preset
5. Calculate B = LWL / (L/B), T = B / (B/T), D = T · (D/T)
6. Check constraints (clamp B, T to max values)
7. Compute ∇ = LWL · B · T · Cb
8. Compute Δ = ρ_sw · ∇  (ρ_sw = 1.025 t/m³)
9. Error = (Δ - Δ_target) / Δ_target
10. If |error| < 1%, DONE
11. Else adjust parameters:
    - If keep_fn unlocked: adjust LWL slightly
    - If keep_l_over_b unlocked: adjust B
    - If keep_b_over_t unlocked: adjust T
    - If keep_cb unlocked: adjust Cb
12. Repeat (max 50 iterations)
```

**Implementation checklist:**
- [ ] Newton loop converges for test cases
- [ ] Respects all locks (Fn, L/B, B/T, D/T, Cb)
- [ ] Handles constraints (max beam, draft, LOA)
- [ ] Returns flags for violations
- [ ] Max 50 iterations with timeout
- [ ] Logs convergence metrics

**6.3 Holtrop Resistance** (`Services/Solvers/HoltropResistanceService.cs`)

**Formulation (Holtrop-Mennen 1984):**
```
1. Froude number: Fn = V / √(g·LWL)
2. Reynolds number: Rn = V·LWL / ν
3. Wetted surface: S ≈ LWL · (2T + B) · √((Cb + 0.5(1-Cb))/2)
4. ITTC-57 friction: Cf = 0.075 / (log₁₀(Rn) - 2)²
5. Form factor: (1+k₁) ≈ 1 + 0.5·Cb (simplified for MVP)
6. Frictional resistance: Rf = 0.5·ρ·V²·S·Cf·(1+k₁)
7. Wave resistance: Rw = f(Fn, Cb, Cp, LWL, B, T) - Holtrop polynomial
8. Total resistance: R = Rf + Rw
9. EHP = R · V (kW)
10. SHP = EHP · (1 + sea_margin) · (1 + service_margin) / η_overall
```

**TODO Markers:**
```csharp
// TODO: CUSTOM_ALGO - Reference SPS SName paper for:
// 1. Modern form factor (1+k₁) corrections for container ships
// 2. Wave resistance refinements at high Fn (0.25-0.30)
// 3. Appendage resistance based on vessel type
// Current implementation uses simplified Holtrop 1984 formulation
```

**Implementation checklist:**
- [ ] ITTC-57 friction coefficient
- [ ] Simplified form factor (1+k₁)
- [ ] Wave resistance (basic polynomial)
- [ ] Total resistance = Rf + Rw
- [ ] EHP and SHP calculations
- [ ] Mark areas for SPS SName improvements

**6.4 Stability Screen** (`Services/Solvers/StabilityScreenService.cs`)

**Quick GM Calculation:**
```
1. Waterplane area: Awp = Cwp · LWL · B
2. Waterplane inertia: Iwp ≈ Cwp · LWL · B³ / 12
3. Transverse metacentric radius: BMt = Iwp / ∇
4. Vertical center of buoyancy: KB ≈ k_B · T  (k_B ≈ 0.53 for typical hulls)
5. Estimate KG from vessel type (container ≈ 0.55·D, tanker ≈ 0.50·D)
6. Transverse metacentric height: GMt = KB + BMt - KG
7. Roll period estimate: T_roll ≈ 2π · k_φ · (B / √(g·GMt))  (k_φ ≈ 0.44)
```

**Implementation checklist:**
- [ ] Iwp calculation
- [ ] BMt calculation
- [ ] KB estimation (type-dependent k_B factor)
- [ ] KG estimation by vessel type
- [ ] GMt calculation
- [ ] Roll period estimate
- [ ] Flag low GM (GMt < 1.0m typical threshold)

**6.5 Froude Targeting** (`Services/Solvers/FroudeTargetingService.cs`)
- [ ] Pick Fn from family preset band
- [ ] Adjust Fn by speed (higher speed → use upper band)
- [ ] Return target Fn for solver

**6.6 Hull Family Service** (`Services/HullFamilyService.cs`)
- [ ] Get applicable families for mission type
- [ ] Filter by constraints (Fn range, cargo density)
- [ ] Return top N families (default 5)

**6.7 Orchestrator** (`Services/Solvers/FirstPrinciplesSolver.cs`)

**Workflow:**
```
1. Parse mission case
2. Convert payload to mass (volume/weight/TEU → tonnes)
3. Estimate total displacement (payload + lightship + fuel + margin)
4. Get applicable hull families
5. For each family:
   a. Pick Fn
   b. Solve LWL
   c. Run displacement closure
   d. Get water properties (cached)
   e. Compute stability
   f. Compute resistance
   g. Calculate wavelength ratio
   h. Generate geometry
   i. Compute score
6. Rank candidates by score
7. Return top N
```

**Implementation checklist:**
- [ ] Payload conversion (volume/weight/TEU)
- [ ] Total displacement estimation (DWT/Δ ratios by type)
- [ ] Multi-family iteration
- [ ] Error handling per family (continue on failure)
- [ ] Candidate ranking
- [ ] Performance: <2s for 5 candidates

**✓ Completion Checklist:**
- Solver generates 3-5 candidates per mission
- Each candidate closes Δ within ±1%
- Fn stays within family band
- Locks respected (keep_fn, keep_l_over_b, etc.)
- Constraints enforced (max beam, draft, LOA)
- Flags computed (draft_exceeded, low_gm, etc.)
- Score computed from KPI weights

---

## Phase 7: Services Layer (Week 3, Days 1-2)

### Goal
Implement business logic services for mission cases, sizing runs, candidates.

### Tasks

**7.1 Mission Case Service** (`Services/IMissionCaseService.cs`)
```csharp
public interface IMissionCaseService
{
    Task<MissionCaseDto> CreateAsync(CreateMissionCaseDto dto, Guid userId, string tenantId, CancellationToken ct);
    Task<PagedResult<MissionCaseDto>> ListAsync(Guid userId, int page, int pageSize, CancellationToken ct);
    Task<MissionCaseDto> GetByIdAsync(Guid id, CancellationToken ct);
    Task<MissionCaseDto> UpdateAsync(Guid id, CreateMissionCaseDto dto, CancellationToken ct);
    Task DeleteAsync(Guid id, CancellationToken ct); // Soft delete
}
```

**Implementation:**
- [ ] CRUD operations with EF Core
- [ ] Tenant isolation (filter by tenant_id)
- [ ] Soft delete (set deleted_at)
- [ ] Pagination for list
- [ ] Validation (FluentValidation)

**7.2 Sizing Run Service** (`Services/ISizingRunService.cs`)
```csharp
public interface ISizingRunService
{
    Task<SizingResultDto> RunSizingAsync(Guid missionCaseId, CreateSizingRunDto dto, Guid userId, CancellationToken ct);
    Task<SizingRunDto> GetRunAsync(Guid runId, CancellationToken ct);
    Task<List<CandidateDesignDto>> GetCandidatesAsync(Guid runId, CancellationToken ct);
}
```

**Implementation:**
- [ ] Create SizingRun record (status=computing)
- [ ] Call FirstPrinciplesSolver
- [ ] Save candidates to database
- [ ] Update run (status=completed, compute_time_ms)
- [ ] Handle errors (status=failed, error_message)
- [ ] Return candidates sorted by rank

**7.3 Candidate Service** (`Services/ICandidateService.cs`)
```csharp
public interface ICandidateService
{
    Task<CandidateDesignDto> GetByIdAsync(Guid id, CancellationToken ct);
    Task<CandidateDesignDto> RecomputeAsync(Guid id, RecomputeRequestDto dto, CancellationToken ct);
    Task<PushToHydrostaticsResultDto> PushToHydrostaticsAsync(Guid id, string vesselName, string idempotencyKey, CancellationToken ct);
}
```

**Implementation:**
- [ ] Get candidate by ID
- [ ] Recompute: adjust parameters, re-run solver for single candidate
- [ ] Push to Hydrostatics: create vessel via DataServiceClient
- [ ] Track idempotency (save to push_operations table)

**✓ Completion Checklist:**
- All services implement interfaces
- Services use async/await throughout
- Tenant isolation enforced
- Error handling with try-catch
- Logging at Info level for operations

---

## Phase 8: Controllers & API (Week 3, Days 3-5)

### Goal
Expose RESTful API endpoints with OpenAPI documentation.

### Tasks

**8.1 Mission Cases Controller** (`Controllers/MissionCasesController.cs`)

**Endpoints:**
- `POST /api/v1/hull-sizing/mission-cases` - Create
- `GET /api/v1/hull-sizing/mission-cases` - List (paginated)
- `GET /api/v1/hull-sizing/mission-cases/{id}` - Get by ID
- `PUT /api/v1/hull-sizing/mission-cases/{id}` - Update
- `DELETE /api/v1/hull-sizing/mission-cases/{id}` - Soft delete

**Implementation checklist:**
- [ ] Extract userId and tenantId from HttpContext.Items
- [ ] Deny if tenantId missing (403 Forbidden)
- [ ] Call MissionCaseService methods
- [ ] Return ProblemDetails on errors (400, 404, etc.)
- [ ] Add XML comments for Swagger docs

**8.2 Sizing Runs Controller** (`Controllers/SizingRunsController.cs`)

**Endpoints:**
- `POST /api/v1/hull-sizing/mission-cases/{missionCaseId}/runs` - Create run
- `GET /api/v1/hull-sizing/runs/{runId}` - Get run details
- `GET /api/v1/hull-sizing/runs/{runId}/candidates` - List candidates

**Implementation checklist:**
- [ ] POST creates run and returns candidates (synchronous)
- [ ] Log compute time
- [ ] Return 202 Accepted if compute >2s (optional enhancement)
- [ ] Include Location header for polling

**8.3 Candidates Controller** (`Controllers/CandidatesController.cs`)

**Endpoints:**
- `GET /api/v1/hull-sizing/candidates/{id}` - Get candidate
- `POST /api/v1/hull-sizing/candidates/{id}/recompute` - Recompute with adjustments
- `POST /api/v1/hull-sizing/candidates/{id}/push-to-hydrostatics` - Create vessel
- `POST /api/v1/hull-sizing/candidates/{id}/export` - Export JSON/CSV (Phase 2)

**Implementation checklist:**
- [ ] Recompute endpoint accepts adjustments (speed, locks)
- [ ] Push-to-hydrostatics requires X-Idempotency-Key header
- [ ] Returns created vessel ID
- [ ] Includes Location header for vessel resource

**8.4 Reference Data Controller** (`Controllers/ReferenceController.cs`)

**Endpoints:**
- `GET /api/v1/hull-sizing/reference/hull-families` - List hull families
- `GET /api/v1/hull-sizing/reference/iso-containers` - List ISO containers
- `GET /api/v1/hull-sizing/reference/kpi-weights` - Get scoring weights

**Implementation checklist:**
- [ ] Return cached reference data
- [ ] No pagination needed (small datasets)
- [ ] Cache responses (5 min TTL)

**✓ Completion Checklist:**
- All controllers use [ApiVersion("1.0")]
- All routes start with /api/v1/hull-sizing/
- All methods have [ProducesResponseType] attributes
- XML comments for Swagger
- ProblemDetails returned on errors
- Tenant isolation enforced

---

## Phase 9: Testing (Week 3-4)

### Goal
Comprehensive unit and integration tests.

### Tasks

**9.1 Unit Tests Project**
```bash
dotnet new xunit -n HullSizingService.Tests -o backend/HullSizingService.Tests
dotnet sln add backend/HullSizingService.Tests
```

**9.2 Solver Tests** (`backend/HullSizingService.Tests/Services/`)
- [ ] `DisplacementClosureServiceTests.cs`
  - Test convergence for known vessels (barge, KCS, KVLCC2)
  - Test lock behavior (keep Fn, keep L/B)
  - Test constraint enforcement (max draft, beam)
- [ ] `HoltropResistanceServiceTests.cs`
  - Test against KCS reference values
  - Test ITTC-57 friction formula
  - Test wave resistance at different Fn
- [ ] `StabilityScreenServiceTests.cs`
  - Test GMt calculation for barge (analytical solution)
  - Test KB estimation
- [ ] `FirstPrinciplesSolverTests.cs`
  - Test full workflow (mission → candidates)
  - Test multi-family generation
  - Test ranking by score

**9.3 Controller Tests** (`backend/HullSizingService.Tests/Controllers/`)
- [ ] `MissionCasesControllerTests.cs` - CRUD operations
- [ ] `SizingRunsControllerTests.cs` - Run creation
- [ ] `CandidatesControllerTests.cs` - Recompute, push-to-hydro

**9.4 Integration Tests**
- [ ] `PushToHydrostaticsIntegrationTests.cs`
  - Mock DataService responses
  - Verify vessel DTO mapping
  - Test idempotency key handling

**9.5 Reference Test Cases** (from test_matrix.csv)
```csharp
[Theory]
[InlineData("container", 58000, 24.0, 230.0, 250.0, 32.0, 36.0)] // Container base
[InlineData("tanker", 300000, 16.0, 320.0, 340.0, 58.0, 62.0)]   // Tanker base
public async Task SolverGeneratesValidCandidateForReferenceCase(
    string missionType, decimal payloadT, decimal speedKn,
    decimal expectedLppMin, decimal expectedLppMax,
    decimal expectedBMin, decimal expectedBMax)
{
    // Arrange
    var mission = new MissionCase { MissionType = missionType, ... };
    
    // Act
    var candidates = await _solver.SolveAsync(mission, new SizingOptions(), CancellationToken.None);
    
    // Assert
    Assert.NotEmpty(candidates);
    var best = candidates.First();
    Assert.InRange(best.LppM, expectedLppMin, expectedLppMax);
    Assert.InRange(best.BM, expectedBMin, expectedBMax);
    Assert.InRange(best.DisplacementT / payloadT - 1, -0.01m, 0.01m); // ±1%
}
```

**✓ Completion Checklist:**
- >80% code coverage for solver services
- All reference test cases pass
- Integration tests pass with mocked DataService
- Controllers return correct status codes
- Validation errors handled properly

---

## Next: Read `04-FRONTEND-PHASES.md` for UI implementation plan
