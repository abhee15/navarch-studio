import React, { Suspense } from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import { ParametricHull3D } from "./WigleyHull3D";
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
 * - Uses vessel-type-specific hull shapes
 */
export const Hull3DThumbnail: React.FC<Hull3DThumbnailProps> = ({ candidate, height = 200 }) => {
  // Calculate optimal camera distance based on hull dimensions
  // Use LPP as primary dimension, with padding for beam and draft
  const lpp = candidate.lppM || 50;
  const beam = candidate.beamM || 10;
  const draft = candidate.draftM || 5;

  // Camera distance should be enough to see the full hull
  // Use diagonal of bounding box: sqrt(L^2 + B^2 + T^2) * 1.5
  const diagonal = Math.sqrt(lpp * lpp + beam * beam + draft * draft);
  const cameraDistance = diagonal * 1.8;

  // Optimal viewing angle: isometric view showing bow, side, and top
  // Position camera to see hull from 3/4 view (bow-left, starboard-right, top-up)
  const cameraX = cameraDistance * 0.7; // Slightly forward of center
  const cameraY = cameraDistance * 0.6; // Elevated view
  const cameraZ = cameraDistance * 0.8; // Side view angle

  return (
    <div
      className="w-full bg-gray-100 dark:bg-gray-900 rounded-lg overflow-hidden"
      style={{ height: `${height}px` }}
    >
      <Canvas
        camera={{
          position: [cameraX, cameraY, cameraZ],
          fov: 50, // Slightly wider FOV for better hull visibility
          near: 0.1,
          far: cameraDistance * 5,
        }}
      >
        <Suspense fallback={null}>
          {/* Lighting - improved for better hull definition */}
          <ambientLight intensity={0.5} />
          <directionalLight position={[lpp * 0.5, beam * 0.5, draft * 2]} intensity={1.0} />
          <directionalLight position={[-lpp * 0.3, -beam * 0.3, draft * 1.5]} intensity={0.4} />
          <pointLight position={[0, 0, draft * 2]} intensity={0.3} />

          {/* Hull is already centered at origin by geometry generator */}
          <group
            rotation={[0, Math.PI / 6, 0]} // Rotate 30° for better 3/4 view
          >
            {/* Hull only - no overlays for thumbnail
                IMPORTANT: Use low resolution (0.3 = 18x12 segments instead of 60x40)
                to prevent WebGL context exhaustion when rendering multiple thumbnails */}
            <ParametricHull3D
              candidate={candidate}
              showWaterplane={false}
              showCenters={false}
              opacity={0.9}
              resolution={0.3}
            />
          </group>

          {/* Orbit controls (enable interaction) */}
          <OrbitControls
            enableDamping
            dampingFactor={0.05}
            minDistance={cameraDistance * 0.4}
            maxDistance={cameraDistance * 2.5}
            maxPolarAngle={Math.PI / 1.8} // Allow slightly below horizon
            target={[0, 0, 0]} // Focus on hull center (already centered)
          />
        </Suspense>
      </Canvas>
    </div>
  );
};
