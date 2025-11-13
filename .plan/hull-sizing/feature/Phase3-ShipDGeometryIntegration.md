# Phase 3: ShipD Geometry Integration - Solver & Visualization

## Overview

Currently, ShipD parameters are generated and stored but **not used** to create actual hull geometry. The solvers still use parametric formulas (Wigley, Series 60) based on form coefficients, and the frontend visualizations use family-specific approximations. This phase integrates ShipD parameters into both backend geometry generation and frontend visualization to produce hulls that match the ShipD taxonomy shapes.

## Current Architecture Analysis

### Backend Solver Flow
1. `ShipDParameterAdapter` generates 45-parameter vector → stored in `ShipdParametersJson`
2. `FirstPrinciplesSolver` uses `HullFamilyPreset` with parametric formulas (wigley, series60, etc.)
3. Geometry is generated from form coefficients (Cb, Cp, Cwp) and ratios (L/B, B/T)
4. **Problem**: ShipD parameters are ignored during geometry generation

### Frontend Visualization Flow
1. `hullShapeGenerator.ts` uses family-specific formulas (container, tanker, fishing, etc.)
2. Formulas are based on form coefficients, not ShipD parameters
3. `WigleyHull3D` / `ParametricHull3D` render the generated geometry
4. **Problem**: ShipD parameters stored in `candidate.shipdParametersJson` are not used

## Implementation Plan

### Phase 3.1: Backend ShipD Geometry Generator

#### 3.1.1 Create ShipD Hull Geometry Service
**File**: `backend/HullSizingService/Services/Geometry/IShipDHullGeometryService.cs` (NEW)

**Purpose**: Convert ShipD 45-parameter vector into actual hull offsets/sections

**Interface**:
```csharp
public interface IShipDHullGeometryService
{
    /// <summary>
    /// Generates hull sections from ShipD parameter vector
    /// </summary>
    Task<HullSections> GenerateSectionsAsync(
        decimal[] shipdVector,
        decimal lppM,
        decimal beamM,
        decimal draftM,
        IReadOnlyList<ShipDParameterMetadataDto> metadata,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates 3D hull mesh from ShipD parameters
    /// </summary>
    Task<HullMesh3D> GenerateMeshAsync(
        decimal[] shipdVector,
        decimal lppM,
        decimal beamM,
        decimal draftM,
        IReadOnlyList<ShipDParameterMetadataDto> metadata,
        int longitudinalSegments = 60,
        int verticalSegments = 40,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates ShipD parameter vector against constraints
    /// </summary>
    Task<ShipDValidationResult> ValidateParametersAsync(
        decimal[] shipdVector,
        IReadOnlyList<ShipDParameterMetadataDto> metadata,
        CancellationToken cancellationToken = default);
}
```

**Key Methods**:
- `ApplyBowGeometry`: Uses indices 1 (Lb), 8-19 (bow parameters) to generate forward sections
- `ApplyMidshipGeometry`: Uses indices 20-21 (midship toggles) for section shape
- `ApplySternGeometry`: Uses indices 2 (Ls), 22-30 (stern parameters) for aft sections
- `ApplyBulbGeometry`: Uses indices 31-37 (bulb parameters) when `bit_BB == 1`
- `ApplyLongitudinalDistribution`: Uses Lb, Ls to set bow/mid/stern boundaries

#### 3.1.2 Implement ShipD Hull Geometry Service
**File**: `backend/HullSizingService/Services/Geometry/ShipDHullGeometryService.cs` (NEW)

**Implementation Strategy**:
1. **Parse ShipD Vector**: Extract normalized parameters (0-1 range)
2. **Denormalize**: Convert to physical units using metadata min/max
3. **Apply Bow Section** (Image 1 & 2):
   - `Beta` (index 8) → Flare angle
   - `Cdrft` (index 19) → Deadrise angle
   - `Rc`, `Rk` (indices 9-10) → Curvature (affected by chine type)
   - `Lb` (index 1) → Bow length ratio
   - Generate forward sections with proper flare, deadrise, curvature

4. **Apply Midship Section** (Image 1):
   - `bit_EP_S` (index 20) → Sheer extrusion toggle
   - `bit_EP_T` (index 21) → Tumblehome toggle
   - Generate midship sections with appropriate fullness

5. **Apply Stern Section** (Image 2):
   - `Ls` (index 2) → Stern length ratio
   - `Atrans`, `Beta_trans`, `Bc_trans` (indices 22, 27, 28) → Transom geometry
   - `Rc_trans`, `Rk_trans` (indices 29-30) → Stern curvature
   - Generate aft sections with transom/shape

6. **Apply Bulb** (Image 3):
   - `bit_BB` (index 31) → Enable/disable
   - `Lbb`, `Hbb`, `Bbb` (indices 33-35) → Bulb dimensions
   - `Lbbm` (index 36) → Asymmetry
   - `Rbb` (index 37) → Fillet radius
   - Generate bulb geometry at bow

**Reference Implementation**:
- Use ShipD repo's `HullParameterization.py` as reference
- Implement equivalent logic in C# for section generation
- Ensure watertightness and no self-intersection

#### 3.1.3 Update FirstPrinciplesSolver to Use ShipD Geometry
**File**: `backend/HullSizingService/Services/Solver/FirstPrinciplesSolver.cs`

**Changes**:
```csharp
private async Task<SolverCandidate?> GenerateCandidateAsync(
    HullFamilyPreset family,
    MissionCase mission,
    decimal targetDisplacementT,
    WaterPropertiesResponse waterProps,
    SizingLocksDto locks,
    SizingOptionsDto options,
    int variantIndex,
    int variantCount,
    CancellationToken cancellationToken)
{
    // ... existing displacement closure logic ...

    // NEW: Check if ShipD parameters are available
    var shipdVector = await GetShipDVectorAsync(mission, options, cancellationToken);
    
    if (shipdVector != null)
    {
        // Use ShipD geometry generator
        var geometryService = _serviceProvider.GetRequiredService<IShipDHullGeometryService>();
        var metadata = await _dataServiceClient.GetShipDParameterMetadataAsync(cancellationToken);
        
        var hullMesh = await geometryService.GenerateMeshAsync(
            shipdVector,
            candidate.LppM,
            candidate.BM,
            candidate.TM,
            metadata,
            cancellationToken: cancellationToken);
        
        // Store ShipD geometry in candidate
        candidate.GeometryJson = JsonSerializer.Serialize(hullMesh);
        candidate.ShipdParametersJson = JsonSerializer.Serialize(shipdVector);
    }
    else
    {
        // Fallback to parametric formulas (existing logic)
        candidate.GeometryJson = GenerateParametricGeometry(candidate, family);
    }
    
    // ... rest of candidate generation ...
}
```

#### 3.1.4 Add ShipD Geometry to CandidateDesign
**File**: `backend/Shared/Models/Sizing/CandidateDesign.cs`

**Changes**:
- `GeometryJson` already exists - store ShipD-generated mesh
- `ShipdParametersJson` already exists - ensure it's populated
- Add validation that geometry matches ShipD parameters when both present

### Phase 3.2: Frontend ShipD Geometry Generator

#### 3.2.1 Create ShipD Geometry Generator Utility
**File**: `frontend/src/utils/shipdGeometryGenerator.ts` (NEW)

**Purpose**: Convert ShipD parameter vector (from `candidate.shipdParametersJson`) into Three.js geometry

**Key Functions**:
```typescript
/**
 * Generates 3D hull geometry from ShipD parameter vector
 */
export function generateShipDHull3D(
  shipdVector: number[],  // 45-element array
  lppM: number,
  beamM: number,
  draftM: number,
  metadata: ShipDParameterMetadata[],  // For denormalization
  resolution: number = 1.0
): THREE.BufferGeometry;

/**
 * Generates hull sections from ShipD parameters
 */
export function generateShipDSections(
  shipdVector: number[],
  lppM: number,
  beamM: number,
  draftM: number,
  metadata: ShipDParameterMetadata[],
  stationCount: number = 20
): HullSection[];

/**
 * Generates bulb geometry from ShipD parameters
 */
export function generateShipDBulb(
  shipdVector: number[],
  lppM: number,
  beamM: number,
  draftM: number,
  metadata: ShipDParameterMetadata[]
): THREE.BufferGeometry | null;
```

**Implementation Notes**:
- Denormalize parameters: `physical = min + (max - min) * normalized`
- Apply bow geometry using indices 1, 8-19
- Apply midship using indices 20-21
- Apply stern using indices 2, 22-30
- Apply bulb using indices 31-37 when `bit_BB == 1`
- Ensure sections are watertight and smooth

#### 3.2.2 Update ParametricHull3D to Use ShipD When Available
**File**: `frontend/src/components/sizing/visualization/WigleyHull3D.tsx`

**Changes**:
```typescript
const hullGeometry = useMemo(() => {
  // Check if ShipD parameters are available
  if (candidate.shipdParametersJson) {
    try {
      const shipdVector = JSON.parse(candidate.shipdParametersJson);
      if (Array.isArray(shipdVector) && shipdVector.length === 45) {
        // Use ShipD geometry generator
        const metadata = await fetchShipDMetadata(); // Cache this
        return generateShipDHull3D(
          shipdVector,
          candidate.lppM,
          candidate.beamM,
          candidate.draftM,
          metadata,
          resolution
        );
      }
    } catch (error) {
      console.warn('[ParametricHull3D] Failed to parse ShipD vector, using fallback', error);
    }
  }
  
  // Fallback to parametric formulas (existing logic)
  return generateHull3DGeometry({
    hullFamily: candidate.hullFamily,
    // ... existing params
  });
}, [candidate, resolution]);
```

#### 3.2.3 Update 2D Visualization Components

**File**: `frontend/src/components/sizing/visualization/Hull2DSections.tsx`

**Changes**:
- Check for `candidate.shipdParametersJson`
- If present, use `generateShipDSections()` instead of parametric formulas
- Apply proper flare, deadrise, chine type from ShipD parameters
- Show bulb in forward sections when present

**File**: `frontend/src/components/sizing/visualization/Hull2DProfile.tsx`

**Changes**:
- Use ShipD parameters for longitudinal distribution (Lb, Lm, Ls)
- Apply bow rake from `Beta` (index 8)
- Apply stern rake from `Beta_trans` (index 27)
- Show bulb profile when `bit_BB == 1`

### Phase 3.3: Enhanced Visualization Panels

#### 3.3.1 Create ShipD Parameter Chart Component
**File**: `frontend/src/components/sizing/visualization/ShipDParameterChart.tsx` (NEW)

**Purpose**: Visualize ShipD parameters vs typical ranges

**Features**:
- Bar chart showing each of 45 parameters
- Color-coded: green (within typical range), yellow (near limits), red (out of range)
- Grouped by category: Principal, Bow, Midship, Stern, Appendages
- Tooltips showing parameter name, value, unit, min/max
- Comparison mode: overlay multiple candidates

**Implementation**:
```typescript
interface ShipDParameterChartProps {
  candidate: CandidateDesign;
  metadata: ShipDParameterMetadata[];
  showComparison?: CandidateDesign[];  // For multi-candidate view
  highlightGroup?: 'principal' | 'bow' | 'midship' | 'stern' | 'appendages';
}
```

#### 3.3.2 Create Geometry Details Panel
**File**: `frontend/src/components/sizing/visualization/GeometryDetailsPanel.tsx` (NEW)

**Purpose**: Show ShipD-specific geometry features

**Sections**:
1. **Section Geometry** (Image 1):
   - Flare angle visualization (degrees)
   - Deadrise angle visualization (degrees)
   - Chine type indicator (hard/soft icon)
   - Curvature type (convex/concave indicator)
   - Tumblehome toggle status

2. **Longitudinal Proportions** (Image 2):
   - Lb, Lm, Ls ratios as stacked bar chart
   - Visual representation of bow/mid/stern boundaries
   - Bow rake angle
   - Stern rake angle

3. **Bulb Geometry** (Image 3) - only if present:
   - Bulb length, width, height ratios
   - Asymmetry factor visualization
   - Fillet radius indicator
   - 3D bulb preview

#### 3.3.3 Update CandidateCard to Show ShipD Features
**File**: `frontend/src/components/sizing/CandidateCard.tsx`

**Changes**:
- Add "ShipD Geometry" badge when `shipdParametersJson` is present
- Show key ShipD parameters in compact format:
  - Flare: 15°, Deadrise: 30°, Hard Chine
  - Longitudinal: Lb=0.32, Lm=0.26, Ls=0.42
  - Bulb: L=0.10, W=0.50 (if present)
- Link to expanded geometry details panel

#### 3.3.4 Create ShipD Comparison View
**File**: `frontend/src/components/sizing/visualization/ShipDComparisonView.tsx` (NEW)

**Purpose**: Compare multiple candidates' ShipD parameters side-by-side

**Features**:
- Parallel coordinate plot for all 45 parameters
- Highlight differences between candidates
- Filter by parameter group
- Export comparison data

### Phase 3.4: Backend Integration Updates

#### 3.4.1 Update SizingRunService
**File**: `backend/HullSizingService/Services/SizingRunService.cs`

**Changes**:
- Ensure `ShipdParametersJson` is populated on `CandidateDesign` entities
- Pass ShipD vector to solver when available
- Store generated geometry in `GeometryJson`

#### 3.4.2 Update CandidateDesignDto
**File**: `backend/Shared/DTOs/Sizing/CandidateDesignDto.cs`

**Changes**:
- Ensure `ShipdParametersJson` is included in response
- Add helper method to parse ShipD vector
- Add validation that geometry matches ShipD when both present

### Phase 3.5: Testing Strategy

#### 3.5.1 Unit Tests
**File**: `backend/HullSizingService.Tests/Services/Geometry/ShipDHullGeometryServiceTests.cs` (NEW)

**Test Cases**:
1. `GenerateSections_WithBulbousBow_IncludesBulbGeometry`
2. `GenerateSections_WithHardChine_ProducesSharpTransitions`
3. `GenerateSections_WithFlareAngle_AppliesCorrectFlare`
4. `GenerateSections_WithLongitudinalRatios_RespectsLbLmLs`
5. `ValidateParameters_WithInvalidVector_ReturnsErrors`
6. `GenerateMesh_ProducesWatertightGeometry`

#### 3.5.2 Integration Tests
**File**: `backend/HullSizingService.Tests/Integration/ShipDGeometryIntegrationTests.cs` (NEW)

**Test Cases**:
1. `CreateSizingRun_WithShipDParameters_GeneratesShipDGeometry`
2. `CreateSizingRun_WithBulbousBow_IncludesBulbInGeometry`
3. `CreateSizingRun_WithDifferentFamilies_ProducesDistinctShapes`
4. `CreateSizingRun_WithInvalidParameters_FallsBackToParametric`

#### 3.5.3 E2E Tests
**File**: `frontend/e2e/shipd-visualization.spec.ts` (NEW)

**Test Cases**:
1. `candidate with ShipD parameters shows ShipD geometry in 3D view`
2. `candidate with bulb shows bulb in sections view`
3. `parameter chart displays all 45 parameters correctly`
4. `geometry details panel shows correct flare/deadrise values`
5. `comparison view highlights differences between candidates`

### Phase 3.6: Performance Optimization

#### 3.6.1 Geometry Caching
- Cache generated ShipD geometry by parameter vector hash
- Invalidate cache when metadata version changes
- Use Web Workers for geometry generation in frontend

#### 3.6.2 Lazy Loading
- Load ShipD geometry only when candidate is viewed
- Progressive rendering: show parametric first, upgrade to ShipD when ready
- Thumbnail vs full resolution modes

### Phase 3.7: Documentation

#### 3.7.1 User Documentation
- Explain ShipD geometry vs parametric geometry
- Show examples of different bow/mid/stern combinations
- Guide on interpreting parameter charts

#### 3.7.2 Developer Documentation
- Document ShipD parameter index mappings
- Explain geometry generation algorithm
- Performance considerations and optimization strategies

## Implementation Order

### Sprint 1: Backend Geometry Generator
1. ✅ Create `IShipDHullGeometryService` interface
2. ✅ Implement `ShipDHullGeometryService` with core section generation
3. ✅ Add bulb geometry generation
4. ✅ Integrate into `FirstPrinciplesSolver`
5. ✅ Write unit tests

### Sprint 2: Frontend Geometry Generator
1. ✅ Create `shipdGeometryGenerator.ts` utility
2. ✅ Update `ParametricHull3D` to use ShipD when available
3. ✅ Update `Hull2DSections` and `Hull2DProfile` for ShipD
4. ✅ Add fallback logic for backward compatibility
5. ✅ Write unit tests

### Sprint 3: Enhanced Visualization
1. ✅ Create `ShipDParameterChart` component
2. ✅ Create `GeometryDetailsPanel` component
3. ✅ Create `ShipDComparisonView` component
4. ✅ Update `CandidateCard` with ShipD features
5. ✅ Write E2E tests

### Sprint 4: Integration & Polish
1. ✅ End-to-end testing
2. ✅ Performance optimization
3. ✅ Documentation
4. ✅ User acceptance testing

## Success Criteria

1. ✅ Generated hulls visually match ShipD taxonomy shapes
2. ✅ Bulb geometry appears when bulbous bow is selected
3. ✅ Flare, deadrise, and chine type are visible in sections
4. ✅ Longitudinal proportions (Lb, Lm, Ls) are respected
5. ✅ Parameter charts show all 45 ShipD parameters
6. ✅ Geometry details panel displays ShipD-specific features
7. ✅ Backward compatibility: parametric geometry still works
8. ✅ Performance: geometry generation < 500ms for typical hulls

## Risk Mitigation

1. **Complexity**: ShipD geometry generation is complex - use ShipD repo as reference
2. **Performance**: Cache generated geometry, use Web Workers for frontend
3. **Accuracy**: Validate against ShipD repo's Python implementation
4. **Backward Compatibility**: Maintain parametric fallback for candidates without ShipD

## Future Enhancements

1. Real-time geometry preview as user adjusts ShipD parameters
2. Export ShipD geometry to STL/STEP formats
3. Import ShipD geometry from external sources
4. AI suggestions for optimal ShipD parameter combinations
5. Visual diff tool showing geometry changes between parameter sets

