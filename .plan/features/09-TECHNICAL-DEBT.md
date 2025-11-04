# Technical Debt Inventory

**Total Items**: 45  
**Total Estimated Effort**: 70-105 hours  
**Last Updated**: November 4, 2025

---

## 📊 Debt Summary by Priority

| Priority | Count | Est. Effort | Notes |
|----------|-------|-------------|-------|
| 🔴 **Critical** | 8 | 15-20 hours | Blocks production |
| 🟠 **High** | 12 | 20-30 hours | Important for stability |
| 🟡 **Medium** | 15 | 25-40 hours | Impacts quality |
| 🟢 **Low** | 10 | 10-15 hours | Nice to have |

---

## 🔴 Critical Priority (Blocks Production)

### 1. Hull Sizing Solver Convergence

**Module**: Hull Sizing  
**Impact**: Production deployment blocked  
**Effort**: 8-12 hours

**Problem**:
- Displacement closure fails for known solutions (KCS, KVLCC2, barge)
- Only 14/39 tests passing (36%)
- Poor initial guess, conservative adjustment factors
- Convergence rate ~40% (needs >95%)

**Root Causes**:
- Initial Lpp guess uses fixed factor (should be cube root scaling)
- Newton loop adjustment factors too conservative (0.5, should be 0.7-0.8)
- No adaptive step sizing
- Fixed-point iteration instead of proper Newton-Raphson

**Fix**:
1. Improve initial guess algorithm
2. Increase adjustment factors
3. Add adaptive stepping
4. Consider secant method

**Code Locations**:
- `backend/HullSizingService/Services/Solver/DisplacementClosureService.cs`
- `.plan/hull-sizing/SOLVER-IMPROVEMENTS-NEEDED.md` - **FULL DETAILS**

**Test Status**: 2/9 passing (22%)

---

### 2. Beam Constraint Not Enforced

**Module**: Hull Sizing  
**Impact**: Results violate user constraints  
**Effort**: 2 hours

**Problem**:
- Result beam (18.5m) exceeds maxBeam constraint (15m)
- Constraint check happens AFTER adjustment
- Final value not clamped

**Fix**:
- Clamp beam/draft BEFORE calculating next iteration
- Add assertion at end: `Debug.Assert(beam <= maxBeam ?? decimal.MaxValue)`

**Code Locations**:
- `backend/HullSizingService/Services/Solver/DisplacementClosureService.cs`

**Test Status**: 0/3 constraint tests passing

---

### 3. Stability Screen Decimal Overflow

**Module**: Hull Sizing  
**Impact**: Solver crashes on some inputs  
**Effort**: 2 hours

**Problem**:
- `System.OverflowException` when GMt is negative or very small
- `Math.Sqrt(negative)` or `Math.Sqrt(near-zero)` causes overflow
- No validation before sqrt

**Fix**:
- Guard against negative GM: `if (gmt <= 0) return StabilityResult with flags`
- Clamp roll period calculation: `Math.Max(0.01, gmt)` before sqrt
- Add validation: KB + BMt should be > KG

**Code Locations**:
- `backend/HullSizingService/Services/Solver/StabilityScreenService.cs`

**Test Status**: 4/10 stability tests passing (40%)

---

### 4. Roll Period Formula Error

**Module**: Hull Sizing  
**Impact**: Incorrect results  
**Effort**: 1 hour

**Problem**:
- T_roll = 50.8s (expected 5-30s)
- Current formula: `T_roll = 2 * B / sqrt(GMt)` (wrong)
- Should use proper radius of gyration

**Fix**:
- Correct formula: `T_roll = 2 * π * kxx / sqrt(g * GMt)`
- Use proper radius of gyration: `kxx ≈ 0.35 * B`
- Final: `T_roll = 2 * π * 0.35 * B / sqrt(9.81 * GMt)`

**Code Locations**:
- `backend/HullSizingService/Services/Solver/StabilityScreenService.cs`

**Test Status**: 1/2 roll period tests passing

---

### 5. AWS SDK Dependency Conflict

**Module**: Infrastructure  
**Impact**: Prevents DataService rebuild  
**Effort**: 15 minutes

**Problem**:
- AWSSDK.Core version conflict
- Cannot rebuild DataService
- Blocks Wigley hull test verification

**Fix**:
```xml
<!-- In backend/DataService/DataService.csproj -->
<PackageReference Include="AWSSDK.Core" Version="4.0.0.32" />
```

Then:
```bash
dotnet build
dotnet test --filter "FullyQualifiedName~WigleyHull"
```

**Expected Result**: 7/7 Wigley tests passing (currently 2/7)

**Code Locations**:
- `backend/DataService/DataService.csproj`

**Related Docs**:
- `temp/HYDROSTATICS_COMPLETION_STATUS.md`
- `temp/WIGLEY_FIX_SUMMARY.md`

---

### 6. CI Workflow Skips Backend Builds

**Module**: CI/CD  
**Impact**: Failed deployments, manual intervention  
**Effort**: 30 minutes

**Problem**:
- Backend builds skipped when only `backend/Shared/` changes
- Services don't get updated code
- Requires manual `force_full_build=true` trigger

**Root Cause**: `has-secrets` condition in workflow

**Fix**:
Remove condition from `.github/workflows/ci-dev.yml`:
```yaml
# Before:
if: needs.check-changes.outputs.backend == 'true' && secrets.AWS_ACCESS_KEY_ID

# After:
if: needs.check-changes.outputs.backend == 'true'
```

**Code Locations**:
- `.github/workflows/ci-dev.yml`
- `.github/workflows/ci-staging.yml`
- `.github/workflows/ci-prod.yml`

**Related Docs**:
- `temp/WORKFLOW-SKIP-ISSUE.md`

---

### 7. Test Coverage Below 50%

**Module**: All  
**Impact**: Bugs slip to production  
**Effort**: 6-8 weeks

**Problem**:
- Overall: 43% pass rate
- Hull Sizing: 36%
- Frontend: 20%
- No E2E tests
- No integration tests

**Fix**: See `08-TESTING-DEBT.md` for comprehensive plan

**Priority Order**:
1. Fix hull sizing solver tests (week 1)
2. Add integration tests (week 2-3)
3. Add E2E tests with Playwright (week 4-5)
4. Add frontend component tests (week 6-8)

---

### 8. Security Hardening Incomplete (Phase 11 Week 2-3)

**Module**: Infrastructure  
**Impact**: Production security posture  
**Effort**: 6 hours

**Incomplete Items**:
1. **Secrets Manager** (2 hours)
   - Move DB passwords to AWS Secrets Manager
   - Move JWT keys to Secrets Manager
   - Inject via App Runner environment

2. **RBAC** (3 hours)
   - Define roles: User, Admin, SuperAdmin
   - Use Cognito groups
   - Implement `[Authorize(Roles = "Admin")]`

3. **Audit Logging** (1 hour)
   - Log login attempts
   - Log password changes
   - Log admin actions
   - Log data access

**Code Locations**:
- All services need Secrets Manager integration
- Terraform: `terraform/deploy/modules/secrets/` (to create)

**Related Docs**:
- `.plan/phase11-security.md` - **DETAILED PLAN**

---

## 🟠 High Priority

### 9. Full Immersion Stability Method Accuracy

**Module**: Hydrostatics  
**Effort**: 4-6 hours

**Problem**: 42,000% error vs Wall-sided at small angles

**Fix**: Debug calculation, compare with reference implementations

**Workaround**: Use Wall-sided for angles <20°

---

### 10. Simplified Holtrop Formula

**Module**: Resistance  
**Effort**: 2-3 days

**Problem**: ±20-30% accuracy reduction vs full formula

**Fix**: Implement full 20-term Holtrop-Mennen 1984 method

**Reference**: SPS SName paper 2014

---

### 11. Form Factor Single Formula

**Module**: Resistance  
**Effort**: 4-6 hours

**Problem**: Doesn't account for hull type (slender vs full)

**Fix**: Use Holtrop 1984 for Cb<0.65, Holtrop 1988 for Cb>0.75

---

### 12. No Propeller Integration

**Module**: Resistance  
**Effort**: 3-5 days

**Problem**: Only EHP calculated, not delivered/brake/installed power

**Blocker**: Needs catalog propeller B-series data

**Fix**: Implement propeller performance + transmission losses

---

### 13. Missing Polly Resilience Policies

**Module**: Hull Sizing  
**Effort**: 1 hour

**Problem**: No retry/circuit breaker for DataService HTTP calls

**Fix**: Add Polly timeout, retry, circuit breaker policies

**Code Locations**:
- `backend/HullSizingService/Program.cs`

**Related Docs**:
- `.plan/hull-sizing/plan/11-FUTURE-IMPROVEMENTS.md`

---

### 14. Missing Database CHECK Constraints

**Module**: Backend (All)  
**Effort**: 1-2 hours per module

**Problem**: No database-level validation for dimensions, coefficients

**Fix**: Add CHECK constraints in migrations:
```sql
ALTER TABLE sizing.mission_cases
  ADD CONSTRAINT chk_speed_positive CHECK (service_speed_kn > 0);

ALTER TABLE sizing.candidate_designs
  ADD CONSTRAINT chk_coefficients_range 
    CHECK (cb BETWEEN 0.3 AND 0.95 AND cp BETWEEN 0.5 AND 1.0);
```

**Code Locations**:
- New migration files in `backend/*/Migrations/`

---

### 15. Property Name Mismatch

**Module**: Hull Sizing  
**Effort**: 30 minutes

**Problem**: Backend DTO uses `BeamM`, frontend expects `bM` (camelCase)

**Mitigation**: JSON serialization configured for camelCase

**Fix**: Verify consistency, add null-safety checks

---

### 16. No Integration Tests

**Module**: All  
**Effort**: 1-2 weeks

**Problem**: Service-to-service integration untested

**Fix**: Create integration test suite with test containers

---

### 17. Missing Loading Indicators

**Module**: Frontend  
**Effort**: 1 hour

**Problem**: Some operations lack visual feedback

**Missing In**:
- Mission wizard submit
- Solver execution
- Workspace initial load
- Data fetching in tables

**Fix**: Add spinners/skeletons

---

### 18. Wizard Step 4 UI Polish

**Module**: Frontend  
**Effort**: 1 hour

**Problem**: Layout cramped, labels unclear, no tooltips

**Fix**: Redesign layout, add help text, improve spacing

---

### 19. No ECR Repository for HullSizingService

**Module**: Infrastructure  
**Effort**: 30 minutes

**Problem**: CI/CD needs `ECR_HULL_SIZING_SERVICE_URL` secret

**Fix**: Add ECR repo in `terraform/setup/main.tf`

---

### 20. No Terraform for HullSizingService

**Module**: Infrastructure  
**Effort**: 2-3 hours

**Problem**: Service deployed manually, not in Terraform

**Fix**: Add App Runner module for HullSizing in `terraform/deploy/`

---

## 🟡 Medium Priority

### 21. Geometry Not Implemented

**Module**: Hull Sizing  
**Effort**: 4-6 hours

**Problem**: Offsets table shows placeholder "0.000" values

**Fix**: Implement geometry generator from Wigley/Series 60 formulas

---

### 22. No Catalog Tests

**Module**: Catalog  
**Effort**: 6-8 hours

**Problem**: Zero test coverage for catalog services

**Fix**: Write unit tests for water service, hull controller, clone workflow

---

### 23. Geometry Storage Not Optimized

**Module**: Catalog  
**Effort**: 1-2 days

**Problem**: JSON for large offset grids (potentially slow)

**Fix**: Consider binary storage or compression

---

### 24. No Data Quality Validation

**Module**: Catalog  
**Effort**: 3-4 days

**Problem**: Catalog data not validated against geometry

**Fix**: Add validation service (Cb should match actual block coefficient, etc.)

---

### 25. Frontend Tests Need Updating

**Module**: Frontend  
**Effort**: 4-6 hours

**Problem**: Old paths in some tests, many components untested

**Fix**: Update tests, add missing coverage

---

### 26. Bundle Size Optimization

**Module**: Frontend  
**Effort**: 2-3 days

**Problem**: 4.1 MB uncompressed (903 KB gzipped)

**Fix**: Lazy load routes, optimize imports, tree-shake Recharts

---

### 27. No Error Boundaries

**Module**: Frontend  
**Effort**: 2-3 hours

**Problem**: Uncaught errors crash entire app

**Fix**: Add React error boundaries at key levels

---

### 28. Limited Hull Form Coverage

**Module**: Resistance  
**Effort**: 1-2 weeks

**Problem**: Poor accuracy for high-speed craft, planing hulls, multihulls

**Fix**: Add method selection by hull type (Savitsky for planing, etc.)

---

### 29. No Appendage Details

**Module**: Resistance  
**Effort**: 1 week

**Problem**: Simplified appendage resistance, doesn't account for specific types

**Fix**: Detailed appendage resistance model

---

### 30. Missing XML Documentation (IdentityService)

**Module**: Infrastructure  
**Effort**: 5 minutes

**Problem**: Swagger docs incomplete for IdentityService

**Fix**: Add to `IdentityService.csproj`:
```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<NoWarn>$(NoWarn);1591</NoWarn>
```

---

### 31. No Backup/Restore Testing

**Module**: Infrastructure  
**Effort**: 2-3 hours

**Problem**: RDS backups enabled but never tested

**Fix**: Create and document restore procedure

---

### 32. No Monitoring/Alerting

**Module**: Infrastructure  
**Effort**: 4-6 hours

**Problem**: Can't detect issues proactively

**Fix**: Add CloudWatch alarms for errors, latency, resource usage

---

### 33. Froude Number Precision

**Module**: Hull Sizing  
**Effort**: 2 hours

**Problem**: Calculated Fn differs from expected by ~0.05

**Fix**: Tests should use solver's Lwl, not assume Lwl = Lpp

---

### 34. No Wetted Surface Validation

**Module**: Resistance  
**Effort**: 2 hours

**Problem**: Can return negative with extreme Cb/Cm values

**Fix**: Validate S > 0, use Moor's approximation as fallback

---

### 35. Inconsistent Spacing

**Module**: Frontend  
**Effort**: 3-4 hours

**Problem**: Some components use different spacing

**Fix**: Audit and standardize with Tailwind spacing scale

---

## 🟢 Low Priority

### 36. Temperature/Salinity Effects

**Module**: Resistance  
**Effort**: 4-6 hours

**Problem**: Uses standard water properties, doesn't adjust for actual conditions

**Fix**: Integrate water properties from catalog

---

### 37. No Roughness Allowance

**Module**: Resistance  
**Effort**: 2 hours

**Problem**: Assumes smooth hull, real vessels have fouling

**Fix**: Add roughness factor input

---

### 38. Limited Search/Filter

**Module**: Catalog  
**Effort**: 2-3 days

**Problem**: Only basic filtering, no search by dimensions/Cb

**Fix**: Add advanced search

---

### 39. No Version Control for Catalog Data

**Module**: Catalog  
**Effort**: 1-2 days

**Problem**: Updates overwrite previous data, no history

**Fix**: Add version/audit table

---

### 40. Limited Keyboard Navigation

**Module**: Frontend  
**Effort**: 2-3 days

**Problem**: Not all modals/forms keyboard accessible

**Fix**: Add keyboard event handlers, focus management

---

### 41. No Cost Monitoring

**Module**: Infrastructure  
**Effort**: 1 hour

**Problem**: No alerts for unexpected AWS charges

**Fix**: Add AWS Budgets

---

### 42. No Disaster Recovery Plan

**Module**: Infrastructure  
**Effort**: 4 hours

**Problem**: No documented DR procedure

**Fix**: Create DR runbook

---

### 43. Unit Conversion Path Inconsistency

**Module**: Backend  
**Effort**: 30 minutes

**Problem**: IdentityService uses root path, DataService uses `config/`

**Fix**: Standardize on `config/unit-systems.xml`

---

### 44. No Keyboard Shortcuts Expansion

**Module**: Frontend  
**Effort**: 1 hour

**Problem**: Only basic shortcuts (1-4, Q, E)

**Fix**: Add Ctrl+S, Ctrl+D, Ctrl+F, ?, Ctrl+K

---

### 45. No Command Palette

**Module**: Frontend  
**Effort**: 2-3 days

**Problem**: No quick access to features

**Fix**: Implement command palette (Ctrl+K) with cmdk library

---

## 📊 Debt Reduction Plan

### Month 1 (November)
**Focus**: Critical issues blocking production

| Week | Tasks | Hours |
|------|-------|-------|
| 1 | Fix solver (1-8), AWS SDK, CI workflow | 15 |
| 2 | Security Week 2-3, add constraints | 8 |
| 3 | Full Holtrop, form factor, integration tests start | 15 |
| 4 | Complete integration tests, add catalog tests | 12 |

**Total**: 50 hours

---

### Month 2 (December)
**Focus**: Testing and stability

| Week | Tasks | Hours |
|------|-------|-------|
| 1 | E2E tests with Playwright | 20 |
| 2 | Frontend component tests | 20 |
| 3 | Propeller integration, monitoring | 10 |
| 4 | Bundle optimization, error boundaries | 10 |

**Total**: 60 hours

---

### Month 3 (January)
**Focus**: Polish and performance

| Week | Tasks | Hours |
|------|-------|-------|
| 1 | Geometry implementation, catalog improvements | 15 |
| 2 | Frontend polish (loading, wizard, keyboard) | 8 |
| 3 | Backup testing, DR plan, documentation | 8 |
| 4 | Low priority cleanup | 8 |

**Total**: 39 hours

---

**Grand Total**: ~150 hours over 3 months

---

## 🎯 Quick Wins (High Impact, Low Effort)

See `10-QUICK-WINS.md` for detailed list of 12 items totaling ~10 hours that resolve 3 critical issues.

---

## 📚 Related Documentation

### Critical Reading
- `.plan/hull-sizing/SOLVER-IMPROVEMENTS-NEEDED.md` - Solver fixes
- `.plan/phase11-security.md` - Security hardening
- `temp/WORKFLOW-SKIP-ISSUE.md` - CI/CD fix
- `temp/HYDROSTATICS_COMPLETION_STATUS.md` - Test status

### Reference
- `08-TESTING-DEBT.md` - Test coverage details
- `10-QUICK-WINS.md` - Low-effort improvements
- `.plan/hull-sizing/plan/11-FUTURE-IMPROVEMENTS.md` - Future plans

---

**Last Updated**: November 4, 2025  
**Owner**: Technical Leadership  
**Review Frequency**: Biweekly, update priorities as needed
