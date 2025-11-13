# Catalog System Features

**Module Status**: 53% Complete - Foundation Solid, Data Import Blocked  
**Last Updated**: November 4, 2025  
**Test Coverage**: Catalog services untested

---

## 📊 Module Overview

The Catalog System provides reference data for naval architecture calculations, including benchmark hull forms, propeller series, and water properties.

### Key Capabilities
- Reference hull particulars (6 vessels)
- Water properties with interpolation (ITTC data)
- Wigley analytical hull geometry
- Clone catalog data to user vessels
- Catalog browser with tabbed interface

### Current Status
✅ **Working**: Database, water properties, Wigley geometry, clone feature  
🚫 **Blocked**: IGES import, benchmark geometries, propeller data (needs external files)  
📋 **Planned**: 3D viewer, validation data, user submissions

---

## ✅ Completed Features

### 1. Database Schema

**Status**: ✅ Complete  
**Priority**: Critical  
**Complexity**: M  
**Phase**: Phase 1

**Description**: Comprehensive catalog database schema with tables for hulls, propellers, and water properties.

**Tables**:
1. **catalog_hulls** - Reference hull particulars
   - Principal dimensions (Lpp, B, T, D, Displacement)
   - Form coefficients (Cb, Cp, Cm, Cwp)
   - Hull type, family, intended use
   - Metadata (source, year, citation)

2. **catalog_propellers** - Propeller series data
   - Series name (Wageningen B, Gawn, etc.)
   - Blades, AE/A0, P/D
   - Open-water curves (KT, KQ, η0 vs J)
   - Demo/production flag

3. **catalog_water_properties** - Water properties by temperature/salinity
   - Temperature (°C), Salinity (PSU)
   - Density, kinematic viscosity, surface tension, vapor pressure
   - Source (ITTC, etc.)

4. **benchmark_geometry** - Hull geometry for validation
   - Stations, waterlines, offsets (JSON)
   - IGES/STEP file references
   - Associated hull ID

5. **benchmark_test_conditions** - Published test results
   - Froude number, speed, Reynolds number
   - Measured resistance, power
   - Test facility, scale, reference

**Code Locations**:
- Backend: `backend/Shared/Models/` (catalog models)
- Migrations: `backend/DataService/Migrations/` (schema creation)

---

### 2. Water Properties (ITTC Data)

**Status**: ✅ Complete  
**Priority**: High  
**Complexity**: M  
**Phase**: Phase 1

**Description**: Seawater and freshwater properties with temperature/salinity interpolation.

**Features**:
- **ITTC Anchor Points**: Standard reference data (0-30°C)
- **Linear Interpolation**: For intermediate temperatures
- **Properties Provided**:
  - Density (kg/m³)
  - Kinematic viscosity (m²/s)
  - Surface tension (N/m)
  - Vapor pressure (Pa)
- **API**: `/api/v1/water-properties?temp=15&salinity=35`

**Use Cases**:
- Resistance calculations (Reynolds number)
- Hydrostatic calculations (displacement)
- Cavitation analysis (vapor pressure)
- Stability calculations (density)

**Code Locations**:
- Backend: `backend/DataService/Services/Catalog/CatalogWaterService.cs`
- Data: `backend/DataService/Data/Seeds/` (CSV seeding)

**Related Docs**:
- `CATALOG_IMPLEMENTATION_SUMMARY.md`

---

### 3. Wigley Hull (Analytical Geometry)

**Status**: ✅ Complete  
**Priority**: Medium  
**Complexity**: M  
**Phase**: Phase 1

**Description**: Mathematical hull form with complete analytical geometry for validation.

**Particulars**:
- Lpp: 100 m
- B: 10 m
- T: 6.25 m
- Cb: ~0.444 (theoretical value)

**Geometry**:
- 21 stations (AP to FP)
- 13 waterlines (keel to deck)
- 273 offset points
- Stored as JSON in database

**Formula**: Wigley parabolic waterlines
```
y(x,z) = (B/2) × (1 - (2x/L)²) × (1 - (z/T)²)
```

**Use Cases**:
- Validation of hydrostatics code
- Benchmark for geometry algorithms
- Test case for export functions
- Reference for visualization

**Code Locations**:
- Backend: `backend/Shared/TestData/HullTestData.cs` (generation code)
- Seeder: `backend/DataService/Data/Seeds/CatalogSeeder.cs`

**Related Docs**:
- `temp/WIGLEY_FIX_SUMMARY.md`

---

### 4. Hull Particulars (6 Vessels)

**Status**: ✅ Complete  
**Priority**: High  
**Complexity**: S  
**Phase**: Phase 1

**Description**: Reference hull particulars for 6 benchmark vessels.

**Reference Hulls**:
1. **Wigley Hull** (analytical, full geometry ✅)
2. **Series 60 (Cb=0.60)** (fast destroyer, geometry pending)
3. **Series 60 (Cb=0.70)** (general cargo, geometry pending)
4. **KCS** (KRISO Container Ship, geometry pending)
5. **KVLCC2** (KRISO VLCC tanker, geometry pending)
6. **DTMB 5415** (destroyer, geometry pending)

**Data Included**:
- All principal dimensions
- Form coefficients
- Intended use, vessel type
- Source citations (SIMMAN, ITTC)

**Data Missing**:
- Geometry for 5 of 6 hulls (only Wigley complete)
- Test condition data
- Resistance/power validation curves

**Code Locations**:
- Backend: `backend/DataService/Controllers/CatalogHullsController.cs`
- Seeder: `backend/DataService/Data/Seeds/` (CSV import)

---

### 5. Clone to Vessel Feature

**Status**: ✅ Complete  
**Priority**: High  
**Complexity**: M  
**Phase**: Phase 1

**Description**: Import catalog hull data into user's vessel workspace.

**Workflow**:
1. User browses catalog
2. Selects reference hull
3. Clicks "Clone to My Vessels"
4. System creates new vessel with:
   - Principal particulars copied
   - Geometry copied (if available)
   - Catalog reference ID stored
   - User ownership assigned

**Features**:
- Tenant isolation (user sees only their vessels + catalog)
- Geometry deep copy (not referenced)
- Metadata preserved (source, citation)
- Editable after cloning

**Code Locations**:
- Backend: `backend/DataService/Controllers/CatalogHullsController.cs` (clone endpoint)
- Frontend: `frontend/src/components/catalog/HullDetailPage.tsx` (clone button)

---

### 6. Catalog Browser UI

**Status**: ✅ Complete  
**Priority**: Medium  
**Complexity**: M  
**Phase**: Phase 1

**Description**: Browse catalog with tabbed interface.

**Tabs**:
1. **Hulls** - List of reference hulls with cards
2. **Propellers** - Propeller series (future)
3. **Water Properties** - Lookup tool

**Hull Cards Show**:
- Hull name (e.g., "KCS - KRISO Container Ship")
- Thumbnail (if geometry available)
- Key dimensions (Lpp, B, T)
- Cb, vessel type
- "View Details" button

**Hull Detail Page**:
- Complete particulars table
- Geometry preview (if available)
- Source/citation info
- "Clone to My Vessels" button

**Code Locations**:
- Frontend: 
  - `frontend/src/pages/catalog/CatalogPage.tsx`
  - `frontend/src/components/catalog/HullDetailPage.tsx`
  - `frontend/src/components/catalog/CatalogBrowser.tsx`

**Related Docs**:
- `CATALOG_IMPLEMENTATION_SUMMARY.md`

---

### 7. Propeller Placeholder

**Status**: ⚠️ Demo Data Only  
**Priority**: Low  
**Complexity**: N/A  
**Phase**: Phase 1

**Description**: Placeholder propeller data (4 demo points) until real B-series data available.

**Current State**:
- Wageningen B-series placeholder
- 4 demo J values with dummy KT/KQ
- Flagged as `IsDemo = true`

**Use Cases**:
- UI/API testing
- Schema validation

**Next**: Replace with real B-series open-water data

**Code Locations**:
- Seeder: `backend/DataService/Data/Seeds/CatalogSeeder.cs`

---

## 🚫 Blocked Features

### 8. IGES File Parsing

**Status**: 🚫 Blocked - Needs Library Evaluation  
**Priority**: High  
**Complexity**: L  
**Phase**: Phase 2

**Description**: Import 3D hull geometry from IGES (or STEP) CAD files.

**Blocker**: Requires IGES parser library and sample files

**Planned Implementation**:
1. Evaluate libraries: IxMilia.Iges, OpenCascade, netDXF
2. Test with SIMMAN IGES files (KCS, KVLCC2, DTMB 5415)
3. Extract surface → resample to stations/waterlines grid
4. Store in `BenchmarkGeometry` table (JSON)
5. Validate against published particulars

**Requirements**:
- IGES files from SIMMAN 2008 workshop
- Surface extraction logic
- Resampling algorithm (e.g., 21 stations × 13 waterlines)
- Coordinate convention handling (AP/FP, keel/baseline)

**Estimated Effort**: 1-2 weeks (with IGES files)

**Dependencies**:
- External data: IGES files from SIMMAN or other sources
- Library choice: IxMilia.Iges recommended

**Related Docs**:
- `temp/CATALOG_NEXT_STEPS.md`

---

### 9. Benchmark Hull Geometries

**Status**: 🚫 Blocked - Needs External Data  
**Priority**: High  
**Complexity**: M  
**Phase**: Phase 2

**Description**: Complete geometry for KCS, KVLCC2, DTMB 5415, Series 60 hulls.

**Blocker**: Requires geometry files from public datasets

**Hulls Needed**:
1. **KCS** - SIMMAN 2008 dataset (IGES or offsets)
2. **KVLCC2** - SIMMAN 2008 dataset
3. **DTMB 5415** - SIMMAN 2008 dataset
4. **Series 60** - Todd (1963) hull forms
5. **Prismatic NPC** - Internal test forms (may be available)

**Data Sources**:
- **SIMMAN 2008**: https://www.simman2008.dk/ (check availability)
- **ITTC**: https://www.ittc.info/procedures (some data)
- **David Taylor Model Basin**: Series 60 reports
- Academic papers with published offsets

**Workaround**: Users can manually upload offsets via CSV

**Estimated Effort**: 3-5 days (once files obtained)

**Related Docs**:
- `temp/CATALOG_NEXT_STEPS.md`

---

### 10. Wageningen B-Series Data

**Status**: 🚫 Blocked - Needs Dataset  
**Priority**: High  
**Complexity**: M  
**Phase**: Phase 2

**Description**: Replace placeholder with real Wageningen B-series open-water data.

**Blocker**: Requires Wageningen dataset (Zenodo or marine propeller databases)

**Data Needed**:
- Blade count: Z = 3, 4, 5, 6, 7
- Area ratio: AE/A0 = 0.30 to 1.05
- Pitch ratio: P/D = 0.60 to 1.40
- Performance curves: KT, KQ, η0 vs J (advance coefficient)

**Data Sources**:
- Zenodo marine propeller datasets
- MARIN publications
- Academic databases (e.g., Iowa Propeller Series)

**Estimated Effort**: 2-3 days (data parsing + import)

**Use Cases**:
- Propeller selection in hull sizing
- Power prediction (resistance → propeller → engine)
- Efficiency optimization

**Related Docs**:
- `temp/CATALOG_NEXT_STEPS.md`

---

### 11. Benchmark Test Conditions

**Status**: 🚫 Blocked - Needs Published Results  
**Priority**: Medium  
**Complexity**: M  
**Phase**: Phase 3

**Description**: Validation data from towing tank tests.

**Blocker**: Requires published test results from ITTC, SIMMAN, or facility reports

**Data Needed**:
- Test conditions: Froude number, speed, Reynolds number
- Measured resistance (total, friction, residuary)
- Measured power
- Model scale vs full scale
- Test facility, date, report reference

**Use Cases**:
- Validate hydrostatics calculations
- Validate resistance predictions
- Benchmark solver accuracy
- Regression testing

**Sources**:
- ITTC workshop reports
- SIMMAN 2008 results
- MARIN, HSVA, Iowa towing tank publications

**Estimated Effort**: 1-2 weeks (data collection + import)

---

## 📋 Planned Features

### 12. 3D Geometry Viewer

**Status**: 📋 Planned  
**Priority**: Medium  
**Complexity**: M  
**Phase**: Phase 3

**Description**: Interactive 3D viewer in catalog hull detail page.

**Features**:
- Three.js hull rendering
- Orbit controls (zoom, pan, rotate)
- Waterline/station overlays
- Perpendicular markers
- Dimensions display

**Code Locations** (to create):
- Frontend: `frontend/src/components/catalog/Hull3DViewer.tsx`

**Estimated Effort**: 2-3 days

**Dependencies**: Hull geometry data available

---

### 13. Custom Hull Upload

**Status**: 📋 Planned  
**Priority**: Low  
**Complexity**: L  
**Phase**: Phase 4

**Description**: Allow users to add their own reference hulls to personal catalog.

**Features**:
- Upload offsets (CSV), IGES, or STEP
- Enter principal particulars
- Validation against geometry
- Private vs public flag
- Moderation workflow (if public)

**Estimated Effort**: 1-2 weeks

---

### 14. Propeller Performance Charts

**Status**: 📋 Planned  
**Priority**: Medium  
**Complexity**: M  
**Phase**: Phase 3

**Description**: Interactive charts for open-water propeller data.

**Features**:
- KT, KQ, η0 vs J curves
- Compare multiple propellers
- Select by required thrust
- Find optimal J for efficiency

**Estimated Effort**: 3-4 days

**Dependencies**: Wageningen B-series data

---

### 15. Comparative Analysis

**Status**: 📋 Planned  
**Priority**: Low  
**Complexity**: M  
**Phase**: Phase 4

**Description**: Compare multiple catalog hulls side-by-side.

**Features**:
- Select 2-5 hulls
- Table comparison (all particulars)
- Radar chart (form coefficients, ratios)
- Overlayed 2D profiles
- Export comparison report

**Estimated Effort**: 4-5 days

---

### 16. Export to CAD

**Status**: 📋 Planned  
**Priority**: Low  
**Complexity**: L  
**Phase**: Phase 4+

**Description**: Export catalog geometry to IGES/STEP for external CAD use.

**Formats**:
- IGES (3D surface)
- STEP (ISO 10303-21)
- DXF (2D lines plan)

**Estimated Effort**: 1 week

---

### 17. Community Contributions

**Status**: 📋 Planned  
**Priority**: Low  
**Complexity**: XL  
**Phase**: Phase 5+

**Description**: User-submitted hulls with moderation and verification.

**Features**:
- Submit custom hull
- Moderation queue
- Community voting
- Data quality checks
- Version control
- Automated bibliography generation

**Estimated Effort**: 3-4 weeks

---

## 🐛 Known Issues & Technical Debt

### High

1. **No Unit Tests for Catalog Services**
   - CatalogWaterService untested
   - CatalogHullsController untested
   - Clone workflow untested
   - **Fix**: Create test suite
   - **Effort**: 4-6 hours

2. **No E2E Test for Clone Workflow**
   - Catalog → Clone → Vessel → Hydrostatics workflow untested
   - **Fix**: Create Playwright E2E test
   - **Effort**: 2-3 hours

### Medium

3. **Geometry Storage Not Optimized**
   - Using JSON for large offset grids (potentially slow)
   - **Fix**: Consider binary storage or compression
   - **Effort**: 1-2 days

4. **No Data Quality Validation**
   - Catalog data not validated against geometry
   - Inconsistencies possible (e.g., Cb doesn't match actual block coefficient)
   - **Fix**: Add validation service
   - **Effort**: 3-4 days

5. **No Geometry Caching**
   - Geometry loaded from DB every time
   - **Fix**: Add caching layer (Redis or in-memory)
   - **Effort**: 4-6 hours

### Low

6. **Limited Search/Filter**
   - Only basic filtering by hull type
   - No search by dimensions, Cb range, etc.
   - **Fix**: Add advanced search
   - **Effort**: 2-3 days

7. **No Version Control for Catalog Data**
   - Updates overwrite previous data
   - No history tracking
   - **Fix**: Add version/audit table
   - **Effort**: 1-2 days

---

## 📈 Test Coverage

**Overall**: 0% (no tests yet)

**Gaps**:
- No unit tests for CatalogWaterService
- No unit tests for CatalogHullsController
- No integration tests for clone workflow
- No E2E tests for catalog browsing
- No validation tests for water property interpolation

**Test Priorities**:
1. **CatalogWaterService interpolation** (1-2 hours)
   - Test 0-30°C range
   - Test boundary conditions
   - Test salinity variations

2. **Clone workflow** (2 hours)
   - Test catalog hull → user vessel
   - Test geometry deep copy
   - Test tenant isolation

3. **E2E catalog browsing** (2 hours)
   - Browse catalog
   - View hull details
   - Clone to workspace
   - Verify in vessel list

---

## 🎯 Next Steps (Priority Order)

### Immediate (Can Do Now)
1. **Write unit tests** for water service and clone workflow (6 hours)
2. **Manual E2E testing** with Wigley hull (2 hours)
3. **Document geometry file formats** (CSV, IGES) (2 hours)

### Short-Term (Waiting on Data)
4. **Obtain SIMMAN IGES files** (external task)
5. **Obtain Wageningen B-series data** (external task)
6. **Evaluate IxMilia.Iges library** (4 hours)

### Medium-Term (After Data Available)
7. **Implement IGES import** (1-2 weeks)
8. **Import benchmark hull geometries** (3-5 days)
9. **Import Wageningen B-series** (2-3 days)
10. **Add 3D geometry viewer** (2-3 days)

---

## 📚 Related Documentation

### Implementation Summaries
- `CATALOG_IMPLEMENTATION_SUMMARY.md` - Complete feature documentation
- `temp/CATALOG_NEXT_STEPS.md` - **CRITICAL** - Blockers and plans
- `temp/CATALOG_SOURCES.md` - Data source references

### Test Data
- `temp/WIGLEY_FIX_SUMMARY.md` - Wigley hull bug fix

### Schema
- `backend/Shared/Models/` - Catalog model classes
- `backend/DataService/Migrations/` - Database schema

---

## 🏆 Success Metrics

**Current Status**: 53% Complete - Foundation Ready

| Metric | Target | Current | Status |
|--------|--------|---------|--------|
| Database Schema | Complete | Complete | ✅ |
| Water Properties | Complete | Complete | ✅ |
| Reference Hulls | 10+ | 6 (1 with geometry) | 🟡 |
| Propeller Data | B-series | Demo only | 🚫 |
| Geometry Import | Working | Blocked | 🚫 |
| Clone Feature | Working | Working | ✅ |
| Test Coverage | >80% | 0% | 🔴 |

**Recommendation**: Foundation is solid and ready for use with Wigley hull. Geometry import blocked by external data dependencies. Manual CSV upload workaround available.

---

**Last Updated**: November 4, 2025  
**Module Owner**: Catalog Team  
**Next Review**: After external data files obtained











