# Data-Driven Mode - Architecture & Schema Diagrams

**Generated:** November 6, 2025  
**Status:** Complete

---

## System Architecture Diagram

```
┌────────────────────────────────────────────────────────────────────┐
│                         USER INTERFACE                              │
│                      React + TypeScript                             │
├────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ Mission Wizard                                                │ │
│  │  Step 1: Cargo → Step 2: Speed → Step 3: Constraints          │ │
│  │  Step 4: **Mode Selection** ← NEW                             │ │
│  │    ┌──────────────┐  ┌──────────────────┐                   │ │
│  │    │ 🧮 First-    │  │ 📊 Data-Driven  │                   │ │
│  │    │   Principles │  │    (600 vessels)│  ← User Clicks    │ │
│  │    └──────────────┘  └──────────────────┘                   │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                         │                                          │
│                         │ POST /api/v1/hull-sizing/runs            │
│                         │ { mode: "data_driven_real" }             │
└─────────────────────────┼──────────────────────────────────────────┘
                          │
                          ▼
┌────────────────────────────────────────────────────────────────────┐
│                        API GATEWAY                                  │
│                     (Port 5001, .NET 8)                            │
│  - JWT Validation                                                  │
│  - Claims Forwarding (user_id, tenant_id)                         │
│  - Routing                                                         │
└─────────────────────────┬──────────────────────────────────────────┘
                          │
                          ▼
┌────────────────────────────────────────────────────────────────────┐
│                    HULL SIZING SERVICE                              │
│                     (Port 5004, .NET 8)                            │
├────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ SizingRunsController                                          │ │
│  │   POST /runs → SizingRunService.CreateAsync()                │ │
│  └────────────────────────┬─────────────────────────────────────┘ │
│                           │                                        │
│                           ▼                                        │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ SizingRunService - Solver Router                             │ │
│  │   if (mode == "data_driven_real")                            │ │
│  │     └─> DataDrivenRealWorldSolver.SolveAsync()               │ │
│  │   else                                                        │ │
│  │     └─> FirstPrinciplesSolver.SolveAsync()                   │ │
│  └────────────────────────┬─────────────────┬───────────────────┘ │
│                           │                 │                      │
│             ┌─────────────┘                 └─────────────┐       │
│             ▼                                             ▼       │
│  ┌─────────────────────────┐              ┌──────────────────┐  │
│  │ DataDrivenRealWorldSolver│              │ FirstPrinciples │  │
│  │                          │              │ Solver          │  │
│  │ Step 1: KNN Search ──────┼──────┐      │ (Existing)      │  │
│  │ Step 2: Scaling          │      │      └──────────────────┘  │
│  │ Step 3: Refine (uses FP) │      │                            │
│  │ Step 4: Rank & Provenance│      │                            │
│  └──────────────────────────┘      │                            │
│                                     │                            │
└─────────────────────────────────────┼────────────────────────────┘
                                      │ POST /api/v1/catalog/vessels/search-similar
                                      │ { vesselType, targetDisplacement, speed, K }
                                      ▼
┌────────────────────────────────────────────────────────────────────┐
│                      DATA SERVICE                                   │
│                     (Port 5003, .NET 8)                            │
├────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ CatalogVesselsController                                      │ │
│  │   POST /search-similar → RealWorldKnnService                 │ │
│  └────────────────────────┬─────────────────────────────────────┘ │
│                           │                                        │
│                           ▼                                        │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ RealWorldKnnService                                           │ │
│  │   1. Load 600 vessels from cache (or DB)                     │ │
│  │   2. Filter by vessel type                                   │ │
│  │   3. Calculate weighted Euclidean distance                   │ │
│  │   4. Return top K similar vessels                            │ │
│  └────────────────────────┬─────────────────────────────────────┘ │
│                           │                                        │
│                  Cache (MemoryCache, 1hr TTL)                     │
│                           │                                        │
└───────────────────────────┼────────────────────────────────────────┘
                            │
                            ▼
┌────────────────────────────────────────────────────────────────────┐
│                      POSTGRESQL DATABASE                            │
│                   (RDS, PostgreSQL 16.4)                           │
├────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ Schema: catalog_user (Read-Write)                             │ │
│  │   └─ vessels_real (600 rows)                                 │ │
│  │      - Real-world vessel catalog                              │ │
│  │      - Indexed for KNN performance                            │ │
│  │      - is_system_data flag for permissions                    │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌──────────────────────────────────────────────────────────────┐ │
│  │ Schema: sizing (HullSizingService)                            │ │
│  │   ├─ mission_cases                                            │ │
│  │   ├─ sizing_runs                                              │ │
│  │   └─ candidate_designs ← Enhanced                             │ │
│  │      - reference_vessel_id (new)                              │ │
│  │      - reference_vessel_name (new)                            │ │
│  │      - similarity_score (new)                                 │ │
│  │      - solver_mode (new)                                      │ │
│  └──────────────────────────────────────────────────────────────┘ │
│                                                                     │
└────────────────────────────────────────────────────────────────────┘
```

---

## Data Flow Diagram

```
┌─────────────┐
│ USER SUBMITS│
│  MISSION    │
│  (Step 4)   │
└──────┬──────┘
       │
       │ 1. POST /hull-sizing/runs { mode: "data_driven_real" }
       ▼
┌────────────────────────────────────────┐
│ HullSizingService                      │
│ SizingRunService.CreateAsync()         │
└──────┬─────────────────────────────────┘
       │
       │ 2. if (mode == "data_driven_real")
       ▼
┌────────────────────────────────────────┐
│ DataDrivenRealWorldSolver              │
│                                        │
│ ┌────────────────────────────────────┐│
│ │ Step 1: KNN Search                 ││
│ │  POST /catalog/vessels/search-     ││
│ │  similar                           ││
│ └────────┬───────────────────────────┘│
│          │                             │
│          │ [KCS, Emma, Madrid, MSC,    │
│          │  OOCL] + similarity scores  │
│          ▼                             │
│ ┌────────────────────────────────────┐│
│ │ Step 2: Scaling                    ││
│ │  VesselScalingService × 5          ││
│ │  (Cube-root law, ratio pres.)      ││
│ └────────┬───────────────────────────┘│
│          │                             │
│          │ [3/5 valid after distortion │
│          │  check]                     │
│          ▼                             │
│ ┌────────────────────────────────────┐│
│ │ Step 3: Refinement                 ││
│ │  FirstPrinciplesSolver × 3         ││
│ │  (Displacement closure, Holtrop)   ││
│ └────────┬───────────────────────────┘│
│          │                             │
│          │ [3 refined candidates]      │
│          ▼                             │
│ ┌────────────────────────────────────┐│
│ │ Step 4: Rank & Attach Provenance   ││
│ │  Sort by score, add:               ││
│ │  - referenceVesselId               ││
│ │  - referenceVesselName             ││
│ │  - similarityScore                 ││
│ │  - solverMode                      ││
│ └────────┬───────────────────────────┘│
│          │                             │
└──────────┼─────────────────────────────┘
           │
           │ 3. Save to database
           ▼
┌────────────────────────────────────────┐
│ PostgreSQL: sizing.candidate_designs   │
│                                        │
│ INSERT INTO candidate_designs (        │
│   ...,                                 │
│   reference_vessel_id: "abc-123",     │
│   reference_vessel_name: "KCS",       │
│   similarity_score: 0.87,             │
│   solver_mode: "DataDrivenRealWorld"  │
│ );                                     │
└──────┬─────────────────────────────────┘
       │
       │ 4. Return SizingRunDto
       ▼
┌────────────────────────────────────────┐
│ FRONTEND                               │
│ SizingRunResults.tsx                   │
│                                        │
│ GET /runs/{id}/candidates              │
│                                        │
│ ┌────────────────────────────────────┐│
│ │ CandidateCard                      ││
│ │                                    ││
│ │ if (solverMode.includes("Data")) { ││
│ │   <ProvenancePanel>                ││
│ │     Reference: KCS                 ││
│ │     Similarity: ████████░░ 87%     ││
│ │   </ProvenancePanel>               ││
│ │ }                                  ││
│ └────────────────────────────────────┘│
└────────────────────────────────────────┘
```

---

## Database Schema Diagram

```
┌───────────────────────────────────────────────────────────────────────┐
│                      POSTGRESQL DATABASE                              │
└───────────────────────────────────────────────────────────────────────┘
          │
          ├─────────────────────────────────────────────────────────────┐
          │                                                             │
          ▼                                                             ▼
┌─────────────────────────────┐                  ┌─────────────────────────────┐
│ Schema: catalog_user         │                  │ Schema: sizing              │
│ (DataService owns)           │                  │ (HullSizingService owns)    │
├─────────────────────────────┤                  ├─────────────────────────────┤
│                              │                  │                             │
│ Table: vessels_real          │                  │ Table: mission_cases        │
│ ├─ id (PK, UUID)            │                  │ ├─ id (PK)                  │
│ ├─ vessel_id (UNIQUE)       │                  │ ├─ user_id                  │
│ ├─ vessel_type              │                  │ ├─ mission_type             │
│ ├─ lpp_m                    │                  │ ├─ cargo_basis              │
│ ├─ beam_m                   │                  │ ├─ service_speed_kn         │
│ ├─ draft_m                  │                  │ ├─ cap_beam_m               │
│ ├─ depth_m                  │                  │ └─ cap_draft_m              │
│ ├─ displacement_t           │                  │                             │
│ ├─ cb, cp, cm, cw           │                  │ Table: sizing_runs          │
│ ├─ service_speed_ms         │                  │ ├─ id (PK)                  │
│ ├─ dwt_t                    │                  │ ├─ mission_case_id (FK)     │
│ ├─ engine_type              │                  │ ├─ mode ← NEW enum          │
│ ├─ year_built               │                  │ ├─ status                   │
│ ├─ source                   │                  │ └─ compute_time_ms          │
│ ├─ data_quality             │                  │                             │
│ ├─ resistance_curve (JSONB) │                  │ Table: candidate_designs    │
│ ├─ hull_geometry_file       │                  │ ├─ id (PK)                  │
│ ├─ is_system_data           │                  │ ├─ sizing_run_id (FK)       │
│ ├─ created_by (nullable)    │                  │ ├─ hull_family              │
│ ├─ created_at               │                  │ ├─ lpp_m, b_m, t_m, d_m     │
│ └─ updated_at               │                  │ ├─ cb, cp, cwp, cm          │
│                              │                  │ ├─ displacement_t           │
│ Indexes (7):                 │                  │ ├─ score, rank              │
│ - idx_vessels_real_type     │                  │ ├─ flags_json               │
│ - idx_vessels_real_displacement                │ │                             │
│ - idx_vessels_real_speed    │    ┌─────────────┤ │ **NEW Provenance Fields:** │
│ - idx_vessels_real_dims     │    │             │ ├─ reference_vessel_id      │
│ - idx_vessels_real_cb       │    │  Foreign    │ ├─ reference_vessel_name    │
│ - idx_vessels_real_system   │    │  Key        │ ├─ similarity_score         │
│ - idx_vessels_real_resistance_gin │  (Logical) │ └─ solver_mode              │
│                              │    │             │                             │
│ Row Count: 600               │    │             │ Indexes (2):                │
│ Size: ~150 KB                │    │             │ - idx_candidate_solver_mode │
└──────────────────────────────┘    │             │ - (existing indexes)        │
                                     │             │                             │
                                     │             │ Row Count: ~10K (growing)   │
                                     │             │ Size: ~5 MB                 │
                                     │             └─────────────────────────────┘
                                     │
                                     └─ Provenance Link (reference_vessel_id → vessels_real.id)
                                        (Not enforced as FK to allow flexibility)
```

---

## Class Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│ DataDrivenRealWorldSolver                                        │
├─────────────────────────────────────────────────────────────────┤
│ - _dataServiceClient: IDataServiceClient                        │
│ - _scalingService: VesselScalingService                         │
│ - _firstPrinciplesSolver: IFirstPrinciplesSolver                │
│ - _logger: ILogger                                              │
├─────────────────────────────────────────────────────────────────┤
│ + SolveAsync(request, ct): Task<List<SolverCandidate>>         │
│ - FindSimilarVesselsAsync(mission, K, ct): Task<List<Similar>> │
│ - ScaleVessels(...): List<(Scaled, Source)>                    │
│ - RefineWithPhysicsAsync(...): Task<List<SolverCandidate>>     │
│ - FallbackToFirstPrinciplesAsync(...): Task<List<...>>         │
│ - CalculateTargetDisplacement(mission): decimal                │
│ - ConvertToVesselModel(dto): CatalogVesselReal                 │
└────────────┬────────────────────────┬───────────────────────────┘
             │ uses                   │ uses
             ▼                        ▼
┌──────────────────────────┐  ┌──────────────────────────────────┐
│ RealWorldKnnService      │  │ VesselScalingService             │
├──────────────────────────┤  ├──────────────────────────────────┤
│ - _context: DbContext    │  │ - _logger: ILogger               │
│ - _cache: IMemoryCache   │  ├──────────────────────────────────┤
│ - _logger: ILogger       │  │ + ScaleToTarget(                 │
├──────────────────────────┤  │     reference,                   │
│ + FindSimilarVesselsAsync│  │     targetDisp,                  │
│     (criteria, K, ct)    │  │     constraints)                 │
│ - ExtractFeatures(...)   │  │     : ScaledCandidate            │
│ - NormalizeFeatures(...) │  │ - ApplyConstraints(...)          │
│ - CalculateDistance(...) │  │ - CalculateDisplacement(...)     │
│ - CalculateStatistics(...)│  │ - CalculateDistortion(...)       │
│ + ClearCache()           │  │ - EstimateCp(cb): decimal        │
└──────────────────────────┘  │ - EstimateCm(cb, cp): decimal    │
                               └──────────────────────────────────┘
```

---

## Sequence Diagram (Happy Path)

```
User         Frontend      API GW      HullSizing    DataService   PostgreSQL
 │              │            │            │               │            │
 │ Select Mode  │            │            │               │            │
 │ "Data-Driven"│            │            │               │            │
 ├─────────────>│            │            │               │            │
 │              │            │            │               │            │
 │ Click        │            │            │               │            │
 │ "Generate"   │            │            │               │            │
 ├─────────────>│            │            │               │            │
 │              │ POST /runs │            │               │            │
 │              │ mode="data_│            │               │            │
 │              │ driven_real"           │               │            │
 │              ├───────────>│            │               │            │
 │              │            │ Forward    │               │            │
 │              │            ├───────────>│               │            │
 │              │            │            │ POST /catalog/│            │
 │              │            │            │ search-similar│            │
 │              │            │            ├──────────────>│            │
 │              │            │            │               │ SELECT *   │
 │              │            │            │               │ FROM vessels_real
 │              │            │            │               │ WHERE type=...
 │              │            │            │               ├───────────>│
 │              │            │            │               │ [600 vessels]
 │              │            │            │               │<───────────┤
 │              │            │            │  In-Memory    │            │
 │              │            │            │  KNN Calc     │            │
 │              │            │            │  (50ms)       │            │
 │              │            │            │               │            │
 │              │            │            │<[KCS, Emma,   │            │
 │              │            │            │  Madrid, MSC, │            │
 │              │            │            │  OOCL] + sim  │            │
 │              │            │            │               │            │
 │              │            │            │ Scaling × 5   │            │
 │              │            │            │ (100ms)       │            │
 │              │            │            │               │            │
 │              │            │            │ [3/5 valid]   │            │
 │              │            │            │               │            │
 │              │            │            │ Refine × 3    │            │
 │              │            │            │ (FP Solver)   │            │
 │              │            │            │ (500ms)       │            │
 │              │            │            │               │            │
 │              │            │            │ Rank & Attach │            │
 │              │            │            │ Provenance    │            │
 │              │            │            │               │            │
 │              │            │            │ INSERT INTO   │            │
 │              │            │            │ candidate_designs           │
 │              │            │            │ (with provenance)           │
 │              │            │            ├──────────────────────────>│
 │              │            │            │               │ [Saved]    │
 │              │            │<[SizingRunDto]            │            │
 │              │<[Result]   │            │               │            │
 │              │            │            │               │            │
 │ GET /runs/{id}/candidates │            │               │            │
 ├────────────>│            │            │               │            │
 │              ├───────────>│            │               │            │
 │              │            ├───────────>│               │            │
 │              │            │            │ SELECT * FROM │            │
 │              │            │            │ candidates    │            │
 │              │            │            │ WHERE run_id  │            │
 │              │            │            ├──────────────────────────>│
 │              │            │            │               │ [5 candidates]
 │              │            │            │               │ (with provenance)
 │              │            │<[Candidates with provenance]            │
 │              │<[Display]  │            │               │            │
 │              │            │            │               │            │
 │ [Provenance  │            │            │               │            │
 │  Panel       │            │            │               │            │
 │  Visible]    │            │            │               │            │
 │<─────────────┤            │            │               │            │
```

---

## Technology Stack Diagram

```
┌──────────────────────────────────────────────────────────────┐
│                         FRONTEND                              │
├──────────────────────────────────────────────────────────────┤
│ React 18 + TypeScript 5 + Vite 5                             │
│ MobX 6 (State Management)                                    │
│ TailwindCSS 3 (Styling)                                      │
│ Axios (HTTP Client)                                          │
│ Lucide React (Icons: Database, Sparkles, etc.)              │
└────────────────────────┬─────────────────────────────────────┘
                         │ HTTP/REST
                         ▼
┌──────────────────────────────────────────────────────────────┐
│                     API GATEWAY (.NET 8)                      │
├──────────────────────────────────────────────────────────────┤
│ ASP.NET Core 8                                               │
│ JWT Middleware (Cognito or Local)                           │
│ CORS Middleware                                              │
│ Claims Forwarding                                            │
└────────────────────────┬─────────────────────────────────────┘
                         │ HTTP (Polly resilience)
          ┌──────────────┴──────────────┐
          ▼                             ▼
┌─────────────────────────┐  ┌────────────────────────────────┐
│ DataService (.NET 8)    │  │ HullSizingService (.NET 8)     │
├─────────────────────────┤  ├────────────────────────────────┤
│ Controllers:            │  │ Controllers:                    │
│ - CatalogVesselsCtrl ←──┼──│ - SizingRunsCtrl               │
│   (NEW)                 │  │                                 │
│                         │  │ Services:                       │
│ Services:               │  │ - DataDrivenRealWorldSolver ←  │
│ - RealWorldKnnService ←─┼──│   (NEW)                        │
│ - VesselCatalogImporter │  │ - VesselScalingService (NEW)   │
│ - CatalogVesselSeeder   │  │ - FirstPrinciplesSolver        │
│                         │  │ - DisplacementClosureService   │
│ Caching:                │  │ - HoltropResistanceService     │
│ - MemoryCache (1hr)     │  │                                 │
│   └─ 600 vessels        │  │ Integration:                    │
│                         │  │ - DataServiceClient (HTTP)     │
└────────┬────────────────┘  └────────┬───────────────────────┘
         │                            │
         │ Entity Framework Core 8    │ Entity Framework Core 8
         ▼                            ▼
┌─────────────────────────────────────────────────────────────┐
│                   POSTGRESQL 16.4 (RDS)                      │
├─────────────────────────────────────────────────────────────┤
│ Schemas:                                                     │
│ - catalog_user: vessels_real (600 rows, 7 indexes)          │
│ - sizing: mission_cases, sizing_runs, candidate_designs     │
│   (enhanced with 4 provenance columns)                      │
└─────────────────────────────────────────────────────────────┘
```

---

## KNN Algorithm Flowchart

```
START
  │
  ├─> Load Catalog (cache or DB)
  │   ├─ Cache Hit? → Use cached (5ms)
  │   └─ Cache Miss → Load from DB (200ms), Cache for 1hr
  │
  ├─> Filter by Vessel Type
  │   Example: WHERE vessel_type = 'Container'
  │   Result: 63 containers (from 600 total)
  │
  ├─> Extract Target Features
  │   [Displacement, Speed, MaxBeam, MaxDraft]
  │   Example: [50000t, 12.5m/s, 35m, 12m]
  │
  ├─> Calculate Statistics (for normalization)
  │   Min/Max for each feature across filtered vessels
  │
  ├─> Normalize Target Features
  │   feature_norm = (feature - min) / (max - min)
  │   Result: [0.42, 0.67, 0.58, 0.71]
  │
  ├─> For Each Vessel in Filtered Set:
  │   │
  │   ├─> Extract Vessel Features
  │   │   [Displacement, Speed, Beam, Draft]
  │   │
  │   ├─> Normalize Vessel Features
  │   │
  │   ├─> Calculate Weighted Distance
  │   │   distance = sqrt(
  │   │     0.40 * (target[0] - vessel[0])² +  // Displacement
  │   │     0.30 * (target[1] - vessel[1])² +  // Speed
  │   │     0.15 * (target[2] - vessel[2])² +  // Beam
  │   │     0.15 * (target[3] - vessel[3])²    // Draft
  │   │   )
  │   │
  │   └─> Store (Vessel, Distance)
  │
  ├─> Sort by Distance (ascending)
  │   [KCS: 0.12, Emma: 0.18, Madrid: 0.23, MSC: 0.25, OOCL: 0.28]
  │
  ├─> Check if <3 matches
  │   ├─ YES → Fallback: search ALL types
  │   └─ NO → Continue
  │
  ├─> Take Top K
  │   K=5 → [KCS, Emma, Madrid, MSC, OOCL]
  │
  ├─> Calculate Similarity Score
  │   similarity = 1 - (distance / max_distance)
  │   KCS: 1 - 0.12/0.50 = 0.76 (76%)
  │
  └─> Return List<SimilarVessel>

END
```

---

## Scaling Algorithm Flowchart

```
START (Reference Vessel, Target Displacement, Constraints)
  │
  ├─> Calculate Scale Factor
  │   k = (Δ_target / Δ_reference)^(1/3)
  │   Example: (50000 / 52030)^(1/3) = 0.988
  │
  ├─> Scale Dimensions
  │   L' = L × k = 230 × 0.988 = 227.2m
  │   B' = B × k = 32.2 × 0.988 = 31.8m
  │   T' = T × k = 10.8 × 0.988 = 10.7m
  │   D' = D × k = 19.0 × 0.988 = 18.8m
  │
  ├─> Preserve Coefficients
  │   Cb' = Cb = 0.6505 (unchanged)
  │   Cp' = Cp = 0.66 (unchanged)
  │   Cm' = Cm = 0.9849 (unchanged)
  │
  ├─> Check Constraints
  │   │
  │   ├─ Max Beam Constraint?
  │   │  ├─ YES, Violated (B' > MaxBeam)
  │   │  │  ├─> Clamp: B' = MaxBeam
  │   │  │  ├─> Compensate: L' *= sqrt(1/beamReduction)
  │   │  │  └─> Compensate: T' *= sqrt(1/beamReduction)
  │   │  └─ NO → Continue
  │   │
  │   └─ Max Draft Constraint?
  │      ├─ YES, Violated (T' > MaxDraft)
  │      │  ├─> Clamp: T' = MaxDraft
  │      │  ├─> Compensate: L' *= sqrt(1/draftReduction)
  │      │  └─> Compensate: B' *= sqrt(1/draftReduction)
  │      └─ NO → Continue
  │
  ├─> Calculate Distortion
  │   distortion = weighted_average(
  │     |L' - L_ideal| / L_ideal * 0.3,
  │     |B' - B_ideal| / B_ideal * 0.4,
  │     |T' - T_ideal| / T_ideal * 0.3
  │   )
  │
  ├─> Validate
  │   ├─ distortion < 10%? → VALID
  │   ├─ displacement_error < 5%? → VALID
  │   └─ ELSE → INVALID
  │
  └─> Return ScaledCandidate
      {
        Lpp, Beam, Draft, Depth,
        Cb, Cp, Cm,
        IsValid: true/false,
        Distortion: 0.08 (8%),
        SimilarityScore: 0.76 (from KNN)
      }

END
```

---

## Deployment Architecture (AWS)

```
┌─────────────────────────────────────────────────────────────┐
│                      USERS (Browser)                         │
└────────────────────────┬────────────────────────────────────┘
                         │ HTTPS
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                    CloudFront CDN                            │
│                  (Static Frontend)                           │
└────────────────────────┬────────────────────────────────────┘
                         │ S3 Origin
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                     S3 Bucket                                │
│              (React Build Artifacts)                         │
└─────────────────────────────────────────────────────────────┘

                         │ API Calls
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                  Application Load Balancer                   │
└────────────────────────┬────────────────────────────────────┘
                         │
          ┌──────────────┴──────────────┐
          ▼                             ▼
┌──────────────────────┐    ┌─────────────────────────────────┐
│ App Runner:          │    │ App Runner:                     │
│ DataService          │    │ HullSizingService               │
│ (ECR Image)          │    │ (ECR Image)                     │
│ - KNN Search ←───────┼────│ - DataDrivenSolver              │
│ - Catalog Seeder     │    │ - Mode Routing                  │
│ - 600 vessels cached │    │                                 │
└──────────┬───────────┘    └──────────┬──────────────────────┘
           │                           │
           │ PostgreSQL Connection     │ PostgreSQL Connection
           └───────────┬───────────────┘
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                    RDS PostgreSQL 16.4                       │
│                    (Multi-AZ, Encrypted)                     │
├─────────────────────────────────────────────────────────────┤
│ catalog_user.vessels_real (600 rows, 150 KB)                │
│ sizing.candidate_designs (10K+ rows, 5 MB, +provenance)     │
└─────────────────────────────────────────────────────────────┘
```

---

**Architecture Documentation:** Complete  
**Schema Documentation:** Complete  
**Deployment Ready:** ✅ YES

