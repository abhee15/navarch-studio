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
  showWaterlines?: boolean; // NEW: Control waterline overlay visibility
  showButtocks?: boolean; // NEW: Control buttock curves visibility
  showSections?: boolean; // NEW: Control section curves visibility
  showWireframe?: boolean; // NEW: Enable true mesh wireframe mode
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
    showWaterlines = true,
    showButtocks = false,
    showSections = false,
    showWireframe = false,
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
            opacity={showWireframe ? 0.3 : opacity}
            transparent={showWireframe || opacity < 1}
            wireframe={showWireframe}
            side={THREE.DoubleSide}
            roughness={0.4}
            metalness={0.15}
            envMapIntensity={0.8}
          />
        </mesh>

        {/* Wireframe edges for better visibility when in wireframe mode */}
        {showWireframe && hullGeometry && (
          <lineSegments>
            <edgesGeometry args={[hullGeometry]} />
            <lineBasicMaterial color={hullColor} linewidth={1} opacity={0.8} transparent />
          </lineSegments>
        )}

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
        {showWaterlines && (
          <WaterlinesOverlay
            geometryJson={candidate.geometryJson}
            lpp={candidate.lppM}
            beam={candidate.beamM}
            draft={candidate.draftM}
          />
        )}

        {/* Buttocks overlay - longitudinal curves */}
        {showButtocks && (
          <ButtocksOverlay
            geometryJson={candidate.geometryJson}
            beam={candidate.beamM}
            draft={candidate.draftM}
            lpp={candidate.lppM}
          />
        )}

        {/* Sections overlay - transverse curves */}
        {showSections && (
          <SectionsOverlay
            geometryJson={candidate.geometryJson}
            beam={candidate.beamM}
            draft={candidate.draftM}
            lpp={candidate.lppM}
          />
        )}

        {/* Hull-waterplane intersection curve - shows exact waterline shape */}
        <WaterplaneIntersectionCurve
          geometryJson={candidate.geometryJson}
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
 * Buttocks Overlay for 3D Hull
 * Renders longitudinal buttock curves on the 3D hull surface
 * Shows hull curvature in the longitudinal direction at constant Y offsets
 */
function ButtocksOverlay({
  geometryJson,
  beam,
  draft,
  lpp,
}: {
  geometryJson?: string;
  beam: number;
  draft: number;
  lpp: number;
}) {
  const buttocksCurves = useMemo(() => {
    if (!geometryJson) return [];

    try {
      const normalized = normalizeGeometry(geometryJson);
      if (!normalized || normalized.stations.length === 0 || normalized.waterlines.length === 0) {
        return [];
      }

      const { stations, waterlines, offsets } = normalized;
      const curves: React.ReactElement[] = [];

      // Generate buttock curves at fixed Y positions (half-breadth values)
      // Typical: 0 (centerline), B/8, B/4, 3B/8, B/2
      const buttockPositions = [0, beam / 8, beam / 4, (3 * beam) / 8, beam / 2];

      for (let buttockIdx = 0; buttockIdx < buttockPositions.length; buttockIdx++) {
        const targetY = buttockPositions[buttockIdx];

        // For each station, interpolate Z values at this Y position
        const points: THREE.Vector3[] = [];

        for (let stIdx = 0; stIdx < stations.length; stIdx++) {
          const stationX = stations[stIdx];

          // Find Z values where half-breadth matches targetY (interpolate between waterlines)
          for (let wlIdx = 0; wlIdx < waterlines.length - 1; wlIdx++) {
            const y1 = offsets[stIdx]?.[wlIdx] || 0;
            const y2 = offsets[stIdx]?.[wlIdx + 1] || 0;
            const z1 = waterlines[wlIdx];
            const z2 = waterlines[wlIdx + 1];

            // Check if targetY is between y1 and y2
            if ((targetY >= y1 && targetY <= y2) || (targetY >= y2 && targetY <= y1)) {
              // Linear interpolation to find Z at targetY
              const t = y2 - y1 !== 0 ? (targetY - y1) / (y2 - y1) : 0;
              const z = z1 + t * (z2 - z1);

              // Add points for both sides
              points.push(new THREE.Vector3(-targetY, z, stationX)); // Port
              points.push(new THREE.Vector3(targetY, z, stationX)); // Starboard
              break;
            }
          }
        }

        if (points.length > 1) {
          // Sort points by Z coordinate for proper curve generation
          points.sort((a, b) => a.z - b.z);

          const curve = new THREE.CatmullRomCurve3(points, false);
          const curvePoints = curve.getPoints(points.length * 2);

          // Color gradient based on Y position
          const yRatio = targetY / (beam / 2);
          const color = new THREE.Color().setHSL(0.35, 0.7, 0.3 + yRatio * 0.4); // Green gradient

          const lineGeometry = new THREE.BufferGeometry();
          lineGeometry.setFromPoints(curvePoints);
          const lineMaterial = new THREE.LineBasicMaterial({
            color,
            linewidth: 2,
            opacity: 0.8,
            transparent: true,
          });
          const lineObject = new THREE.Line(lineGeometry, lineMaterial);

          curves.push(<primitive key={`buttock-${buttockIdx}`} object={lineObject} />);
        }
      }

      return curves;
    } catch (error) {
      console.warn("[ButtocksOverlay] Failed to generate buttock curves:", error);
      return [];
    }
  }, [geometryJson, beam, draft, lpp]);

  return <group>{buttocksCurves}</group>;
}

/**
 * Sections Overlay for 3D Hull
 * Renders transverse section curves on the 3D hull surface
 * Shows hull cross-sections at each station
 */
function SectionsOverlay({
  geometryJson,
  beam,
  draft,
  lpp,
}: {
  geometryJson?: string;
  beam: number;
  draft: number;
  lpp: number;
}) {
  const sectionCurves = useMemo(() => {
    if (!geometryJson) return [];

    try {
      const normalized = normalizeGeometry(geometryJson);
      if (!normalized || normalized.stations.length === 0 || normalized.waterlines.length === 0) {
        return [];
      }

      const { stations, waterlines, offsets } = normalized;
      const curves: React.ReactElement[] = [];

      // Render every Nth station to avoid clutter
      const stationStep = Math.max(1, Math.floor(stations.length / 10)); // Show ~10 sections

      for (let stIdx = 0; stIdx < stations.length; stIdx += stationStep) {
        const stationX = stations[stIdx];

        // Extract section curve points at this station
        const points: THREE.Vector3[] = [];

        // Port side (bottom to top)
        for (let wlIdx = 0; wlIdx < waterlines.length; wlIdx++) {
          const z = waterlines[wlIdx];
          const halfBreadth = offsets[stIdx]?.[wlIdx] || 0;

          if (halfBreadth >= 0) {
            points.push(new THREE.Vector3(-halfBreadth, z, stationX));
          }
        }

        // Starboard side (top to bottom) - mirror and reverse
        for (let wlIdx = waterlines.length - 1; wlIdx >= 0; wlIdx--) {
          const z = waterlines[wlIdx];
          const halfBreadth = offsets[stIdx]?.[wlIdx] || 0;

          if (halfBreadth >= 0) {
            points.push(new THREE.Vector3(halfBreadth, z, stationX));
          }
        }

        if (points.length > 2) {
          const curve = new THREE.CatmullRomCurve3(points, false);
          const curvePoints = curve.getPoints(points.length * 2);

          // Color based on station position (gradient from aft to forward)
          const stationRatio = stIdx / (stations.length - 1);
          const color = new THREE.Color().setHSL(0.85, 0.6, 0.4 + stationRatio * 0.3); // Pink-purple gradient

          const lineGeometry = new THREE.BufferGeometry();
          lineGeometry.setFromPoints(curvePoints);
          const lineMaterial = new THREE.LineBasicMaterial({
            color,
            linewidth: 2,
            opacity: 0.7,
            transparent: true,
          });
          const lineObject = new THREE.Line(lineGeometry, lineMaterial);

          curves.push(<primitive key={`section-${stIdx}`} object={lineObject} />);
        }
      }

      return curves;
    } catch (error) {
      console.warn("[SectionsOverlay] Failed to generate section curves:", error);
      return [];
    }
  }, [geometryJson, beam, draft, lpp]);

  return <group>{sectionCurves}</group>;
}

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

/**
 * Hull-Waterplane Intersection Curve
 * Renders explicit curve where hull meets waterplane at design draft
 * Helps verify geometry accuracy and shows exact waterline shape
 */
function WaterplaneIntersectionCurve({
  geometryJson,
  draft,
}: {
  geometryJson?: string;
  draft: number;
}) {
  const intersectionCurve = useMemo(() => {
    if (!geometryJson) return null;

    try {
      const normalized = normalizeGeometry(geometryJson);
      if (!normalized || normalized.stations.length === 0) {
        return null;
      }

      const { stations, waterlines, offsets } = normalized;

      // Find waterline closest to design draft
      let closestWlIdx = 0;
      let minDiff = Math.abs(waterlines[0] - draft);

      for (let i = 1; i < waterlines.length; i++) {
        const diff = Math.abs(waterlines[i] - draft);
        if (diff < minDiff) {
          minDiff = diff;
          closestWlIdx = i;
        }
      }

      // Extract intersection curve points at design draft waterline
      const points: THREE.Vector3[] = [];

      // Port side (aft to forward)
      for (let stIdx = 0; stIdx < stations.length; stIdx++) {
        const stationX = stations[stIdx];
        const halfBreadth = offsets[stIdx]?.[closestWlIdx] || 0;
        if (halfBreadth > 0) {
          points.push(new THREE.Vector3(-halfBreadth, draft, stationX));
        }
      }

      // Starboard side (forward to aft) to close the curve
      for (let stIdx = stations.length - 1; stIdx >= 0; stIdx--) {
        const stationX = stations[stIdx];
        const halfBreadth = offsets[stIdx]?.[closestWlIdx] || 0;
        if (halfBreadth > 0) {
          points.push(new THREE.Vector3(halfBreadth, draft, stationX));
        }
      }

      if (points.length > 1) {
        const curve = new THREE.CatmullRomCurve3(points, true); // Closed curve
        const curvePoints = curve.getPoints(points.length * 3);
        const lineGeometry = new THREE.BufferGeometry();
        lineGeometry.setFromPoints(curvePoints);
        const lineMaterial = new THREE.LineBasicMaterial({
          color: "#fbbf24", // Yellow/amber for high visibility
          linewidth: 3,
          opacity: 1.0,
        });
        return new THREE.Line(lineGeometry, lineMaterial);
      }

      return null;
    } catch (error) {
      console.warn("[WaterplaneIntersectionCurve] Failed to generate curve:", error);
      return null;
    }
  }, [geometryJson, draft]);

  if (!intersectionCurve) return null;

  return <primitive object={intersectionCurve} />;
}

// Note: This file was renamed from WigleyHull3D.tsx to ParametricHull3D.tsx
// to better reflect its purpose (parametric hull generation, not Wigley-specific)
