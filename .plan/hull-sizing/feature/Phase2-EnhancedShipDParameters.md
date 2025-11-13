# Phase 2: Enhanced ShipD Parameter Inputs

## Overview

Based on the ShipD support images analysis, we need to expose additional geometric parameters that allow users to fine-tune hull shapes beyond just selecting bow/midship/stern families. This enhancement will make generated designs more aligned with ShipD taxonomy and provide users with granular control over hull geometry.

## Reference Images Analysis

### Image 1: Section Geometry Parameters
**Location**: `support_files/image-1.png`

**Key Parameters Identified**:
- **Flare**: Outward angle of upper sides (degrees)
- **Hard Chine vs Soft Chine**: Sharp corner vs rounded corner transition
- **Deadrise**: Angle of lower sides relative to horizontal (degrees)
- **Convex Curvature**: Rounded bottom section
- **Concave Curvature**: Inward curving sides
- **Tumblehome**: Inward curving upper sides

**ShipD Parameter Mapping**:
- Flare → `Beta` (index 8) - Bow flare angle
- Deadrise → `Cdrft` (index 19) - Forward deadrise/flare control angle
- Tumblehome → `bit_EP_T` (index 21) - Midship tumblehome toggle
- Chine type → Affects `Rc` (index 9) and `Rk` (index 10) curvature coefficients
- Curvature type → Affects `Kappa_bow` (index 14) and `Kappa_stern` (index 24)

### Image 2: Longitudinal Segmentation
**Location**: `support_files/image-2.png`

**Key Parameters Identified**:
- **Lb**: Bow length ratio relative to LOA
- **Lm**: Mid-body length ratio relative to LOA
- **Ls**: Stern length ratio relative to LOA
- **Bow rake and curvature**: Forward section geometry
- **Transition from taper to mid-body**: Taper control
- **Stern shape and transom cross section**: Aft geometry

**ShipD Parameter Mapping**:
- Lb → `Lb` (index 1) - Already exists in metadata
- Ls → `Ls` (index 2) - Already exists in metadata
- Lm → Calculated as `1 - Lb - Ls` (derived)
- Bow rake → `Beta` (index 8), `Adel_bow` (index 15), `Bdel_bow` (index 16)
- Stern shape → `Atrans` (index 22), `Beta_trans` (index 27), `Bc_trans` (index 28)

### Image 3: Bulb Geometry
**Location**: `support_files/image-3.png`

**Key Parameters Identified**:
- **Bulb Length**: `Lbb` (index 33)
- **Bulb Width**: `Bbb` (index 35)
- **Bulb Height**: `Hbb` (index 34)
- **Asymmetry Factors**: `Lbbm` (index 36) - Bulb longitudinal moment coefficient
- **Fillet Radius**: `Rbb` (index 37) - Bulb radius coefficient

**ShipD Parameter Mapping**:
- All parameters already exist in metadata (indices 31-37)
- Currently only `bit_BB` (index 31) is conditionally exposed
- Need to expose all bulb parameters when `bulbous_bow` is selected

## Implementation Plan

### Phase 2.1: Backend Data Model Extensions

#### 2.1.1 Extend ShipD Parameter Metadata
**File**: `backend/DataService/Data/ShipD/ShipDMetadataDefaults.cs`

**Changes**:
- Add metadata flags for conditional parameter groups:
  - `RequiresBowFamily`: Parameters that only apply when specific bow families are selected
  - `RequiresMidshipFamily`: Parameters that only apply when specific midship families are selected
  - `RequiresSternFamily`: Parameters that only apply when specific stern families are selected
  - `RequiresBulbousBow`: Parameters that only apply when bulbous bow is enabled
- Add parameter grouping metadata:
  - `SectionGeometryGroup`: Flare, deadrise, chine type, curvature
  - `LongitudinalGroup`: Lb, Lm, Ls, rake angles
  - `BulbGroup`: All bulb parameters (33-37)

**New Structure**:
```csharp
public record ShipDParameterMetadataSeed(
    int ParameterIndex,
    string Label,
    string? Group,
    string? Description,
    string? Unit,
    decimal? Min,
    decimal? Max,
    decimal? Mean,
    decimal? StdDev,
    string? MetadataJson,
    // NEW FIELDS:
    string[]? RequiredFamilies = null,  // e.g., ["bulbous_bow"] for bulb params
    string? ParameterGroup = null,      // "section_geometry", "longitudinal", "bulb"
    bool IsConditional = false          // Requires family selection
);
```

#### 2.1.2 Extend AdditionalParameters Schema
**File**: `backend/Shared/DTOs/Sizing/SizingRunDto.cs`

**Changes**:
- Define strongly-typed structure for conditional parameters:
```csharp
public record ShipDAdditionalParameters
{
    // Section Geometry (Image 1)
    public decimal? FlareAngleDeg { get; init; }        // Beta (index 8)
    public decimal? DeadriseAngleDeg { get; init; }     // Cdrft (index 19)
    public string? ChineType { get; init; }              // "hard" | "soft" → affects Rc, Rk
    public string? CurvatureType { get; init; }         // "convex" | "concave" → affects Kappa
    public bool? TumblehomeEnabled { get; init; }       // bit_EP_T (index 21)
    
    // Longitudinal Segmentation (Image 2)
    public decimal? BowLengthRatio { get; init; }       // Lb (index 1) - override default
    public decimal? MidBodyLengthRatio { get; init; }   // Lm (derived: 1 - Lb - Ls)
    public decimal? SternLengthRatio { get; init; }     // Ls (index 2) - override default
    public decimal? BowRakeAngleDeg { get; init; }       // Beta (index 8) - separate from flare
    public decimal? SternRakeAngleDeg { get; init; }    // Beta_trans (index 27)
    
    // Bulb Geometry (Image 3) - only when bulbous_bow selected
    public decimal? BulbLengthRatio { get; init; }      // Lbb (index 33)
    public decimal? BulbWidthRatio { get; init; }       // Bbb (index 35)
    public decimal? BulbHeightRatio { get; init; }      // Hbb (index 34)
    public decimal? BulbAsymmetryFactor { get; init; }   // Lbbm (index 36)
    public decimal? BulbFilletRadius { get; init; }      // Rbb (index 37)
}
```

#### 2.1.3 Update ShipDParameterAdapter
**File**: `backend/HullSizingService/Services/ShipD/ShipDParameterAdapter.cs`

**Changes**:
- Add method `ApplyConditionalParameters` that:
  1. Reads `AdditionalParameters` from `CreateSizingRunDto`
  2. Maps user inputs to ShipD parameter indices based on selected families
  3. Validates ranges using metadata min/max
  4. Applies normalized values to the 45-parameter vector
  5. Logs warnings for out-of-range or conflicting inputs

**Key Logic**:
```csharp
private void ApplyConditionalParameters(
    decimal[] vector,
    ShipDAdditionalParameters? additional,
    string? bowFamily,
    string? midshipFamily,
    string? sternFamily,
    IReadOnlyList<ShipDParameterMetadataDto> metadata)
{
    if (additional == null) return;
    
    // Section Geometry (Image 1)
    if (additional.FlareAngleDeg.HasValue)
    {
        var param = metadata.First(m => m.ParameterIndex == 8); // Beta
        vector[8] = Normalize(additional.FlareAngleDeg.Value, param.Min, param.Max);
    }
    
    if (additional.DeadriseAngleDeg.HasValue)
    {
        var param = metadata.First(m => m.ParameterIndex == 19); // Cdrft
        vector[19] = Normalize(additional.DeadriseAngleDeg.Value, param.Min, param.Max);
    }
    
    // Chine type affects Rc (index 9) and Rk (index 10)
    if (additional.ChineType == "hard")
    {
        // Hard chine: sharper transition, lower Rc, higher Rk
        vector[9] = 0.2m;  // Lower curvature
        vector[10] = 0.8m; // Higher knuckle
    }
    else if (additional.ChineType == "soft")
    {
        // Soft chine: rounded transition, higher Rc, lower Rk
        vector[9] = 0.6m;  // Higher curvature
        vector[10] = 0.3m; // Lower knuckle
    }
    
    // Tumblehome toggle
    if (additional.TumblehomeEnabled == true && midshipFamily == "fine_midship")
    {
        vector[21] = 1.0m; // bit_EP_T
    }
    
    // Longitudinal Segmentation (Image 2)
    if (additional.BowLengthRatio.HasValue)
    {
        var param = metadata.First(m => m.ParameterIndex == 1); // Lb
        vector[1] = Math.Clamp(additional.BowLengthRatio.Value, param.Min.Value, param.Max.Value);
    }
    
    if (additional.SternLengthRatio.HasValue)
    {
        var param = metadata.First(m => m.ParameterIndex == 2); // Ls
        vector[2] = Math.Clamp(additional.SternLengthRatio.Value, param.Min.Value, param.Max.Value);
    }
    
    // Bulb Geometry (Image 3) - only if bulbous_bow selected
    if (bowFamily == "bulbous_bow" && additional.BulbLengthRatio.HasValue)
    {
        vector[31] = 1.0m; // bit_BB - enable bulb
        var param = metadata.First(m => m.ParameterIndex == 33); // Lbb
        vector[33] = Math.Clamp(additional.BulbLengthRatio.Value, param.Min.Value, param.Max.Value);
        
        if (additional.BulbWidthRatio.HasValue)
        {
            param = metadata.First(m => m.ParameterIndex == 35); // Bbb
            vector[35] = Math.Clamp(additional.BulbWidthRatio.Value, param.Min.Value, param.Max.Value);
        }
        
        if (additional.BulbHeightRatio.HasValue)
        {
            param = metadata.First(m => m.ParameterIndex == 34); // Hbb
            vector[34] = Math.Clamp(additional.BulbHeightRatio.Value, param.Min.Value, param.Max.Value);
        }
        
        if (additional.BulbAsymmetryFactor.HasValue)
        {
            param = metadata.First(m => m.ParameterIndex == 36); // Lbbm
            vector[36] = Math.Clamp(additional.BulbAsymmetryFactor.Value, param.Min.Value, param.Max.Value);
        }
        
        if (additional.BulbFilletRadius.HasValue)
        {
            param = metadata.First(m => m.ParameterIndex == 37); // Rbb
            vector[37] = Math.Clamp(additional.BulbFilletRadius.Value, param.Min.Value, param.Max.Value);
        }
    }
}
```

#### 2.1.4 Update Validation
**File**: `backend/Shared/Validators/Sizing/CreateSizingRunDtoValidator.cs`

**Changes**:
- Add validation rules for `AdditionalParameters`:
  - Flare angle: 0-45 degrees
  - Deadrise angle: 0-60 degrees
  - Length ratios: Lb + Ls < 1.0 (to ensure Lm > 0)
  - Bulb parameters: Only valid when `bowFamily == "bulbous_bow"`
  - Chine type: Must be "hard" or "soft" if provided

### Phase 2.2: Frontend UI Extensions

#### 2.2.1 Extend TypeScript Types
**File**: `frontend/src/types/sizing.ts`

**Changes**:
```typescript
export interface ShipDAdditionalParameters {
  // Section Geometry (Image 1)
  flareAngleDeg?: number;
  deadriseAngleDeg?: number;
  chineType?: "hard" | "soft";
  curvatureType?: "convex" | "concave";
  tumblehomeEnabled?: boolean;
  
  // Longitudinal Segmentation (Image 2)
  bowLengthRatio?: number;
  midBodyLengthRatio?: number;
  sternLengthRatio?: number;
  bowRakeAngleDeg?: number;
  sternRakeAngleDeg?: number;
  
  // Bulb Geometry (Image 3)
  bulbLengthRatio?: number;
  bulbWidthRatio?: number;
  bulbHeightRatio?: number;
  bulbAsymmetryFactor?: number;
  bulbFilletRadius?: number;
}

export interface SizingOptionsDto {
  familyHints?: string[];
  maxCandidates?: number;
  minFn?: number;
  maxFn?: number;
  additionalParameters?: ShipDAdditionalParameters; // Updated type
}
```

#### 2.2.2 Create New Wizard Step: Hull Geometry Details
**File**: `frontend/src/components/sizing/wizard/Step2bHullGeometryDetails.tsx` (NEW)

**Purpose**: Conditional step that appears after Step 2 (Hull Families) to capture detailed geometry parameters based on selected families.

**Structure**:
- **Section 1: Section Geometry** (always visible)
  - Flare Angle (degrees) - slider 0-45
  - Deadrise Angle (degrees) - slider 0-60
  - Chine Type - radio: Hard / Soft
  - Curvature Type - radio: Convex / Concave
  - Tumblehome - checkbox (only enabled if `midshipFamily == "fine_midship"`)

- **Section 2: Longitudinal Proportions** (always visible)
  - Bow Length Ratio (Lb) - slider 0.05-0.90
  - Mid-Body Length Ratio (Lm) - calculated display (1 - Lb - Ls)
  - Stern Length Ratio (Ls) - slider 0.05-0.90
  - Bow Rake Angle (degrees) - slider 0-45
  - Stern Rake Angle (degrees) - slider 0-60

- **Section 3: Bulb Geometry** (only visible if `bowFamily == "bulbous_bow"`)
  - Bulb Length Ratio - slider 0.0-0.2
  - Bulb Width Ratio - slider 0.0-1.0
  - Bulb Height Ratio - slider 0.0-1.0
  - Bulb Asymmetry Factor - slider -1.0 to 1.0
  - Bulb Fillet Radius - slider 0.05-0.33

**Implementation Notes**:
- Use collapsible sections with icons
- Show parameter descriptions from ShipD metadata
- Display min/max ranges as tooltips
- Auto-calculate Lm when Lb or Ls changes
- Validate that Lb + Ls < 1.0
- Show warnings for invalid combinations

#### 2.2.3 Update MissionWizard Flow
**File**: `frontend/src/pages/sizing/MissionWizard.tsx`

**Changes**:
- Insert new step between Step 2 (Hull Families) and Step 3 (Speed & Environment)
- New step order:
  1. Mission & Cargo
  2. Hull Families
  3. **Hull Geometry Details** (NEW - conditional)
  4. Speed & Environment
  5. Constraints
  6. Options & Review

- Conditionally show Step 2b only if:
  - All three families are selected
  - OR user explicitly enables "Advanced Geometry" toggle

**Step Logic**:
```typescript
const steps = [
  "Mission & Cargo",
  "Hull Families",
  ...(showGeometryDetails ? ["Hull Geometry Details"] : []),
  "Speed & Environment",
  "Constraints",
  "Options & Review"
];
```

#### 2.2.4 Update Step4Options Summary
**File**: `frontend/src/components/sizing/wizard/Step4Options.tsx`

**Changes**:
- Display selected geometry parameters in the summary:
  - "Flare: 15°, Deadrise: 30°, Hard Chine"
  - "Longitudinal: Lb=0.32, Lm=0.26, Ls=0.42"
  - "Bulb: L=0.10, W=0.50, H=0.50" (if applicable)

### Phase 2.3: Backend Integration

#### 2.3.1 Update SizingRunService
**File**: `backend/HullSizingService/Services/SizingRunService.cs`

**Changes**:
- Ensure `AdditionalParameters` from `CreateSizingRunDto.Options` are passed to `ShipDParameterAdapter.BuildAsync`
- No changes needed - already flows through

#### 2.3.2 Update ShipDParameterAdapter Integration
**File**: `backend/HullSizingService/Services/ShipD/ShipDParameterAdapter.cs`

**Changes**:
- Call `ApplyConditionalParameters` after building base vector
- Merge user `AdditionalParameters` with taxonomy defaults
- Prefer user values over taxonomy defaults when both present

### Phase 2.4: Data Migration

#### 2.4.1 Database Schema
**No schema changes required** - using existing JSON fields:
- `mission_cases.shipd_inputs_json` - Already exists
- `sizing_runs.options_json` - Already contains `AdditionalParameters`
- `candidate_designs.shipd_parameters_json` - Already exists

#### 2.4.2 Backward Compatibility
- Existing missions without geometry details will use taxonomy defaults
- `AdditionalParameters` is optional - existing code paths unchanged
- Validation only applies when parameters are provided

### Phase 2.5: Testing Strategy

#### 2.5.1 Unit Tests
**File**: `backend/HullSizingService.Tests/Services/ShipD/ShipDParameterAdapterTests.cs` (NEW)

**Test Cases**:
1. `ApplyConditionalParameters_WithFlareAngle_SetsBetaIndex`
2. `ApplyConditionalParameters_WithHardChine_AdjustsRcAndRk`
3. `ApplyConditionalParameters_WithBulbousBow_EnablesBulbParameters`
4. `ApplyConditionalParameters_WithInvalidLengthRatios_ThrowsValidationException`
5. `ApplyConditionalParameters_MergesUserAndTaxonomyDefaults`

#### 2.5.2 Integration Tests
**File**: `backend/HullSizingService.Tests/Integration/SizingRunWithGeometryTests.cs` (NEW)

**Test Cases**:
1. `CreateSizingRun_WithGeometryDetails_GeneratesCorrectShipDVector`
2. `CreateSizingRun_WithBulbousBow_IncludesBulbParameters`
3. `CreateSizingRun_WithInvalidRatios_ReturnsValidationError`

#### 2.5.3 E2E Tests
**File**: `frontend/e2e/hull-geometry-details.spec.ts` (NEW)

**Test Cases**:
1. `wizard shows geometry step when families selected`
2. `bulb section appears only for bulbous_bow`
3. `longitudinal ratios validate correctly`
4. `geometry parameters persist in summary`

### Phase 2.6: Documentation

#### 2.6.1 User Documentation
- Add tooltips/help text explaining each parameter
- Reference ShipD support images in help modal
- Provide typical value ranges for common vessel types

#### 2.6.2 Developer Documentation
- Document parameter index mappings
- Explain normalization logic
- Document validation rules

## Implementation Order

### Sprint 1: Backend Foundation
1. ✅ Extend `ShipDParameterMetadataSeed` with conditional flags
2. ✅ Create `ShipDAdditionalParameters` record
3. ✅ Implement `ApplyConditionalParameters` in adapter
4. ✅ Add validation rules
5. ✅ Write unit tests

### Sprint 2: Frontend UI
1. ✅ Create `Step2bHullGeometryDetails` component
2. ✅ Extend TypeScript types
3. ✅ Integrate into wizard flow
4. ✅ Update summary display
5. ✅ Write E2E tests

### Sprint 3: Integration & Polish
1. ✅ End-to-end testing
2. ✅ Performance validation
3. ✅ Documentation
4. ✅ User acceptance testing

## Success Criteria

1. ✅ Users can specify flare, deadrise, and chine type for all vessel types
2. ✅ Users can adjust longitudinal proportions (Lb, Lm, Ls)
3. ✅ Bulb parameters appear conditionally when bulbous bow is selected
4. ✅ All parameters validate against ShipD metadata ranges
5. ✅ Generated hulls reflect user-specified geometry details
6. ✅ Backward compatibility maintained for existing missions

## Risk Mitigation

1. **Parameter Conflicts**: Validate that user inputs don't conflict with family defaults
2. **Performance**: Geometry step is optional - only shown when needed
3. **Complexity**: Use collapsible sections to reduce UI clutter
4. **Validation**: Client-side validation prevents invalid submissions

## Future Enhancements

1. Visual preview of geometry changes (3D hull update in real-time)
2. Preset geometry profiles for common vessel types
3. Import/export geometry configurations
4. AI suggestions for optimal parameter combinations
