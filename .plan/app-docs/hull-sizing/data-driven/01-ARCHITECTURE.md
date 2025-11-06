# Data-Driven Mode - Architecture & Service Design

**Last Updated:** November 6, 2025  
**Status:** Planning Complete

---

## Service Topology

```
┌───────────────────────────────────────────────────────────┐
│                    Frontend (React + TS)                   │
│  Mission Wizard → Mode Selection (FP / DD-Real / DD-ML)   │
└─────────────────────────┬─────────────────────────────────┘
                          │ HTTP/REST
                          │
┌─────────────────────────▼─────────────────────────────────┐
│                      API Gateway                           │
│  POST /api/v1/hull-sizing/runs?solverMode={mode}         │
│  - CancellationToken support                              │
│  - JWT validation, Claims forwarding                      │
└──────────────────────────┬────────────────────────────────┘
                           │
┌──────────────────────────▼────────────────────────────────┐
│               HullSizingService (Port 5004)                │
│  ┌──────────────────────────────────────────────────┐    │
│  │ SizingRunsController                              │    │
│  │  - Routes to appropriate solver based on mode     │    │
│  └────┬─────────────────┬───────────────┬───────────┘    │
│       │                 │               │                 │
│  ┌────▼──────┐   ┌──────▼───────┐  ┌───▼──────────┐     │
│  │ First-    │   │ DataDriven   │  │ DataDriven   │     │
│  │ Principles│   │ RealWorld    │  │ Parametric   │     │
│  │ Solver    │   │ Solver       │  │ Solver       │     │
│  │ (existing)│   │ (Phase 1)    │  │ (Phase 2)    │     │
│  └───────────┘   └──────┬───────┘  └───┬──────────┘     │
│                         │               │                 │
│                    ┌────▼───────────────▼────┐           │
│                    │  KNN Search Services     │           │
│                    │  - RealWorldKnnService   │           │
│                    │  - ParametricKnnService  │           │
│                    │  - In-memory cache       │           │
│                    └────┬────────────────┬────┘           │
│                         │                │                 │
│                    ┌────▼────────────────▼────┐           │
│                    │  VesselScalingService     │           │
│                    │  - Ratio-preserving       │           │
│                    │  - Constraint validation  │           │
│                    └──────────┬────────────────┘           │
│                               │                             │
│                         Calls existing                      │
│                    FirstPrinciplesSolver                    │
│                    for refinement                           │
└────────────────────────┬──────────────────────────────────┘
                         │ HTTP (Polly policies)
┌────────────────────────▼──────────────────────────────────┐
│              DataService (Port 5003)                       │
│  ┌──────────────────────────────────────────────────┐    │
│  │ CatalogService                                     │    │
│  │  - Provides water properties                       │    │
│  │  - Hydrostatics calculations                       │    │
│  └───────────┬───────────────────────┬──────────────┘    │
└──────────────┼───────────────────────┼───────────────────┘
               │                       │
               │                       │
┌──────────────▼───────────────────────▼───────────────────┐
│           PostgreSQL Database (Single Instance)           │
│  ┌───────────────────────────────────────────────────┐   │
│  │ Schema: catalog_user (Read-Write)                  │   │
│  │  - catalog_vessels_real (600 + user additions)     │   │
│  │  - vessel_id, vessel_type, lpp_m, beam_m, ...      │   │
│  │  - displacement_t, cb, cp, cm, service_speed_ms    │   │
│  │  - dwt_t, resistance_curve (JSON), geometry_file   │   │
│  │  - is_system_data (boolean), created_by (uuid)     │   │
│  ├───────────────────────────────────────────────────┤   │
│  │ Schema: catalog_ml (Read-Only)                     │   │
│  │  - parametric_hulls (82,168 synthetic designs)     │   │
│  │  - hull_id, parametric_vector (JSONB - 45 params)  │   │
│  │  - loa_m, bd_m, dd_m, bs_m (extracted dims)        │   │
│  │  - geometric_measures (JSONB per draft ratio)      │   │
│  │  - volume, lcb, vcb, area_wp, area_ws, cw, ixx... │   │
│  ├───────────────────────────────────────────────────┤   │
│  │ Schema: sizing (HullSizingService)                 │   │
│  │  - mission_cases (with solver_mode enum)           │   │
│  │  - candidate_designs (with provenance fields)      │   │
│  │    * reference_vessel_id (FK to catalog)           │   │
│  │    * similarity_score (0-100%)                     │   │
│  │    * solver_mode (FirstPrinciples/DataDrivenReal/  │   │
│  │                   DataDrivenML)                     │   │
│  └───────────────────────────────────────────────────┘   │
└───────────────────────────────────────────────────────────┘
```

---

## Data-Driven Solver Components

### Phase 1: Real-World Solver

```csharp
public class DataDrivenRealWorldSolver : ISolver
{
    private readonly RealWorldKnnService _knnService;
    private readonly VesselScalingService _scalingService;
    private readonly FirstPrinciplesSolver _refinementSolver;
    
    public async Task<List<CandidateDesign>> SolveAsync(
        MissionCase mission,
        CancellationToken cancellationToken = default)
    {
        // 1. KNN Search (mission-based features)
        var similarVessels = await _knnService.FindSimilarVesselsAsync(
            mission, K: 5, cancellationToken);
        
        // 2. Scale each to target displacement
        var scaled = new List<ScaledCandidate>();
        foreach (var vessel in similarVessels)
        {
            var candidate = _scalingService.ScaleToTarget(
                vessel, mission.TargetDisplacement, mission.Constraints);
            
            if (candidate.IsValid)
                scaled.Add(candidate);
        }
        
        // 3. Refine with first-principles (displacement closure)
        var refined = new List<CandidateDesign>();
        foreach (var candidate in scaled)
        {
            var result = await _refinementSolver.RefineAsync(
                candidate, mission, cancellationToken);
            
            result.ReferenceVesselId = candidate.SourceVesselId;
            result.SimilarityScore = candidate.SimilarityScore;
            result.SolverMode = SolverMode.DataDrivenRealWorld;
            
            refined.Add(result);
        }
        
        // 4. Rank and return top 5
        return refined.OrderByDescending(c => c.Score).Take(5).ToList();
    }
}
```

### Phase 2: ML/Parametric Solver

```csharp
public class DataDrivenParametricSolver : ISolver
{
    private readonly ParametricKnnService _knnService;
    private readonly ParametricConverter _converter;
    private readonly VesselScalingService _scalingService;
    private readonly FirstPrinciplesSolver _refinementSolver;
    
    public async Task<List<CandidateDesign>> SolveAsync(
        MissionCase mission,
        CancellationToken cancellationToken = default)
    {
        // 1. Derive target geometry from mission
        var targetGeometry = EstimateGeometryFromMission(mission);
        
        // 2. KNN Search (geometry-based: 45 parametric features)
        var similarHulls = await _knnService.FindSimilarHullsAsync(
            targetGeometry, K: 5, cancellationToken);
        
        // 3. Convert parametric → principal dimensions
        var converted = new List<ConvertedHull>();
        foreach (var hull in similarHulls)
        {
            var principal = _converter.ConvertToP

rincipalDimensions(hull);
            converted.Add(principal);
        }
        
        // 4. Scale and refine (same as Real-World)
        // ... (similar logic)
    }
}
```

---

## Database Schema Design

### Schema: catalog_user (User-Editable)

```sql
CREATE SCHEMA IF NOT EXISTS catalog_user;

CREATE TABLE catalog_user.vessels_real (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vessel_id TEXT UNIQUE NOT NULL,
    vessel_type TEXT NOT NULL,
    
    -- Principal Dimensions
    lpp_m DECIMAL(10,3) NOT NULL CHECK (lpp_m > 0),
    beam_m DECIMAL(10,3) NOT NULL CHECK (beam_m > 0),
    draft_m DECIMAL(10,3) NOT NULL CHECK (draft_m > 0),
    depth_m DECIMAL(10,3) CHECK (depth_m > 0),
    displacement_t DECIMAL(12,2) NOT NULL CHECK (displacement_t > 0),
    
    -- Form Coefficients
    cb DECIMAL(5,4) NOT NULL CHECK (cb BETWEEN 0.3 AND 0.95),
    cp DECIMAL(5,4) CHECK (cp BETWEEN 0.5 AND 1.0),
    cm DECIMAL(5,4) CHECK (cm BETWEEN 0.7 AND 1.0),
    cw DECIMAL(5,4) CHECK (cw BETWEEN 0.5 AND 1.0),
    
    -- Performance
    service_speed_ms DECIMAL(6,3) CHECK (service_speed_ms > 0),
    dwt_t DECIMAL(12,2) CHECK (dwt_t >= 0),
    
    -- Additional Data
    engine_type TEXT,
    year_built INTEGER CHECK (year_built BETWEEN 1900 AND 2100),
    source TEXT,
    data_quality TEXT,
    
    -- Geometry & Performance Data (JSONB)
    resistance_curve JSONB,  -- {"Fn": [...], "Resistance_N": [...]}
    hull_geometry_file TEXT,
    
    -- Permissions
    is_system_data BOOLEAN DEFAULT TRUE,  -- TRUE = read-only seeded data
    created_by UUID,  -- NULL for system, user ID for user-added
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    CONSTRAINT chk_system_or_user CHECK (
        (is_system_data = TRUE AND created_by IS NULL) OR
        (is_system_data = FALSE AND created_by IS NOT NULL)
    )
);

-- Indexes for KNN search
CREATE INDEX idx_vessel_type ON catalog_user.vessels_real(vessel_type);
CREATE INDEX idx_displacement ON catalog_user.vessels_real(displacement_t);
CREATE INDEX idx_service_speed ON catalog_user.vessels_real(service_speed_ms);
CREATE INDEX idx_lpp_beam_draft ON catalog_user.vessels_real(lpp_m, beam_m, draft_m);
```

### Schema: catalog_ml (Read-Only, System-Managed)

```sql
CREATE SCHEMA IF NOT EXISTS catalog_ml;

CREATE TABLE catalog_ml.parametric_hulls (
    id SERIAL PRIMARY KEY,
    hull_id TEXT UNIQUE NOT NULL,
    dataset_source TEXT NOT NULL,  -- 'Constrained_Set_1', 'Diffusion_Aug_1', etc.
    
    -- Parametric Vector (45 parameters as JSONB)
    parametric_vector JSONB NOT NULL,
    /* 
    {
        "LOA": 10.0, "Lb": 0.487, "Ls": 0.351, "Bd": 0.324, "Dd": 0.067,
        "Bs": 0.894, "WL": 0.341, "Bc": 0.266, "Beta": 16.44,
        ... (45 parameters total)
    }
    */
    
    -- Extracted Principal Dimensions (for faster querying)
    loa_m DECIMAL(10,3) NOT NULL CHECK (loa_m > 0),
    bd_normalized DECIMAL(8,6) NOT NULL,  -- Beam/Depth ratio
    dd_normalized DECIMAL(8,6) NOT NULL,  -- Draft/Depth ratio
    bs_normalized DECIMAL(8,6) NOT NULL,  -- Something/Depth ratio
    
    -- Geometric Measures at Multiple Drafts (JSONB)
    geometric_measures JSONB NOT NULL,
    /*
    {
        "Volume": [0.000163, 0.000698, ...],  // 10 draft ratios
        "LCB": [0.576, 0.581, ...],
        "VCB": [...],
        "Area_WP": [...],
        "Area_WS": [...],
        "Cw": [...],
        "Ixx": [...],
        "Iyy": [...]
    }
    */
    
    -- Computed at Design Draft (T/Dd = 0.5 typically)
    volume_normalized DECIMAL(12,8),  -- Volume/LOA^3
    lcb_normalized DECIMAL(6,4),  -- LCB/LOA
    vcb_normalized DECIMAL(6,4),  -- VCB/Dd
    cw_coeff DECIMAL(5,4),
    
    -- Metadata
    imported_at TIMESTAMPTZ DEFAULT NOW(),
    data_version INTEGER DEFAULT 1
);

-- Indexes for geometric KNN search
CREATE INDEX idx_loa ON catalog_ml.parametric_hulls(loa_m);
CREATE INDEX idx_volume ON catalog_ml.parametric_hulls(volume_normalized);
CREATE INDEX idx_lcb ON catalog_ml.parametric_hulls(lcb_normalized);
CREATE INDEX idx_parametric_gin ON catalog_ml.parametric_hulls USING GIN(parametric_vector);
CREATE INDEX idx_geometric_gin ON catalog_ml.parametric_hulls USING GIN(geometric_measures);

-- Read-only permissions (enforced at application layer)
-- In production, GRANT SELECT only to app_user
```

---

## Solver Mode Enum

```csharp
public enum SolverMode
{
    FirstPrinciples = 0,      // Pure physics-based
    DataDrivenRealWorld = 1,  // KNN on 600 real vessels
    DataDrivenParametric = 2  // KNN on 82K ML hulls
}
```

---

## KNN Services Architecture

### Real-World KNN (Mission-Based)

```csharp
public class RealWorldKnnService
{
    private readonly IMemoryCache _cache;
    private List<VesselCatalogEntry> _catalog;  // All 600 in memory
    
    public async Task<List<SimilarVessel>> FindSimilarVesselsAsync(
        MissionCase mission,
        int K = 5,
        CancellationToken cancellationToken = default)
    {
        // 1. Load catalog from cache (or DB if cold)
        _catalog = await GetCatalogAsync(cancellationToken);
        
        // 2. Filter by vessel type (primary)
        var sameType = _catalog
            .Where(v => v.VesselType == mission.VesselType)
            .ToList();
        
        // 3. Extract mission features
        var targetVector = new double[]
        {
            mission.CargoValue,  // or Displacement
            mission.ServiceSpeedKn,
            mission.MaxBeam ?? 999,
            mission.MaxDraft ?? 999
        };
        
        // 4. Normalize features
        var normalized = NormalizeFeatures(targetVector);
        
        // 5. Calculate weighted Euclidean distance
        var distances = sameType.Select(v => new
        {
            Vessel = v,
            Distance = CalculateDistance(normalized, v.FeatureVector)
        }).OrderBy(x => x.Distance).ToList();
        
        // 6. Return top K (or fallback to all types if <3)
        if (distances.Count < 3)
        {
            // Fallback: search all types
            distances = _catalog.Select(v => new {
                Vessel = v,
                Distance = CalculateDistance(normalized, v.FeatureVector)
            }).OrderBy(x => x.Distance).ToList();
        }
        
        return distances.Take(K).Select(d => new SimilarVessel
        {
            VesselId = d.Vessel.VesselId,
            VesselName = d.Vessel.VesselId,  // From CSV
            SimilarityScore = 1.0 - (d.Distance / MaxDistance),  // 0-1
            Vessel = d.Vessel
        }).ToList();
    }
    
    private double CalculateDistance(double[] target, double[] candidate)
    {
        // Weighted Euclidean distance
        var weights = new[] { 0.30, 0.20, 0.15, 0.15 };  // Disp, Speed, Beam, Draft
        
        double sum = 0;
        for (int i = 0; i < target.Length; i++)
        {
            sum += weights[i] * Math.Pow(target[i] - candidate[i], 2);
        }
        return Math.Sqrt(sum);
    }
}
```

### Parametric KNN (Geometry-Based)

```csharp
public class ParametricKnnService
{
    private readonly IMemoryCache _cache;
    private List<ParametricHull> _catalog;  // Subset in memory (10K?)
    
    public async Task<List<SimilarParametricHull>> FindSimilarHullsAsync(
        GeometryTarget target,
        int K = 5,
        CancellationToken cancellationToken = default)
    {
        // 1. Load catalog subset (or query DB for geometric search)
        _catalog = await GetParametricCatalogAsync(cancellationToken);
        
        // 2. Extract target geometry features (from mission or user input)
        var targetVector = new double[]
        {
            target.EstimatedLoa,
            target.EstimatedBdRatio,
            target.EstimatedDdRatio,
            target.TargetVolume
        };
        
        // 3. Normalize
        var normalized = NormalizeGeometricFeatures(targetVector);
        
        // 4. Calculate distance (geometric similarity)
        var distances = _catalog.Select(h => new
        {
            Hull = h,
            Distance = CalculateGeometricDistance(normalized, h.ExtractedFeatures)
        }).OrderBy(x => x.Distance).ToList();
        
        // 5. Return top K
        return distances.Take(K).Select(d => new SimilarParametricHull
        {
            HullId = d.Hull.HullId,
            SimilarityScore = 1.0 - (d.Distance / MaxDistance),
            Hull = d.Hull
        }).ToList();
    }
}
```

---

## Scaling Service (Shared by Both)

```csharp
public class VesselScalingService
{
    public ScaledCandidate ScaleToTarget(
        VesselCatalogEntry reference,
        decimal targetDisplacement,
        MissionConstraints constraints)
    {
        // Cube-root scaling (preserve ratios)
        var scaleFactor = (decimal)Math.Pow(
            (double)(targetDisplacement / reference.Displacement), 
            1.0/3.0
        );
        
        var scaled = new ScaledCandidate
        {
            Lpp = reference.Lpp * scaleFactor,
            Beam = reference.Beam * scaleFactor,
            Draft = reference.Draft * scaleFactor,
            Depth = reference.Depth * scaleFactor,
            
            // Preserve form coefficients
            Cb = reference.Cb,
            Cp = reference.Cp ?? EstimateCp(reference.Cb),
            Cm = reference.Cm ?? EstimateCm(reference.Cb, reference.Cp),
            
            SourceVesselId = reference.Id,
            SimilarityScore = /* from KNN */
        };
        
        // Validate constraints
        if (constraints.MaxBeam.HasValue && scaled.Beam > constraints.MaxBeam)
        {
            // Attempt clamping
            scaled.Beam = constraints.MaxBeam.Value;
            // Re-adjust T to maintain displacement
            scaled.Draft = CalculateDraftForBeam(scaled, targetDisplacement);
        }
        
        if (constraints.MaxDraft.HasValue && scaled.Draft > constraints.MaxDraft)
        {
            scaled.Draft = constraints.MaxDraft.Value;
            scaled.Beam = CalculateBeamForDraft(scaled, targetDisplacement);
        }
        
        // Check if distortion acceptable (<10%)
        var distortion = CalculateDistortion(scaled, reference);
        scaled.IsValid = distortion < 0.10;
        
        return scaled;
    }
}
```

---

## API Flow with CancellationToken

```csharp
[HttpPost("runs")]
public async Task<ActionResult<SizingRunDto>> CreateRun(
    [FromBody] CreateSizingRunRequest request,
    [FromQuery] SolverMode solverMode = SolverMode.FirstPrinciples,
    CancellationToken cancellationToken = default)  // ✅ CancellationToken
{
    // Feature flag check
    if (solverMode == SolverMode.DataDrivenRealWorld && 
        !_config.GetValue<bool>("FeatureFlags:DataDrivenReal"))
    {
        return BadRequest("Data-Driven Real-World mode is not enabled");
    }
    
    if (solverMode == SolverMode.DataDrivenParametric && 
        !_config.GetValue<bool>("FeatureFlags:DataDrivenML"))
    {
        return BadRequest("Data-Driven ML/Parametric mode is not enabled");
    }
    
    // Select solver
    ISolver solver = solverMode switch
    {
        SolverMode.FirstPrinciples => _firstPrinciplesSolver,
        SolverMode.DataDrivenRealWorld => _dataDrivenRealSolver,
        SolverMode.DataDrivenParametric => _dataDrivenParametricSolver,
        _ => throw new ArgumentException($"Unknown solver mode: {solverMode}")
    };
    
    // Execute with cancellation support
    var candidates = await solver.SolveAsync(mission, cancellationToken);
    
    // Save results with provenance
    var run = await _runService.SaveRunAsync(mission.Id, candidates, cancellationToken);
    
    return Ok(run);
}
```

---

## Key Architectural Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Database Schemas** | Separate (`catalog_user`, `catalog_ml`) | Clear permissions, DB-enforced read-only for ML |
| **KNN Services** | Two separate implementations | Optimized for different feature types |
| **Scaling** | Shared service | Same algorithm works for both catalogs |
| **Caching** | In-memory for Real (600), partial for ML (10K) | Balance memory vs performance |
| **Feature Flags** | Independent (DataDrivenReal, DataDrivenML) | Gradual rollout, independent enabling |
| **Cancellation** | All async methods support CancellationToken | User can abort long operations |
| **Seeding** | Immediate for Real, background for ML | Don't block startup |

---

## Communication Patterns

### HullSizingService → DataService

**Existing (unchanged):**
- GET `/catalog/water-properties` - Water density, viscosity
- POST `/hydrostatics/vessels` - Create vessel from candidate

**New (Phase 1):**
- GET `/catalog/vessels-real` - Query real-world catalog
- GET `/catalog/vessels-real/{id}` - Get specific vessel details

**New (Phase 2):**
- GET `/catalog/parametric-hulls` - Query ML catalog (with filters)
- GET `/catalog/parametric-hulls/{id}` - Get specific hull + geometry

### Resilience (Polly Policies)

**Already implemented:**
- ✅ Timeout: 2s
- ✅ Retry: 3x with jitter
- ✅ Circuit Breaker: 5 failures / 30s

**Enhancement for catalog:**
- Cache catalog in memory (1-hour TTL)
- Fallback to First-Principles if catalog unavailable

---

## Performance Targets

| Operation | Target | Notes |
|-----------|--------|-------|
| Real-World KNN search | <100ms | 600 vessels in-memory |
| Parametric KNN search | <500ms | May query DB or cache subset |
| Scaling algorithm | <50ms per candidate | Pure math, no I/O |
| Full workflow (Real) | <1s | KNN + scale + refine |
| Full workflow (ML) | <1.5s | Includes conversion overhead |
| Catalog load (Real) | <200ms | On first query, then cached |
| Catalog import (ML) | Background | 82K hulls, don't block startup |

---

## Security & Permissions

### Database Level

```sql
-- Application user (HullSizingService, DataService)
GRANT SELECT ON catalog_ml.parametric_hulls TO app_user;  -- Read-only
GRANT SELECT, INSERT, UPDATE ON catalog_user.vessels_real TO app_user;

-- Prevent deletion of system data
CREATE POLICY system_data_immutable ON catalog_user.vessels_real
FOR DELETE
USING (is_system_data = FALSE);  -- Can only delete user-added vessels
```

### Application Level

```csharp
// In CatalogService
public async Task<Result> DeleteVesselAsync(UUID vesselId, UUID userId)
{
    var vessel = await _context.VesselsReal.FindAsync(vesselId);
    
    if (vessel.IsSystemData)
    {
        return Result.Failure("Cannot delete system catalog data. ML catalog is read-only.");
    }
    
    if (vessel.CreatedBy != userId)
    {
        return Result.Failure("You can only delete vessels you created.");
    }
    
    _context.VesselsReal.Remove(vessel);
    await _context.SaveChangesAsync();
    return Result.Success();
}
```

---

## Data Flow Diagrams

### Real-World Data-Driven Flow

```
Mission Input
  │
  ├─> Extract Features (VesselType, Cargo, Speed, Constraints)
  │
  ├─> RealWorldKnnService
  │     └─> Search 600 vessels (in-memory)
  │     └─> Calculate weighted distance
  │     └─> Return top 5 similar vessels
  │
  ├─> For each similar vessel:
  │     └─> VesselScalingService
  │           └─> Scale to target displacement
  │           └─> Validate constraints
  │           └─> Check distortion <10%
  │
  ├─> For each scaled candidate:
  │     └─> FirstPrinciplesSolver (refinement)
  │           └─> Displacement closure
  │           └─> Physics validation
  │
  └─> Rank by score, attach provenance
      └─> Return top 5 candidates
```

### ML/Parametric Data-Driven Flow

```
Mission Input
  │
  ├─> Derive Target Geometry (estimate Lpp, B, T from payload)
  │
  ├─> ParametricKnnService
  │     └─> Search 82K hulls (DB query or cached subset)
  │     └─> Calculate geometric distance (45-D vector)
  │     └─> Return top 5 similar hulls
  │
  ├─> For each similar hull:
  │     └─> ParametricConverter
  │           └─> Extract geometric measures
  │           └─> Convert 45 params → Lpp, B, T, Cb
  │           └─> Derive form coefficients
  │
  ├─> VesselScalingService (same as Real-World)
  │
  ├─> FirstPrinciplesSolver refinement
  │
  └─> Return ranked candidates
```

---

## Fallback Strategy

```csharp
public async Task<List<CandidateDesign>> SolveWithFallbackAsync(
    MissionCase mission,
    SolverMode requestedMode,
    CancellationToken cancellationToken)
{
    try
    {
        var solver = GetSolver(requestedMode);
        var candidates = await solver.SolveAsync(mission, cancellationToken);
        
        // Check if results acceptable
        if (candidates.Count < 3 || candidates.Any(c => c.SimilarityScore < 0.5))
        {
            _logger.LogWarning(
                "Data-Driven mode returned low-confidence results. " +
                "Falling back to First-Principles.");
            
            candidates = await _firstPrinciplesSolver.SolveAsync(
                mission, cancellationToken);
        }
        
        return candidates;
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        _logger.LogError(ex, "Data-Driven solver failed. Falling back to First-Principles.");
        return await _firstPrinciplesSolver.SolveAsync(mission, cancellationToken);
    }
}
```

---

## Feature Flags Configuration

```json
{
  "FeatureFlags": {
    "DataDrivenReal": false,      // Enable after Phase 1 testing
    "DataDrivenML": false,         // Enable after Phase 2 testing
    "CatalogUserAdditions": true   // Users can add vessels
  },
  "CatalogSettings": {
    "RealWorldCacheDuration": "01:00:00",  // 1 hour
    "MLCacheDuration": "06:00:00",          // 6 hours
    "KnnDefaultK": 5,
    "MinimumSimilarityThreshold": 0.5,
    "MaxScaleDistortion": 0.10  // 10% max distortion
  }
}
```

---

## Monitoring & Observability

### Metrics to Track

```csharp
// Custom metrics
_metrics.RecordSolverModeSelection(solverMode);
_metrics.RecordKnnSearchTime(elapsed);
_metrics.RecordSimilarityScore(averageScore);
_metrics.RecordFallbackEvent(fromMode, reason);
```

### Structured Logging

```csharp
_logger.LogInformation(
    "Data-Driven search completed. Mode: {Mode}, K: {K}, " +
    "AvgSimilarity: {Similarity:P}, Duration: {Duration}ms",
    solverMode, K, avgSimilarity, elapsed.TotalMilliseconds);
```

---

## Next Documents

- `02-DATABASE-SCHEMA.md` - Complete DDL, migrations, seed strategy
- `03-PHASE1-REAL-WORLD.md` - Detailed implementation plan for Real-World catalog
- `04-PHASE2-ML-PARAMETRIC.md` - Detailed implementation plan for ML catalog
- `05-KNN-ALGORITHMS.md` - Feature engineering, normalization, distance metrics

**Document 2/15 Complete**

