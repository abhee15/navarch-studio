# Bow/Stern Shape Fix - Implementation Plan

**Date**: December 2, 2025  
**Owner**: Development Team  
**Severity**: P0 - CRITICAL  
**Estimated Total Effort**: 15 hours (P0: 5 hours, P1: 10 hours)

---

## OVERVIEW

Fix the long-standing issue where user-selected bow/stern families (bulbous_bow, transom_stern, axe_bow, twin_skeg) are not properly reflected in generated hull geometry.

**Root Cause**: Wrong generator used - form-coefficient (can't do family shapes) is primary, ShipD (has family shapes) is fallback.

**Solution**: Reverse priority - make ShipD generator primary.

---

## PHASE 1: CRITICAL FIXES (P0 - 5 hours)

### Task 1.1: Reverse Generator Priority (2 hours)

**File**: `backend/HullSizingService/Services/SizingRunService.cs`

**Current Code** (lines 520-710):
```csharp
// Priority 1: Form-coefficient OffsetsGrid
var offsetsGrid = await _hullGeometryGenerator.GenerateOffsetsFromCandidateAsync(...);
if (offsetsGrid != null) {
    geometryJson = JsonSerializer.Serialize(offsetsGrid);
}

// Priority 2: ShipD geometry fallback (ONLY if Priority 1 fails)
if (string.IsNullOrEmpty(geometryJson)) {
    var sections = await _shipdGeometryService.GenerateSectionsAsync(...);
}
```

**New Code**:
```csharp
// Priority 1: ShipD geometry (HAS proper family shapes)
if (candidateShipdVector != null && candidateShipdVector.Length == 45 && _shipdGeometryService != null)
{
    try
    {
        var sections = await _shipdGeometryService.GenerateSectionsAsync(
            candidateShipdVector,
            sc.LppM,
            sc.BeamM,
            sc.DraftM,
            shipdMetadata,
            stationCount: 60,
            cancellationToken);
            
        geometryJson = JsonSerializer.Serialize(sections, JsonOptions);
        geometryStatus = GeometryGenerationStatus.ShipD;
        _logger.LogInformation("[SIZING_RUN] ✅ Generated ShipD geometry (PRIMARY) for candidate {Rank}", i + 1);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "[SIZING_RUN] ShipD geometry generation failed for candidate {Rank}. Will fallback to form-coefficient.", i + 1);
    }
}

// Priority 2: Form-coefficient fallback (ONLY if ShipD fails or unavailable)
if (string.IsNullOrEmpty(geometryJson) && _hullGeometryGenerator != null)
{
    try
    {
        var offsetsGrid = await _hullGeometryGenerator.GenerateOffsetsFromCandidateAsync(...);
        geometryJson = JsonSerializer.Serialize(offsetsGrid, JsonOptions);
        geometryStatus = GeometryGenerationStatus.FormCoefficient;
        _logger.LogInformation("[SIZING_RUN] Generated form-coefficient geometry (FALLBACK) for candidate {Rank}", i + 1);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[SIZING_RUN] Both ShipD and form-coefficient generation failed for candidate {Rank}", i + 1);
    }
}
```

**Testing**:
1. Generate container with bulbous_bow
2. Check logs: Should say "✅ Generated ShipD geometry (PRIMARY)"
3. Verify bulb visible in all views

---

### Task 1.2: Enhance Family Defaults (2 hours)

**File**: `backend/DataService/Data/ShipD/shipd_vessel_taxonomy_seed.json`

**Current** (lines 88-102):
```json
"familyDefaults": {
  "bulbous_bow": {"bit_BB": 1},
  "transom_stern": {"Atrans": 0.5},
  "twin_skeg": {"bit_SB": 1}
}
```

**Enhanced** (complete parameter sets):
```json
"familyDefaults": {
  "bulbous_bow": {
    "bit_BB": 1,
    "Lbb": 0.04,      // Bulb length ratio (4% of Lpp)
    "Hbb": 0.8,       // Bulb height ratio (80% of draft)
    "Bbb": 0.6,       // Bulb breadth ratio (60% of beam)
    "Lbbm": 0.5,      // Bulb asymmetry (centered)
    "Rbb": 0.15,      // Bulb radius (15% fillet)
    "Beta": 15,       // Bow flare angle (15 degrees)
    "Rc": 0.3,        // Bow curvature (moderate)
    "Rk": 0.2         // Bow knuckle (moderate)
  },
  "axe_bow": {
    "bit_BB": 0,      // No bulb
    "Beta": 25,       // Sharp flare (25 degrees)
    "Rk": 0.4,        // Strong knuckle (sharp entry)
    "Rc": 0.2         // Fine curvature
  },
  "fine_entry": {
    "bit_BB": 0,      // No bulb
    "Beta": 10,       // Gentle flare
    "Rc": 0.4,        // Moderate curvature
    "Rk": 0.1         // Subtle knuckle
  },
  "straight_raked": {
    "bit_BB": 0,      // No bulb
    "Beta": 5,        // Minimal flare
    "Rc": 0.3,        // Standard curvature
    "Rk": 0.15        // Moderate knuckle
  },
  "transom_stern": {
    "Atrans": 0.7,    // Strong transom (70% area ratio)
    "Beta_trans": 10, // Rake angle (10 degrees)
    "Bc_trans": 0.85, // Transom width (85% of beam)
    "Rc_trans": 0.3,  // Stern curvature (moderate)
    "Rk_trans": 0.15, // Stern knuckle (moderate)
    "Kappa_stern": 0.5 // Neutral convexity
  },
  "cruiser_stern": {
    "Atrans": 0.2,    // Minimal transom (rounded)
    "Adel_stern": 0.4, // Sheer coefficient A
    "Bdel_stern": 0.3, // Sheer coefficient B
    "Rc_trans": 0.4,  // Higher curvature (rounder)
    "Kappa_stern": 0.6 // Convex (outward curve)
  },
  "canoe_stern": {
    "Atrans": 0.1,    // Nearly no transom
    "Adel_stern": 0.6, // Strong sheer
    "Bdel_stern": 0.5, // Strong roundness
    "Rc_trans": 0.5,  // High curvature
    "Kappa_stern": 0.5 // Neutral
  },
  "twin_skeg": {
    "bit_SB": 1,      // Skeg enabled
    "Atrans": 0.6,    // Some transom
    "SK_z": 0.3,      // Skeg vertical offset
    "Lsb": 0.15,      // Skeg length (15% of Ls)
    "Hsb": 0.4,       // Skeg height (40% of draft)
    "Bsb": 0.3,       // Skeg breadth (30% of beam)
    "Lsbm": 0.5,      // Skeg asymmetry (centered)
    "Rsb": 0.2        // Skeg radius
  },
  "fine_midship": {
    "bit_EP_T": 1,    // Tumblehome enabled
    "Rc": 0.4         // Bilge curvature
  },
  "full_midship": {
    "bit_EP_T": 0,    // No tumblehome
    "Rc": 0.3,        // Standard curvature
    "Cdrft": 6.5      // Moderate deadrise (6.5 degrees)
  },
  "deep_v_midship": {
    "Cdrft": 25,      // High deadrise (25 degrees)
    "Adrft": 0.4,     // Draft rocker A
    "Bdrft": 0.3      // Draft rocker B
  }
}
```

**For ALL vessel types** (general_cargo, bulk_carrier, container, tanker, lng_carrier, cruise_vessel, passenger_vessel, fishing_vessel).

**Testing**:
1. Leave families blank in wizard
2. Check logs: Should show "Applied bow family default..."
3. Verify shape matches family (e.g., bulbous_bow shows bulb)

---

### Task 1.3: Create DataService Migration (1 hour)

**File**: New migration in `backend/DataService/Migrations/`

**Purpose**: Update shipd_taxonomy table with enhanced familyDefaults JSON

**Command**:
```bash
cd backend/DataService
dotnet ef migrations add UpdateShipDFamilyDefaultsComplete
```

**Testing**: Verify taxonomy seed data gets updated in database

---

## PHASE 2: COMPLETE FAMILY SUPPORT (P1 - 10 hours)

### Task 2.1: Implement Twin Skeg Geometry (4 hours)

**File**: `backend/HullSizingService/Services/Geometry/ShipDHullGeometryService.cs`

**Add Method** (after GenerateBulbOffsets at line 883):
```csharp
private Dictionary<decimal, decimal> GenerateSkegOffsets(
    decimal stationPos,
    decimal beamM,
    decimal draftM,
    decimal lppM,
    Dictionary<int, decimal> denormalized,
    decimal[] shipdVector)
{
    var offsets = new Dictionary<decimal, decimal>();
    
    // Check if skeg enabled
    var bitSB = shipdVector[32];
    if (bitSB <= 0.5m)
        return offsets; // No skeg
    
    // Skeg parameters
    var skZ = denormalized[23];    // SK_z - vertical offset
    var lsb = denormalized[39];    // Skeg length ratio
    var hsb = denormalized[41];    // Skeg height ratio (twin)
    var bsb = denormalized[42];    // Skeg breadth ratio
    var lsbm = denormalized[43];   // Longitudinal moment
    var rsb = denormalized[44];    // Skeg radius
    
    var ls = shipdVector[2]; // Stern length ratio
    var skegExtent = lsb * ls;
    
    if (stationPos >= 0 && stationPos <= skegExtent)
    {
        // Position within skeg (0 = stern tip, 1 = skeg end)
        var skegPos = skegExtent > 0 ? stationPos / skegExtent : 0m;
        
        // Skeg dimensions
        var skegHeight = hsb * draftM;
        var skegBreadth = bsb * beamM / 2m; // Half-breadth
        var skegVerticalOffset = skZ * draftM;
        
        // Generate ellipsoidal skeg profile (similar to bulb)
        var heightSteps = 20;
        for (int h = 0; h <= heightSteps; h++)
        {
            var height = skegVerticalOffset - skegHeight + (decimal)h / heightSteps * skegHeight;
            if (height < 0) continue; // Skip below keel
            
            // Ellipsoidal profile
            var verticalPos = (height - skegVerticalOffset + skegHeight / 2m) / (skegHeight / 2m);
            var longitudinalPos = (skegPos - 0.5m) * 2m;
            
            // Ellipsoid equation: x²/a² + y²/b² + z²/c² = 1
            var ellipsoidValue = 1m - verticalPos * verticalPos - longitudinalPos * longitudinalPos;
            if (ellipsoidValue > 0)
            {
                var halfBreadth = skegBreadth * (decimal)Math.Sqrt((double)ellipsoidValue);
                
                // Apply fillet radius
                halfBreadth *= (1m - rsb * 0.3m);
                
                offsets[height] = halfBreadth;
            }
        }
    }
    
    return offsets;
}
```

**Integration**: Call from GenerateStationOffsets for stern region stations

**Testing**: Generate container with twin_skeg, verify skeg appendages in all views

---

### Task 2.2: Verify Cruiser/Canoe Stern (2 hours)

**File**: `backend/HullSizingService/Services/Geometry/ShipDHullGeometryService.cs` lines 759-793

**Actions**:
1. Review Adel_stern, Bdel_stern usage
2. Test with cruise_vessel + cruiser_stern
3. Test with fishing_vessel + canoe_stern
4. Compare with ShipD reference images
5. Adjust parameters if shapes don't match

---

### Task 2.3: Add Comprehensive Logging (2 hours)

**Files**: 
- `backend/HullSizingService/Services/SizingRunService.cs`
- `backend/HullSizingService/Services/ShipD/ShipDParameterAdapter.cs`
- `backend/HullSizingService/Services/Geometry/ShipDHullGeometryService.cs`

**Add Logging**:
```csharp
// In SizingRunService
_logger.LogInformation(
    "[SIZING_RUN] Geometry generation for candidate {Rank}: Generator={Generator}, BowFamily={Bow}, SternFamily={Stern}, bit_BB={BitBB}, Atrans={Atrans}",
    i + 1, geometryStatus, bowFamily, sternFamily, 
    candidateShipdVector?[31], candidateShipdVector?[22]);

// In ShipDParameterAdapter
_logger.LogInformation(
    "[SHIPD_ADAPTER] Family selections: Bow={Bow}, Midship={Mid}, Stern={Stern}",
    bowFamily ?? "null", midshipFamily ?? "null", sternFamily ?? "null");
    
_logger.LogDebug(
    "[SHIPD_ADAPTER] Family parameters: bit_BB={BitBB}, Lbb={Lbb}, Hbb={Hbb}, Atrans={Atrans}, Bc_trans={BcTrans}",
    vector[31], vector[33], vector[34], vector[22], vector[28]);

// In ShipDHullGeometryService
_logger.LogInformation(
    "[SHIPD_GEOMETRY] Generating geometry with families - bulb_enabled={Bulb}, transom_type={Transom}, skeg_enabled={Skeg}",
    shipdVector[31] > 0.5m, shipdVector[22] > 0.5m ? "transom" : "canoe", 
    shipdVector[32] > 0.5m);
```

---

### Task 2.4: Create Integration Tests (2 hours)

**File**: New `backend/HullSizingService.Tests/Validation/Integration/BowSternFamilyShapeTests.cs`

**Tests**:
```csharp
[Fact]
[Trait("Category", "Integration")]
public async Task GenerateGeometry_WithBulbousBow_ShowsBulbInPlanView()
{
    // Arrange: Container ship with bulbous_bow
    var missionCase = new MissionCase {
        BowFamily = "bulbous_bow",
        MissionType = "container",
        // ... other required fields
    };
    
    // Act: Generate candidate
    var result = await _solver.SolveAsync(request);
    var candidate = result.Candidates.First();
    
    // Assert: bit_BB should be 1, bulb parameters should be set
    Assert.Contains("bit_BB", candidate.ShipdParametersJson);
    // Parse and verify bit_BB = 1, Lbb > 0, Hbb > 0, Bbb > 0
    
    // Assert: Geometry should have bulb stations forward of FP
    // Parse geometryJson and verify stations extend beyond Lpp
}

[Fact]
[Trait("Category", "Integration")]
public async Task GenerateGeometry_WithTransomStern_ShowsFlatStern()
{
    // Similar test for transom_stern
    // Verify Atrans > 0.5, Bc_trans set
    // Verify stern waterlines show flat transom (not tapered point)
}

[Fact]
[Trait("Category", "Integration")]
public async Task GenerateGeometry_WithAxeBow_ShowsSharpEntry()
{
    // Test axe_bow
    // Verify bit_BB = 0 (no bulb)
    // Verify high Beta and Rk (sharp entry)
}

[Fact]
[Trait("Category", "Integration")]
public async Task GenerateGeometry_WithDefaultFamilies_UsesT axonomyDefaults()
{
    // Leave families null
    // Verify taxonomy defaults applied
    // Verify complete parameter sets (not just flags)
}
```

---

## PHASE 2: ADDITIONAL CRITICAL ISSUES (P1 - 5 hours)

### Task 3.1: Fix maxCandidates Not Passed from UI (15 min)

**Files**: 
- `frontend/src/components/sizing/wizard/Step4Options.tsx`
- `frontend/src/pages/sizing/MissionWizard.tsx`

**Change Step4Options**:
```typescript
// Add to props interface
interface Step4OptionsProps {
  // ... existing props
  onSubmit: (options: {maxCandidates: number, minFn: number, maxFn: number, locks: any}) => void;
}

// In component
const handleNext = () => {
  onSubmit({
    maxCandidates,  // ✅ Pass actual value
    minFn,
    maxFn,
    locks: { keepFn, keepLOverB, keepBOverT, keepDOverT, keepCbBand }
  });
};
```

**Change MissionWizard** (line 386):
```typescript
// Remove hardcoded value
options: {
  maxCandidates: solverOptions.maxCandidates,  // ✅ Use from Step4Options
  additionalParameters: additionalParameters,
}
```

---

### Task 3.2: Vessel-Type Constraint Filtering (2-3 hours)

**File**: New `frontend/src/utils/vesselConstraintRules.ts`

**Implementation**:
```typescript
export const CANAL_CONSTRAINT_APPLICABILITY = {
  container: ['Panamax', 'Neo-Panamax', 'Suezmax', 'Malaccamax'],
  tanker: ['Panamax', 'Neo-Panamax', 'Suezmax', 'Aframax', 'VLCC'],
  bulk_carrier: ['Panamax', 'Neo-Panamax', 'Capesize'],
  lng_carrier: ['Malaccamax', 'Q-Max'],
  cruise_vessel: ['Panamax', 'Neo-Panamax'],
  general_cargo: ['Panamax', 'Neo-Panamax'],
  passenger_vessel: ['Panamax'],
  fishing_vessel: [], // No canal constraints
  recreational: [],   // No canal constraints
} as const;

export function getApplicableConstraints(vesselType: string): string[] {
  const normalized = vesselType.toLowerCase().replace(/[^a-z]/g, '_');
  return CANAL_CONSTRAINT_APPLICABILITY[normalized] || [];
}
```

**File**: `frontend/src/components/sizing/wizard/Step3Constraints.tsx`

**Change**:
```typescript
const applicablePresets = getApplicableConstraints(formData.missionType);

// Filter preset buttons
<button 
  disabled={!applicablePresets.includes('Panamax')}
  title={!applicablePresets.includes('Panamax') ? 
    'Panamax constraints not applicable to this vessel type' : 
    'Apply Panamax constraints'}
  ...
>
  Panamax
</button>
```

---

### Task 3.3: Pre-Flight Constraint Check (2-3 hours)

**File**: New `backend/HullSizingService/Services/Solver/ConstraintFeasibilityValidator.cs`

**Implementation**:
```csharp
public class ConstraintFeasibilityValidator
{
    public (bool IsFeasible, List<string> Warnings) CheckFeasibility(
        decimal targetDisplacementT,
        decimal? maxBeamM,
        decimal? maxDraftM,
        decimal? maxLoaM,
        string vesselType)
    {
        var warnings = new List<string>();
        
        // Estimate required dimensions for target displacement
        // Using typical L/B, B/T ratios for vessel type
        var (estimatedLength, estimatedBeam, estimatedDraft) = 
            EstimateRequiredDimensions(targetDisplacementT, vesselType);
        
        // Check against constraints
        if (maxBeamM.HasValue && estimatedBeam > maxBeamM.Value * 1.1m)
        {
            warnings.Add($"Beam constraint ({maxBeamM:F1}m) likely too restrictive. Estimated need: ~{estimatedBeam:F1}m for {targetDisplacementT:F0}t displacement.");
            return (false, warnings);
        }
        
        if (maxDraftM.HasValue && estimatedDraft > maxDraftM.Value * 1.1m)
        {
            warnings.Add($"Draft constraint ({maxDraftM:F1}m) likely too restrictive. Estimated need: ~{estimatedDraft:F1}m.");
            return (false, warnings);
        }
        
        // ... similar for LOA
        
        return (true, warnings);
    }
}
```

**Integration**: Call from FirstPrinciplesSolver before generating variants

---

## DOCUMENTATION DELIVERABLES

### Doc 1: Root Cause Analysis (DONE)

**File**: `.plan/active/hull-sizing/BOW-STERN-SHAPE-ROOT-CAUSE-ANALYSIS.md`  
**Status**: ✅ Created

### Doc 2: Implementation Plan (THIS FILE)

**File**: `.plan/active/hull-sizing/BOW-STERN-SHAPE-FIX-PLAN.md`  
**Status**: ✅ Created

### Doc 3: Known Issues Log

**File**: `.plan/active/hull-sizing/HULL-SIZING-KNOWN-ISSUES.md`  
**Status**: To be created

**Contents**:
- maxCandidates not passed (Issue A)
- Panamax constraints too restrictive (Issue B)
- Missing LOA in UI (Issue C)
- No pre-flight validation (Issue D)
- Generator priority backwards (ROOT CAUSE)
- Twin skeg not implemented (Issue E)
- Minimal family defaults (Issue F)

### Doc 4: Testing Checklist

**File**: `.plan/active/hull-sizing/BOW-STERN-SHAPE-TESTING-CHECKLIST.md`  
**Status**: To be created

**Test Cases**:
- Bulbous bow visibility
- Transom stern flatness
- Axe bow sharp entry
- Twin skeg appendages
- Default family selection
- Longitudinal proportions
- All 4 views consistency

---

## SUCCESS CRITERIA

### Before Fix (Current State):
- ❌ Bulbous bow: Generic taper, no bulb protrusion
- ❌ Transom stern: Tapered to point, no flat surface
- ❌ Axe bow: Similar to other bows, not sharp
- ❌ Twin skeg: No skeg appendages visible
- ❌ User selections don't affect geometry meaningfully

### After P0 Fixes (5 hours):
- ✅ Bulbous bow: Visible bulb protrusion in all views
- ✅ Transom stern: Flat transom surface visible
- ✅ Axe bow: Sharp bow entry visible
- ✅ Default families: Complete parameters applied
- ✅ Shapes match prefinal_1 requirements

### After P1 Fixes (15 hours):
- ✅ Twin skeg: Skeg appendages visible
- ✅ maxCandidates: User gets requested count
- ✅ Constraints: Only applicable ones shown
- ✅ Cruiser/canoe stern: Verified correct

---

## ROLLOUT STRATEGY

### Phase 1: P0 Fixes (Week 1)
1. Day 1: Reverse generator priority + testing
2. Day 2: Enhance family defaults + migration
3. Day 3: Integration testing + visual verification

### Phase 2: P1 Fixes (Week 2)
1. Day 1-2: Twin skeg implementation
2. Day 3: Quick wins (maxCandidates, constraint filtering)
3. Day 4: Pre-flight validation
4. Day 5: Comprehensive testing

### Phase 3: Documentation (Week 3)
1. Complete all .plan/ documents
2. Update user-facing documentation
3. Create before/after comparison screenshots
4. Video demonstration of fixes

---

## RISK MITIGATION

**Risk**: ShipD generator might fail for some edge cases  
**Mitigation**: Form-coefficient remains as fallback

**Risk**: Enhanced defaults might not suit all vessels  
**Mitigation**: User can override via advanced parameters

**Risk**: Migration might fail in production  
**Mitigation**: Test thoroughly in local/dev environments first

**Risk**: Breaking existing hulls  
**Mitigation**: Run all 85 unit tests + regression tests

---

## MEASUREMENT

**Before Fix Metrics** (baseline):
- Generate 10 containers with bulbous_bow
- Count how many show visible bulb: Likely 0/10
- User satisfaction: Low (reports ongoing issue)

**After Fix Metrics** (target):
- Same 10 containers
- Count with visible bulb: Should be 10/10
- User satisfaction: High (selections finally work!)

---

**Status**: READY FOR IMPLEMENTATION  
**Next Action**: Execute Phase 1 (P0 fixes)

