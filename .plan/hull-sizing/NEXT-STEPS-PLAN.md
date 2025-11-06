# Hull Sizing - Next Steps Plan

**Date:** 2025-11-03  
**Current Status:** 95% MVP Complete - Ready for User Testing  
**Last Updated:** After test investigation and progress review

---

## 🎯 Executive Summary

**We've completed 95% of the MVP!** Here's what we do next:

**Immediate (Today):** Test end-to-end after deployment completes  
**Short-term (1-2 days):** Fix known issues (auto-deploy, JWT claims)  
**Medium-term (1-2 weeks):** Enhanced features (sliders, DXF export)  
**Long-term (1-2 months):** Advanced features (IGES, data-driven mode)

---

## ⏰ Immediate Actions (Next 30 Minutes)

### **1. Wait for Deployment to Complete**
**Status:** In progress (userId fix deployment)  
**ETA:** ~5-10 more minutes  
**Blocking:** Yes

**What's deploying:**
- Commit: `8ec0d73` - userId parsing fix
- Fix: `Guid.TryParse()` instead of `Guid.Parse()`
- Impact: Fixes 500 error when creating missions

**How to monitor:**
```bash
gh run list --limit 1 --json status,conclusion
```

---

### **2. End-to-End User Testing**
**Priority:** CRITICAL  
**Duration:** 30 minutes  
**Depends on:** Deployment complete

**Test Scenario:**
```
1. Login to https://d3mdtlqx5z2yf6.cloudfront.net
2. Navigate to Hull Sizing
3. Click "Create New Mission"
4. Fill wizard:
   Step 1: Name="Test Container", Type=Commercial, Basis=TEU, Count=500
   Step 2: Speed=18 kn
   Step 3: (Leave constraints empty)
   Step 4: Max Candidates=5, Family Hints=(optional)
5. Click "Generate Hulls"
6. Wait ~1-2 seconds
7. Verify:
   ✓ Candidates appear in grid
   ✓ 3D thumbnails render
   ✓ Scores and metrics shown
   ✓ No console errors
8. Click candidate card → workspace
9. Verify:
   ✓ 3D/2D views render
   ✓ Quad viewport works
   ✓ Click to maximize viewport
   ✓ Export SVG/PNG works
10. Navigate back → Compare mode
11. Select 2-3 candidates
12. Click "Compare"
13. Verify:
   ✓ Side-by-side comparison renders
   ✓ Metrics highlighted (best/worst)
```

**Expected:** All tests pass ✅  
**If fails:** Check browser console, App Runner logs

---

## 🔧 Short-Term Fixes (Next 1-2 Days)

### **3. Fix Auto-Deploy Workflow Issue**
**Priority:** MEDIUM  
**Duration:** 2 hours  
**Impact:** Developer velocity

**Problem:**
Push events skip `build-and-push` job, requiring manual `workflow_dispatch` trigger.

**Root Cause:**
`check-infrastructure.outputs.has-secrets` evaluates differently on push vs dispatch.

**Solution (Recommended):**
Remove the secrets check entirely - infrastructure is stable.

**Implementation:**
```yaml
# File: .github/workflows/ci-dev.yml (line 357)

# BEFORE:
build-and-push:
  needs: [...]
  if: needs.check-infrastructure.outputs.has-secrets == 'true'  # ❌ REMOVE

# AFTER:
build-and-push:
  needs: [frontend-quality, backend-quality, check-changed-paths, ...]
  # Always run after quality checks pass ✅
```

**Test:**
```bash
# Make a change
echo "// test" >> backend/Shared/Middleware/ClaimsForwardingMiddleware.cs
git add -A && git commit -m "test: auto-deploy" && git push

# Expected: build-and-push runs automatically
# Actual (before fix): skipped
```

**Acceptance Criteria:**
- ✅ Push to `backend/**` triggers automatic build
- ✅ Push to `frontend/**` triggers automatic frontend deploy
- ✅ No manual `workflow_dispatch` needed

---

### **4. Fix JWT Claims Forwarding (Proper Multi-Tenancy)**
**Priority:** MEDIUM  
**Duration:** 3-4 hours  
**Impact:** Production readiness

**Problem:**
Real JWT claims not being forwarded from API Gateway to downstream services.

**Current State:**
- Dev fallback allows testing (uses `dev-default-tenant`)
- Works for single-tenant development
- NOT production-ready (no real isolation)

**Investigation Steps:**
1. **Check if User is authenticated:**
   ```csharp
   // Add to ApiGateway/Services/HttpClientService.cs
   _logger.LogInformation("🔍 IsAuthenticated: {IsAuth}", 
       httpContext.User?.Identity?.IsAuthenticated);
   _logger.LogInformation("🔍 Claims count: {Count}", 
       httpContext.User?.Claims?.Count());
   ```

2. **Log all claims:**
   ```csharp
   foreach (var claim in httpContext.User?.Claims ?? Enumerable.Empty<Claim>())
   {
       _logger.LogInformation("🔍 Claim: {Type}={Value}", claim.Type, claim.Value);
   }
   ```

3. **Check JWT middleware:**
   - Is `JwtBearerAuthentication` populating `context.User`?
   - Are claims being mapped correctly?
   - Is `custom:tenantId` in the JWT?

**Expected Root Cause:**
JWT validation middleware not populating `httpContext.User.Claims`.

**Solution:**
Fix JWT middleware configuration in `backend/ApiGateway/Program.cs`.

**After Fix:**
- Remove dev fallback from `ClaimsForwardingMiddleware.cs`
- Verify multi-tenant isolation works
- Test with real Cognito JWT

**Acceptance Criteria:**
- ✅ `httpContext.User.Identity.IsAuthenticated == true`
- ✅ Claims populated: `sub`, `custom:tenantId`, `email`, `cognito:groups`
- ✅ Headers forwarded: `X-User-Sub`, `X-Tenant-Id`, etc.
- ✅ Multi-tenant isolation verified (users can't see other tenants' data)

---

## 🚀 Medium-Term Features (Next 1-2 Weeks)

### **5. Enhanced Solver UI - Interactive Sliders**
**Priority:** HIGH  
**Duration:** 4-6 hours  
**User Value:** HIGH

**What Exists:**
- ✅ Slider UI components (`ParameterSliders.tsx`)
- ✅ Debouncing support
- ✅ Visual feedback

**What's Missing:**
- ❌ Wire sliders to solver re-run
- ❌ Display recompute progress
- ❌ Show before/after comparison

**Implementation:**

1. **Add recompute endpoint:**
   ```csharp
   // POST /api/v1/hull-sizing/candidates/{id}/adjust
   public async Task<CandidateDesignDto> AdjustCandidate(
       Guid id,
       [FromBody] CandidateAdjustmentDto dto,
       CancellationToken ct)
   {
       // Get existing candidate
       var candidate = await _context.CandidateDesigns.FindAsync(id);
       
       // Run closure with new parameters
       var closureRequest = new ClosureRequest(
           TargetDisplacementT: dto.TargetDisplacementT ?? candidate.DisplacementT,
           // ... other params from dto or candidate
       );
       
       var closureResult = await _closureService.SolveAsync(closureRequest, ct);
       
       // Update candidate with new dimensions
       candidate.LppM = closureResult.LppM;
       candidate.BeamM = closureResult.BeamM;
       // ... etc
       
       await _context.SaveChangesAsync(ct);
       return MapToDto(candidate);
   }
   ```

2. **Wire frontend sliders:**
   ```typescript
   const handleSliderChange = async (param: string, value: number) => {
       setIsUpdating(true);
       
       const updated = await sizingApi.adjustCandidate(candidate.id, {
           [param]: value
       });
       
       runInAction(() => {
           // Update candidate in store
           sizingStore.updateCandidate(updated);
           setIsUpdating(false);
       });
   };
   ```

3. **Add comparison overlay:**
   - Show original candidate as ghost overlay
   - Highlight what changed (green = better, red = worse)
   - Display delta values (+2.5m length, -0.5m beam, etc.)

**Acceptance Criteria:**
- ✅ Slider drag updates candidate <300ms
- ✅ 3D/2D views update in real-time
- ✅ KPIs update (Fn, GMt, EHP, etc.)
- ✅ Original candidate shown as ghost
- ✅ Deltas displayed clearly

---

### **6. Sensitivity Analysis**
**Priority:** MEDIUM  
**Duration:** 3-4 hours  
**User Value:** HIGH

**Feature:**
Show how changes in one parameter affect others.

**UI Mockup:**
```
┌──────────────────────────────────────┐
│ 📊 Sensitivity Analysis              │
├──────────────────────────────────────┤
│ If Lpp increases by 10%:             │
│   Displacement: +33% (cube law)      │
│   EHP: +15% (wetted surface)         │
│   GMt: -5% (longer = less stable)    │
│   Material cost: +25%                │
│                                      │
│ [█████████░░] Lpp +10%               │
│                                      │
│ Recommended: Keep Lpp, increase B    │
└──────────────────────────────────────┘
```

**Implementation:**
```typescript
const calculateSensitivity = (param: string, delta: number) => {
    // Analytical approximations
    const sensitivities = {
        lpp: {
            displacement: 3.0 * delta, // Cube law
            ehp: 1.5 * delta,          // Wetted surface
            gmt: -0.5 * delta,         // Longer = less stable
        },
        beam: {
            displacement: 3.0 * delta, // Cube law
            gmt: 2.0 * delta,          // Wider = more stable
            ehp: 0.8 * delta,          // Form factor
        },
        // ... etc
    };
    
    return sensitivities[param];
};
```

**Acceptance Criteria:**
- ✅ Real-time sensitivity display on slider hover
- ✅ Shows impact on all key metrics
- ✅ Recommendations based on constraints
- ✅ "What-if" scenarios (e.g., "To add 100t cargo, need +5m Lpp")

---

### **7. DXF Export (2D Lines Plan)**
**Priority:** MEDIUM  
**Duration:** 6-8 hours  
**User Value:** HIGH (CAD interop)

**Scope:**
Export 2D hull lines (plan, profile, sections) to DXF format for AutoCAD.

**Library:**
Use `netDxf` (C# library, MIT license, mature)

**Installation:**
```bash
dotnet add backend/HullSizingService package netDxf
```

**Implementation:**

1. **Generate DXF layers:**
   ```csharp
   public byte[] ExportToDxf(CandidateDesign candidate)
   {
       var doc = new DxfDocument();
       
       // Layer 1: Hull outline (plan view)
       var hullLayer = new Layer("HULL_PLAN");
       hullLayer.Color = AciColor.Blue;
       doc.Layers.Add(hullLayer);
       
       // Add waterlines as polylines
       for (int i = 0; i < 7; i++)
       {
           var wl = GenerateWaterline(candidate, i);
           var polyline = new LwPolyline(wl.Points);
           polyline.Layer = hullLayer;
           doc.AddEntity(polyline);
       }
       
       // Layer 2: Stations
       var stationsLayer = new Layer("STATIONS");
       stationsLayer.Color = AciColor.Red;
       doc.Layers.Add(stationsLayer);
       
       // Add stations as lines
       for (int i = 0; i < 11; i++)
       {
           var station = GenerateStation(candidate, i);
           var line = new Line(station.Start, station.End);
           line.Layer = stationsLayer;
           doc.AddEntity(line);
       }
       
       // Layer 3: Dimensions
       var dimsLayer = new Layer("DIMENSIONS");
       dimsLayer.Color = AciColor.Green;
       doc.Layers.Add(dimsLayer);
       
       // Add dimensions
       var lppDim = new LinearDimension(/* ... */);
       lppDim.Layer = dimsLayer;
       doc.AddEntity(lppDim);
       
       // Save to memory stream
       using var ms = new MemoryStream();
       doc.Save(ms);
       return ms.ToArray();
   }
   ```

2. **API endpoint:**
   ```csharp
   [HttpGet("{id}/export/dxf")]
   public async Task<IActionResult> ExportDxf(Guid id, CancellationToken ct)
   {
       var candidate = await _service.GetByIdAsync(id, ct);
       if (candidate == null) return NotFound();
       
       var dxf = _exportService.ExportToDxf(candidate);
       
       return File(dxf, "application/dxf", $"{candidate.Name}_lines.dxf");
   }
   ```

3. **Frontend integration:**
   ```typescript
   const handleExportDXF = async () => {
       const blob = await sizingApi.exportCandidateDxf(candidate.id);
       const url = URL.createObjectURL(blob);
       const a = document.createElement('a');
       a.href = url;
       a.download = `${candidate.name}_lines.dxf`;
       a.click();
   };
   ```

**DXF Structure:**
```
Layers:
  - HULL_PLAN (blue): Waterlines, LWL, Lpp outline
  - HULL_PROFILE (red): Buttock lines, keel, deck
  - HULL_SECTIONS (green): Station curves (AP to FP)
  - STATIONS (cyan): Vertical station lines
  - WATERLINES (magenta): Horizontal waterline planes
  - DIMENSIONS (yellow): Length, beam, draft annotations
  - CENTERLINE (white): Symmetry axis
  - MARKERS (orange): LCB, LCG, KB points
```

**Acceptance Criteria:**
- ✅ DXF opens in AutoCAD without errors
- ✅ All layers properly separated
- ✅ Dimensions readable and accurate
- ✅ Coordinate system correct (bow = +X, port = +Y, up = +Z)
- ✅ Scale 1:1 (meters)
- ✅ File size <1 MB

---

## 🎨 Long-Term Features (Next 1-2 Months)

### **8. IGES/STEP Export (3D Surfaces)**
**Priority:** LOW  
**Duration:** 12-16 hours  
**User Value:** VERY HIGH (full CAD integration)

**Scope:**
Export 3D NURBS surfaces for import into Rhino, SolidWorks, CATIA, etc.

**Challenges:**
- NURBS surface generation from parametric hull
- Control point optimization for fairness
- Format compliance (IGES 5.3, STEP AP203)

**Libraries to Evaluate:**
- OpenCASCADE.NET (complex, powerful)
- Custom IGES writer (simpler, limited)
- STEP-NC-Machine (experimental)

**Deferred Reason:**
- DXF (2D) covers 80% of use cases for preliminary design
- IGES/STEP requires significant additional work
- Users can import DXF into CAD and loft surfaces manually

---

### **9. Data-Driven Mode (KNN + Regression)**
**Priority:** MEDIUM  
**Duration:** 8-10 hours  
**User Value:** MEDIUM (faster, uses real data)

**Concept:**
Use K-Nearest Neighbors to find similar vessels in catalog, then scale.

**Algorithm:**
```python
def data_driven_sizing(payload_t, speed_kn):
    # 1. Query catalog for similar vessels
    candidates = knn_search(
        catalog,
        features=[payload_t, speed_kn],
        k=10,
        distance='euclidean'
    )
    
    # 2. Scale by payload
    scaled = []
    for vessel in candidates:
        scale_factor = (payload_t / vessel.dwt) ** (1/3)  # Cube root
        scaled.append({
            'lpp': vessel.lpp * scale_factor,
            'beam': vessel.beam * scale_factor,
            'draft': vessel.draft * scale_factor,
            'cb': vessel.cb,  # Preserve
            'source': vessel.name,
            'confidence': similarity_score(vessel, target)
        })
    
    # 3. Refine with first-principles closure
    refined = [first_principles_closure(s) for s in scaled]
    
    # 4. Score by similarity + validity
    scored = [score_hybrid(r) for r in refined]
    
    return top_n(scored, 5)
```

**UI Toggle:**
```
Mode: ○ First-Principles  ● Data-Driven
      ↑ Physics-based     ↑ Catalog-based
      
Data-driven uses 324 reference vessels
Closest match: MAERSK E-CLASS (18,000 TEU)
Confidence: 87%
```

**Acceptance Criteria:**
- ✅ Toggle between first-principles and data-driven
- ✅ KNN search returns similar vessels
- ✅ Scaling preserves ratios correctly
- ✅ Provenance shown (which vessels influenced design)
- ✅ Confidence score displayed
- ✅ Hybrid mode: KNN initial guess → FP refinement

---

### **10. Advanced Stability (Intact GZ Curve)**
**Priority:** LOW  
**Duration:** 8-10 hours  
**User Value:** MEDIUM

**Scope:**
Full stability analysis with GZ curve, not just GMt estimate.

**Deferred Reason:**
- Full hydrostatics module exists for detailed stability
- MVP quick screen (GMt, roll period) is sufficient for preliminary sizing
- Users can push to hydrostatics for full analysis

---

## 📊 Prioritization Matrix

**High Priority & High Value:**
1. ✅ End-to-end testing (TODAY)
2. 🔧 Fix auto-deploy workflow (1-2 days)
3. 🎨 Enhanced solver UI - sliders (1-2 weeks)
4. 🎨 Sensitivity analysis (1-2 weeks)
5. 📤 DXF export (1-2 weeks)

**High Priority & Medium Value:**
6. 🔧 Fix JWT claims forwarding (1-2 days)

**Medium Priority & High Value:**
7. 🎨 Data-driven mode (1-2 months)
8. 📤 IGES/STEP export (1-2 months)

**Low Priority:**
9. Advanced stability (deferred, hydrostatics module exists)
10. Custom algorithm refinements (current is sufficient)

---

## 🎯 Recommended Focus

### **This Week:**
1. ✅ **Test end-to-end** (today, after deployment)
2. 🔧 **Fix auto-deploy** (2 hours, high ROI)
3. 🔧 **Fix JWT claims** (4 hours, production requirement)

### **Next Week:**
4. 🎨 **Enhanced sliders** (6 hours, high user value)
5. 🎨 **Sensitivity analysis** (4 hours, complements sliders)
6. 📤 **DXF export** (8 hours, CAD integration)

### **Month 2:**
7. 🎨 **Data-driven mode** (10 hours, alternative solver)
8. 📤 **IGES/STEP export** (16 hours, advanced CAD)
9. 📚 **User documentation** (8 hours, onboarding)
10. 🎥 **Demo video** (2 hours, marketing)

---

## 📈 Success Metrics

**After This Week:**
- ✅ 0 manual workflow triggers needed (auto-deploy fixed)
- ✅ Real multi-tenant isolation working (JWT claims fixed)
- ✅ End-to-end test successful (mission → hulls → workspace)

**After Month 1:**
- ✅ Interactive sliders working (<300ms response)
- ✅ DXF export to AutoCAD successful
- ✅ ≥10 missions created by users
- ✅ ≥50 hull designs generated

**After Month 2:**
- ✅ Data-driven mode operational
- ✅ IGES/STEP export working
- ✅ User guide published
- ✅ Demo video recorded
- ✅ ≥5 active users

---

## 🏆 Definition of Done (Full Release)

**Technical:**
- ✅ All tests passing (39/39)
- ✅ Auto-deploy on push working
- ✅ Multi-tenant isolation verified
- ✅ Performance targets met (<300ms, <2s, ≥45 FPS)
- ✅ Security scan clean (Trivy)

**Features:**
- ✅ First-principles solver working
- ✅ Interactive sliders functional
- ✅ DXF export to AutoCAD
- ⏳ IGES/STEP export (Phase 2+)
- ⏳ Data-driven mode (Phase 2+)

**Documentation:**
- ✅ Technical docs complete (13 files)
- ⏳ User guide written
- ⏳ API examples documented
- ⏳ Demo video recorded

**User Validation:**
- ⏳ ≥5 active users
- ⏳ ≥90% satisfaction (survey)
- ⏳ 0 critical bugs in production
- ⏳ Positive feedback on UI/UX

---

## 📝 Notes

### **What We've Accomplished:**
- 🎉 95% MVP complete in ~2 days of focused work
- 🎉 39/39 tests passing (100%)
- 🎉 Professional CAD-style UI
- 🎉 Production-ready solver (validated against references)
- 🎉 Deployed to AWS (ECR, App Runner, RDS)

### **What's Left:**
- 🔄 End-to-end testing (waiting for deployment)
- 🔧 2 known issues (auto-deploy, JWT claims)
- 🎨 3 high-value features (sliders, sensitivity, DXF)
- 📚 User documentation (guides, videos)

### **Timeline to Full Release:**
- **MVP Testing:** Today (30 min)
- **Bug Fixes:** 1-2 days (6 hours)
- **Core Features:** 1-2 weeks (18 hours)
- **Polish & Docs:** 2-4 weeks (18 hours)
- **Total:** ~4 weeks to full production release

---

**Next Action:** Wait for deployment, then test end-to-end! 🚀

**File Created:** 2025-11-03  
**Next Review:** After end-to-end testing completes





