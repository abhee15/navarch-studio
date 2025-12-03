import React, { Suspense, useState, useEffect, useRef } from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls, Grid, Environment, Text } from "@react-three/drei";
import type { OrbitControls as OrbitControlsImpl } from "three-stdlib";
import { ParametricHull3D } from "./WigleyHull3D";
import type { CandidateDesign } from "../../../types/sizing";
import { Home, Move3D } from "lucide-react";

interface Hull3DSceneProps {
  candidate: CandidateDesign;
  showWaterplane?: boolean;
  showCenters?: boolean;
  showGrid?: boolean;
}

/**
 * 3D Scene Container for Hull Visualization
 *
 * Features:
 * - Orbit controls (rotate, zoom, pan)
 * - Grid helper (XZ plane)
 * - Environment lighting
 * - Responsive canvas
 */
export const Hull3DScene: React.FC<Hull3DSceneProps> = ({
  candidate,
  showWaterplane = true,
  showCenters = true,
  showGrid = true,
}) => {
  const [showDimensions, setShowDimensions] = useState(false);
  const [showHint, setShowHint] = useState(true);
  const [visibility, setVisibility] = useState({
    waterplane: true,
    centers: true,
    grid: true,
  });
  const controlsRef = useRef<OrbitControlsImpl>(null);

  // Auto-hide hint after 3 seconds or first interaction
  useEffect(() => {
    const timer = setTimeout(() => setShowHint(false), 3000);
    const hideHint = () => setShowHint(false);

    window.addEventListener("mousedown", hideHint, { once: true });
    window.addEventListener("touchstart", hideHint, { once: true });

    return () => {
      clearTimeout(timer);
      window.removeEventListener("mousedown", hideHint);
      window.removeEventListener("touchstart", hideHint);
    };
  }, []);

  const resetCamera = () => {
    if (controlsRef.current) {
      controlsRef.current.reset();
    }
  };

  const setCameraView = (view: "bow" | "stern" | "side" | "top" | "isometric") => {
    if (!controlsRef.current) return;

    const controls = controlsRef.current;

    switch (view) {
      case "bow":
        // Bow quarter view - looking from forward-starboard
        controls.object.position.set(
          cameraDistance * 0.8,
          cameraDistance * 0.5,
          cameraDistance * 1.0
        );
        break;
      case "stern":
        // Stern quarter view - looking from aft-port
        controls.object.position.set(
          -cameraDistance * 0.8,
          cameraDistance * 0.5,
          -cameraDistance * 1.0
        );
        break;
      case "side":
        // Beam view - pure side elevation
        controls.object.position.set(cameraDistance * 1.5, cameraDistance * 0.3, 0);
        break;
      case "top":
        // Top view - plan view angle
        controls.object.position.set(0, cameraDistance * 1.8, 0);
        break;
      case "isometric":
      default:
        // Default isometric view
        controls.object.position.set(cameraX, cameraY, cameraZ);
        break;
    }

    controls.target.set(0, 0, 0);
    controls.update();
  };

  // Calculate optimal camera distance based on hull dimensions
  const lpp = candidate.lppM || 50;
  const beam = candidate.beamM || 10;
  const draft = candidate.draftM || 5;

  // Camera distance should be enough to see the full hull
  // Use diagonal of bounding box: sqrt(L^2 + B^2 + T^2) * 1.5
  const diagonal = Math.sqrt(lpp * lpp + beam * beam + draft * draft);
  const cameraDistance = diagonal * 1.8;

  // Optimal viewing angle: isometric view showing bow, side, and top
  // Position camera to see hull from 3/4 view (bow-left, starboard-right, top-up)
  const cameraX = cameraDistance * 0.65; // Forward-right position for better bow view
  const cameraY = cameraDistance * 0.75; // Higher elevation to see full hull height (35% freeboard)
  const cameraZ = cameraDistance * 0.85; // Pulled back slightly for better perspective

  return (
    <div className="w-full h-full p-4 relative flex flex-col">
      <div className="flex-1 bg-gradient-to-b from-gray-100 via-gray-50 to-gray-100 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900 border border-gray-300 dark:border-gray-600 rounded-t-lg shadow-lg relative">
        <Canvas
          camera={{
            position: [cameraX, cameraY, cameraZ],
            fov: 50, // Slightly wider FOV for better hull visibility
            near: 0.1,
            far: cameraDistance * 5,
          }}
          shadows
        >
          <Suspense fallback={null}>
            {/* Enhanced lighting for better hull form definition */}
            <ambientLight intensity={0.6} />
            {/* Key light from above-forward to highlight bow */}
            <directionalLight
              position={[lpp * 0.5, beam * 0.8, lpp * 0.6]}
              intensity={1.2}
              castShadow
              shadow-mapSize-width={2048}
              shadow-mapSize-height={2048}
            />
            {/* Fill light from port side to reduce harsh shadows */}
            <directionalLight position={[-beam * 0.5, draft * 0.5, lpp * 0.2]} intensity={0.5} />
            {/* Rim light from aft to highlight stern shape */}
            <directionalLight
              position={[0, draft * 0.3, -lpp * 0.4]}
              intensity={0.6}
              color="#c0e0ff"
            />
            {/* Point light at midship for ambient fill */}
            <pointLight position={[0, draft * 1.5, lpp * 0.2]} intensity={0.4} />

            {/* Environment (realistic reflections) */}
            <Environment preset="city" />

            {/* Hull is already centered at origin by geometry generator */}
            <group rotation={[0, Math.PI / 6, 0]}>
              <ParametricHull3D
                candidate={candidate}
                showWaterplane={showWaterplane && visibility.waterplane}
                showCenters={showCenters && visibility.centers}
              />

              {/* Orientation markers - BOW/STERN labels for user clarity */}
              {showCenters && visibility.centers && (
                <>
                  {/* Bow label (forward perpendicular) - GREEN */}
                  <Text
                    position={[0, draft * 0.4, lpp * 0.55]}
                    fontSize={Math.max(lpp * 0.04, 3)}
                    color="#22c55e"
                    anchorX="center"
                    anchorY="middle"
                    outlineWidth={lpp * 0.002}
                    outlineColor="#000000"
                  >
                    BOW (FP)
                  </Text>

                  {/* Stern label (aft perpendicular) - RED */}
                  <Text
                    position={[0, draft * 0.4, -lpp * 0.05]}
                    fontSize={Math.max(lpp * 0.04, 3)}
                    color="#ef4444"
                    anchorX="center"
                    anchorY="middle"
                    outlineWidth={lpp * 0.002}
                    outlineColor="#000000"
                  >
                    STERN (AP)
                  </Text>
                </>
              )}
            </group>

            {/* Grid helper - aligned with hull coordinate system */}
            {/* Grid is in XZ plane (horizontal), positioned at waterline level, rotated with hull */}
            {showGrid && visibility.grid && (
              <group rotation={[0, Math.PI / 6, 0]}>
                <Grid
                  args={[Math.max(lpp, beam) * 2, Math.max(lpp, beam) * 2]}
                  cellSize={Math.max(lpp, beam) / 20}
                  cellThickness={0.5}
                  cellColor="#6b7280"
                  sectionSize={Math.max(lpp, beam) / 4}
                  sectionThickness={1}
                  sectionColor="#3b82f6"
                  fadeDistance={cameraDistance}
                  fadeStrength={1}
                  position={[0, draft, 0]} // Position at waterline (Y = draft)
                />
              </group>
            )}

            {/* Orbit controls */}
            <OrbitControls
              ref={controlsRef}
              enableDamping
              dampingFactor={0.05}
              rotateSpeed={0.8}
              zoomSpeed={1.2}
              minDistance={cameraDistance * 0.4}
              maxDistance={cameraDistance * 2.5}
              maxPolarAngle={Math.PI / 1.8} // Allow slightly below horizon
              target={[0, 0, 0]} // Focus on hull center (already centered)
            />
          </Suspense>
        </Canvas>

        {/* Auto-hide Rotation Hint */}
        {showHint && (
          <div className="absolute inset-0 flex items-center justify-center pointer-events-none z-10">
            <div className="bg-black/70 text-white px-6 py-3 rounded-xl backdrop-blur-sm shadow-2xl animate-pulse">
              <div className="flex items-center gap-2 text-sm font-medium">
                <Move3D className="h-5 w-5" />
                <span>Drag to rotate • Scroll to zoom</span>
              </div>
            </div>
          </div>
        )}

        {/* Camera View Preset Buttons */}
        <div className="absolute top-4 right-4 flex flex-col gap-2">
          <div className="bg-white/90 dark:bg-gray-800/90 backdrop-blur-sm rounded-lg shadow-lg p-2 flex gap-1">
            <button
              onClick={() => setCameraView("bow")}
              className="px-2 py-1 text-xs font-medium text-gray-700 dark:text-gray-300 hover:bg-green-100 dark:hover:bg-green-900/30 rounded transition-colors"
              title="Bow Quarter View"
            >
              Bow
            </button>
            <button
              onClick={() => setCameraView("stern")}
              className="px-2 py-1 text-xs font-medium text-gray-700 dark:text-gray-300 hover:bg-red-100 dark:hover:bg-red-900/30 rounded transition-colors"
              title="Stern Quarter View"
            >
              Stern
            </button>
            <button
              onClick={() => setCameraView("side")}
              className="px-2 py-1 text-xs font-medium text-gray-700 dark:text-gray-300 hover:bg-blue-100 dark:hover:bg-blue-900/30 rounded transition-colors"
              title="Beam View (Side)"
            >
              Side
            </button>
            <button
              onClick={() => setCameraView("top")}
              className="px-2 py-1 text-xs font-medium text-gray-700 dark:text-gray-300 hover:bg-purple-100 dark:hover:bg-purple-900/30 rounded transition-colors"
              title="Top View (Plan)"
            >
              Top
            </button>
            <button
              onClick={resetCamera}
              className="px-2 py-1 text-xs font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded transition-colors flex items-center gap-1"
              title="Reset to Isometric View"
            >
              <Home className="h-3 w-3" />
              Reset
            </button>
          </div>
        </div>
      </div>

      {/* Integrated Info Bar - Part of the same panel */}
      <div className="bg-white dark:bg-gray-800 border-t-0 border-l border-r border-b border-gray-300 dark:border-gray-600 rounded-b-lg shadow-lg p-3 flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        {/* Left: Interactive legend items */}
        <div className="flex items-center gap-3 flex-wrap">
          <button
            onClick={() => setVisibility((prev) => ({ ...prev, waterplane: !prev.waterplane }))}
            className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all text-xs font-medium ${
              visibility.waterplane
                ? "bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300"
                : "bg-gray-200/50 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 line-through"
            }`}
          >
            <div className="w-5 h-0.5 bg-cyan-500"></div>
            <span>Waterplane</span>
          </button>

          <button
            onClick={() => setVisibility((prev) => ({ ...prev, grid: !prev.grid }))}
            className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all text-xs font-medium ${
              visibility.grid
                ? "bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300"
                : "bg-gray-200/50 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 line-through"
            }`}
          >
            <div className="w-5 h-0.5 bg-gray-400"></div>
            <span>Grid</span>
          </button>

          <button
            onClick={() => setVisibility((prev) => ({ ...prev, centers: !prev.centers }))}
            className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all text-xs font-medium ${
              visibility.centers
                ? "bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300"
                : "bg-gray-200/50 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 line-through"
            }`}
          >
            <div className="flex items-center gap-2">
              <div className="w-2 h-2 rounded-full bg-red-500"></div>
              <div className="w-2 h-2 rounded-full bg-green-500"></div>
              <div className="w-2 h-2 rounded-full bg-blue-500"></div>
            </div>
            <span>Centers</span>
          </button>
        </div>

        {/* Separator */}
        <div className="hidden md:block h-6 w-px bg-border"></div>

        {/* Right: Dimensions dropdown */}
        <div className="relative">
          <button
            onClick={() => setShowDimensions(!showDimensions)}
            className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 rounded-lg shadow-sm px-3 py-1.5 text-xs font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors flex items-center gap-1.5"
          >
            {showDimensions ? "▼" : "▶"} Dimensions
          </button>
          {showDimensions && (
            <div className="absolute bottom-full right-0 mb-2 bg-white/95 dark:bg-gray-800/95 backdrop-blur-md rounded-xl shadow-2xl p-4 text-xs border border-gray-200 dark:border-gray-700 min-w-[200px]">
              <div className="font-bold text-gray-900 dark:text-gray-100 mb-3 text-sm border-b border-gray-200 dark:border-gray-600 pb-2">
                Dimensions
              </div>
              <div className="grid grid-cols-2 gap-x-4 gap-y-2">
                <span className="text-gray-600 dark:text-gray-400">Lpp:</span>
                <span className="font-semibold text-gray-900 dark:text-gray-100">
                  {candidate.lppM.toFixed(2)} m
                </span>
                <span className="text-gray-600 dark:text-gray-400">Beam:</span>
                <span className="font-semibold text-gray-900 dark:text-gray-100">
                  {candidate.beamM.toFixed(2)} m
                </span>
                <span className="text-gray-600 dark:text-gray-400">Draft:</span>
                <span className="font-semibold text-cyan-700 dark:text-cyan-400">
                  {candidate.draftM.toFixed(2)} m
                </span>
                <span className="text-gray-600 dark:text-gray-400">Depth:</span>
                <span className="font-semibold text-purple-700 dark:text-purple-400">
                  {candidate.depthM.toFixed(2)} m
                </span>
                <span className="text-gray-600 dark:text-gray-400">Cb:</span>
                <span className="font-semibold text-blue-700 dark:text-blue-400">
                  {candidate.cb.toFixed(4)}
                </span>
                {candidate.lcbPctLpp && (
                  <>
                    <span className="text-gray-600 dark:text-gray-400">LCB:</span>
                    <span className="font-semibold text-red-700 dark:text-red-400">
                      {candidate.lcbPctLpp.toFixed(1)}% Lpp
                    </span>
                  </>
                )}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
