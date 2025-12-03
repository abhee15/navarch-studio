# 3D View Bow/Stern Orientation Issue - Visual Evidence

**Date**: December 2, 2025  
**Reporter**: User Testing on AWS Dev Environment  
**Status**: ⚠️ **CONFIRMED - NO BOW/STERN LABELS IN 3D VIEWS**

---

## ISSUE CONFIRMED

**User Statement**: "Not unclear there are no bow and stern plotting on the designs"

**Translation**: It's very clear that bow and stern labels are NOT being shown on the 3D design visualizations.

---

## VISUAL EVIDENCE

### Test Case: Generated Design - Candidate #1
- **Brief**: 5000 TEU Container Feeder (49,997t cargo, 11 kn)
- **Generated**: 5 candidates with ShipD geometry
- **Vessel**: Lpp 232.6m, Beam 40.4m, Draft 14.4m
- **Environment**: AWS Dev (https://d16ae133ahbxsm.cloudfront.net)

### Screenshot Captured
**File**: `design-results-no-bow-stern-labels.png`  
**Location**: Workspace view with quad layout (all 4 views visible)

---

## ANALYSIS BY VIEW

### ✅ Plan View (Top-Down) - **HAS LABELS**
**Orientation Markers Present**:
- ✅ "AP" label visible (Aft Perpendicular = **STERN**, left side)
- ✅ "FP" label visible (Forward Perpendicular = **BOW**, right side)
- ✅ Waterlines labeled (WL0 through WL12)
- ✅ LCB (Longitudinal Center of Buoyancy) marker
- ✅ Perpendicular lines clearly marked

**Result**: User can immediately identify bow vs stern ✅

---

### ✅ Profile View (Side Elevation) - **HAS LABELS**
**Orientation Markers Present**:
- ✅ Perpendicular lines (AP and FP) visible
- ✅ Buttocks labeled (BL0, BL2, BL4)
- ✅ Baseline, waterline, sheerline visible
- ✅ Toggle buttons for sheerline, buttocks, waterline, baseline, perpendiculars

**Result**: User can identify bow vs stern by perpendicular lines ✅

---

### ✅ Sections View (Body Plan) - **HAS LABELS**
**Orientation Markers Present**:
- ✅ "← AFT (0-4)" label (red, left side)
- ✅ "FORWARD (6-10) →" label (green, right side)
- ✅ "⊥ MIDSHIP (Station 5)" label (center)
- ✅ Station numbers labeled (0-59)

**Result**: User can immediately identify forward vs aft sections ✅

---

### ❌ 3D Isometric View - **NO LABELS**
**Orientation Markers Present**:
- ❌ **NO "BOW" or "STERN" text labels**
- ❌ **NO forward direction arrow**
- ❌ **NO color coding (e.g., bow=green, stern=red)**
- ❌ **NO coordinate axis indicator**
- ❌ **NO perpendicular planes or markers**

**What IS visible**:
- ✅ 3D wireframe hull (light blue)
- ✅ Grid at waterline level
- ✅ "Drag to rotate • Scroll to zoom" hint
- ✅ Reset button
- ✅ Toggle buttons (Waterplane, Grid, Centers)

**Result**: ❌ **User CANNOT identify bow vs stern without rotating hull** ❌

---

## USER EXPERIENCE IMPACT

### Current Behavior
1. User generates hull designs
2. Views results in workspace (quad layout)
3. Sees hull in 3D isometric view (lower right)
4. **Cannot tell which end is bow/stern**
5. Must rotate view OR check plan view to orient
6. Confusing and time-consuming

### Expected Behavior
1. User generates hull designs
2. Views results in workspace
3. **Immediately sees "BOW" and "STERN" labels** in 3D view
4. Understands orientation without interaction
5. Can rotate with confidence knowing which end is which

---

## COORDINATE SYSTEM VERIFICATION

### Three.js Coordinate System (Confirmed from Code)
```
X-axis: Transverse (Beam)
  - Negative X = Port side
  - Positive X = Starboard side

Y-axis: Vertical (Height)
  - Y = 0: Baseline (keel)
  - Y = draft: Design waterline
  - Positive Y = Up

Z-axis: Longitudinal (Length)
  - Z = 0: Aft Perpendicular (AP) = **STERN**
  - Z = Lpp: Forward Perpendicular (FP) = **BOW**
  - Positive Z = Forward direction
```

### Hull Positioning
- Hull is correctly positioned with:
  - Stern at Z = 0 (left side in top view)
  - Bow at Z = Lpp (right side in top view)
  - Centerline at X = 0
  - Keel at Y = 0

**Geometry is CORRECT**, labels are **MISSING**.

---

## AFFECTED USER WORKFLOWS

### 1. Hull Sizing - Design Generation
**Page**: Results page after running solver  
**User Action**: Reviewing generated designs  
**Issue**: Cannot tell bow from stern in 3D preview  
**Workaround**: Must check plan view or rotate hull  

### 2. Hull Sizing - Workspace View
**Page**: Detailed workspace for single candidate  
**User Action**: Analyzing hull geometry in quad view  
**Issue**: 3D view lacks orientation while 2D views have it  
**Workaround**: Reference plan view labels  

### 3. Hydrostatics - Workspace 3D View
**Page**: Hydrostatics analysis workspace  
**User Action**: Visualizing hull in 3D  
**Issue**: Same - no bow/stern labels  
**Workaround**: Rotate or check other views  

---

## ROOT CAUSE

### Missing Components in 3D Scene

**Files Affected**:
1. `frontend/src/components/sizing/visualization/Hull3DScene.tsx`
   - Used in: Hull Sizing results/workspace
   - Missing: Bow/stern text labels, direction arrow

2. `frontend/src/components/hydrostatics/Vessel3DViewer.tsx`
   - Used in: Hydrostatics workspace
   - Missing: Bow/stern text labels, direction arrow

**Why 2D Views Have Labels**:
- Plan view SVG includes explicit `<text>` elements for "AP" and "FP"
- Profile view SVG includes perpendicular lines
- Sections view SVG includes "AFT" and "FORWARD" text
- All 2D views have static, pre-positioned labels

**Why 3D View Has No Labels**:
- 3D scene renders only hull geometry mesh
- No Three.js `<Text>` components added for labels
- No arrow helpers for direction
- No visual orientation cues implemented

---

## PRIORITY & IMPACT

**Priority**: **P0 (Critical UX Issue)**

**Impact**:
- **Severity**: High - Affects all 3D visualizations across application
- **Frequency**: Every time user views 3D hull
- **Users Affected**: All users generating or viewing hull designs
- **Workaround Available**: Yes (rotate view, check 2D views)
- **User Frustration**: High - violates expectation that 3D view should be self-explanatory

**Business Impact**:
- Reduces confidence in application
- Increases time to interpret designs
- Creates confusion during presentations
- Inconsistent UX (2D has labels, 3D doesn't)

---

## RECOMMENDED FIXES (From Previous Analysis)

### Fix 1: Add Text Labels (P0)
```typescript
// In Hull3DScene.tsx and Vessel3DViewer.tsx
import { Text } from "@react-three/drei";

<Text
  position={[0, draft * 0.3, lpp * 0.6]}  // Near bow
  fontSize={lpp * 0.05}
  color="#22c55e"  // Green
  outlineWidth={lpp * 0.002}
  outlineColor="#000000"
>
  BOW (FP)
</Text>

<Text
  position={[0, draft * 0.3, -lpp * 0.6]}  // Near stern
  fontSize={lpp * 0.05}
  color="#ef4444"  // Red
  outlineWidth={lpp * 0.002}
  outlineColor="#000000"
>
  STERN (AP)
</Text>
```

### Fix 2: Add Direction Arrow (P0)
```typescript
<arrowHelper
  args={[
    [0, 0, 1],           // Direction (forward +Z)
    [0, 0, lpp * 0.5],   // Origin (at bow)
    lpp * 0.15,          // Length
    0x22c55e,            // Green color
  ]}
/>
```

### Fix 3: Make Labels Rotate with Hull (P0)
```typescript
// Wrap labels in same rotation group as hull
<group rotation={[0, Math.PI / 6, 0]}>
  {/* Hull mesh */}
  {/* Bow/stern labels */}
  {/* Direction arrow */}
</group>
```

---

## TESTING CHECKLIST

After implementing fixes:

- [ ] Generate hull designs and verify bow/stern labels appear in results 3D preview
- [ ] Open workspace and verify labels in quad view 3D panel
- [ ] Rotate 3D view and verify labels rotate with hull
- [ ] Maximize 3D view and verify labels remain visible
- [ ] Test with different vessel sizes (small/large) - labels should scale
- [ ] Test in Hydrostatics workspace 3D view
- [ ] Verify labels don't obstruct hull geometry
- [ ] Verify labels are readable in both light/dark themes
- [ ] Check mobile/responsive view (labels may need different sizing)

---

## CONCLUSION

**Issue Confirmed**: ✅  
**Evidence Captured**: ✅  
**Root Cause Identified**: ✅  
**Fix Proposed**: ✅  
**Priority**: P0 (Critical UX)  

**Next Steps**:
1. Implement text labels in both 3D view components
2. Add direction arrow for bow indication
3. Test across all hull sizing and hydrostatics workflows
4. Deploy to AWS dev for user verification
5. User acceptance testing

---

**User Quote**: "Not unclear there are no bow and stern plotting on the designs"

**Interpretation**: The lack of bow/stern labels in 3D views is glaringly obvious and needs to be fixed.

**Status**: Ready for implementation ✅

