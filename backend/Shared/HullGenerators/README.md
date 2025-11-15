# Hull Generator System

## Overview

The hull generator system provides two complementary approaches for generating realistic ship hull offsets:

1. **Parent Hull Method** (Primary) - Uses reference hull forms from BSRA/Series 60, scales and adjusts them
2. **Parametric Method** (Fallback) - Generates hulls from form coefficients using mathematical formulas

## Architecture

### Core Components

- **HullGeneratorFactory** - Smart factory that selects the appropriate generator
- **ParentHullHullGenerator** - Primary generator using parent hull scaling
- **FormCoefficientHullGenerator** - Parametric fallback generator
- **VesselTypeSpecific Generators** - Specialized generators for each vessel type

### Supporting Components

- **ParentHullLoader** - Loads parent hull data from CSV files
- **ParentHullScaler** - Scales parent hulls to target dimensions
- **LCBSectionSwing** - Adjusts LCB by swinging sections
- **CubicSplineFairing** - Smooths hull lines using clamped cubic splines
- **BSRASimpsonIntegration** - Accurate numerical integration with BSRA multipliers
- **VesselTypeMapper** - Maps ShipdType/VesselType to registry types

## Usage

### Basic Usage

```csharp
// Use factory to get appropriate generator
var factory = new HullGeneratorFactory();
var generator = factory.GetGenerator("product_carrier", 0.80m);

// Generate hull geometry
var dims = new HullDimensions(185m, 28m, 12.87m, 2.08m);
var geometry = generator.Generate(dims, 0.80m, 0.82m, 0.99m, 0.87m);
```

### Vessel Type Mapping

The system automatically maps common vessel type identifiers:

- `"product_carrier"`, `"Product Carrier"` → `product_carrier`
- `"tanker"`, `"Oil Tanker"` → `tanker`
- `"container"`, `"Container Ship"` → `container`
- `"bulk_carrier"`, `"Bulk Carrier"` → `bulk_carrier`
- `"general_cargo"`, `"General Cargo"`, `"Multi-purpose"` → `general_cargo`

### Direct Generator Usage

```csharp
// Use parent hull generator directly
var generator = new ParentHullHullGenerator(null, "product_carrier");
var geometry = generator.Generate(dims, cb, cp, cm, cwp);

// Use parametric generator directly
var parametricGenerator = new FormCoefficientHullGenerator();
var geometry = parametricGenerator.Generate(dims, cb, cp, cm, cwp);
```

## Data Files

### Parent Hull Registry

Location: `backend/Shared/Data/BSRA/parent_hulls_registry.csv`

Contains metadata about available parent hulls:
- Vessel type
- Block coefficient (Cb)
- Principal dimensions (Lbp, B, D, T)
- Form coefficients (Cm, Cw, LCB%)

### Parent Hull Offsets

Location: `backend/Shared/Data/BSRA/parent_hulls/{vessel_type}_cb{cb}_offsets.csv`

Contains actual offset tables (half-breadths) for each parent hull.

### File Path Resolution

The system searches for data files in the following order:

1. `NAVARCH_DATA_DIR` environment variable (production)
2. Assembly directory (production deployment)
3. AppDomain base directory (production)
4. Current working directory (development)
5. Solution root (development)

## Vessel Type Specific Generators

Each vessel type can have a specialized generator with type-specific defaults:

- **ProductCarrierHullGenerator** - Product carriers
- **ContainerShipHullGenerator** - Container ships
- **TankerHullGenerator** - Tankers
- **BulkCarrierHullGenerator** - Bulk carriers
- **GeneralCargoHullGenerator** - General cargo vessels

These generators inherit from `ParentHullHullGenerator` and provide:
- Type-specific default LCB percentages
- Parameter range validation
- Vessel-type-specific optimizations

## Adding New Parent Hulls

1. Add entry to `parent_hulls_registry.csv`
2. Create offset CSV file: `parent_hulls/{vessel_type}_cb{cb}_offsets.csv`
3. Format: First row is header with `station,wl_1,wl_2,...`, subsequent rows are station data

## Adding New Vessel Types

1. Add vessel type ranges to `BSRAConstants.VesselTypeRanges`
2. Create vessel-type-specific generator class
3. Add mapping in `VesselTypeMapper.MapToRegistryType`
4. Register in `HullGeneratorFactory.CreateVesselTypeSpecificGenerator`

## Testing

### Unit Tests

Located in `backend/Shared.Tests/HullGenerators/`

- **Fairing/** - Cubic spline fairing tests
- **Integration/** - BSRA Simpson integration tests
- **ParentHull/** - Parent hull loader and generator tests
- **Integration/** - End-to-end integration tests

### Running Tests

```bash
# Run all tests
dotnet test

# Run only unit tests (exclude integration)
dotnet test --filter "Category!=Integration"

# Run only integration tests
dotnet test --filter "Category=Integration"
```

## Production Deployment

### Environment Variables

Set `NAVARCH_DATA_DIR` to point to the directory containing the `Data/BSRA/` folder:

```bash
export NAVARCH_DATA_DIR=/path/to/data
```

### Data File Structure

```
NAVARCH_DATA_DIR/
  Data/
    BSRA/
      parent_hulls_registry.csv
      parent_hulls/
        product_carrier_cb080_offsets.csv
        ...
```

## Performance Considerations

- Parent hull data is cached after first load
- BSRA Simpson integration is optimized for 23-station layout
- Cubic spline fairing uses MathNet.Numerics for efficient matrix operations

## Accuracy

- **Parent Hull Method**: ±2% Cb accuracy (when parent hull available)
- **Parametric Method**: ±5-10% Cb accuracy (calibration in progress)

## References

- BSRA Series documentation
- Series 60 systematic hull series
- MATLAB code from naval architecture literature
