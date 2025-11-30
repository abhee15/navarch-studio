import {
  useMemo,
  useRef,
  useEffect,
  useState,
  useImperativeHandle,
  forwardRef,
  useCallback,
} from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls, Grid, PerspectiveCamera } from "@react-three/drei";
import type { OrbitControls as OrbitControlsImpl } from "three-stdlib";
import * as THREE from "three";
import { observer } from "mobx-react-lite";
import { Button } from "../ui/button";
import {
  ZoomIn,
  ZoomOut,
  Maximize2,
  EyeOff,
  Grid3x3,
  Move3D,
  RotateCcw,
  Box,
  ChevronLeft,
  ChevronRight,
} from "lucide-react";
import type { HydroResult, OffsetsGrid } from "../../types/hydrostatics";
import { geometryApi } from "../../services/hydrostaticsApi";

interface Vessel3DViewerProps {
  lpp: number; // Length between perpendiculars
  beam: number; // Maximum breadth
  designDraft: number; // Design draft
  draft?: number; // Current draft (for waterplane)
  kb?: number; // Center of buoyancy vertical position
  lcb?: number; // Center of buoyancy longitudinal position
  kg?: number; // Center of gravity vertical position
  lcg?: number; // Center of gravity longitudinal position
  currentResult?: HydroResult | null;
  vesselId?: string; // Optional vessel ID to load actual offsets
}

export interface Vessel3DViewerRef {
  zoomIn: () => void;
  zoomOut: () => void;
  fitToView: () => void;
  setView: (view: "front" | "side" | "back" | "top" | "bottom" | "isometric") => void;
  resetView: () => void;
}

/**
 * Generate hull geometry from actual offsets or parametric formula
 * Three.js coordinate system: X = transverse (starboard/port), Y = vertical (up/down), Z = longitudinal (forward/back)
 */
function generateHullGeometryFromOffsets(offsetsGrid: OffsetsGrid): THREE.BufferGeometry {
  const geometry = new THREE.BufferGeometry();
  const vertices: number[] = [];
  const indices: number[] = [];

  let { stations, waterlines, offsets } = offsetsGrid;

  if (stations.length === 0 || waterlines.length === 0 || offsets.length === 0) {
    return geometry; // Return empty geometry
  }

  // Defensive: validate inputs are finite numbers and arrays are consistent
  const allFinite = (arr: number[]) => arr.every((v) => Number.isFinite(v));
  if (
    !allFinite(stations as unknown as number[]) ||
    !allFinite(waterlines as unknown as number[])
  ) {
    console.warn("[HullMesh] Invalid stations/waterlines (non-finite values). Skipping mesh.");
    return geometry;
  }

  // Verify stations are sorted (required for correct mesh generation)
  // If not sorted, sort them and reorder offsets accordingly
  const sortedStations = [...stations].sort((a, b) => a - b);
  const stationsAreSorted = stations.every((val, idx) => val === sortedStations[idx]);

  if (!stationsAreSorted) {
    console.warn("[HullMesh] Stations are not sorted, sorting now and reordering offsets.");
    // Create index map for reordering
    const stationIndices = stations.map((val, idx) => ({ val, idx }));
    stationIndices.sort((a, b) => a.val - b.val);
    const newIndices = stationIndices.map((item) => item.idx);

    // Reorder stations and offsets
    stations = sortedStations;
    offsets = newIndices.map((oldIdx) => offsets[oldIdx]);
  }
  const expectedWaterlineCount = waterlines.length;
  if (!offsets.every((row) => Array.isArray(row) && row.length === expectedWaterlineCount)) {
    console.warn("[HullMesh] Offsets grid rows have inconsistent lengths. Skipping mesh.");
    return geometry;
  }
  for (let s = 0; s < offsets.length; s++) {
    for (let w = 0; w < offsets[s].length; w++) {
      const hb = Number(offsets[s][w]);
      if (!Number.isFinite(hb)) {
        console.warn("[HullMesh] Found non-finite half-breadth at", { s, w, hb });
        return geometry;
      }
    }
  }

  // Generate vertices from actual offsets
  // Three.js: X = transverse (half-breadth), Y = vertical (waterline Z), Z = longitudinal (station X)
  for (let wlIdx = 0; wlIdx < waterlines.length; wlIdx++) {
    const waterlineZ = waterlines[wlIdx]; // Vertical position (Y in Three.js)
    if (!Number.isFinite(waterlineZ)) {
      continue;
    }

    for (let stIdx = 0; stIdx < stations.length; stIdx++) {
      const stationX = stations[stIdx]; // Longitudinal position (Z in Three.js)
      if (!Number.isFinite(stationX)) {
        continue;
      }
      const hbRaw = offsets[stIdx]?.[wlIdx];
      const halfBreadth = Number.isFinite(hbRaw) ? Math.max(0, hbRaw as number) : 0; // Transverse position (X)

      // Port side (negative X)
      vertices.push(-halfBreadth, waterlineZ, stationX);
    }
  }

  // Generate indices for triangles
  for (let wlIdx = 0; wlIdx < waterlines.length - 1; wlIdx++) {
    for (let stIdx = 0; stIdx < stations.length - 1; stIdx++) {
      const a = wlIdx * stations.length + stIdx;
      const b = a + 1;
      const c = a + stations.length;
      const d = c + 1;

      // Two triangles per quad (port side)
      indices.push(a, c, b);
      indices.push(b, c, d);
    }
  }

  // Mirror to starboard side
  const portVertexCount = vertices.length / 3;
  const portStartIndex = portVertexCount;

  for (let wlIdx = 0; wlIdx < waterlines.length; wlIdx++) {
    for (let stIdx = 0; stIdx < stations.length; stIdx++) {
      const idx = wlIdx * stations.length + stIdx;
      const baseIdx = idx * 3;
      const x = vertices[baseIdx];
      const y = vertices[baseIdx + 1];
      const z = vertices[baseIdx + 2];

      // Starboard side (positive X, mirrored)
      vertices.push(-x, y, z);
    }
  }

  // Generate indices for starboard side
  for (let wlIdx = 0; wlIdx < waterlines.length - 1; wlIdx++) {
    for (let stIdx = 0; stIdx < stations.length - 1; stIdx++) {
      const a = portStartIndex + wlIdx * stations.length + stIdx;
      const b = a + 1;
      const c = a + stations.length;
      const d = c + 1;

      indices.push(a, b, c);
      indices.push(b, d, c);
    }
  }

  // Add closing faces at bow tip (last station, forward perpendicular)
  // Close bow end to centerline (pointed tip)
  const lastStationIdx = stations.length - 1;
  for (let wlIdx = 0; wlIdx < waterlines.length - 1; wlIdx++) {
    const aPort = wlIdx * stations.length + lastStationIdx;
    const bPort = (wlIdx + 1) * stations.length + lastStationIdx;
    const aStarboard = portStartIndex + aPort;
    const bStarboard = portStartIndex + bPort;

    // Get half-breadths at this waterline
    const hb1 = offsets[lastStationIdx]?.[wlIdx] ?? 0;
    const hb2 = offsets[lastStationIdx]?.[wlIdx + 1] ?? 0;

    // Create closing face connecting port and starboard sides
    // If half-breadth is very small, vertices are already near centerline, but we still need to close the face
    if (hb1 > 0.0001 || hb2 > 0.0001) {
      // Create closing quad (two triangles) from port to starboard
      // Triangle 1: aPort -> bPort -> aStarboard
      indices.push(aPort, bPort, aStarboard);
      // Triangle 2: bPort -> bStarboard -> aStarboard
      indices.push(bPort, bStarboard, aStarboard);
    }
    // If both are at centerline (hb1 ≈ 0 and hb2 ≈ 0), vertices are already converged, no closing face needed
  }

  // Add closing faces at stern tip (first station, aft perpendicular)
  // Handle both transom sterns (flat face) and canoe sterns (pointed tip)
  const firstStationIdx = 0;

  // Check if stern has width (transom) or tapers to point (canoe)
  // If max half-breadth at stern is > 5% of beam, it's likely a transom
  const maxSternHalfBreadth = Math.max(
    ...(offsets[firstStationIdx] ?? []).map((hb) => Number(hb) || 0)
  );
  const isTransomStern = maxSternHalfBreadth > 0.05; // 5% of beam threshold

  for (let wlIdx = 0; wlIdx < waterlines.length - 1; wlIdx++) {
    const aPort = wlIdx * stations.length + firstStationIdx;
    const bPort = (wlIdx + 1) * stations.length + firstStationIdx;
    const aStarboard = portStartIndex + aPort;
    const bStarboard = portStartIndex + bPort;

    const hb1 = offsets[firstStationIdx]?.[wlIdx] ?? 0;
    const hb2 = offsets[firstStationIdx]?.[wlIdx + 1] ?? 0;

    if (isTransomStern && (hb1 > 0.0001 || hb2 > 0.0001)) {
      // TRANSOM STERN: Create flat closing face (port and starboard are separate)
      // Stern closing face (two triangles forming a quad)
      // Triangle 1: aStarboard -> aPort -> bStarboard
      indices.push(aStarboard, aPort, bStarboard);
      // Triangle 2: bStarboard -> aPort -> bPort
      indices.push(bStarboard, aPort, bPort);
    } else if (!isTransomStern && (hb1 > 0.0001 || hb2 > 0.0001)) {
      // CANOE/CRUISER STERN: Close to centerline (pointed tip, similar to bow)
      // Triangle 1: aPort -> bPort -> aStarboard
      indices.push(aPort, bPort, aStarboard);
      // Triangle 2: bPort -> bStarboard -> aStarboard
      indices.push(bPort, bStarboard, aStarboard);
    }
    // If both are at centerline, no closing face needed (already converged)
  }

  geometry.setIndex(indices);
  geometry.setAttribute("position", new THREE.Float32BufferAttribute(vertices, 3));
  // Defensive check for NaNs
  const posArr = geometry.getAttribute("position") as THREE.BufferAttribute;
  let hasNaN = false;
  {
    const arr = posArr.array as unknown as number[];
    for (let i = 0; i < arr.length; i++) {
      if (!Number.isFinite(arr[i])) {
        hasNaN = true;
        break;
      }
    }
  }
  if (hasNaN) {
    console.warn(
      "[Vessel3DViewer] Invalid offsets produced NaN positions. Returning empty geometry."
    );
    return new THREE.BufferGeometry();
  }
  geometry.computeVertexNormals();
  geometry.computeBoundingBox();

  return geometry;
}

function HullMesh({
  wireframe,
  offsetsGrid,
}: {
  wireframe: boolean;
  offsetsGrid?: OffsetsGrid | null;
}) {
  const geometry = useMemo(() => {
    if (offsetsGrid && offsetsGrid.stations.length > 0 && offsetsGrid.waterlines.length > 0) {
      console.log("[HullMesh] Using actual offsets for geometry generation");
      return generateHullGeometryFromOffsets(offsetsGrid);
    }
    console.warn("[HullMesh] No offsets available; skipping mesh (no parametric fallback)");
    return new THREE.BufferGeometry();
  }, [offsetsGrid]);

  return (
    <mesh geometry={geometry} castShadow receiveShadow>
      <meshStandardMaterial
        color="#6b7280"
        metalness={0.2}
        roughness={0.6}
        side={THREE.DoubleSide}
        wireframe={wireframe}
        flatShading={!wireframe}
      />
    </mesh>
  );
}

function Waterplane({ lpp, beam, draft }: { lpp: number; beam: number; draft: number }) {
  if (!draft || draft <= 0) return null;

  // Three.js: X = transverse, Y = vertical, Z = longitudinal
  // Waterplane is horizontal at Y = draft, centered at Z = lpp/2
  return (
    <mesh position={[0, draft, lpp / 2]} rotation={[-Math.PI / 2, 0, 0]}>
      <planeGeometry args={[beam * 1.2, lpp * 1.2]} />
      <meshStandardMaterial color="#3b82f6" transparent opacity={0.3} side={THREE.DoubleSide} />
    </mesh>
  );
}

function BuoyancyMarker({ lcb, kb }: { lcb: number; kb: number }) {
  if (lcb === undefined || kb === undefined || lcb <= 0 || kb <= 0) return null;

  const markerSize = Math.max(lcb, kb) * 0.02;
  // Three.js: X = transverse, Y = vertical, Z = longitudinal
  return (
    <group position={[0, kb, lcb]}>
      <mesh>
        <sphereGeometry args={[markerSize, 16, 16]} />
        <meshStandardMaterial color="#10b981" />
      </mesh>
    </group>
  );
}

function GravityMarker({ lcg, kg }: { lcg: number; kg: number }) {
  if (kg === undefined || lcg === undefined || kg <= 0 || lcg <= 0) return null;

  const markerSize = Math.max(lcg, kg) * 0.02;
  // Three.js: X = transverse, Y = vertical, Z = longitudinal
  return (
    <group position={[0, kg, lcg]}>
      <mesh>
        <sphereGeometry args={[markerSize, 16, 16]} />
        <meshStandardMaterial color="#ef4444" />
      </mesh>
    </group>
  );
}

interface SceneContentProps extends Vessel3DViewerProps {
  controlsRef: React.RefObject<OrbitControlsImpl | null>;
  cameraRef: React.RefObject<THREE.PerspectiveCamera | null>;
  wireframe: boolean;
  showGrid: boolean;
  showAxes: boolean;
  offsetsGrid?: OffsetsGrid | null;
  fitToView?: () => void;
}

function SceneContent({
  lpp,
  beam,
  designDraft,
  draft,
  kb,
  lcb,
  kg,
  lcg,
  controlsRef,
  cameraRef,
  wireframe,
  showGrid,
  showAxes,
  offsetsGrid,
  fitToView,
}: SceneContentProps) {
  // Calculate bounds from actual offsets if available, otherwise use parametric dimensions
  const bounds = useMemo(() => {
    if (offsetsGrid && offsetsGrid.stations.length > 0 && offsetsGrid.waterlines.length > 0) {
      // Use actual dimensions from offsets
      const maxStation = Math.max(...offsetsGrid.stations);
      const maxWaterline = Math.max(...offsetsGrid.waterlines);
      const maxHalfBreadth = Math.max(
        ...offsetsGrid.offsets.flatMap((stationOffsets) => stationOffsets || [])
      );
      const maxDim = Math.max(maxStation, maxWaterline, maxHalfBreadth * 2);
      return maxDim * 1.5;
    }
    // Fallback to parametric dimensions
    const maxDim = Math.max(lpp, beam, designDraft);
    return maxDim * 1.5;
  }, [lpp, beam, designDraft, offsetsGrid]);

  // Calculate actual hull center and dimensions from offsets
  const hullCenter = useMemo(() => {
    if (offsetsGrid && offsetsGrid.stations.length > 0 && offsetsGrid.waterlines.length > 0) {
      const maxStation = Math.max(...offsetsGrid.stations);
      const maxWaterline = Math.max(...offsetsGrid.waterlines);
      // Three.js: X = transverse (0 at centerline), Y = vertical, Z = longitudinal
      return new THREE.Vector3(0, maxWaterline / 2, maxStation / 2);
    }
    // Fallback to parametric center
    return new THREE.Vector3(0, designDraft / 2, lpp / 2);
  }, [lpp, designDraft, offsetsGrid]);

  // Auto-fit camera when parameters or offsets change
  // Three.js: X = transverse, Y = vertical, Z = longitudinal
  useEffect(() => {
    if (controlsRef.current && cameraRef.current) {
      // Reset controls and adjust camera position
      const newBounds = bounds;

      // Update camera position to fit new dimensions
      const camera = cameraRef.current;
      camera.position.set(
        hullCenter.x + newBounds,
        hullCenter.y + newBounds * 0.8,
        hullCenter.z + newBounds
      );
      camera.lookAt(hullCenter.x, hullCenter.y, hullCenter.z);

      // Reset controls to update target
      controlsRef.current.target.copy(hullCenter);
      controlsRef.current.update();
    }
  }, [bounds, hullCenter, controlsRef, cameraRef]);

  // Auto-fit when offsets are loaded or changed
  useEffect(() => {
    if (offsetsGrid && fitToView) {
      // Small delay to allow geometry to update
      const timer = setTimeout(() => {
        fitToView();
      }, 150);
      return () => clearTimeout(timer);
    }
  }, [offsetsGrid, fitToView]);

  return (
    <>
      <PerspectiveCamera
        ref={cameraRef}
        makeDefault
        position={[bounds, bounds * 0.8, bounds]}
        fov={45}
        near={0.1}
        far={bounds * 10}
      />
      <OrbitControls
        ref={controlsRef}
        enableDamping
        dampingFactor={0.05}
        minDistance={bounds * 0.3}
        maxDistance={bounds * 5}
        enablePan={true}
        enableZoom={true}
        enableRotate={true}
        target={[0, designDraft / 2, lpp / 2]}
      />

      {/* Enhanced Lighting */}
      <ambientLight intensity={0.7} />
      <directionalLight position={[10, 10, 5]} intensity={1.0} castShadow />
      <directionalLight position={[-10, 5, -5]} intensity={0.4} />
      <pointLight position={[0, 10, 0]} intensity={0.3} />

      {/* Grid helper - positioned at the base plane (Y=0) */}
      {/* Three.js: X = transverse, Y = vertical, Z = longitudinal */}
      {showGrid && (
        <group position={[hullCenter.x, 0, hullCenter.z]}>
          <Grid
            args={[bounds * 2, 20]}
            cellColor="#6b7280"
            sectionColor="#4b5563"
            fadeDistance={bounds}
            rotation={[-Math.PI / 2, 0, 0]}
          />
        </group>
      )}

      {/* Axes helper */}
      {showAxes && <axesHelper args={[bounds * 0.4]} />}

      {/* Hull */}
      <HullMesh wireframe={wireframe} offsetsGrid={offsetsGrid} />

      {/* Waterplane */}
      <Waterplane lpp={lpp} beam={beam} draft={draft || designDraft} />

      {/* Center of Buoyancy */}
      {kb !== undefined && lcb !== undefined && <BuoyancyMarker lcb={lcb} kb={kb} />}

      {/* Center of Gravity */}
      <GravityMarker lcg={lcg || 0} kg={kg || 0} />
    </>
  );
}

// Control Panel Component
function ControlPanel({
  onZoomIn,
  onZoomOut,
  onFitToView,
  onSetView,
  onResetView,
  wireframe,
  onToggleWireframe,
  showGrid,
  onToggleGrid,
  showAxes,
  onToggleAxes,
  isCollapsed,
  onToggleCollapse,
}: {
  onZoomIn: () => void;
  onZoomOut: () => void;
  onFitToView: () => void;
  onSetView: (view: "front" | "side" | "back" | "top" | "bottom" | "isometric") => void;
  onResetView: () => void;
  wireframe: boolean;
  onToggleWireframe: () => void;
  showGrid: boolean;
  onToggleGrid: () => void;
  showAxes: boolean;
  onToggleAxes: () => void;
  isCollapsed: boolean;
  onToggleCollapse: () => void;
}) {
  return (
    <div className="absolute top-4 right-4 z-10 flex flex-col gap-2 items-end">
      {/* Collapse/Expand Toggle Button - Always visible */}
      <Button
        variant="default"
        size="icon"
        onClick={onToggleCollapse}
        title={isCollapsed ? "Show Controls" : "Hide Controls"}
        className="h-8 w-8 bg-background/90 backdrop-blur-sm border border-border shadow-lg"
      >
        {isCollapsed ? <ChevronLeft className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
      </Button>

      {/* Control panels - shown when not collapsed */}
      {!isCollapsed && (
        <div className="flex flex-col gap-2">
          {/* Camera Controls */}
          <div className="bg-background/90 backdrop-blur-sm border border-border rounded-lg p-2 shadow-lg">
            <div className="flex flex-col gap-1">
              <div className="text-xs font-semibold text-muted-foreground mb-1 px-2">Camera</div>
              <div className="flex gap-1">
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={onZoomIn}
                  title="Zoom In"
                  className="h-8 w-8"
                >
                  <ZoomIn className="h-4 w-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={onZoomOut}
                  title="Zoom Out"
                  className="h-8 w-8"
                >
                  <ZoomOut className="h-4 w-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={onFitToView}
                  title="Fit to View"
                  className="h-8 w-8"
                >
                  <Maximize2 className="h-4 w-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="icon"
                  onClick={onResetView}
                  title="Reset View"
                  className="h-8 w-8"
                >
                  <RotateCcw className="h-4 w-4" />
                </Button>
              </div>
            </div>
          </div>

          {/* View Presets */}
          <div className="bg-background/90 backdrop-blur-sm border border-border rounded-lg p-2 shadow-lg">
            <div className="flex flex-col gap-1">
              <div className="text-xs font-semibold text-muted-foreground mb-1 px-2">Views</div>
              <div className="grid grid-cols-3 gap-1">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onSetView("front")}
                  title="Front View"
                  className="h-7 text-xs"
                >
                  Front
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onSetView("side")}
                  title="Side View"
                  className="h-7 text-xs"
                >
                  Side
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onSetView("back")}
                  title="Back View"
                  className="h-7 text-xs"
                >
                  Back
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onSetView("top")}
                  title="Top View"
                  className="h-7 text-xs"
                >
                  Top
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onSetView("bottom")}
                  title="Bottom View"
                  className="h-7 text-xs"
                >
                  Bottom
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => onSetView("isometric")}
                  title="Isometric View"
                  className="h-7 text-xs"
                >
                  ISO
                </Button>
              </div>
            </div>
          </div>

          {/* Display Options */}
          <div className="bg-background/90 backdrop-blur-sm border border-border rounded-lg p-2 shadow-lg">
            <div className="flex flex-col gap-1">
              <div className="text-xs font-semibold text-muted-foreground mb-1 px-2">Display</div>
              <div className="flex flex-col gap-1">
                <Button
                  variant={wireframe ? "default" : "ghost"}
                  size="sm"
                  onClick={onToggleWireframe}
                  title="Toggle Wireframe"
                  className="h-7 justify-start text-xs"
                >
                  <Box className="h-3 w-3 mr-1" />
                  Wireframe
                </Button>
                <Button
                  variant={showGrid ? "default" : "ghost"}
                  size="sm"
                  onClick={onToggleGrid}
                  title="Toggle Grid"
                  className="h-7 justify-start text-xs"
                >
                  {showGrid ? (
                    <Grid3x3 className="h-3 w-3 mr-1" />
                  ) : (
                    <EyeOff className="h-3 w-3 mr-1" />
                  )}
                  Grid
                </Button>
                <Button
                  variant={showAxes ? "default" : "ghost"}
                  size="sm"
                  onClick={onToggleAxes}
                  title="Toggle Axes"
                  className="h-7 justify-start text-xs"
                >
                  {showAxes ? (
                    <Move3D className="h-3 w-3 mr-1" />
                  ) : (
                    <EyeOff className="h-3 w-3 mr-1" />
                  )}
                  Axes
                </Button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export const Vessel3DViewer = observer(
  forwardRef<Vessel3DViewerRef, Vessel3DViewerProps>(function Vessel3DViewer(
    { lpp, beam, designDraft, draft, kb, lcb, kg, lcg, currentResult, vesselId },
    ref
  ) {
    const controlsRef = useRef<OrbitControlsImpl>(null);
    const cameraRef = useRef<THREE.PerspectiveCamera>(null);
    const [wireframe, setWireframe] = useState(false);
    const [showGrid, setShowGrid] = useState(true);
    const [showAxes, setShowAxes] = useState(true);
    const [isCollapsed, setIsCollapsed] = useState(true); // Start collapsed by default
    const [offsetsGrid, setOffsetsGrid] = useState<OffsetsGrid | null>(null);

    const displayDraft = draft ?? currentResult?.draft ?? designDraft;
    const displayKb = kb ?? currentResult?.kBz;
    const displayLcb = lcb ?? currentResult?.lCBx;

    // Load actual offsets if vesselId is provided
    useEffect(() => {
      if (!vesselId) {
        setOffsetsGrid(null);
        return;
      }

      let isCancelled = false;

      const isFiniteArray = (arr: number[]) =>
        Array.isArray(arr) && arr.every((v) => Number.isFinite(v));
      const isValidGrid = (grid: OffsetsGrid | null | undefined) => {
        if (!grid) return false;
        if (!isFiniteArray(grid.stations) || !isFiniteArray(grid.waterlines)) return false;
        if (!Array.isArray(grid.offsets) || grid.offsets.length !== grid.stations.length)
          return false;
        for (let i = 0; i < grid.offsets.length; i++) {
          const row = grid.offsets[i];
          if (!Array.isArray(row) || row.length !== grid.waterlines.length) return false;
          for (let j = 0; j < row.length; j++) {
            const v = row[j];
            if (!Number.isFinite(v) || v < 0) return false;
          }
        }
        return true;
      };

      const delay = (ms: number) => new Promise((res) => setTimeout(res, ms));

      const fetchWithRetry = async (retries = 3, baseDelayMs = 300) => {
        let attempt = 0;
        while (attempt <= retries && !isCancelled) {
          try {
            const grid = await geometryApi.getOffsetsGrid(vesselId);
            if (isValidGrid(grid)) {
              if (isCancelled) return;
              console.log("[Vessel3DViewer] Loaded offsets (attempt", attempt + 1, "):", {
                stations: grid.stations.length,
                waterlines: grid.waterlines.length,
                offsets: grid.offsets.length,
                firstStation: grid.stations[0],
                lastStation: grid.stations[grid.stations.length - 1],
                firstWaterline: grid.waterlines[0],
                lastWaterline: grid.waterlines[grid.waterlines.length - 1],
              });
              setOffsetsGrid(grid);
              return;
            } else {
              console.warn("[Vessel3DViewer] Invalid offsets grid received; will retry.");
            }
          } catch (error) {
            console.warn("[Vessel3DViewer] Failed to load offsets; will retry. Error:", error);
          }

          attempt++;
          if (attempt <= retries && !isCancelled) {
            const backoff = baseDelayMs * Math.pow(2, attempt - 1); // 300, 600, 1200...
            await delay(backoff);
          }
        }

        if (!isCancelled) {
          console.error("[Vessel3DViewer] Offsets unavailable after retries; using fallback.");
          setOffsetsGrid(null);
        }
      };

      void fetchWithRetry();

      return () => {
        isCancelled = true;
      };
    }, [vesselId]);

    // Calculate bounds from actual offsets if available, otherwise use parametric dimensions
    const bounds = useMemo(() => {
      if (offsetsGrid && offsetsGrid.stations.length > 0 && offsetsGrid.waterlines.length > 0) {
        // Use actual dimensions from offsets
        const maxStation = Math.max(...offsetsGrid.stations);
        const maxWaterline = Math.max(...offsetsGrid.waterlines);
        const maxHalfBreadth = Math.max(
          ...offsetsGrid.offsets.flatMap((stationOffsets) => stationOffsets || [])
        );
        const maxDim = Math.max(maxStation, maxWaterline, maxHalfBreadth * 2);
        return maxDim * 1.5;
      }
      // Fallback to parametric dimensions
      const maxDim = Math.max(lpp, beam, designDraft);
      return maxDim * 1.5;
    }, [lpp, beam, designDraft, offsetsGrid]);

    // Calculate actual hull center and dimensions from offsets
    const hullCenter = useMemo(() => {
      if (offsetsGrid && offsetsGrid.stations.length > 0 && offsetsGrid.waterlines.length > 0) {
        const maxStation = Math.max(...offsetsGrid.stations);
        const maxWaterline = Math.max(...offsetsGrid.waterlines);
        // Three.js: X = transverse (0 at centerline), Y = vertical, Z = longitudinal
        return new THREE.Vector3(0, maxWaterline / 2, maxStation / 2);
      }
      // Fallback to parametric center
      return new THREE.Vector3(0, designDraft / 2, lpp / 2);
    }, [lpp, designDraft, offsetsGrid]);

    // Camera control functions
    const zoomIn = () => {
      if (controlsRef.current && cameraRef.current) {
        const distance = controlsRef.current.getDistance();
        const newDistance = Math.max(distance * 0.8, bounds * 0.3);
        // Move camera closer by dollying
        const direction = new THREE.Vector3();
        cameraRef.current.getWorldDirection(direction);
        cameraRef.current.position.addScaledVector(direction, distance - newDistance);
        controlsRef.current.update();
      }
    };

    const zoomOut = () => {
      if (controlsRef.current && cameraRef.current) {
        const distance = controlsRef.current.getDistance();
        const newDistance = Math.min(distance * 1.2, bounds * 5);
        // Move camera farther by dollying
        const direction = new THREE.Vector3();
        cameraRef.current.getWorldDirection(direction);
        cameraRef.current.position.addScaledVector(direction, distance - newDistance);
        controlsRef.current.update();
      }
    };

    const fitToView = useCallback(() => {
      if (controlsRef.current && cameraRef.current) {
        const fitBounds = bounds;
        const camera = cameraRef.current;

        // Calculate position to show entire hull
        // Three.js: X = transverse, Y = vertical, Z = longitudinal
        camera.position.set(
          hullCenter.x + fitBounds,
          hullCenter.y + fitBounds * 0.8,
          hullCenter.z + fitBounds
        );

        controlsRef.current.target.copy(hullCenter);
        controlsRef.current.update();
      }
    }, [bounds, hullCenter]);

    const setView = (view: "front" | "side" | "back" | "top" | "bottom" | "isometric") => {
      if (!controlsRef.current || !cameraRef.current) return;

      const distance = bounds;
      // Three.js: X = transverse, Y = vertical, Z = longitudinal
      const camera = cameraRef.current;

      switch (view) {
        case "front":
          // Looking from forward (positive Z)
          camera.position.set(hullCenter.x, hullCenter.y, hullCenter.z + distance);
          break;
        case "side":
          // Looking from starboard (positive X)
          camera.position.set(hullCenter.x + distance, hullCenter.y, hullCenter.z);
          break;
        case "back":
          // Looking from aft (negative Z)
          camera.position.set(hullCenter.x, hullCenter.y, hullCenter.z - distance);
          break;
        case "top":
          // Looking from above (positive Y)
          camera.position.set(hullCenter.x, hullCenter.y + distance, hullCenter.z);
          break;
        case "bottom":
          // Looking from below (negative Y)
          camera.position.set(hullCenter.x, hullCenter.y - distance, hullCenter.z);
          break;
        case "isometric":
          // Isometric view
          camera.position.set(
            hullCenter.x + distance,
            hullCenter.y + distance * 0.8,
            hullCenter.z + distance
          );
          break;
      }

      camera.lookAt(hullCenter.x, hullCenter.y, hullCenter.z);
      controlsRef.current.target.copy(hullCenter);
      controlsRef.current.update();
    };

    const resetView = () => {
      if (controlsRef.current) {
        controlsRef.current.reset();
      }
    };

    // Expose methods via ref
    useImperativeHandle(ref, () => ({
      zoomIn,
      zoomOut,
      fitToView,
      setView,
      resetView,
    }));

    return (
      <div className="w-full h-full bg-gray-900 dark:bg-gray-950 relative">
        <Canvas
          shadows
          gl={{
            antialias: true,
            alpha: false,
            powerPreference: "high-performance",
          }}
        >
          <SceneContent
            lpp={lpp}
            beam={beam}
            designDraft={designDraft}
            draft={displayDraft}
            kb={displayKb}
            lcb={displayLcb}
            kg={kg}
            lcg={lcg}
            currentResult={currentResult}
            controlsRef={controlsRef}
            cameraRef={cameraRef}
            wireframe={wireframe}
            showGrid={showGrid}
            showAxes={showAxes}
            offsetsGrid={offsetsGrid}
            fitToView={fitToView}
          />
        </Canvas>
        <ControlPanel
          onZoomIn={zoomIn}
          onZoomOut={zoomOut}
          onFitToView={fitToView}
          onSetView={setView}
          onResetView={resetView}
          wireframe={wireframe}
          onToggleWireframe={() => setWireframe(!wireframe)}
          showGrid={showGrid}
          onToggleGrid={() => setShowGrid(!showGrid)}
          showAxes={showAxes}
          onToggleAxes={() => setShowAxes(!showAxes)}
          isCollapsed={isCollapsed}
          onToggleCollapse={() => setIsCollapsed(!isCollapsed)}
        />
      </div>
    );
  })
);
