# Hull Sizing Module - Executive Overview

## Vision
Build a production-grade Mission→Hull Sizing module as a standalone microservice that transforms mission requirements into preliminary hull designs with advanced 3D visualization, first-principles solver, and seamless integration with existing hydrostatics.

## Success Criteria
- **Performance:** <300ms slider interactions, <2s full compute, ≥45 FPS 3D rendering
- **Accuracy:** Displacement closure within ±1%, Fn within target band
- **Reliability:** 99.9% uptime, circuit breaker on failures, cached fallbacks
- **User Experience:** Grouped dashboard (Design/Analysis/Validation), smooth workflow

## Timeline
**6-8 weeks** for production-ready MVP

## Key Decisions (Architectural)

### Service Boundaries
- **HullSizingService** (NEW) - Port 5004, Schema `sizing`
  - Mission cases, sizing runs, candidates
  - First-principles solver (displacement closure, Holtrop)
  - Geometry generation (Wigley, Series 60)
  
- **DataService** (EXISTING) - Port 5003, Schema `data`
  - Hydrostatics, Resistance, Catalog (water properties, propellers)
  - HullSizing calls this for water properties and vessel creation

- **IdentityService** (EXISTING) - Port 5001, Schema `identity`
  - User auth, JWT validation

- **ApiGateway** (EXISTING) - Port 5002
  - Routes `/api/v1/hull-sizing/*` to HullSizingService

### Communication Pattern
```
Frontend → ApiGateway → HullSizingService → DataService
                                    ↓
                              (Polly: retry, circuit breaker)
                              (Cache: water props 12h TTL)
                              (Claims: sub, tenantId, roles)
```

### Frontend Organization
Dashboard grouped by workflow phase:
- **Design Phase:** Hull Sizing (NEW), Catalog Browser
- **Analysis Phase:** Hydrostatics, Resistance & Powering
- **Validation Phase:** Benchmarks

### Database
Same PostgreSQL instance, separate schemas:
- `identity.*` - IdentityService
- `data.*` - DataService (includes `catalog_water_properties`)
- `sizing.*` - HullSizingService (NEW)

### Production Hardening (Day One)
1. **Resilience:** Polly policies (timeout 2s, retry 3x with jitter, circuit breaker 5/30s)
2. **Caching:** Water properties cached 12h, fallback to stale on DataService failure
3. **Tracing:** OpenTelemetry with traceparent across all services
4. **Security:** Claims forwarding (sub, tenantId, orgId, roles, scope), deny if tenant missing
5. **API Hygiene:** Versioned routes `/api/v1/`, ProblemDetails on errors, OpenAPI docs
6. **DB Hardening:** CHECK constraints, indexes on hot paths, numeric precision
7. **Secrets:** RDS creds + JWT keys in AWS Secrets Manager
8. **Performance:** 80k tris max, dynamic LOD, Web Worker for solver
9. **CI/CD:** GitHub Actions with Docker build, Trivy scan, format check
10. **Feature Flags:** Gate Phase 2 features (data-driven mode, DXF export)

## Algorithm Strategy
- **Start:** Standard Holtrop-Mennen + ITTC-57 (proven methods)
- **Reference:** SPS SName paper for modern improvements
- **Mark:** Areas for custom algorithm enhancement with `// TODO: CUSTOM_ALGO`
- **Plan:** Develop proprietary displacement closure + resistance refinements in Phase 3

## Phases Overview

### Phase 0: Foundation (Week 1)
- Create HullSizingService project
- Database schema with hardening
- Shared models/DTOs
- Docker + ApiGateway routing

### Phase 1: Resilient Communication (Week 1)
- Polly policies (retry, timeout, circuit breaker)
- Water properties caching (12h TTL)
- Claims forwarding (multi-tenancy)
- OpenTelemetry tracing

### Phase 2: Solver & Services (Week 2-3)
- First-principles solver orchestrator
- Displacement closure (Newton loop with locks)
- Holtrop resistance (ITTC-57 + wave)
- Stability screen (GM, BMt, KB)
- Hull family service

### Phase 3: Seed Data (Week 3)
- CSV importer (hull families, KPI weights, vessel catalog)
- ISO containers, water properties
- Run on startup (idempotent)

### Phase 4: API Endpoints (Week 3-4)
- MissionCasesController (CRUD, soft delete)
- SizingRunsController (POST /runs, GET candidates)
- CandidatesController (recompute, push-to-hydrostatics)

### Phase 5: Frontend Foundation (Week 4)
- Create hull-sizing routes
- MobX store (HullSizingStore)
- API client with typed DTOs
- Mission input form (wizard-style)

### Phase 6: 3D Visualization (Week 4-5)
- react-three-fiber setup
- Parametric hull generators (Wigley, Series 60)
- Overlays (waterplane, markers, wavelength grid)
- SlicePlanes (draggable stations/waterlines)
- CurvatureHeatmap (fairness checking)
- Performance: LOD, Web Worker

### Phase 7: 2D Views (Week 5)
- Plan view (top-down waterplane)
- Profile view (side elevation)
- Body plan (sections + SAC)
- Offsets grid (AG Grid, editable)

### Phase 8: Workspace & Interaction (Week 5-6)
- SizingWorkspace layout (input panel + visualization)
- SpeedShapeSlider (<300ms debounced)
- Locks panel (Keep Fn, L/B, B/T, etc.)
- KPIs panel (live metrics)
- Resistance curve chart

### Phase 9: Integration (Week 6)
- Push to Hydrostatics (synchronous, idempotent)
- Navigate to created vessel
- Toast notifications with links

### Phase 10: Testing (Week 6-7)
- Unit tests (solver, closure, Holtrop)
- Integration tests (API endpoints)
- E2E tests (Cypress: wizard → compute → push)
- Reference cases (Container 6000 TEU, Tanker 200k DWT)
- Performance benchmarks

### Phase 11: DevOps & Infrastructure (Week 7)
- GitHub Actions workflow (hull-sizing-ci.yml)
- Terraform: App Runner service + ECR
- Docker build + push
- Trivy security scan
- CloudWatch metrics

### Phase 12: Polish & Documentation (Week 7-8)
- Dashboard grouped cards
- Error handling, loading states
- Keyboard shortcuts
- User guide, API docs
- Demo video

## Key Files Created

### Backend
```
backend/HullSizingService/
├── Program.cs (Polly, OpenTelemetry, Serilog)
├── Data/SizingDbContext.cs
├── Controllers/
│   ├── MissionCasesController.cs
│   ├── SizingRunsController.cs
│   └── CandidatesController.cs
├── Services/
│   ├── Solvers/
│   │   ├── FirstPrinciplesSolver.cs
│   │   ├── DisplacementClosureService.cs
│   │   ├── HoltropResistanceService.cs
│   │   └── StabilityScreenService.cs
│   ├── Integration/
│   │   └── DataServiceClient.cs (Polly + cache)
│   └── SeedService.cs
└── Migrations/
```

### Frontend
```
frontend/src/
├── pages/hull-sizing/
│   ├── HullSizingLanding.tsx
│   ├── SizingWorkspace.tsx
│   └── CandidateComparison.tsx
├── components/hull-sizing/
│   ├── MissionInputForm.tsx
│   ├── CandidatesGrid.tsx
│   ├── 3d/
│   │   ├── HullMesh.tsx
│   │   ├── SlicePlanes.tsx
│   │   └── CurvatureHeatmap.tsx
│   ├── 2d/
│   │   ├── PlanView.tsx
│   │   ├── ProfileView.tsx
│   │   └── BodyPlanView.tsx
│   └── controls/
│       ├── SpeedShapeSlider.tsx
│       └── LocksPanel.tsx
├── stores/HullSizingStore.ts
└── api/hull-sizing-api.ts
```

### Infrastructure
```
docker-compose.yml (add hull-sizing-service)
.github/workflows/hull-sizing-ci.yml (NEW)
terraform/deploy/modules/app-runner/hull-sizing.tf (NEW)
```

## Definition of Done (MVP)

### Functional
- ✅ User creates mission case with cargo/speed/constraints
- ✅ Sizing run generates ≥3 ranked candidates
- ✅ Each candidate closes Δ within ±1%, Fn in band
- ✅ 3D hull renders with overlays at ≥45 FPS
- ✅ 2D views (Plan/Profile/Body) display correctly
- ✅ Slider adjusts hull with <300ms response
- ✅ Locks respected by solver
- ✅ Push to Hydrostatics creates vessel
- ✅ Navigate to hydrostatics workspace

### Technical
- ✅ Polly policies prevent cascading failures
- ✅ Water properties cached (12h TTL)
- ✅ OpenTelemetry traces span all services
- ✅ Claims forwarded (multi-tenancy works)
- ✅ Database CHECK constraints enforce validity
- ✅ All indexes created
- ✅ Unit tests pass (>80% coverage for solvers)
- ✅ Integration tests pass
- ✅ E2E tests pass (Cypress)
- ✅ Docker compose starts all services
- ✅ GitHub Actions builds + tests + scans
- ✅ Terraform deploys to AWS

### Performance
- ✅ Slider: <300ms (p95)
- ✅ Full compute: <2s (p95)
- ✅ 3D FPS: ≥45 (p50)
- ✅ Memory: <500MB per session

### UX
- ✅ Dashboard has grouped cards (Design/Analysis/Validation)
- ✅ Mission form validates input
- ✅ Candidates grid shows rankings
- ✅ Workspace responsive (desktop + tablet)
- ✅ Loading states, error handling
- ✅ Toast notifications with links

## Deferred to Phase 2 (Post-MVP)
- Data-driven mode (KNN/regression over vessel catalog)
- DXF export (2D sections)
- IGES/STEP/SAT export (3D surfaces)
- KCS/KVLCC2 parametric templates
- Planing mode (Savitsky)
- Advanced stability (intact stability curves)
- Multi-speed resistance curve
- Container TEU fit calculator (bay/row/tier)
- Canal constraint presets (1-click apply)
- Custom algorithm refinements (SPS SName paper improvements)

## Risk Mitigation

### Risk: Solver doesn't converge
- **Mitigation:** Max iterations (50), soft constraints, fallback to relaxed tolerance
- **Fallback:** Return "no solution" gracefully with partial results

### Risk: 3D rendering <45 FPS
- **Mitigation:** Dynamic LOD, 80k tris cap, Web Worker for solver
- **Fallback:** "Performance Mode" toggle (reduce mesh detail)

### Risk: DataService down
- **Mitigation:** Circuit breaker, cached water properties
- **Fallback:** Serve stale cached values, degrade gracefully

### Risk: Integration with hydrostatics fails
- **Mitigation:** Idempotency keys, retry with Polly, validate offsets
- **Fallback:** Manual export (download JSON, user uploads)

### Risk: Timeline slips
- **Mitigation:** Week 6-8 is buffer, MVP scope clearly defined
- **Fallback:** Defer 2D views and comparison if needed

## Success Metrics (Post-Launch)

### Usage (30 days)
- ≥50% of active users create ≥1 mission case
- ≥30% of candidates pushed to hydrostatics
- ≥40% of users return to hull sizing within 7 days

### Performance (Continuous)
- Slider response: p95 <300ms
- Full recompute: p95 <2s
- 3D FPS: p50 ≥45

### Quality
- Δ closure success: ≥95% of runs converge within ±1%
- Constraint violations: Flags fire correctly in ≥99% of test cases
- Push to Hydrostatics success: ≥98%

### Business
- Feature becomes demo differentiator
- User feedback: NPS ≥50
- Enables early-phase design consulting contracts

## Next Steps
1. Read `01-ARCHITECTURE.md` for service boundaries
2. Read `02-DATABASE-SCHEMA.md` for DDL
3. Read `03-BACKEND-PHASES.md` for implementation plan
4. Start Phase 0: Foundation
