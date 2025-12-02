# Bow/Stern Shape Consistency - Root Cause Analysis

**Date**: December 2, 2025  
**Issue**: User-selected bow/stern families not reflected in hull geometry  
**Severity**: CRITICAL (Long-standing issue, significant time invested)  
**Status**: ROOT CAUSE IDENTIFIED

---

## EXECUTIVE SUMMARY

**Root Cause**: Wrong geometry generator is being used as primary.

**Current Architecture** (BROKEN):
```
Priority 1: Form-Coefficient Generator (can't do family-specific shapes)
Priority 2: ShipD Generator (has proper shapes, only runs as fallback)
```

**Required Architecture** (FIX):
```
Priority 1: ShipD Generator (has proper family-specific shapes)
Priority 2: Form-Coefficient Generator (fallback for edge cases)
```

**Impact**: When user selects "bulbous_bow + transom_stern", they get generic tapered hull instead of actual bulbous bow protrusion and flat transom surface.

---

## USER'S OBSERVATION

> "I want the shapes in the designs and 3d isometric reflect the bow and stern selections made by the user and respect the proportions selected. A long running issue and a lot of time spent on this with various iterations but still i feel something amiss here"

**User is 100% CORRECT**. Analysis confirms:
- Issue affects ALL views (plan, profile, sections, 3D)
- Issue affects ALL bow/stern combinations
- Issue affects First-Principles solver primarily
- Previous iterations fixed artifacts but NOT root cause

---

## TECHNICAL ROOT CAUSE

### File: `backend/HullSizingService/Services/SizingRunService.cs`

**Lines 521-710**: Geometry generation priority order

**Current Flow**:
```csharp
// Priority 1: Form-coefficient-based OffsetsGrid (line 521)
var offsetsGrid = await _hullGeometryGenerator.GenerateOffsetsFromCandidateAsync(
    sc,
    vesselType: vesselType,
    numStations: 60,
    bowFamily: bowFamily,      // ✅ Passed but...
    midshipFamily: midshipFamily,
    sternFamily: sternFamily,
    cancellationToken);

// Priority 2: ShipD geometry as fallback (line 690)
// ONLY runs if Priority 1 fails!
var sections = await _shipdGeometryService.GenerateSectionsAsync(
    candidateShipdVector,
    sc.LppM,
    sc.BeamM,
    sc.DraftM,
    shipdMetadata,
    stationCount: 60,
    cancellationToken);
```

### Why Form-Coefficient Generator Can't Do Family-Specific Shapes

**File**: `backend/Shared/HullGenerators/FormCoefficientHullGenerator.cs`

**Line 370-381**: Bow family handling
```csharp
private decimal GetBowFamilyMultiplier(string? bowFamily)
{
    return bowFamily.ToLowerInvariant() switch
    {
        "bulbous_bow" => 0.5m,   // ❌ Just a multiplier!
        "axe_bow" => 1.8m,       // ❌ Just makes taper steeper!
        "fine_entry" => 1.5m,    // ❌ Just a number!
        _ => 1.0m
    };
}
```

This multiplier is applied to bow taper exponent (line 472):
```csharp
decimal bowExponent = baseBowExponent * bowFamilyMultiplier * vesselTypeMultiplier;
```

**What this does**: Makes waterline taper faster/slower  
**What this does NOT do**: Create actual bulbous bow protrusion  
**What this does NOT do**: Create actual transom stern flat surface

### Why ShipD Generator CAN Do Family-Specific Shapes

**File**: `backend/HullSizingService/Services/Geometry/ShipDHullGeometryService.cs`

**Lines 814-883**: `GenerateBulbOffsets()` - Creates actual bulb geometry  
**Lines 680-793**: Transom stern logic - Creates actual flat transom  
**Lines 460-543**: Bow region - Uses Beta, Rc, Rk for actual bow shapes

**Example - Transom Stern** (line 685-757):
```csharp
var isTransomStern = atransNorm > 0.5m; // Detects transom from parameter
if (isTransomStern)
{
    var atrans = denormalized[22];
    // ... applies transom width, rake angle, actual flat surface
    var transomWidth = beamM * bcTrans;  // ✅ Actual transom geometry
}
```

**Example - Bulbous Bow** (line 814):
```csharp
private Dictionary<decimal, decimal> GenerateBulbOffsets(...)
{
    // Generates actual ellipsoidal bulb geometry
    // Uses Lbb, Hbb, Bbb, Lbbm, Rbb parameters
    // Creates protrusion forward of FP
    // ✅ REAL bulbous bow
}
```

---

## PROOF: Why Previous Fixes Didn't Work

### Previous Work (From temp/ Analysis)

**Fix Attempts Found**:
1. `temp/SHIPD-BOW-STERN-CLOSURE-FIX.md` - Added bow/stern closure corrections
2. `temp/SHIPD-GEOMETRY-INVESTIGATION-FINDINGS.md` - Fixed interpolation issues
3. `temp/GEOMETRY-ISSUES-ANALYSIS.md` - Fixed zig-zag patterns
4. `temp/hull-geometry-inversion-fix-complete.md` - Fixed coordinate inversions

**Why They Helped But Didn't Solve It**:
- These fixed **artifacts** (wide stern, blunt bow, zig-zags)
- But didn't fix **root cause** (wrong generator being used)
- Form-coefficient generator fundamentally cannot create family-specific shapes
- Only ShipD generator can, but it's relegated to fallback

**User's Observation is Correct**: "something amiss" - The architecture is backwards!

---

## VALIDATION FROM prefinal_1 DOCUMENT

**File**: `temp/prefinal_1_key_requirements.md`

**40,000 DWT Product Carrier Requirements**:
- **Bow**: "Bulbous (for efficiency at 14 knots)" - REQUIRES visible bulb protrusion
- **Stern**: "Transom" - REQUIRES flat transom surface (not tapered point)
- **Midship**: "Full form, nearly rectangular section (Cm=0.99)"

**Current form-coefficient generator**:
- ❌ Cannot create bulb protrusion
- ❌ Cannot create flat transom surface
- ❌ Can only adjust taper rate with multipliers

**ShipD generator**:
- ✅ Has GenerateBulbOffsets() for actual bulb
- ✅ Has transom logic for flat stern
- ✅ But only runs as fallback!

---

## ADDITIONAL ISSUES DISCOVERED

### Issue A: Minimal Family Defaults

**File**: `backend/DataService/Data/ShipD/shipd_vessel_taxonomy_seed.json` lines 88-102

**Current Defaults**:
```json
"familyDefaults": {
  "bulbous_bow": {
    "bit_BB": 1  // ❌ ONLY flag, no bulb dimensions!
  },
  "transom_stern": {
    "Atrans": 0.5  // ❌ ONLY area ratio, no rake/width!
  },
  "twin_skeg": {
    "bit_SB": 1  // ❌ Flag only, skeg NOT implemented!
  }
}
```

**When user leaves families blank**: Taxonomy defaults apply family name, but only 1-2 parameters get set. Other shape parameters remain at 0.

**Required**: Complete parameter sets for each family (8-10 parameters per family).

### Issue B: Twin Skeg Not Implemented

**From**: `temp/ShipD_Implementation_Gap_Analysis.md`

**Status**: ❌ NOT IMPLEMENTED
- Parameters exist: SK_z, Lsb, Hsb, Bsb, Kappa_SB
- Taxonomy defines twin_skeg family
- **No geometry generation code**
- Impact: Container ships, LNG carriers show wrong stern

**Required**: `GenerateSkegOffsets()` method similar to bulb.

---

## WHY THIS IS A LONG-RUNNING ISSUE

1. **Architecture Problem Not Recognized**: Previous fixes assumed form-coefficient generator could be enhanced
2. **Wrong Generator Used**: Form-coefficient is fundamentally limited to taper adjustments
3. **ShipD Generator Underutilized**: Has all the right logic but relegated to fallback
4. **Minimal Defaults**: Even when ShipD runs, family defaults don't provide enough parameters

**Multiple Iterations Tried**:
- Bow/stern closure fixes (addressed artifacts)
- Interpolation fixes (improved smoothness)
- Coordinate system fixes (fixed inversions)
- Parameter enforcement fixes (improved curvature)

**None Addressed Root Cause**: Wrong generator priority order.

---

## THE SOLUTION

### Core Fix: Reverse Generator Priority

**Make ShipD Generator PRIMARY** for all First-Principles candidates.

**Rationale**:
1. ShipD generator has actual family-specific geometry
2. MIT ShipD is industry-standard parameterization
3. All the work on ShipD implementation is underutilized
4. Form-coefficient generator can stay as fallback for edge cases

### Supporting Fixes

1. **Enhance family defaults** - Add complete parameter sets
2. **Implement twin skeg** - GenerateSkegOffsets() method
3. **Add logging** - Debug family parameter flow
4. **Verify cruiser/canoe stern** - Test non-transom sterns

---

## IMPACT ASSESSMENT

**Once Fixed**:
- ✅ User selects bulbous_bow → sees actual bulb protrusion
- ✅ User selects transom_stern → sees flat transom surface
- ✅ User selects axe_bow → sees sharp bow entry
- ✅ User selects twin_skeg → sees skeg appendages (after skeg implementation)
- ✅ All 4 views (plan, profile, sections, 3D) show correct family shapes
- ✅ Proportions (Lb, Lm, Ls) respected in longitudinal scaling
- ✅ prefinal_1 validation case shows correct product carrier geometry

**User Confidence**: Will dramatically improve - selections will actually matter!

---

## FILES REQUIRING CHANGES

### Critical:
1. `backend/HullSizingService/Services/SizingRunService.cs` - Reverse priority (lines 520-710)
2. `backend/DataService/Data/ShipD/shipd_vessel_taxonomy_seed.json` - Enhanced defaults
3. `backend/HullSizingService/Services/Geometry/ShipDHullGeometryService.cs` - Add skeg method

### Supporting:
4. Create migration for taxonomy update
5. Add logging throughout geometry generation pipeline
6. Update tests to verify family shapes

---

## NEXT STEPS

See [`BOW-STERN-SHAPE-FIX-PLAN.md`](.plan/active/hull-sizing/BOW-STERN-SHAPE-FIX-PLAN.md) for implementation plan.

---

**Status**: ✅ ROOT CAUSE CONFIRMED  
**Confidence**: HIGH  
**Solution**: CLEAR AND ACTIONABLE

