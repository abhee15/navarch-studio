import React, { Suspense } from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import { WigleyHull3D } from "./WigleyHull3D";
import type { CandidateDesign } from "../../../types/sizing";

interface Hull3DThumbnailProps {
  candidate: CandidateDesign;
  height?: number;
}

/**
 * Simplified 3D thumbnail for candidate cards
 *
 * - No grid, no overlays, no controls text
 * - Auto-rotate disabled for performance
 * - Fixed camera angle for consistency
 */
export const Hull3DThumbnail: React.FC<Hull3DThumbnailProps> = ({ candidate, height = 200 }) => {
  const cameraDistance = Math.max(candidate.lppM, candidate.beamM, candidate.draftM) * 2;

  return (
    <div
      className="w-full bg-slate-50 rounded-lg overflow-hidden"
      style={{ height: `${height}px` }}
    >
      <Canvas
        camera={{
          position: [cameraDistance * 0.8, cameraDistance * 0.5, cameraDistance * 0.7],
          fov: 45,
        }}
      >
        <Suspense fallback={null}>
          {/* Lighting */}
          <ambientLight intensity={0.6} />
          <directionalLight position={[10, 10, 5]} intensity={0.8} />
          <pointLight position={[-10, -10, -5]} intensity={0.2} />

          {/* Hull only - no overlays for thumbnail */}
          <WigleyHull3D
            candidate={candidate}
            showWaterplane={false}
            showCenters={false}
            opacity={0.9}
          />

          {/* Orbit controls (enable interaction) */}
          <OrbitControls
            enableDamping
            dampingFactor={0.05}
            minDistance={cameraDistance * 0.5}
            maxDistance={cameraDistance * 2}
            maxPolarAngle={Math.PI / 2}
          />
        </Suspense>
      </Canvas>
    </div>
  );
};
