# Hull Generators

## Form Coefficient Hull Generator

### Overview

The `FormCoefficientHullGenerator` generates realistic hull offsets from form coefficients (Cb, Cp, Cm, Cwp, LCB) using parametric methods. This replaces the simplistic Wigley formula with a more sophisticated approach that directly uses solver outputs.

### Current Status

**Phase 1 (Initial Implementation)**: ✅ Complete
- Core generator implemented
- Form coefficient-based parametric generation
- Integration with HullTestData
- Comprehensive unit tests

**Accuracy**: 
- Current implementation produces reasonable hull forms
- Form coefficient accuracy: ~10% (will be improved in Phase 3)
- Volume accuracy: ~5%
- LCB accuracy: ~5% of length

**Note**: This is a first implementation. Phase 3 (BSRA calibration) will improve accuracy to ±0.5% through calibration against industry-standard data.

### Algorithm

1. **Sectional Area Curve**: Generated from Cp and LCB using raised cosine base function
2. **Section Shapes**: Generated from Cm using parametric profiles (U vs V shape control)
3. **Waterline Half-Breadths**: Generated from Cwp using parametric planform curves
4. **Offset Combination**: Iteratively combines all three to match sectional area targets

### Usage

```csharp
var generator = new FormCoefficientHullGenerator();
var dims = new HullDimensions(length: 200m, beam: 32m, draft: 12m, lcbPercent: 2.0m);
var geometry = generator.Generate(dims, cb: 0.80m, cp: 0.82m, cm: 0.99m, cwp: 0.87m);

// geometry.Stations - List of station positions
// geometry.Waterlines - List of waterline heights
// geometry.Offsets - 2D grid: offsets[stationIndex][waterlineIndex]
// geometry.ComputedCoefficients - Validation results
```

### Vessel Type Support

The generator supports all vessel types:
- **Container**: Moderate Cb (0.65), fine ends
- **Tanker**: High Cb (0.80), full form, U-sections
- **Bulker**: Moderate-high Cb (0.75)
- **Fishing**: Moderate Cb (0.60)
- **Fast Ferry**: Low Cb (0.50), fine form, V-sections

### Next Steps

- **Phase 2**: Solver integration (automatic offset generation)
- **Phase 3**: BSRA/Series 60 calibration (improve accuracy to ±0.5%)
- **Phase 4**: Vessel type presets
