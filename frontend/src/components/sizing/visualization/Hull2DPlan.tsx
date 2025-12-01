import { useMemo, useState, forwardRef } from "react";
import type { CandidateDesign } from "../../../types/sizing";
import { extractWaterlinesFromShipD } from "../../../utils/shipd2DGeometry";
import { generateShipDSections } from "../../../utils/shipdGeometryGenerator";
import { useStore } from "../../../stores";
import { generateSmoothCurve } from "../../../utils/splineInterpolation";
import {
  generateFormCoefficientHull,
  type HullDimensions,
} from "../../../utils/formCoefficientHullGenerator";
import { normalizeGeometry } from "../../../utils/geometryFormatConverter";

interface Hull2DPlanProps {
  candidate: CandidateDesign;
  showWaterlines?: boolean;
  showStations?: boolean;
  showDimensions?: boolean;
  showCenterline?: boolean;
  waterlineCount?: number;
}

/**
 * 2D Plan View (Top-Down Projection) - Professional CAD Style
 *
 * Features:
 * - Vessel-type-specific hull shapes based on form coefficients
 * - Animated waterlines with staggered fade-in
 * - Gradient ocean background
 * - Hover effects with tooltips
 * - Professional engineering drawing aesthetic
 * - SVG-based for crisp scaling and export
 */
export const Hull2DPlan = forwardRef<SVGSVGElement, Hull2DPlanProps>(
  (
    {
      candidate,
      showWaterlines = true,
      showStations = true,
      showDimensions = true,
      showCenterline = true,
      waterlineCount = 7,
    },
    ref
  ) => {
    const [hoveredWaterline, setHoveredWaterline] = useState<number | null>(null);
    const [hoveredStation, setHoveredStation] = useState<number | null>(null);
    const [showDimensionsState, setShowDimensionsState] = useState(showDimensions);
    const [visibility, setVisibility] = useState({
      waterlines: true,
      stations: true,
      centerline: true,
      perpendiculars: true,
      lcb: true,
    });
    const [hoveredLegendItem, setHoveredLegendItem] = useState<string | null>(null);

    const { sizingStore } = useStore();

    // Check geometry generation status - don't use fallback if generation failed
    const geometryGenerationFailed =
      candidate.geometryGenerationStatus === "BothFailed" ||
      candidate.geometryGenerationStatus === "FormCoefficientFailed";

    // Generate waterlines - prioritize ShipD geometry if available
    const waterlines = useMemo(() => {
      // If geometry generation failed, return empty (will show error message)
      if (geometryGenerationFailed) {
        console.warn(
          "[Hull2DPlan] Geometry generation failed, not using fallback:",
          candidate.geometryGenerationError
        );
        return [];
      }

      // Validate candidate dimensions early to prevent NaN propagation
      const lppM = candidate.lppM;
      const beamM = candidate.beamM;
      const draftM = candidate.draftM;

      if (!Number.isFinite(lppM) || lppM <= 0 || !Number.isFinite(beamM) || beamM <= 0) {
        console.warn("[Hull2DPlan] Invalid candidate dimensions, returning empty waterlines", {
          lppM,
          beamM,
          draftM,
        });
        return [];
      }

      // Check if geometry is available (OffsetsGridDto format from form-coefficient generator)
      if (candidate.geometryJson) {
        try {
          // Debug: Log geometryJson content to diagnose format detection issues
          console.log("[Hull2DPlan] Checking geometryJson:", {
            hasGeometryJson: !!candidate.geometryJson,
            geometryJsonLength: candidate.geometryJson?.length,
            geometryJsonPreview: candidate.geometryJson?.substring(0, 200),
            geometryGenerationStatus: candidate.geometryGenerationStatus,
          });

          // Normalize geometry to OffsetsGrid format (handles both ShipD and OffsetsGrid)
          const normalizedGeometry = normalizeGeometry(candidate.geometryJson);

          if (normalizedGeometry) {
            console.log("[Hull2DPlan] Using OffsetsGrid geometry from backend", {
              stationCount: normalizedGeometry.stations.length,
              waterlineCount: normalizedGeometry.waterlines.length,
            });

            // Convert OffsetsGrid to waterlines format
            // normalizedGeometry.offsets is [stationIndex][waterlineIndex]
            const result = normalizedGeometry.waterlines.map((wlZ: number, wlIdx: number) => {
              const points: [number, number][] = [];

              // Extract half-breadths for this waterline across all stations
              for (let stIdx = 0; stIdx < normalizedGeometry.stations.length; stIdx++) {
                const stationX = normalizedGeometry.stations[stIdx];
                const halfBreadth = normalizedGeometry.offsets[stIdx]?.[wlIdx] ?? 0;
                points.push([stationX, halfBreadth]);
              }

              return {
                depth: wlZ,
                points,
                isDesignWaterline: Math.abs(wlZ - draftM) < 0.01,
              };
            });

            console.log("[Hull2DPlan] Extracted waterlines from OffsetsGrid geometry", {
              waterlineCount: result.length,
              designWaterlineIndex: result.findIndex(
                (wl: { isDesignWaterline: boolean }) => wl.isDesignWaterline
              ),
            });
            return result;
          }

          // Check if it's ShipD format (legacy) - normalizeGeometry returned null, try direct parsing
          const geometry = JSON.parse(candidate.geometryJson);
          const sections = geometry as {
            stations?: Array<{
              position: number;
              offsets: Record<number, number>;
              hasBulb?: boolean;
              bulbOffsets?: Record<number, number>;
            }>;
            stationPositions?: number[];
          };

          if (
            sections &&
            sections.stations &&
            Array.isArray(sections.stations) &&
            sections.stations.length > 0
          ) {
            console.log("[Hull2DPlan] Using ShipD geometry from backend", {
              stationCount: sections.stations.length,
              hasBulb: sections.stations.some((s) => s.hasBulb),
            });

            // Convert to ShipDHullSections format
            const shipdSections = {
              stations: sections.stations.map((s) => ({
                position: s.position,
                offsets: s.offsets,
                hasBulb: s.hasBulb || false,
                bulbOffsets: s.bulbOffsets,
              })),
              stationPositions: sections.stationPositions || [],
            };

            // Extract waterlines from ShipD geometry
            // Heights should be from 0 (keel) to draft (waterline)
            const maxDraft = candidate.draftM || 5;
            const waterlineHeights = Array.from(
              { length: waterlineCount },
              (_, i) => (i / (waterlineCount - 1)) * maxDraft
            );

            const result = extractWaterlinesFromShipD(
              shipdSections,
              lppM,
              waterlineHeights,
              draftM
            );
            console.log("[Hull2DPlan] Extracted waterlines from ShipD geometry", {
              waterlineCount: result.length,
              designWaterlineIndex: result.findIndex((wl) => wl.isDesignWaterline),
            });
            return result;
          } else {
            // Debug log - this is expected when backend geometry hasn't been generated yet
            console.debug("[Hull2DPlan] Geometry has no stations, falling back");
          }
        } catch (error) {
          console.error(
            "[Hull2DPlan] Failed to parse geometry, falling back to parametric:",
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
            // Debug log - called repeatedly during optimization
            console.debug("[Hull2DPlan] Generating ShipD geometry from parameters", {
              hasMetadata: sizingStore.shipdParameters.length > 0,
            });

            // Use higher station density for Plan View to ensure smooth waterlines
            // 40-60 stations for typical vessels (120m) gives ~2-3m spacing, sufficient for smooth curves
            const planViewStationCount = Math.max(40, Math.ceil(lppM / 3)); // ~3m spacing minimum
            const sections = generateShipDSections(
              {
                shipdVector,
                lppM: lppM,
                beamM: beamM,
                draftM: draftM,
                metadata: sizingStore.shipdParameters,
                resolution: 1.0,
              },
              planViewStationCount
            );

            // Heights should be from 0 (keel) to draft (waterline)
            const maxDraft = draftM || 5;
            const waterlineHeights = Array.from(
              { length: waterlineCount },
              (_, i) => (i / (waterlineCount - 1)) * maxDraft
            );

            const result = extractWaterlinesFromShipD(sections, lppM, waterlineHeights, draftM);
            // Debug log - called repeatedly during optimization
            console.debug("[Hull2DPlan] Generated waterlines from ShipD parameters", {
              waterlineCount: result.length,
            });
            return result;
          } else {
            console.warn("[Hull2DPlan] Invalid ShipD vector format", {
              isArray: Array.isArray(shipdVector),
              length: Array.isArray(shipdVector) ? shipdVector.length : 0,
            });
          }
        } catch (error) {
          console.error(
            "[Hull2DPlan] Failed to generate ShipD geometry, falling back to parametric:",
            error
          );
        }
      } else {
        console.log("[Hull2DPlan] No ShipD data available", {
          hasParametersJson: !!candidate.shipdParametersJson,
          hasMetadata: !!(sizingStore.shipdParameters && sizingStore.shipdParameters.length > 0),
        });
      }

      // Fallback: Generate using FormCoefficientHullGenerator (solver logic)
      // This ensures non-isometric geometry matching solver output
      try {
        const dims: HullDimensions = {
          length: lppM,
          beam: beamM,
          draft: draftM,
          lcbPercent: candidate.lcbPctLpp ?? 0,
        };

        const generated = generateFormCoefficientHull(
          dims,
          candidate.cb ?? 0.68,
          candidate.cp ?? 0.73,
          candidate.cm ?? 0.93,
          candidate.cwp ?? 0.8,
          23, // BSRA stations
          13, // BSRA waterlines
          candidate.bowFamily,
          candidate.midshipFamily,
          candidate.sternFamily,
          sizingStore.currentRun?.vesselType ?? candidate.vesselType
        );

        // Convert OffsetsGrid to waterlines format (reuse existing logic)
        // generated.offsets is [stationIndex][waterlineIndex]
        const result = generated.waterlines.map((wlZ: number, wlIdx: number) => {
          const points: [number, number][] = [];

          // Extract half-breadths for this waterline across all stations
          for (let stIdx = 0; stIdx < generated.stations.length; stIdx++) {
            const stationX = generated.stations[stIdx];
            const halfBreadth = generated.offsets[stIdx]?.[wlIdx] ?? 0;
            points.push([stationX, halfBreadth]);
          }

          return {
            depth: wlZ,
            points,
            isDesignWaterline: Math.abs(wlZ - draftM) < 0.01,
          };
        });

        console.log("[Hull2DPlan] Generated waterlines using FormCoefficientHullGenerator", {
          waterlineCount: result.length,
        });

        return result;
      } catch (error) {
        console.error(
          "[Hull2DPlan] Failed to generate geometry using FormCoefficientHullGenerator:",
          error
        );
        // Return empty - will show error message
        return [];
      }
    }, [
      geometryGenerationFailed,
      candidate.geometryGenerationError,
      candidate.lppM,
      candidate.bowFamily,
      candidate.midshipFamily,
      candidate.sternFamily,
      candidate.vesselType,
      candidate.geometryGenerationStatus,
      sizingStore.currentRun?.vesselType,
      candidate.beamM,
      candidate.draftM,
      candidate.cb,
      candidate.cp,
      candidate.cwp,
      candidate.cm,
      candidate.lcbPctLpp,
      candidate.geometryJson,
      candidate.shipdParametersJson,
      sizingStore.shipdParameters,
      waterlineCount,
    ]);

    // Calculate actual geometry bounds from waterline points
    // This accounts for hull extensions beyond Lpp (bulbous bow, stern appendages)
    const geometryBounds = useMemo(() => {
      const lpp = candidate.lppM ?? 100;
      const beam = candidate.beamM ?? 20;

      if (waterlines.length === 0) {
        // Fallback to Lpp-based bounds if no waterlines
        return {
          minX: -lpp / 2,
          maxX: lpp / 2,
          maxHalfBreadth: beam / 2,
          extentX: lpp,
          fullBeam: beam,
          centerX: 0,
        };
      }

      let minX = Infinity;
      let maxX = -Infinity;
      let maxHalfBreadth = 0;

      waterlines.forEach((wl) => {
        wl.points.forEach(([x, y]) => {
          if (Number.isFinite(x) && Number.isFinite(y)) {
            minX = Math.min(minX, x);
            maxX = Math.max(maxX, x);
            maxHalfBreadth = Math.max(maxHalfBreadth, Math.abs(y));
          }
        });
      });

      // Use calculated bounds if valid, otherwise fallback to Lpp
      const bounds = {
        minX: Number.isFinite(minX) ? minX : -lpp / 2,
        maxX: Number.isFinite(maxX) ? maxX : lpp / 2,
        maxHalfBreadth: Number.isFinite(maxHalfBreadth) ? maxHalfBreadth : beam / 2,
        extentX: Number.isFinite(minX) && Number.isFinite(maxX) ? maxX - minX : lpp,
        fullBeam: Number.isFinite(maxHalfBreadth) ? maxHalfBreadth * 2 : beam,
        centerX: Number.isFinite(minX) && Number.isFinite(maxX) ? (minX + maxX) / 2 : 0,
      };

      console.log("[Hull2DPlan] Geometry bounds calculated:", {
        minX: bounds.minX,
        maxX: bounds.maxX,
        extentX: bounds.extentX,
        lpp,
        loa: candidate.loaM,
        maxHalfBreadth: bounds.maxHalfBreadth,
        fullBeam: bounds.fullBeam,
      });

      return bounds;
    }, [waterlines, candidate.lppM, candidate.beamM, candidate.loaM]);

    // Generate stations - use ShipD geometry if available
    const stations = useMemo(() => {
      const lpp = candidate.lppM;

      // Validate lpp to prevent NaN
      if (!Number.isFinite(lpp) || lpp <= 0) {
        console.warn("[Hull2DPlan] Invalid lpp for stations:", lpp);
        return [];
      }

      // Check if ShipD geometry is available
      if (candidate.geometryJson) {
        try {
          const sections = JSON.parse(candidate.geometryJson) as {
            stations?: Array<{ position: number }>;
            stationPositions?: number[];
          };

          if (sections && sections.stations && Array.isArray(sections.stations)) {
            // Use ShipD station positions
            return sections.stations.map((s, idx) => ({
              number: idx,
              x: (s.position - 0.5) * lpp, // Convert 0-1 to -Lpp/2 to +Lpp/2
            }));
          }
        } catch (error) {
          console.warn("[Hull2DPlan] Failed to parse ShipD geometry for stations:", error);
        }
      }

      // Fallback: Generate evenly spaced stations
      const numStations = 10;
      return Array.from({ length: numStations + 1 }, (_, i) => ({
        number: i,
        x: (i / numStations) * lpp - lpp / 2,
      }));
    }, [candidate.lppM, candidate.geometryJson]);

    const padding = 60;
    const svgWidth = 800;
    const svgHeight = 400;
    const lpp = candidate.lppM ?? 100; // Fallback to 100m if undefined
    const beam = candidate.beamM ?? 20; // Fallback to 20m if undefined

    // Validate values to prevent NaN
    if (!Number.isFinite(lpp) || lpp <= 0) {
      console.warn("[Hull2DPlan] Invalid Lpp value, using fallback:", candidate.lppM);
      return (
        <div className="w-full h-full flex items-center justify-center text-gray-500">
          Invalid vessel dimensions (Lpp: {String(candidate.lppM)})
        </div>
      );
    }
    if (!Number.isFinite(beam) || beam <= 0) {
      console.warn("[Hull2DPlan] Invalid Beam value, using fallback:", candidate.beamM);
      return (
        <div className="w-full h-full flex items-center justify-center text-gray-500">
          Invalid vessel dimensions (Beam: {String(candidate.beamM)})
        </div>
      );
    }

    // Use LOA if available, otherwise use geometry bounds extent, fallback to Lpp
    // This ensures bulbous bow and stern appendages are visible
    const actualLength = candidate.loaM ?? geometryBounds.extentX ?? lpp;
    // CRITICAL: Use fullBeam (port + starboard) to ensure both sides are visible
    // fullBeam is already calculated as maxHalfBreadth * 2, so it accounts for both sides
    const actualBeam = geometryBounds.fullBeam ?? beam;

    // Verify actualBeam accounts for both port and starboard
    if (geometryBounds.maxHalfBreadth && geometryBounds.fullBeam) {
      const expectedFullBeam = geometryBounds.maxHalfBreadth * 2;
      if (Math.abs(geometryBounds.fullBeam - expectedFullBeam) > 0.01) {
        console.warn("[Hull2DPlan] fullBeam calculation mismatch:", {
          fullBeam: geometryBounds.fullBeam,
          expected: expectedFullBeam,
        });
      }
    }

    const scaleX = (svgWidth - 2 * padding) / actualLength;
    const scaleY = (svgHeight - 2 * padding) / actualBeam;
    const scale = Math.min(scaleX, scaleY);

    // Verify scale accounts for full beam (both port and starboard)
    console.log("[Hull2DPlan] Viewport scaling verification:", {
      actualBeam,
      maxHalfBreadth: geometryBounds.maxHalfBreadth,
      scaleY,
      svgHeight,
      padding,
      beamRange: actualBeam * scale,
      viewportHeight: svgHeight - 2 * padding,
      bothSidesVisible: actualBeam * scale <= svgHeight - 2 * padding,
    });

    console.log("[Hull2DPlan] Scaling calculation:", {
      lpp,
      loa: candidate.loaM,
      geometryExtent: geometryBounds.extentX,
      actualLength,
      beam,
      actualBeam,
      scaleX,
      scaleY,
      scale,
    });

    // Validate scale to prevent NaN
    if (!Number.isFinite(scale) || scale <= 0) {
      console.warn("[Hull2DPlan] Invalid scale calculated:", { scaleX, scaleY, lpp, beam });
      return (
        <div className="w-full h-full flex items-center justify-center text-gray-500">
          Unable to calculate view scale
        </div>
      );
    }

    // Center viewport on actual geometry center (not assumed Lpp/2)
    // This ensures the hull is properly centered even when LOA > Lpp
    const geometryCenterX = geometryBounds.centerX;

    // Coordinate transformation for Plan View (top-down projection)
    // In Plan View, we're looking DOWN at the hull:
    // - X: longitudinal (stern to bow) → SVG X (left to right)
    // - Y: half-breadth (centerline to starboard) → SVG Y (top to bottom)
    //   - Starboard (positive y) → below centerline in SVG (lower on screen)
    //   - Port (negative y) → above centerline in SVG (higher on screen)
    // Note: In SVG, Y increases downward, so "above" means smaller Y values
    const toSVG = (x: number, y: number): [number, number] => {
      // X coordinate: longitudinal position (stern to bow)
      const svgX = svgWidth / 2 + (x - geometryCenterX) * scale;

      // Y coordinate: half-breadth (transverse position)
      // Starboard (positive y) → below centerline (larger SVG Y)
      // Port (negative y) → above centerline (smaller SVG Y)
      const svgY = svgHeight / 2 - y * scale;

      // Validate coordinates are within reasonable bounds
      if (!Number.isFinite(svgX) || !Number.isFinite(svgY)) {
        console.warn("[Hull2DPlan] Invalid SVG coordinate transformation:", { x, y, svgX, svgY });
      }

      return [svgX, svgY];
    };

    const waterlinePath = (points: [number, number][]) => {
      // Naval architecture Plan View: waterlines should be closed curves
      // Path: stern centerline → starboard side → bow centerline → port side → stern centerline

      // Filter out invalid points (NaN, Infinity, etc.)
      const validPoints = points.filter(([x, y]) => Number.isFinite(x) && Number.isFinite(y));

      if (validPoints.length === 0) {
        console.warn("[Hull2DPlan] No valid points for waterline path");
        return { starboard: "", port: "", closed: "" };
      }

      // Debug: Log coordinate ranges
      const xRange = [
        Math.min(...validPoints.map(([x]) => x)),
        Math.max(...validPoints.map(([x]) => x)),
      ];
      const yRange = [
        Math.min(...validPoints.map(([, y]) => y)),
        Math.max(...validPoints.map(([, y]) => y)),
      ];
      console.log("[Hull2DPlan] Waterline coordinate ranges:", {
        xRange,
        yRange,
        pointCount: validPoints.length,
      });

      // Use spline interpolation for smooth curves
      // Convert to format expected by spline utility (x = x, y = y)
      const splinePoints = validPoints.map(([x, y]) => ({ x, y }));

      // Interpolate for smoothness with higher resolution
      // Use 200-300 points for very smooth curves, especially important after cubic spline smoothing
      const numInterpPoints = Math.max(200, Math.ceil(validPoints.length * 10));
      const interpolated = generateSmoothCurve(splinePoints, numInterpPoints);

      // Starboard side: points from stern to bow (x from -lpp/2 to +lpp/2, y >= 0)
      const starboardPoints = interpolated.map((p) => toSVG(p.x, p.y));
      const starboardPath = starboardPoints
        .map(([x, y], i) => {
          // Validate coordinates before formatting
          if (!Number.isFinite(x) || !Number.isFinite(y)) {
            console.warn("[Hull2DPlan] Invalid SVG coordinate:", { x, y });
            return "";
          }
          return `${i === 0 ? "M" : "L"} ${x.toFixed(2)},${y.toFixed(2)}`;
        })
        .filter((segment) => segment !== "")
        .join(" ");

      // Port side: reverse the points and negate y to create closed loop
      // We reverse so the path goes: bow centerline → port side → stern centerline
      // CRITICAL: Negate y to mirror across centerline (port is negative y in world coords)
      // This ensures port side appears above centerline in SVG (since y is negative, -y is positive, but we subtract in toSVG)
      const portPoints = [...interpolated].reverse().map((p) => toSVG(p.x, -p.y));

      // Debug: Log port side coordinate ranges
      const portSVGRange = {
        x: [Math.min(...portPoints.map(([x]) => x)), Math.max(...portPoints.map(([x]) => x))],
        y: [Math.min(...portPoints.map(([, y]) => y)), Math.max(...portPoints.map(([, y]) => y))],
        viewportBounds: { width: svgWidth, height: svgHeight },
        starboardLastPoint: starboardPoints[starboardPoints.length - 1],
        portFirstPoint: portPoints[0],
      };
      console.log("[Hull2DPlan] Port side SVG coordinate ranges:", portSVGRange);

      // Verify port side points are within viewport (with some tolerance)
      const portPointsInViewport = portPoints.filter(
        ([x, y]) => x >= -100 && x <= svgWidth + 100 && y >= -100 && y <= svgHeight + 100
      );
      if (portPointsInViewport.length < portPoints.length) {
        console.warn(
          `[Hull2DPlan] ${portPoints.length - portPointsInViewport.length} port side points outside viewport`
        );
      }

      // Ensure port path connects properly to starboard path at bow centerline
      // The last starboard point should be at bow centerline, and first port point should also be at bow centerline
      const portPath = portPoints
        .map(([x, y]) => {
          // Validate coordinates before formatting
          if (!Number.isFinite(x) || !Number.isFinite(y)) {
            console.warn("[Hull2DPlan] Invalid SVG coordinate (port):", { x, y });
            return "";
          }
          // Use 'L' (line to) for all points - the first 'L' connects to the last starboard point
          // This should be at the bow centerline
          return `L ${x.toFixed(2)},${y.toFixed(2)}`;
        })
        .filter((segment) => segment !== "")
        .join(" ");

      // Close the path by connecting back to stern centerline
      // Path structure: M (move to stern centerline) → L (starboard side) → L (port side) → Z (close)
      // The 'Z' command closes the path by drawing a line from the last point back to the first (M) point
      // CRITICAL: Ensure both paths exist and are non-empty before combining
      if (!starboardPath || !portPath) {
        console.error("[Hull2DPlan] Missing path segments:", {
          hasStarboard: !!starboardPath,
          hasPort: !!portPath,
          starboardLength: starboardPath?.length ?? 0,
          portLength: portPath?.length ?? 0,
        });
      }
      const closedPath = starboardPath && portPath ? `${starboardPath} ${portPath} Z` : "";

      // Verify path closure: first and last points should be at stern centerline
      if (starboardPoints.length > 0 && portPoints.length > 0) {
        const firstPoint = starboardPoints[0];
        const lastPoint = portPoints[portPoints.length - 1];
        const centerlineY = svgHeight / 2;
        const tolerance = 5; // 5 pixels tolerance for centerline closure

        const firstAtCenterline = Math.abs(firstPoint[1] - centerlineY) < tolerance;
        const lastAtCenterline = Math.abs(lastPoint[1] - centerlineY) < tolerance;

        if (!firstAtCenterline || !lastAtCenterline) {
          console.warn("[Hull2DPlan] Path may not close properly at centerline:", {
            firstPoint,
            lastPoint,
            centerlineY,
            firstAtCenterline,
            lastAtCenterline,
          });
        }
      }

      // Debug: Log path segment counts
      console.log("[Hull2DPlan] Path generation:", {
        starboardPoints: starboardPoints.length,
        portPoints: portPoints.length,
        starboardPathLength: starboardPath.length,
        portPathLength: portPath.length,
        closedPathLength: closedPath.length,
        hasStarboard: starboardPath.length > 0,
        hasPort: portPath.length > 0,
        hasClosed: closedPath.length > 0,
      });

      return { starboard: starboardPath, port: portPath, closed: closedPath };
    };

    // Show error message if geometry generation failed
    if (geometryGenerationFailed) {
      return (
        <div className="w-full h-full p-4 relative flex flex-col">
          <div className="flex-1 bg-gradient-to-b from-blue-50 via-white to-cyan-50 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900 border border-gray-300 dark:border-gray-600 rounded-lg shadow-inner flex flex-col items-center justify-center">
            <div className="rounded-lg border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20 shadow-lg max-w-md">
              <div className="p-6">
                <h3 className="text-red-800 dark:text-red-200 font-bold flex items-center gap-2 mb-2">
                  Geometry Generation Failed
                </h3>
                <p className="text-red-700 dark:text-red-300 text-sm mb-3">
                  Unable to generate hull geometry for this candidate. The plan view cannot be
                  displayed.
                </p>
                {candidate.geometryGenerationError && (
                  <div className="mt-3 p-3 bg-red-100 dark:bg-red-900/30 rounded border border-red-200 dark:border-red-800">
                    <p className="text-xs font-mono text-red-800 dark:text-red-200">
                      {candidate.geometryGenerationError}
                    </p>
                  </div>
                )}
                <p className="text-red-600 dark:text-red-400 text-xs mt-3">
                  Please try adjusting parameters or contact support if this issue persists.
                </p>
              </div>
            </div>
          </div>
        </div>
      );
    }

    return (
      <div className="w-full h-full p-4 relative flex flex-col">
        <div className="flex-1 bg-gradient-to-b from-blue-50 via-white to-cyan-50 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900 border border-gray-300 dark:border-gray-600 rounded-t-lg shadow-inner flex flex-col">
          <svg
            ref={ref}
            width="100%"
            height="100%"
            viewBox={`0 0 ${svgWidth} ${svgHeight}`}
            className="flex-1"
          >
            {/* Gradient definitions for professional look */}
            <defs>
              <linearGradient id="waterGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                <stop offset="0%" stopColor="#e0f2fe" stopOpacity="0.3" />
                <stop offset="100%" stopColor="#06b6d4" stopOpacity="0.1" />
              </linearGradient>
              <filter id="dropShadow">
                <feGaussianBlur in="SourceAlpha" stdDeviation="2" />
                <feOffset dx="0" dy="1" result="offsetblur" />
                <feComponentTransfer>
                  <feFuncA type="linear" slope="0.3" />
                </feComponentTransfer>
                <feMerge>
                  <feMergeNode />
                  <feMergeNode in="SourceGraphic" />
                </feMerge>
              </filter>
              <marker
                id="arrowhead-left"
                markerWidth="10"
                markerHeight="7"
                refX="0"
                refY="3.5"
                orient="auto"
              >
                <polygon points="10 0, 10 7, 0 3.5" fill="#374151" />
              </marker>
              <marker
                id="arrowhead-right"
                markerWidth="10"
                markerHeight="7"
                refX="10"
                refY="3.5"
                orient="auto"
              >
                <polygon points="0 0, 10 3.5, 0 7" fill="#374151" />
              </marker>
            </defs>

            {/* Background water effect */}
            <rect
              x="0"
              y="0"
              width={svgWidth}
              height={svgHeight}
              fill="url(#waterGradient)"
              opacity="0.5"
            />

            {/* Title with professional styling */}
            <text
              x={svgWidth / 2}
              y={25}
              textAnchor="middle"
              className="fill-gray-900 dark:fill-gray-100 font-bold"
              style={{ fontSize: "14px", letterSpacing: "0.5px" }}
            >
              PLAN VIEW (TOP-DOWN PROJECTION)
            </text>
            <text
              x={svgWidth / 2}
              y={40}
              textAnchor="middle"
              className="fill-gray-600 dark:fill-gray-400"
              style={{ fontSize: "11px" }}
            >
              {candidate.hullFamily.replace("_", " ").toUpperCase()} · Lpp {lpp.toFixed(2)}m × B{" "}
              {beam.toFixed(2)}m
              {candidate.geometryJson && (
                <tspan className="fill-green-600 dark:fill-green-400" style={{ fontSize: "10px" }}>
                  {" "}
                  · Hull Form Parameters
                </tspan>
              )}
            </text>

            {/* Centerline */}
            {showCenterline && visibility.centerline && Number.isFinite(lpp) && lpp > 0 && (
              <line
                x1={toSVG(-lpp / 2, 0)[0]}
                y1={toSVG(-lpp / 2, 0)[1]}
                x2={toSVG(lpp / 2, 0)[0]}
                y2={toSVG(lpp / 2, 0)[1]}
                stroke="#9ca3af"
                strokeWidth="1"
                strokeDasharray="5,5"
                opacity={hoveredLegendItem && hoveredLegendItem !== "centerline" ? 0.3 : 0.6}
                style={{ transition: "all 0.3s ease" }}
              />
            )}

            {/* Perpendiculars */}
            {showStations &&
              visibility.perpendiculars &&
              Number.isFinite(lpp) &&
              Number.isFinite(beam) &&
              lpp > 0 &&
              beam > 0 && (
                <>
                  <line
                    x1={toSVG(-lpp / 2, -beam / 2)[0]}
                    y1={toSVG(-lpp / 2, -beam / 2)[1]}
                    x2={toSVG(-lpp / 2, beam / 2)[0]}
                    y2={toSVG(-lpp / 2, beam / 2)[1]}
                    stroke="#ef4444"
                    strokeWidth="2.5"
                    filter="url(#dropShadow)"
                    opacity={hoveredLegendItem && hoveredLegendItem !== "perpendiculars" ? 0.3 : 1}
                    style={{ transition: "all 0.3s ease" }}
                  />
                  <text
                    x={toSVG(-lpp / 2, -beam / 2 - 5)[0]}
                    y={toSVG(-lpp / 2, -beam / 2 - 5)[1]}
                    textAnchor="middle"
                    className="fill-red-600 font-bold"
                    style={{ fontSize: "12px" }}
                  >
                    AP
                  </text>

                  <line
                    x1={toSVG(lpp / 2, -beam / 2)[0]}
                    y1={toSVG(lpp / 2, -beam / 2)[1]}
                    x2={toSVG(lpp / 2, beam / 2)[0]}
                    y2={toSVG(lpp / 2, beam / 2)[1]}
                    stroke="#10b981"
                    strokeWidth="2.5"
                    filter="url(#dropShadow)"
                    opacity={hoveredLegendItem && hoveredLegendItem !== "perpendiculars" ? 0.3 : 1}
                    style={{ transition: "all 0.3s ease" }}
                  />
                  <text
                    x={toSVG(lpp / 2, -beam / 2 - 5)[0]}
                    y={toSVG(lpp / 2, -beam / 2 - 5)[1]}
                    textAnchor="middle"
                    className="fill-green-600 font-bold"
                    style={{ fontSize: "12px" }}
                  >
                    FP
                  </text>

                  <line
                    x1={toSVG(0, -beam / 2)[0]}
                    y1={toSVG(0, -beam / 2)[1]}
                    x2={toSVG(0, beam / 2)[0]}
                    y2={toSVG(0, beam / 2)[1]}
                    stroke="#f59e0b"
                    strokeWidth="1.5"
                    strokeDasharray="8,4"
                    opacity="0.8"
                  />
                </>
              )}

            {/* Station markers with hover effects */}
            {showStations &&
              visibility.stations &&
              Number.isFinite(beam) &&
              beam > 0 &&
              stations.map((station) => {
                const [sx] = toSVG(station.x, 0);
                const isHovered = hoveredStation === station.number;
                const dimmed = hoveredLegendItem && hoveredLegendItem !== "stations";

                return (
                  <g
                    key={station.number}
                    onMouseEnter={() => setHoveredStation(station.number)}
                    onMouseLeave={() => setHoveredStation(null)}
                    style={{ cursor: "pointer" }}
                  >
                    <line
                      x1={toSVG(station.x, -beam / 2)[0]}
                      y1={toSVG(station.x, -beam / 2)[1]}
                      x2={toSVG(station.x, beam / 2)[0]}
                      y2={toSVG(station.x, beam / 2)[1]}
                      stroke={isHovered ? "#3b82f6" : "#d1d5db"}
                      strokeWidth={isHovered ? "1.5" : "0.5"}
                      strokeDasharray="2,2"
                      opacity={dimmed ? 0.3 : 1}
                      style={{ transition: "all 0.3s ease" }}
                    />
                    <text
                      x={sx}
                      y={toSVG(station.x, beam / 2 + 3)[1]}
                      textAnchor="middle"
                      className={isHovered ? "fill-blue-600 font-semibold" : "fill-gray-400"}
                      style={{ fontSize: isHovered ? "11px" : "10px", transition: "all 0.2s ease" }}
                    >
                      {station.number}
                    </text>

                    {/* Tooltip on hover */}
                    {isHovered && (
                      <g>
                        <rect
                          x={sx - 30}
                          y={toSVG(station.x, beam / 2 + 3)[1] + 5}
                          width="60"
                          height="18"
                          rx="3"
                          fill="#1f2937"
                          opacity="0.9"
                        />
                        <text
                          x={sx}
                          y={toSVG(station.x, beam / 2 + 3)[1] + 17}
                          textAnchor="middle"
                          fill="#ffffff"
                          style={{ fontSize: "9px" }}
                        >
                          x = {station.x.toFixed(1)}m
                        </text>
                      </g>
                    )}
                  </g>
                );
              })}

            {/* Waterlines with gradient strokes and animations */}
            {showWaterlines &&
              visibility.waterlines &&
              waterlines.map(
                (
                  wl: { points: [number, number][]; isDesignWaterline: boolean; depth: number },
                  idx: number
                ) => {
                  const paths = waterlinePath(wl.points);
                  const dimmed = hoveredLegendItem && hoveredLegendItem !== "waterlines";
                  const isHovered = hoveredWaterline === idx;
                  const baseColor = wl.isDesignWaterline ? "#3b82f6" : "#60a5fa";
                  const hoverColor = wl.isDesignWaterline ? "#2563eb" : "#3b82f6";
                  const strokeWidth = wl.isDesignWaterline ? 2.5 : isHovered ? 2 : 1.2;

                  return (
                    <g
                      key={idx}
                      onMouseEnter={() => setHoveredWaterline(idx)}
                      onMouseLeave={() => setHoveredWaterline(null)}
                      style={{
                        opacity: dimmed ? 0.3 : 0,
                        animation: `fadeIn 0.4s ease-in forwards ${idx * 0.08}s`,
                        transition: "opacity 0.3s ease",
                      }}
                    >
                      {/* Closed waterline path (naval architecture standard) */}
                      {/* Render closed path - includes both starboard and port sides */}
                      {paths.closed && (
                        <path
                          d={paths.closed}
                          fill="none"
                          stroke={isHovered ? hoverColor : baseColor}
                          strokeWidth={strokeWidth}
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          filter={wl.isDesignWaterline ? "url(#dropShadow)" : undefined}
                          style={{
                            transition: "all 0.3s ease",
                            cursor: "pointer",
                          }}
                        />
                      )}

                      {/* Label with background */}
                      {(idx % 2 === 0 || isHovered) && (
                        <g>
                          <rect
                            x={toSVG(lpp / 2 + 6, wl.points[wl.points.length - 1][1])[0]}
                            y={toSVG(lpp / 2, wl.points[wl.points.length - 1][1])[1] - 10}
                            width="55"
                            height="14"
                            rx="2"
                            fill={isHovered ? "#3b82f6" : "#ffffff"}
                            opacity={isHovered ? 0.95 : 0.85}
                            stroke={baseColor}
                            strokeWidth="0.5"
                          />
                          <text
                            x={toSVG(lpp / 2 + 33, 0)[0]}
                            y={toSVG(lpp / 2, wl.points[wl.points.length - 1][1])[1] - 1}
                            className={isHovered ? "fill-white font-semibold" : "fill-blue-600"}
                            style={{ fontSize: "9px" }}
                          >
                            WL{idx} {wl.depth.toFixed(1)}m
                          </text>
                        </g>
                      )}
                    </g>
                  );
                }
              )}

            {/* Dimensions */}
            {showDimensionsState && (
              <>
                <line
                  x1={toSVG(-lpp / 2, -beam / 2 - 8)[0]}
                  y1={toSVG(-lpp / 2, -beam / 2 - 8)[1]}
                  x2={toSVG(lpp / 2, -beam / 2 - 8)[0]}
                  y2={toSVG(lpp / 2, -beam / 2 - 8)[1]}
                  stroke="#374151"
                  strokeWidth="1.2"
                  markerStart="url(#arrowhead-left)"
                  markerEnd="url(#arrowhead-right)"
                />
                <rect
                  x={svgWidth / 2 - 45}
                  y={toSVG(0, -beam / 2 - 8)[1] - 20}
                  width="90"
                  height="16"
                  rx="3"
                  fill="#ffffff"
                  stroke="#3b82f6"
                  strokeWidth="1"
                />
                <text
                  x={svgWidth / 2}
                  y={toSVG(0, -beam / 2 - 8)[1] - 9}
                  textAnchor="middle"
                  className="fill-gray-900 font-bold"
                  style={{ fontSize: "11px" }}
                >
                  Lpp = {lpp.toFixed(2)} m
                </text>

                <line
                  x1={toSVG(-lpp / 2 - 8, -beam / 2)[0]}
                  y1={toSVG(-lpp / 2 - 8, -beam / 2)[1]}
                  x2={toSVG(-lpp / 2 - 8, beam / 2)[0]}
                  y2={toSVG(-lpp / 2 - 8, beam / 2)[1]}
                  stroke="#374151"
                  strokeWidth="1.2"
                  markerStart="url(#arrowhead-left)"
                  markerEnd="url(#arrowhead-right)"
                />
                <rect
                  x={toSVG(-lpp / 2 - 8, 0)[0] - 60}
                  y={svgHeight / 2 - 10}
                  width="80"
                  height="16"
                  rx="3"
                  fill="#ffffff"
                  stroke="#3b82f6"
                  strokeWidth="1"
                />
                <text
                  x={toSVG(-lpp / 2 - 8, 0)[0] - 20}
                  y={svgHeight / 2 + 1}
                  textAnchor="middle"
                  className="fill-gray-900 font-bold"
                  style={{ fontSize: "11px" }}
                >
                  B = {beam.toFixed(2)} m
                </text>
              </>
            )}

            {/* LCB marker with glow effect */}
            {candidate.lcbPctLpp && visibility.lcb && (
              <g
                opacity={hoveredLegendItem && hoveredLegendItem !== "lcb" ? 0.3 : 1}
                style={{ transition: "opacity 0.3s ease" }}
              >
                <circle
                  cx={toSVG((candidate.lcbPctLpp / 100 - 0.5) * lpp, 0)[0]}
                  cy={toSVG((candidate.lcbPctLpp / 100 - 0.5) * lpp, 0)[1]}
                  r="6"
                  fill="#ef4444"
                  stroke="#ffffff"
                  strokeWidth="2"
                  filter="url(#dropShadow)"
                  style={{
                    animation: "pulse 2s ease-in-out infinite",
                  }}
                />
                <text
                  x={toSVG((candidate.lcbPctLpp / 100 - 0.5) * lpp, 0)[0]}
                  y={toSVG((candidate.lcbPctLpp / 100 - 0.5) * lpp, 0)[1] - 14}
                  textAnchor="middle"
                  className="fill-red-600 font-bold"
                  style={{ fontSize: "11px" }}
                >
                  LCB
                </text>
              </g>
            )}

            {/* Scale ruler */}
            <text x={20} y={svgHeight - 10} className="fill-gray-500" style={{ fontSize: "10px" }}>
              Scale 1:{Math.round(1 / scale)}
            </text>
          </svg>
        </div>

        {/* Integrated Info Bar - Part of the same panel */}
        <div className="bg-white dark:bg-gray-800 border-t-0 border-l border-r border-b border-gray-300 dark:border-gray-600 rounded-b-lg shadow-lg p-3 flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
          {/* Left: Interactive legend items */}
          <div className="flex items-center gap-3 flex-wrap">
            <button
              onClick={() => setVisibility((prev) => ({ ...prev, waterlines: !prev.waterlines }))}
              onMouseEnter={() => setHoveredLegendItem("waterlines")}
              onMouseLeave={() => setHoveredLegendItem(null)}
              className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all text-xs font-medium ${
                visibility.waterlines
                  ? "bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300"
                  : "bg-gray-200/50 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 line-through"
              }`}
            >
              <div className="w-5 h-0.5 bg-gradient-to-r from-blue-600 to-blue-400"></div>
              <span>Waterlines</span>
            </button>

            <button
              onClick={() => setVisibility((prev) => ({ ...prev, stations: !prev.stations }))}
              onMouseEnter={() => setHoveredLegendItem("stations")}
              onMouseLeave={() => setHoveredLegendItem(null)}
              className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all text-xs font-medium ${
                visibility.stations
                  ? "bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300"
                  : "bg-gray-200/50 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 line-through"
              }`}
            >
              <div className="w-5 h-0.5 bg-gray-400"></div>
              <span>Stations</span>
            </button>

            <button
              onClick={() =>
                setVisibility((prev) => ({ ...prev, perpendiculars: !prev.perpendiculars }))
              }
              onMouseEnter={() => setHoveredLegendItem("perpendiculars")}
              onMouseLeave={() => setHoveredLegendItem(null)}
              className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all text-xs font-medium ${
                visibility.perpendiculars
                  ? "bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300"
                  : "bg-gray-200/50 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 line-through"
              }`}
            >
              <div className="w-5 h-0.5 bg-gradient-to-r from-red-500 to-green-500"></div>
              <span>Perpendiculars</span>
            </button>

            <button
              onClick={() => setVisibility((prev) => ({ ...prev, centerline: !prev.centerline }))}
              onMouseEnter={() => setHoveredLegendItem("centerline")}
              onMouseLeave={() => setHoveredLegendItem(null)}
              className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all text-xs font-medium ${
                visibility.centerline
                  ? "bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300"
                  : "bg-gray-200/50 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 line-through"
              }`}
            >
              <div className="w-5 h-0.5 bg-gray-500"></div>
              <span>Centerline</span>
            </button>

            {candidate.lcbPctLpp && (
              <button
                onClick={() => setVisibility((prev) => ({ ...prev, lcb: !prev.lcb }))}
                onMouseEnter={() => setHoveredLegendItem("lcb")}
                onMouseLeave={() => setHoveredLegendItem(null)}
                className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all text-xs font-medium ${
                  visibility.lcb
                    ? "bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300"
                    : "bg-gray-200/50 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 line-through"
                }`}
              >
                <div className="w-3 h-3 rounded-full bg-red-500"></div>
                <span>LCB</span>
              </button>
            )}
          </div>

          {/* Separator */}
          <div className="hidden md:block h-6 w-px bg-border"></div>

          {/* Right: Dimensions dropdown */}
          <div className="relative">
            <button
              onClick={() => setShowDimensionsState(!showDimensionsState)}
              className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 rounded-lg shadow-sm px-3 py-1.5 text-xs font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors flex items-center gap-1.5"
            >
              {showDimensionsState ? "▼" : "▶"} Dimensions
            </button>
            {showDimensionsState && (
              <div className="absolute bottom-full right-0 mb-2 bg-white/95 dark:bg-gray-800/95 backdrop-blur-md rounded-xl shadow-2xl p-4 text-xs border border-gray-200 dark:border-gray-700 min-w-[200px]">
                <div className="font-bold text-gray-900 dark:text-gray-100 mb-3 text-sm border-b border-gray-200 dark:border-gray-600 pb-2">
                  Dimensions
                </div>
                <div className="grid grid-cols-2 gap-x-4 gap-y-2">
                  <span className="text-gray-600 dark:text-gray-400">Lpp:</span>
                  <span className="font-semibold text-gray-900 dark:text-gray-100">
                    {lpp.toFixed(2)} m
                  </span>
                  <span className="text-gray-600 dark:text-gray-400">Lwl:</span>
                  <span className="font-semibold text-gray-900 dark:text-gray-100">
                    {candidate.lwlM.toFixed(2)} m
                  </span>
                  <span className="text-gray-600 dark:text-gray-400">LOA:</span>
                  <span className="font-semibold text-gray-900 dark:text-gray-100">
                    {candidate.loaM.toFixed(2)} m
                  </span>
                  <span className="text-gray-600 dark:text-gray-400">Beam:</span>
                  <span className="font-semibold text-blue-700 dark:text-blue-400">
                    {beam.toFixed(2)} m
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

        {/* Add keyframes for animations */}
        <style>{`
        @keyframes fadeIn {
          from { opacity: 0; transform: translateY(5px); }
          to { opacity: 1; transform: translateY(0); }
        }
        @keyframes pulse {
          0%, 100% { opacity: 1; }
          50% { opacity: 0.6; }
        }
      `}</style>
      </div>
    );
  }
);

Hull2DPlan.displayName = "Hull2DPlan";
