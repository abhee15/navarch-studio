import React, { Suspense } from 'react';
import { Canvas } from '@react-three/fiber';
import { OrbitControls, Grid, Environment } from '@react-three/drei';
import { WigleyHull3D } from './WigleyHull3D';
import type { CandidateDesign } from '../../../types/sizing';

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
  // Calculate camera distance based on hull size
  const cameraDistance = Math.max(candidate.lppM, candidate.bM, candidate.tM) * 2;

  return (
    <div className="w-full h-full relative bg-slate-50">
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
          <WigleyHull3D
            candidate={candidate}
            showWaterplane={showWaterplane}
            showCenters={showCenters}
          />

          {/* Grid helper */}
          {showGrid && (
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
            enableDamping
            dampingFactor={0.05}
            minDistance={cameraDistance * 0.5}
            maxDistance={cameraDistance * 3}
            maxPolarAngle={Math.PI / 2} // Prevent going below ground
          />
        </Suspense>
      </Canvas>

      {/* Legend overlay */}
      <div className="absolute top-4 right-4 bg-white/90 backdrop-blur-sm rounded-lg shadow-lg p-4 space-y-2 text-sm">
        <h3 className="font-semibold text-gray-900 mb-2">Hull Parameters</h3>
        <div className="space-y-1">
          <div className="flex items-center gap-2">
            <span className="text-gray-600">Lpp:</span>
            <span className="font-medium">{candidate.lppM.toFixed(2)} m</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-gray-600">Beam:</span>
            <span className="font-medium">{candidate.bM.toFixed(2)} m</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-gray-600">Draft:</span>
            <span className="font-medium">{candidate.tM.toFixed(2)} m</span>
          </div>
          <div className="flex items-center gap-2">
            <span className="text-gray-600">Cb:</span>
            <span className="font-medium">{candidate.cb.toFixed(3)}</span>
          </div>
        </div>

        {showCenters && (
          <>
            <hr className="my-2 border-gray-200" />
            <div className="space-y-1">
              <div className="flex items-center gap-2">
                <div className="w-3 h-3 rounded-full bg-red-500"></div>
                <span className="text-xs text-gray-600">LCB (Center of Buoyancy)</span>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-3 h-3 rounded-full bg-green-500"></div>
                <span className="text-xs text-gray-600">LCG (Center of Gravity)</span>
              </div>
              <div className="flex items-center gap-2">
                <div className="w-3 h-3 rounded-full bg-blue-500"></div>
                <span className="text-xs text-gray-600">KB (Vertical CoB)</span>
              </div>
            </div>
          </>
        )}
      </div>

      {/* Controls hint */}
      <div className="absolute bottom-4 left-4 bg-white/90 backdrop-blur-sm rounded-lg shadow-lg p-3 text-xs text-gray-600">
        <div className="space-y-1">
          <div><span className="font-medium">Left Click + Drag:</span> Rotate</div>
          <div><span className="font-medium">Right Click + Drag:</span> Pan</div>
          <div><span className="font-medium">Scroll:</span> Zoom</div>
        </div>
      </div>
    </div>
  );
};

