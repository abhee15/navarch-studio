# Resistance & Powering Module Features

**Module Status**: 56% Complete - Good Progress  
**Last Updated**: November 4, 2025  
**Test Pass Rate**: 7/11 (64%)

---

## 📊 Module Overview

The Resistance & Powering (RES/PWR) module provides computational tools for estimating vessel resistance and power requirements using empirical methods.

### Key Capabilities
- Holtrop-Mennen resistance calculations
- Component breakdown analysis (friction, residuary, appendage, etc.)
- Speed-power-draft matrix (heatmap) analysis
- Interactive visualizations and comparisons
- Export to multiple formats (SVG, PNG, CSV)

### Current Status
✅ **Working**: Holtrop-Mennen (simplified), component breakdowns, heatmaps, charts  
⚠️ **Partial**: Simplified formula in use (full version planned)  
📋 **Planned**: Propeller integration, optimization, additional methods

---

## ✅ Completed Features

### 1. Holtrop-Mennen Resistance Calculation

**Status**: ⚠️ Partial - Simplified Version  
**Priority**: Critical  
**Complexity**: L  
**Phase**: Phase 2

**Description**: Industry-standard empirical resistance prediction method for preliminary design.

**Current State**:
- Simplified Holtrop-Mennen polynomial implemented
- Calculates: RF (friction), RR (residuary), RA (appendage), RCA (correlation), RAA (air)
- Works for typical ship forms (Cb 0.5-0.85)
- ±20-30% accuracy vs full method

**Components Calculated**:
1. **Friction Resistance (RF)** - ITTC 1957 friction line
2. **Residuary Resistance (RR)** - Wave + eddy making
3. **Appendage Resistance (RA)** - Rudder, bilge keels, etc.
4. **Correlation Allowance (RCA)** - Model-ship correlation
5. **Air Resistance (RAA)** - Above-water drag

**Formula Status**:
- ✅ Basic displacement, speed, dimensions
- ✅ Form factor (1+k1)
- ✅ Wetted surface (Holtrop S approximation)
- ⚠️ Simplified wave resistance (polynomial)
- ❌ Full 20-term Holtrop 1984 formula (planned)

**Code Locations**:
- Backend: `backend/DataService/Services/Resistance/HoltropMennenService.cs`
- Tests: `backend/DataService.Tests/Services/Resistance/HoltropMennenTests.cs`

**Test Results**: 7/11 tests passing (64%)  
**Performance**: <50ms per calculation

**Related Docs**:
- `temp/resistance-powering-implementation-plan.md`
- SPS SName paper 2014 (full formula reference)

**Future Enhancement**: Replace with full 20-term accurate formula

---

### 2. Component Breakdown Pie Charts

**Status**: ✅ Complete  
**Priority**: High  
**Complexity**: M  
**Phase**: Phase 2

**Description**: Visual breakdown of resistance components with comparison mode.

**Features**:
- Interactive pie chart (Recharts)
- Percentage labels on segments
- Tooltips with absolute values (kN)
- Color-coded components:
  - Friction (RF) - Blue
  - Residuary (RR) - Green
  - Appendage (RA) - Orange
  - Correlation (RCA) - Purple
  - Air (RAA) - Gray
- Legend with values

**Single View**: Display breakdown at one speed  
**Comparison Mode**: Side-by-side comparison of two speeds

**Code Locations**:
- Frontend: `frontend/src/components/resistance/ResistanceCharts.tsx`

**Related Docs**:
- `temp/FEATURE_1.5_COMPONENT_CONTRIBUTION_EXPANSION.md`
- `temp/resistance-pie-chart-implementation.md`

---

### 3. Two-Speed Comparison View

**Status**: ✅ Complete  
**Priority**: Medium  
**Complexity**: S  
**Phase**: Phase 2

**Description**: Compare resistance component breakdowns at two different speeds.

**Features**:
- Toggle button to switch between single/comparison views
- Two speed selection dropdowns (knots + m/s)
- Split view layout (responsive: 1 col mobile, 2 col desktop)
- Two side-by-side pie charts
- Comparison metrics panel showing:
  - Total resistance change (kN and %)
  - Component-wise deltas (RF, RR, RA, RCA, RAA)
  - Color-coded: red (increase), green (decrease), gray (no change)

**User Workflow**:
1. Click chart to select speed
2. Click "Compare Two Speeds" button
3. Select two speeds from dropdowns
4. View charts + metrics side-by-side
5. Toggle back to "Single View"

**Code Locations**:
- Frontend: `frontend/src/components/resistance/ResistanceCharts.tsx`

**Verification**:
- ✅ TypeScript compilation
- ✅ Linting passed
- ✅ Production build successful (57s)

**Related Docs**:
- `temp/FEATURE_1.5_COMPONENT_CONTRIBUTION_EXPANSION.md`

---

### 4. Speed-Power-Draft Heatmap

**Status**: ✅ Complete  
**Priority**: High  
**Complexity**: L  
**Phase**: Phase 2

**Description**: Interactive heatmap showing power/resistance across speed and draft ranges.

**Features**:
- **Batch Calculation**: Backend computes resistance for entire grid (speed × draft)
- **Interactive Heatmap**: Color-coded scatter plot (Recharts)
- **Dynamic Color Scale**: Blue → green → yellow → orange → red
- **Toggle**: Color by power (kW) or resistance (kN)
- **Click for Details**: Side panel shows:
  - Operating condition (speed, draft, Fn)
  - Full resistance breakdown (RF, RR, RA, RCA, RAA, RT)
  - EHP (Effective Horse Power)
  - Form coefficients at that draft (CB, CP, CM)
  - Non-dimensional parameters (Re, CF)
- **Design & Trial Points**: Overlay design conditions and trial data
- **Responsive**: Full-screen modal layout

**Configuration Panel**:
- Speed range (min/max) and steps (2-100)
- Draft range (min/max) and steps (2-100)
- Live feedback: resolution, % of design draft
- Total points and estimated time
- Validation (ensure valid ranges)

**Data Flow**:
1. User configures grid (e.g., 20 speeds × 15 drafts)
2. Backend generates linear grids
3. For each draft: compute hydrostatics → form coefficients
4. For each (speed, draft): calculate resistance using Holtrop-Mennen
5. Frontend renders heatmap from matrix data
6. User clicks point → detailed breakdown displayed

**Performance**:
- Grid resolution: Up to 100×100 points (10,000 max)
- Typical grid: 20×15 = 300 points
- Computation time: 5-30 seconds
- Results cached until next calculation

**Code Locations**:
- Backend: 
  - `backend/Shared/DTOs/SpeedDraftMatrixDto.cs`
  - `backend/DataService/Services/Resistance/SpeedDraftMatrixService.cs`
  - `backend/DataService/Controllers/ResistanceCalculationController.cs`
- Frontend:
  - `frontend/src/components/resistance/SpeedPowerDraftHeatmap.tsx`
  - `frontend/src/components/resistance/panels/SpeedDraftMatrixPanel.tsx`

**Verification**:
- ✅ Backend build successful
- ✅ Frontend type-check passed
- ✅ Production build successful

**Related Docs**:
- `temp/SPEED_DRAFT_MATRIX_IMPLEMENTATION.md`

---

### 5. Resistance Breakdown Panel

**Status**: ✅ Complete  
**Priority**: Medium  
**Complexity**: S  
**Phase**: Phase 2

**Description**: Detailed panel showing all resistance components in tabular format.

**Features**:
- Table with component names, values (kN), percentages
- Total resistance (RT)
- Effective power (EHP)
- Color-coded rows
- Expandable/collapsible

**Code Locations**:
- Frontend: `frontend/src/components/resistance/panels/ResistanceBreakdownPanel.tsx`

**Related Docs**:
- `temp/resistance-breakdown-implementation-summary.md`

---

### 6. Resistance Curves (EHP vs Speed)

**Status**: ✅ Complete  
**Priority**: Medium  
**Complexity**: S  
**Phase**: Phase 2

**Description**: Traditional resistance curve showing EHP vs speed.

**Features**:
- Line chart (Recharts)
- Design speed marker (vertical line)
- Max hull speed indicator
- Froude number scale on secondary axis
- Hover tooltips with values
- Grid lines

**Code Locations**:
- Frontend: `frontend/src/components/resistance/ResistanceCharts.tsx`

---

### 7. Stacked Area Chart (Component Contributions)

**Status**: ✅ Complete  
**Priority**: Medium  
**Complexity**: M  
**Phase**: Phase 2

**Description**: Stacked area chart showing how resistance components change with speed.

**Features**:
- Click to select speed (triggers pie chart update)
- Color-coded areas matching pie chart
- Legend with component names
- Tooltips with values at each speed
- Smooth interpolation

**Code Locations**:
- Frontend: `frontend/src/components/resistance/ResistanceCharts.tsx`

---

## 📋 Planned Features

### 8. Full Holtrop-Mennen 1984 Formula

**Status**: 📋 Planned  
**Priority**: High  
**Complexity**: L  
**Phase**: Phase 3

**Description**: Implement complete 20-term Holtrop-Mennen formula for higher accuracy.

**Current Limitation**: Simplified polynomial wave resistance (±20-30% error)

**Full Formula Includes**:
- All 20 coefficients (C1-C20)
- Bulbous bow corrections
- Transom stern corrections
- Accurately modeled wave resistance hump
- Speed-dependent form factor adjustments

**Estimated Effort**: 2-3 days

**Reference**: Holtrop & Mennen (1982, 1984), SPS SName paper 2014

**Code Locations** (to update):
- Backend: `backend/DataService/Services/Resistance/HoltropMennenService.cs`
- Tests: Add validation against published results

**Related Docs**:
- `.plan/hull-sizing/SOLVER-IMPROVEMENTS-NEEDED.md`

---

### 9. Form Factor Improvements

**Status**: 📋 Planned  
**Priority**: Medium  
**Complexity**: M  
**Phase**: Phase 3

**Description**: Use different form factor formulas for slender vs full hulls.

**Current State**: Single formula for all hull types

**Planned**:
- Slender hulls (Cb < 0.65): Use Holtrop 1984 formula
- Full hulls (Cb > 0.75): Use Holtrop 1988 update
- Intermediate: Interpolate between methods

**Estimated Effort**: 4-6 hours

---

### 10. Wetted Surface Validation

**Status**: 📋 Planned  
**Priority**: Medium  
**Complexity**: S  
**Phase**: Phase 3

**Description**: Add fallback for wetted surface calculation edge cases.

**Current Issue**: Simplified Holtrop S formula can return negative with extreme Cb/Cm values

**Planned Fix**:
- Validate S > 0
- If S < 0, use Moor's approximation as backup
- Log warning when fallback used

**Estimated Effort**: 2 hours

---

### 11. Propeller Open-Water Performance

**Status**: 📋 Planned  
**Priority**: High  
**Complexity**: M  
**Phase**: Phase 3

**Description**: Integrate propeller performance curves (Wageningen B-series).

**Features**:
- KT, KQ, η0 curves vs J (advance coefficient)
- Propeller selection based on required thrust
- Efficiency optimization
- Delivered power (PD) = EHP / η0

**Dependencies**: Catalog propeller B-series data

**Estimated Effort**: 3-5 days

**Related Docs**:
- `temp/CATALOG_NEXT_STEPS.md`

---

### 12. Shaft, Gearbox, Engine Losses

**Status**: 📋 Planned  
**Priority**: Medium  
**Complexity**: S  
**Phase**: Phase 3

**Description**: Calculate installed power accounting for transmission losses.

**Features**:
- Shaft efficiency (typical: 97-99%)
- Gearbox efficiency (typical: 95-98%)
- Delivered Power (PD) → Brake Power (PB) → Installed Power (PI)
- Configurable efficiency factors

**Estimated Effort**: 1-2 days

---

### 13. Additional Resistance Methods

**Status**: 📋 Planned  
**Priority**: Low  
**Complexity**: L  
**Phase**: Phase 4+

**Description**: Support alternative resistance prediction methods.

**Planned Methods**:
- **Guldhammer-Harvald**: For displacement vessels
- **Series 60**: Regression-based (if vessel matches)
- **Savitsky**: For planing hulls
- **Slender Body Theory**: For high-speed craft
- **CFD Integration**: API to external CFD solvers

**Estimated Effort**: 1 week per method

---

### 14. Optimization Engine

**Status**: 📋 Planned  
**Priority**: Medium  
**Complexity**: XL  
**Phase**: Phase 4+

**Description**: Find optimal speed/draft for minimum resistance or power.

**Features**:
- Minimize resistance at given speed
- Minimize power for voyage
- Find optimal cruising speed for range
- Multi-objective: speed + efficiency + cost
- Constraint handling (draft limits, speed limits)

**Algorithms**:
- Gradient descent
- Genetic algorithm
- Particle swarm optimization

**Estimated Effort**: 2-3 weeks

---

### 15. Seakeeping Integration

**Status**: 📋 Planned  
**Priority**: Low  
**Complexity**: XL  
**Phase**: Phase 4+

**Description**: Add wave resistance in seaway conditions.

**Features**:
- Added resistance in waves (RAW)
- Wave spectra (PM, JONSWAP)
- RAOs (Response Amplitude Operators)
- Operational profiles (% time in sea states)
- Voyage simulation

**Dependencies**: Seakeeping module

**Estimated Effort**: 3-4 weeks

---

## 🐛 Known Issues & Technical Debt

### High

1. **Simplified Holtrop Formula**
   - Using polynomial approximation instead of full formula
   - ±20-30% accuracy reduction
   - **Fix**: Implement full 20-term Holtrop 1984
   - **Effort**: 2-3 days

2. **Form Factor Single Formula**
   - Doesn't account for hull type (slender vs full)
   - **Fix**: Use Holtrop 1984/1988 by Cb range
   - **Effort**: 4-6 hours

3. **No Wetted Surface Validation**
   - Can return negative with extreme inputs
   - **Fix**: Add validation + Moor's fallback
   - **Effort**: 2 hours

### Medium

4. **No Propeller Integration**
   - Only calculates EHP, not delivered/brake/installed power
   - **Fix**: Implement propeller performance + transmission losses
   - **Effort**: 3-5 days
   - **Blocker**: Needs catalog propeller data

5. **Limited Hull Form Coverage**
   - Optimized for conventional displacement hulls
   - Poor accuracy for:
     - High-speed craft (Fn > 0.5)
     - Planing hulls
     - Catamarans/multihulls
     - Very slender or full forms
   - **Fix**: Add method selection by hull type
   - **Effort**: 1-2 weeks

6. **No Appendage Details**
   - Uses simplified appendage resistance
   - Doesn't account for specific appendage types/sizes
   - **Fix**: Detailed appendage resistance model
   - **Effort**: 1 week

### Low

7. **Temperature/Salinity Effects**
   - Uses standard water properties
   - Doesn't adjust for actual conditions
   - **Fix**: Integrate water properties from catalog
   - **Effort**: 4-6 hours

8. **No Roughness Allowance**
   - Assumes smooth hull
   - Real vessels have fouling/roughness
   - **Fix**: Add roughness factor input
   - **Effort**: 2 hours

---

## 📈 Test Coverage

**Overall**: 7/11 tests passing (64%)

| Test Area | Status | Notes |
|-----------|--------|-------|
| Basic Holtrop Calc | ✅ Pass | Friction, wetted surface, form factor |
| Component Breakdown | ✅ Pass | RF, RR, RA calculated correctly |
| Speed Matrix | ✅ Pass | Multiple speeds calculated |
| Edge Cases | ⚠️ Partial | Some extreme inputs fail |
| Accuracy Validation | ❌ Fail | ±30% error vs published data |

**Gaps**:
- No validation against benchmark hulls (KCS, Series 60)
- No comparison with towing tank data
- Limited edge case testing
- No performance regression tests
- No frontend component tests

**Test Improvements Needed**:
1. Add benchmark hull validation tests
2. Compare with published Holtrop results
3. Test extreme form coefficients
4. Add heatmap integration tests
5. Frontend component tests for charts

---

## 🎯 Next Steps (Priority Order)

### Sprint 1 (This Month)
1. **Full Holtrop-Mennen implementation** - 2-3 days
2. **Form factor improvements** - 4-6 hours
3. **Wetted surface validation** - 2 hours
4. **Benchmark validation tests** - 1 day

**Goal**: Improve accuracy to ±10% of published data

### Sprint 2 (Next Month)
5. **Propeller integration** (depends on catalog data) - 3-5 days
6. **Shaft/gearbox losses** - 1-2 days
7. **Temperature/salinity effects** - 4-6 hours
8. **Roughness allowance** - 2 hours

**Goal**: Complete power prediction chain (EHP → PI)

### Sprint 3 (Q1 2026)
9. **Optimization engine** - 2-3 weeks
10. **Additional methods** (Guldhammer-Harvald) - 1 week
11. **Seakeeping integration** - 3-4 weeks

**Goal**: Advanced analysis capabilities

---

## 📚 Related Documentation

### Implementation Summaries
- `temp/SPEED_DRAFT_MATRIX_IMPLEMENTATION.md` - Heatmap feature
- `temp/FEATURE_1.5_COMPONENT_CONTRIBUTION_EXPANSION.md` - Comparison mode
- `temp/resistance-powering-implementation-plan.md` - Original plan
- `temp/resistance-breakdown-implementation-summary.md` - Breakdown panel
- `temp/resistance-pie-chart-implementation.md` - Pie charts

### Plans & Improvements
- `.plan/hull-sizing/SOLVER-IMPROVEMENTS-NEEDED.md` - Solver accuracy needs
- `temp/resistance-powering-ui-analysis.md` - UI design

### References
- SPS SName paper 2014 - Full Holtrop-Mennen implementation
- Holtrop & Mennen (1982) - Original RINA paper
- Holtrop (1984) - Updated formulas
- ITTC Resistance Committee reports

---

## 🏆 Success Metrics

**Current Status**: 56% Complete - Good Progress

| Metric | Target | Current | Status |
|--------|--------|---------|--------|
| Core Calculations | Working | Working | ✅ |
| Test Coverage | >80% | 64% | 🟡 |
| Accuracy | ±10% | ±30% | ⚠️ |
| Performance | <100ms | <50ms | ✅ |
| Visualization | Complete | Complete | ✅ |
| Propeller Integration | Complete | Not Started | ❌ |

**Recommendation**: Functional for preliminary design. Improve accuracy with full Holtrop formula before high-fidelity work.

---

**Last Updated**: November 4, 2025  
**Module Owner**: Resistance & Powering Team  
**Next Review**: December 1, 2025 (post-accuracy improvements)








