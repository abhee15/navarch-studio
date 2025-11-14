# Frontend Implementation Phases (Week-by-Week)

## Overview
React + TypeScript + MobX + react-three-fiber for advanced 3D visualization.

**Route:** `/hull-sizing/*`
**Port:** 3000 (development)

---

## Phase 5: Frontend Foundation (Week 4, Days 1-2)

### Goal
Create routing, MobX store, API client, and basic mission input form.

### Tasks

**5.1 Routing Structure** (`frontend/src/App.tsx`)

Add routes:
```typescript
// Hull Sizing routes
<Route path="/hull-sizing" element={<ProtectedRoute><HullSizingLanding /></ProtectedRoute>} />
<Route path="/hull-sizing/cases" element={<ProtectedRoute><MissionCasesList /></ProtectedRoute>} />
<Route path="/hull-sizing/cases/:id" element={<ProtectedRoute><MissionCaseDetail /></ProtectedRoute>} />
<Route path="/hull-sizing/workspace/:runId" element={<ProtectedRoute><SizingWorkspace /></ProtectedRoute>} />
<Route path="/hull-sizing/compare" element={<ProtectedRoute><CandidateComparison /></ProtectedRoute>} />
```

**5.2 MobX Store** (`frontend/src/stores/HullSizingStore.ts`)
```typescript
import { makeAutoObservable, runInAction } from "mobx";
import { hullSizingApi } from "../api/hull-sizing-api";
import type { MissionCase, SizingRun, CandidateDesign } from "../types/hull-sizing";

export class HullSizingStore {
  missionCases: MissionCase[] = [];
  currentMissionCase: MissionCase | null = null;
  currentRun: SizingRun | null = null;
  candidates: CandidateDesign[] = [];
  selectedCandidate: CandidateDesign | null = null;
  loading: boolean = false;
  error: string | null = null;

  constructor() {
    makeAutoObservable(this);
  }

  async loadMissionCases() {
    this.loading = true;
    try {
      const result = await hullSizingApi.listMissionCases();
      runInAction(() => {
        this.missionCases = result.data;
        this.loading = false;
      });
    } catch (err) {
      runInAction(() => {
        this.error = getErrorMessage(err);
        this.loading = false;
      });
    }
  }

  async createMissionCase(dto: CreateMissionCaseDto) {
    this.loading = true;
    try {
      const created = await hullSizingApi.createMissionCase(dto);
      runInAction(() => {
        this.currentMissionCase = created;
        this.loading = false;
      });
      return created;
    } catch (err) {
      runInAction(() => {
        this.error = getErrorMessage(err);
        this.loading = false;
      });
      throw err;
    }
  }

  async runSizing(missionCaseId: string, locks: LocksDto, options: SizingOptionsDto) {
    this.loading = true;
    try {
      const result = await hullSizingApi.createRun(missionCaseId, { mode: 'first_principles', locks, options });
      runInAction(() => {
        this.currentRun = result.run;
        this.candidates = result.candidates;
        this.loading = false;
      });
      return result;
    } catch (err) {
      runInAction(() => {
        this.error = getErrorMessage(err);
        this.loading = false;
      });
      throw err;
    }
  }

  selectCandidate(candidateId: string) {
    this.selectedCandidate = this.candidates.find(c => c.id === candidateId) || null;
  }

  async pushToHydrostatics(candidateId: string, vesselName: string) {
    this.loading = true;
    try {
      const result = await hullSizingApi.pushToHydrostatics(candidateId, vesselName);
      runInAction(() => {
        this.loading = false;
      });
      return result;
    } catch (err) {
      runInAction(() => {
        this.error = getErrorMessage(err);
        this.loading = false;
      });
      throw err;
    }
  }
}

export const hullSizingStore = new HullSizingStore();
```

**5.3 API Client** (`frontend/src/api/hull-sizing-api.ts`)
```typescript
import { api } from "./api";
import type {
  MissionCase,
  CreateMissionCaseDto,
  SizingRun,
  CreateSizingRunDto,
  SizingResultDto,
  CandidateDesign,
  PushToHydrostaticsResultDto,
  HullFamilyPreset,
} from "../types/hull-sizing";

const BASE_PATH = "/hull-sizing";

export const hullSizingApi = {
  // Mission Cases
  async listMissionCases(page = 1, pageSize = 20) {
    const response = await api.get(`${BASE_PATH}/mission-cases?page=${page}&pageSize=${pageSize}`);
    return response.data;
  },

  async createMissionCase(dto: CreateMissionCaseDto) {
    const response = await api.post(`${BASE_PATH}/mission-cases`, dto);
    return response.data;
  },

  async getMissionCase(id: string) {
    const response = await api.get(`${BASE_PATH}/mission-cases/${id}`);
    return response.data;
  },

  async updateMissionCase(id: string, dto: CreateMissionCaseDto) {
    const response = await api.put(`${BASE_PATH}/mission-cases/${id}`, dto);
    return response.data;
  },

  async deleteMissionCase(id: string) {
    await api.delete(`${BASE_PATH}/mission-cases/${id}`);
  },

  // Sizing Runs
  async createRun(missionCaseId: string, dto: CreateSizingRunDto): Promise<SizingResultDto> {
    const response = await api.post(`${BASE_PATH}/mission-cases/${missionCaseId}/runs`, dto);
    return response.data;
  },

  async getRun(runId: string) {
    const response = await api.get(`${BASE_PATH}/runs/${runId}`);
    return response.data;
  },

  async getCandidates(runId: string) {
    const response = await api.get(`${BASE_PATH}/runs/${runId}/candidates`);
    return response.data;
  },

  // Candidates
  async getCandidate(id: string) {
    const response = await api.get(`${BASE_PATH}/candidates/${id}`);
    return response.data;
  },

  async recomputeCandidate(id: string, adjustments: any) {
    const response = await api.post(`${BASE_PATH}/candidates/${id}/recompute`, { adjustments });
    return response.data;
  },

  async pushToHydrostatics(candidateId: string, vesselName: string): Promise<PushToHydrostaticsResultDto> {
    const idempotencyKey = `push-hydro-${candidateId}-${Date.now()}`;
    const response = await api.post(
      `${BASE_PATH}/candidates/${candidateId}/push-to-hydrostatics`,
      { vesselName },
      { headers: { 'X-Idempotency-Key': idempotencyKey } }
    );
    return response.data;
  },

  // Reference Data
  async getHullFamilies(): Promise<HullFamilyPreset[]> {
    const response = await api.get(`${BASE_PATH}/reference/hull-families`);
    return response.data;
  },

  async getIsoContainers() {
    const response = await api.get(`${BASE_PATH}/reference/iso-containers`);
    return response.data;
  },
};
```

**5.4 TypeScript Types** (`frontend/src/types/hull-sizing.ts`)
- [ ] Define all interfaces matching backend DTOs
- [ ] Export types for use across frontend

**✓ Completion Checklist:**
- Routing configured
- MobX store created with observables
- API client typed with TypeScript
- Store integrated with RootStore
- Error handling in place

---

## Phase 6: Mission Input Form (Week 4, Day 3)

### Goal
Create wizard-style mission input form with validation.

### Component Structure

```
frontend/src/components/hull-sizing/mission/
├── MissionInputForm.tsx (main container)
├── CargoInput.tsx
├── SpeedEnvironmentInput.tsx
├── ConstraintsInput.tsx
└── OptionsInput.tsx
```

### MissionInputForm.tsx

**Sections:**

1. **Mission Type Selection**
   - Dropdown with categories: Commercial, Government, Pleasure
   - Sub-type cards: Container, Tanker, Bulk, Fishing, Yacht, HSC, etc.

2. **Cargo Basis (Radio Buttons)**
   - Volume (m³) → shows volume input + density input
   - Weight (tonnes) → shows weight input only
   - TEU → shows TEU count + stowage options

3. **Speed & Environment**
   - Service speed (kn) with slider (4-30 kn)
   - Sea margin (%) - default 15%, slider 0-30%
   - Service margin (%) - default 15%, slider 0-30%
   - Sea state: Beaufort dropdown OR custom Hs/Tz inputs
   - Wave period Tz (for wavelength overlay)

4. **Constraints (Optional)**
   - Max LOA (m)
   - Max Beam (m)
   - Max Draft (m)
   - Max Air Draft (m)
   - Canal presets dropdown (None, Panamax, Suezmax, etc.)

5. **Options**
   - Hull family hint (auto-suggested, can override)
   - Locks: Checkboxes for Keep Fn, Keep L/B, Keep B/T, Keep D/T

6. **Actions**
   - "Compute" button → calls API, navigates to results

**Validation:**
```typescript
const validateMissionCase = (values: CreateMissionCaseDto): Record<string, string> => {
  const errors: Record<string, string> = {};
  
  if (!values.name) errors.name = "Mission name required";
  if (!values.missionType) errors.missionType = "Mission type required";
  if (!values.cargoBasis) errors.cargoBasis = "Cargo basis required";
  
  if (values.cargoBasis === 'volume' && !values.cargoVolumeM3) {
    errors.cargoVolumeM3 = "Cargo volume required";
  }
  if (values.cargoBasis === 'weight' && !values.cargoValue) {
    errors.cargoValue = "Cargo weight required";
  }
  if (values.cargoBasis === 'teu' && !values.teuCount) {
    errors.teuCount = "TEU count required";
  }
  
  if (!values.serviceSpeedKn || values.serviceSpeedKn <= 0) {
    errors.serviceSpeedKn = "Speed must be positive";
  }
  
  return errors;
};
```

**✓ Completion Checklist:**
- Form validates all required fields
- Cargo basis toggle shows/hides relevant inputs
- Canal presets auto-fill constraint values
- Beaufort scale helper for Hs/Tz
- Unit conversion displays (kn ↔ m/s)
- "Compute" button disabled while loading

---

## Phase 7: Candidates Grid (Week 4, Day 4)

### Goal
Display sizing results as ranked candidate cards with 3D thumbnails.

### Component: CandidatesGrid.tsx

**Layout:**
- Responsive grid (3 columns desktop, 2 tablet, 1 mobile)
- Sorted by rank
- Filter/sort controls (by score, EHP, displacement)

**CandidateCard.tsx:**
```typescript
interface CandidateCardProps {
  candidate: CandidateDesign;
  onSelect: (id: string) => void;
  isSelected: boolean;
}

export const CandidateCard: React.FC<CandidateCardProps> = ({ candidate, onSelect, isSelected }) => {
  return (
    <div className={`card ${isSelected ? 'border-blue-500' : ''}`}>
      {/* 3D Thumbnail (static render) */}
      <div className="h-40 bg-gray-100">
        <HullThumbnail geometry={candidate.geometryJson} />
      </div>
      
      {/* Hull family badge */}
      <div className="badge">{candidate.hullFamily}</div>
      
      {/* Key metrics */}
      <div className="metrics">
        <div>Lpp: {candidate.lppM.toFixed(1)} m</div>
        <div>B: {candidate.bM.toFixed(1)} m</div>
        <div>T: {candidate.tM.toFixed(1)} m</div>
        <div>Fn: {candidate.fn.toFixed(3)}</div>
        <div>Δ: {candidate.displacementT.toFixed(0)} t</div>
        <div>EHP: {candidate.ehpKw.toFixed(0)} kW</div>
      </div>
      
      {/* Score gauge */}
      <div className="score-gauge">
        <CircularProgress value={candidate.score * 100} />
        <span>Score: {(candidate.score * 100).toFixed(1)}%</span>
      </div>
      
      {/* Flags (warnings/errors) */}
      {candidate.flagsJson && Object.keys(candidate.flagsJson).length > 0 && (
        <div className="flags">
          {Object.entries(candidate.flagsJson).map(([key, value]) =>
            value ? <span key={key} className="flag-chip">{key}</span> : null
          )}
        </div>
      )}
      
      {/* Actions */}
      <button onClick={() => onSelect(candidate.id)}>Open Workspace</button>
    </div>
  );
};
```

**HullThumbnail.tsx (Static 3D render):**
```typescript
// Simplified 3D mesh for card thumbnail
export const HullThumbnail: React.FC<{ geometry: any }> = ({ geometry }) => {
  return (
    <Canvas camera={{ position: [2, 1, 2], fov: 50 }}>
      <OrbitControls enabled={false} />
      <ambientLight intensity={0.5} />
      <directionalLight position={[5, 5, 5]} />
      <HullWireframe geometry={geometry} simplified={true} />
    </Canvas>
  );
};
```

**✓ Completion Checklist:**
- Candidates grid displays all results
- Cards show 3D thumbnails (low-poly for performance)
- Sorting works (by rank, score, EHP)
- Filtering works (by family, flags)
- Click card → navigate to workspace
- Responsive layout (mobile-friendly)

---

## Phase 8: 3D Visualization (Week 4-5, Days 5-10)

### Goal
Build advanced 3D hull viewer with react-three-fiber.

### Install Dependencies
```bash
npm install @react-three/fiber @react-three/drei three gl-matrix
npm install --save-dev @types/three
```

### Component Structure

```
frontend/src/components/hull-sizing/3d/
├── HullViewer3D.tsx (main Canvas wrapper)
├── HullMesh.tsx (parametric hull geometry)
├── WaterplaneOverlay.tsx
├── MarkerPoints.tsx (CB, LCB, LCG, KB)
├── ConstraintBoxes.tsx (max beam/draft/LOA wireframes)
├── WavelengthGrid.tsx (λ overlay on water surface)
├── SlicePlanes.tsx (draggable stations/waterlines/buttocks)
├── CurvatureHeatmap.tsx (fairness checking)
└── CompareOverlay.tsx (ghost reference hull)
```

### HullViewer3D.tsx

```typescript
import { Canvas } from "@react-three/fiber";
import { OrbitControls, Stats, Grid } from "@react-three/drei";
import { HullMesh } from "./HullMesh";
import { WaterplaneOverlay } from "./WaterplaneOverlay";
import { MarkerPoints } from "./MarkerPoints";

export const HullViewer3D: React.FC<{ candidate: CandidateDesign }> = ({ candidate }) => {
  const [showWaterplane, setShowWaterplane] = useState(true);
  const [showMarkers, setShowMarkers] = useState(true);
  const [showSlices, setShowSlices] = useState(false);
  
  return (
    <div className="w-full h-full relative">
      {/* Toolbar */}
      <div className="absolute top-4 left-4 z-10 space-x-2">
        <button onClick={() => setShowWaterplane(!showWaterplane)}>Waterplane</button>
        <button onClick={() => setShowMarkers(!showMarkers)}>Markers</button>
        <button onClick={() => setShowSlices(!showSlices)}>Slices</button>
      </div>
      
      {/* 3D Canvas */}
      <Canvas camera={{ position: [2, 1, 2], fov: 50 }}>
        <color attach="background" args={['#f0f0f0']} />
        
        <ambientLight intensity={0.5} />
        <directionalLight position={[10, 10, 5]} intensity={0.8} />
        
        <OrbitControls makeDefault />
        <Grid infiniteGrid cellSize={10} sectionSize={50} />
        <Stats />
        
        {/* Main hull */}
        <HullMesh geometry={candidate.geometryJson} lpp={candidate.lppM} />
        
        {/* Overlays */}
        {showWaterplane && <WaterplaneOverlay lpp={candidate.lppM} b={candidate.bM} t={candidate.tM} />}
        {showMarkers && <MarkerPoints candidate={candidate} />}
        {showSlices && <SlicePlanes candidate={candidate} />}
      </Canvas>
    </div>
  );
};
```

### HullMesh.tsx (Parametric Hull Geometry)

```typescript
import { useMemo } from "react";
import { BufferGeometry, Vector3, Float32BufferAttribute } from "three";

export const HullMesh: React.FC<{ geometry: any; lpp: number }> = ({ geometry, lpp }) => {
  const meshGeometry = useMemo(() => {
    // Parse geometry JSON: {stations: [{x, waterlines: [{z, y}]}]}
    const bufferGeom = new BufferGeometry();
    
    const vertices: number[] = [];
    const indices: number[] = [];
    
    // Build mesh from offsets grid
    geometry.stations.forEach((station: any, stationIdx: number) => {
      station.waterlines.forEach((wl: any, wlIdx: number) => {
        // Port side
        vertices.push(station.x, wl.y, wl.z);
        // Starboard side (mirror)
        vertices.push(station.x, -wl.y, wl.z);
      });
    });
    
    // Build triangles (simplified - full implementation uses proper indexing)
    // ... triangle generation logic
    
    bufferGeom.setAttribute('position', new Float32BufferAttribute(vertices, 3));
    bufferGeom.setIndex(indices);
    bufferGeom.computeVertexNormals();
    
    return bufferGeom;
  }, [geometry]);
  
  return (
    <mesh geometry={meshGeometry}>
      <meshStandardMaterial color="#3b82f6" wireframe={false} />
    </mesh>
  );
};
```

### Performance Optimization

**LOD System:**
```typescript
import { useLOD } from "../../../hooks/useLOD";

export const HullMesh: React.FC<{ geometry: any; lpp: number }> = ({ geometry, lpp }) => {
  const camera = useThree((state) => state.camera);
  const lodLevel = useLOD(camera, lpp); // Returns 'near' | 'mid' | 'far'
  
  const meshGeometry = useMemo(() => {
    const triCounts = { near: 80000, mid: 40000, far: 20000 };
    return generateHullGeometry(geometry, triCounts[lodLevel]);
  }, [geometry, lodLevel]);
  
  // ... rest
};
```

**Web Worker for Mesh Generation:**
```typescript
// public/workers/hull-geometry.worker.ts
import { expose } from "comlink";

const api = {
  generateMesh(geometry: any, triCount: number) {
    // Heavy mesh generation logic
    // Returns: { vertices: Float32Array, indices: Uint32Array }
  }
};

expose(api);
```

**✓ Completion Checklist:**
- 3D hull renders correctly
- Waterplane overlay visible
- Markers (CB, LCB, LCG, KB) positioned correctly
- LOD system reduces tris based on camera distance
- Performance: ≥45 FPS on mid-range laptop
- Camera controls smooth (orbit, zoom, pan)

---

## Phase 9: 2D Views (Week 5, Days 1-2)

### Goal
Create Plan, Profile, and Body Plan views using SVG.

### Component Structure

```
frontend/src/components/hull-sizing/2d/
├── PlanView.tsx (top-down waterplane)
├── ProfileView.tsx (side elevation)
├── BodyPlanView.tsx (sections + SAC)
└── OffsetsGrid.tsx (editable AG Grid)
```

### PlanView.tsx (Top-Down)

```typescript
export const PlanView: React.FC<{ candidate: CandidateDesign }> = ({ candidate }) => {
  const viewBoxWidth = 800;
  const viewBoxHeight = 400;
  const margin = { top: 40, right: 40, bottom: 40, left: 40 };
  
  // Parse geometry
  const { stations } = candidate.geometryJson;
  
  // Scale factors
  const scaleX = (viewBoxWidth - margin.left - margin.right) / candidate.lppM;
  const scaleY = (viewBoxHeight - margin.top - margin.bottom) / candidate.bM;
  
  return (
    <svg viewBox={`0 0 ${viewBoxWidth} ${viewBoxHeight}`} className="w-full h-full">
      {/* Axes */}
      <line x1={margin.left} y1={margin.top} x2={margin.left} y2={viewBoxHeight - margin.bottom} stroke="#333" />
      <line x1={margin.left} y1={viewBoxHeight - margin.bottom} x2={viewBoxWidth - margin.right} y2={viewBoxHeight - margin.bottom} stroke="#333" />
      
      {/* Waterplane shape (at z = 0) */}
      <path d={generateWaterplanePath(stations, scaleX, scaleY, margin)} fill="none" stroke="#3b82f6" strokeWidth="2" />
      
      {/* Centerline */}
      <line x1={margin.left} y1={viewBoxHeight/2} x2={viewBoxWidth - margin.right} y2={viewBoxHeight/2} stroke="#999" strokeDasharray="4 4" />
      
      {/* Station lines */}
      {stations.map((station, idx) => (
        <line key={idx} x1={toSvgX(station.x)} y1={margin.top} x2={toSvgX(station.x)} y2={viewBoxHeight - margin.bottom} stroke="#ccc" strokeWidth="0.5" />
      ))}
      
      {/* LCB/LCG markers */}
      <circle cx={toSvgX(candidate.lcbPctLpp * candidate.lppM / 100)} cy={viewBoxHeight/2} r="4" fill="red" />
      
      {/* Dimensions */}
      <text x={viewBoxWidth/2} y={viewBoxHeight - 10} textAnchor="middle">Lpp: {candidate.lppM.toFixed(1)} m</text>
    </svg>
  );
};
```

### ProfileView.tsx (Side Elevation)

- Shows hull outline (keel to deck)
- Waterlines as horizontal lines
- KB, LCB markers
- Freeboard indication
- Dimensions (Lpp, LWL, LOA, D, T)

### BodyPlanView.tsx (Transverse Sections)

- Traditional body plan (forward sections on right, aft on left)
- Sectional area curve (SAC)
- Bonjean curves (optional overlay)

**✓ Completion Checklist:**
- Plan view shows waterplane correctly
- Profile view shows keel line and deck
- Body plan shows all sections
- Dimensions labeled clearly
- SVG responsive (scales with container)
- Export as PNG works

---

## Phase 10: Workspace & Interaction (Week 5, Days 3-5)

### Goal
Build main workspace with input panel, visualization tabs, and interactive controls.

### SizingWorkspace.tsx

**Layout:**
```typescript
<div className="flex h-screen">
  {/* Left Panel (30%) */}
  <div className="w-1/3 border-r overflow-y-auto">
    <MissionSummary mission={currentMissionCase} />
    <LocksPanel locks={locks} onChange={setLocks} />
    <ParameterSliders candidate={selectedCandidate} onAdjust={handleRecompute} />
    <KPISummaryPanel candidate={selectedCandidate} />
  </div>
  
  {/* Right Panel (70%) */}
  <div className="w-2/3 flex flex-col">
    <Tabs defaultValue="3d">
      <TabsList>
        <TabsTrigger value="3d">3D View</TabsTrigger>
        <TabsTrigger value="plan">Plan</TabsTrigger>
        <TabsTrigger value="profile">Profile</TabsTrigger>
        <TabsTrigger value="body">Body Plan</TabsTrigger>
        <TabsTrigger value="offsets">Offsets</TabsTrigger>
      </TabsList>
      
      <TabsContent value="3d" className="flex-1">
        <HullViewer3D candidate={selectedCandidate} />
        <SpeedShapeSlider value={speed} onChange={handleSpeedChange} />
      </TabsContent>
      
      <TabsContent value="plan"><PlanView candidate={selectedCandidate} /></TabsContent>
      <TabsContent value="profile"><ProfileView candidate={selectedCandidate} /></TabsContent>
      <TabsContent value="body"><BodyPlanView candidate={selectedCandidate} /></TabsContent>
      <TabsContent value="offsets"><OffsetsGrid candidate={selectedCandidate} /></TabsContent>
    </Tabs>
    
    {/* Action bar */}
    <div className="border-t p-4 flex gap-2">
      <button onClick={handleSave}>Save Candidate</button>
      <button onClick={handlePushToHydro}>→ Hydrostatics</button>
      <button onClick={handleExport}>Export</button>
    </div>
  </div>
</div>
```

### SpeedShapeSlider.tsx

**Interactive slider with debouncing:**
```typescript
import { debounce } from "lodash";

export const SpeedShapeSlider: React.FC<{ value: number; onChange: (v: number) => void }> = ({ value, onChange }) => {
  const [localValue, setLocalValue] = useState(value);
  
  // Debounce API call (300ms)
  const debouncedOnChange = useMemo(
    () => debounce((v: number) => onChange(v), 300),
    [onChange]
  );
  
  const handleChange = (v: number) => {
    setLocalValue(v); // Immediate UI update
    debouncedOnChange(v); // Debounced API call
  };
  
  return (
    <div className="slider-container">
      <label>← Lower Fn / Fuller Cb</label>
      <input
        type="range"
        min="0"
        max="100"
        value={localValue}
        onChange={(e) => handleChange(Number(e.target.value))}
        className="slider"
      />
      <label>Higher Fn / Finer Cp →</label>
    </div>
  );
};
```

**Recompute Logic:**
```typescript
const handleSpeedChange = async (sliderValue: number) => {
  // Map slider (0-100) to speed range
  const minSpeed = candidate.fn * 0.8;  // 80% of current
  const maxSpeed = candidate.fn * 1.2;  // 120% of current
  const newSpeed = minSpeed + (maxSpeed - minSpeed) * (sliderValue / 100);
  
  try {
    const updated = await hullSizingApi.recomputeCandidate(candidate.id, {
      serviceSpeedKn: newSpeed,
      locks: currentLocks
    });
    
    runInAction(() => {
      // Update candidate in store
      hullSizingStore.updateCandidate(updated);
    });
  } catch (err) {
    console.error("Recompute failed:", err);
  }
};
```

**Performance target:** <300ms from slider drag to UI update

### LocksPanel.tsx

```typescript
export const LocksPanel: React.FC<{ locks: LocksDto; onChange: (locks: LocksDto) => void }> = ({ locks, onChange }) => {
  return (
    <div className="locks-panel">
      <h3>Lock Parameters</h3>
      <div className="space-y-2">
        <label className="flex items-center">
          <input type="checkbox" checked={locks.keepFn} onChange={(e) => onChange({...locks, keepFn: e.target.checked})} />
          <span className="ml-2">Keep Froude Number (Fn)</span>
        </label>
        
        <label className="flex items-center">
          <input type="checkbox" checked={locks.keepLOverB} onChange={(e) => onChange({...locks, keepLOverB: e.target.checked})} />
          <span className="ml-2">Keep L/B Ratio</span>
        </label>
        
        <label className="flex items-center">
          <input type="checkbox" checked={locks.keepBOverT} onChange={(e) => onChange({...locks, keepBOverT: e.target.checked})} />
          <span className="ml-2">Keep B/T Ratio</span>
        </label>
        
        <label className="flex items-center">
          <input type="checkbox" checked={locks.keepDOverT} onChange={(e) => onChange({...locks, keepDOverT: e.target.checked})} />
          <span className="ml-2">Keep D/T Ratio</span>
        </label>
        
        <label className="flex items-center">
          <input type="checkbox" checked={locks.keepCb} onChange={(e) => onChange({...locks, keepCb: e.target.checked})} />
          <span className="ml-2">Keep Cb Band</span>
        </label>
      </div>
    </div>
  );
};
```

**✓ Completion Checklist:**
- Workspace layout responsive
- 3D/2D tabs switch correctly
- Slider updates candidate with <300ms response
- Locks toggle and affect recompute
- KPIs panel shows live metrics
- Action buttons work (Save, Push, Export)

---

## Phase 11: Integration & Handoff (Week 6, Days 1-2)

### Goal
Implement "Push to Hydrostatics" with toast notifications and navigation.

### PushToHydroDialog.tsx

```typescript
export const PushToHydroDialog: React.FC<{ candidate: CandidateDesign; onClose: () => void }> = ({ candidate, onClose }) => {
  const [vesselName, setVesselName] = useState(`Hull Sizing - ${candidate.hullFamily} - ${new Date().toISOString().split('T')[0]}`);
  const [pushing, setPushing] = useState(false);
  const navigate = useNavigate();
  const { toast } = useToast();
  
  const handlePush = async () => {
    setPushing(true);
    try {
      const result = await hullSizingStore.pushToHydrostatics(candidate.id, vesselName);
      
      toast({
        title: "Vessel Created",
        description: `Vessel "${vesselName}" created successfully in Hydrostatics`,
        action: {
          label: "Open Vessel",
          onClick: () => navigate(`/hydrostatics/vessels/${result.vesselId}/workspace`)
        }
      });
      
      onClose();
    } catch (err) {
      toast({
        title: "Push Failed",
        description: getErrorMessage(err),
        variant: "destructive"
      });
    } finally {
      setPushing(false);
    }
  };
  
  return (
    <Dialog open onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Push to Hydrostatics</DialogTitle>
          <DialogDescription>
            Create a vessel in Hydrostatics module with dimensions from this candidate.
          </DialogDescription>
        </DialogHeader>
        
        <div className="space-y-4">
          <div>
            <label>Vessel Name</label>
            <input value={vesselName} onChange={(e) => setVesselName(e.target.value)} />
          </div>
          
          <div className="text-sm text-gray-600">
            <p>Lpp: {candidate.lppM.toFixed(1)} m</p>
            <p>Beam: {candidate.bM.toFixed(1)} m</p>
            <p>Draft: {candidate.tM.toFixed(1)} m</p>
          </div>
        </div>
        
        <DialogFooter>
          <button onClick={onClose} disabled={pushing}>Cancel</button>
          <button onClick={handlePush} disabled={pushing || !vesselName}>
            {pushing ? "Creating..." : "Create Vessel"}
          </button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
};
```

**✓ Completion Checklist:**
- Push to Hydrostatics dialog opens
- Vessel name pre-filled with default
- Idempotency key generated
- Toast notification shows success
- "Open Vessel" link navigates to /hydrostatics/vessels/{id}/workspace
- Error handling (retries with Polly)

---

## Phase 12: Dashboard Integration (Week 6, Day 3)

### Goal
Add Hull Sizing card to dashboard with grouped layout.

### Update DashboardPage.tsx

```typescript
<div className="space-y-8">
  {/* Design Phase */}
  <section>
    <h2 className="text-2xl font-bold mb-4">Design Phase</h2>
    <p className="text-gray-600 mb-4">Early-stage ship design and concept development</p>
    
    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
      {/* Hull Sizing - NEW */}
      <Card className="hover:shadow-lg transition-shadow" onClick={() => navigate("/hull-sizing")}>
        <CardHeader>
          <div className="flex items-start justify-between">
            <div className="rounded-lg bg-purple-500/10 p-3 mb-2">
              <svg className="h-6 w-6 text-purple-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
              </svg>
            </div>
          </div>
          <CardTitle>Hull Sizing</CardTitle>
          <CardDescription>
            Mission→Hull preliminary sizing with 3D visualization
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button className="w-full" variant="outline">Open Hull Sizing →</Button>
        </CardContent>
      </Card>
      
      {/* Catalog Browser */}
      <Card className="hover:shadow-lg transition-shadow" onClick={() => navigate("/catalog")}>
        <CardHeader>
          <div className="flex items-start justify-between">
            <div className="rounded-lg bg-amber-500/10 p-3 mb-2">
              <svg className="h-6 w-6 text-amber-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
              </svg>
            </div>
          </div>
          <CardTitle>Catalog Browser</CardTitle>
          <CardDescription>
            Reference vessels and design data
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button className="w-full" variant="outline">Browse Catalog →</Button>
        </CardContent>
      </Card>
    </div>
  </section>
  
  {/* Analysis Phase */}
  <section>
    <h2 className="text-2xl font-bold mb-4">Analysis Phase</h2>
    <p className="text-gray-600 mb-4">Detailed calculations on defined geometry</p>
    
    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
      {/* Hydrostatics (existing) */}
      {/* Resistance & Powering (existing) */}
    </div>
  </section>
  
  {/* Validation Phase */}
  <section>
    <h2 className="text-2xl font-bold mb-4">Validation Phase</h2>
    <p className="text-gray-600 mb-4">Benchmark against standards</p>
    
    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
      {/* Benchmarks (existing) */}
    </div>
  </section>
</div>
```

**✓ Completion Checklist:**
- Dashboard has 3 sections (Design/Analysis/Validation)
- Hull Sizing card in Design Phase
- Catalog moved to Design Phase
- Hydrostatics and Resistance in Analysis Phase
- Benchmarks in Validation Phase
- All cards navigate correctly

---

## Next: Read `05-SOLVER-ALGORITHM.md` for mathematical details
