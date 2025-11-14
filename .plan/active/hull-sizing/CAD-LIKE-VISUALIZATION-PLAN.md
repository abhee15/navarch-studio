# Hull Sizing - CAD-Like Visualization Plan
## Making It "AutoCAD-Level" Feature-Rich

**Goal:** Transform the 3D/2D visualization from "good enough" to "industry-leading"  
**Target Users:** Naval architects who currently use AutoCAD, Rhino, Maxsurf, AVEVA  
**Benchmark:** Match or exceed commercial ship design software

---

## 🎯 **Current State vs Target**

### **What We Have (Basic 3D)**
- ✅ Wigley parametric hull (60x40 mesh)
- ✅ Orbit controls (rotate, zoom, pan)
- ✅ Waterplane overlay
- ✅ Center markers (LCB, LCG, KB)
- ✅ Grid helper
- ✅ Legend with dimensions

### **What's Missing (AutoCAD-Level Features)**
- ❌ 2D plan/profile/sections (orthographic projections)
- ❌ Stations view (transverse sections every 0.1L)
- ❌ Waterlines view (horizontal sections every 0.5m)
- ❌ Buttocks view (vertical longitudinal sections)
- ❌ Dimensions & annotations
- ❌ Layers control (show/hide elements)
- ❌ Measurement tools (distance, area, volume)
- ❌ Comparison mode (ghost overlay)
- ❌ Export to CAD formats (DXF, IGES, STEP)
- ❌ Offsets table (numeric coordinates)
- ❌ Curvature analysis (fairness checking)
- ❌ Hull modification tools (drag points, adjust curves)

---

## 📐 **Phase 2A: Professional 2D Views** (Priority: CRITICAL)

### **1. Plan View (Top-Down Projection)**
**Component:** `HullPlanView2D.tsx`

**Features:**
- SVG-based rendering (scalable, exportable)
- Waterlines at draft intervals (every 0.5m or 10% T)
- Centerline (bow to stern)
- Perpendiculars (AP, FP, midship)
- Breadth molded lines (max beam)
- Dimensions annotations (Lpp, Lwl, LOA, B)
- Scale ruler (graphical scale bar)
- Station markers (every 10% Lpp: #0, #1, ..., #10)
- Bulbous bow outline (if applicable)
- Stern overhang

**Technologies:**
- SVG + D3.js (advanced path generation, scales, axes)
- OR: HTML Canvas (better performance for many curves)
- Export to SVG file (Save As)

**Reference Implementation:**
```typescript
// Waterline calculation (XY projection at depth Z)
const waterlinePoints = (zDepth: number) => {
  const points: [number, number][] = [];
  for (let i = 0; i <= numStations; i++) {
    const x = (i / numStations) * lpp - lpp/2;
    const y = wigleyFormula(x, zDepth, lpp, beam, draft);
    points.push([x, y]);
  }
  return points;
};

// Generate SVG path
const waterlinePath = waterlinePoints(draft * 0.5)
  .map(([x, y], i) => `${i === 0 ? 'M' : 'L'} ${x},${y}`)
  .join(' ');
```

**AutoCAD Equivalent:**
- View → Plan (top view)
- Freeze waterlines layer
- Annotate dimensions

---

### **2. Profile View (Side Elevation)**
**Component:** `HullProfileView2D.tsx`

**Features:**
- Hull outline (sheerline, keel, stem, stern)
- Waterline (at design draft)
- Buttock lines (vertical longitudinal sections: 0, 0.25B, 0.5B, 0.75B, B)
- Freeboard markers
- Deck line
- Baseline (keel)
- Dimensions: Lpp, LOA, T, D, freeboard
- Station markers (vertical lines)

**AutoCAD Equivalent:**
- View → Right elevation
- Show buttocks

---

### **3. Sections View (Body Plan)**
**Component:** `HullSectionsView2D.tsx`

**Features:**
- Transverse sections at stations (typically 10-20 stations)
- Forward half (bow) mirrored to starboard
- Aft half (stern) mirrored to port
- Centerline (vertical)
- Waterline (horizontal at T)
- Baseline (keel)
- Station labels (#0 AP, #10 FP)
- Half-breadth scale

**AutoCAD Equivalent:**
- View → Sections (body plan)
- Forward sections on right, aft on left

---

## 🎨 **Phase 2B: Advanced 3D Features** (Priority: HIGH)

### **4. Multiple Hull Forms (Beyond Wigley)**

**Series 60 Family:**
```typescript
// Adjustable Cb (0.60, 0.65, 0.70, 0.75, 0.80)
// More realistic for cargo ships
// Based on Taylor-Gertler systematic series
```

**KCS Hull (Container Ship):**
```typescript
// KRISO Container Ship (public domain)
// Includes bulbous bow
// Modern container ship form
```

**KVLCC2 (Tanker):**
```typescript
// KRISO Very Large Crude Carrier
// Full-form tanker (Cb ≈ 0.81)
// Realistic for oil/bulk carriers
```

**Planing Hull (High-Speed Craft):**
```typescript
// Hard chine, V-bottom
// Savitsky method for planing lift
// For ferries, patrol boats
```

**NURBS-Based Custom Hulls:**
```typescript
// Load from IGES/STEP files
// User-defined control points
// Catmull-Rom spline interpolation
```

**Package:** `verb-nurbs-web` (NURBS library for Three.js)

---

### **5. Curvature Analysis (Fairness Checking)**

**Heatmap Overlay:**
```typescript
// Compute Gaussian curvature at each vertex
// Color map: Blue (concave) → Green (flat) → Red (convex)
// Highlights unfair regions (inflection points, bumps)
```

**Longitudinal Curvature Plot:**
- Plot κ(x) along keel, sheer, chine
- Identify unfair transitions
- Suggest fairing adjustments

**Package:** `three-mesh-bvh` (for fast curvature computation)

---

### **6. Comparison Mode (Ghost Overlay)**

**Features:**
- Load 2-3 candidates simultaneously
- Ghost mode: Semi-transparent overlay (opacity 0.3)
- Color-coded: Candidate A (blue), B (green), C (red)
- Difference heatmap (show where hulls differ most)
- Side-by-side split screen
- Synced camera controls

**Implementation:**
```typescript
<Canvas>
  <WigleyHull3D candidate={candidateA} color="#3b82f6" opacity={0.8} />
  <WigleyHull3D candidate={candidateB} color="#10b981" opacity={0.3} /> {/* Ghost */}
  <WigleyHull3D candidate={candidateC} color="#ef4444" opacity={0.3} /> {/* Ghost */}
</Canvas>
```

---

### **7. Measurement Tools**

**Distance Measurement:**
- Click two points → show distance in meters
- Snap to vertices, centers, perpendiculars
- Display in legend

**Area Measurement:**
- Draw polygon on hull surface
- Calculate wetted surface area
- Calculate waterplane area

**Volume Verification:**
- Display computed volume (from mesh)
- Compare to analytical Δ = L·B·T·Cb
- Show discrepancy (should be <1%)

**Package:** `three-mesh-bvh` (ray casting, intersection tests)

---

### **8. Slicing Tools (Interactive Planes)**

**Draggable Cutting Planes:**
- Horizontal plane (waterlines) - drag up/down
- Vertical transverse (stations) - drag fore/aft
- Vertical longitudinal (buttocks) - drag port/starboard
- Real-time section curve display
- Export section coordinates

**Implementation:**
```typescript
// Plane mesh with TransformControls
<mesh position={[0, 0, -draft]} rotation={[-Math.PI/2, 0, 0]}>
  <planeGeometry args={[lpp, beam]} />
  <meshBasicMaterial color="#06b6d4" opacity={0.5} transparent side={DoubleSide} />
</mesh>
<TransformControls mode="translate" axis="Z" />
```

**Package:** `@react-three/drei` (TransformControls, Plane)

---

### **9. Lighting & Rendering Quality**

**Realistic Materials:**
- PBR (Physically Based Rendering) materials
- Hull: Painted steel (roughness 0.4, metalness 0.6)
- Waterplane: Realistic water shader (refraction, reflection)
- Deck: Wood texture
- Underwater: Different color (dark blue/green)

**Advanced Lighting:**
- HDRI environment maps (ship in harbor, open sea)
- Directional sun (adjustable time of day)
- Subsurface scattering for water
- Caustics (light refraction through water)

**Shadows:**
- Real-time shadow mapping
- Contact shadows (AO - ambient occlusion)
- Soft shadows (PCF - percentage-closer filtering)

**Packages:**
- `@react-three/postprocessing` (bloom, SSAO, tone mapping)
- `leva` (GUI controls for lighting adjustments)

---

### **10. Camera Presets & Views**

**Named Views:**
- Isometric (default)
- Port side elevation
- Starboard side elevation
- Bow view
- Stern view
- Plan (top-down)
- Profile (side)
- Sections (body plan)

**Smooth Transitions:**
- Animated camera movement between views
- "Fit to view" button (auto-zoom to hull bounds)
- Save custom views

**Implementation:**
```typescript
const viewPresets = {
  isometric: { position: [1, 0.6, 0.8] * cameraDistance, target: [0, 0, -draft/2] },
  port: { position: [0, cameraDistance, 0], target: [0, 0, -draft/2] },
  bow: { position: [cameraDistance, 0, 0], target: [0, 0, -draft/2] },
  // ...
};

// Animate to view
const goToView = (view: keyof typeof viewPresets) => {
  camera.position.lerp(viewPresets[view].position, 0.1);
  controls.target.lerp(viewPresets[view].target, 0.1);
};
```

---

## 📏 **Phase 2C: CAD Export & Interoperability** (Priority: HIGH)

### **11. DXF Export (2D Drawings)**

**Content:**
- Layer 0: Hull outline (polyline)
- Layer 1: Waterlines (splines)
- Layer 2: Stations (splines)
- Layer 3: Buttocks (splines)
- Layer 4: Dimensions (text + leader lines)
- Layer 5: Centerline, perpendiculars (construction lines)
- Layer 6: Title block

**Format:**
- DXF R2013 (compatible with AutoCAD 2013+)
- All coordinates in meters
- Proper line types (continuous, dashed, centerline)
- Text styles (Arial, 2.5mm height)

**Package:** `dxf-writer` (pure JS DXF generator)

**Implementation:**
```typescript
import DxfWriter from 'dxf-writer';

const exportDXF = (candidate: CandidateDesign) => {
  const dxf = new DxfWriter();
  
  // Create layers
  dxf.addLayer('Hull', DxfWriter.ACI.BLUE, 'Continuous');
  dxf.addLayer('Waterlines', DxfWriter.ACI.CYAN, 'Continuous');
  dxf.addLayer('Stations', DxfWriter.ACI.GREEN, 'Continuous');
  dxf.addLayer('Dimensions', DxfWriter.ACI.WHITE, 'Continuous');
  
  // Draw hull outline (plan view)
  dxf.setActiveLayer('Hull');
  const outline = calculateHullOutline(candidate);
  dxf.drawPolyline(outline.map(([x, y]) => ({ x, y })));
  
  // Draw waterlines
  dxf.setActiveLayer('Waterlines');
  for (let i = 0; i <= 10; i++) {
    const z = -(i / 10) * candidate.tM;
    const waterline = calculateWaterline(candidate, z);
    dxf.drawSpline(waterline.map(([x, y]) => ({ x, y })));
  }
  
  // Add dimensions
  dxf.setActiveLayer('Dimensions');
  dxf.drawText(0, 0, `Lpp: ${candidate.lppM.toFixed(2)} m`, 2.5);
  dxf.drawLinearDimension(/* ... */);
  
  // Download
  const blob = new Blob([dxf.toDxfString()], { type: 'application/dxf' });
  downloadBlob(blob, `${candidate.hullFamily}_plan.dxf`);
};
```

---

### **12. IGES Export (3D Surfaces)**

**Content:**
- NURBS surfaces (hull exterior)
- Trimmed surfaces (deck, bulkheads)
- Curve entities (waterlines, stations, buttocks)
- Metadata (vessel name, dimensions, date)

**Format:**
- IGES 5.3 (widely supported)
- Entity types:
  - Type 128: NURBS surface
  - Type 126: NURBS curve
  - Type 314: Color definition

**Package:** `verb-nurbs-web` (NURBS operations) + custom IGES writer

**Implementation:**
```typescript
// Convert Wigley mesh to NURBS surface
import verb from 'verb-nurbs-web';

const meshToNurbs = (geometry: THREE.BufferGeometry) => {
  const positions = geometry.attributes.position.array;
  const controlPoints = extractControlPoints(positions, 10, 10); // Reduce to 10x10 control mesh
  
  const surface = verb.geom.NurbsSurface.byKnotsControlPointsWeights(
    3, 3, // degree U, V
    knotsU, knotsV,
    controlPoints
  );
  
  return surface;
};

const exportIGES = (candidate: CandidateDesign) => {
  const surface = meshToNurbs(hullGeometry);
  const igesContent = generateIGES(surface, candidate);
  downloadBlob(new Blob([igesContent], { type: 'model/iges' }), `hull.igs`);
};
```

---

### **13. STEP Export (Solid Model)**

**Content:**
- Closed solid (hull + deck)
- Boundary representation (B-rep)
- Assembly structure (hull, superstructure, appendages)
- Product metadata (ISO 10303-21)

**Format:**
- STEP AP203 or AP214 (mechanical design)
- Compatible with: FreeCAD, Fusion 360, SolidWorks, CATIA

**Package:** `opencascade.js` (full CAD kernel in WASM, ~15MB)
- **Pro:** Complete STEP/IGES read/write, Boolean operations, filleting
- **Con:** Large bundle size, complex API
- **Alternative:** Server-side conversion (backend endpoint with OpenCASCADE C++)

---

## 🖥️ **Phase 2D: AutoCAD-Like UI** (Priority: HIGH)

### **14. Viewport Quad Layout**

**Classic CAD Layout:**
```
┌─────────────┬─────────────┐
│  Top (Plan) │ Front       │
│             │ (Profile)   │
├─────────────┼─────────────┤
│  Right      │ Isometric   │
│  (Sections) │ (3D)        │
└─────────────┴─────────────┘
```

**Implementation:**
```typescript
<div className="grid grid-cols-2 grid-rows-2 h-screen">
  <div className="border"><Hull2DPlan candidate={candidate} /></div>
  <div className="border"><Hull2DProfile candidate={candidate} /></div>
  <div className="border"><Hull2DSections candidate={candidate} /></div>
  <div className="border"><Hull3DScene candidate={candidate} /></div>
</div>
```

**Features:**
- Synced highlighting (hover station in plan → highlights in all views)
- Synced camera (zoom plan → zoom all 2D views)
- Maximize any viewport (double-click)
- Swap viewport layouts (1-up, 2-up, 3-up, 4-up)

---

### **15. Layers Panel (AutoCAD-Style)**

**Layers:**
- ☑ Hull Surface
- ☑ Waterlines
- ☑ Stations
- ☑ Buttocks
- ☑ Centers (LCB, LCG, KB, KG)
- ☑ Dimensions
- ☑ Grid
- ☑ Constraints (max beam, draft, LOA boxes)
- ☑ Wavelength grid (from Tz)

**Controls:**
- Eye icon: Show/hide
- Lock icon: Freeze (prevent selection)
- Color picker: Change layer color
- Transparency slider (0-100%)

**Package:** `react-sortable-hoc` (drag-reorder layers)

---

### **16. Properties Panel**

**When User Selects Element:**
- Hull surface → Show Cb, Cp, Cwp, area, volume
- Waterline → Show Cwp at that waterline, area
- Station → Show section area, Cm
- Center marker → Show coordinates (X, Y, Z)

**Editable Properties:**
- Change color
- Change opacity
- Change name/label
- Lock/unlock

---

### **17. Command Line (Power User Feature)**

**AutoCAD-Like Command Palette:**
```
Command: ZOOM EXTENTS
Command: LAYER FREEZE WATERLINES
Command: DISTANCE
Command: MEASURE AREA
Command: EXPORT DXF
```

**Implementation:**
```typescript
// Keyboard shortcut: / or : to open command palette
const commands = {
  'zoom extents': () => fitCameraToHull(),
  'layer freeze': (layerName) => toggleLayer(layerName, false),
  'measure distance': () => setMeasureMode('distance'),
  'export dxf': () => exportDXF(candidate),
  // ...
};

<CommandPalette commands={commands} />
```

**Package:** `kbar` or `cmdk` (command palette UI)

---

## 🔬 **Phase 2E: Analysis Tools** (Priority: MEDIUM)

### **18. Hydrostatic Curves (Embedded)**

**Mini hydrostatics preview:**
- GZ curve (stability)
- Displacement vs draft
- Trim sensitivity
- "View Full Analysis" → Navigate to Hydrostatics module

**Package:** Reuse `Recharts` (already in project)

---

### **19. Resistance Curve (Holtrop)**

**Speed vs Power Plot:**
- EHP(V), SHP(V), BHP(V) curves
- Design point marker (service speed)
- Optimum speed (minimum power/mile)
- "View Full Analysis" → Navigate to Resistance module

---

### **20. Structural Weight Estimation**

**Steel Weight Breakdown:**
- Hull plating (area × thickness × ρ_steel)
- Longitudinal framing
- Transverse framing
- Bulkheads
- Deck
- **Total:** Lightweight estimate

**Use in DWT calculation:**
- DWT = Δ - Lightweight
- Verify payload capacity

---

## 🎮 **Phase 2F: Interactive Editing** (Priority: MEDIUM)

### **21. Direct Manipulation (Drag to Edit)**

**Drag Handles:**
- Drag bow → Adjust Lpp
- Drag beam line → Adjust B
- Drag waterline → Adjust T
- Drag Cb slider → Adjust form fullness

**Inverse Solver:**
```typescript
// User drags bow forward +5m
// Solver back-calculates: Lpp += 5, re-solve Δ closure
// Update candidate in real-time (<300ms)
```

**Implementation:**
- `@react-three/drei` → `TransformControls`
- Attach to control points (bow, stern, max beam, waterline)
- On drag end → `PUT /candidates/{id}` with new params
- Re-solve displacement closure
- Update 3D mesh

---

### **22. Parametric Sliders (Bottom Toolbar)**

**Sliders:**
- Service Speed: 10-30 kn (updates Fn, Lwl, resistance)
- Block Coefficient: 0.55-0.85 (updates hull fullness)
- L/B Ratio: 6.0-10.0 (updates proportions)
- B/T Ratio: 2.0-4.0 (updates stability)
- Freeboard: 0.05L-0.10L (updates depth)

**Lock Toggles:**
- 🔒 Keep Fn constant (lock Froude number)
- 🔒 Keep L/B ratio
- 🔒 Keep B/T ratio
- 🔒 Keep Cb band

**Re-Solve on Change:**
- Debounce slider (300ms)
- Call `PUT /candidates/{id}` with new params
- Solver runs displacement closure (~200ms)
- Update 3D mesh smoothly (transition animation)

**Package:** `rc-slider` (professional slider component)

---

## 🌊 **Phase 2G: Advanced Overlays** (Priority: MEDIUM)

### **23. Wavelength Grid (from Sea State)**

**Based on Tz (wave period):**
```typescript
// Wavelength: λ = g·T²/(2π) ≈ 1.56·T²
const wavelength = 1.56 * (envTzS ** 2); // meters

// Draw sinusoidal wave grid on water surface
const waveGrid = [];
for (let x = -lpp/2; x <= lpp/2; x += wavelength) {
  const waveProfile = (y, t) => waveHeight * Math.sin(2*Math.PI*(x + y)/wavelength);
  waveGrid.push(waveProfile);
}
```

**Visual:**
- Animated waves (moving forward at wave celerity)
- Hull penetration visualization (where hull meets waves)
- L/λ ratio annotation (important for resistance)

---

### **24. Constraint Envelopes (Bounding Boxes)**

**Visual Indicators:**
- Red box: Max beam constraint (30m → 30m wide box)
- Yellow box: Max draft constraint (10m → box depth)
- Orange box: Max LOA constraint
- Green box: Max air draft (bridge clearance)

**When Violated:**
- Box turns solid red
- Flashing animation
- Warning icon in legend

---

### **25. Canal Constraint Templates**

**Presets:**
- Panamax: LOA < 294.1m, B < 32.3m, T < 12m
- Suezmax: LOA < 400m, B < 77.5m (laden), T < 20.1m
- Malaccamax: T < 21m, LOA < 470m
- ULCS: No constraint (ultra-large container ships)

**Visual:**
- Load canal template → Shows bounding box in 3D
- Hull changes color if exceeds (red = too large)
- "Canal Compliance" badge in legend

---

## 🧮 **Phase 2H: Numeric Precision** (Priority: MEDIUM)

### **26. Offsets Table (Traditional Naval Architecture)**

**Table Columns:**
- Station: 0 (AP), 1, 2, ..., 10 (FP)
- Waterlines: 0 (BL), WL1, WL2, ..., Design WL, Deck
- Half-breadths at each station/waterline intersection
- Heights at each station/waterline

**Export Formats:**
- CSV (Excel-compatible)
- Rhino-compatible offsets
- Maxsurf GF format
- FAIRWAY format (for hull fairing)

**UI:**
- Spreadsheet-like grid (editable)
- Click cell → Highlights in 3D view
- Edit cell → Re-solve hull surface (NURBS fit)

**Package:** `react-data-grid` (Excel-like grid with editing)

---

### **27. Section Area Curve (SAC)**

**Plot:**
- X-axis: Station position (0 = AP, 10 = FP)
- Y-axis: Sectional area (m²)
- Curve shape indicates hull fullness distribution

**Use:**
- Validate Cp (area under SAC / (L × max area))
- Check fullness progression (no abrupt changes)
- Compare candidates

---

### **28. Bonjean Curves**

**Buoyancy Curves:**
- For each station: Plot draft vs sectional area
- Used for: Trim, stability, loading calculations
- Export to CSV for external tools

**Package:** Reuse `Recharts` (already in project)

---

## 🔧 **Phase 2I: Advanced Geometry** (Priority: LOW)

### **29. NURBS Surface Editing**

**Instead of parametric Wigley:**
- Load NURBS control mesh (10×10 control points)
- User drags control points → Hull reshapes
- Fairness constraints (minimum curvature radius)
- Real-time volume recalculation

**Package:** `verb-nurbs-web` (NURBS operations)

**Implementation:**
```typescript
// Load NURBS surface from candidate
const surface = candidateToNurbsSurface(candidate);

// Convert to Three.js mesh
const mesh = nurbsSurfaceToBufferGeometry(surface, 60, 40);

// Edit mode: Show control points
{editMode && (
  <ControlPointsGizmo 
    surface={surface} 
    onDrag={(i, j, newPos) => updateControlPoint(i, j, newPos)} 
  />
)}
```

---

### **30. Boolean Operations**

**Hull Modifications:**
- Cut opening for rudder, propeller, bow thruster
- Add bulbous bow (blend NURBS surface)
- Add skeg, bilge keel (extrude along path)

**Package:** `three-bvh-csg` (CSG operations for Three.js)

**Use Case:**
- User loads Wigley hull
- Adds bulbous bow from library
- CSG union operation
- Export to STEP with appurtenances

---

## 🎯 **Recommended Package Additions**

### **Core 3D/CAD (High Priority)**
```bash
npm install verb-nurbs-web          # NURBS geometry (3D curves/surfaces)
npm install dxf-writer              # DXF export (2D CAD drawings)
npm install @react-three/postprocessing  # Advanced rendering (SSAO, bloom)
npm install @react-three/drei       # Already installed - helpers
npm install three-mesh-bvh          # Fast ray casting, measurements
npm install leva                    # GUI controls (lighting, parameters)
```

### **Data Visualization (Medium Priority)**
```bash
npm install d3-shape d3-scale       # Advanced 2D plotting (SAC, Bonjean)
npm install react-data-grid         # Offsets table (Excel-like)
npm install kbar                    # Command palette (AutoCAD-like)
```

### **Optional (Future)**
```bash
npm install opencascade.js          # Full CAD kernel (STEP/IGES, ~15MB bundle!)
npm install three-bvh-csg           # Boolean operations (CSG)
npm install react-spring            # Animation (smooth camera transitions)
```

---

## 📋 **Detailed Implementation Phases**

### **Week 3: 2D Professional Views** (20-25 hours)
**Day 1-2:**
- [ ] Implement `Hull2DPlan.tsx` (plan view with waterlines, stations)
- [ ] Implement `Hull2DProfile.tsx` (profile with buttocks, sheerline)
- [ ] Implement `Hull2DSections.tsx` (body plan with stations)
- [ ] Add SVG export for each view
- [ ] Add dimensions annotations (D3.js for arrows, text)

**Day 3:**
- [ ] Quad viewport layout (4-up view)
- [ ] Viewport synchronization (hover, selection)
- [ ] Viewport maximize/minimize

**Day 4:**
- [ ] Camera presets (isometric, port, starboard, bow, stern)
- [ ] Smooth camera transitions
- [ ] "Fit to view" button

**Day 5 (Polish):**
- [ ] Layers panel (show/hide elements)
- [ ] Properties panel (when element selected)
- [ ] Print layout (for reports)

---

### **Week 4: CAD Export & Measurements** (15-20 hours)
**Day 1:**
- [ ] DXF export (2D plan, profile, sections)
- [ ] Test with AutoCAD import
- [ ] Fix layer/color/linetype issues

**Day 2:**
- [ ] NURBS conversion (mesh → NURBS surface)
- [ ] IGES export (3D surface)
- [ ] Test with Rhino import

**Day 3:**
- [ ] Measurement tools (distance, area, volume)
- [ ] Snap to grid/vertices
- [ ] Display in HUD overlay

**Day 4:**
- [ ] Offsets table generation
- [ ] CSV export of offsets
- [ ] Editable offsets (back-solve to NURBS)

**Day 5 (Polish):**
- [ ] Command palette (keyboard shortcuts)
- [ ] Help tooltips (for all tools)
- [ ] User guide (embedded in UI)

---

### **Week 5: Advanced Features** (20-25 hours)
**Day 1-2:**
- [ ] Comparison mode (3-up ghost overlay)
- [ ] Difference heatmap
- [ ] Side-by-side comparison table

**Day 3:**
- [ ] Curvature analysis (fairness check)
- [ ] Gaussian curvature heatmap
- [ ] Longitudinal curvature plots

**Day 4:**
- [ ] Interactive sliders (bottom toolbar)
- [ ] Real-time re-solve (<300ms)
- [ ] Lock toggles

**Day 5:**
- [ ] Wavelength overlay (animated)
- [ ] Constraint envelopes (bounding boxes)
- [ ] Canal compliance indicators

---

### **Week 6: Pro Features** (20-25 hours)
**Day 1-2:**
- [ ] NURBS editing (drag control points)
- [ ] Fairness constraints
- [ ] Volume validation

**Day 3:**
- [ ] Advanced lighting (PBR, HDRI, shadows)
- [ ] Realistic water shader
- [ ] Post-processing (SSAO, bloom)

**Day 4:**
- [ ] Hydrostatic curves (embedded preview)
- [ ] Resistance curve (embedded)
- [ ] Section area curve

**Day 5 (Polish):**
- [ ] Performance profiling (GPU, FPS)
- [ ] Dynamic LOD (level of detail)
- [ ] Web Worker for heavy computation

---

## 🎨 **Design Mockups Needed**

### **Full-Featured Workspace (Target):**
```
┌─────────────────────────────────────────────────────────────────┐
│  NavArch Studio - Hull Sizing Workspace                         │
│  ◀ Back to Results    [Export ▼] [Push to Hydro] [Analyze]     │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────┬─────────────┐  ┌─────────────────────────┐   │
│  │ Plan View   │ Profile     │  │ Layers         Properties│   │
│  │ (Waterlines)│ (Buttocks)  │  │ ☑ Hull                   │   │
│  │             │             │  │ ☑ Waterlines             │   │
│  │             │             │  │ ☑ Stations               │   │
│  ├─────────────┼─────────────┤  │ ☑ Centers                │   │
│  │ Sections    │ 3D Isometric│  │ ☐ Grid                   │   │
│  │ (Body Plan) │ (Interactive│  │ ☐ Constraints            │   │
│  │             │  +lighting) │  │                           │   │
│  └─────────────┴─────────────┘  │ Selected: Hull Surface   │   │
│                                  │ Area: 1,245 m²           │   │
│  ┌──────────────────────────────│ Volume: 8,245 m³         │   │
│  │ ◀──── V: 15 kn ────▶         │ Cb: 0.701                │   │
│  │  🔒 Fn   🔓 L/B   🔓 B/T    └─────────────────────────┘   │
│  └──────────────────────────────┐                             │
│                                                                  │
│  [Measure] [Slice] [Compare] [Export ▼] [Command: _]          │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🚀 **Immediate Action Plan**

### **This Week (Critical for MVP Completeness):**

1. **Fix Missing Cargo Density Input** (~15 mins)
   - Show `cargoDensityTPerM3` for ALL cargo bases (not just volume)
   - Default values: TEU: 0.5 t/m³, Weight: User-provided, Volume: User-provided
   - Help text: "Used for holds volume estimation"

2. **Create 2D Plan View** (~4-6 hours)
   - SVG-based
   - Waterlines projection (5-10 waterlines)
   - Stations markers
   - Dimensions annotations
   - Export to SVG

3. **Create 2D Profile View** (~3-4 hours)
   - Sheerline, keel line
   - Buttocks (3-5 vertical planes)
   - Freeboard marker
   - Dimensions

4. **Create Sections/Body Plan** (~4-6 hours)
   - 10-20 transverse sections
   - Forward half on right, aft on left
   - Centerline, waterline, baseline
   - Section area annotations

5. **Quad Viewport Layout** (~2-3 hours)
   - 2×2 grid layout
   - Responsive (collapses to 1-up on mobile)
   - Maximize any viewport

### **Next Week (Professional Features):**

6. **DXF Export** (~6-8 hours)
7. **Layers Panel** (~4 hours)
8. **Measurement Tools** (~6 hours)
9. **Comparison Mode** (~4 hours)
10. **Parametric Sliders** (~6 hours)

---

## 💡 **Key Decision Points**

### **Question 1: Bundle Size vs Features**
- **Option A:** All-in browser (verb-nurbs, opencascade.js) → +15-20 MB bundle
- **Option B:** Hybrid (basic in browser, heavy ops on backend) → +2-3 MB bundle
- **Recommendation:** Option B - Keep frontend fast, use backend for STEP/IGES conversion

### **Question 2: 2D Rendering**
- **Option A:** SVG (scalable, exportable, simple) → Best for plan/profile/sections
- **Option B:** HTML Canvas (faster for animations) → Best for real-time measurements
- **Recommendation:** SVG for static views, Canvas for interactive tools

### **Question 3: NURBS vs Parametric**
- **Option A:** Keep Wigley/Series60 parametric (fast, simple, good enough)
- **Option B:** Full NURBS editing (pro-level, complex, slow)
- **Recommendation:** Option A for MVP, Option B for Phase 3+ (advanced users)

### **Question 4: Web Worker for Solver**
- **Priority:** High (prevents UI blocking during re-solve)
- **Effort:** ~4 hours
- **Benefit:** Smooth slider interactions, responsive UI
- **Recommendation:** Implement this week

---

## 📊 **Estimated Effort Summary**

**Minimum Viable (AutoCAD-Comparable):**
- 2D Views: ~15-20 hours
- DXF Export: ~6-8 hours
- Measurements: ~6 hours
- Layers Panel: ~4 hours
- **Total:** ~30-40 hours (1 week full-time)

**Full Professional Suite:**
- Above + Sliders + Comparison + Curvature: ~60-80 hours (2 weeks)

**World-Class (Exceed AutoCAD):**
- Above + NURBS editing + STEP export + Web Worker: ~100-120 hours (3 weeks)

---

## 🎯 **My Recommendations (Priority Order)**

**Phase 2A (This Week):**
1. Fix cargo density visibility ✅
2. Create 2D plan view (SVG waterlines)
3. Create 2D profile view (SVG buttocks)
4. Create quad viewport layout
5. Add view presets (camera positions)

**Phase 2B (Next Week):**
6. DXF export (2D drawings)
7. Layers panel (show/hide)
8. Measurement tools (distance, area)
9. Comparison mode (ghost overlay)
10. Parametric sliders (bottom toolbar)

**Phase 2C (Week After):**
11. Curvature analysis
12. Offsets table
13. IGES export (3D surface)
14. Web Worker for solver
15. Advanced lighting

---

**Shall I start with:**
1. **Fix cargo density field** (15 mins)
2. **Create comprehensive 2D plan view** (4-6 hours)
3. **Deep dive into DXF export strategy** (research + plan)

**Or would you like to review this plan first and adjust priorities?**

---

**Generated:** November 3, 2025, 3:45 PM UTC  
**Status:** Awaiting user direction for next sprint
















