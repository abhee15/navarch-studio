# Hull Sizing Module Features

**Module Status**: 50% Complete - Core Functional, Solver Needs Fixes  
**Last Updated**: November 4, 2025  
**Test Pass Rate**: 14/39 (36%) - **NEEDS IMPROVEMENT**

---

## 📊 Module Overview

The Hull Sizing module provides automated preliminary hull design using first-principles calculations, generating optimal hull candidates based on mission requirements.

### Key Capabilities
- Automated mission case creation
- Multi-candidate generation (typical: 5 candidates)
- CAD-style workspace with quad viewport
- Real-time parameter adjustment
- Interactive 3D visualization
- Comparison and analysis tools

### Current Status
✅ **Working**: Mission wizard, basic solver, workspace UI, claims forwarding  
🔧 **In Progress**: Parameter sliders (deployed), comparison mode  
🚫 **Blocked**: Production deployment (solver issues)  
📋 **Planned**: CAD exports, sensitivity analysis, optimization

---

## ✅ Completed Features

### 1. Mission Creation Wizard

**Status**: ✅ Complete  
**Priority**: Critical  
**Complexity**: M  
**Phase**: Phase 1

**Description**: 4-step wizard for creating hull sizing mission cases with comprehensive validation.

**Wizard Steps**:
1. **Basic Info**: Mission name, vessel type (commercial, government, research, pleasure)
2. **Cargo**: Cargo basis (volume/weight/TEU), cargo value, density field
3. **Speed & Constraints**: Service speed, max beam, max draft, max displacement
4. **Advanced Options**: Locks (Fn, L/B, B/T), hull family hints

**Features**:
- Form validation at each step
- Progress indicator
- Draft/save capability
- Cargo density auto-calculation for TEU

**Code Locations**:
- Frontend: `frontend/src/components/sizing/MissionWizard.tsx`
- Backend: `backend/HullSizingService/Controllers/MissionCaseController.cs`
- DTOs: `backend/Shared/DTOs/MissionCaseDto.cs`

**Performance**: <100ms for mission creation

**Related Docs**:
- `temp/PROGRESS-REPORT-NOV-4.md`
- `.plan/hull-sizing/plan/04-FRONTEND-PHASES.md`

---

### 2. First-Principles Solver

**Status**: ⚠️ Partial - Works but has convergence issues  
**Priority**: Critical  
**Complexity**: XL  
**Phase**: Phase 1

**Description**: Automated hull sizing solver using displacement closure, resistance estimation, and stability screening.

**Current State**:
- Generates 5 candidates in ~500ms
- Uses Newton-Raphson displacement closure
- Simplified Holtrop-Mennen resistance
- Stability screen (GMt, KB, roll period)
- Multi-objective scoring

**Solver Components**:
1. **Displacement Closure** - Converges Lpp/B/T to match displacement
2. **Holtrop Resistance** - Estimates EHP at service speed
3. **Stability Screen** - Basic GM and roll period checks
4. **Scoring** - Weights speed, efficiency, stability, buildability

**Test Results**: 14/39 passing (36%) - **CRITICAL ISSUE**

**Known Problems**:
- ❌ Displacement closure fails to converge for known solutions
- ❌ Beam constraint not respected (exceeds maxBeam)
- ❌ Stability screen decimal overflow
- ❌ Roll period formula incorrect (50s vs expected 5-30s)
- ⚠️ Froude number precision issues

**Code Locations**:
- Backend: `backend/HullSizingService/Services/Solver/`
  - `FirstPrinciplesSolver.cs`
  - `DisplacementClosureService.cs`
  - `HoltropResistanceService.cs`
  - `StabilityScreenService.cs`
- Tests: `backend/HullSizingService.Tests/Services/`

**Performance**: 
- Target: <500ms per candidate ✅ ACHIEVED
- Target: <2s for 5 candidates ✅ ACHIEVED
- Target: >95% convergence rate ❌ FAILING (~40%)

**Related Docs**:
- `.plan/hull-sizing/SOLVER-IMPROVEMENTS-NEEDED.md` - **CRITICAL READ**
- `.plan/hull-sizing/plan/05-SOLVER-ALGORITHM.md`
- `temp/SOLVER-VALIDATION-COMPLETE.md`

---

### 3. Candidate Workspace (CAD-Style)

**Status**: ✅ Complete  
**Priority**: High  
**Complexity**: XL  
**Phase**: Phase 1

**Description**: Professional CAD-style workspace with quad viewport layout for candidate exploration.

**Viewport Modes**:
1. **3D View**: Interactive Wigley hull with orbit controls
2. **Plan View**: Top-down with 7 waterlines, dimensions
3. **Profile View**: Side elevation with 5 buttocks, sheerline
4. **Sections View**: Body plan with 10 transverse sections

**Features**:
- Click header to maximize any view
- Smooth zoom animations (0.3s)
- Gradient backgrounds (ocean, sky, engineering themes)
- Hover tooltips with coordinates
- Active viewport indicator (pulsing dot)
- Keyboard shortcuts (1-4 for views, Q for quad, E for export)

**Visual Polish**:
- Staggered fade-in animations for waterlines
- Gradient stroke on midship section
- Glow effects on design waterline
- Professional dimensions panels
- Color-coded perpendiculars (AP red, FP green, Midship orange)

**Code Locations**:
- Frontend: `frontend/src/pages/sizing/CandidateWorkspace.tsx`
- Visualization: `frontend/src/components/visualization/`
  - `WigleyHull3D.tsx`
  - `Hull2DPlan.tsx`
  - `Hull2DProfile.tsx`
  - `Hull2DSections.tsx`
  - `ViewportQuadLayout.tsx`

**Performance**: 60 FPS rendering, <1.2s workspace load

**Related Docs**:
- `temp/HULL-SIZING-FINAL-STATUS.md` - Premium UI features
- `.plan/hull-sizing/CAD-LIKE-VISUALIZATION-PLAN.md`

---

### 4. Mission List & Management

**Status**: ✅ Complete  
**Priority**: Medium  
**Complexity**: M  
**Phase**: Phase 1

**Description**: Browse, search, filter, and manage hull sizing missions.

**Features**:
- **Quick Stats Dashboard**: Total missions, by type, avg speed
- **Search**: By mission name
- **Filter**: By vessel type (all, commercial, government, research, pleasure)
- **Cards**: 3D thumbnail, mission name, vessel type, speed, cargo
- **Actions**: Run solver, view results, delete
- **Delete Functionality**: Red button (visible on hover)

**Code Locations**:
- Frontend: `frontend/src/pages/sizing/MissionList.tsx`
- Store: `frontend/src/stores/SizingStore.ts`

**Related Docs**:
- `temp/PROGRESS-REPORT-NOV-4.md`

---

### 5. Results Grid with 3D Thumbnails

**Status**: ✅ Complete  
**Priority**: High  
**Complexity**: M  
**Phase**: Phase 1

**Description**: Grid display of candidate designs with interactive 3D hull thumbnails.

**Features**:
- 3D rotatable thumbnails (180px)
- Candidate scores and rankings
- Principal dimensions (Lpp, B, T, D)
- Form coefficients (Cb, Cp, Cwp)
- Performance metrics (Fn, EHP, SHP)
- Flags (warnings/errors)
- "Open Workspace" button

**Code Locations**:
- Frontend: `frontend/src/components/sizing/CandidateCard.tsx`
- Thumbnail: `frontend/src/components/visualization/Hull3DThumbnail.tsx`

---

### 6. KPI Panel (Grouped Metrics)

**Status**: ✅ Complete  
**Priority**: Medium  
**Complexity**: S  
**Phase**: Phase 1

**Description**: Comprehensive metrics dashboard in workspace sidebar.

**Metric Groups**:
1. **Principal Dimensions**: Lpp, B, T, D, Displacement
2. **Form Coefficients**: Cb, Cp, Cm, Cwp
3. **Ratios**: L/B, B/T, D/T, Lwl/λ
4. **Performance**: Δ, Fn, EHP, SHP
5. **Stability Estimates**: KB, LCB, GM
6. **Gradient Score**: Overall candidate ranking

**Visual Design**:
- Color-coded by category (blue, cyan, green, purple, orange)
- Gradient headers
- Hover effects
- Compact layout

**Code Locations**:
- Frontend: `frontend/src/pages/sizing/CandidateWorkspace.tsx` (KPI section)

---

### 7. Offsets Table Viewer

**Status**: ⚠️ Placeholder Data  
**Priority**: Low  
**Complexity**: S  
**Phase**: Phase 1

**Description**: Traditional naval architecture offsets table (stations × waterlines).

**Current State**:
- UI complete (11 stations × 7 waterlines)
- Shows placeholder "0.000" values
- Full geometry generation not yet implemented

**Future**: Connect to hull geometry generator

**Code Locations**:
- Frontend: `frontend/src/pages/sizing/CandidateWorkspace.tsx` (Offsets section)

---

### 8. Resistance Curve Panel

**Status**: ✅ Complete  
**Priority**: Medium  
**Complexity**: S  
**Phase**: Phase 1

**Description**: EHP vs. Speed curve visualization.

**Features**:
- Design speed marker
- Max hull speed indicator
- Froude number scale
- Recharts visualization

**Code Locations**:
- Frontend: `frontend/src/pages/sizing/CandidateWorkspace.tsx` (Resistance section)

---

### 9. Claims Forwarding (JWT)

**Status**: ✅ Complete  
**Priority**: Critical  
**Complexity**: M  
**Phase**: Phase 0

**Description**: Proper JWT claims forwarding from API Gateway to HullSizingService.

**Implementation**:
- API Gateway reads from `context.User` (JWT middleware)
- Forwards as HTTP headers (`X-Tenant-Id`, `X-User-Sub`, etc.)
- HullSizingService `ClaimsForwardingMiddleware` extracts headers
- Dev fallback: derives tenantId from userId if missing

**Security**:
- Tenant isolation enforced
- User context preserved across services
- Claims validated at each hop

**Code Locations**:
- API Gateway: `backend/ApiGateway/Middleware/ClaimsForwardingMiddleware.cs`
- Hull Sizing: `backend/HullSizingService/Program.cs` (middleware registration)

**Related Docs**:
- `temp/DEEP-ANALYSIS-CLAIMS-ISSUE.md`
- `temp/ROOT-CAUSE-ANALYSIS-COMPLETE.md`

---

## 🔧 In Progress Features

### 10. Real-Time Parameter Adjustment

**Status**: 🔧 In Progress - Deployed, needs end-to-end testing  
**Priority**: High  
**Complexity**: M  
**Phase**: Phase 2

**Description**: Interactive sliders for real-time hull parameter tweaking.

**Current State**:
- Backend API deployed: `POST /api/v1/hull-sizing/candidates/:id/adjust`
- Frontend sliders wired to API
- Debounced recomputation (300ms target)
- Loading indicators during adjustment

**Parameters Adjustable**:
- Lpp: ±30% range
- Beam: ±30% range
- Draft: ±30% range
- Cb: ±0.15 range

**Recomputation** (Fast Mode):
- Displacement recalculated from L × B × T × Cb
- Froude number updated
- Lwl/λ ratio recomputed
- Target: <300ms response

**Remaining Work**:
- End-to-end testing in production
- Verify API response times
- Test error handling
- Full physics mode (integrate resistance + stability)

**Estimated Completion**: This week (testing only)

**Code Locations**:
- Backend: `backend/HullSizingService/Controllers/CandidatesController.cs`
- Frontend: `frontend/src/pages/sizing/CandidateWorkspace.tsx` (ParameterSliders)

**Related Docs**:
- `temp/PROGRESS-REPORT-NOV-4.md`
- `temp/HULL-SIZING-NEXT-STEPS.md`

---

### 11. Comparison Mode

**Status**: 🔧 In Progress - UI complete, backend partial  
**Priority**: High  
**Complexity**: M  
**Phase**: Phase 2

**Description**: Side-by-side comparison of up to 3 candidates.

**Current State**:
- UI components created (`ComparisonView.tsx`)
- Side-by-side card layout
- Batch selection in results grid
- Backend endpoints partially implemented

**Planned Features**:
- Overlay 2D profiles for visual comparison
- Radar chart for multi-attribute comparison
- Export comparison report (PDF)
- "Why is this better?" AI explanation

**Remaining Work**:
- Wire backend comparison endpoint
- Implement profile overlay logic
- Create radar chart visualization
- Add PDF export

**Estimated Completion**: 2-3 days

**Code Locations**:
- Frontend: `frontend/src/components/sizing/ComparisonView.tsx`
- Backend: `backend/HullSizingService/Controllers/ComparisonController.cs` (partial)

**Related Docs**:
- `temp/COMPARISON_MODE_IMPLEMENTATION_SUMMARY.md`

---

## 📋 Planned Features

### 12. DXF/IGES/STL Export

**Status**: 📋 Planned  
**Priority**: High  
**Complexity**: L  
**Phase**: Phase 2

**Description**: Export hull geometry to standard CAD formats.

**Export Formats**:
1. **DXF** - 2D lines plan (AutoCAD compatible)
2. **IGES** - 3D surface for Rhino/SolidWorks
3. **STL** - 3D mesh for 3D printing/CFD

**Estimated Effort**: 4-6 days

**Dependencies**: Full hull geometry generation (stations, waterlines, offsets)

**Code Locations** (to create):
- Backend: `backend/HullSizingService/Services/ExportService.cs`
- Frontend: Export button in workspace

**Related Docs**:
- `temp/HULL-SIZING-NEXT-STEPS.md`

---

### 13. Sensitivity Analysis Dashboard

**Status**: 📋 Planned  
**Priority**: High  
**Complexity**: M  
**Phase**: Phase 2

**Description**: Visualize ±10% impact of each parameter on key metrics.

**Features**:
- Tornado charts (parameter impact ranking)
- ±10% variation analysis
- Show impact on: Speed, Displacement, Stability (GM), Power (EHP)
- Highlight parameters with highest impact
- "Optimize for..." presets (speed, efficiency, stability)

**Estimated Effort**: 2-3 days

**Code Locations** (to create):
- Frontend: `frontend/src/components/sizing/SensitivityDashboard.tsx`
- Backend: `backend/HullSizingService/Controllers/AnalysisController.cs`

---

### 14. Design Space Exploration

**Status**: 📋 Planned  
**Priority**: Medium  
**Complexity**: M  
**Phase**: Phase 2

**Description**: Generate and visualize multiple design variants automatically.

**Features**:
- "Generate Variants" button creates 10 designs
- Vary key parameters systematically
- Scatter plot visualization (e.g., Speed vs Displacement)
- Click point to view design details
- Export all variants to CSV

**Estimated Effort**: 3-4 days

---

### 15. Batch Operations

**Status**: 📋 Planned (UI created)  
**Priority**: Medium  
**Complexity**: S  
**Phase**: Phase 2

**Description**: Perform operations on multiple candidates at once.

**Current State**: UI components created (`BatchActions.tsx`)

**Features**:
- Batch delete
- Batch export (ZIP file with multiple formats)
- Batch tag/organize missions
- Bulk status updates

**Remaining Work**:
- Wire backend batch endpoints
- Implement ZIP generation
- Test multi-select workflow

**Estimated Effort**: 1-2 days

---

### 16. Multi-Objective Optimization

**Status**: 📋 Planned  
**Priority**: Low  
**Complexity**: XL  
**Phase**: Phase 3

**Description**: Pareto frontier visualization for trade-off analysis.

**Features**:
- Pareto optimal candidates highlighted
- Trade-off analysis (speed vs fuel vs cost)
- Constraint relaxation suggestions
- Interactive Pareto chart

**Estimated Effort**: 1-2 weeks

---

### 17. AI-Assisted Design

**Status**: 📋 Planned  
**Priority**: Low  
**Complexity**: XL  
**Phase**: Phase 3+

**Description**: Machine learning for design recommendations.

**Features**:
- "Suggest improvements" based on flags
- Learn from user preferences (which designs they select)
- Auto-tag missions by vessel type
- Predict convergence likelihood

**Estimated Effort**: 3-4 weeks

**Dependencies**: Collected user data, ML model training

---

### 18. Collaboration Features

**Status**: 📋 Planned  
**Priority**: Low  
**Complexity**: L  
**Phase**: Phase 4+

**Description**: Multi-user collaboration on designs.

**Features**:
- Share mission link with team
- Comments on candidate designs
- Version history for missions
- Design approval workflow
- Real-time co-editing

**Estimated Effort**: 2-3 weeks

---

## 🚫 Blocked Features

### 19. Series 60 Hull Generator

**Status**: 🚫 Blocked  
**Priority**: Medium  
**Complexity**: M  
**Phase**: Phase 2

**Description**: Generate Series 60 hull forms (CB = 0.60, 0.70, 0.80).

**Blocker**: Requires Todd (1963) hull form data or equivalent

**Workaround**: Using Wigley hull only

**Estimated Effort**: 2-3 days (once data available)

**Dependencies**: Catalog geometry data or reference publication

---

### 20. Data-Driven Mode

**Status**: 🚫 Blocked  
**Priority**: Low  
**Complexity**: XL  
**Phase**: Phase 3+

**Description**: Use catalog data to inform design instead of pure first-principles.

**Blocker**: Requires benchmark hull database with full particulars

**Features** (when implemented):
- Search catalog for similar vessels
- Use regression models trained on catalog data
- Interpolate between known hulls
- Higher accuracy than first-principles for covered range

**Estimated Effort**: 2-3 weeks (once catalog populated)

**Dependencies**: Catalog module with 20+ benchmark hulls

---

## 🐛 Known Issues & Technical Debt

### Critical (Blocks Production)

1. **Solver Convergence Failures** (36% pass rate)
   - Displacement closure doesn't converge for known solutions
   - Initial Lpp guess too poor
   - Newton loop adjustment factors too conservative
   - **Fix**: Improve initial guess, increase adjustment factors, add adaptive stepping
   - **Effort**: 8-12 hours
   - **Related**: `.plan/hull-sizing/SOLVER-IMPROVEMENTS-NEEDED.md`

2. **Beam Constraint Violation**
   - Result beam (18.5m) exceeds maxBeam (15m)
   - Constraint check after adjustment, but final value not clamped
   - **Fix**: Clamp beam/draft BEFORE next iteration
   - **Effort**: 2 hours

3. **Stability Screen Decimal Overflow**
   - `System.OverflowException` when GMt negative or very small
   - sqrt(negative) causes overflow
   - **Fix**: Guard against negative GM, clamp roll period calc
   - **Effort**: 2 hours

4. **Roll Period Formula Error**
   - T_roll = 50.8s (expected 5-30s)
   - Formula may be wrong or GM too small
   - **Fix**: Use proper radius of gyration formula
   - **Effort**: 1 hour

### High

5. **CI Workflow Skip Issue**
   - Backend builds skipped when only `backend/Shared/` changes
   - Requires manual deployment trigger
   - **Fix**: Remove `has-secrets` condition from workflow
   - **Effort**: 30 min
   - **Related**: `temp/WORKFLOW-SKIP-ISSUE.md`

6. **Property Name Mismatch (Potential)**
   - Backend DTO: `BeamM`, `DraftM` (PascalCase)
   - Frontend expects: `bM`, `tM` (camelCase)
   - Mitigation: JSON serialization configured for camelCase
   - **Fix**: Verify consistency, add null-safety checks
   - **Effort**: 30 min

### Medium

7. **Geometry Not Implemented**
   - Offsets table shows placeholder "0.000" values
   - Full hull geometry (stations, waterlines) TBD
   - **Fix**: Implement geometry generator from Wigley formula
   - **Effort**: 4-6 hours

8. **Missing Polly Resilience Policies**
   - No retry/circuit breaker for DataService HTTP calls
   - **Fix**: Add Polly policies for resilience
   - **Effort**: 1 hour
   - **Related**: `.plan/hull-sizing/plan/11-FUTURE-IMPROVEMENTS.md`

9. **Missing Database CHECK Constraints**
   - No database-level validation for dimensions, coefficients
   - **Fix**: Add CHECK constraints in migration
   - **Effort**: 1 hour

10. **No Integration Tests**
    - Only unit tests exist
    - Need end-to-end workflow tests
    - **Effort**: 4-6 hours

### Low

11. **Simplified Holtrop-Mennen**
    - Using simplified polynomial instead of full 20-term formula
    - ±20-30% accuracy reduction
    - **Fix**: Implement full Holtrop 1984 method
    - **Effort**: 2-3 days

12. **Form Factor Improvements**
    - Single formula for all hull types
    - Should use different formulas for slender vs full hulls
    - **Effort**: 4-6 hours

---

## 📈 Test Coverage

**Overall**: 14/39 tests passing (36%) - **CRITICAL ISSUE**

| Test Suite | Passing | Total | % |
|------------|---------|-------|---|
| Resistance Service | 7 | 11 | 64% |
| Stability Service | 4 | 10 | 40% |
| Displacement Closure | 2 | 9 | 22% |
| Full Solver | 1 | 9 | 11% |

**Performance Tests** (for passing tests):
- ✅ Closure: <100ms
- ✅ Resistance: <50ms
- ✅ Stability: <10ms
- ✅ Full solver: <2s

**Gaps**:
- Most solver tests failing due to convergence issues
- No integration tests
- No E2E tests
- No frontend component tests
- No performance regression tests

**Test Improvements Needed**:
1. Fix critical solver bugs to get pass rate >90%
2. Add integration tests for full workflows
3. Create E2E test: wizard → solver → workspace → export
4. Add frontend component tests
5. Performance benchmarks

---

## 🎯 Next Steps (Priority Order)

### Sprint 1 (This Week) - Critical Fixes
1. **Fix solver convergence** (displacement closure algorithm) - 8 hours
2. **Fix beam constraint enforcement** - 2 hours
3. **Fix stability overflow** - 2 hours
4. **Fix roll period formula** - 1 hour
5. **Test parameter sliders end-to-end** - 2 hours

**Goal**: Get solver test pass rate to >90%

### Sprint 2 (Next Week) - Stabilization
6. **Fix CI workflow skip issue** - 30 min
7. **Add Polly resilience policies** - 1 hour
8. **Add database CHECK constraints** - 1 hour
9. **Complete comparison mode** - 2-3 days
10. **Create integration tests** - 4-6 hours

**Goal**: Production-ready with confidence

### Sprint 3 (Week 3) - Features
11. **DXF export implementation** - 4-6 days
12. **Sensitivity analysis dashboard** - 2-3 days
13. **Full hull geometry generation** - 4-6 hours

**Goal**: CAD export capability

---

## 📚 Related Documentation

### Critical Reading
- `.plan/hull-sizing/SOLVER-IMPROVEMENTS-NEEDED.md` - **MUST READ for solver work**
- `temp/PROGRESS-REPORT-NOV-4.md` - Latest status
- `temp/HULL-SIZING-FINAL-STATUS.md` - Production readiness assessment

### Implementation Details
- `temp/HULL-SIZING-NEXT-STEPS.md` - Roadmap
- `temp/SOLVER-VALIDATION-COMPLETE.md` - Test results
- `temp/DEEP-ANALYSIS-CLAIMS-ISSUE.md` - Claims forwarding fix
- `temp/WORKFLOW-SKIP-ISSUE.md` - CI/CD issue

### Plans
- `.plan/hull-sizing/plan/` - 13 detailed plan files
- `.plan/hull-sizing/CAD-LIKE-VISUALIZATION-PLAN.md`
- `.plan/hull-sizing/IMPLEMENTATION-STATUS.md`

---

## 🏆 Success Metrics

**Production Readiness**: 🚫 NOT YET (solver issues)

| Metric | Target | Current | Status |
|--------|--------|---------|--------|
| Test Pass Rate | >90% | 36% | 🔴 |
| Solver Convergence | >95% | ~40% | 🔴 |
| Performance | <2s | <2s | ✅ |
| UI Complete | 100% | 100% | ✅ |
| End-to-End Flow | Working | Working | ✅ |
| Claims Forwarding | Working | Working | ✅ |

**Recommendation**: Fix solver convergence issues before production deployment. UI and infrastructure are production-ready.

---

**Last Updated**: November 4, 2025  
**Module Owner**: Hull Sizing Team  
**Next Review**: November 11, 2025 (post-solver fixes)











