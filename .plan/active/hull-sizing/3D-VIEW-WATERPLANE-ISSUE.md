# 3D Isometric View - Waterplane & Orientation Issues

**Date**: December 2, 2025  
**Reported By**: User  
**Status**: 🔍 Investigation Required

---

## REPORTED ISSUES

1. **Can't identify bow vs stern** in 3D isometric view (no labels/orientation markers)
2. **Waterplane to hull intersection doesn't look right**

---

## COORDINATE SYSTEM ANALYSIS

### Three.js Coordinate System (from code comments)

```typescript
// frontend/src/components/hydrostatics/Vessel3DViewer.tsx:62
// Three.js coordinate system: X = transverse (starboard/port), Y = vertical (up/down), Z = longitudinal (forward/back)
```

**Mapping**:
- **X-axis**: Transverse (beam) - Starboard (+X) / Port (-X)
- **Y-axis**: Vertical (height) - Up (+Y) / Down (-Y)
- **Z-axis**: Longitudinal (length) - Forward (+Z) / Back (-Z)

**Hull Origin**:
- X = 0: Centerline
- Y = 0: Baseline (keel)
- Z = 0: Aft Perpendicular (AP)
- Z = Lpp: Forward Perpendicular (FP)

---

## HULL GEOMETRY GENERATION

### Vertex Positioning (`Vessel3DViewer.tsx:268`)

```typescript
// Port side (negative X)
vertices.push(-halfBreadth, waterlineZ, stationX);

// Starboard side (positive X, mirrored)
vertices.push(-x, y, z);  // where x was originally -halfBreadth, so this becomes +halfBreadth
```

**Correct Positioning**: ✅
- Port: X = -halfBreadth
- Starboard: X = +halfBreadth
- Vertical: Y = waterlineZ
- Longitudinal: Z = stationX

---

## WATERPLANE RENDERING

### Current Implementation (`Vessel3DViewer.tsx:789-798`)

```typescript
function Waterplane({ lpp, beam, draft }: { lpp: number; beam: number; draft: number }) {
  if (!draft || draft <= 0) return null;

  // Three.js: X = transverse, Y = vertical, Z = longitudinal
  // Waterplane is horizontal at Y = draft, centered at Z = lpp/2
  return (
    <mesh position={[0, draft, lpp / 2]} rotation={[-Math.PI / 2, 0, 0]}>
      <planeGeometry args={[beam * 1.2, lpp * 1.2]} />
      <meshBasicMaterial color="#4299e1" opacity={0.3} transparent side={THREE.DoubleSide} />
    </mesh>
  );
}
```

### **POTENTIAL ISSUES IDENTIFIED** ⚠️

#### Issue 1: Waterplane Positioning

**Problem**: Waterplane is positioned at `[0, draft, lpp / 2]`

**Analysis**:
- X = 0 ✅ (centerline - correct)
- Y = draft ✅ (design waterplane height - correct)
- Z = lpp / 2 ✅ (centered longitudinally - correct)

**Rotation**: `[-Math.PI / 2, 0, 0]` rotates the plane from vertical (default) to horizontal ✅

**BUT**: The `planeGeometry` dimensions are `[beam * 1.2, lpp * 1.2]`

In THREE.js, `planeGeometry(width, height)`:
- **Width** maps to **X-axis**
- **Height** maps to **Y-axis** (before rotation)

After rotation by -90° around X-axis:
- **Width** (beam * 1.2) → **X-axis** ✅
- **Height** (lpp * 1.2) → **Z-axis** (after rotation) ✅

**Conclusion for Issue 1**: Positioning appears correct ✅

---

#### Issue 2: Waterplane May Not Be Visible

**Problem**: Waterplane only renders if `draft > 0`

**Check Required**:
1. What is the actual draft value being passed?
2. Is the waterplane being rendered at all?
3. Is the waterplane at the correct elevation relative to hull geometry?

**From AWS test data**:
```
Draft: 14.36m
```

So waterplane should render at Y = 14.36m. ✅

---

#### Issue 3: No Bow/Stern Labels in 3D View ⚠️

**Current State**: No orientation markers in 3D isometric view

**Expected**:
- Labels for "BOW" (FP) at Z = Lpp
- Labels for "STERN" (AP) at Z = 0
- Or visual cues (arrow, color gradient)

**Plan View (2D) Labels** (for reference):
- ✅ "FP" label at bow (right side)
- ✅ "AP" label at stern (left side)

**3D View**: ❌ No labels

---

## DIAGNOSIS STEPS

### Step 1: Verify Waterplane is Rendering

**Action**: Add console logging or visual debugging
```typescript
console.log("[Waterplane] Rendering at", { x: 0, y: draft, z: lpp / 2, dimensions: [beam * 1.2, lpp * 1.2] });
```

**Check**: Is the waterplane mesh being created?

### Step 2: Verify Hull-Waterplane Intersection

**Problem Statement**: "Waterplane to hull intersection is not right"

**Possible Causes**:
1. **Waterplane at wrong elevation**: If Y = draft doesn't match hull waterline heights
2. **Hull geometry wrong**: If hull waterlines don't match expected draft
3. **Coordinate system mismatch**: If hull uses different origin than waterplane

**Test**:
- Check hull geometry at Y = draft
- Should match waterline offsets at design draft
- Verify with plan view waterline (WL10 at 14.4m)

### Step 3: Add Orientation Markers to 3D View

**Recommendation**: Add visual cues for bow/stern

**Options**:
1. **Text Labels**: "BOW" and "STERN" at Z = Lpp and Z = 0
2. **Arrow**: Pointing forward at bow
3. **Color Gradient**: Hull color gradient from bow (blue) to stern (red)
4. **Axis Indicator**: Show X/Y/Z axes with labels

---

## RECOMMENDED FIXES

### Fix 1: Add Bow/Stern Labels (P0 - Critical)

**File**: `frontend/src/components/hydrostatics/Vessel3DViewer.tsx`

**Add component**:
```typescript
function OrientationMarkers({ lpp }: { lpp: number }) {
  return (
    <group>
      {/* Bow marker (forward perpendicular) */}
      <Text
        position={[0, 2, lpp + 5]}
        fontSize={3}
        color="#22c55e"
        anchorX="center"
        anchorY="middle"
      >
        BOW (FP)
      </Text>
      
      {/* Stern marker (aft perpendicular) */}
      <Text
        position={[0, 2, -5]}
        fontSize={3}
        color="#ef4444"
        anchorX="center"
        anchorY="middle"
      >
        STERN (AP)
      </Text>

      {/* Forward arrow */}
      <arrowHelper
        args={[
          new THREE.Vector3(0, 0, 1),  // Direction (forward)
          new THREE.Vector3(0, 0, lpp), // Origin (at bow)
          5,                             // Length
          0x22c55e,                      // Green color
        ]}
      />
    </group>
  );
}
```

**Integration**:
```typescript
<Scene lpp={lpp} beam={beam} designDraft={designDraft} draft={draft} ...>
  {/* Existing components */}
  <OrientationMarkers lpp={lpp} />
</Scene>
```

---

### Fix 2: Verify Waterplane Intersection (P0)

**Action**: Add visual debugging for waterplane elevation

**Add to Scene**:
```typescript
{/* Debug: Show waterplane elevation line */}
<Line
  points={[
    [-beam/2, draft, 0],
    [beam/2, draft, 0],
  ]}
  color="yellow"
  lineWidth={2}
/>
```

**Check**: Does the yellow line intersect the hull at the waterline?

---

### Fix 3: Improve 3D View Camera Angle (P1)

**Current**: Default isometric view may not clearly show bow/stern

**Recommendation**: Set initial camera position to better show orientation

**Default Camera Position**:
```typescript
// Current (likely)
position={[lpp * 0.8, lpp * 0.6, lpp * 0.8]}

// Recommended (better bow/stern visibility)
position={[lpp * 1.2, lpp * 0.4, -lpp * 0.3]}  // View from aft-quarter
```

---

## TESTING PLAN

1. **Test waterplane visibility**:
   - Check if waterplane mesh is rendered
   - Verify waterplane at correct Y elevation
   - Check if waterplane intersects hull at expected waterline

2. **Test orientation markers**:
   - Add BOW/STERN labels
   - Verify labels are visible and correctly positioned
   - Test with different camera angles

3. **Test hull-waterplane intersection**:
   - Compare 3D waterplane intersection with Plan View waterline (WL10)
   - Should match at design draft (14.36m in test case)

---

## NOTES

- Plan View (2D) correctly shows FP (bow) and AP (stern) labels
- Profile View (2D) correctly shows perpendiculars
- 3D Isometric View lacks orientation cues
- User expectation: 3D view should make bow/stern immediately obvious

---

## ACTION ITEMS

- [ ] P0: Add bow/stern labels to 3D isometric view
- [ ] P0: Verify waterplane is rendering at correct elevation
- [ ] P0: Check hull-waterplane intersection visually
- [ ] P1: Add orientation arrow pointing forward
- [ ] P1: Improve default camera angle for better bow/stern visibility
- [ ] P2: Consider adding color gradient (bow-to-stern) for visual orientation

---

**Priority**: P0 (User Experience - Critical)  
**Estimated Effort**: 2-3 hours  
**Dependencies**: None (frontend only)

