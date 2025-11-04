import React, { useMemo } from "react";
import * as THREE from "three";
import type { CandidateDesign } from "../../../types/sizing";

interface WigleyHull3DProps {
  candidate: CandidateDesign;
  showWaterplane?: boolean;
  showCenters?: boolean;
  color?: string;
  opacity?: number;
}

/**
 * Wigley Hull Generator (Parametric)
 *
 * Wigley form: y = (B/2) · (1 - z²/T²) · (1 - ((2x/L) - 1)²)
 *
 * Simple parabolic form good for preliminary design visualization.
 * For production, consider Series 60, KCS, or NURBS-based forms.
 */
export const WigleyHull3D: React.FC<WigleyHull3DProps> = ({
  candidate,
  showWaterplane = true,
  showCenters = true,
  color = "#3b82f6",
  opacity = 0.8,
}) => {
  // Generate hull geometry
  const hullGeometry = useMemo(() => {
    const lpp = candidate.lppM || 50;
    const beam = candidate.beamM || 10;
    const draft = candidate.draftM || 5;

    // Defensive check for NaN/undefined
    if (!lpp || !beam || !draft || isNaN(lpp) || isNaN(beam) || isNaN(draft)) {
      console.warn("[WigleyHull3D] Invalid dimensions:", { lpp, beam, draft, candidate });
      // Return empty geometry
      return new THREE.BufferGeometry();
    }

    // Mesh resolution (balance between quality and performance)
    const longitudinalSegments = 60;
    const verticalSegments = 40;

    const geometry = new THREE.BufferGeometry();
    const vertices: number[] = [];
    const indices: number[] = [];

    // Generate vertices
    for (let i = 0; i <= longitudinalSegments; i++) {
      const x = (i / longitudinalSegments) * lpp - lpp / 2; // x: -L/2 to +L/2
      const xNorm = (2 * i) / longitudinalSegments - 1; // x: -1 to +1

      for (let j = 0; j <= verticalSegments; j++) {
        const z = -(j / verticalSegments) * draft; // z: 0 to -T (downward)
        const zNorm = z / draft; // z: 0 to -1

        // Wigley equation: y = (B/2) * (1 - z²/T²) * (1 - x_norm²)
        const y = (beam / 2) * (1 - zNorm * zNorm) * (1 - xNorm * xNorm);

        // Starboard side (positive y)
        vertices.push(x, y, z);
        // Port side (negative y) - mirror
        vertices.push(x, -y, z);
      }
    }

    // Generate indices for triangles
    for (let i = 0; i < longitudinalSegments; i++) {
      for (let j = 0; j < verticalSegments; j++) {
        const stride = (verticalSegments + 1) * 2; // 2 sides per longitudinal station

        // Starboard side
        const a = i * stride + j * 2;
        const b = a + stride;
        const c = a + 2;
        const d = b + 2;

        indices.push(a, b, c);
        indices.push(b, d, c);

        // Port side
        const a2 = a + 1;
        const b2 = b + 1;
        const c2 = c + 1;
        const d2 = d + 1;

        indices.push(a2, c2, b2);
        indices.push(b2, c2, d2);
      }
    }

    geometry.setAttribute("position", new THREE.Float32BufferAttribute(vertices, 3));
    geometry.setIndex(indices);
    geometry.computeVertexNormals(); // Smooth shading

    return geometry;
  }, [candidate.lppM, candidate.beamM, candidate.draftM]);

  // Waterplane (horizontal plane at draft)
  const waterplaneGeometry = useMemo(() => {
    if (!showWaterplane) return null;

    const lpp = candidate.lppM;
    const beam = candidate.beamM;

    const geometry = new THREE.PlaneGeometry(lpp, beam);
    geometry.rotateX(-Math.PI / 2); // Rotate to horizontal
    return geometry;
  }, [candidate.lppM, candidate.beamM, showWaterplane]);

  // Center markers (LCB, LCG, KB)
  const centerMarkers = useMemo(() => {
    if (!showCenters) return null;

    const lpp = candidate.lppM;
    const draft = candidate.draftM;
    const lcbPct = candidate.lcbPctLpp || 0;
    const kb = candidate.kbM || draft / 2; // Approximate if not provided

    // LCB position (% aft of FP)
    const lcbX = (lcbPct / 100) * lpp - lpp / 2;

    return {
      lcb: { x: lcbX, y: 0, z: -kb, color: "#ef4444" }, // Red - LCB
      lcg: { x: 0, y: 0, z: -draft / 2, color: "#10b981" }, // Green - CG (approximate)
      kb: { x: 0, y: 0, z: -kb, color: "#3b82f6" }, // Blue - KB
    };
  }, [candidate.lppM, candidate.draftM, candidate.lcbPctLpp, candidate.kbM, showCenters]);

  return (
    <group>
      {/* Hull mesh */}
      <mesh geometry={hullGeometry}>
        <meshStandardMaterial
          color={color}
          opacity={opacity}
          transparent={opacity < 1}
          side={THREE.DoubleSide}
          roughness={0.5}
          metalness={0.1}
        />
      </mesh>

      {/* Waterplane */}
      {showWaterplane && waterplaneGeometry && (
        <mesh geometry={waterplaneGeometry} position={[0, 0, 0]}>
          <meshStandardMaterial color="#06b6d4" opacity={0.3} transparent side={THREE.DoubleSide} />
        </mesh>
      )}

      {/* Center markers */}
      {showCenters && centerMarkers && (
        <>
          {/* LCB (Longitudinal Center of Buoyancy) */}
          <mesh position={[centerMarkers.lcb.x, centerMarkers.lcb.y, centerMarkers.lcb.z]}>
            <sphereGeometry args={[0.3, 16, 16]} />
            <meshStandardMaterial color={centerMarkers.lcb.color} />
          </mesh>

          {/* LCG (Longitudinal Center of Gravity) - Approximate */}
          <mesh position={[centerMarkers.lcg.x, centerMarkers.lcg.y, centerMarkers.lcg.z]}>
            <sphereGeometry args={[0.3, 16, 16]} />
            <meshStandardMaterial color={centerMarkers.lcg.color} />
          </mesh>

          {/* KB (Vertical Center of Buoyancy) */}
          <mesh position={[centerMarkers.kb.x, centerMarkers.kb.y, centerMarkers.kb.z]}>
            <sphereGeometry args={[0.3, 16, 16]} />
            <meshStandardMaterial color={centerMarkers.kb.color} />
          </mesh>
        </>
      )}
    </group>
  );
};
