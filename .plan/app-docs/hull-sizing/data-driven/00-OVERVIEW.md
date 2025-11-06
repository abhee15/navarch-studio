# Data-Driven Hull Sizing - Executive Overview

**Last Updated:** November 6, 2025  
**Status:** Planning Phase  
**Timeline:** 4 weeks (2 phases)

---

## Vision

Implement **dual-catalog Data-Driven mode** for Hull Sizing, combining:
1. **Real-World Catalog** (600 proven vessels) - Mission-based matching
2. **ML/Parametric Catalog** (82,000+ synthetic hulls) - Geometric exploration

This provides two complementary workflows beyond the existing First-Principles physics-based solver.

---

## The Three Workflows

### Workflow 1: First-Principles Mode ✅ (EXISTING)
- **Status:** Complete and deployed
- **Approach:** Pure physics (displacement closure, Holtrop resistance)
- **Best for:** Novel designs, no reference data needed
- **Speed:** ~2s per run

### Workflow 2: Data-Driven Real-World Mode 🎯 (PHASE 1 - Weeks 1-2)
- **Status:** To be implemented
- **Approach:** KNN search on 600 real vessels + scaling + refinement
- **Best for:** Standard vessels (containers, tankers, ferries, etc.)
- **Speed:** <1s per run (faster than physics)
- **Data:** 600 curated real vessels (SIMMAN, ITTC, ship registries)

### Workflow 3: Data-Driven ML/Parametric Mode 🚀 (PHASE 2 - Weeks 3-4)
- **Status:** To be implemented
- **Approach:** KNN search on 82K parametric hulls + geometry conversion
- **Best for:** Design space exploration, geometric similarity
- **Speed:** <1s per run
- **Data:** MIT ShipD dataset (82,168 parametric hull forms)

---

## Key Capabilities

### Phase 1 (Real-World Catalog)
- ✅ Import 600 proven vessel designs
- ✅ Mission-based KNN search (vessel type, payload, speed)
- ✅ Intelligent scaling (preserve ratios, respect constraints)
- ✅ Hybrid refinement (KNN seed + first-principles closure)
- ✅ Provenance tracking (show which vessels influenced design)
- ✅ UI toggle: First-Principles vs Data-Driven (Real)

### Phase 2 (ML/Parametric Catalog)
- ✅ Import 82K parametric hull forms
- ✅ Geometry-based KNN search (dimensions, form, shape)
- ✅ Parametric-to-principal conversion (45 params → Lpp, B, T, Cb)
- ✅ UI catalog toggle: Real-World vs ML/Parametric
- ✅ Design space exploration features
- ✅ STL/geometry generation capability

---

## Success Criteria

### Phase 1 (Real-World)
- **Functional:** KNN returns relevant vessels, scaling produces valid hulls
- **Accuracy:** Displacement within ±1-2% after refinement
- **Performance:** <1s end-to-end (vs ~2s for First-Principles)
- **Coverage:** All vessel types represented (Container, Tanker, Bulk, Ferry, etc.)
- **UX:** Clear mode selection, provenance displayed, graceful fallback
- **Quality:** >80% test coverage, all validation tests pass

### Phase 2 (ML/Parametric)
- **Functional:** Search 82K hulls by geometry, convert to principal dimensions
- **Accuracy:** Geometric measures match source data within 1%
- **Performance:** <1s including conversion
- **UX:** Catalog toggle (Real vs ML), design exploration UI
- **Quality:** >70% test coverage on new components

---

## Timeline

**Phase 1: Real-World Data-Driven (Weeks 1-2)**
- Days 1-2: Foundation + Quick Wins
- Days 3-4: Database + Import (600 vessels)
- Days 5-6: KNN + Scaling algorithms
- Days 7-8: Hybrid Solver + API
- Days 9-10: Frontend + Testing + Docs

**Phase 2: ML/Parametric Enhancement (Weeks 3-4)**
- Days 11-12: ShipD import + Conversion
- Days 13-14: Parametric KNN + Integration
- Days 15-16: Catalog Toggle UI
- Days 17-18: Design Exploration Features
- Days 19-20: Testing + Documentation + Diagrams

---

## Risk Mitigation

### Phase 1 Risks
- **Risk:** 600 vessels insufficient coverage → **Mitigation:** Fallback to First-Principles
- **Risk:** KNN matches poor quality → **Mitigation:** Similarity threshold, warnings
- **Risk:** Scaling violates constraints → **Mitigation:** Clamping + fallback to next neighbor

### Phase 2 Risks
- **Risk:** 82K import too slow → **Mitigation:** Background seeding, partial import OK
- **Risk:** Parametric→Principal conversion inaccurate → **Mitigation:** Validate against geometric measures
- **Risk:** No vessel types in ML data → **Mitigation:** Geometry-only search, no mission matching

---

## Key Decisions

### Database
- ✅ Separate schemas: `catalog_ml` (read-only) + `catalog_user` (editable)
- ✅ Background seeding for 82K hulls (don't block startup)
- ✅ JSONB storage for 45 parametric parameters + extracted columns

### Algorithms
- ✅ Two KNN services: RealWorldKnn (mission-based), ParametricKnn (geometry-based)
- ✅ Same scaling algorithm for both (ratio-preserving)
- ✅ Always refine with first-principles for accuracy

### API Design
- ✅ Add CancellationToken to all long-running endpoints
- ✅ Feature flags: DataDrivenReal, DataDrivenML (independent toggles)
- ✅ Provenance in response (reference vessels, similarity scores)

### User Experience
- ✅ Mode toggle in Wizard Step 4 (Advanced Options)
- ✅ Catalog toggle when Data-Driven selected (Real vs ML)
- ✅ Auto-suggest based on input (vessel type → Real, geometric query → ML)
- ✅ Clear provenance display (which vessels/parameters influenced design)

---

## Definition of Done

### Phase 1 Complete When:
- [ ] 600 real vessels in database, seeded from CSV
- [ ] RealWorldKnnService returns top 5 similar vessels
- [ ] Scaling algorithm produces valid dimensions
- [ ] Hybrid solver combines KNN + first-principles
- [ ] API endpoint with CancellationToken support
- [ ] UI has mode toggle (First-Principles vs Data-Driven Real)
- [ ] Provenance displayed in results
- [ ] Tests: >80% coverage, validation passing
- [ ] Documentation complete (user guide + technical)
- [ ] Deployed to staging/dev

### Phase 2 Complete When:
- [ ] 82K parametric hulls in database (catalog_ml schema)
- [ ] ParametricKnnService searches geometric parameters
- [ ] Conversion: 45 params → Lpp, B, T, Cb, Displacement
- [ ] UI catalog toggle (Real vs ML)
- [ ] Design exploration features (parameter space visualization)
- [ ] Tests: >70% coverage on new components
- [ ] Documentation complete
- [ ] Schema diagram + architecture diagrams generated
- [ ] Deployed to staging/dev

---

## Next Documents

This overview links to detailed plans:
- `01-ARCHITECTURE.md` - Service boundaries, schemas, data flow
- `02-DATABASE-SCHEMA.md` - Complete DDL, permissions, indexes
- `03-PHASE1-REAL-WORLD.md` - Real-world catalog implementation (Weeks 1-2)
- `04-PHASE2-ML-PARAMETRIC.md` - ML catalog implementation (Weeks 3-4)
- `05-KNN-ALGORITHMS.md` - Both KNN implementations
- `06-SCALING-ALGORITHM.md` - Unified scaling approach
- `07-API-SPECIFICATION.md` - Endpoints, DTOs, CancellationTokens
- `08-TESTING-STRATEGY.md` - Test plans for both phases
- `09-DEPLOYMENT-PLAN.md` - Seeding, migrations, rollout
- `10-USER-GUIDE.md` - When/how to use each mode

---

**Document 1/10 Complete**  
**Next:** Architecture & Schema Design
