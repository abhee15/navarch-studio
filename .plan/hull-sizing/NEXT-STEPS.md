# Hull Sizing - Next Steps

**Current Status:** Phase 1 (MVP) - 95% Complete ✅  
**Blocking:** Final deployment in progress (userId fix)  
**Last Updated:** 2025-11-03

---

## 🎯 Immediate Next Steps (After Current Build)

### 1. **Verify End-to-End Functionality** ⏱️ 15 minutes
**Priority:** CRITICAL  
**Status:** Pending deployment

**Test Flow:**
```
1. Login to https://d3mdtlqx5z2yf6.cloudfront.net
2. Navigate to Hull Sizing
3. Create a new mission:
   - Name: "Test Container Feeder"
   - Type: Commercial
   - Cargo Basis: TEU
   - TEU Count: 500
   - Service Speed: 18 kn
   - Click "Generate Hulls"
4. Verify:
   ✓ No errors in console
   ✓ Candidates appear in grid
   ✓ 3D/2D visualizations render
   ✓ Can click into workspace
   ✓ Can compare candidates
   ✓ Can export SVG/PNG
```

**If successful:** Move to #2  
**If fails:** Debug with App Runner logs

---

## 🔧 Short-Term Improvements (Next 1-2 Days)

### 2. **Fix Remaining Solver Tests** ⏱️ 3-4 hours
**Priority:** HIGH  
**Impact:** Solver accuracy and reliability

**Status:** 7 tests failing (documented in `SOLVER-IMPROVEMENTS-NEEDED.md`)

**Tasks:**
- [ ] Fix `DisplacementClosureService` convergence for extreme constraints
- [ ] Improve `StabilityScreenService` GMt calculations for edge cases
- [ ] Refine `FirstPrinciplesSolver` scoring weights
- [ ] Add reference vessel validation tests (KCS, KVLCC2, Series-60)

**Files to modify:**
- `backend/HullSizingService.Tests/Services/DisplacementClosureServiceTests.cs`
- `backend/HullSizingService/Services/Solver/DisplacementClosureService.cs`
- `backend/HullSizingService/Services/Solver/StabilityScreenService.cs`
- `backend/HullSizingService/Services/Solver/FirstPrinciplesSolver.cs`

**Expected outcome:** 39/39 tests passing ✅

---

### 3. **Fix GitHub Actions Auto-Deploy Issue** ⏱️ 2 hours
**Priority:** MEDIUM  
**Impact:** Developer velocity

**Problem:** Push events skip build-and-push job (requires manual trigger)

**Options:**
- **A) Remove secrets check** (simplest, recommended)
  ```yaml
  build-and-push:
    needs: [frontend-quality, backend-quality, check-changed-paths, ...]
    # Remove: if: needs.check-infrastructure.outputs.has-secrets == 'true'
  ```

- **B) Add explicit debug logging** (investigative)
  ```yaml
  - name: Debug secrets
    run: |
      echo "Event: ${{ github.event_name }}"
      echo "ECR secret length: ${#SECRET}"
      if [ -n "${{ secrets.ECR_IDENTITY_SERVICE_URL }}" ]; then
        echo "has-secrets=true" >> $GITHUB_OUTPUT
      else
        echo "has-secrets=false" >> $GITHUB_OUTPUT
      fi
      cat $GITHUB_OUTPUT
  ```

- **C) Use environment variable** (cleanest)
  ```yaml
  env:
    ECR_URL: ${{ secrets.ECR_IDENTITY_SERVICE_URL }}
  steps:
    - run: if [ -z "$ECR_URL" ]; then ...
  ```

**Recommended:** Option A (infrastructure is stable, check is no longer needed)

**Files to modify:**
- `.github/workflows/ci-dev.yml` (line 357)

**Test:**
```bash
# Make a backend change
echo "// test" >> backend/Shared/Middleware/ClaimsForwardingMiddleware.cs
git add -A && git commit -m "test: trigger auto-deploy" && git push

# Expected: build-and-push runs automatically
# Actual (current): skipped, requires manual trigger
```

---

### 4. **Proper JWT Claims Forwarding** ⏱️ 3-4 hours
**Priority:** MEDIUM (works with dev fallback, but not production-ready)  
**Impact:** Multi-tenancy, security

**Problem:** JWT claims not being forwarded from API Gateway to downstream services

**Investigation needed:**
1. Is `httpContext.User.Identity.IsAuthenticated` true in API Gateway?
2. Are claims populated in `httpContext.User`?
3. Does Cognito JWT have `custom:tenantId` claim?

**Debug approach:**
```csharp
// Add to ApiGateway/Services/HttpClientService.cs
_logger.LogInformation("🔍 User authenticated: {IsAuth}", httpContext.User?.Identity?.IsAuthenticated);
_logger.LogInformation("🔍 Claims count: {Count}", httpContext.User?.Claims?.Count() ?? 0);
_logger.LogInformation("🔍 All claims: {Claims}", 
    string.Join(", ", httpContext.User?.Claims?.Select(c => $"{c.Type}={c.Value}") ?? Array.Empty<string>()));
```

**Expected root cause:** JWT validation middleware not populating `context.User`

**Files to investigate:**
- `backend/ApiGateway/Program.cs` (JWT authentication setup)
- `backend/ApiGateway/Services/HttpClientService.cs` (claims extraction)
- `backend/Shared/Middleware/ClaimsForwardingMiddleware.cs` (header reading)

**After fix:** Remove dev fallback from `ClaimsForwardingMiddleware.cs`

---

## 🚀 Medium-Term Features (Next 1-2 Weeks)

### 5. **Enhanced Solver Options** ⏱️ 4-6 hours
**Priority:** MEDIUM  
**User Value:** HIGH

**Features:**
- [ ] Live parameter adjustment with instant re-solve
- [ ] Sensitivity analysis (how changes affect design space)
- [ ] Constraint violation warnings before solving
- [ ] "Lock & Optimize" mode (lock one dimension, optimize others)
- [ ] Design space visualization (scatter plot: Cb vs Fn, L/B vs B/T)

**UI mockup:**
```
┌─────────────────────────────────────┐
│ 🎚️ Interactive Sliders             │
│ Lpp: [====|====] 150m               │
│ Beam: [===|=====] 22m               │
│ Draft: [====|===] 8.5m              │
│                                     │
│ [🔄 Re-solve] [📊 Sensitivity]     │
└─────────────────────────────────────┘
```

---

### 6. **Export Enhancements** ⏱️ 6-8 hours
**Priority:** MEDIUM  
**User Value:** HIGH (CAD interop)

**Phase 2 - DXF Export:**
- [ ] 2D DXF (plan, profile, sections as separate layers)
- [ ] Lines plan with waterlines/stations/buttocks
- [ ] Dimensions and annotations
- [ ] AutoCAD-compatible layer structure

**Phase 3 - IGES/STEP Export:**
- [ ] NURBS surface generation for hull
- [ ] Control point optimization
- [ ] Export to IGES (surfaces)
- [ ] Export to STEP (optional, surfaces + solids)
- [ ] Round-trip validation with AutoCAD/Rhino

**Libraries to evaluate:**
- **DXF:** `netDxf` (C#, MIT license)
- **IGES:** Custom implementation or IGES-5.3 spec
- **STEP:** `STEP-NC-Machine` or OpenCASCADE.NET

---

### 7. **Data-Driven Mode (Catalog + KNN)** ⏱️ 8-10 hours
**Priority:** LOW (first-principles works well)  
**User Value:** MEDIUM (faster, uses real data)

**Features:**
- [ ] Import vessel catalog CSV (expand beyond seed data)
- [ ] K-Nearest Neighbors search by payload/speed
- [ ] Hybrid mode: KNN initial guess → first-principles refinement
- [ ] Provenance tracking (show which reference vessels influenced design)
- [ ] Confidence scoring (how similar are references to target?)

**Data sources (free/public):**
- ✅ KCS, KVLCC2, Series-60 (already seeded)
- ✅ FAO fishing vessel database
- ⚠️ AIS data (check licensing before use)
- ⚠️ IHS Fairplay (commercial, expensive)

**Algorithm:**
```python
def data_driven_sizing(payload, speed):
    # 1. Query catalog for similar vessels
    candidates = knn_search(catalog, payload, speed, k=10)
    
    # 2. Scale to target payload
    scaled = [scale_by_payload(c, payload) for c in candidates]
    
    # 3. Refine with first-principles
    refined = [first_principles_closure(c, payload, speed) for c in scaled]
    
    # 4. Score by similarity + physical validity
    scored = [score_hybrid(c) for c in refined]
    
    return top_n(scored, 5)
```

---

### 8. **Performance Optimization** ⏱️ 4-6 hours
**Priority:** LOW (currently meets targets)  
**Trigger:** If user reports slow performance

**Current Targets (MEETING ✅):**
- Slider response: <300 ms
- Full solve: <2 s
- 3D rendering: ≥45 FPS

**If needed:**
- [ ] WebAssembly solver (C++ via Emscripten)
- [ ] GPU-accelerated mesh generation (WebGPU)
- [ ] Aggressive LOD (reduce mesh at distance)
- [ ] Server-side caching (memoize common solve scenarios)

---

## 🎨 Polish & UX Improvements (Ongoing)

### 9. **UI Enhancements** ⏱️ 2-3 hours each
**Priority:** LOW (nice-to-have)  

**Quick wins:**
- [ ] Loading skeletons instead of spinners
- [ ] Smooth scroll to results after solve
- [ ] Keyboard shortcuts legend (`?` to show)
- [ ] Dark mode toggle
- [ ] Undo/Redo for parameter changes
- [ ] "Save workspace" (persist view state)

**Advanced:**
- [ ] Guided tour for first-time users
- [ ] Inline help tooltips (explain Cb, Fn, GMt, etc.)
- [ ] Video tutorials embedded in UI
- [ ] "Example missions" gallery (click to load)

---

### 10. **Integration with Other Modules** ⏱️ 4-6 hours
**Priority:** MEDIUM  
**Dependencies:** Hydrostatics, Resistance modules complete

**Features:**
- [ ] "Push to Hydrostatics" from candidate workspace
  - Creates vessel in hydrostatics module
  - Pre-fills dimensions from hull sizing
  - Returns displacement curve, righting arm, BMt, etc.
  
- [ ] "Push to Resistance & Powering" from candidate workspace
  - Creates vessel in resistance module
  - Pre-fills dimensions + speed
  - Returns resistance curve, power curve, propeller recommendation

- [ ] Round-trip updates
  - User modifies in hydrostatics → suggests re-solve in hull sizing
  - "What-if" scenarios: "What if I need 20% more payload?"

**API design:**
```typescript
// From hull sizing workspace
const pushToHydro = async (candidateId: string) => {
  const response = await api.post('/hydrostatics/vessels', {
    sourceModule: 'hull-sizing',
    sourceCandidateId: candidateId,
    name: `${candidate.name} - Hydrostatics`,
    dimensions: {
      lpp: candidate.lppM,
      beam: candidate.beamM,
      draft: candidate.draftM,
      // ... other dims
    }
  });
  
  navigate(`/hydrostatics/vessels/${response.data.id}`);
};
```

---

## 📊 Analytics & Telemetry (Future)

### 11. **Usage Analytics** ⏱️ 6-8 hours
**Priority:** LOW  
**Business Value:** HIGH (understand user behavior)

**Metrics to track:**
- Solve count per day/user
- Average solver time
- Constraint violation frequency
- Export format popularity (JSON vs CSV vs DXF)
- Canvas interaction (zoom, pan, rotate)
- Feature adoption (locks, family hints, comparison mode)

**Implementation:**
- [ ] Event tracking (Mixpanel, Amplitude, or self-hosted)
- [ ] Performance monitoring (Sentry)
- [ ] User feedback widget
- [ ] Admin dashboard (view usage stats)

---

## 🧪 Testing & Quality

### 12. **Expand Test Coverage** ⏱️ 8-10 hours
**Priority:** MEDIUM  
**Current Coverage:** ~60% (backend), ~20% (frontend)

**Backend:**
- [ ] Integration tests (API → Service → DB)
- [ ] End-to-end solver tests with real scenarios
- [ ] Performance benchmarks (track regression)
- [ ] Load testing (100+ concurrent solves)

**Frontend:**
- [ ] Component tests (React Testing Library)
- [ ] Visual regression tests (Percy, Chromatic)
- [ ] E2E tests (Playwright)
  - Create mission → solve → workspace → export
- [ ] Accessibility tests (axe-core)

---

## 📝 Documentation

### 13. **User Documentation** ⏱️ 4-6 hours
**Priority:** MEDIUM  
**Audience:** End users (naval architects)

**Content:**
- [ ] Getting started guide
- [ ] Mission wizard walkthrough
- [ ] Solver options explained (locks, family hints)
- [ ] Interpretation guide (how to read Cb, Fn, GMt, etc.)
- [ ] Best practices (when to use locks, how to handle constraints)
- [ ] Troubleshooting (common errors, FAQ)
- [ ] Video tutorials (screen recordings)

**Format:** In-app help center (Markdown files served by frontend)

---

### 14. **Developer Documentation** ⏱️ 2-3 hours
**Priority:** LOW  
**Audience:** Future developers

**Content:**
- [ ] Architecture diagram (service boundaries, data flow)
- [ ] Database schema documentation
- [ ] API reference (OpenAPI/Swagger already generated)
- [ ] Solver algorithm deep-dive
- [ ] Testing guide (how to run, how to add tests)
- [ ] Deployment guide (manual vs CI/CD)

**Already documented:**
- ✅ Implementation plan (`.plan/hull-sizing/plan/`)
- ✅ Phase completion checklist
- ✅ Solver improvements needed

---

## 🎯 Success Metrics (Review After 1 Month)

**Adoption:**
- [ ] ≥10 missions created per week
- [ ] ≥50 solves per week
- [ ] ≥5 active users

**Performance:**
- [ ] 95% of solves complete in <2 seconds
- [ ] 0 crashes or 500 errors in logs
- [ ] Average FPS ≥45 in 3D viewport

**Quality:**
- [ ] ≤5% of solves produce invalid designs (constraint violations)
- [ ] ≥90% user satisfaction (survey or feedback)
- [ ] 0 critical bugs reported

**Business:**
- [ ] Positive ROI (time saved vs development cost)
- [ ] Feature requests prioritized by user value
- [ ] Integration with other modules complete

---

## 🚫 Out of Scope (Not Planning)

**These features were considered but deprioritized:**

1. **Real-time collaboration** (multiple users editing same mission)
   - Reason: Limited multi-user scenarios
   - Revisit: If enterprise adoption grows

2. **Mobile app** (iOS/Android)
   - Reason: CAD-style UI not suited for mobile
   - Alternative: Responsive web UI works on tablets

3. **AI-powered hull optimization** (ML model)
   - Reason: First-principles + data-driven is sufficient
   - Revisit: If we have ≥10,000 vessel dataset

4. **CFD integration** (run simulations)
   - Reason: Out of scope for preliminary design
   - Alternative: Export to OpenFOAM/STAR-CCM+ externally

5. **Cost estimation** (construction, operation)
   - Reason: Requires extensive cost database
   - Revisit: If we partner with shipyards

---

## 📅 Roadmap Summary

**Immediate (After Current Build):**
1. ✅ Verify end-to-end functionality
2. 🔧 Fix remaining solver tests
3. 🔧 Fix auto-deploy workflow

**Short-term (1-2 weeks):**
4. 🔧 Proper JWT claims forwarding
5. ✨ Enhanced solver options
6. 📤 DXF export (Phase 2)

**Medium-term (1-2 months):**
7. 📊 Data-driven mode
8. 🔗 Integration with hydrostatics/resistance
9. 📈 Usage analytics

**Long-term (3-6 months):**
10. 📤 IGES/STEP export (Phase 3)
11. 📚 Comprehensive documentation
12. 🧪 Expand test coverage
13. 🎨 Advanced UI polish

---

**Last Updated:** 2025-11-03  
**Next Review:** After current deployment completes  
**Owner:** Development Team


