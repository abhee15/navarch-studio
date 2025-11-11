import React, { Suspense, useState, useEffect, useRef } from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls, Grid, Environment } from "@react-three/drei";
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

  // Calculate camera distance based on hull size
  const cameraDistance = Math.max(candidate.lppM, candidate.beamM, candidate.draftM) * 2;

  return (
    <div className="w-full h-full p-4 relative flex flex-col">
      <div className="flex-1 bg-gradient-to-b from-gray-100 via-gray-50 to-gray-100 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900 border border-gray-300 dark:border-gray-600 rounded-t-lg shadow-lg relative">
        <Canvas
        camera={{
          position: [cameraDistance, cameraDistance * 0.6, cameraDistance * 0.8],
          fov: 50,
        }}
        shadows
      >
        <Suspense fallback={null}>
          {/* Lighting */}
          <ambientLight intensity={0.5} />
          <directionalLight
            position={[10, 10, 5]}
            intensity={1}
            castShadow
            shadow-mapSize-width={1024}
            shadow-mapSize-height={1024}
          />
          <pointLight position={[-10, -10, -5]} intensity={0.3} />

          {/* Environment (realistic reflections) */}
          <Environment preset="city" />

          {/* Hull */}
          <ParametricHull3D
            candidate={candidate}
            showWaterplane={showWaterplane && visibility.waterplane}
            showCenters={showCenters && visibility.centers}
          />

          {/* Grid helper */}
          {showGrid && visibility.grid && (
            <Grid
              args={[candidate.lppM * 1.5, candidate.lppM * 1.5]}
              cellSize={1}
              cellThickness={0.5}
              cellColor="#6b7280"
              sectionSize={5}
              sectionThickness={1}
              sectionColor="#3b82f6"
              fadeDistance={100}
              fadeStrength={1}
              position={[0, 0, 0]}
            />
          )}

          {/* Orbit controls */}
          <OrbitControls
            ref={controlsRef}
            enableDamping
            dampingFactor={0.05}
            rotateSpeed={0.8}
            zoomSpeed={1.2}
            minDistance={cameraDistance * 0.5}
            maxDistance={cameraDistance * 3}
            maxPolarAngle={Math.PI / 1.8} // Prevent extreme angles
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

      {/* Reset Camera Button */}
      <button
        onClick={resetCamera}
        className="absolute top-4 right-4 bg-white/90 dark:bg-gray-800/90 backdrop-blur-sm rounded-lg shadow-lg px-3 py-1.5 text-xs font-medium text-gray-700 dark:text-gray-300 hover:bg-white dark:hover:bg-gray-800 transition-colors flex items-center gap-1.5"
        title="Reset View"
      >
        <Home className="h-3 w-3" />
        <span>Reset</span>
      </button>
      </div>

      {/* Integrated Info Bar - Part of the same panel */}
      <div className="bg-white dark:bg-gray-800 border-t-0 border-l border-r border-b border-gray-300 dark:border-gray-600 rounded-b-lg shadow-lg p-3 flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
        {/* Left: Interactive legend items */}
        <div className="flex items-center gap-3 flex-wrap">
          <button
            onClick={() => setVisibility(prev => ({ ...prev, waterplane: !prev.waterplane }))}
            className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all text-xs font-medium ${
              visibility.waterplane
                ? 'bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300'
                : 'bg-gray-200/50 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 line-through'
            }`}
          >
            <div className="w-5 h-0.5 bg-cyan-500"></div>
            <span>Waterplane</span>
          </button>

          <button
            onClick={() => setVisibility(prev => ({ ...prev, grid: !prev.grid }))}
            className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all text-xs font-medium ${
              visibility.grid
                ? 'bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300'
                : 'bg-gray-200/50 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 line-through'
            }`}
          >
            <div className="w-5 h-0.5 bg-gray-400"></div>
            <span>Grid</span>
          </button>

          <button
            onClick={() => setVisibility(prev => ({ ...prev, centers: !prev.centers }))}
            className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all text-xs font-medium ${
              visibility.centers
                ? 'bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300'
                : 'bg-gray-200/50 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 line-through'
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
