import React, { useMemo } from "react";
import { observer } from "mobx-react-lite";
import * as THREE from "three";
import type { CandidateDesign } from "../../../types/sizing";
import { useTheme } from "../../../contexts/ThemeContext";
import { generateHull3DGeometry } from "../../../utils/hullShapeGenerator";
import { generateShipDHull3D } from "../../../utils/shipdGeometryGenerator";
import { useStore } from "../../../stores";
import {
  normalizeGeometry,
  convertOffsetsGridToShipD,
} from "../../../utils/geometryFormatConverter";

interface ParametricHull3DProps {
  candidate: CandidateDesign;
  showWaterplane?: boolean;
  showCenters?: boolean;
  color?: string;
  opacity?: number;
  /** Resolution multiplier (0-1). Use 0.5 for thumbnails to reduce WebGL load. Default: 1.0 */
  resolution?: number;
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
export const ParametricHull3D: React.FC<ParametricHull3DProps> = observer(
  ({
    candidate,
    showWaterplane = true,
    showCenters = true,
    color,
    opacity = 0.8,
    resolution = 1.0,
  }) => {
    const { theme } = useTheme();

    // Theme-aware hull color for better contrast
    // Light mode: darker blue (blue-800) for contrast against light gray background
    // Dark mode: brighter blue (blue-400) for contrast against dark gray background
    const hullColor = color || (theme === "dark" ? "#60a5fa" : "#1e40af");

    const { sizingStore } = useStore();

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

      // Helper to validate geometry positions (no NaNs)
      const isGeometryValid = (geom: THREE.BufferGeometry | null | undefined) => {
        if (!geom) return false;
        const pos = geom.getAttribute("position") as THREE.BufferAttribute | undefined;
        if (!pos) return false;
        const arr = pos.array as unknown as number[];
        for (let i = 0; i < arr.length; i++) {
          if (!Number.isFinite(arr[i])) return false;
        }
        return true;
      };

      // Check if geometry is available (from backend) - handle both ShipD and OffsetsGrid formats
      if (candidate.geometryJson) {
        try {
          // Normalize geometry to OffsetsGrid format (handles both ShipD and OffsetsGrid)
          const normalizedGeometry = normalizeGeometry(candidate.geometryJson);

          if (normalizedGeometry) {
            // Convert OffsetsGrid to ShipD format for 3D generation
            const shipdSections = convertOffsetsGridToShipD(normalizedGeometry);

            // Use converted geometry for 3D generation
            // Enable smoothing for smooth 3D rendering - interpolation creates smooth surfaces
            // while preserving family-specific characteristics
            // Use higher multipliers (4x) for Isometric view to eliminate faceting
            const fromSections = generateShipDHull3D({
              sections: {
                stations: shipdSections.stations,
                stationPositions: shipdSections.stationPositions || [],
              },
              lppM: lpp,
              draftM: draft, // Pass draft to ensure waterplane alignment
              smooth: true, // Enable interpolation for smooth 3D rendering
              stationMultiplier: 4, // Higher resolution for smoother surfaces
              heightMultiplier: 4, // Higher resolution for smoother surfaces
            });
            if (isGeometryValid(fromSections)) {
              return fromSections;
            }
            console.warn(
              "[ParametricHull3D] Invalid geometry from normalized sections; trying ShipD vector."
            );
          } else {
            // Fallback: Try parsing as ShipD format directly (legacy support)
            const sections = JSON.parse(candidate.geometryJson) as {
              stations?: Array<{
                position: number;
                offsets: Record<number, number>;
                hasBulb?: boolean;
                bulbOffsets?: Record<number, number>;
              }>;
              stationPositions?: number[];
            };
            if (sections && sections.stations && Array.isArray(sections.stations)) {
              // Use ShipD geometry from backend
              // Enable smoothing for smooth 3D rendering - interpolation creates smooth surfaces
              // while preserving family-specific characteristics
              // Use higher multipliers (4x) for Isometric view to eliminate faceting
              const fromSections = generateShipDHull3D({
                sections: {
                  stations: sections.stations,
                  stationPositions: sections.stationPositions || [],
                },
                lppM: lpp,
                draftM: draft, // Pass draft to ensure waterplane alignment
                smooth: true, // Enable interpolation for smooth 3D rendering
                stationMultiplier: 4, // Higher resolution for smoother surfaces
                heightMultiplier: 4, // Higher resolution for smoother surfaces
              });
              if (isGeometryValid(fromSections)) {
                return fromSections;
              }
              console.warn(
                "[ParametricHull3D] Invalid geometry from sections; trying ShipD vector."
              );
            }
          }
        } catch (error) {
          console.warn(
            "[ParametricHull3D] Failed to parse geometry, falling back to parametric:",
            error
          );
        }
      }

      // Check if ShipD parameters are available (generate from vector)
      if (
        candidate.shipdParametersJson &&
        sizingStore.shipdParameters &&
        sizingStore.shipdParameters.length > 0
      ) {
        try {
          const shipdVector = JSON.parse(candidate.shipdParametersJson);
          if (Array.isArray(shipdVector) && shipdVector.length === 45) {
            // Generate ShipD geometry from vector
            const fromVector = generateShipDHull3D({
              shipdVector,
              lppM: lpp,
              beamM: beam,
              draftM: draft,
              metadata: sizingStore.shipdParameters,
              resolution,
            });
            if (isGeometryValid(fromVector)) {
              return fromVector;
            }
            console.warn(
              "[ParametricHull3D] Invalid geometry from vector; falling back to parametric."
            );
          }
        } catch (error) {
          console.warn(
            "[ParametricHull3D] Failed to generate ShipD geometry from vector, falling back to parametric:",
            error
          );
        }
      }

      // Check geometry generation status - don't use fallback if generation failed
      if (
        candidate.geometryGenerationStatus === "BothFailed" ||
        candidate.geometryGenerationStatus === "FormCoefficientFailed"
      ) {
        // Geometry generation failed - return empty geometry instead of misleading fallback
        console.warn(
          "[ParametricHull3D] Geometry generation failed, not using fallback:",
          candidate.geometryGenerationError
        );
        return new THREE.BufferGeometry();
      }

      // Fallback: Use vessel-type-specific hull shape generator
      // Adjust resolution based on prop (thumbnails use lower resolution)
      const longitudinalSegments = Math.max(10, Math.floor(60 * resolution));
      const verticalSegments = Math.max(8, Math.floor(40 * resolution));

      try {
        const parametric = generateHull3DGeometry({
          hullFamily: candidate.hullFamily,
          lppM: candidate.lppM,
          beamM: candidate.beamM,
          draftM: candidate.draftM,
          cb: candidate.cb,
          cp: candidate.cp,
          cwp: candidate.cwp,
          cm: candidate.cm,
          lcbPctLpp: candidate.lcbPctLpp,
          longitudinalSegments,
          verticalSegments,
        });
        if (isGeometryValid(parametric)) {
          return parametric;
        }
        console.warn("[ParametricHull3D] Invalid parametric geometry; returning empty mesh.");
        return new THREE.BufferGeometry();
      } catch (error) {
        console.error("[ParametricHull3D] Error generating hull geometry:", error);
        // Return empty geometry on error
        return new THREE.BufferGeometry();
      }
    }, [candidate, sizingStore.shipdParameters, resolution]);

    // Cleanup hull geometry when component unmounts or geometry changes
    React.useEffect(() => {
      return () => {
        if (hullGeometry) {
          hullGeometry.dispose();
        }
      };
    }, [hullGeometry]);

    // Waterplane (horizontal plane at draft) with proper cleanup
    const waterplaneGeometry = useMemo(() => {
      if (!showWaterplane) return null;

      const lpp = candidate.lppM;
      const beam = candidate.beamM;

      // PlaneGeometry(width, height) creates plane in XY plane
      // After rotateX(-PI/2), it becomes horizontal (XZ plane)
      // Hull coordinate system: X=transverse (beam), Y=vertical (height), Z=longitudinal (lpp)
      // So waterplane should be: width (X) = beam, height (Y→Z after rotation) = lpp
      const geometry = new THREE.PlaneGeometry(beam, lpp);
      geometry.rotateX(-Math.PI / 2); // Rotate to horizontal (XZ plane)
      return geometry;
    }, [candidate.lppM, candidate.beamM, showWaterplane]);

    // Cleanup waterplane geometry
    React.useEffect(() => {
      return () => {
        if (waterplaneGeometry) {
          waterplaneGeometry.dispose();
        }
      };
    }, [waterplaneGeometry]);

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
        lcb: { x: lcbX, y: kb, z: 0, color: "#ef4444" }, // Red - LCB (Y = height from keel)
        lcg: { x: 0, y: draft / 2, z: 0, color: "#10b981" }, // Green - CG (approximate)
        kb: { x: 0, y: kb, z: 0, color: "#3b82f6" }, // Blue - KB (Y = height from keel)
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
          <mesh geometry={waterplaneGeometry} position={[0, candidate.draftM, 0]}>
            <meshStandardMaterial
              color="#06b6d4"
              opacity={0.3}
              transparent
              side={THREE.DoubleSide}
            />
          </mesh>
        )}

        {/* Plan view waterlines overlay - shows hull shape from above in 3D */}
        <WaterlinesOverlay
          geometryJson={candidate.geometryJson}
          lpp={candidate.lppM}
          beam={candidate.beamM}
          draft={candidate.draftM}
        />

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
  }
);

/**
 * Plan View Waterlines Overlay for 3D Hull
 * Renders waterline curves on the 3D hull surface to show plan view shape
 * Helps visualize bow/stern family characteristics (bulbous bow protrusion, transom width, etc.)
 */
function WaterlinesOverlay({
  geometryJson,
  lpp,
  beam,
  draft,
}: {
  geometryJson?: string;
  lpp: number;
  beam: number;
  draft: number;
}) {
  const waterlinesCurves = useMemo(() => {
    if (!geometryJson) return [];

    try {
      // Normalize geometry to OffsetsGrid format
      const normalized = normalizeGeometry(geometryJson);
      if (!normalized || normalized.stations.length === 0 || normalized.waterlines.length === 0) {
        return [];
      }

      const { stations, waterlines, offsets } = normalized;
      const curves: React.ReactElement[] = [];

      // Render every 2nd or 3rd waterline to avoid clutter (select key waterlines)
      const waterlineStep = Math.max(1, Math.floor(waterlines.length / 7)); // Show ~7 waterlines

      for (let wlIdx = 0; wlIdx < waterlines.length; wlIdx += waterlineStep) {
        const waterlineZ = waterlines[wlIdx]; // Vertical position

        // Extract half-breadths for this waterline across all stations
        const points: THREE.Vector3[] = [];

        for (let stIdx = 0; stIdx < stations.length; stIdx++) {
          const stationX = stations[stIdx]; // Longitudinal position
          const halfBreadth = offsets[stIdx]?.[wlIdx] || 0;

          if (halfBreadth > 0) {
            // Add points for port and starboard sides
            // Port side (negative X)
            points.push(new THREE.Vector3(-halfBreadth, waterlineZ, stationX));
          }
        }

        // Add starboard side in reverse order to complete the curve
        for (let stIdx = stations.length - 1; stIdx >= 0; stIdx--) {
          const stationX = stations[stIdx];
          const halfBreadth = offsets[stIdx]?.[wlIdx] || 0;

          if (halfBreadth > 0) {
            // Starboard side (positive X)
            points.push(new THREE.Vector3(halfBreadth, waterlineZ, stationX));
          }
        }

        if (points.length > 1) {
          // Create curve from points
          const curve = new THREE.CatmullRomCurve3(points, false);
          const curvePoints = curve.getPoints(points.length * 2); // Smooth curve

          // Color based on waterline height (gradient from keel to deck)
          const heightRatio = waterlineZ / (draft * 1.5);
          const color = new THREE.Color().setHSL(0.55, 0.7, 0.3 + heightRatio * 0.4); // Blue-cyan gradient

          // Create Three.js Line object
          const lineGeometry = new THREE.BufferGeometry();
          lineGeometry.setFromPoints(curvePoints);
          const lineMaterial = new THREE.LineBasicMaterial({
            color,
            linewidth: 2,
            opacity: 0.8,
            transparent: true,
          });
          const lineObject = new THREE.Line(lineGeometry, lineMaterial);

          curves.push(<primitive key={`waterline-${wlIdx}`} object={lineObject} />);
        }
      }

      return curves;
    } catch (error) {
      console.warn("[WaterlinesOverlay] Failed to generate waterline curves:", error);
      return [];
    }
  }, [geometryJson, lpp, beam, draft]);

  return <group>{waterlinesCurves}</group>;
}

// Maintain backward compatibility with old name
export const WigleyHull3D = ParametricHull3D;
