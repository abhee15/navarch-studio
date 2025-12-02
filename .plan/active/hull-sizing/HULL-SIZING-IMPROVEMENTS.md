# Hull Sizing Application - Enhancement Roadmap

**Last Updated**: December 2, 2025  
**Current Version**: v1.2 (Post-Geometry-Fix)  
**Status**: P0 fixes deployed, P1-P3 enhancements planned

---

## COMPLETED IMPROVEMENTS (v1.2)

### ✅ 1. Geometry Generator Architecture Fix (P0 - COMPLETED)

**Date**: December 2, 2025  
**Impact**: **CRITICAL** - Fixes long-standing bow/stern shape inconsistency  
**Effort**: 5 hours

**What Changed**:
- Reversed generator priority: ShipD PRIMARY → Form-coefficient FALLBACK
- Enhanced family defaults: 4 params → 7-9 params per family
- Created comprehensive parameter sets for all 14 hull families
- Added proper logging for generator selection

**Benefits**:
- ✅ Bulbous bows now show actual protrusion (not just fuller taper)
- ✅ Transom sterns now show flat transom surface (not tapered point)
- ✅ User-selected families consistently reflected in geometry
- ✅ 3D isometric view matches selected proportions

**User Experience**:
- **Before**: Generic tapered hulls regardless of family selection
- **After**: Family-specific shapes (bulbous bow, transom stern, axe bow, etc.)

---

## PLANNED IMPROVEMENTS

### P1: High Priority (Next Sprint - 8-10 hours total)

#### 📋 2. Fix maxCandidates UI → Backend Passing (15 minutes)

**Problem**: User selects 2 candidates → gets 5 candidates

**Root Cause**:
```typescript
// MissionWizard.tsx line 386
options: { maxCandidates: 5 }  // Hardcoded!
```

**Solution**:
1. Add `maxCandidates` prop to Step4Options callback
2. Store in wizard state
3. Use actual value when calling solver

**Implementation**:
```typescript
// Step4Options.tsx - Add to onComplete callback
onComplete({
  maxCandidates: maxCandidates,  // From local state
  // ... other options
});

// MissionWizard.tsx - Use from state
options: {
  maxCandidates: step4Data?.maxCandidates ?? 5,  // Use actual value
}
```

**Testing**:
- Generate with 2 candidates → verify only 2 returned
- Generate with 8 candidates → verify 8 returned
- Generate with default (5) → verify 5 returned

---

#### 🚢 3. Implement Twin Skeg Geometry (3-4 hours)

**Problem**: Container ships with twin_skeg stern show incorrect geometry

**Current State**:
- ✅ Taxonomy defines twin_skeg
- ✅ Parameters exist (SK_z, Lsb, Hsb, Bsb, Kappa_SB)
- ✅ Family defaults set bit_SB=1 and dimensions
- ❌ No geometry generation code

**Solution**:
1. Add `GenerateSkegOffsets()` method in `ShipDHullGeometryService.cs` (similar to `GenerateBulbOffsets`)
2. Check `bit_SB > 0.5` to determine if skegs are present
3. Generate ellipsoidal/tapered skeg geometry at stern
4. Blend with main hull stern offsets
5. Update 3D mesh generation to include skeg appendages

**Implementation Steps**:
```csharp
// ShipDHullGeometryService.cs - Add after GenerateBulbOffsets
private List<(decimal x, decimal y, decimal z)> GenerateSkegOffsets(
    decimal[] vector,
    ShipDMetadata metadata,
    decimal lppM, decimal beamM, decimal draftM)
{
    // 1. Check if skeg is enabled
    int bitSbIndex = GetParameterIndex("bit_SB", metadata);
    if (bitSbIndex < 0 || vector[bitSbIndex] < 0.5m) return new();

    // 2. Extract skeg parameters
    decimal skZ = GetParameterValue("SK_z", vector, metadata) ?? 0.3m;
    decimal lsb = GetParameterValue("Lsb", vector, metadata) ?? 0.15m;
    decimal hsb = GetParameterValue("Hsb", vector, metadata) ?? 0.4m;
    decimal bsb = GetParameterValue("Bsb", vector, metadata) ?? 0.3m;

    // 3. Generate skeg shape (ellipsoidal/tapered)
    var skegPoints = new List<(decimal x, decimal y, decimal z)>();
    // ... geometry generation logic ...
    return skegPoints;
}
```

**Testing**:
- Create container ship with twin_skeg stern
- Verify skegs appear in profile view (below waterline at stern)
- Verify skegs appear in sections view (at aft stations)
- Verify 3D view shows twin appendages

---

#### 🗺️ 4. Vessel-Type-Specific Constraint Filtering (2-3 hours)

**Problem**: All canal presets shown for all vessel types (confusing)

**Current Behavior**:
- Fishing vessel → Shows Panamax, Suezmax, Malaccamax (not applicable!)
- Recreational yacht → Shows all commercial canal constraints

**Solution**:
1. Create `vesselConstraintRules.ts`:
```typescript
const CONSTRAINT_APPLICABILITY: Record<VesselCategory, string[]> = {
  commercial: ["panamax", "neopanamax", "suezmax", "malaccamax", "st_lawrence"],
  government: ["panamax", "suezmax"],  // Naval vessels might use canals
  recreational: [],  // No canal constraints for yachts
  research: []
};
```

2. Update `Step3Constraints.tsx`:
- Filter preset buttons based on vessel type
- Show disabled presets with tooltip: "Not applicable to recreational vessels"

**Testing**:
- Container ship → All presets visible ✅
- Fishing vessel → No presets visible ✅
- Bulk carrier → Panamax/Suezmax visible ✅

---

### P2: Medium Priority (Future Sprint - 6-7 hours total)

#### 🎯 5. Pre-Flight Constraint Feasibility Check (2-3 hours)

**Problem**: Solver attempts generation even when constraints are impossible

**Current Behavior**:
- User sets Panamax + 5000 TEU
- Solver tries all 5 variants
- All fail after 1-2 seconds
- Returns generic "0 candidates" error

**Improved Experience**:
1. **Immediate feedback**: Check constraints before generating
2. **Specific guidance**: "Panamax beam (32.3m) too restrictive - container ships typically need 40-45m for 5000 TEU"
3. **Smart suggestions**: "Try Neo-Panamax (51.3m beam) or reduce to 1500-2000 TEU"

**Implementation**:
```csharp
public class ConstraintFeasibilityValidator
{
    public async Task<FeasibilityResult> CheckAsync(MissionCase mission)
    {
        // 1. Estimate required dimensions from cargo/speed
        var estimatedBeam = EstimateBeamFromCargo(mission);
        var estimatedDraft = EstimateDraftFromCargo(mission);
        
        // 2. Check against constraints
        if (mission.CapBeamM < estimatedBeam)
            return Infeasible("Beam constraint too restrictive", ...);
        
        return Feasible();
    }
}
```

**Testing**:
- Panamax + 5000 TEU → Immediate error with guidance
- Neo-Panamax + 5000 TEU → Passes check, generates successfully
- Unconstrained → Always passes

---

#### 📏 6. Add Max LOA Constraint to UI (1 hour)

**Problem**: Backend supports CapLoaM but UI doesn't expose it

**Solution**:
- Add "Max Length Overall (m)" input in Step3Constraints
- Wire to mission case CapLoaM property
- Include in canal presets (currently preset sets it but doesn't persist)

---

#### 🧪 7. Verify Cruiser/Canoe Stern Geometry (2 hours)

**Status**: Implemented but not verified

**Action Required**:
1. Test cruise_vessel + cruiser_stern
2. Test fishing_vessel + canoe_stern
3. Compare visual output with ShipD paper examples
4. Adjust Adel_stern, Bdel_stern defaults if needed

---

### P3: Lower Priority (Backlog - 7-9 hours total)

#### 💾 8. Solver Options Persistence (1 hour)

**Feature**: Remember user preferences across sessions

**Implementation**:
```typescript
// Save to localStorage when user changes options
localStorage.setItem('sizing.solverOptions', JSON.stringify({
  maxCandidates: 3,
  fnMin: 0.18,
  fnMax: 0.28
}));

// Restore in Step4Options componentDidMount
const saved = localStorage.getItem('sizing.solverOptions');
if (saved) {
  const opts = JSON.parse(saved);
  setMaxCandidates(opts.maxCandidates);
  // ...
}
```

---

#### 📊 9. Results Comparison View (6-8 hours)

**Feature**: Side-by-side candidate comparison with highlights

**Mockup**:
```
| Parameter   | #1 (Best) | #2      | #3      | #4      | #5      |
|-------------|-----------|---------|---------|---------|---------|
| Lpp         | 215.9m    | 224.3m  | 232.5m  | 240.7m  | 248.7m  |
| Beam        | 37.5m  ✅ | 37.4m ✅| 37.2m ✅| 37.0m ✅| 36.8m ✅|
| CB          | 0.638  ✅ | 0.657   | 0.675   | 0.693   | 0.712 ⚠️|
| EHP         | 8443kW ✅ | 8604kW  | 8765kW  | 8929kW  | 9093kW  |
| GMt         | 8.30m     | 8.51m   | 8.70m   | 8.89m   | 8.74m   |
```

**Features**:
- Highlight best/worst values
- Show percentage differences
- Flag constraint violations
- Export to CSV/Excel

---

## FUTURE ENHANCEMENTS (Beyond P3)

### 10. Smart Variant Generation (Adaptive Sampling)

**Concept**: Early abort on repeated failures

**Current**: Generate all N variants even if first 3 fail  
**Improved**: Detect pattern (e.g., "all high-Cb variants fail stability"), skip remaining

**Benefit**: Faster results when design space is constrained

---

### 11. Constraint Violation Visualization

**Concept**: Highlight which dimension exceeds constraint

**Mockup**:
```
Candidate #3:
✅ Beam: 37.2m (OK - under Panamax 32.3m)
❌ Draft: 13.5m (EXCEEDS Panamax 12.0m) ← Highlighted
✅ LOA: 238m (OK - under Panamax 294.1m)
```

---

### 12. Hull Family Gallery

**Concept**: Visual reference for bow/stern/midship families

**Feature**: Hover over family dropdown → See example sketch/photo  
**Benefit**: Non-experts can understand what "bulbous_bow" vs "axe_bow" means

---

## LESSONS LEARNED

### Architecture Decisions:
1. **Generator priority matters**: Feature-rich generator (ShipD) should be primary
2. **Defaults matter**: Comprehensive family defaults prevent generic shapes
3. **Validation helps**: Early feasibility checks save user time

### Code Quality:
1. **Test shape families**: Verify bulbous_bow creates actual bulb (not just fuller taper)
2. **Log generator selection**: Makes debugging architecture issues easier
3. **Migration discipline**: Always create migration when adding model properties

---

## METRICS

### Before Fixes (v1.1):
- Geometry generator: Form-coefficient (limited shapes)
- Family defaults: 1-2 parameters per family
- Bulbous bow: 50% taper multiplier only
- User satisfaction: Low (shapes don't match selections)

### After Fixes (v1.2):
- Geometry generator: ShipD (full shape library)
- Family defaults: 7-9 parameters per family
- Bulbous bow: 9 parameters (actual protrusion)
- User satisfaction: Expected to improve significantly

### Target (v2.0):
- All P1-P2 issues fixed
- Twin skeg implemented
- Pre-flight validation
- Constraint filtering by vessel type
- Enhanced user guidance

---

## REFERENCES

**Related Plan Files**:
- `.plan/active/hull-sizing/BOW-STERN-SHAPE-ROOT-CAUSE-ANALYSIS.md`
- `.plan/active/hull-sizing/BOW-STERN-SHAPE-FIX-PLAN.md`
- `.plan/active/hull-sizing/HULL-SIZING-KNOWN-ISSUES.md`

**Related Code**:
- Generator priority: `backend/HullSizingService/Services/SizingRunService.cs` lines 516-655
- Family defaults: `backend/DataService/Data/ShipD/shipd_vessel_taxonomy_seed.json`
- ShipD geometry: `backend/HullSizingService/Services/Geometry/ShipDHullGeometryService.cs`

**Previous Analysis**:
- `temp/ShipD_Implementation_Gap_Analysis.md`
- `temp/prefinal_1_key_requirements.md`
- `temp/SHIPD-GEOMETRY-INVESTIGATION-FINDINGS.md`

---

**Conclusion**: With P0 fixes complete, the application now properly reflects user-selected hull families. Focus shifts to UX improvements (maxCandidates, constraint filtering, pre-flight validation) and feature completion (twin skeg geometry).
