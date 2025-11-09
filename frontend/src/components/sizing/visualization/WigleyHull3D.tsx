import React, { useMemo } from "react";
import * as THREE from "three";
import type { CandidateDesign } from "../../../types/sizing";
import { useTheme } from "../../../contexts/ThemeContext";
import { generateHull3DGeometry } from "../../../utils/hullShapeGenerator";

interface ParametricHull3DProps {
  candidate: CandidateDesign;
  showWaterplane?: boolean;
  showCenters?: boolean;
  color?: string;
  opacity?: number;
}

/**
 * Parametric Hull 3D Generator
 *
 * Generates vessel-type-specific 3D hull geometry based on form coefficients.
 * Replaces generic Wigley formula with accurate representations for different vessel types:
 * - Container ships: Parallel mid-body, moderate taper
 * - Tankers: Very full forms, minimal taper
 * - Fishing vessels: Fine ends, curved forms
 * - Yachts: Elegant, streamlined shapes
 */
export const ParametricHull3D: React.FC<ParametricHull3DProps> = ({
  candidate,
  showWaterplane = true,
  showCenters = true,
  color,
  opacity = 0.8,
}) => {
  const { theme } = useTheme();

  // Theme-aware hull color for better contrast
  // Light mode: darker blue (blue-800) for contrast against light gray background
  // Dark mode: brighter blue (blue-400) for contrast against dark gray background
  const hullColor = color || (theme === "dark" ? "#60a5fa" : "#1e40af");

  // Generate vessel-type-specific hull geometry
  const hullGeometry = useMemo(() => {
    const lpp = candidate.lppM || 50;
    const beam = candidate.beamM || 10;
    const draft = candidate.draftM || 5;

    // Defensive check for NaN/undefined
    if (!lpp || !beam || !draft || isNaN(lpp) || isNaN(beam) || isNaN(draft)) {
      console.warn("[ParametricHull3D] Invalid dimensions:", { lpp, beam, draft, candidate });
      // Return empty geometry
      return new THREE.BufferGeometry();
    }

    // Use vessel-type-specific hull shape generator
    try {
      return generateHull3DGeometry({
        hullFamily: candidate.hullFamily,
        lppM: candidate.lppM,
        beamM: candidate.beamM,
        draftM: candidate.draftM,
        cb: candidate.cb,
        cp: candidate.cp,
        cwp: candidate.cwp,
        cm: candidate.cm,
        lcbPctLpp: candidate.lcbPctLpp,
        longitudinalSegments: 60,
        verticalSegments: 40,
      });
    } catch (error) {
      console.error("[ParametricHull3D] Error generating hull geometry:", error);
      // Return empty geometry on error
      return new THREE.BufferGeometry();
    }
  }, [
    candidate.hullFamily,
    candidate.lppM,
    candidate.beamM,
    candidate.draftM,
    candidate.cb,
    candidate.cp,
    candidate.cwp,
    candidate.cm,
    candidate.lcbPctLpp,
  ]);

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
          color={hullColor}
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

// Maintain backward compatibility with old name
export const WigleyHull3D = ParametricHull3D;
