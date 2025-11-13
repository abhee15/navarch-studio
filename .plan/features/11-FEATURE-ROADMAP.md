# Feature Roadmap - Prioritized Next Steps

**Planning Horizon**: 3 Months (November 2025 - January 2026)  
**Last Updated**: November 4, 2025  
**Status**: 54% Complete → Target 85% by End of Q1 2026

---

## 🎯 Strategic Goals

### Month 1 (November 2025)
**Theme**: Stabilize & Secure  
**Goal**: Fix critical issues, complete security, achieve >80% test coverage

### Month 2 (December 2025)
**Theme**: Enhance & Export  
**Goal**: CAD exports, advanced analysis, propeller integration

### Month 3 (January 2026)
**Theme**: Optimize & Polish  
**Goal**: Performance improvements, mobile optimization, developer experience

---

## 📅 Month 1: November 2025 - Stabilize & Secure

### Week 1 (Nov 4-8): Critical Fixes

**Priority**: 🔴 CRITICAL

#### Quick Wins Day (2 days, 10 hours)
- [ ] AWS SDK dependency fix (15 min)
- [ ] CI workflow skip fix (30 min)
- [ ] Polly timeout policy (45 min)
- [ ] Enable Wigley tests (30 min)
- [ ] Loading indicators (1 hour)
- [ ] Wizard Step 4 polish (1 hour)
- [ ] CHECK constraints (1 hour)
- [ ] Keyboard shortcuts (30 min)
- [ ] XML docs (5 min)
- [ ] Property mismatch fix (30 min)
- [ ] Parameter tooltips (1.5 hours)
- [ ] Catalog tests (2 hours)

**Deliverable**: 12 quick wins complete, 3 critical blockers resolved

#### Solver Fixes (3 days, 24 hours)
- [ ] Fix displacement closure convergence
  - Improve initial Lpp guess (cube root scaling)
  - Increase adjustment factors (0.5 → 0.7-0.8)
  - Add adaptive step sizing
  - Test with KCS, KVLCC2, barge (8 hours)
- [ ] Fix beam constraint enforcement (2 hours)
- [ ] Fix stability overflow (2 hours)
- [ ] Fix roll period formula (1 hour)
- [ ] Re-run all 39 tests
- [ ] Target: >90% pass rate (currently 36%)

**Deliverable**: Hull sizing solver production-ready

**Success Metrics**:
- Solver tests: 36% → >90% ✅
- Hydrostatics tests: 81% → 100% ✅
- CI reliability: 100% ✅

---

### Week 2 (Nov 11-15): Security & Testing Foundation

**Priority**: 🔴 CRITICAL

#### Complete Security (Phase 11 Week 2-3) (2 days, 12 hours)
- [ ] Secrets Manager integration (4 hours)
  - Move DB passwords
  - Move JWT keys
  - Update Terraform
  - Test in staging
- [ ] RBAC implementation (6 hours)
  - Define roles (User, Admin, SuperAdmin)
  - Use Cognito groups
  - Add `[Authorize(Roles)]` attributes
  - Test role enforcement
- [ ] Audit logging (2 hours)
  - Log login attempts
  - Log admin actions
  - Test log capture

**Deliverable**: Phase 11 security complete

#### Integration Test Foundation (3 days, 20 hours)
- [ ] Set up test containers (PostgreSQL) (4 hours)
- [ ] Write API Gateway integration tests (8 hours)
- [ ] Write service → database tests (6 hours)
- [ ] Add authentication flow tests (2 hours)
- [ ] Target: 30+ integration tests

**Deliverable**: Integration test suite operational

**Success Metrics**:
- Security: Phase 11 100% complete ✅
- Integration tests: 0 → 30+ ✅

---

### Week 3 (Nov 18-22): Testing Expansion

**Priority**: 🟠 HIGH

#### More Integration Tests (2 days, 16 hours)
- [ ] Multi-service transaction tests (6 hours)
- [ ] Tenant isolation tests (4 hours)
- [ ] Error propagation tests (4 hours)
- [ ] Performance tests (2 hours)
- [ ] Target: 50+ total integration tests

**Deliverable**: Comprehensive integration coverage

#### Monitoring & Alerting (3 days, 20 hours)
- [ ] CloudWatch alarms (6 hours)
  - Error rate alerts
  - Latency alerts (P95/P99)
  - Resource utilization
- [ ] Performance dashboards (8 hours)
  - Response times
  - Request counts
  - Database metrics
- [ ] Cost monitoring (2 hours)
  - AWS Budgets
  - Cost alerts
- [ ] DR plan documentation (4 hours)

**Deliverable**: Production monitoring ready

**Success Metrics**:
- Integration tests: 30 → 50+ ✅
- Monitoring: Comprehensive ✅

---

### Week 4 (Nov 25-29): Full Holtrop & Catalog

**Priority**: 🟠 HIGH

#### Full Holtrop-Mennen Implementation (3 days, 20 hours)
- [ ] Implement full 20-term formula (12 hours)
- [ ] Add bulbous bow corrections (3 hours)
- [ ] Add transom stern corrections (2 hours)
- [ ] Benchmark validation tests (3 hours)
- [ ] Target: ±10% accuracy (currently ±30%)

**Deliverable**: Accurate resistance prediction

#### Catalog Improvements (2 days, 12 hours)
- [ ] Advanced search/filter (6 hours)
- [ ] Data quality validation (4 hours)
- [ ] Geometry caching (2 hours)

**Deliverable**: Enhanced catalog functionality

**Success Metrics**:
- Resistance accuracy: ±30% → ±10% ✅
- Catalog features: Enhanced ✅

---

## 📅 Month 2: December 2025 - Enhance & Export

### Week 5 (Dec 2-6): E2E Testing Setup

**Priority**: 🔴 CRITICAL

#### Playwright E2E Tests (5 days, 30 hours)
- [ ] Playwright setup & CI integration (4 hours)
- [ ] Hydrostatics full flow (6 hours)
  - Register → Login → Vessel → Loadcase → Compute → Export
- [ ] Hull sizing full flow (8 hours)
  - Login → Mission → Solver → Workspace → Adjust
- [ ] Catalog flow (4 hours)
  - Browse → Details → Clone → Verify
- [ ] Comparison flow (4 hours)
- [ ] Error path tests (4 hours)
- [ ] Target: 20+ critical paths

**Deliverable**: E2E test suite operational

**Success Metrics**:
- E2E tests: 0 → 20+ ✅
- Critical user paths: Tested ✅

---

### Week 6-7 (Dec 9-20): Frontend Testing

**Priority**: 🔴 CRITICAL

#### Frontend Component Tests (2 weeks, 60 hours)
- [ ] Mission wizard tests (8 hours)
- [ ] Vessel workspace tests (10 hours)
- [ ] Candidate workspace tests (12 hours)
- [ ] Chart component tests (10 hours)
- [ ] Form tests (8 hours)
- [ ] Comparison view tests (8 hours)
- [ ] Catalog browser tests (4 hours)
- [ ] Target: >70% frontend coverage

**Deliverable**: Comprehensive frontend test coverage

**Success Metrics**:
- Frontend tests: 20% → >70% ✅

---

### Week 8 (Dec 23-27): CAD Exports

**Priority**: 🟠 HIGH

#### DXF/IGES/STL Export (5 days, 30 hours)
- [ ] Research libraries (IxMilia, netDXF) (4 hours)
- [ ] Implement DXF export (10 hours)
  - 2D lines plan
  - Waterlines and stations
  - Dimensions
- [ ] Implement IGES export (10 hours)
  - 3D surface generation
  - AutoCAD compatible
- [ ] Implement STL export (4 hours)
  - 3D mesh for CFD/3D printing
- [ ] Export UI integration (2 hours)

**Deliverable**: CAD export capability

**Success Metrics**:
- Export formats: 4 → 7 (CSV, JSON, PDF, Excel, DXF, IGES, STL) ✅

---

## 📅 Month 3: January 2026 - Optimize & Polish

### Week 9 (Dec 30 - Jan 3): Propeller Integration

**Priority**: 🟠 HIGH

#### Obtain Wageningen B-Series Data (External)
- [ ] Download dataset from Zenodo or MARIN
- [ ] Parse open-water curves (KT, KQ, η0 vs J)
- [ ] Import to catalog database

#### Propeller Performance Module (3 days, 20 hours)
- [ ] Propeller selection logic (8 hours)
- [ ] Performance curve interpolation (6 hours)
- [ ] Delivered power calculation (4 hours)
- [ ] UI integration (2 hours)

**Deliverable**: EHP → PD → PB → PI power chain

**Success Metrics**:
- Power calculation: Complete chain ✅

---

### Week 10 (Jan 6-10): Sensitivity Analysis

**Priority**: 🟡 MEDIUM

#### Sensitivity Analysis Dashboard (4 days, 24 hours)
- [ ] ±10% parameter variation engine (8 hours)
- [ ] Tornado chart visualization (6 hours)
- [ ] "Optimize for..." presets (4 hours)
- [ ] Export analysis report (4 hours)
- [ ] UI integration (2 hours)

**Deliverable**: Design space exploration tool

**Success Metrics**:
- Analysis tools: Enhanced ✅

---

### Week 11 (Jan 13-17): Performance Optimization

**Priority**: 🟡 MEDIUM

#### Redis Caching (2 days, 12 hours)
- [ ] AWS ElastiCache setup (Terraform) (4 hours)
- [ ] Implement caching layer (6 hours)
  - Water properties (12h TTL)
  - Catalog hulls (1h TTL)
  - API responses (5min TTL)
- [ ] Cache invalidation strategy (2 hours)

#### Bundle Size Optimization (3 days, 18 hours)
- [ ] Lazy load routes (6 hours)
- [ ] Tree-shake Recharts (4 hours)
- [ ] Optimize images (WebP) (4 hours)
- [ ] Code splitting (4 hours)
- [ ] Target: <3 MB uncompressed

**Deliverable**: Faster load times, lower AWS costs

**Success Metrics**:
- Bundle size: 4.1 MB → <3 MB ✅
- Cache hit rate: >80% ✅

---

### Week 12 (Jan 20-24): Mobile & Accessibility

**Priority**: 🟡 MEDIUM

#### Mobile Optimization (3 days, 20 hours)
- [ ] Touch gestures (swipe, pinch) (6 hours)
- [ ] Mobile navigation improvements (6 hours)
- [ ] Simplified forms (4 hours)
- [ ] PWA support (offline mode) (4 hours)

#### Accessibility (A11y) (2 days, 12 hours)
- [ ] Screen reader support (4 hours)
- [ ] Keyboard navigation (4 hours)
- [ ] ARIA labels (2 hours)
- [ ] Color contrast fixes (2 hours)

**Deliverable**: Mobile-optimized, accessible app

**Success Metrics**:
- Mobile UX: Optimized ✅
- WCAG 2.1 AA: Compliant ✅

---

### Week 13 (Jan 27-31): Polish & Documentation

**Priority**: 🟢 LOW

#### Final Polish (2 days, 12 hours)
- [ ] Guided tours (8 hours)
- [ ] Advanced animations (Framer Motion) (4 hours)

#### Documentation (3 days, 20 hours)
- [ ] API reference guide (6 hours)
- [ ] User guide with screenshots (8 hours)
- [ ] Video tutorials (6 hours)

**Deliverable**: Production-ready documentation

**Success Metrics**:
- Documentation: Complete ✅
- User onboarding: Streamlined ✅

---

## 🎯 Success Criteria

### End of Month 1 (November 30)
- [ ] Solver test pass rate: >90% (currently 36%)
- [ ] Hydrostatics test pass rate: 100% (currently 81%)
- [ ] Security: Phase 11 complete
- [ ] Integration tests: 50+ scenarios
- [ ] Monitoring: Comprehensive dashboards
- [ ] Full Holtrop: Implemented

**Status Goal**: 65% complete (from 54%)

---

### End of Month 2 (December 31)
- [ ] E2E tests: 20+ critical paths
- [ ] Frontend tests: >70% coverage (currently 20%)
- [ ] CAD exports: DXF, IGES, STL functional
- [ ] Overall test pass rate: >85%

**Status Goal**: 75% complete (from 65%)

---

### End of Month 3 (January 31)
- [ ] Propeller integration: Complete power chain
- [ ] Sensitivity analysis: Implemented
- [ ] Performance: Redis caching, bundle <3 MB
- [ ] Mobile: Optimized UX
- [ ] Accessibility: WCAG 2.1 AA compliant
- [ ] Documentation: Complete

**Status Goal**: 85% complete (from 75%)

---

## 📊 Velocity Tracking

### Historical Velocity
- **Week 1 (Oct 28-Nov 3)**: 8 features, 25 tests, 12 docs
- **Week 2 (Nov 4-Nov 10)**: 5 features, 10 tests, 6 docs
- **Average**: 6.5 features/week, 17.5 tests/week

### Projected Velocity
- **Month 1**: 20-25 features, 80+ tests
- **Month 2**: 15-20 features, 60+ tests
- **Month 3**: 10-15 features, 40+ tests

**Total Expected**: 45-60 features, 180+ tests over 3 months

---

## 🚧 Risk Mitigation

### High Risk Items

1. **External Data Dependencies**
   - **Risk**: Catalog geometry files, propeller data unavailable
   - **Mitigation**: Manual CSV upload workaround, synthetic data generation
   - **Impact**: Catalog/propeller features delayed by 1-2 weeks

2. **Solver Convergence Fix**
   - **Risk**: Fixes don't achieve >90% pass rate
   - **Mitigation**: Allocate extra time (8-12 hours → 16-20 hours), consult algorithm experts
   - **Impact**: Week 1 extends to Week 2

3. **E2E Test Complexity**
   - **Risk**: Playwright setup more complex than estimated
   - **Mitigation**: Use template/boilerplate, focus on critical paths only
   - **Impact**: Week 5 extends by 2-3 days

4. **Performance Optimization**
   - **Risk**: Bundle size reduction doesn't reach target
   - **Mitigation**: Aggressive code splitting, consider alternative bundler (Rollup)
   - **Impact**: Week 11 goal adjusted to <3.5 MB

---

## 🎯 Milestone Checkpoints

### Checkpoint 1: November 15
**Theme**: Critical fixes complete

**Must Have**:
- ✅ Solver tests >90%
- ✅ Security Phase 11 complete
- ✅ Integration tests foundation (30+)

**Go/No-Go**: If not achieved, extend Month 1 by 1 week

---

### Checkpoint 2: December 15
**Theme**: Testing complete

**Must Have**:
- ✅ E2E tests (20+)
- ✅ Frontend tests >70%
- ✅ CAD exports functional

**Go/No-Go**: If not achieved, defer Month 3 polish work

---

### Checkpoint 3: January 31
**Theme**: Production-ready

**Must Have**:
- ✅ Overall completion >85%
- ✅ All critical/high priority items complete
- ✅ Documentation complete
- ✅ Performance targets met

**Go/No-Go**: Production launch approval

---

## 🔄 Feedback Loops

### Weekly Sprint Reviews
- **When**: Every Friday 4pm
- **Duration**: 1 hour
- **Attendees**: Dev team, stakeholders
- **Agenda**:
  - Completed features demo
  - Velocity review
  - Blockers discussion
  - Next week planning

### Biweekly Retrospectives
- **When**: Every other Friday 3pm (before sprint review)
- **Duration**: 1 hour
- **Focus**: What went well, what to improve, action items

### Monthly Stakeholder Review
- **When**: Last Friday of month
- **Duration**: 2 hours
- **Focus**: Progress dashboard, roadmap adjustments, prioritization

---

## 📞 Questions for Stakeholder Discussion

Before finalizing roadmap:

1. **Priority Alignment**: Is stabilization → testing → enhancement the right order?
2. **Timeline Pressure**: Any hard deadlines we should know about?
3. **Feature Priorities**: Any features we should prioritize/deprioritize?
4. **Resource Availability**: Team size/availability constant over 3 months?
5. **External Dependencies**: Can we obtain SIMMAN/Wageningen data?
6. **Budget Constraints**: AWS costs for Redis, monitoring, etc. approved?

---

## 💡 Notes

- This roadmap assumes full-time dedication
- Adjust timelines if part-time or parallel work
- Quick wins can be done in parallel with larger features
- Technical debt paydown continues alongside new features
- User feedback may shift priorities mid-stream

---

**Roadmap Owner**: Product Management + Engineering Lead  
**Last Updated**: November 4, 2025  
**Next Review**: November 15, 2025 (Checkpoint 1)  
**Status**: **APPROVED** / Pending Review / Needs Revision

---

## 🎉 Vision: End of Q1 2026

**NavArch Studio** will be:
- ✅ Production-ready with >85% feature completion
- ✅ Secure (Phase 11 complete)
- ✅ Well-tested (>80% coverage, E2E tests)
- ✅ Fast (<2s load, Redis caching)
- ✅ Professional (CAD exports, advanced analysis)
- ✅ Accessible (mobile-optimized, WCAG compliant)
- ✅ Documented (guides, videos, API docs)

**Ready for**: Public beta launch, customer acquisition, feature expansion

---

**Prepared by**: Development Team  
**Date**: November 4, 2025  
**Approved by**: _________________ (Stakeholder)  
**Date**: _________________











