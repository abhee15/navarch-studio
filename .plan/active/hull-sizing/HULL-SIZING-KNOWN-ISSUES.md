# Hull Sizing - Known Issues & Status

**Last Updated**: December 2, 2025  
**Status**: P0 issues RESOLVED, P1-P2 issues documented for future work

---

## RESOLVED ISSUES (P0 - Fixed)

### ✅ Issue 1: Bow/Stern Family Shapes Not Reflected in Geometry (CRITICAL - FIXED)

**Status**: ✅ **RESOLVED**  
**Date Fixed**: December 2, 2025  
**Root Cause**: Wrong geometry generator was being used as primary

**Problem**:
- User selected "bulbous_bow + transom_stern" but geometry showed generic tapered hull
- Form-coefficient generator (Priority 1) could only adjust taper rates, not create actual family shapes
- ShipD generator (Priority 2) had proper family shapes but only ran as fallback
- Affected all views: Plan, Profile, Sections, 3D Isometric

**Solution Implemented**:
1. **Reversed generator priority** in `SizingRunService.cs`:
   - ShipD generator is now PRIMARY (has actual family-specific shapes)
   - Form-coefficient is now FALLBACK (for edge cases)
2. **Enhanced family defaults** in taxonomy JSON:
   - Added complete parameter sets for all 14 families
   - bulbous_bow: 9 parameters (bit_BB, Lbb, Hbb, Bbb, Lbbm, Rbb, Beta, Rc, Rk)
   - transom_stern: 6 parameters (Atrans, Beta_trans, Bc_trans, Rc_trans, Rk_trans, Kappa_stern)
   - All bow/midship/stern families now have comprehensive defaults
3. **Created migrations**:
   - DataService: `UpdateShipDFamilyDefaultsComplete`
   - HullSizingService: `AddValidationResultsJson`

**Verification**:
- ✅ Generated 40,000t general cargo with bulbous_bow + transom_stern
- ✅ ALL 5 candidates show "ShipD" geometry (not form-coefficient)
- ✅ ALL 5 candidates show "Bulb: Present"
- ✅ Workspace displays proper hull visualizations

**Files Changed**:
- `backend/HullSizingService/Services/SizingRunService.cs` (lines 516-655)
- `backend/DataService/Data/ShipD/shipd_vessel_taxonomy_seed.json` (all familyDefaults sections)
- New migrations created

---

## PENDING ISSUES (P1 - High Priority)

### Issue 2: maxCandidates Not Passed from UI

**Status**: ⏳ **IDENTIFIED, NOT YET FIXED**  
**Severity**: P1 - High (UX issue)  
**Impact**: User selects 2 candidates but gets 5

**Problem**:
- UI Step 4 has maxCandidates input (default: 5)
- User can change it to 1-10
- But value is hardcoded to 5 in `MissionWizard.tsx` line 386
- Backend receives 5 regardless of user selection

**Root Cause**:
```typescript
// MissionWizard.tsx line 386
options: {
  maxCandidates: 5,  // ❌ Hardcoded!
  additionalParameters: additionalParameters,
}
```

**Solution Required**:
1. Pass maxCandidates from Step4Options state to parent
2. Use actual value in MissionWizard (remove hardcoded 5)

**Estimated Effort**: 15 minutes

---

### Issue 3: Twin Skeg Geometry Not Implemented

**Status**: ⏳ **IDENTIFIED, NOT YET FIXED**  
**Severity**: P1 - High (Missing functionality)  
**Impact**: Container ships, LNG carriers with twin_skeg stern show incorrect geometry

**Problem**:
- Taxonomy defines twin_skeg family
- Parameters exist: SK_z, Lsb, Hsb, Bsb, Kappa_SB
- Family defaults now set bit_SB=1 and other params
- **BUT**: No geometry generation code for skegs
- ShipD generator has `GenerateBulbOffsets()` but no `GenerateSkegOffsets()`

**Solution Required**:
1. Add `GenerateSkegOffsets()` method in `ShipDHullGeometryService.cs`
2. Similar to bulb: check bit_SB > 0.5, generate ellipsoidal skeg geometry
3. Blend skeg with main hull stern geometry
4. Update 3D mesh generation to include skeg vertices

**Estimated Effort**: 3-4 hours

---

### Issue 4: No Vessel-Type-Specific Constraint Filtering

**Status**: ⏳ **IDENTIFIED, NOT YET FIXED**  
**Severity**: P1 - High (UX confusion)  
**Impact**: All canal presets shown for all vessel types (confusing)

**Problem**:
- UI shows Panamax, Suezmax, Malaccamax for ALL vessel types
- Fishing vessels, recreational boats don't need canal constraints
- Users see irrelevant options

**Solution Required**:
1. Create `vesselConstraintRules.ts` with applicability matrix
2. Filter canal preset buttons based on vessel type
3. Show disabled presets with tooltip explaining why

**Estimated Effort**: 2-3 hours

---

## PENDING ISSUES (P2 - Medium Priority)

### Issue 5: No Pre-Flight Constraint Validation

**Status**: ⏳ **IDENTIFIED, NOT YET FIXED**  
**Severity**: P2 - Medium (Poor UX)  
**Impact**: Solver tries generation even when constraints are impossible

**Problem**:
- User applies Panamax to 5000 TEU container
- Solver attempts all 5 variants
- All fail due to beam/draft constraints
- Returns 0 candidates with generic error
- User doesn't know which constraint caused issue

**Solution Required**:
1. Create `ConstraintFeasibilityValidator.cs`
2. Before generating variants, estimate required dimensions
3. Compare against constraints
4. Fail fast with specific guidance: "Panamax beam (32.3m) too restrictive - need ~42m for 5000 TEU"

**Estimated Effort**: 2-3 hours

---

### Issue 6: Missing LOA Constraint in UI

**Status**: ⏳ **IDENTIFIED, NOT YET FIXED**  
**Severity**: P2 - Medium (Incomplete feature)  
**Impact**: Backend supports MaxLoaM but UI doesn't expose it

**Problem**:
- Canal presets set maxLengthOverall in state but don't persist to mission case
- Backend `CapLoaM` exists but unused
- Users can't set LOA constraints manually

**Solution Required**:
- Add Max LOA input field in Step3Constraints
- Wire it to mission case CapLoaM property

**Estimated Effort**: 1 hour

---

### Issue 7: Cruiser/Canoe Stern Verification Needed

**Status**: ⏳ **NEEDS VERIFICATION**  
**Severity**: P2 - Medium (Uncertainty)  
**Impact**: Non-transom sterns might not render correctly

**Problem**:
- Cruiser stern uses Adel_stern, Bdel_stern parameters
- Canoe stern uses same parameters with different values
- Implementation exists but not verified against ShipD reference

**Solution Required**:
1. Test cruise_vessel + cruiser_stern
2. Test fishing_vessel + canoe_stern
3. Compare with ShipD repository images
4. Adjust parameters if shapes don't match

**Estimated Effort**: 2 hours

---

## BACKLOG ISSUES (P3 - Nice to Have)

### Issue 8: Solver Options Not Persisted

**Status**: ⏳ **IDENTIFIED, NOT YET FIXED**  
**Severity**: P3 - Low (Convenience feature)  
**Impact**: Users must re-enter preferences each time

**Solution**: Save solver options to localStorage, pre-populate wizard

**Estimated Effort**: 1 hour

---

### Issue 9: No Results Comparison View

**Status**: ⏳ **FEATURE REQUEST**  
**Severity**: P3 - Low (Enhancement)  
**Impact**: Hard to compare candidates side-by-side

**Solution**: Create comparison table showing differences and tradeoffs

**Estimated Effort**: 6-8 hours

---

## TESTING STATUS

### P0 Fixes Verified:
- ✅ ShipD generator used as primary
- ✅ Bulbous bow family applied correctly
- ✅ Transom stern family recognized  
- ✅ 5 candidates generated successfully (399ms)
- ✅ All candidates show "ShipD" geometry
- ✅ All candidates show "Bulb: Present"

### Remaining Testing:
- ⏳ Visual verification of bulb protrusion in plan view
- ⏳ Visual verification of transom flatness in profile view
- ⏳ Test with axe_bow family
- ⏳ Test with cruiser_stern, canoe_stern
- ⏳ Test default family selection (user leaves blank)

---

## WORKAROUNDS (Until Fixes Applied)

### For Issue 2 (maxCandidates):
**Workaround**: Accept that you'll get 5 candidates even if you select 2. Delete extras after generation.

### For Issue 3 (twin_skeg):
**Workaround**: Use transom_stern instead for container ships until twin_skeg is implemented.

### For Issue 4 (constraint filtering):
**Workaround**: Ignore irrelevant canal presets. Only select constraints that apply to your vessel type.

### For Issue 5 (pre-flight validation):
**Workaround**: Start with unconstrained designs, then add constraints incrementally. If 0 candidates, relax constraints.

---

## PRIORITY SUMMARY

**COMPLETED (P0)**:
- ✅ Bow/stern family shapes (CRITICAL long-standing issue)

**TODO (P1 - Next Sprint)**:
1. Fix maxCandidates passing (15 min)
2. Implement twin skeg geometry (3-4 hrs)
3. Vessel-type constraint filtering (2-3 hrs)

**TODO (P2 - Future)**:
4. Pre-flight constraint validation (2-3 hrs)
5. Add LOA constraint UI (1 hr)
6. Verify cruiser/canoe stern (2 hrs)

**TODO (P3 - Backlog)**:
7. Solver options persistence (1 hr)
8. Results comparison view (6-8 hrs)

---

**Next Steps**: Move to P1 fixes (maxCandidates, twin skeg, constraint filtering)
