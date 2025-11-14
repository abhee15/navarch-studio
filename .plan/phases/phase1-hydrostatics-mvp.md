# Phase 1 — Hydrostatics MVP (NavArch Studio)

**Goal**: Deliver a usable, credible hydrostatics module focused on intact-hydrostatics at static equilibrium for monohull surface vessels.

**Timeline**: 2-4 sprints (4-8 weeks)

- Sprints 1-2: Core compute & basic curves
- Sprints 3-4: Stability summaries & reporting

**Last Updated**: October 25, 2025

---

## Table of Contents

1. [Core Functionality](#1-core-functionality-mvp-scope)
2. [Hull Input](#2-hull-input-ingest--modeling)
3. [Outputs & Visualization](#3-outputs--visualization)
4. [User Stories](#4-user-stories)
5. [Technical Constraints](#5-technical--standards-constraints)
6. [Data Model](#6-data-model-initial-schema)
7. [Backend Services](#7-backend-services-hydrostaticsservice)
8. [REST API](#8-rest-api-first-pass)
9. [Acceptance Criteria](#9-acceptance-criteria)
10. [UI Outline](#10-ui-outline-mvp)
11. [Test Plan](#11-test-plan)
12. [Implementation Roadmap](#12-implementation-roadmap)
13. [Backlog](#13-backlog-nice-to-have--phase-15)

---

## 1) Core Functionality (MVP scope)

### 1.1 Hydrostatic Primitives at Arbitrary Drafts / Waterlines

**Primary Calculations:**

- **Displacement (∆)**: Volume + weight via ρ
- **Waterplane Area (Awp)**: Area of vessel at waterline
- **Waterplane Second Moment (Iwp)**: For metacentric calculations
- **Center of Buoyancy (KB)**: Vertical center of buoyancy
  - KBx: Longitudinal component
  - KBy: Athwartship component
  - KBz: Vertical component
- **Longitudinal Center of Buoyancy (LCB)**: Fore/aft position of CB
- **Transverse Center of Buoyancy (TCB)**: Port/starboard position of CB
- **Metacentric Radii**:
  - BMt (transverse)
  - BMl (longitudinal)
- **Initial Metacentric Heights** (given KG):
  - GMt (transverse)
  - GMl (longitudinal)
- **Form Coefficients**:
  - Cp: Prismatic coefficient
  - Cb: Block coefficient
  - Cwp: Waterplane coefficient
  - Cm: Midship coefficient

**Curves:**

- **Bonjean Curves**: Sectional areas vs. draft per station
- **Curves of Form**:
  - ∆(T): Displacement vs. draft
  - LCB(T): Longitudinal center of buoyancy vs. draft
  - KB(T): Center of buoyancy height vs. draft
  - BM(T): Metacentric radius vs. draft
  - Awp(T): Waterplane area vs. draft

### 1.2 Hydrostatic Curves / Tables

**Hydrostatic Table Columns:**
For each draft Ti, compute:

- T: Draft
- ∆: Displacement (weight)
- ∇: Displacement (volume)
- Awp: Waterplane area
- KB: Vertical center of buoyancy
- LCB: Longitudinal center of buoyancy
- BMt: Transverse metacentric radius
- BMl: Longitudinal metacentric radius
- GMt: Transverse metacentric height (for given KG)
- GMl: Longitudinal metacentric height (for given KG)
- Cp: Prismatic coefficient
- Cb: Block coefficient
- Cwp: Waterplane coefficient
- Cm: Midship coefficient

**Optional (Phase 1.5):**

- KN(φ) or GZ(φ) kernels for stability (large heel angles)

### 1.3 Stability Parameters (Initial, Intact)

- **Initial GMt** at design/loaded conditions (user-provided KG)
- **Trim Solver**: Solve draft at FP/AP for specified displacement (static equilibrium in pitch)
- **Lightship vs. Loadcase**: Accept KG/KM input per condition

### 1.4 Numerics / Methods

**Integration Methods:**

- **Simpson's Rule**: Preferred for equally-spaced data
- **Composite Simpson's**: For larger datasets
- **Trapezoidal Rule**: Fallback for odd-numbered or irregular spacing

**Geometry Support:**

- Stations & waterlines grid sampling
- Simpson's or trapezoidal numerical integration for volumes & areas
- Optional NURBS/B-spline surface support (Phase 1.5)

**Density Handling:**

- Fresh/salt water density (ρ) as input per loadcase
- Default: ρ_salt = 1025 kg/m³, ρ_fresh = 1000 kg/m³

---

## 2) Hull Input (Ingest & Modeling)

### 2.1 Manual Offsets Table (MVP)

**Features:**

- UI grid editor for **Station × Waterline** offsets
- Half-breadths to centerline (y-coordinates)
- **Units**: m/ft selection; internal storage in SI
- **Symmetry**: Port/starboard mirror toggle
- In-browser editing with validation

**Validation:**

- Monotonic X (stations) and Z (waterlines)
- Non-negative breadths
- No gaps in grid
- Unit consistency

### 2.2 CSV/Excel Upload (MVP)

**Supported Templates:**

**Template 1: Offsets CSV**

```csv
station_index, station_x, waterline_index, waterline_z, half_breadth_y
0, 0.0, 0, 0.0, 0.0
0, 0.0, 1, 1.0, 2.5
1, 10.0, 0, 0.0, 0.5
1, 10.0, 1, 1.0, 3.0
...
```

**Template 2: Separate Stations & Waterlines**

```csv
# stations.csv
station_index, x
0, 0.0
1, 10.0
2, 20.0
...

# waterlines.csv
waterline_index, z
0, 0.0
1, 1.0
2, 2.0
...

# offsets.csv
station_index, waterline_index, half_breadth_y
0, 0, 0.0
0, 1, 2.5
1, 0, 0.5
...
```

**Import Wizard:**

- Drag & drop or file picker
- Auto-detect CSV format
- Unit inference (m/ft/in)
- Preview before import
- Validation with error highlighting

### 2.3 CAD Import (Phase 2)

**Deferred to Phase 2:**

- IGES/STEP hull surface import
- Auto-sectioning to produce offsets
- Support for complex surfaces

### 2.4 3D Hull Modeler (Phase 3)

**Deferred to Phase 3:**

- Spline-based hull sketcher
- Control curves (keel, sheer, chine lines)
- Interactive hull generation

---

## 3) Outputs & Visualization

### 3.1 Numerical Outputs (MVP)

**Hydrostatic Table:**

- User-specified draft grid: [T_min, T_max, step]
- OR displacement grid: [∆_min, ∆_max, step]
- Columns: All hydrostatic parameters (see §1.2)
- Units toggleable (SI ↔ Imperial)

**Trim Solution Report:**

- Target displacement ∆
- Solved drafts: T_AP, T_FP
- Longitudinal Center of Flotation (LCF)
- Moment to Change Trim (MTC)
- Trim angle (degrees)

**Coefficients Summary:**

- Cb, Cp, Cm, Cwp at each draft
- Range and optimal values highlighted

**Centers & Metacentric Data:**

- KB, LCB, TCB
- BMt, BMl
- GMt, GMl (for given KG)
- KMt, KMl

### 3.2 Curves & Plots

**Bonjean Curves:**

- X-axis: Station index or x-position
- Y-axis: Draft
- Contours: Sectional area
- One curve per station

**Curves of Form:**

- ∆(T): Displacement vs. draft
- LCB(T): Longitudinal CB vs. draft
- KB(T): Vertical CB vs. draft
- Awp(T): Waterplane area vs. draft
- GMt(T): Transverse GM vs. draft (for given KG)

**Optional Preview (Phase 1.5):**

- KN(φ): Righting arm kernel vs. heel angle

### 3.3 Visualizations

**2D Views:**

- **Body Plan**: Stations overlayed (cross-sections)
- **Sheer Plan**: Waterlines overlayed (side view)
- **Half-Breadths Plot**: Top view

**3D View:**

- Lightweight mesh reconstructed from offsets
- Current waterplane intersecting hull (translucent blue plane)
- LCB/KB points marked with labeled spheres
- Coordinate axes (X: forward, Y: port, Z: up)
- Camera controls: orbit, pan, zoom

**Interactive Features:**

- Slider to adjust draft in 3D view
- Hover tooltips on curves
- Click to highlight station/waterline

### 3.4 Reports

**Export Formats:**

- **PDF**: Engineered layout with company branding hooks
- **Excel**: Multi-sheet workbook with tables and embedded charts
- **CSV**: Raw hydrostatic tables (one file per table)
- **JSON**: Full API-compatible data dump

**Report Sections:**

1. **Cover Page**: Vessel name, date, logo placeholder
2. **Vessel Metadata**: Principal particulars, units
3. **Inputs Summary**: Stations, waterlines, loadcases, methodology
4. **Methodology Note**: Integration rules, assumptions, standards
5. **Hydrostatic Table**: Full tabulated results
6. **Curves Gallery**: 4-6 key plots
7. **Key Results Summary**: Highlighted parameters at design draft
8. **Appendices**: Offsets table, references

---

## 4) User Stories

### Ingestion & Setup

**US-1**: _As a naval architect, I can create a vessel and enter principal particulars (Lpp, B, T_design, ρ, KG), so that I can compute hydrostatics at design points._

**Acceptance Criteria:**

- Vessel creation form with fields: name, Lpp, B, T_design, units
- Validation: positive dimensions, reasonable ranges
- Vessel saved to database with unique ID

---

**US-2**: _As a user, I can paste/edit an offsets table in a grid (stations × waterlines with half‑breadths), so that I can run calculations without leaving the app._

**Acceptance Criteria:**

- Spreadsheet-like grid editor (e.g., AG Grid, Handsontable)
- Paste from Excel preserves structure
- Cell validation (numeric, non-negative)
- Undo/redo support

---

**US-3**: _As a user, I can upload CSV/Excel offsets using a provided template, so that I can import geometry from other tools._

**Acceptance Criteria:**

- CSV template downloadable from app
- Upload via drag-drop or file picker
- Import wizard shows preview and validates
- Error messages show exact row/column of issues

---

**US-4**: _As a user, I see validation errors (non‑monotonic stations, negative breadths, unit mismatch), so that my results are trustworthy._

**Acceptance Criteria:**

- Real-time validation on grid edit
- Highlighted cells with error messages
- Summary of all errors before save
- Cannot proceed with invalid data

---

### Computation

**US-5**: _As a user, I can compute a hydrostatic table at multiple drafts, so that I can see how ∆, KB, LCB, GM vary with T._

**Acceptance Criteria:**

- Input: draft range [T_min, T_max, step]
- Table computed in <5s for typical vessel (100 stations)
- Results displayed in sortable/filterable table
- All parameters per §1.2 included

---

**US-6**: _As a user, I can solve for a target displacement, so that the tool returns the equilibrium draft(s) and trim._

**Acceptance Criteria:**

- Input: target ∆ and loadcase
- Solver returns T_AP, T_FP within ±2mm of analytical
- Trim angle and LCF displayed
- Convergence in <10 iterations

---

**US-7**: _As a user, I can set water density and KG per loadcase, so that results reflect freshwater/saltwater and stowage changes._

**Acceptance Criteria:**

- Loadcase editor with fields: name, ρ, KG, notes
- Multiple loadcases per vessel
- Hydrostatic computations use selected loadcase
- Unit-aware inputs (kg/m³, lb/ft³)

---

### Visualization & Reporting

**US-8**: _As a user, I can view Bonjean curves and key hydrostatic curves (∆(T), KB(T), LCB(T), GM(T)), so that I can analyze sensitivity to draft._

**Acceptance Criteria:**

- Curve selector: checkboxes for each curve type
- Plots render in <300ms for 100-point grids
- Export curves as PNG/SVG/CSV
- Interactive tooltips show exact values

---

**US-9**: _As a user, I can view a 3D hull preview with the current waterline and centers marked, so that I can visually verify the geometry and results._

**Acceptance Criteria:**

- 3D view using Three.js or similar
- Draft slider updates waterplane in real-time
- LCB/KB markers positioned correctly
- Performance: 60fps for typical mesh

---

**US-10**: _As a user, I can export PDF/Excel/CSV reports, so that I can share results with teammates and include them in submissions._

**Acceptance Criteria:**

- Export dialog with format selection and options
- PDF includes all sections per §3.4
- Excel includes tables and embedded charts
- CSV exports raw data tables
- JSON exports full vessel + results
- Deterministic: same inputs → identical files

---

### Quality & Governance

**US-11**: _As a reviewer, I can see a Methods & Assumptions section in the report (integration rules, station spacing, symmetry), so that results are auditable._

**Acceptance Criteria:**

- Report includes methodology section
- Lists integration method used (Simpson's/Trapezoidal)
- Documents station/waterline counts and spacing
- Notes symmetry assumptions
- Cites relevant standards (IMO, ISO, ABS)

---

**US-12**: _As a QA engineer, I can run unit tests against reference hulls (e.g., rectangular barge, Wigley hull), so that numerical accuracy is verified._

**Acceptance Criteria:**

- Test suite includes reference geometries
- Barge: analytical solutions (∆, KB, BM, GM)
- Wigley hull: published benchmark data
- Tests pass with <0.5% error for barge, <2% for Wigley
- CI/CD runs tests on every commit

---

## 5) Technical & Standards Constraints

### Standards (Informative)

**IMO Intact Stability Code (MSC.267(85)):**

- Use terminology for GM, KN, GZ
- Reference for stability expectations
- Intact stability criteria awareness

**ISO 12217 (Small Craft Stability):**

- Not mandatory for ships but useful for validation formats
- Stability assessment methodology

**ABS Rules (Hydrostatics):**

- Data structure and terminology
- Report format expectations
- Professional presentation standards

### Units & Precision

**Internal Storage:**

- All values in SI units (m, kg, m³, etc.)

**Display Units:**

- SI: m, kg, m³, kg/m³
- Imperial: ft, lb, ft³, lb/ft³
- User-selectable per session

**Precision:**

- Configurable decimals per column
- Default: 3 decimals for lengths, 1 for large values (∆)

### Performance Targets

**Geometry Processing:**

- Up to 200 stations × 200 waterlines (40,000 points)
- Full hydrostatic table in <5 seconds on standard laptop
- Numerical stability with irregular spacing
- Graceful degradation with sparse meshes

**UI Responsiveness:**

- Grid editing: <50ms per cell change
- 3D view: 60fps for typical vessel
- Curve rendering: <300ms for 100-point dataset

### Determinism

- Same inputs must yield identical outputs
- No randomness or non-deterministic algorithms
- Repeatable results for auditing

### Numerics

**Integration Methods:**

- Simpson's/Composite Simpson where spacing permits
- Trapezoidal as fallback
- Centroid formulas verified against analytical shapes

**Test Cases:**

- Rectangular barge: analytical validation
- Prismatic sections: closed-form solutions
- Wigley hull: benchmark comparison

### Extensibility

**Future-Proofing:**

- Geometry interface supports NURBS/CAD sectioning
- Modular calculation engine
- Plugin architecture for new methods

---

## 6) Data Model (Initial Schema)

### Entities

#### `vessels`

```sql
CREATE TABLE vessels (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id),
    name VARCHAR(255) NOT NULL,
    description TEXT,
    lpp DECIMAL(10,3),          -- Length between perpendiculars (m)
    beam DECIMAL(10,3),          -- Maximum breadth (m)
    design_draft DECIMAL(10,3),  -- Design draft (m)
    units_system VARCHAR(10) DEFAULT 'SI', -- 'SI' or 'Imperial'
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    deleted_at TIMESTAMP NULL
);
```

#### `loadcases`

```sql
CREATE TABLE loadcases (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vessel_id UUID NOT NULL REFERENCES vessels(id) ON DELETE CASCADE,
    name VARCHAR(255) NOT NULL,
    rho DECIMAL(10,3) NOT NULL,  -- Water density (kg/m³)
    kg DECIMAL(10,3),             -- Vertical center of gravity (m)
    notes TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW()
);
```

#### `stations`

```sql
CREATE TABLE stations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vessel_id UUID NOT NULL REFERENCES vessels(id) ON DELETE CASCADE,
    station_index INTEGER NOT NULL,
    x DECIMAL(10,4) NOT NULL,     -- Longitudinal position (m)
    UNIQUE(vessel_id, station_index)
);
```

#### `waterlines`

```sql
CREATE TABLE waterlines (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vessel_id UUID NOT NULL REFERENCES vessels(id) ON DELETE CASCADE,
    waterline_index INTEGER NOT NULL,
    z DECIMAL(10,4) NOT NULL,     -- Vertical position (m)
    UNIQUE(vessel_id, waterline_index)
);
```

#### `offsets`

```sql
CREATE TABLE offsets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vessel_id UUID NOT NULL REFERENCES vessels(id) ON DELETE CASCADE,
    station_index INTEGER NOT NULL,
    waterline_index INTEGER NOT NULL,
    half_breadth_y DECIMAL(10,4) NOT NULL,  -- Half-breadth (m)
    UNIQUE(vessel_id, station_index, waterline_index),
    FOREIGN KEY (vessel_id, station_index)
        REFERENCES stations(vessel_id, station_index) ON DELETE CASCADE,
    FOREIGN KEY (vessel_id, waterline_index)
        REFERENCES waterlines(vessel_id, waterline_index) ON DELETE CASCADE
);
```

#### `hydro_results`

```sql
CREATE TABLE hydro_results (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vessel_id UUID NOT NULL REFERENCES vessels(id) ON DELETE CASCADE,
    loadcase_id UUID REFERENCES loadcases(id) ON DELETE CASCADE,
    draft DECIMAL(10,4) NOT NULL,
    disp_volume DECIMAL(15,4),        -- Displacement volume (m³)
    disp_weight DECIMAL(15,4),        -- Displacement weight (kg)
    kb_z DECIMAL(10,4),               -- Vertical center of buoyancy (m)
    lcb_x DECIMAL(10,4),              -- Longitudinal center of buoyancy (m)
    tcb_y DECIMAL(10,4),              -- Transverse center of buoyancy (m)
    bm_t DECIMAL(10,4),               -- Transverse metacentric radius (m)
    bm_l DECIMAL(10,4),               -- Longitudinal metacentric radius (m)
    gm_t DECIMAL(10,4),               -- Transverse metacentric height (m)
    gm_l DECIMAL(10,4),               -- Longitudinal metacentric height (m)
    awp DECIMAL(12,4),                -- Waterplane area (m²)
    iwp DECIMAL(15,4),                -- Waterplane second moment (m⁴)
    cb DECIMAL(6,4),                  -- Block coefficient
    cp DECIMAL(6,4),                  -- Prismatic coefficient
    cm DECIMAL(6,4),                  -- Midship coefficient
    cwp DECIMAL(6,4),                 -- Waterplane coefficient
    trim_angle DECIMAL(6,3),          -- Trim angle (degrees)
    meta JSONB,                       -- Additional metadata
    created_at TIMESTAMP DEFAULT NOW()
);
```

#### `curves`

```sql
CREATE TABLE curves (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vessel_id UUID NOT NULL REFERENCES vessels(id) ON DELETE CASCADE,
    type VARCHAR(50) NOT NULL,        -- 'displacement', 'kb', 'lcb', 'gm', 'bonjean', etc.
    x_label VARCHAR(100),
    y_label VARCHAR(100),
    meta JSONB,
    created_at TIMESTAMP DEFAULT NOW()
);
```

#### `curve_points`

```sql
CREATE TABLE curve_points (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    curve_id UUID NOT NULL REFERENCES curves(id) ON DELETE CASCADE,
    x DECIMAL(15,6) NOT NULL,
    y DECIMAL(15,6) NOT NULL,
    sequence INTEGER NOT NULL         -- For ordering points
);
```

#### `files`

```sql
CREATE TABLE files (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    vessel_id UUID NOT NULL REFERENCES vessels(id) ON DELETE CASCADE,
    type VARCHAR(50) NOT NULL,        -- 'offsets_csv', 'report_pdf', 'report_excel', etc.
    uri TEXT NOT NULL,                -- S3 URI or file path
    meta JSONB,
    created_at TIMESTAMP DEFAULT NOW()
);
```

### Indexes

```sql
CREATE INDEX idx_vessels_user_id ON vessels(user_id);
CREATE INDEX idx_loadcases_vessel_id ON loadcases(vessel_id);
CREATE INDEX idx_stations_vessel_id ON stations(vessel_id);
CREATE INDEX idx_waterlines_vessel_id ON waterlines(vessel_id);
CREATE INDEX idx_offsets_vessel_id ON offsets(vessel_id);
CREATE INDEX idx_hydro_results_vessel_id ON hydro_results(vessel_id);
CREATE INDEX idx_hydro_results_loadcase_id ON hydro_results(loadcase_id);
CREATE INDEX idx_curves_vessel_id ON curves(vessel_id);
CREATE INDEX idx_curve_points_curve_id ON curve_points(curve_id);
CREATE INDEX idx_files_vessel_id ON files(vessel_id);
```

### Notes

- All numeric storage in SI units
- Convert on ingest/export based on `units_system`
- `meta` JSONB columns for extensibility
- Soft delete on vessels via `deleted_at`
- Foreign keys ensure referential integrity

---

## 7) Backend Services (HydrostaticsService)

### Service Architecture

```
HydrostaticsService/
├── GeometryService           # Validate & store geometry
├── IntegrationEngine         # Numerical integration
├── HydroCalculator           # Core hydrostatic calculations
├── CurvesGenerator           # Generate curves data
├── TrimSolver                # Solve for equilibrium
├── ExportService             # PDF/Excel/CSV generation
└── ValidationService         # Input validation
```

### Key Algorithms

#### 1. Section Area Integration

**For each station:**

```
foreach station_i:
    foreach waterline pair (z_j, z_{j+1}):
        y_j = half_breadth at (station_i, waterline_j)
        y_{j+1} = half_breadth at (station_i, waterline_{j+1})

        # Trapezoidal integration for area strip
        strip_area = (y_j + y_{j+1}) / 2 * (z_{j+1} - z_j)

    section_area[i] = sum(strip_areas) * 2  # Mirror for full section
```

#### 2. Volume Integration

**Simpson's Rule (preferred):**

```
# For even number of stations
volume = (dx/3) * (A_0 + 4*A_1 + 2*A_2 + 4*A_3 + ... + A_n)

where:
- dx = station spacing
- A_i = sectional area at station i
```

**Trapezoidal Rule (fallback):**

```
volume = sum((A_i + A_{i+1}) / 2 * (x_{i+1} - x_i))
```

#### 3. Center of Buoyancy

**Vertical KB:**

```
KB = (sum of (z_centroid_i * area_i * dx)) / volume

where:
- z_centroid_i = centroid height of section i
- area_i = sectional area at station i
```

**Longitudinal LCB:**

```
LCB = (sum of (x_i * area_i * dx)) / volume
```

#### 4. Metacentric Radius

**Transverse BM:**

```
BM_t = I_t / volume

where:
- I_t = transverse second moment of waterplane area
- I_t = integral(y² dA) over waterplane
```

**Longitudinal BM:**

```
BM_l = I_l / volume

where:
- I_l = longitudinal second moment of waterplane area
- I_l = integral(x² dA) over waterplane
```

#### 5. Metacentric Height

```
GM_t = KM_t - KG
where: KM_t = KB + BM_t

GM_l = KM_l - KG
where: KM_l = KB + BM_l
```

#### 6. Form Coefficients

**Block Coefficient:**

```
Cb = volume / (Lpp * B * T)
```

**Prismatic Coefficient:**

```
Cp = volume / (A_midship * Lpp)
```

**Midship Coefficient:**

```
Cm = A_midship / (B * T)
```

**Waterplane Coefficient:**

```
Cwp = Awp / (Lpp * B)
```

#### 7. Trim Solver

**Equilibrium Conditions:**

```
1. Sum of vertical forces = 0
   ∆(T_AP, T_FP) = target_displacement

2. Sum of moments about LCF = 0
   M_trim = 0
```

**Newton-Raphson Iteration:**

```
Given target ∆:
    Initialize T_AP, T_FP = design_draft

    while not converged:
        compute ∆_current and LCB_current

        error_disp = ∆_current - target_∆
        error_moment = (LCB_current - LCF) * ∆_current

        # Adjust drafts
        T_AP += correction_AP
        T_FP += correction_FP

        if |error_disp| < tolerance and |error_moment| < tolerance:
            converged = True
```

### Service Methods

**GeometryService:**

```csharp
Task<Vessel> CreateVessel(VesselDto dto);
Task ValidateOffsets(List<OffsetDto> offsets);
Task<List<Station>> ImportStations(Guid vesselId, List<StationDto> stations);
Task<List<Waterline>> ImportWaterlines(Guid vesselId, List<WaterlineDto> waterlines);
Task<List<Offset>> ImportOffsets(Guid vesselId, List<OffsetDto> offsets);
Task<OffsetsGrid> GetOffsetsGrid(Guid vesselId);
```

**HydroCalculator:**

```csharp
Task<HydroResult> ComputeAtDraft(Guid vesselId, Guid loadcaseId, decimal draft);
Task<List<HydroResult>> ComputeTable(Guid vesselId, Guid loadcaseId, List<decimal> drafts);
Task<TrimSolution> SolveForDisplacement(Guid vesselId, Guid loadcaseId, decimal targetDisp);
```

**CurvesGenerator:**

```csharp
Task<Curve> GenerateDisplacementCurve(Guid vesselId, Guid loadcaseId, decimal minDraft, decimal maxDraft, int points);
Task<Curve> GenerateKBCurve(Guid vesselId, Guid loadcaseId, decimal minDraft, decimal maxDraft, int points);
Task<List<Curve>> GenerateBonjeanCurves(Guid vesselId);
```

**ExportService:**

```csharp
Task<byte[]> ExportPDF(Guid vesselId, ExportOptions options);
Task<byte[]> ExportExcel(Guid vesselId, ExportOptions options);
Task<string> ExportCSV(Guid vesselId, ExportOptions options);
Task<string> ExportJSON(Guid vesselId, ExportOptions options);
```

---

## 8) REST API (First Pass)

### Base URL

```
/api/v1/hydrostatics
```

### Endpoints

#### Vessel Management

**Create Vessel**

```http
POST /api/v1/hydrostatics/vessels
Content-Type: application/json

{
  "name": "MV Example Ship",
  "description": "Bulk carrier",
  "lpp": 150.0,
  "beam": 25.0,
  "design_draft": 10.0,
  "units_system": "SI"
}

Response: 201 Created
{
  "id": "uuid",
  "name": "MV Example Ship",
  "lpp": 150.0,
  ...
}
```

**Get Vessel**

```http
GET /api/v1/hydrostatics/vessels/{vesselId}

Response: 200 OK
{
  "id": "uuid",
  "name": "MV Example Ship",
  "lpp": 150.0,
  "beam": 25.0,
  "design_draft": 10.0,
  "stations_count": 21,
  "waterlines_count": 11,
  "offsets_count": 231
}
```

**List Vessels**

```http
GET /api/v1/hydrostatics/vessels

Response: 200 OK
{
  "vessels": [
    { "id": "uuid1", "name": "Vessel 1", ... },
    { "id": "uuid2", "name": "Vessel 2", ... }
  ],
  "total": 2
}
```

#### Geometry Definition

**Import Stations**

```http
POST /api/v1/hydrostatics/vessels/{vesselId}/stations
Content-Type: application/json

{
  "stations": [
    { "index": 0, "x": 0.0 },
    { "index": 1, "x": 7.5 },
    { "index": 2, "x": 15.0 },
    ...
  ]
}

Response: 201 Created
{ "imported": 21 }
```

**Import Waterlines**

```http
POST /api/v1/hydrostatics/vessels/{vesselId}/waterlines
Content-Type: application/json

{
  "waterlines": [
    { "index": 0, "z": 0.0 },
    { "index": 1, "z": 1.0 },
    { "index": 2, "z": 2.0 },
    ...
  ]
}

Response: 201 Created
{ "imported": 11 }
```

**Bulk Import Offsets**

```http
POST /api/v1/hydrostatics/vessels/{vesselId}/offsets:bulk
Content-Type: application/json

{
  "offsets": [
    { "station_index": 0, "waterline_index": 0, "half_breadth_y": 0.0 },
    { "station_index": 0, "waterline_index": 1, "half_breadth_y": 2.5 },
    { "station_index": 1, "waterline_index": 0, "half_breadth_y": 0.5 },
    ...
  ]
}

Response: 201 Created
{ "imported": 231 }
```

**Upload CSV**

```http
POST /api/v1/hydrostatics/vessels/{vesselId}/offsets:upload
Content-Type: multipart/form-data

file: [CSV file]
format: "offsets" | "stations_waterlines_separate"

Response: 201 Created
{
  "stations_imported": 21,
  "waterlines_imported": 11,
  "offsets_imported": 231,
  "validation_errors": []
}
```

**Get Offsets Grid**

```http
GET /api/v1/hydrostatics/vessels/{vesselId}/offsets

Response: 200 OK
{
  "stations": [0.0, 7.5, 15.0, ...],
  "waterlines": [0.0, 1.0, 2.0, ...],
  "offsets": [
    [0.0, 2.5, 3.8, ...],  // Station 0
    [0.5, 3.0, 4.2, ...],  // Station 1
    ...
  ]
}
```

#### Loadcases

**Create Loadcase**

```http
POST /api/v1/hydrostatics/vessels/{vesselId}/loadcases
Content-Type: application/json

{
  "name": "Design Condition",
  "rho": 1025.0,
  "kg": 6.5,
  "notes": "Saltwater, full load"
}

Response: 201 Created
{
  "id": "uuid",
  "name": "Design Condition",
  "rho": 1025.0,
  "kg": 6.5
}
```

**List Loadcases**

```http
GET /api/v1/hydrostatics/vessels/{vesselId}/loadcases

Response: 200 OK
{
  "loadcases": [
    { "id": "uuid1", "name": "Design Condition", ... },
    { "id": "uuid2", "name": "Ballast Condition", ... }
  ]
}
```

#### Hydrostatic Calculations

**Compute Hydrostatic Table**

```http
POST /api/v1/hydrostatics/vessels/{vesselId}/compute/table
Content-Type: application/json

{
  "loadcase_id": "uuid",
  "drafts": [8.0, 8.5, 9.0, 9.5, 10.0, 10.5, 11.0]
}

Response: 200 OK
{
  "results": [
    {
      "draft": 8.0,
      "disp_volume": 28500.5,
      "disp_weight": 29213012.5,
      "kb_z": 4.23,
      "lcb_x": 74.2,
      "tcb_y": 0.0,
      "bm_t": 5.67,
      "bm_l": 142.3,
      "gm_t": 3.4,
      "gm_l": 140.03,
      "awp": 3250.2,
      "cb": 0.712,
      "cp": 0.698,
      "cm": 0.982,
      "cwp": 0.867
    },
    ...
  ],
  "computation_time_ms": 342
}
```

**Solve for Target Displacement**

```http
POST /api/v1/hydrostatics/vessels/{vesselId}/compute/trim
Content-Type: application/json

{
  "loadcase_id": "uuid",
  "target_displacement": 30000000.0  // kg
}

Response: 200 OK
{
  "target_displacement": 30000000.0,
  "draft_ap": 10.2,
  "draft_fp": 9.8,
  "mean_draft": 10.0,
  "trim_angle": 0.15,  // degrees
  "lcf": 76.5,
  "mtc": 2340.5,
  "converged": true,
  "iterations": 5
}
```

#### Curves

**Get Available Curves**

```http
GET /api/v1/hydrostatics/vessels/{vesselId}/curves/types

Response: 200 OK
{
  "curve_types": [
    "displacement",
    "kb",
    "lcb",
    "awp",
    "gm_t",
    "bonjean"
  ]
}
```

**Generate Curves**

```http
POST /api/v1/hydrostatics/vessels/{vesselId}/curves
Content-Type: application/json

{
  "loadcase_id": "uuid",
  "types": ["displacement", "kb", "lcb", "gm_t"],
  "min_draft": 8.0,
  "max_draft": 12.0,
  "points": 100
}

Response: 200 OK
{
  "curves": [
    {
      "type": "displacement",
      "x_label": "Draft (m)",
      "y_label": "Displacement (kg)",
      "points": [
        { "x": 8.0, "y": 29213012.5 },
        { "x": 8.04, "y": 29345678.2 },
        ...
      ]
    },
    ...
  ]
}
```

**Get Bonjean Curves**

```http
GET /api/v1/hydrostatics/vessels/{vesselId}/curves/bonjean

Response: 200 OK
{
  "curves": [
    {
      "station_index": 0,
      "station_x": 0.0,
      "points": [
        { "draft": 0.0, "area": 0.0 },
        { "draft": 0.5, "area": 1.25 },
        { "draft": 1.0, "area": 5.0 },
        ...
      ]
    },
    ...
  ]
}
```

#### Export

**Export Report**

```http
POST /api/v1/hydrostatics/vessels/{vesselId}/export
Content-Type: application/json

{
  "format": "pdf",  // "pdf" | "excel" | "csv" | "json"
  "loadcase_id": "uuid",
  "sections": [
    "metadata",
    "inputs",
    "methodology",
    "hydrostatic_table",
    "curves",
    "summary"
  ],
  "drafts": [8.0, 9.0, 10.0, 11.0, 12.0]
}

Response: 200 OK
Content-Type: application/pdf
Content-Disposition: attachment; filename="vessel_hydrostatics_report.pdf"

[Binary PDF data]
```

**Download CSV Templates**

```http
GET /api/v1/hydrostatics/templates/offsets.csv
GET /api/v1/hydrostatics/templates/stations.csv
GET /api/v1/hydrostatics/templates/waterlines.csv

Response: 200 OK
Content-Type: text/csv
[CSV template with headers and example rows]
```

---

## 9) Acceptance Criteria

### US-2: Manual Offsets Grid

**Given:** A vessel with 21 stations and 11 waterlines
**When:** User edits cell (station 5, waterline 3) to value 4.25
**Then:**

- Cell updates immediately
- Value validates as numeric and non-negative
- Save button enables
- Other cells remain unchanged

**Given:** User pastes from Excel (5 columns × 10 rows)
**When:** Paste action triggered
**Then:**

- All 50 cells populate
- Invalid cells highlight in red
- Error summary lists issues
- Valid cells save on confirmation

---

### US-3: CSV Upload

**Given:** Sample offsets CSV template with 231 rows
**When:** User uploads via drag-drop
**Then:**

- Upload completes in <2s
- Preview shows first 10 rows
- Validation runs automatically
- All 231 offsets import successfully

**Given:** CSV with non-monotonic station X values
**When:** Import attempted
**Then:**

- Error: "Station X values must be monotonically increasing"
- Exact rows listed: "Rows 15, 16"
- Import blocked until corrected

---

### US-5: Hydrostatic Table

**Given:** Rectangular barge (L=100m, B=20m, T=5m)
**When:** Compute table for drafts [4.0, 4.5, 5.0, 5.5, 6.0]
**Then:**

- ∆ at T=5.0m = 1025 _ 100 _ 20 \* 5 = 10,250,000 kg (within 0.5%)
- KB at T=5.0m = 2.5m (exact)
- BM_t = (B²/12) / T = 20²/(12\*5) = 6.67m (within 0.5%)
- Computation time <1s

**Given:** Wigley hull with published benchmark
**When:** Compute at benchmark draft
**Then:**

- ∆, KB, LCB match benchmark within 2%
- All coefficients within 2%

---

### US-6: Trim Solver

**Given:** Rectangular barge, target ∆ = 11,000,000 kg
**When:** Solve for equilibrium
**Then:**

- Mean draft T = 11,000,000 / (1025*100*20) ≈ 5.37m (within ±2mm)
- Converges in <10 iterations
- Trim angle = 0° (symmetry)

**Given:** Ship hull, target ∆ = 30,000,000 kg
**When:** Solve for equilibrium
**Then:**

- Returns T_AP, T_FP within ±2mm of analytical
- Trim angle reasonable (< 2°)
- LCF within vessel length

---

### US-8: Curves Rendering

**Given:** 100-point draft grid from 8m to 12m
**When:** Request displacement curve
**Then:**

- 100 (draft, disp) pairs returned
- Render time <300ms
- Curve smooth and monotonic
- Tooltips show exact values on hover

**Given:** Export curve as CSV
**When:** Download triggered
**Then:**

- CSV contains 100 rows
- Values match UI display exactly
- Headers: "Draft (m)", "Displacement (kg)"

---

### US-10: PDF Export

**Given:** Vessel with complete hydrostatic table
**When:** Export PDF report
**Then:**

- PDF includes all sections per §3.4
- Tables formatted professionally
- 4+ plots embedded as images
- Regenerate → identical file (deterministic)
- File size <5MB for typical report

**Given:** Same vessel, export Excel
**When:** Download triggered
**Then:**

- Excel has multiple sheets: Metadata, Table, Curves
- Charts embedded and linked to data
- Formulas preserved (e.g., GM = KM - KG)
- Opens without errors in Excel/LibreOffice

---

### US-12: Reference Hull Tests

**Rectangular Barge:**

```csharp
[Fact]
public async Task RectangularBarge_Displacement_MatchesAnalytical()
{
    // Arrange
    var barge = CreateRectangularBarge(L: 100, B: 20, T: 5);
    var rho = 1025;

    // Act
    var result = await _hydroCalculator.ComputeAtDraft(barge.Id, loadcaseId, draft: 5.0);

    // Assert
    var expected = rho * 100 * 20 * 5; // 10,250,000 kg
    result.DispWeight.Should().BeApproximately(expected, expected * 0.005); // 0.5%
}

[Fact]
public async Task RectangularBarge_KB_MatchesAnalytical()
{
    // Arrange
    var barge = CreateRectangularBarge(L: 100, B: 20, T: 5);

    // Act
    var result = await _hydroCalculator.ComputeAtDraft(barge.Id, loadcaseId, draft: 5.0);

    // Assert
    result.KBz.Should().BeApproximately(2.5, 0.001); // Exact to 1mm
}

[Fact]
public async Task RectangularBarge_BMt_MatchesAnalytical()
{
    // Arrange
    var barge = CreateRectangularBarge(L: 100, B: 20, T: 5);

    // Act
    var result = await _hydroCalculator.ComputeAtDraft(barge.Id, loadcaseId, draft: 5.0);

    // Assert
    var expected = Math.Pow(20, 2) / (12 * 5); // 6.667m
    result.BMt.Should().BeApproximately(expected, expected * 0.005);
}
```

**Wigley Hull:**

```csharp
[Fact]
public async Task WigleyHull_MatchesBenchmark()
{
    // Arrange
    var wigley = CreateWigleyHull(); // From published offsets
    var benchmark = LoadWigleyBenchmark(); // Published results

    // Act
    var result = await _hydroCalculator.ComputeAtDraft(wigley.Id, loadcaseId, draft: 0.0625);

    // Assert
    result.DispVolume.Should().BeApproximately(benchmark.DispVolume, benchmark.DispVolume * 0.02); // 2%
    result.LCBx.Should().BeApproximately(benchmark.LCBx, benchmark.LCBx * 0.02);
    result.Cb.Should().BeApproximately(benchmark.Cb, 0.02);
}
```

---

## 10) UI Outline (MVP)

### Navigation Structure

```
Vessels (List View)
  ├─ Create Vessel
  └─ [Vessel Name]
      ├─ Overview
      ├─ Geometry ←─────────────────┐
      │   ├─ Stations                │
      │   ├─ Waterlines              │
      │   ├─ Offsets Grid ──────────┤ MVP Focus
      │   └─ Import CSV              │
      ├─ Loadcases                   │
      ├─ Hydrostatics ──────────────┤
      │   ├─ Compute Table           │
      │   ├─ Trim Solver             │
      │   └─ Results                 │
      ├─ Curves ────────────────────┤
      │   ├─ Displacement            │
      │   ├─ KB / LCB                │
      │   ├─ GM                      │
      │   └─ Bonjean                 │
      ├─ Visualization               │
      │   ├─ 2D Views                │
      │   └─ 3D Hull ───────────────┤
      └─ Reports                     │
          ├─ Configure               │
          └─ Export (PDF/Excel/CSV) ─┘
```

### Page Wireframes

#### 1. Vessels List

```
┌───────────────────────────────────────────────┐
│ NavArch Studio         [+ New Vessel]  [User]│
├───────────────────────────────────────────────┤
│                                               │
│  My Vessels                                   │
│                                               │
│  ┌─────────────────────────────────────────┐ │
│  │ 🚢 MV Example Ship                      │ │
│  │    150m × 25m × 10m                     │ │
│  │    21 stations, 11 waterlines           │ │
│  │    Last modified: 2 hours ago           │ │
│  └─────────────────────────────────────────┘ │
│                                               │
│  ┌─────────────────────────────────────────┐ │
│  │ 🚢 Bulk Carrier Alpha                   │ │
│  │    200m × 32m × 12m                     │ │
│  │    41 stations, 21 waterlines           │ │
│  │    Last modified: 1 day ago             │ │
│  └─────────────────────────────────────────┘ │
│                                               │
└───────────────────────────────────────────────┘
```

#### 2. Geometry: Offsets Grid

```
┌────────────────────────────────────────────────────┐
│ MV Example Ship  │  Overview  Geometry  Loadcases  │
├────────────────────────────────────────────────────┤
│                                                    │
│  Geometry Definition                               │
│                                                    │
│  [Stations] [Waterlines] [Offsets Grid] [Import]  │
│                                                    │
│  Offsets Grid (Half-breadths in meters)           │
│  ┌───────┬──────┬──────┬──────┬──────┬─────────┐  │
│  │ Sta\WL│ 0.0m │ 1.0m │ 2.0m │ 3.0m │ 4.0m   │  │
│  ├───────┼──────┼──────┼──────┼──────┼─────────┤  │
│  │ 0 (0m)│ 0.00 │ 2.50 │ 3.80 │ 4.50 │ 4.80  │  │
│  │ 1 (7m)│ 0.50 │ 3.00 │ 4.20 │ 5.00 │ 5.30  │  │
│  │ 2(15m)│ 0.80 │ 3.50 │ 4.80 │ 5.60 │ 5.90  │  │
│  │ ...   │ ...  │ ...  │ ...  │ ...  │ ...   │  │
│  └───────┴──────┴──────┴──────┴──────┴─────────┘  │
│                                                    │
│  [Paste from Excel] [Download CSV] [Save]         │
│                                                    │
└────────────────────────────────────────────────────┘
```

#### 3. Hydrostatics: Compute Table

```
┌────────────────────────────────────────────────────┐
│ MV Example Ship  │  Geometry  Hydrostatics  Curves │
├────────────────────────────────────────────────────┤
│                                                    │
│  Hydrostatic Table                                 │
│                                                    │
│  Loadcase: [Design Condition ▼]  Units: [SI ▼]   │
│                                                    │
│  Draft Range:                                      │
│    Min: [8.0] m   Max: [12.0] m   Step: [0.5] m  │
│                                                    │
│  [Compute Table]                                   │
│                                                    │
│  Results:                                          │
│  ┌──────┬─────────┬────────┬──────┬──────┬──────┐ │
│  │ T(m) │ ∆(t)    │ KB(m)  │LCB(m)│ GMt │ Cb  │ │
│  ├──────┼─────────┼────────┼──────┼──────┼──────┤ │
│  │ 8.0  │ 29,213  │  4.23  │ 74.2 │ 3.4 │0.712│ │
│  │ 8.5  │ 31,045  │  4.48  │ 74.5 │ 3.5 │0.714│ │
│  │ 9.0  │ 32,890  │  4.73  │ 74.8 │ 3.6 │0.716│ │
│  │ ...  │ ...     │  ...   │ ...  │ ... │ ... │ │
│  └──────┴─────────┴────────┴──────┴──────┴──────┘ │
│                                                    │
│  [Export CSV] [Export Excel] [View Curves]        │
│                                                    │
└────────────────────────────────────────────────────┘
```

#### 4. Curves

```
┌────────────────────────────────────────────────────┐
│ MV Example Ship  │  Hydrostatics  Curves  Reports  │
├────────────────────────────────────────────────────┤
│                                                    │
│  Hydrostatic Curves                                │
│                                                    │
│  Loadcase: [Design Condition ▼]                   │
│                                                    │
│  Select Curves:                                    │
│  ☑ Displacement    ☑ KB    ☑ LCB    ☑ GM         │
│  ☐ Awp    ☐ Bonjean                               │
│                                                    │
│  ┌─────────────────────────────────────────────┐  │
│  │         Displacement vs Draft               │  │
│  │  35,000├──────────────────────────────┐     │  │
│  │        │                          ╱   │     │  │
│  │  30,000├─────────────────────╱────────┤     │  │
│  │        │                 ╱             │     │  │
│  │  25,000├────────────╱───────────────  │     │  │
│  │        └────────────────────────────  │     │  │
│  │         8.0    9.0    10.0    11.0    12.0  │  │
│  └─────────────────────────────────────────────┘  │
│                                                    │
│  ┌─────────────────────────────────────────────┐  │
│  │         KB vs Draft                         │  │
│  │   6.0 ├──────────────────────────────┐      │  │
│  │       │                          ╱   │      │  │
│  │   5.0 ├─────────────────────╱────────┤      │  │
│  │       │                 ╱             │      │  │
│  │   4.0 ├────────────╱───────────────  │      │  │
│  │       └────────────────────────────  │      │  │
│  │         8.0    9.0    10.0    11.0    12.0  │  │
│  └─────────────────────────────────────────────┘  │
│                                                    │
│  [Export PNG] [Export SVG] [Export Data CSV]      │
│                                                    │
└────────────────────────────────────────────────────┘
```

#### 5. 3D Visualization

```
┌────────────────────────────────────────────────────┐
│ MV Example Ship  │  Curves  Visualization  Reports │
├────────────────────────────────────────────────────┤
│                                                    │
│  3D Hull Preview                                   │
│                                                    │
│  ┌────────────────────────────────────────────┐   │
│  │                                            │   │
│  │         ╱───────────────────╲              │   │
│  │        ╱                     ╲             │   │
│  │       │      ▓▓▓▓▓▓▓▓▓▓      │            │   │
│  │       │  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓   │            │   │
│  │      │ ━━━━━━━━━━━━━━━━━━━━━━━ │ ← Waterline│   │
│  │       │  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓   │            │   │
│  │        ╲  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓   ╱             │   │
│  │         ╲                   ╱              │   │
│  │          ╲_____     ______╱               │   │
│  │                 ╲ ╱                       │   │
│  │                  •  ← KB (4.23m)          │   │
│  │                  •  ← LCB (74.2m)         │   │
│  │                                            │   │
│  └────────────────────────────────────────────┘   │
│                                                    │
│  Draft: [═══●═══════] 10.0 m                      │
│                                                    │
│  [Rotate] [Pan] [Zoom] [Reset View]               │
│                                                    │
└────────────────────────────────────────────────────┘
```

#### 6. Reports

```
┌────────────────────────────────────────────────────┐
│ MV Example Ship  │  Visualization  Reports         │
├────────────────────────────────────────────────────┤
│                                                    │
│  Export Report                                     │
│                                                    │
│  Format:                                           │
│  ○ PDF    ○ Excel    ○ CSV    ○ JSON              │
│                                                    │
│  Sections to Include:                              │
│  ☑ Vessel Metadata                                 │
│  ☑ Inputs Summary (Geometry)                       │
│  ☑ Methodology & Assumptions                       │
│  ☑ Hydrostatic Table                               │
│  ☑ Curves Gallery                                  │
│  ☑ Key Results Summary                             │
│  ☐ Appendix: Offsets Table                        │
│                                                    │
│  Loadcase: [Design Condition ▼]                   │
│                                                    │
│  Draft Range:                                      │
│    Min: [8.0] m   Max: [12.0] m   Step: [0.5] m  │
│                                                    │
│  [Generate Report]                                 │
│                                                    │
│  ┌──────────────────────────────────────────┐     │
│  │ ✓ Report generated successfully           │     │
│  │   vessel_hydrostatics_report_2025-10-25.pdf│   │
│  │   Size: 2.4 MB                            │     │
│  │   [Download]                              │     │
│  └──────────────────────────────────────────┘     │
│                                                    │
└────────────────────────────────────────────────────┘
```

### Component Library (Frontend)

**Core Components:**

- `VesselCard` - Vessel summary card
- `OffsetsGrid` - Editable spreadsheet grid (AG Grid or Handsontable)
- `StationsEditor` - Station X positions editor
- `WaterlinesEditor` - Waterline Z positions editor
- `LoadcaseForm` - Loadcase input form
- `HydroTable` - Results table with sorting/filtering
- `CurveChart` - Interactive chart (Recharts or Chart.js)
- `Hull3DViewer` - 3D hull visualization (Three.js)
- `ExportDialog` - Export configuration modal
- `ImportWizard` - CSV import stepper

**Shared Components:**

- `UnitSelector` - SI/Imperial toggle
- `DraftSlider` - Draft range slider
- `ValidationError` - Error display
- `LoadingSpinner` - Computation progress
- `DownloadButton` - File download trigger

---

## 11) Test Plan

### Unit Tests

#### Geometry Validation

```csharp
[Theory]
[InlineData(new[] { 0.0, 10.0, 20.0 }, true)]  // Monotonic
[InlineData(new[] { 0.0, 20.0, 10.0 }, false)] // Non-monotonic
public void ValidateStations_MonotonicCheck(decimal[] xValues, bool expectedValid)
```

#### Integration Methods

```csharp
[Fact]
public void Simpsons_EvenSpacing_MatchesAnalytical()

[Fact]
public void Trapezoidal_IrregularSpacing_AccurateWithin1Percent()
```

#### Form Coefficients

```csharp
[Theory]
[InlineData(100, 20, 10, 10000, 0.5)]  // Cb = V / (L*B*T)
public void BlockCoefficient_RectangularBarge_Exact(
    decimal L, decimal B, decimal T, decimal volume, decimal expectedCb)
```

### Integration Tests

#### End-to-End: CSV Upload → Compute → Export

```csharp
[Fact]
public async Task E2E_UploadCSV_ComputeTable_ExportPDF()
{
    // Arrange
    var csvFile = LoadSampleCSV("barge_offsets.csv");

    // Act: Upload
    var vessel = await _client.PostAsync("/api/v1/hydrostatics/vessels/{id}/offsets:upload", csvFile);

    // Act: Compute
    var table = await _client.PostAsync("/api/v1/hydrostatics/vessels/{id}/compute/table",
        new { drafts = new[] { 4.0, 5.0, 6.0 } });

    // Act: Export
    var pdf = await _client.PostAsync("/api/v1/hydrostatics/vessels/{id}/export",
        new { format = "pdf" });

    // Assert
    pdf.Should().NotBeNull();
    pdf.ContentType.Should().Be("application/pdf");
    pdf.Content.Length.Should().BeLessThan(5_000_000); // <5MB
}
```

### Visual Regression Tests

**Curves Rendering:**

```typescript
describe("Displacement Curve", () => {
  it("matches baseline snapshot", async () => {
    const curve = await renderDisplacementCurve(testVessel);
    expect(curve).toMatchImageSnapshot();
  });
});
```

**3D Hull Visualization:**

```typescript
describe("3D Hull Viewer", () => {
  it("renders barge correctly", async () => {
    const viewer = await render3DHull(rectangularBarge);
    expect(viewer).toMatchImageSnapshot();
  });
});
```

### Performance Tests

**Large Offsets Grid:**

```csharp
[Fact]
public async Task ComputeTable_200Stations_200Waterlines_Under5Seconds()
{
    // Arrange
    var largeVessel = CreateLargeVessel(stations: 200, waterlines: 200);
    var stopwatch = Stopwatch.StartNew();

    // Act
    var results = await _hydroCalculator.ComputeTable(
        largeVessel.Id, loadcaseId, drafts: GenerateDrafts(8, 12, 0.5));

    // Assert
    stopwatch.Stop();
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
}
```

**UI Responsiveness:**

```typescript
test("Offsets grid cell edit responds in <50ms", async () => {
  const start = performance.now();
  await grid.editCell(5, 3, "4.25");
  const elapsed = performance.now() - start;
  expect(elapsed).toBeLessThan(50);
});
```

### Reference Hull Test Data

**Rectangular Barge:**

```json
{
  "name": "Rectangular Barge",
  "lpp": 100.0,
  "beam": 20.0,
  "design_draft": 5.0,
  "stations": 5,
  "waterlines": 3,
  "analytical": {
    "volume": 10000.0,
    "displacement": 10250000.0,
    "kb": 2.5,
    "lcb": 50.0,
    "bm_t": 6.667,
    "cb": 1.0,
    "cp": 1.0,
    "cm": 1.0,
    "cwp": 1.0
  }
}
```

**Wigley Hull:**

```json
{
  "name": "Wigley Parabolic Hull",
  "lpp": 1.0,
  "beam": 0.1,
  "design_draft": 0.0625,
  "stations": 41,
  "waterlines": 11,
  "source": "Wigley (1942)",
  "benchmark": {
    "volume": 0.00289,
    "cb": 0.444,
    "lcb": 0.5
  }
}
```

---

## 12) Implementation Roadmap

### Sprint 1 (Week 1-2): Foundation

**Backend:**

- ✅ Create database schema and migrations
- ✅ Implement Vessel, Station, Waterline, Offset entities
- ✅ Create VesselService and GeometryService
- ✅ Implement CSV import with validation
- ✅ Build core integration engine (Simpson's, Trapezoidal)

**Frontend:**

- ✅ Create Vessels list page
- ✅ Create Vessel detail page with tabs
- ✅ Implement offsets grid component (AG Grid)
- ✅ Build CSV import wizard

**Testing:**

- ✅ Unit tests for integration methods
- ✅ Rectangular barge test case

**Deliverables:**

- CSV templates
- Basic geometry CRUD

---

### Sprint 2 (Week 3-4): Core Calculations

**Backend:**

- ✅ Implement HydroCalculator service
- ✅ Sectional area integration
- ✅ Volume, KB, LCB calculations
- ✅ BM, GM calculations
- ✅ Form coefficients (Cb, Cp, Cm, Cwp)
- ✅ Hydrostatic table generation API

**Frontend:**

- ✅ Loadcase management UI
- ✅ Hydrostatic table compute page
- ✅ Results table with unit conversion
- ✅ Loading states and error handling

**Testing:**

- ✅ Unit tests for all calculations
- ✅ Barge analytical validation (<0.5% error)
- ✅ Integration tests for table generation

**Deliverables:**

- Working hydrostatic table
- Validated against barge

---

### Sprint 3 (Week 5-6): Curves & Visualization

**Backend:**

- ✅ CurvesGenerator service
- ✅ Displacement curve generation
- ✅ KB, LCB, Awp curves
- ✅ Bonjean curves
- ✅ Curve data export (CSV/JSON)

**Frontend:**

- ✅ Curves page with selectable plots
- ✅ Interactive charts (Recharts)
- ✅ 2D body plan/sheer plan views
- ✅ 3D hull viewer (Three.js)
- ✅ Draft slider with real-time waterplane update

**Testing:**

- ✅ Visual regression tests for curves
- ✅ 3D rendering tests
- ✅ Performance tests (<300ms rendering)

**Deliverables:**

- Interactive curves
- 3D visualization

---

### Sprint 4 (Week 7-8): Trim Solver & Reporting

**Backend:**

- ✅ TrimSolver service (Newton-Raphson)
- ✅ Target displacement equilibrium solver
- ✅ ExportService (PDF generation)
- ✅ Excel export with charts
- ✅ CSV/JSON export

**Frontend:**

- ✅ Trim solver UI
- ✅ Reports configuration page
- ✅ Export dialog with format selection
- ✅ Download handling

**Testing:**

- ✅ Trim solver convergence tests
- ✅ Export determinism tests
- ✅ Wigley hull benchmark validation (<2% error)
- ✅ End-to-end integration tests

**Deliverables:**

- Complete Phase 1 MVP
- PDF/Excel reports
- All acceptance criteria met

---

## 13) Backlog (Nice-to-Have / Phase 1.5+)

### Phase 1.5 (Weeks 9-12)

**NURBS/CAD Surface Support:**

- Import IGES/STEP hull surfaces
- Auto-sectioning algorithm
- Procedural waterline/station generation

**Advanced Stability:**

- KN/GZ curve generation
- Heel/trim coupled hydrostatics (T, φ)
- Downflooding angle detection

**Enhanced UI:**

- Dark mode
- Keyboard shortcuts for grid navigation
- Bulk operations (duplicate loadcase, etc.)

### Phase 2

**Damage Stability:**

- Compartment definition
- Flooding scenarios
- Damaged stability analysis

**Weights & Loadcases:**

- Tank definitions with sounding tables
- Weight tree (lightship + variable loads)
- Automatic KG/LCG calculation from weights

**Longitudinal Strength:**

- Shear force & bending moment
- Still water loads
- Wave loads (sagging/hogging)

### Phase 3

**Seakeeping:**

- RAO (Response Amplitude Operators)
- Ship motions in waves
- Seasickness/comfort criteria

**Regulatory Compliance:**

- IMO intact stability criteria checks
- SOLAS requirements
- Classification society rules (ABS, Lloyd's, DNV)

**Collaboration:**

- Multi-user editing
- Comments/annotations
- Version control for vessels

---

## Deliverables Summary

### Phase 1 Deliverables

**Documentation:**

- ✅ This Phase 1 plan
- ✅ API documentation (Swagger)
- ✅ User guide
- ✅ CSV template examples

**Code:**

- ✅ Backend services (7 services)
- ✅ REST API (15+ endpoints)
- ✅ Database schema (9 tables)
- ✅ Frontend UI (12+ components)

**Test Assets:**

- ✅ Rectangular barge test case
- ✅ Wigley hull test case
- ✅ Unit tests (50+ tests)
- ✅ Integration tests (10+ scenarios)

**Export Templates:**

- ✅ PDF report template
- ✅ Excel workbook template
- ✅ CSV data format

---

## Success Criteria

### Functional

- ✅ User can import hull geometry via CSV
- ✅ User can compute hydrostatic table
- ✅ User can visualize curves and 3D hull
- ✅ User can export PDF/Excel reports
- ✅ All 12 user stories implemented

### Quality

- ✅ Barge tests pass with <0.5% error
- ✅ Wigley tests pass with <2% error
- ✅ 40k offsets compute in <5s
- ✅ No linting errors
- ✅ >80% code coverage

### User Experience

- ✅ Intuitive UI (user testing feedback)
- ✅ Responsive on desktop (1920×1080 and 1366×768)
- ✅ Clear error messages
- ✅ Professional reports

---

## References

1. **IMO MSC.267(85)** - Intact Stability Code
2. **ISO 12217** - Small Craft Stability
3. **ABS Rules** - Hydrostatics and Stability
4. **Wigley (1942)** - "A Comparison of Experiment and Calculated Wave Profiles"
5. **Rawson & Tupper** - "Basic Ship Theory" (5th ed.)
6. **Schneekluth & Bertram** - "Ship Design for Efficiency and Economy"

---

**Phase 1 Complete When:**

- All acceptance criteria met
- All tests passing
- User can complete end-to-end workflow: create vessel → import geometry → compute → visualize → export report

**Target Completion:** 8 weeks from start

**Review Milestone:** Week 4 (end of Sprint 2) - validate core calculations before visualization work
