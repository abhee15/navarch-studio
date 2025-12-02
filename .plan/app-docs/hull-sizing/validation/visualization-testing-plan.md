# Visualization Testing Plan - Hull Sizing Module

## Overview

This document outlines comprehensive testing requirements for frontend visualization components (Plan View, Profile View, Sections View, 3D Isometric) and their integration with backend geometry generation.

## Objectives

1. Ensure accurate geometric representation of hull forms
2. Validate data flow from backend to frontend
3. Handle edge cases gracefully (missing/invalid geometry, extreme dimensions)
4. Provide professional-quality visualizations for naval architects
5. Maintain consistency across all view types

## Test Categories

### 1. Geometry Data Flow Tests

#### 1.1 Backend to Frontend Data Pipeline
- [ ] Verify `CandidateDesign.GeometryJson` is properly serialized in backend
- [ ] Verify `CandidateDesignDto.GeometryJson` is correctly mapped
- [ ] Verify frontend receives geometry JSON in API responses
- [ ] Verify geometry JSON format matches expected structure (OffsetsGrid or ShipD)

#### 1.2 Geometry Format Conversion
- [ ] Test ShipD to OffsetsGrid conversion
- [ ] Test OffsetsGrid normalization
- [ ] Test handling of both camelCase and PascalCase property names
- [ ] Test format detection logic

### 2. Plan View (Hull2DPlan) Tests

#### 2.1 Basic Rendering
- [ ] Verify waterlines are correctly extracted and displayed
- [ ] Verify station markers are correctly positioned
- [ ] Verify centerline and perpendiculars are accurate
- [ ] Verify dimensions annotations are correct

#### 2.2 Geometry Accuracy
- [ ] Test with actual candidate geometry (OffsetsGrid format)
- [ ] Test with ShipD geometry format
- [ ] Test with parametric fallback geometry
- [ ] Verify waterline curves match hull form

#### 2.3 Edge Cases
- [ ] Missing geometry JSON
- [ ] Invalid geometry JSON
- [ ] Empty stations/waterlines arrays
- [ ] Extreme aspect ratios (very long/narrow, very wide/short)
- [ ] Invalid dimensions (NaN, negative, zero)

#### 2.4 Coordinate System
- [ ] Verify plan view coordinate transformation (top-down projection)
- [ ] Verify starboard/port symmetry
- [ ] Verify centerline alignment
- [ ] Verify scaling and viewport bounds

### 3. Profile View (Hull2DProfile) Tests

#### 3.1 Basic Rendering
- [ ] Verify buttock curves are correctly extracted
- [ ] Verify sheerline is correctly displayed
- [ ] Verify waterline is at correct draft level
- [ ] Verify baseline is correctly positioned

#### 3.2 Geometry Accuracy
- [ ] Test with actual candidate geometry
- [ ] Verify buttock extraction from OffsetsGrid
- [ ] Verify sheerline extraction
- [ ] Test with parametric fallback

#### 3.3 Edge Cases
- [ ] Missing geometry
- [ ] Invalid geometry
- [ ] Extreme draft-to-beam ratios
- [ ] Missing depth/draft values

### 4. Sections View (Hull2DSections) Tests

#### 4.1 Basic Rendering
- [ ] Verify station sections are correctly displayed
- [ ] Verify port/starboard mirroring
- [ ] Verify section ordering (forward to aft)
- [ ] Verify section curve smoothness

#### 4.2 Geometry Accuracy
- [ ] Test with actual candidate geometry
- [ ] Verify section extraction from OffsetsGrid
- [ ] Verify bulbous bow sections (if present)
- [ ] Test with parametric fallback

#### 4.3 Edge Cases
- [ ] Missing geometry
- [ ] Invalid geometry
- [ ] Sections with extreme shapes
- [ ] Missing station data

### 5. 3D Isometric View (Hull3DScene) Tests

#### 5.1 Basic Rendering
- [ ] Verify hull mesh is correctly generated
- [ ] Verify waterplane is at correct draft
- [ ] Verify centers (B, G) are correctly positioned
- [ ] Verify grid alignment

#### 5.2 Geometry Accuracy
- [ ] Test with actual candidate geometry
- [ ] Verify 3D mesh generation from OffsetsGrid
- [ ] Verify 3D mesh generation from ShipD geometry
- [ ] Test with parametric fallback
- [ ] Verify hull symmetry (port/starboard)

#### 5.3 Performance
- [ ] Test with large geometries (50+ stations, 20+ waterlines)
- [ ] Verify frame rate is acceptable (>30 FPS)
- [ ] Test camera controls responsiveness
- [ ] Test geometry updates performance

#### 5.4 Edge Cases
- [ ] Missing geometry
- [ ] Invalid geometry
- [ ] Extreme dimensions
- [ ] Geometry with holes or gaps

### 6. Viewport Synchronization Tests

#### 6.1 Quad Layout
- [ ] Verify all four views render simultaneously
- [ ] Verify viewport switching works correctly
- [ ] Verify maximized view mode
- [ ] Verify keyboard shortcuts

#### 6.2 Cross-View Consistency
- [ ] Verify dimensions match across views
- [ ] Verify geometry consistency (same hull shown in all views)
- [ ] Verify scaling is appropriate for each view

### 7. Error Handling Tests

#### 7.1 Missing Geometry
- [ ] Verify graceful error messages
- [ ] Verify fallback rendering (if applicable)
- [ ] Verify error state UI

#### 7.2 Invalid Geometry
- [ ] Verify validation catches invalid data
- [ ] Verify error messages are helpful
- [ ] Verify partial rendering (if applicable)

#### 7.3 Geometry Generation Failures
- [ ] Verify handling of `GeometryGenerationStatus.BothFailed`
- [ ] Verify handling of `GeometryGenerationStatus.FormCoefficientFailed`
- [ ] Verify error messages from `GeometryGenerationError`

### 8. Data Validation Tests

#### 8.1 Offsets Grid Validation
- [ ] Verify NaN detection and sanitization
- [ ] Verify negative value handling
- [ ] Verify ordering validation (stations, waterlines)
- [ ] Verify empty geometry detection
- [ ] Verify extreme value warnings

#### 8.2 Dimension Validation
- [ ] Verify Lpp, Beam, Draft are within reasonable ranges
- [ ] Verify aspect ratio warnings
- [ ] Verify dimension consistency

### 9. Chart Data Validation

#### 9.1 Hydrostatics Charts
- [ ] Verify chart data matches backend calculations
- [ ] Verify curve smoothness
- [ ] Verify axis scaling

#### 9.2 Resistance Charts
- [ ] Verify resistance data matches backend
- [ ] Verify speed-EHP relationships

### 10. Integration Tests

#### 10.1 End-to-End Workflow
- [ ] Create mission case → Generate sizing run → View candidates → Verify all views render
- [ ] Test with multiple candidates
- [ ] Test candidate selection and switching

#### 10.2 Backend-Frontend Consistency
- [ ] Verify geometry from backend matches frontend visualization
- [ ] Verify dimensions match (Lpp, Beam, Draft, etc.)
- [ ] Verify form coefficients match displayed geometry

## Test Data Requirements

### Reference Test Cases
1. **Calibration Case**: 40,000 DWT Product Carrier (known good geometry)
2. **TC-A**: Bulk Carrier (high Cb)
3. **TC-B**: General Cargo (moderate Cb)
4. **TC-C**: Fast Container Ship (low Cb)

### Edge Case Test Data
1. Missing geometry JSON
2. Invalid JSON structure
3. Empty arrays
4. NaN values in geometry
5. Negative dimensions
6. Extreme aspect ratios
7. Very large geometries (performance test)

## Validation Criteria

### Accuracy Requirements
- Plan view waterlines should match actual hull form within ±2% visually
- Profile view buttocks should be smooth and continuous
- Sections view should show correct body plan structure
- 3D view should accurately represent hull shape

### Performance Requirements
- Initial render: < 1 second for typical geometry
- Interactive frame rate: > 30 FPS
- Viewport switching: < 100ms

### Error Handling Requirements
- All invalid geometry should show helpful error messages
- No crashes or blank screens on invalid data
- Warnings should be non-blocking

## Implementation Status

- [x] Geometry validation utility created
- [x] Geometry validation wrapper component created
- [x] Backend geometry generation integration tests created
- [ ] Frontend visualization component tests (in progress)
- [ ] End-to-end integration tests (planned)
- [ ] Performance optimization (planned)

## Next Steps

1. Integrate validation wrapper into visualization components
2. Create comprehensive unit tests for each view component
3. Create end-to-end integration tests with real candidate data
4. Performance profiling and optimization
5. User acceptance testing with naval architects

