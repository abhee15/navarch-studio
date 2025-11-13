# Testing Coverage & Debt

**Overall Test Status**: 56% Pass Rate - **CRITICAL IMPROVEMENT NEEDED**  
**Last Updated**: November 4, 2025

---

## 📊 Testing Overview

### Summary by Module

| Module | Passing | Total | % | Status |
|--------|---------|-------|---|--------|
| **Hydrostatics** | 25 | 31 | 81% | ✅ Good |
| **Hull Sizing** | 14 | 39 | 36% | 🔴 Critical |
| **Resistance** | 7 | 11 | 64% | 🟡 Needs Work |
| **Catalog** | 0 | 0 | N/A | 🔴 Missing |
| **Frontend** | ~10 | ~50 | 20% | 🔴 Critical |
| **E2E** | 0 | 0 | N/A | 🔴 Missing |
| **Integration** | 0 | 0 | N/A | 🔴 Missing |
| **TOTAL** | **56** | **131+** | **43%** | **🔴** |

---

## 🔴 Critical Issues

### 1. Hull Sizing Solver (36% Pass Rate)

**Status**: 🔴 CRITICAL - Blocks production deployment

**Test Results**: 14/39 passing (36%)

| Test Suite | Passing | Total | % |
|------------|---------|-------|---|
| Resistance Service | 7 | 11 | 64% |
| Stability Service | 4 | 10 | 40% |
| Displacement Closure | 2 | 9 | 22% |
| Full Solver | 1 | 9 | 11% |

**Failing Tests**:
1. **Displacement Closure**
   - KCS test (known solution) - NOT CONVERGING
   - KVLCC2 test (known solution) - NOT CONVERGING
   - Rectangular barge - NOT CONVERGING
   - Convergence iteration count - FAILS
   - Initial guess accuracy - FAILS

2. **Stability Screen**
   - Decimal overflow when GMt < 0
   - Roll period formula incorrect (T=50s vs expected 5-30s)
   - KB + BMt > KG validation fails

3. **Beam Constraint**
   - Result exceeds maxBeam constraint
   - Clamping not applied correctly

**Root Causes**:
- Poor initial Lpp guess
- Conservative Newton adjustment factors
- Missing constraint enforcement
- Formula errors

**Impact**: **BLOCKS PRODUCTION DEPLOYMENT**

**Fix Priority**: 🔴 CRITICAL  
**Estimated Effort**: 8-12 hours

**Code Locations**:
- Tests: `backend/HullSizingService.Tests/Services/`
- Implementation: `backend/HullSizingService/Services/Solver/`

**Related Docs**:
- `.plan/hull-sizing/SOLVER-IMPROVEMENTS-NEEDED.md` - **MUST READ**

---

### 2. Frontend Component Tests (20% Coverage)

**Status**: 🔴 CRITICAL - Insufficient coverage

**Current State**:
- Only ExportDialog tested (20+ tests)
- Most components untested
- No integration tests
- No E2E tests

**Missing Tests**:
- Mission wizard (4 steps)
- Vessel workspace
- Candidate workspace
- Chart components (except tested ones)
- Forms (vessel, loadcase)
- Comparison views
- Catalog browser

**Impact**: Bugs in production, regression risks

**Fix Priority**: 🔴 CRITICAL  
**Estimated Effort**: 2-3 weeks for full coverage

**Code Locations**:
- Existing tests: `frontend/src/components/hydrostatics/workspace/ExportDialog.test.tsx`
- Missing: Most components in `frontend/src/`

---

### 3. No Integration Tests

**Status**: 🔴 CRITICAL - Major gap

**Missing Coverage**:
- API Gateway → downstream services
- Service → database workflows
- Multi-service transactions
- Authentication flows
- Tenant isolation
- Error propagation

**Impact**: Integration bugs slip through unit tests

**Fix Priority**: 🔴 CRITICAL  
**Estimated Effort**: 1-2 weeks

**Recommended Tool**: xUnit with test containers (PostgreSQL, Redis)

---

### 4. No E2E Tests

**Status**: 🔴 CRITICAL - No user workflow validation

**Missing Scenarios**:
1. **Hydrostatics Full Flow**
   - Register → Login → Create Vessel → Add Loadcase → Compute → Export

2. **Hull Sizing Full Flow**
   - Login → Create Mission → Run Solver → Open Workspace → Adjust Parameters

3. **Catalog Flow**
   - Browse Catalog → View Details → Clone to Vessel → Verify

4. **Comparison Flow**
   - Select Vessels → Compare → Export Report

5. **Error Paths**
   - Invalid input → Error message → Correction → Success

**Impact**: Critical user paths untested

**Fix Priority**: 🔴 CRITICAL  
**Estimated Effort**: 2-3 weeks

**Recommended Tool**: Playwright

---

## 🟡 Moderate Issues

### 5. Hydrostatics Tests (81% Pass Rate)

**Status**: 🟡 Good but improvable

**Test Results**: 25/31 passing (81%)

| Test Suite | Passing | Total | % |
|------------|---------|-------|---|
| Integration Engine | 8 | 8 | 100% |
| Hydro Calculator | 6 | 6 | 100% |
| Curves Generator | 4 | 4 | 100% |
| Export Service | 10 | 10 | 100% |
| Stability | 2 | 4 | 50% |
| Wigley Hull | 2 | 7 | 29% |

**Skipped Tests** (6):
1. **Wigley Hull Tests** (5 skipped)
   - Center of buoyancy (KB/LCB)
   - Waterplane area
   - Metacentric radius
   - Form coefficients
   - GZ curve
   - **Reason**: z-normalization bug FIXED, awaiting rebuild

2. **Stability Test** (1 skipped)
   - Wall-sided vs Full Immersion agreement
   - **Reason**: 42,000% error at small angles (needs debug)

**Blocker**: AWS SDK dependency conflict prevents rebuild

**Fix Priority**: 🟠 High  
**Estimated Effort**: 1 hour (fix conflict + rebuild)

**Code Locations**:
- Tests: `backend/DataService.Tests/Services/Hydrostatics/`
- Blocker fix: `backend/DataService/DataService.csproj` (update AWSSDK.Core to 4.0.0.32)

**Related Docs**:
- `temp/HYDROSTATICS_COMPLETION_STATUS.md`
- `temp/WIGLEY_FIX_SUMMARY.md`

---

### 6. Resistance Tests (64% Pass Rate)

**Status**: 🟡 Needs improvement

**Test Results**: 7/11 passing (64%)

**Failing Tests**:
- Accuracy validation against published data (±30% error)
- Extreme form coefficients (Cb < 0.3 or > 0.95)
- Wetted surface validation (can return negative)
- Form factor edge cases

**Root Causes**:
- Using simplified Holtrop formula (not full 20-term)
- No validation for edge cases
- Limited benchmark data

**Fix Priority**: 🟠 High  
**Estimated Effort**: 2-3 days (implement full formula + tests)

**Code Locations**:
- Tests: `backend/DataService.Tests/Services/Resistance/`
- Implementation: `backend/DataService/Services/Resistance/`

---

### 7. No Catalog Tests

**Status**: 🟡 Missing entirely

**Missing Tests**:
- CatalogWaterService interpolation
- CatalogHullsController endpoints
- Clone workflow (catalog → vessel)
- Propeller queries
- Database seeding

**Fix Priority**: 🟡 Medium  
**Estimated Effort**: 6-8 hours

**Code Locations** (tests to create):
- `backend/DataService.Tests/Services/Catalog/`
- `backend/DataService.Tests/Controllers/CatalogTests.cs`

---

## 🟢 Good Coverage Areas

### 8. Export Service Tests (100%)

**Status**: ✅ Excellent

**Test Results**: 10/10 passing (100%)

**Coverage**:
- CSV export
- JSON export
- PDF export (with/without curves)
- Excel export (with/without curves)
- Error handling
- File format validation

**Code Locations**:
- Tests: `backend/DataService.Tests/Services/Hydrostatics/ExportServiceTests.cs`

---

### 9. Integration Engine Tests (100%)

**Status**: ✅ Excellent

**Test Results**: 8/8 passing (100%)

**Coverage**:
- Trapezoidal rule
- Simpson's 1/3 rule
- Simpson's 3/8 rule
- First/second moments
- Automatic method selection

**Code Locations**:
- Tests: `backend/DataService.Tests/Services/Hydrostatics/IntegrationEngineTests.cs`

---

## 📋 Testing Priorities

### Sprint 1 (This Week) - Critical Fixes

1. **Fix Hull Sizing Solver** (8-12 hours)
   - Fix displacement closure convergence
   - Fix beam constraint enforcement
   - Fix stability overflow
   - Fix roll period formula
   - **Goal**: >90% pass rate

2. **Fix AWS SDK Conflict** (15 min)
   - Update DataService.csproj
   - Rebuild and verify Wigley tests
   - **Goal**: 31/31 hydrostatics tests passing

3. **Fix CI Workflow Skip** (30 min)
   - Ensure backend builds on Shared/ changes
   - **Goal**: Reliable deployments

---

### Sprint 2 (Next Week) - Integration Tests

4. **Create Integration Test Suite** (1-2 weeks)
   - Set up test containers (PostgreSQL)
   - Write API Gateway integration tests
   - Test service → database workflows
   - Test authentication flows
   - **Goal**: 50+ integration tests

5. **Add Catalog Tests** (6-8 hours)
   - Test water property interpolation
   - Test clone workflow
   - Test catalog queries
   - **Goal**: 20+ catalog tests

---

### Sprint 3 (Month 2) - E2E & Frontend

6. **E2E Test Suite with Playwright** (2-3 weeks)
   - Set up Playwright
   - Write 20+ critical user path tests
   - Add to CI/CD pipeline
   - **Goal**: Automated E2E regression tests

7. **Frontend Component Tests** (2-3 weeks)
   - Test mission wizard
   - Test vessel workspace
   - Test candidate workspace
   - Test forms
   - **Goal**: >70% frontend coverage

---

## 🎯 Testing Strategy

### Unit Tests
- **Coverage Target**: >80% per module
- **Tools**: xUnit (backend), Jest (frontend)
- **CI**: Run on every PR
- **Blockers**: Solver issues, missing coverage

### Integration Tests
- **Coverage Target**: 50+ scenarios
- **Tools**: xUnit + Test Containers
- **CI**: Run on merge to main
- **Blockers**: Need to set up test infrastructure

### E2E Tests
- **Coverage Target**: 20+ critical paths
- **Tools**: Playwright
- **CI**: Run nightly + before production deploy
- **Blockers**: None, ready to implement

### Performance Tests
- **Coverage Target**: 10+ benchmarks
- **Tools**: BenchmarkDotNet (backend), Lighthouse (frontend)
- **CI**: Run weekly
- **Blockers**: Need to define benchmarks

---

## 📚 Test Documentation Needed

1. **Testing Strategy Guide** - Overall approach
2. **Unit Test Examples** - Patterns to follow
3. **Integration Test Setup** - How to run locally
4. **E2E Test Guide** - Writing Playwright tests
5. **Test Data Guide** - Fixtures and factories
6. **CI/CD Testing** - How tests run in pipeline

---

## 🛠️ Testing Infrastructure

### Current State
- ✅ xUnit configured (backend)
- ✅ Jest configured (frontend)
- ✅ Tests run in CI/CD
- ❌ No test containers
- ❌ No E2E framework
- ❌ No performance testing
- ❌ No test coverage reports

### Needed Additions
1. **Test Containers** (PostgreSQL, Redis)
2. **Playwright** for E2E
3. **BenchmarkDotNet** for performance
4. **Coverlet** for coverage reports
5. **Test data factories** (Bogus library)
6. **Mock libraries** (Moq already in use)

---

## 🏆 Success Metrics

**Current Status**: 43% overall pass rate - **UNACCEPTABLE**

| Metric | Target | Current | Status |
|--------|--------|---------|--------|
| Overall Pass Rate | >95% | 43% | 🔴 |
| Backend Unit | >80% | 56% | 🔴 |
| Frontend Unit | >70% | 20% | 🔴 |
| Integration Tests | 50+ | 0 | 🔴 |
| E2E Tests | 20+ | 0 | 🔴 |
| Coverage Report | Enabled | Disabled | 🔴 |

**Recommendation**: Testing is the #1 blocker to production deployment. Must fix solver tests and add integration/E2E tests before launch.

---

## 🚨 Deployment Blockers

**Cannot deploy to production until:**

1. ✅ Fix hull sizing solver (36% → >90%)
2. ✅ Fix AWS SDK conflict (enable Wigley tests)
3. ✅ Add integration tests (50+ scenarios)
4. ✅ Add E2E tests (20+ critical paths)
5. ✅ Frontend test coverage >50%

**Estimated Total Effort**: 6-8 weeks of dedicated testing work

---

## 📊 Test Coverage by File

### Backend (sampled)

| File | Lines | Coverage | Status |
|------|-------|----------|--------|
| HoltropResistanceService.cs | 250 | ~70% | 🟡 |
| DisplacementClosureService.cs | 200 | ~40% | 🔴 |
| StabilityScreenService.cs | 180 | ~50% | 🔴 |
| ExportService.cs | 300 | 100% | ✅ |
| IntegrationEngine.cs | 150 | 100% | ✅ |
| CatalogWaterService.cs | 100 | 0% | 🔴 |

### Frontend (sampled)

| Component | Tests | Coverage | Status |
|-----------|-------|----------|--------|
| ExportDialog.tsx | 20+ | 100% | ✅ |
| MissionWizard.tsx | 0 | 0% | 🔴 |
| VesselWorkspace.tsx | 0 | 0% | 🔴 |
| CandidateWorkspace.tsx | 0 | 0% | 🔴 |
| ResistanceCharts.tsx | 0 | 0% | 🔴 |

---

**Last Updated**: November 4, 2025  
**Owner**: QA Team  
**Next Review**: Weekly until >80% coverage achieved











