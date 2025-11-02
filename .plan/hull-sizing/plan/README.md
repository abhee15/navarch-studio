# Hull Sizing Module - Complete Implementation Plan

## 📚 Plan Documents

This folder contains the comprehensive implementation plan for the Mission→Hull Sizing module, merging the best ideas from 4 independent analyses and incorporating production-grade hardening requirements.

### Document Index

1. **[00-OVERVIEW.md](./00-OVERVIEW.md)** - Executive Summary
   - Vision and success criteria
   - Architecture decisions
   - Timeline (6-8 weeks)
   - Key technologies
   - Risk mitigation

2. **[01-ARCHITECTURE.md](./01-ARCHITECTURE.md)** - Service Architecture
   - Service topology diagram
   - HullSizingService details (port 5004, schema `sizing`)
   - Service-to-service communication (Polly resilience)
   - Database schema ownership
   - Security (multi-tenancy, JWT)
   - OpenTelemetry tracing

3. **[02-DATABASE-SCHEMA.md](./02-DATABASE-SCHEMA.md)** - Database Design
   - Complete DDL for all 8 tables
   - CHECK constraints, indexes, foreign keys
   - EF Core configuration
   - Migration strategy
   - Soft delete pattern

4. **[03-BACKEND-PHASES.md](./03-BACKEND-PHASES.md)** - Backend Implementation
   - Phase 0: Foundation (project structure)
   - Phase 1: Models & DTOs
   - Phase 2: Database migration
   - Phase 3: Seed data import
   - Phase 4: Resilient HTTP client
   - Phase 5: Claims & multi-tenancy
   - Phase 6: First-principles solver
   - Phase 7: Services layer
   - Phase 8: Controllers & API
   - Phase 9: Testing

5. **[04-FRONTEND-PHASES.md](./04-FRONTEND-PHASES.md)** - Frontend Implementation
   - Phase 5: Foundation (routing, store, API client)
   - Phase 6: Mission input form
   - Phase 7: Candidates grid
   - Phase 8: 3D visualization (react-three-fiber)
   - Phase 9: 2D views (SVG)
   - Phase 10: Workspace & interaction
   - Phase 11: Integration & handoff
   - Phase 12: Dashboard integration

6. **[05-SOLVER-ALGORITHM.md](./05-SOLVER-ALGORITHM.md)** - Mathematical Formulation
   - Algorithm flow diagram
   - Payload conversion (volume/weight/TEU)
   - Displacement estimation (DWT/Δ ratios)
   - Froude number targeting
   - Displacement closure (Newton-Raphson)
   - Stability screen (quick GM)
   - Holtrop-Mennen resistance (ITTC-57 + wave)
   - Geometry generation (Wigley, Series 60)
   - Multi-objective scoring
   - **Custom algorithm development notes** (marked with TODO: CUSTOM_ALGO)
   - **SPS SName paper reference** for Phase 3 improvements

7. **[06-API-SPECIFICATION.md](./06-API-SPECIFICATION.md)** - API Reference
   - All endpoint signatures
   - Request/response examples
   - Error responses (ProblemDetails)
   - Rate limiting (100 req/min)
   - OpenAPI/Swagger
   - Example workflows (curl commands)

8. **[07-TESTING-STRATEGY.md](./07-TESTING-STRATEGY.md)** - QA Plan
   - Unit tests (backend: >80% coverage for solvers)
   - Integration tests (E2E workflows)
   - Frontend tests (Jest, React Testing Library)
   - E2E tests (Cypress)
   - Performance tests (benchmarks)
   - Reference test cases (Container, Tanker, Fishing)
   - CI/CD integration

9. **[08-DEVOPS-CICD.md](./08-DEVOPS-CICD.md)** - Infrastructure
   - Docker configuration (docker-compose.yml)
   - GitHub Actions workflows (CI + deploy)
   - Terraform (ECR + App Runner + CloudWatch)
   - Secrets management (AWS Secrets Manager)
   - Monitoring & observability (metrics, alarms, traces)
   - Deployment checklist
   - Rollback strategy

10. **[09-PERFORMANCE-TARGETS.md](./09-PERFORMANCE-TARGETS.md)** - Optimization
    - Performance SLAs (backend: <2s, frontend: <300ms, 3D: ≥45 FPS)
    - Optimization strategies (parallel generation, caching, LOD, debouncing)
    - Profiling tools (dotTrace, BenchmarkDotNet, React DevTools)
    - Load testing (k6)
    - Memory management

11. **[10-COMPLETION-CHECKLIST.md](./10-COMPLETION-CHECKLIST.md)** - Progress Tracking
    - Checkboxes for every task across all phases
    - Phase completion comments
    - Reference test cases tracking
    - MVP Definition of Done
    - Post-MVP deferred features
    - Progress summary

---

## 🎯 Quick Start Guide

### For Developers Starting Implementation

1. **Read in Order:**
   - Start with `00-OVERVIEW.md` (15 min read)
   - Skim `01-ARCHITECTURE.md` for service boundaries (10 min)
   - Review `02-DATABASE-SCHEMA.md` for data model (15 min)
   - Follow either:
     - **Backend:** `03-BACKEND-PHASES.md` → `05-SOLVER-ALGORITHM.md` → `07-TESTING-STRATEGY.md`
     - **Frontend:** `04-FRONTEND-PHASES.md` → `09-PERFORMANCE-TARGETS.md`

2. **Reference While Coding:**
   - `06-API-SPECIFICATION.md` - When implementing endpoints
   - `05-SOLVER-ALGORITHM.md` - When implementing solver
   - `10-COMPLETION-CHECKLIST.md` - Track your progress

3. **Before Deploying:**
   - `08-DEVOPS-CICD.md` - Infrastructure setup
   - `10-COMPLETION-CHECKLIST.md` - Verify all tasks done

---

## 🔑 Key Decisions Summary

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Service Structure | New HullSizingService microservice | Clean boundaries, independent scaling |
| Port | 5004 | Next available after DataService (5003) |
| Database | Same PostgreSQL, separate `sizing` schema | Isolated data, shared infrastructure |
| Catalog Location | Stay in DataService | Already has water properties, small dataset |
| Communication | Direct HTTP with Polly resilience | Matches existing pattern, proven reliability |
| Frontend Routing | Grouped by design phase | Natural workflow (Design → Analysis → Validation) |
| 3D Library | react-three-fiber + Three.js | Industry standard, performant, flexible |
| Solver Approach | First-principles (MVP), data-driven (Phase 2) | Proven methods first, ML later |
| Resistance Method | Holtrop-Mennen (simplified for MVP) | Standard for preliminary design |
| Custom Algorithm | Mark with TODO, develop in Phase 3 | Ship proven methods first, innovate later |
| CAD Export | Phase 2 (DXF), Phase 3 (IGES/STEP) | Focus on visualization in MVP |
| Timeline | 6-8 weeks | Realistic for production-grade quality |

---

## 📊 Plan Comparison Summary

This plan synthesizes ideas from 4 independent agent analyses:

### Agent 1 Contributions
- Service decomposition (DisplacementClosureService, FroudeTargetingService, StabilityScreenService)
- Database fields (deleted_at, mission_category, cargo separation, is_selected)
- Frontend route structure (/cases, /workspace/:runId)

### Agent 2 Contributions
- Scientific completeness (Cm coefficient, salinity, service_margin_pct)
- IHullGenerator interface detail
- Freeze compute performance hack
- Side-by-side comparison modal
- Realistic timeline (6-8 weeks)

### Agent 3 Contributions
- RESTful API design (/reference/* namespace, POST /mission-cases/{id}/runs)
- Parallel candidate generation
- Database index specifics
- Lazy loading, virtualization
- Manual E2E test checklist
- Hour-based phase estimates
- Commercial success metrics

### My Plan (Agent 4) Contributions
- Advanced 3D features (SlicePlanes, CurvatureHeatmap)
- Detailed 2D views (Plan/Profile/Body separate)
- OffsetsGrid editor
- CloudWatch telemetry
- Web Worker with Comlink
- Golden file testing (CAD round-trip)
- Definition of Done (20+ checkboxes)

### Your Production Hardening Requirements
- Polly resilience (timeout 2s, retry 3x jitter, circuit breaker 5/30s)
- Water properties caching (12h TTL, stale fallback)
- Claims forwarding (sub, tenantId, orgId, roles, scopes)
- OpenTelemetry tracing (traceparent across services)
- Idempotency keys (push-to-hydrostatics)
- Feature flags (data-driven mode, DXF export)
- Security hardening (Secrets Manager, tenant denial)
- CI/CD polish (Trivy scan, format check, cache ~/.nuget)

---

## 🚀 Ready to Implement

All plan documents are complete. You can now:

1. **Start implementation** following Phase 0 in `03-BACKEND-PHASES.md`
2. **Track progress** using `10-COMPLETION-CHECKLIST.md`
3. **Reference algorithms** from `05-SOLVER-ALGORITHM.md`
4. **Test against criteria** from `07-TESTING-STRATEGY.md`
5. **Deploy using** `08-DEVOPS-CICD.md`

**Questions?** Review the specific plan document or ask for clarification.

**Let's build this! 🛳️**
