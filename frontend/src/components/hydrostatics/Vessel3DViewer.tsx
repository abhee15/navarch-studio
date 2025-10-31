import { useMemo, useRef, useEffect, useState, useImperativeHandle, forwardRef } from "react";
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
import type { HydroResult } from "../../types/hydrostatics";

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
}

export interface Vessel3DViewerRef {
  zoomIn: () => void;
  zoomOut: () => void;
  fitToView: () => void;
  setView: (view: "front" | "side" | "back" | "top" | "bottom" | "isometric") => void;
  resetView: () => void;
}

/**
 * Generate parametric hull surface using modified Wigley hull equation
 * y(x,z) = (B/2) * (1 - z²/D²) * (1 - (2x/Lpp - 1)²)
 */
function generateHullGeometry(
  lpp: number,
  beam: number,
  designDraft: number
): THREE.BufferGeometry {
  const geometry = new THREE.BufferGeometry();
  const vertices: number[] = [];
  const indices: number[] = [];

  // Increased grid resolution for better quality
  const xSegments = 60; // Longitudinal segments
  const zSegments = 40; // Vertical segments

  const halfBeam = beam / 2;

  // Generate vertices
  for (let zi = 0; zi <= zSegments; zi++) {
    const z = (zi / zSegments) * designDraft;
    const zNorm = z / designDraft; // Normalized z [0, 1]

    for (let xi = 0; xi <= xSegments; xi++) {
      const x = (xi / xSegments) * lpp;
      const xNorm = (2 * x) / lpp - 1; // Normalized x [-1, 1]

      // Modified Wigley hull equation
      const y = halfBeam * (1 - zNorm * zNorm) * (1 - xNorm * xNorm);

      // Port side (negative y)
      vertices.push(x, -y, z);
    }
  }

  // Generate indices for triangles (port side)
  for (let zi = 0; zi < zSegments; zi++) {
    for (let xi = 0; xi < xSegments; xi++) {
      const a = zi * (xSegments + 1) + xi;
      const b = a + 1;
      const c = a + (xSegments + 1);
      const d = c + 1;

      // Two triangles per quad
      indices.push(a, c, b);
      indices.push(b, c, d);
    }
  }

  // Mirror to starboard side
  const portVertexCount = vertices.length / 3;
  const portStartIndex = portVertexCount;

  for (let zi = 0; zi <= zSegments; zi++) {
    for (let xi = 0; xi <= xSegments; xi++) {
      const idx = zi * (xSegments + 1) + xi;
      const baseIdx = idx * 3;
      const x = vertices[baseIdx];
      const y = vertices[baseIdx + 1];
      const z = vertices[baseIdx + 2];

      // Starboard side (positive y, mirrored)
      vertices.push(x, -y, z);
    }
  }

  // Generate indices for starboard side
  for (let zi = 0; zi < zSegments; zi++) {
    for (let xi = 0; xi < xSegments; xi++) {
      const a = portStartIndex + zi * (xSegments + 1) + xi;
      const b = a + 1;
      const c = a + (xSegments + 1);
      const d = c + 1;

      indices.push(a, b, c);
      indices.push(b, d, c);
    }
  }

  // Add keel/base surface
  const keelStartIdx = vertices.length / 3;
  for (let xi = 0; xi <= xSegments; xi++) {
    const x = (xi / xSegments) * lpp;
    vertices.push(x, 0, 0); // Centerline at keel
  }

  // Connect keel to port and starboard
  for (let xi = 0; xi < xSegments; xi++) {
    const keelA = keelStartIdx + xi;
    const keelB = keelStartIdx + xi + 1;
    const portA = xi;
    const portB = xi + 1;
    const starA = portStartIndex + xi;
    const starB = portStartIndex + xi + 1;

    indices.push(keelA, portA, keelB);
    indices.push(keelB, portA, portB);
    indices.push(keelA, keelB, starA);
    indices.push(keelB, starB, starA);
  }

  geometry.setIndex(indices);
  geometry.setAttribute("position", new THREE.Float32BufferAttribute(vertices, 3));
  geometry.computeVertexNormals();
  geometry.computeBoundingBox();

  return geometry;
}

function HullMesh({
  lpp,
  beam,
  designDraft,
  wireframe,
}: {
  lpp: number;
  beam: number;
  designDraft: number;
  wireframe: boolean;
}) {
  const geometry = useMemo(
    () => generateHullGeometry(lpp, beam, designDraft),
    [lpp, beam, designDraft]
  );

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

  return (
    <mesh position={[lpp / 2, 0, draft]} rotation={[-Math.PI / 2, 0, 0]}>
      <planeGeometry args={[lpp * 1.2, beam * 1.2]} />
      <meshStandardMaterial color="#3b82f6" transparent opacity={0.3} side={THREE.DoubleSide} />
    </mesh>
  );
}

function BuoyancyMarker({ lcb, kb }: { lcb: number; kb: number }) {
  if (lcb === undefined || kb === undefined || lcb <= 0 || kb <= 0) return null;

  const markerSize = Math.max(lcb, kb) * 0.02;
  return (
    <group position={[lcb, 0, kb]}>
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
  return (
    <group position={[lcg, 0, kg]}>
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
}: SceneContentProps) {
  const bounds = useMemo(() => {
    const maxDim = Math.max(lpp, beam, designDraft);
    return maxDim * 1.5;
  }, [lpp, beam, designDraft]);

  // Auto-fit camera when parameters change significantly
  useEffect(() => {
    if (controlsRef.current && cameraRef.current) {
      // Reset controls and adjust camera position
      const maxDim = Math.max(lpp, beam, designDraft);
      const newBounds = maxDim * 1.5;

      // Update camera position to fit new dimensions
      const camera = cameraRef.current;
      camera.position.set(newBounds, newBounds * 0.8, newBounds);
      camera.lookAt(lpp / 2, 0, designDraft / 2);

      // Reset controls to update target
      controlsRef.current.target.set(lpp / 2, 0, designDraft / 2);
      controlsRef.current.update();
    }
  }, [lpp, beam, designDraft, controlsRef, cameraRef]);

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
        target={[lpp / 2, 0, designDraft / 2]}
      />

      {/* Enhanced Lighting */}
      <ambientLight intensity={0.7} />
      <directionalLight position={[10, 10, 5]} intensity={1.0} castShadow />
      <directionalLight position={[-10, 5, -5]} intensity={0.4} />
      <pointLight position={[0, 10, 0]} intensity={0.3} />

      {/* Grid helper - positioned at the base plane (z=0) */}
      {showGrid && (
        <group position={[lpp / 2, 0, 0]}>
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
      <HullMesh lpp={lpp} beam={beam} designDraft={designDraft} wireframe={wireframe} />

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
    { lpp, beam, designDraft, draft, kb, lcb, kg, lcg, currentResult },
    ref
  ) {
    const controlsRef = useRef<OrbitControlsImpl>(null);
    const cameraRef = useRef<THREE.PerspectiveCamera>(null);
    const [wireframe, setWireframe] = useState(false);
    const [showGrid, setShowGrid] = useState(true);
    const [showAxes, setShowAxes] = useState(true);
    const [isCollapsed, setIsCollapsed] = useState(true); // Start collapsed by default

    const displayDraft = draft ?? currentResult?.draft ?? designDraft;
    const displayKb = kb ?? currentResult?.kBz;
    const displayLcb = lcb ?? currentResult?.lCBx;

    const bounds = useMemo(() => {
      const maxDim = Math.max(lpp, beam, designDraft);
      return maxDim * 1.5;
    }, [lpp, beam, designDraft]);

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

    const fitToView = () => {
      if (controlsRef.current && cameraRef.current) {
        const maxDim = Math.max(lpp, beam, designDraft);
        const fitBounds = maxDim * 1.5;
        const camera = cameraRef.current;

        // Calculate position to show entire hull
        const center = new THREE.Vector3(lpp / 2, 0, designDraft / 2);
        camera.position.set(center.x + fitBounds, center.y + fitBounds * 0.8, center.z + fitBounds);

        controlsRef.current.target.copy(center);
        controlsRef.current.update();
      }
    };

    const setView = (view: "front" | "side" | "back" | "top" | "bottom" | "isometric") => {
      if (!controlsRef.current || !cameraRef.current) return;

      const maxDim = Math.max(lpp, beam, designDraft);
      const distance = maxDim * 1.5;
      const center = new THREE.Vector3(lpp / 2, 0, designDraft / 2);
      const camera = cameraRef.current;

      switch (view) {
        case "front":
          camera.position.set(center.x, center.y, center.z + distance);
          break;
        case "side":
          camera.position.set(center.x + distance, center.y, center.z);
          break;
        case "back":
          camera.position.set(center.x, center.y, center.z - distance);
          break;
        case "top":
          camera.position.set(center.x, center.y + distance, center.z);
          break;
        case "bottom":
          camera.position.set(center.x, center.y - distance, center.z);
          break;
        case "isometric":
          camera.position.set(center.x + distance, center.y + distance * 0.8, center.z + distance);
          break;
      }

      camera.lookAt(center);
      controlsRef.current.target.copy(center);
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
