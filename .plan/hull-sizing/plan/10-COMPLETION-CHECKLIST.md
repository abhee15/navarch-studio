# Completion Checklist & Progress Tracking

## How to Use This Checklist
- Mark `[ ]` as `[x]` when task is fully complete
- Add comments after each section: `<!-- Completed: 2024-11-05 by [name] -->`
- Update as you progress through phases

---

## Phase 0: Foundation (Week 1)

### Backend Structure
- [x] Create HullSizingService project (.csproj)
- [x] Create Program.cs with Serilog, OpenTelemetry, JWT
- [x] Create appsettings.json with all config sections
- [x] Create Dockerfile (multi-stage build)
- [x] Create SizingDbContext with sizing schema
- [ ] Build succeeds: `dotnet build backend/HullSizingService`
- [ ] Health endpoint responds: `curl http://localhost:5004/health`

### Models & DTOs (Shared)
- [ ] Create MissionCase.cs model
- [ ] Create SizingRun.cs model
- [ ] Create CandidateDesign.cs model
- [ ] Create HullFamilyPreset.cs model
- [ ] Create VesselCatalog.cs model
- [ ] Create KpiWeight.cs model
- [ ] Create PushOperation.cs model
- [ ] Create all DTO classes (10+ files)

### Database Migration
- [ ] Create initial migration: `dotnet ef migrations add InitialSizingSchema`
- [ ] Apply migration: `dotnet ef database update`
- [ ] Verify sizing schema exists in PostgreSQL
- [ ] Verify all 8 tables created
- [ ] Verify all CHECK constraints active
- [ ] Verify all indexes created

### Docker & Infrastructure
- [ ] Add hull-sizing-service to docker-compose.yml
- [ ] Update ApiGateway routing (Program.cs)
- [ ] Update ApiGateway appsettings.json (Services:HullSizingService)
- [ ] `docker-compose up` starts all 5 services
- [ ] HullSizingService accessible on port 5004
- [ ] Swagger UI accessible: http://localhost:5004/swagger

<!-- Phase 0 Completed: [DATE] by [NAME] -->

---

## Phase 1: Resilience & Communication (Week 1-2)

### Polly Policies
- [ ] Install Polly NuGet packages
- [ ] Create IDataServiceClient interface
- [ ] Implement DataServiceClient with HttpClient
- [ ] Configure timeout policy (2s)
- [ ] Configure retry policy (3 attempts, jitter 200-600ms)
- [ ] Configure circuit breaker (5 failures, 30s break)
- [ ] Register HttpClient with Polly in Program.cs

### Water Properties Caching
- [ ] Implement GetWaterPropertiesAsync with IMemoryCache
- [ ] Set cache TTL to 12 hours
- [ ] Implement fallback to stale cache on DataService failure
- [ ] Add cache hit/miss logging
- [ ] Test cache with DataService stopped (should serve stale)

### Claims Forwarding
- [ ] Create ClaimsForwardingMiddleware
- [ ] Extract claims from JWT (sub, tenantId, orgId, roles, scopes)
- [ ] Store in HttpContext.Items
- [ ] Deny requests if tenantId missing (403 Forbidden)
- [ ] Update DataServiceClient to forward claims (X-User-Id, X-Tenant-Id, etc.)
- [ ] Test multi-tenancy isolation (users can't see other tenant's data)

<!-- Phase 1 Completed: [DATE] by [NAME] -->

---

## Phase 2: Solver Implementation (Week 2-3)

### Displacement Closure Service
- [ ] Create IDisplacementClosureService interface
- [ ] Implement DisplacementClosureService
- [ ] Newton loop converges to ±1% for barge test case
- [ ] Newton loop converges to ±1% for KCS test case
- [ ] Newton loop converges to ±1% for KVLCC2 test case
- [ ] Locks respected (keep_fn keeps Fn constant)
- [ ] Locks respected (keep_l_over_b keeps L/B constant)
- [ ] Constraints enforced (max_beam clamps B)
- [ ] Constraints enforced (max_draft clamps T)
- [ ] Flags generated (draft_exceeded, beam_exceeded, etc.)
- [ ] Max 50 iterations enforced
- [ ] Performance: <50ms per candidate

### Holtrop Resistance Service
- [ ] Create IHoltropResistanceService interface
- [ ] Implement HoltropResistanceService
- [ ] ITTC-57 friction coefficient (Cf formula)
- [ ] Form factor (1+k₁) - simplified version
- [ ] Wave resistance (Rw) - simplified Holtrop polynomial
- [ ] Total resistance R = Rf + Rw
- [ ] EHP calculation (EHP = R · V)
- [ ] SHP calculation (with sea/service margins)
- [ ] Mark areas for custom algorithm: `// TODO: CUSTOM_ALGO`
- [ ] Reference SPS SName paper in comments
- [ ] Test against KCS reference (EHP within ±10%)
- [ ] Performance: <20ms per calculation

### Stability Screen Service
- [ ] Create IStabilityScreenService interface
- [ ] Implement StabilityScreenService
- [ ] Waterplane inertia (Iwp) calculation
- [ ] Transverse metacentric radius (BMt) calculation
- [ ] Vertical center of buoyancy (KB) estimation
- [ ] Vertical center of gravity (KG) estimation by type
- [ ] Transverse metacentric height (GMt) calculation
- [ ] Roll period estimate (T_roll)
- [ ] Flag low_gm if GMt < 1.0 m
- [ ] Test against barge analytical solution (GMt within ±5%)

### Froude Targeting Service
- [ ] Create IFroudeTargetingService interface
- [ ] Implement FroudeTargetingService
- [ ] Pick Fn from family preset band
- [ ] Adjust Fn based on speed (higher speed → upper band)
- [ ] Clamp to family min/max

### Hull Family Service
- [ ] Create IHullFamilyService interface
- [ ] Implement HullFamilyService
- [ ] Get applicable families for mission type
- [ ] Filter by Fn range compatibility
- [ ] Filter by constraints (draft, beam)
- [ ] Return top 5 families by priority

### First-Principles Solver (Orchestrator)
- [ ] Create IFirstPrinciplesSolver interface
- [ ] Implement FirstPrinciplesSolver orchestrator
- [ ] Payload conversion (volume/weight/TEU → mass)
- [ ] Total displacement estimation (DWT/Δ ratios)
- [ ] Multi-family iteration (parallel with Task.WhenAll)
- [ ] Call displacement closure for each family
- [ ] Call water properties (cached)
- [ ] Call stability screen
- [ ] Call Holtrop resistance
- [ ] Generate hull geometry (Wigley for MVP)
- [ ] Compute scores (weighted KPIs)
- [ ] Rank candidates by score
- [ ] Performance: <2s for 5 candidates

<!-- Phase 2 Completed: [DATE] by [NAME] -->

---

## Phase 3: Seed Data (Week 3)

### CSV Import Service
- [ ] Create ISeedService interface
- [ ] Implement SeedService
- [ ] SeedHullFamiliesAsync (from hull_family_presets_extended.csv)
- [ ] SeedVesselCatalogAsync (from vessel_catalog_seed.csv)
- [ ] SeedKpiWeightsAsync (from kpi_weights.csv)
- [ ] SeedIsoContainersAsync (from iso_containers.csv)
- [ ] Idempotent checks (skip if table not empty)
- [ ] Copy CSV files to Data/Seeds/csv/
- [ ] Register seeder in Program.cs
- [ ] Run seeder on startup (after migrations)
- [ ] Verify 13 hull families imported
- [ ] Verify 3-4 vessel catalog entries (KCS, KVLCC2, Series 60)
- [ ] Verify 5 KPI weights (system defaults)
- [ ] Verify 4 ISO container types

<!-- Phase 3 Completed: [DATE] by [NAME] -->

---

## Phase 4: API Endpoints (Week 3-4)

### Mission Cases Controller
- [ ] Create MissionCasesController
- [ ] POST /mission-cases - create
- [ ] GET /mission-cases - list (paginated)
- [ ] GET /mission-cases/{id} - get by ID
- [ ] PUT /mission-cases/{id} - update
- [ ] DELETE /mission-cases/{id} - soft delete
- [ ] Extract userId and tenantId from HttpContext.Items
- [ ] Deny if tenantId missing (403)
- [ ] Return ProblemDetails on validation errors
- [ ] XML comments for Swagger
- [ ] Test with Postman/curl

### Sizing Runs Controller
- [ ] Create SizingRunsController
- [ ] POST /mission-cases/{id}/runs - create run
- [ ] GET /runs/{runId} - get run details
- [ ] GET /runs/{runId}/candidates - list candidates
- [ ] Log compute time in X-Compute-Time-Ms header
- [ ] Handle solver errors gracefully (422 Unprocessable Entity)
- [ ] XML comments for Swagger
- [ ] Test end-to-end (POST mission → POST run → GET candidates)

### Candidates Controller
- [ ] Create CandidatesController
- [ ] GET /candidates/{id} - get candidate
- [ ] POST /candidates/{id}/recompute - recompute with adjustments
- [ ] POST /candidates/{id}/push-to-hydrostatics - create vessel
- [ ] Require X-Idempotency-Key header for push
- [ ] Check idempotency (return existing vessel ID if duplicate key)
- [ ] Return Location header with vessel URL
- [ ] XML comments for Swagger
- [ ] Test push-to-hydrostatics with mocked DataService

### Reference Data Controller
- [ ] Create ReferenceController
- [ ] GET /reference/hull-families - list families
- [ ] GET /reference/iso-containers - list containers
- [ ] GET /reference/kpi-weights - get scoring weights
- [ ] Cache responses (5 min TTL)

<!-- Phase 4 Completed: [DATE] by [NAME] -->

---

## Phase 5: Frontend Foundation (Week 4)

### Routing & Store
- [ ] Add hull-sizing routes to App.tsx
- [ ] Create HullSizingStore.ts (MobX)
- [ ] Create hull-sizing-api.ts (API client)
- [ ] Create types/hull-sizing.ts (TypeScript interfaces)
- [ ] Register HullSizingStore in RootStore
- [ ] Test store with mock API

### Mission Input Form
- [ ] Create MissionInputForm.tsx
- [ ] Mission type dropdown (Commercial/Government/Pleasure)
- [ ] Mission sub-type cards (Container, Tanker, etc.)
- [ ] Cargo basis radio buttons (Volume/Weight/TEU)
- [ ] Conditional inputs based on cargo basis
- [ ] Speed & environment inputs
- [ ] Constraints inputs (max LOA, beam, draft)
- [ ] Locks checkboxes (Keep Fn, L/B, etc.)
- [ ] Form validation (required fields)
- [ ] "Compute" button calls API
- [ ] Navigate to results on success

### Candidates Grid
- [ ] Create CandidatesGrid.tsx
- [ ] Create CandidateCard.tsx
- [ ] Display 3D thumbnail (static render)
- [ ] Display key metrics (Lpp, B, T, Fn, Δ, EHP)
- [ ] Display score gauge (circular progress)
- [ ] Display flags (constraint violations)
- [ ] Sort/filter controls (by score, family)
- [ ] Click card → navigate to workspace
- [ ] Responsive grid (3 cols desktop, 1 col mobile)

<!-- Phase 5 Completed: [DATE] by [NAME] -->

---

## Phase 6: 3D Visualization (Week 4-5)

### Setup
- [ ] Install dependencies: `@react-three/fiber`, `@react-three/drei`, `three`
- [ ] Install gl-matrix for matrix operations

### Core 3D Components
- [ ] Create HullViewer3D.tsx (Canvas wrapper)
- [ ] Create HullMesh.tsx (parametric hull geometry)
- [ ] Create WaterplaneOverlay.tsx (plane at draft T)
- [ ] Create MarkerPoints.tsx (CB, LCB, LCG, KB spheres)
- [ ] Create ConstraintBoxes.tsx (max beam/draft/LOA wireframes)
- [ ] Create WavelengthGrid.tsx (λ overlay from Tz)
- [ ] Create SlicePlanes.tsx (draggable stations/waterlines/buttocks)
- [ ] Create CurvatureHeatmap.tsx (fairness checking)
- [ ] Create CompareOverlay.tsx (ghost reference hull)

### Performance
- [ ] Implement LOD system (near/mid/far)
- [ ] Add FPS monitoring (Stats.js from drei)
- [ ] Verify ≥45 FPS on mid-range laptop
- [ ] useMemo on all geometry generation
- [ ] Dispose geometries on unmount
- [ ] Throttle re-renders during slider drag

### Interactions
- [ ] OrbitControls (rotate, zoom, pan)
- [ ] Camera presets (Isometric, Port, Starboard, Bow, Stern, Top)
- [ ] Toggle overlays (waterplane, markers, slices, heatmap)
- [ ] Screenshot export (PNG)

<!-- Phase 6 Completed: [DATE] by [NAME] -->

---

## Phase 7: 2D Views (Week 5)

### SVG Views
- [ ] Create PlanView.tsx (top-down waterplane)
- [ ] Create ProfileView.tsx (side elevation)
- [ ] Create BodyPlanView.tsx (sections + SAC)
- [ ] Create OffsetsGrid.tsx (AG Grid, editable)

### 2D Features
- [ ] Dimensions labeled (Lpp, LWL, LOA, B, T, D)
- [ ] Station lines on plan view
- [ ] Waterlines on profile view
- [ ] LCB/LCG markers on plan
- [ ] KB marker on profile
- [ ] Freeboard indication on profile
- [ ] SAC (Sectional Area Curve) on body plan
- [ ] Responsive SVG (scales with container)
- [ ] Export as PNG

<!-- Phase 7 Completed: [DATE] by [NAME] -->

---

## Phase 8: Workspace & Interaction (Week 5-6)

### Workspace Layout
- [ ] Create SizingWorkspace.tsx (two-panel layout)
- [ ] Left panel: Mission summary, locks, sliders, KPIs
- [ ] Right panel: Tabs (3D, Plan, Profile, Body, Offsets)
- [ ] Responsive layout (desktop + tablet)

### Interactive Controls
- [ ] Create SpeedShapeSlider.tsx
- [ ] Debouncing (300ms)
- [ ] Slider updates candidate (<300ms target)
- [ ] Create LocksPanel.tsx (checkboxes)
- [ ] Locks toggle and affect recompute
- [ ] Create KPISummaryPanel.tsx (live metrics)
- [ ] Create ResistanceCurveChart.tsx (EHP vs V)

### Integration
- [ ] Create PushToHydroDialog.tsx
- [ ] "Push to Hydrostatics" button opens dialog
- [ ] Dialog shows vessel name input
- [ ] Dialog shows candidate dimensions
- [ ] Push creates vessel in DataService
- [ ] Toast notification shows success
- [ ] "Open Vessel" link navigates to hydrostatics workspace

<!-- Phase 8 Completed: [DATE] by [NAME] -->

---

## Phase 9: Testing (Week 6-7)

### Backend Unit Tests
- [ ] Create HullSizingService.Tests project
- [ ] DisplacementClosureServiceTests (convergence, locks, constraints)
- [ ] HoltropResistanceServiceTests (KCS reference, ITTC-57)
- [ ] StabilityScreenServiceTests (barge analytical)
- [ ] FirstPrinciplesSolverTests (full workflow)
- [ ] MissionCasesControllerTests (CRUD, tenant isolation)
- [ ] SizingRunsControllerTests (create run)
- [ ] CandidatesControllerTests (recompute, push)
- [ ] >80% coverage for solver services
- [ ] All tests pass

### Frontend Tests
- [ ] MissionInputForm.test.tsx (validation, cargo basis toggle)
- [ ] CandidateCard.test.tsx (metrics display, flags)
- [ ] HullSizingStore.test.ts (API calls, state management)
- [ ] All tests pass

### E2E Tests (Cypress)
- [ ] hull-sizing-wizard.spec.ts (full wizard flow)
- [ ] hull-sizing-slider.spec.ts (slider <300ms)
- [ ] hull-sizing-push.spec.ts (push to hydrostatics)
- [ ] All E2E tests pass

### Reference Test Cases
- [ ] FP_Container_Base (58,000t, 24kn) - passes
- [ ] FP_Tanker_Base (300,000t, 16kn) - passes
- [ ] FP_Fishing_Base (1,000t, 12kn) - passes

<!-- Phase 9 Completed: [DATE] by [NAME] -->

---

## Phase 10: DevOps & Infrastructure (Week 7)

### GitHub Actions
- [ ] Create .github/workflows/hull-sizing-ci.yml
- [ ] Build job (restore, build, test, format check)
- [ ] Trivy security scan
- [ ] Docker build job (ECR push)
- [ ] Cache NuGet packages (~/.nuget)
- [ ] Workflow triggers on PR and push to main/develop
- [ ] All CI checks pass

### Terraform
- [ ] Create terraform/setup/ecr.tf (hull-sizing repository)
- [ ] Create terraform/deploy/modules/app-runner/hull-sizing.tf
- [ ] Configure App Runner (CPU: 1024, Memory: 2048)
- [ ] Configure health check (/health endpoint)
- [ ] Configure VPC connector (for RDS access)
- [ ] Add secrets (DB credentials, JWT secret)
- [ ] Add CloudWatch log group
- [ ] Add CloudWatch alarms (error rate, latency)
- [ ] `terraform plan` succeeds
- [ ] `terraform apply` deploys successfully

### AWS Resources Created
- [ ] ECR repository: navarch-hull-sizing
- [ ] App Runner service: navarch-hull-sizing-production
- [ ] CloudWatch log group: /aws/apprunner/navarch-hull-sizing-production
- [ ] CloudWatch alarms (2): error rate, latency
- [ ] Service accessible via API Gateway

<!-- Phase 10 Completed: [DATE] by [NAME] -->

---

## Phase 11: Dashboard & UX (Week 7)

### Dashboard Updates
- [ ] Update DashboardPage.tsx with grouped layout
- [ ] Add "Design Phase" section
- [ ] Add Hull Sizing card (purple icon)
- [ ] Move Catalog to Design Phase
- [ ] Add "Analysis Phase" section (Hydrostatics, Resistance)
- [ ] Add "Validation Phase" section (Benchmarks)
- [ ] All cards navigate correctly

### UX Polish
- [ ] Loading states (spinners during compute)
- [ ] Error handling (friendly messages)
- [ ] Toast notifications (success/error)
- [ ] Tooltips on technical terms (Fn, Cb, Cp, etc.)
- [ ] Keyboard shortcuts (V for view toggle, R for reset camera)
- [ ] Dark mode support (consistent with existing app)
- [ ] Responsive design (desktop + tablet + mobile)

<!-- Phase 11 Completed: [DATE] by [NAME] -->

---

## Phase 12: Documentation (Week 7-8)

### Technical Documentation
- [x] 00-OVERVIEW.md (executive summary)
- [x] 01-ARCHITECTURE.md (service boundaries, communication)
- [x] 02-DATABASE-SCHEMA.md (detailed DDL)
- [x] 03-BACKEND-PHASES.md (implementation phases)
- [x] 04-FRONTEND-PHASES.md (UI implementation)
- [x] 05-SOLVER-ALGORITHM.md (math formulation, custom algo notes)
- [x] 06-API-SPECIFICATION.md (all endpoints with examples)
- [x] 07-TESTING-STRATEGY.md (unit, integration, E2E)
- [x] 08-DEVOPS-CICD.md (Docker, GitHub Actions, Terraform)
- [x] 09-PERFORMANCE-TARGETS.md (optimization strategies)
- [x] 10-COMPLETION-CHECKLIST.md (this file)

### User Documentation
- [ ] USER-GUIDE.md (how to use hull sizing wizard)
- [ ] SOLVER-EXPLAINED.md (non-technical explanation of algorithm)
- [ ] INTEGRATION-GUIDE.md (how to push to hydrostatics/resistance)

### API Documentation
- [ ] Swagger UI published
- [ ] OpenAPI JSON downloadable
- [ ] Generate TypeScript client from OpenAPI
- [ ] API examples in documentation

### Demo & Training
- [ ] Record demo video (3-5 min walkthrough)
- [ ] Create sample mission cases (container, tanker, fishing)
- [ ] Internal training session (team walkthrough)

<!-- Phase 12 Completed: [DATE] by [NAME] -->

---

## MVP Definition of Done

### Functional Requirements
- [x] Plan documents created (10 files)
- [ ] User can create mission case with cargo/speed/constraints
- [ ] Sizing run generates ≥3 ranked candidates
- [ ] Each candidate closes Δ within ±1%
- [ ] Fn stays within family target band
- [ ] 3D hull renders with overlays
- [ ] 2D views (Plan, Profile, Body) display correctly
- [ ] Slider adjusts hull with <300ms response
- [ ] Locks respected by solver (keep Fn, L/B, B/T, etc.)
- [ ] Constraints enforced (max beam, draft, LOA)
- [ ] Flags displayed (draft_exceeded, low_gm, etc.)
- [ ] Push to Hydrostatics creates vessel
- [ ] Navigate to hydrostatics workspace works
- [ ] Dashboard has grouped cards (Design/Analysis/Validation)

### Technical Requirements
- [ ] HullSizingService builds without errors
- [ ] All unit tests pass (>80% coverage for solvers)
- [ ] All integration tests pass
- [ ] All E2E tests pass (Cypress)
- [ ] docker-compose up starts all 5 services
- [ ] Migrations apply automatically in production
- [ ] Seed data imports on first run
- [ ] Polly policies prevent cascading failures
- [ ] Water properties cached (12h TTL)
- [ ] OpenTelemetry traces span all services
- [ ] Claims forwarded (multi-tenancy works)
- [ ] Idempotency prevents duplicate vessel creation

### Performance Requirements
- [ ] Slider interaction: p95 <300ms
- [ ] Full sizing run: p95 <2s
- [ ] 3D rendering: p50 ≥45 FPS
- [ ] Memory usage: <500 MB per session
- [ ] Displacement closure: <50ms per candidate
- [ ] Holtrop calculation: <20ms per candidate

### DevOps Requirements
- [ ] GitHub Actions CI workflow passes
- [ ] Docker build succeeds
- [ ] Trivy security scan clean
- [ ] Terraform plan succeeds
- [ ] Terraform apply deploys to AWS
- [ ] App Runner service healthy
- [ ] CloudWatch logs appearing
- [ ] CloudWatch metrics tracked
- [ ] OpenTelemetry traces in X-Ray
- [ ] Secrets injected from Secrets Manager

### Documentation Requirements
- [ ] All 10 plan documents complete
- [ ] User guide written
- [ ] API documented in Swagger
- [ ] Demo video recorded
- [ ] README updated with hull sizing feature

---

## Post-MVP (Phase 2+)

### Deferred Features
- [ ] Data-driven mode (KNN/regression over vessel catalog)
- [ ] DXF export (2D sections)
- [ ] IGES export (3D NURBS surfaces)
- [ ] STEP/SAT export (CAD formats)
- [ ] KCS/KVLCC2 parametric templates
- [ ] Planing mode (Savitsky method)
- [ ] Series 60 hull generator
- [ ] Advanced stability (intact curves)
- [ ] Multi-speed resistance curve
- [ ] Container TEU fit calculator (bay/row/tier)
- [ ] Canal constraint presets (1-click Panamax/Suezmax)
- [ ] Custom algorithm refinements (SPS SName improvements)
- [ ] User-uploadable vessel catalog
- [ ] Pareto multi-objective optimization
- [ ] Comparison view (side-by-side 3 candidates)

---

## Progress Summary

**Phase 0:** ⬜ Not Started | ⏳ In Progress | ✅ Complete  
**Phase 1:** ⬜ Not Started  
**Phase 2:** ⬜ Not Started  
**Phase 3:** ⬜ Not Started  
**Phase 4:** ⬜ Not Started  
**Phase 5:** ⬜ Not Started  
**Phase 6:** ⬜ Not Started  
**Phase 7:** ⬜ Not Started  
**Phase 8:** ⬜ Not Started  
**Phase 9:** ⬜ Not Started  
**Phase 10:** ⬜ Not Started  
**Phase 11:** ⬜ Not Started  
**Phase 12:** ⬜ Not Started (Docs: ✅ 10/13 files)

**Overall Progress:** 10% (Plan complete, implementation pending)

---

## File Locations

All plan documents are located at:
```
.plan/hull-sizing/plan/
├── 00-OVERVIEW.md
├── 01-ARCHITECTURE.md
├── 02-DATABASE-SCHEMA.md
├── 03-BACKEND-PHASES.md
├── 04-FRONTEND-PHASES.md
├── 05-SOLVER-ALGORITHM.md
├── 06-API-SPECIFICATION.md
├── 07-TESTING-STRATEGY.md
├── 08-DEVOPS-CICD.md
├── 09-PERFORMANCE-TARGETS.md
└── 10-COMPLETION-CHECKLIST.md (this file)
```

**To view files:**
```bash
# List all plan files
ls -la .plan/hull-sizing/plan/

# Read overview
cat .plan/hull-sizing/plan/00-OVERVIEW.md
```

---

## Next Steps
1. Review all 10 plan documents
2. Ask clarifying questions if needed
3. Begin implementation starting with Phase 0
4. Update this checklist as you complete tasks
5. Reference specific plan documents for detailed guidance
