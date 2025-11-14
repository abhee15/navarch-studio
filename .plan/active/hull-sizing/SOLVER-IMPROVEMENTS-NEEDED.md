# Hull Sizing Solver - Improvements Needed (Found via Testing)

## Issues Discovered During Unit Testing

### 🔴 CRITICAL - Must Fix Before Deploy

**1. Displacement Closure Not Converging**
- **Issue:** Solver fails to converge even for known solutions (KCS, KVLCC2, Barge)
- **Root Cause:** Initial Lpp guess is poor, Newton loop adjustment factors too conservative
- **Fix Needed:**
  - Improve initial guess: Use cube root scaling instead of fixed factor
  - Increase adjustment factors from 0.5 to 0.7-0.8
  - Add adaptive step sizing (larger steps when error is large)
  - Consider alternative: secant method instead of fixed-point iteration
  
**2. Beam Constraint Not Respected**
- **Issue:** Result beam (18.5m) exceeds maxBeam constraint (15m)
- **Root Cause:** Constraint check happens AFTER adjustment, but final value isn't clamped
- **Fix Needed:**
  - Clamp beam/draft BEFORE calculating next iteration
  - Verify clamping in every iteration loop
  - Add assertion at end: `Debug.Assert(beam <= maxBeam ?? decimal.MaxValue)`

**3. Stability Screen Decimal Overflow**
- **Issue:** `System.OverflowException` when converting Math.Sqrt result to decimal
- **Root Cause:** GMt is negative or very small → sqrt(negative) or sqrt(near-zero) → overflow
- **Fix Needed:**
  - Guard against negative GM: `if (gmt <= 0) return StabilityResult with flags`
  - Clamp roll period calculation: `Math.Max(0.01, gmt)` before sqrt
  - Add validation: KB + BMt should be > KG

**4. Roll Period Out of Range**
- **Issue:** T_roll = 50.8s (expected 5-30s)
- **Root Cause:** Formula `T_roll = 2 * B / sqrt(GMt)` may be wrong or GM is too small
- **Fix Needed:**
  - Review formula: Standard is `T = 2π * sqrt(kxx² / (g * GMt))`
  - Use proper radius of gyration: `kxx ≈ 0.35 * B` for ships
  - Formula should be: `T_roll = 2 * π * 0.35 * B / sqrt(g * GMt)`

---

### 🟡 MODERATE - Improve Algorithm Accuracy

**5. Froude Number Precision**
- **Issue:** Calculated Fn (0.172) differs from expected (0.12) by 0.05
- **Root Cause:** Lwl estimate in tests doesn't match what solver calculates
- **Fix Needed:**
  - Tests should use solver's calculated Lwl, not assume Lwl = Lpp
  - Or: solver should accept Lwl as input/target instead of deriving from Fn

**6. Convergence Rate**
- **Issue:** Some cases hit max iterations (50) without converging
- **Root Cause:** Linear adjustment doesn't work well for large errors
- **Fix Needed:**
  - Implement damped Newton-Raphson with line search
  - Use Jacobian matrix for multi-dimensional convergence
  - Add early exit if error increases for 5 consecutive iterations

---

### 🟢 ENHANCEMENTS - Phase 3 Custom Algorithm

**7. Replace Simplified Holtrop with Full Formula**
- Current: Simplified polynomial wave resistance
- Needed: Full 20-term Holtrop-Mennen (from SPS SName paper 2014)
- Impact: ±20-30% accuracy improvement

**8. Add Wetted Surface Validation**
- Current: Uses simplified Holtrop S formula
- Needed: Fallback if S < 0 (happens with extreme Cb/Cm values)
- Add: Moor's approximation as backup

**9. Form Factor Improvements**
- Current: Single formula for all hull types
- Needed: Different formulas for slender (Cb<0.65) vs full (Cb>0.75) hulls
- Reference: Holtrop 1984 updated formulas

---

## Recommended Fix Priority

### Sprint 1 (This Week):
1. Fix displacement closure convergence (CRITICAL)
2. Fix beam/draft constraint enforcement (CRITICAL)
3. Fix stability overflow (CRITICAL)
4. Fix roll period formula (CRITICAL)

### Sprint 2 (Next Week):
5. Improve Froude number handling (tests + solver)
6. Improve convergence rate (damping, Jacobian)
7. Add comprehensive logging for debugging

### Phase 3 (Custom Algorithm):
8. Full Holtrop-Mennen implementation
9. Wetted surface validation
10. Form factor by hull type
11. Appendage resistance
12. Custom neural network-based resistance (SPS SName approach)

---

## Test Results Summary

**Total:** 39 tests
**Passed:** 14 (36%)
**Failed:** 25 (64%)

**By Component:**
- ✅ Resistance Service: 7/11 passed (64%) - Basic logic works, need accuracy
- ⚠️  Stability Service: 4/10 passed (40%) - Overflow bugs, formula issues
- ❌ Displacement Closure: 2/9 passed (22%) - Convergence algorithm needs work
- ❌ Full Solver: 1/9 passed (11%) - Blocked by closure failures

**Performance (Passed Tests):**
- ✅ Closure: <100ms ✓
- ✅ Resistance: <50ms ✓
- ✅ Stability: <10ms ✓
- ✅ Full solver: <2s ✓

---

## Action Plan

1. **Fix critical bugs** (items 1-4 above)
2. **Re-run tests** → target 90%+ pass rate
3. **Commit fixes** with "fix: improve solver convergence and stability"
4. **Deploy to dev** and test with real API calls
5. **Document learnings** in this file
6. **Schedule Phase 3** custom algorithm work (reference SPS SName paper)

---

## Notes for Future Developers

- The solver is intentionally simplified for MVP
- All TODO: CUSTOM_ALGO comments mark areas for improvement
- SPS SName paper 2014 has the full Holtrop implementation
- Test-driven development revealed these issues BEFORE production!
- Keep this file updated as we improve the algorithm
















