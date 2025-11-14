# Hydrostatics Module Architecture

**Module**: Hydrostatics Analysis  
**Service**: DataService  
**Schema**: `data`  
**Status**: Phase 1 - In Development

---

## Overview

The Hydrostatics Module provides naval architecture analysis capabilities for intact hydrostatics at static equilibrium. It enables users to define vessel hull geometry, compute hydrostatic parameters, generate curves, and export professional reports.

---

## Database Schema

### Entity Relationship Diagram

```
┌──────────────┐
│    users     │
│ (Identity)   │
└───────┬──────┘
        │
        │ 1:N
        │
┌───────▼────────┐
│    Vessel      │ ← Main entity
│  ──────────    │
│  • id (PK)     │
│  • user_id     │
│  • name        │
│  • lpp         │
│  • beam        │
│  • design_draft│
└───┬──────┬─────┘
    │      │
    │ 1:N  │ 1:N
    │      │
    │  ┌───▼─────────┐
    │  │  Loadcase   │
    │  │ ──────────  │
    │  │ • rho       │
    │  │ • kg        │
    │  └──┬──────────┘
    │     │ 1:N
    │     │
    ├─────┼──────┬──────┐
    │     │      │      │
┌───▼────┐  ┌───▼────┐  │
│Station │  │Waterline  │
│────────│  │──────────│  │
│• index │  │• index   │  │
│• x     │  │• z       │  │
└────────┘  └──────────┘  │
    │           │          │
    └─────┬─────┘          │
          │ N:N            │
      ┌───▼────┐       ┌───▼────────┐
      │ Offset │       │ HydroResult│
      │────────│       │────────────│
      │• sta_idx│      │• draft     │
      │• wl_idx │      │• disp      │
      │• y      │      │• kb, lcb   │
      └────────┘       │• gm, bm    │
                       │• cb, cp    │
                       └────────────┘
```

### Tables

#### Vessels

```sql
CREATE TABLE vessels (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    lpp DECIMAL(10,3),          -- Length (m)
    beam DECIMAL(10,3),          -- Breadth (m)
    design_draft DECIMAL(10,3),  -- Draft (m)
    units_system VARCHAR(10) DEFAULT 'SI',
    created_at TIMESTAMP,
    updated_at TIMESTAMP,
    deleted_at TIMESTAMP         -- Soft delete
);
```

#### Stations

```sql
CREATE TABLE stations (
    id UUID PRIMARY KEY,
    vessel_id UUID REFERENCES vessels,
    station_index INT NOT NULL,
    x DECIMAL(10,4) NOT NULL,    -- Position (m)
    UNIQUE(vessel_id, station_index)
);
```

#### Waterlines

```sql
CREATE TABLE waterlines (
    id UUID PRIMARY KEY,
    vessel_id UUID REFERENCES vessels,
    waterline_index INT NOT NULL,
    z DECIMAL(10,4) NOT NULL,    -- Height (m)
    UNIQUE(vessel_id, waterline_index)
);
```

#### Offsets

```sql
CREATE TABLE offsets (
    id UUID PRIMARY KEY,
    vessel_id UUID REFERENCES vessels,
    station_index INT NOT NULL,
    waterline_index INT NOT NULL,
    half_breadth_y DECIMAL(10,4) NOT NULL,  -- Half-breadth (m)
    UNIQUE(vessel_id, station_index, waterline_index)
);
```

#### Loadcases

```sql
CREATE TABLE loadcases (
    id UUID PRIMARY KEY,
    vessel_id UUID REFERENCES vessels,
    name VARCHAR(255) NOT NULL,
    rho DECIMAL(10,3) NOT NULL,  -- Density (kg/m³)
    kg DECIMAL(10,3),             -- VCG (m)
    notes TEXT,
    created_at TIMESTAMP,
    updated_at TIMESTAMP
);
```

#### HydroResults

```sql
CREATE TABLE hydro_results (
    id UUID PRIMARY KEY,
    vessel_id UUID REFERENCES vessels,
    loadcase_id UUID REFERENCES loadcases,
    draft DECIMAL(10,4) NOT NULL,

    -- Volume & Displacement
    disp_volume DECIMAL(15,4),    -- Volume (m³)
    disp_weight DECIMAL(15,4),    -- Weight (kg)

    -- Centers
    kb_z DECIMAL(10,4),           -- Vertical CB (m)
    lcb_x DECIMAL(10,4),          -- Longitudinal CB (m)
    tcb_y DECIMAL(10,4),          -- Transverse CB (m)

    -- Metacentric Data
    bm_t DECIMAL(10,4),           -- Transverse BM (m)
    bm_l DECIMAL(10,4),           -- Longitudinal BM (m)
    gm_t DECIMAL(10,4),           -- Transverse GM (m)
    gm_l DECIMAL(10,4),           -- Longitudinal GM (m)

    -- Waterplane
    awp DECIMAL(12,4),            -- Area (m²)
    iwp DECIMAL(15,4),            -- Second moment (m⁴)

    -- Form Coefficients
    cb DECIMAL(6,4),              -- Block
    cp DECIMAL(6,4),              -- Prismatic
    cm DECIMAL(6,4),              -- Midship
    cwp DECIMAL(6,4),             -- Waterplane

    trim_angle DECIMAL(6,3),      -- Degrees
    meta JSONB,                   -- Additional data
    created_at TIMESTAMP
);
```

#### Curves

```sql
CREATE TABLE curves (
    id UUID PRIMARY KEY,
    vessel_id UUID REFERENCES vessels,
    type VARCHAR(50) NOT NULL,
    x_label VARCHAR(100),
    y_label VARCHAR(100),
    meta JSONB,
    created_at TIMESTAMP
);

CREATE TABLE curve_points (
    id UUID PRIMARY KEY,
    curve_id UUID REFERENCES curves,
    x DECIMAL(15,6) NOT NULL,
    y DECIMAL(15,6) NOT NULL,
    sequence INT NOT NULL
);
```

---

## Service Architecture

### Services Layer

```
DataService/
└── Services/
    └── Hydrostatics/
        ├── IVesselService.cs
        ├── VesselService.cs
        │   └── Vessel CRUD operations
        │
        ├── IGeometryService.cs
        ├── GeometryService.cs
        │   └── Stations, waterlines, offsets management
        │
        ├── IValidationService.cs
        ├── ValidationService.cs
        │   └── Input validation rules
        │
        ├── IIntegrationEngine.cs
        ├── IntegrationEngine.cs
        │   ├── Simpson's rule
        │   └── Trapezoidal rule
        │
        ├── IHydroCalculator.cs
        ├── HydroCalculator.cs
        │   ├── Volume integration
        │   ├── Center calculations (KB, LCB)
        │   ├── Metacentric calculations (BM, GM)
        │   └── Form coefficients (Cb, Cp, Cm, Cwp)
        │
        ├── ICurvesGenerator.cs
        ├── CurvesGenerator.cs
        │   ├── Displacement curves
        │   ├── KB/LCB curves
        │   └── Bonjean curves
        │
        ├── ITrimSolver.cs
        ├── TrimSolver.cs
        │   └── Newton-Raphson equilibrium solver
        │
        └── IExportService.cs
        └── ExportService.cs
            ├── PDF generation
            ├── Excel export
            └── CSV export
```

### Key Algorithms

#### 1. Section Area Integration

```csharp
public decimal ComputeSectionArea(int stationIndex, decimal draft)
{
    var offsets = GetOffsets(stationIndex, upTo: draft);
    var area = 0m;

    for (int i = 0; i < offsets.Count - 1; i++)
    {
        var z1 = offsets[i].Z;
        var z2 = offsets[i + 1].Z;
        var y1 = offsets[i].HalfBreadth;
        var y2 = offsets[i + 1].HalfBreadth;

        // Trapezoidal integration for strip
        area += (y1 + y2) / 2 * (z2 - z1);
    }

    return area * 2; // Mirror for full section
}
```

#### 2. Volume Integration (Simpson's Rule)

```csharp
public decimal ComputeVolume(decimal draft)
{
    var stations = GetStations();
    var areas = new List<decimal>();

    foreach (var station in stations)
    {
        areas.Add(ComputeSectionArea(station.Index, draft));
    }

    // Simpson's 1/3 rule (requires even number of intervals)
    if (areas.Count % 2 == 0)
    {
        return SimpsonsRule(stations, areas);
    }
    else
    {
        return TrapezoidalRule(stations, areas);
    }
}

private decimal SimpsonsRule(List<Station> stations, List<decimal> areas)
{
    var dx = (stations[^1].X - stations[0].X) / (areas.Count - 1);
    var sum = areas[0] + areas[^1];

    for (int i = 1; i < areas.Count - 1; i++)
    {
        sum += (i % 2 == 1 ? 4 : 2) * areas[i];
    }

    return sum * dx / 3;
}
```

#### 3. Center of Buoyancy

```csharp
public (decimal KB, decimal LCB, decimal TCB) ComputeCenters(decimal draft)
{
    var stations = GetStations();
    var volume = 0m;
    var momentX = 0m; // For LCB
    var momentZ = 0m; // For KB

    for (int i = 0; i < stations.Count - 1; i++)
    {
        var dx = stations[i + 1].X - stations[i].X;
        var area1 = ComputeSectionArea(stations[i].Index, draft);
        var area2 = ComputeSectionArea(stations[i + 1].Index, draft);
        var avgArea = (area1 + area2) / 2;

        var dVolume = avgArea * dx;
        volume += dVolume;

        // Moment arms
        var x = (stations[i].X + stations[i + 1].X) / 2;
        momentX += dVolume * x;

        var z = ComputeSectionCentroidZ(stations[i].Index, draft);
        momentZ += dVolume * z;
    }

    var LCB = momentX / volume;
    var KB = momentZ / volume;
    var TCB = 0m; // Symmetry assumption

    return (KB, LCB, TCB);
}
```

#### 4. Metacentric Radius

```csharp
public (decimal BMt, decimal BMl) ComputeMetacentricRadii(decimal draft, decimal volume)
{
    // Transverse BM = I_t / volume
    var It = ComputeTransverseSecondMoment(draft);
    var BMt = It / volume;

    // Longitudinal BM = I_l / volume
    var Il = ComputeLongitudinalSecondMoment(draft);
    var BMl = Il / volume;

    return (BMt, BMl);
}

private decimal ComputeTransverseSecondMoment(decimal draft)
{
    // I_t = ∫∫ y² dA over waterplane
    var stations = GetStations();
    var It = 0m;

    foreach (var station in stations)
    {
        var y = GetHalfBreadthAtDraft(station.Index, draft);
        var dx = GetStationSpacing(station.Index);

        // For rectangular element: I = (b³h)/12 + A*d²
        It += (2 * Math.Pow(y, 3) / 3) * dx; // Parallel axis theorem
    }

    return It;
}
```

#### 5. Form Coefficients

```csharp
public record FormCoefficients(decimal Cb, decimal Cp, decimal Cm, decimal Cwp);

public FormCoefficients ComputeCoefficients(decimal draft, decimal lpp, decimal beam)
{
    var volume = ComputeVolume(draft);
    var awp = ComputeWaterplaneArea(draft);
    var aMidship = ComputeMidshipArea(draft);

    var Cb = volume / (lpp * beam * draft);
    var Cp = volume / (aMidship * lpp);
    var Cm = aMidship / (beam * draft);
    var Cwp = awp / (lpp * beam);

    return new FormCoefficients(Cb, Cp, Cm, Cwp);
}
```

---

## API Endpoints

### Vessel Management

```
POST   /api/v1/hydrostatics/vessels
GET    /api/v1/hydrostatics/vessels
GET    /api/v1/hydrostatics/vessels/{id}
PUT    /api/v1/hydrostatics/vessels/{id}
DELETE /api/v1/hydrostatics/vessels/{id}
```

### Geometry Import

```
POST   /api/v1/hydrostatics/vessels/{id}/stations
POST   /api/v1/hydrostatics/vessels/{id}/waterlines
POST   /api/v1/hydrostatics/vessels/{id}/offsets:bulk
POST   /api/v1/hydrostatics/vessels/{id}/offsets:upload
GET    /api/v1/hydrostatics/vessels/{id}/offsets
```

### Loadcases

```
POST   /api/v1/hydrostatics/vessels/{id}/loadcases
GET    /api/v1/hydrostatics/vessels/{id}/loadcases
GET    /api/v1/hydrostatics/vessels/{id}/loadcases/{lcId}
PUT    /api/v1/hydrostatics/vessels/{id}/loadcases/{lcId}
DELETE /api/v1/hydrostatics/vessels/{id}/loadcases/{lcId}
```

### Computations

```
POST   /api/v1/hydrostatics/vessels/{id}/compute/table
POST   /api/v1/hydrostatics/vessels/{id}/compute/trim
```

### Curves

```
POST   /api/v1/hydrostatics/vessels/{id}/curves
GET    /api/v1/hydrostatics/vessels/{id}/curves/bonjean
```

### Export

```
POST   /api/v1/hydrostatics/vessels/{id}/export
GET    /api/v1/hydrostatics/templates/{type}.csv
```

---

## Data Flow

### Import Workflow

```
User uploads CSV
     │
     ▼
ValidationService
  ├── Check monotonic
  ├── Check non-negative
  └── Check completeness
     │
     ▼
GeometryService
  ├── Parse CSV
  ├── Create entities
  └── Save to DB
     │
     ▼
Success Response
```

### Computation Workflow

```
User requests hydro table
     │
     ▼
Load vessel geometry
  ├── Stations
  ├── Waterlines
  └── Offsets
     │
     ▼
For each draft:
  ├── IntegrationEngine
  │   ├── Section areas
  │   └── Volume
  │
  ├── HydroCalculator
  │   ├── Centers (KB, LCB)
  │   ├── BM, GM
  │   └── Coefficients
  │
  └── Save HydroResult
     │
     ▼
Return results table
```

---

## Validation Rules

### Stations

- ✅ Monotonically increasing X values
- ✅ Non-negative X values
- ✅ No duplicate indices
- ✅ Minimum 3 stations required

### Waterlines

- ✅ Monotonically increasing Z values
- ✅ Non-negative Z values
- ✅ No duplicate indices
- ✅ Minimum 3 waterlines required

### Offsets

- ✅ Non-negative half-breadths
- ✅ All (station, waterline) pairs must have offset
- ✅ Keel offsets (Z=0) typically zero

---

## Testing Strategy

### Unit Tests

- Integration methods (Simpson's, Trapezoidal)
- Center calculations
- Form coefficients
- Validation rules

### Reference Cases

**Rectangular Barge** (Analytical):

```
L = 100m, B = 20m, T = 5m
Expected:
  - Volume = 10,000 m³
  - KB = 2.5 m
  - BMt = 6.667 m
  - Cb = 1.0
Tolerance: <0.5%
```

**Wigley Hull** (Benchmark):

```
Parabolic hull form
Expected: Published results
Tolerance: <2%
```

### Integration Tests

- End-to-end: Upload CSV → Compute → Export
- API contract tests
- Database operations

---

## Performance Targets

- **40,000 offsets** (200 stations × 200 waterlines): <5 seconds
- **Hydrostatic table** (10 drafts): <1 second
- **Curve generation** (100 points): <2 seconds
- **PDF export**: <5 seconds

---

## Standards Compliance

### Informative References

- **IMO MSC.267(85)**: Intact Stability Code (terminology)
- **ISO 12217**: Small Craft Stability (formats)
- **ABS Rules**: Hydrostatics data structure

### Terminology

- Uses industry-standard naming (KB, LCB, GM, BM, etc.)
- Follows SI units internally
- Report formats aligned with classification societies

---

## Future Enhancements (Phase 2+)

### Phase 2

- NURBS/CAD surface import
- Heel/trim coupled hydrostatics
- GZ curves (large angles)
- Tanks & weight distribution

### Phase 3

- Damage stability
- Longitudinal strength
- Regulatory compliance checks

---

## References

1. **Rawson & Tupper** - "Basic Ship Theory" (5th ed.)
2. **Schneekluth & Bertram** - "Ship Design for Efficiency"
3. **Wigley (1942)** - "Wave Profiles" benchmark

---

**Last Updated**: October 25, 2025  
**Status**: Phase 1 - Foundation Complete  
**Next**: Service Implementation
