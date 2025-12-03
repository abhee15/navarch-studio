import { useMemo, useState, forwardRef } from "react";
import type { CandidateDesign } from "../../../types/sizing";
import {
  extractButtocksFromShipD,
  extractSheerlineFromShipD,
  extractButtocksFromOffsetsGrid,
  extractSheerlineFromOffsetsGrid,
} from "../../../utils/shipd2DGeometry";
import { normalizeGeometry } from "../../../utils/geometryFormatConverter";
import {
  generateShipDSections,
  type ShipDHullStation,
} from "../../../utils/shipdGeometryGenerator";
import { useStore } from "../../../stores";
import { generateSmoothCurve } from "../../../utils/splineInterpolation";
import {
  generateFormCoefficientHull,
  type HullDimensions,
} from "../../../utils/formCoefficientHullGenerator";
import {
  generateControlPointGridFromOffsets,
  diagnoseHullSection,
} from "../../../utils/nurbsSurface";

interface Hull2DProfileProps {
  candidate: CandidateDesign;
  showButtocks?: boolean;
  showWaterline?: boolean;
  showDimensions?: boolean;
  showBaseline?: boolean;
  buttockCount?: number;
}

/**
 * 2D Profile View (Side Elevation) - Professional Engineering Drawing Style
 *
 * Features:
 * - Gradient sky-to-water background
 * - Animated buttock curves (staggered fade-in)
 * - Hover tooltips with coordinates
 * - Professional dimension annotations
 * - Drop shadows for depth
 */
export const Hull2DProfile = forwardRef<SVGSVGElement, Hull2DProfileProps>(
  (
    {
      candidate,
      showButtocks = true,
      showWaterline = true,
      showDimensions = true,
      showBaseline = true,
      buttockCount = 5,
    },
    ref
  ) => {
    const [hoveredButtock, setHoveredButtock] = useState<number | null>(null);
    const [showDimensionsPanel, setShowDimensionsPanel] = useState(false);
    const [visibility, setVisibility] = useState({
      sheerline: true,
      buttocks: true,
      waterline: true,
      baseline: true,
      perpendiculars: true,
    });
    const [hoveredLegendItem, setHoveredLegendItem] = useState<string | null>(null);

    const { sizingStore } = useStore();

    // Check geometry generation status - don't use fallback if generation failed
    const geometryGenerationFailed =
      candidate.geometryGenerationStatus === "BothFailed" ||
      candidate.geometryGenerationStatus === "FormCoefficientFailed";

    // Generate buttocks - prioritize ShipD geometry if available
    const buttocks = useMemo(() => {
      // If geometry generation failed, return empty (will show error message)
      if (geometryGenerationFailed) {
        console.warn(
          "[Hull2DProfile] Geometry generation failed, not using fallback:",
          candidate.geometryGenerationError
        );
        return [];
      }

      const lpp = candidate.lppM;
      const beam = candidate.beamM;
      const draft = candidate.draftM;

      // Validate candidate dimensions early to prevent NaN propagation
      if (
        !Number.isFinite(lpp) ||
        lpp <= 0 ||
        !Number.isFinite(beam) ||
        beam <= 0 ||
        !Number.isFinite(draft) ||
        draft <= 0
      ) {
        console.warn("[Hull2DProfile] Invalid candidate dimensions, returning empty buttocks", {
          lpp,
          beam,
          draft,
        });
        return [];
      }

      // Check if geometry is available (OffsetsGrid or ShipD format from backend)
      if (candidate.geometryJson) {
        try {
          // Debug: Log geometryJson content to diagnose format detection issues
          console.log("[Hull2DProfile] Checking geometryJson:", {
            hasGeometryJson: !!candidate.geometryJson,
            geometryJsonLength: candidate.geometryJson?.length,
            geometryJsonPreview: candidate.geometryJson?.substring(0, 200),
            geometryGenerationStatus: candidate.geometryGenerationStatus,
          });

          // Normalize geometry to OffsetsGrid format (handles both ShipD and OffsetsGrid)
          const normalizedGeometry = normalizeGeometry(candidate.geometryJson);

          if (normalizedGeometry) {
            console.log("[Hull2DProfile] Using OffsetsGrid geometry from backend", {
              stationCount: normalizedGeometry.stations.length,
              waterlineCount: normalizedGeometry.waterlines.length,
            });

            // Generate buttock offsets
            const buttockOffsets = Array.from(
              { length: buttockCount + 1 },
              (_, i) => (i / buttockCount) * (beam / 2)
            );

            const result = extractButtocksFromOffsetsGrid(
              normalizedGeometry,
              lpp,
              draft,
              buttockOffsets
            );
            console.log("[Hull2DProfile] Extracted buttocks from OffsetsGrid geometry", {
              buttockCount: result.length,
              centerlineIndex: result.findIndex((b) => b.isCenterline),
            });
            return result;
          }

          // Fallback: Try ShipD format (legacy)
          const sections = JSON.parse(candidate.geometryJson) as {
            stations?: Array<{
              position: number;
              offsets: Record<number, number>;
            }>;
            stationPositions?: number[];
          };

          if (
            sections &&
            sections.stations &&
            Array.isArray(sections.stations) &&
            sections.stations.length > 0
          ) {
            console.log("[Hull2DProfile] Using ShipD geometry from backend (legacy)", {
              stationCount: sections.stations.length,
            });

            const shipdSections = {
              stations: sections.stations.map((s: ShipDHullStation) => ({
                position: s.position,
                offsets: s.offsets,
                hasBulb: s.hasBulb || false,
                bulbOffsets: s.bulbOffsets,
                hasSkeg: s.hasSkeg || false,
                skegOffsets: s.skegOffsets,
              })),
              stationPositions: sections.stationPositions || [],
            };

            // Generate buttock offsets
            const buttockOffsets = Array.from(
              { length: buttockCount + 1 },
              (_, i) => (i / buttockCount) * (beam / 2)
            );

            const result = extractButtocksFromShipD(shipdSections, lpp, draft, buttockOffsets);
            console.log("[Hull2DProfile] Extracted buttocks from ShipD geometry", {
              buttockCount: result.length,
              centerlineIndex: result.findIndex((b) => b.isCenterline),
            });
            return result;
          } else {
            // Debug log - this is expected when backend geometry hasn't been generated yet
            console.debug("[Hull2DProfile] Geometry has no stations, falling back");
          }
        } catch (error) {
          console.error(
            "[Hull2DProfile] Failed to parse geometry, falling back to parametric:",
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
            console.debug("[Hull2DProfile] Generating ShipD geometry from parameters", {
              hasMetadata: sizingStore.shipdParameters.length > 0,
            });

            const sections = generateShipDSections(
              {
                shipdVector,
                lppM: lpp,
                beamM: beam,
                draftM: draft,
                metadata: sizingStore.shipdParameters,
                resolution: 1.0,
              },
              20
            );

            // NURBS C² Continuity Diagnostic (development mode)
            // Analyze midship section for curvature smoothness
            if (process.env.NODE_ENV === "development" && sections.stations.length >= 4) {
              try {
                // Convert sections to control point grid for NURBS analysis
                const stationPositions = sections.stations.map((s) => s.position);
                const allHeights = new Set<number>();
                for (const station of sections.stations) {
                  for (const height of Object.keys(station.offsets).map(Number)) {
                    if (Number.isFinite(height)) {
                      allHeights.add(height);
                    }
                  }
                }
                const waterlines = Array.from(allHeights).sort((a, b) => a - b);

                if (waterlines.length > 0 && stationPositions.length > 0) {
                  // Create offsets grid
                  const offsets: number[][] = [];
                  for (let sIdx = 0; sIdx < stationPositions.length; sIdx++) {
                    const station = sections.stations[sIdx];
                    const stationOffsets: number[] = [];
                    for (let wIdx = 0; wIdx < waterlines.length; wIdx++) {
                      const height = waterlines[wIdx];
                      const halfBreadth = station.offsets[height] ?? 0;
                      stationOffsets.push(Math.max(0, halfBreadth));
                    }
                    offsets.push(stationOffsets);
                  }

                  // Normalize stations and waterlines to [0, 1] range
                  const stationMin = Math.min(...stationPositions);
                  const stationMax = Math.max(...stationPositions);
                  const stationRange = stationMax - stationMin || 1;
                  const normalizedStations = stationPositions.map(
                    (s) => (s - stationMin) / stationRange
                  );

                  const waterlineMin = Math.min(...waterlines);
                  const waterlineMax = Math.max(...waterlines);
                  const waterlineRange = waterlineMax - waterlineMin || 1;
                  const normalizedWaterlines = waterlines.map(
                    (w) => (w - waterlineMin) / waterlineRange
                  );

                  // Generate control point grid
                  const controlPointGrid = generateControlPointGridFromOffsets(
                    normalizedStations,
                    normalizedWaterlines,
                    offsets,
                    lpp,
                    beam,
                    draft
                  );

                  // Analyze midship section (approximately middle station)
                  const midshipIndex = Math.floor(controlPointGrid.numStations / 2);
                  const diagnostic = diagnoseHullSection(controlPointGrid, midshipIndex, 100);

                  // Log diagnostic results
                  console.group("[Hull2DProfile] NURBS C² Continuity Diagnostic - Midship Section");
                  console.log("Station Index:", diagnostic.stationIndex);
                  console.log("C² Continuity:", {
                    isContinuous: diagnostic.c2Continuity.isContinuous,
                    discontinuities: diagnostic.c2Continuity.discontinuities.length,
                    maxDiscontinuity: diagnostic.c2Continuity.maxDiscontinuity,
                  });
                  console.log("Curvature Statistics:", {
                    min: diagnostic.curvatureStats.min.toFixed(4),
                    max: diagnostic.curvatureStats.max.toFixed(4),
                    mean: diagnostic.curvatureStats.mean.toFixed(4),
                    stdDev: diagnostic.curvatureStats.stdDev.toFixed(4),
                    oscillatoryRegions: diagnostic.curvatureStats.oscillatoryRegions.length,
                  });
                  console.log("Recommendations:", diagnostic.recommendations);
                  if (diagnostic.c2Continuity.discontinuities.length > 0) {
                    console.warn(
                      "C² Discontinuities detected:",
                      diagnostic.c2Continuity.discontinuities
                    );
                  }
                  console.groupEnd();
                }
              } catch (error) {
                console.warn("[Hull2DProfile] NURBS diagnostic failed:", error);
              }
            }

            const buttockOffsets = Array.from(
              { length: buttockCount + 1 },
              (_, i) => (i / buttockCount) * (beam / 2)
            );

            const result = extractButtocksFromShipD(sections, lpp, draft, buttockOffsets);
            // Debug log - called repeatedly during optimization
            console.debug("[Hull2DProfile] Generated buttocks from ShipD parameters", {
              buttockCount: result.length,
            });
            return result;
          } else {
            console.warn("[Hull2DProfile] Invalid ShipD vector format", {
              isArray: Array.isArray(shipdVector),
              length: Array.isArray(shipdVector) ? shipdVector.length : 0,
            });
          }
        } catch (error) {
          console.error(
            "[Hull2DProfile] Failed to generate ShipD geometry, falling back to parametric:",
            error
          );
        }
      } else {
        console.log("[Hull2DProfile] No ShipD data available", {
          hasParametersJson: !!candidate.shipdParametersJson,
          hasMetadata: !!(sizingStore.shipdParameters && sizingStore.shipdParameters.length > 0),
        });
      }

      // Fallback: Generate using FormCoefficientHullGenerator (solver logic)
      // This ensures non-isometric geometry matching solver output
      try {
        const dims: HullDimensions = {
          length: lpp,
          beam,
          draft,
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

        // Generate buttock offsets
        const buttockOffsets = Array.from(
          { length: buttockCount + 1 },
          (_, i) => (i / buttockCount) * (beam / 2)
        );

        // Extract buttocks from generated OffsetsGrid
        const result = extractButtocksFromOffsetsGrid(
          {
            stations: generated.stations,
            waterlines: generated.waterlines,
            offsets: generated.offsets,
          },
          lpp,
          draft,
          buttockOffsets
        );

        console.log("[Hull2DProfile] Generated buttocks using FormCoefficientHullGenerator", {
          buttockCount: result.length,
        });

        return result;
      } catch (error) {
        console.error(
          "[Hull2DProfile] Failed to generate geometry using FormCoefficientHullGenerator:",
          error
        );
        // Return empty - will show error message
        return [];
      }
    }, [
      geometryGenerationFailed,
      candidate.geometryGenerationError,
      candidate.lppM,
      candidate.beamM,
      candidate.draftM,
      candidate.cb,
      candidate.cp,
      candidate.cm,
      candidate.cwp,
      candidate.lcbPctLpp,
      candidate.bowFamily,
      candidate.midshipFamily,
      candidate.sternFamily,
      candidate.vesselType,
      candidate.geometryGenerationStatus,
      candidate.geometryJson,
      candidate.shipdParametersJson,
      sizingStore.shipdParameters,
      sizingStore.currentRun?.vesselType,
      buttockCount,
    ]);

    // Generate sheerline - prioritize OffsetsGrid geometry if available
    const sheerline = useMemo(() => {
      const lpp = candidate.lppM;
      const depth = candidate.depthM;
      const draft = candidate.draftM;

      // Check if geometry is available (OffsetsGrid or ShipD format)
      if (candidate.geometryJson) {
        try {
          // Normalize geometry to OffsetsGrid format (handles both ShipD and OffsetsGrid)
          const normalizedGeometry = normalizeGeometry(candidate.geometryJson);

          if (normalizedGeometry) {
            console.log("[Hull2DProfile] Using OffsetsGrid geometry for sheerline", {
              stationCount: normalizedGeometry.stations.length,
            });
            return extractSheerlineFromOffsetsGrid(normalizedGeometry, lpp, depth, draft);
          }

          // Fallback: Try ShipD format (legacy)
          const sections = JSON.parse(candidate.geometryJson) as {
            stations?: Array<{
              position: number;
              offsets: Record<number, number>;
            }>;
          };

          if (sections && sections.stations && Array.isArray(sections.stations)) {
            const shipdSections = {
              stations: sections.stations.map((s) => ({
                position: s.position,
                offsets: s.offsets,
                hasBulb: false,
                bulbOffsets: undefined,
              })),
              stationPositions: [],
            };

            return extractSheerlineFromShipD(shipdSections, lpp, depth, draft);
          }
        } catch (error) {
          console.warn("[Hull2DProfile] Failed to parse geometry for sheerline:", error);
        }
      }

      // Fallback: Generate using FormCoefficientHullGenerator (solver logic)
      try {
        const dims: HullDimensions = {
          length: lpp,
          beam: candidate.beamM ?? 30,
          draft,
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

        // Extract sheerline from generated OffsetsGrid
        const result = extractSheerlineFromOffsetsGrid(
          {
            stations: generated.stations,
            waterlines: generated.waterlines,
            offsets: generated.offsets,
          },
          lpp,
          depth,
          draft
        );

        console.log("[Hull2DProfile] Generated sheerline using FormCoefficientHullGenerator");

        return result;
      } catch (error) {
        console.error(
          "[Hull2DProfile] Failed to generate sheerline using FormCoefficientHullGenerator:",
          error
        );
        // Return empty - will show error message
        return [];
      }
    }, [
      candidate.lppM,
      candidate.depthM,
      candidate.draftM,
      candidate.beamM,
      candidate.cb,
      candidate.cp,
      candidate.cm,
      candidate.cwp,
      candidate.lcbPctLpp,
      candidate.bowFamily,
      candidate.midshipFamily,
      candidate.sternFamily,
      candidate.vesselType,
      candidate.geometryJson,
      sizingStore.currentRun?.vesselType,
    ]);

    const padding = 80;
    const svgWidth = 900;
    const svgHeight = 520;
    const lpp = candidate.lppM ?? 100; // Fallback to 100m if undefined
    const depth = candidate.depthM ?? 10; // Fallback to 10m if undefined

    // Validate values to prevent NaN
    if (!Number.isFinite(lpp) || lpp <= 0) {
      console.warn("[Hull2DProfile] Invalid Lpp value:", candidate.lppM);
      return (
        <div className="w-full h-full flex items-center justify-center text-gray-500">
          Invalid vessel dimensions (Lpp: {String(candidate.lppM)})
        </div>
      );
    }
    if (!Number.isFinite(depth) || depth <= 0) {
      console.warn("[Hull2DProfile] Invalid Depth value:", candidate.depthM);
      return (
        <div className="w-full h-full flex items-center justify-center text-gray-500">
          Invalid vessel dimensions (Depth: {String(candidate.depthM)})
        </div>
      );
    }

    const scaleX = (svgWidth - 2 * padding) / lpp;
    const scaleY = (svgHeight - 2 * padding) / depth;
    const scale = Math.min(scaleX, scaleY);

    // Validate scale to prevent NaN
    if (!Number.isFinite(scale) || scale <= 0) {
      console.warn("[Hull2DProfile] Invalid scale calculated:", { scaleX, scaleY, lpp, depth });
      return (
        <div className="w-full h-full flex items-center justify-center text-gray-500">
          Unable to calculate view scale
        </div>
      );
    }

    const toSVG = (x: number, y: number): [number, number] => [
      svgWidth / 2 + x * scale,
      svgHeight - padding - (y + candidate.draftM) * scale,
    ];

    const generatePath = (points: [number, number][]) => {
      // Filter out invalid points (NaN, Infinity, etc.)
      const validPoints = points.filter(([x, y]) => Number.isFinite(x) && Number.isFinite(y));

      if (validPoints.length === 0) {
        console.warn("[Hull2DProfile] No valid points for path");
        return "";
      }

      // Use spline interpolation for smooth curves
      // Convert to format expected by spline utility (x = x, y = y)
      const splinePoints = validPoints.map(([x, y]) => ({ x, y }));

      // Interpolate for smoothness (120 points for improved smoothness without discretization artifacts)
      const interpolated = generateSmoothCurve(splinePoints, 120);

      // Convert back and generate SVG path
      return interpolated
        .map((p, i) => {
          const [svgX, svgY] = toSVG(p.x, p.y);
          // Validate coordinates before formatting
          if (!Number.isFinite(svgX) || !Number.isFinite(svgY)) {
            console.warn("[Hull2DProfile] Invalid SVG coordinate:", { svgX, svgY, x: p.x, y: p.y });
            return "";
          }
          return `${i === 0 ? "M" : "L"} ${svgX.toFixed(2)},${svgY.toFixed(2)}`;
        })
        .filter((segment) => segment !== "")
        .join(" ");
    };

    // Show error message if geometry generation failed
    if (geometryGenerationFailed) {
      return (
        <div className="w-full h-full p-4 relative flex flex-col">
          <div className="flex-1 bg-gradient-to-b from-sky-50 via-white to-blue-50 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900 border border-gray-300 dark:border-gray-600 rounded-lg shadow-inner flex flex-col items-center justify-center">
            <div className="rounded-lg border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20 shadow-lg max-w-md">
              <div className="p-6">
                <h3 className="text-red-800 dark:text-red-200 font-bold flex items-center gap-2 mb-2">
                  Geometry Generation Failed
                </h3>
                <p className="text-red-700 dark:text-red-300 text-sm mb-3">
                  Unable to generate hull geometry for this candidate. The profile view cannot be
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
        <div className="flex-1 bg-gradient-to-b from-sky-100 via-blue-50 to-cyan-100 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900 border border-gray-300 dark:border-gray-600 rounded-t-lg shadow-lg flex flex-col">
          <svg
            ref={ref}
            width="100%"
            height="100%"
            viewBox={`0 0 ${svgWidth} ${svgHeight}`}
            className="flex-1"
          >
            <defs>
              <linearGradient id="skyGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                <stop offset="0%" stopColor="#e0f2fe" stopOpacity="0.3" />
                <stop offset="60%" stopColor="#ffffff" stopOpacity="0.1" />
                <stop offset="100%" stopColor="#cffafe" stopOpacity="0.2" />
              </linearGradient>
              <filter id="glow">
                <feGaussianBlur stdDeviation="2" result="coloredBlur" />
                <feMerge>
                  <feMergeNode in="coloredBlur" />
                  <feMergeNode in="SourceGraphic" />
                </feMerge>
              </filter>
              <filter id="shadow">
                <feDropShadow dx="0" dy="2" stdDeviation="3" floodOpacity="0.3" />
              </filter>
            </defs>

            <rect x="0" y="0" width={svgWidth} height={svgHeight} fill="url(#skyGradient)" />

            <text
              x={svgWidth / 2}
              y={25}
              textAnchor="middle"
              className="fill-gray-900 dark:fill-gray-100 font-bold"
              style={{ fontSize: "14px", letterSpacing: "0.5px" }}
            >
              PROFILE VIEW (SIDE ELEVATION)
            </text>
            <text
              x={svgWidth / 2}
              y={40}
              textAnchor="middle"
              className="fill-gray-600 dark:fill-gray-400"
              style={{ fontSize: "11px" }}
            >
              {candidate.hullFamily.replace("_", " ").toUpperCase()} · T{" "}
              {candidate.draftM.toFixed(2)}m × D {candidate.depthM.toFixed(2)}m
              {candidate.geometryJson && (
                <tspan className="fill-green-600 dark:fill-green-400" style={{ fontSize: "10px" }}>
                  {" "}
                  · Hull Form Parameters
                </tspan>
              )}
            </text>

            {/* Baseline */}
            {showBaseline && visibility.baseline && (
              <line
                x1={toSVG(-lpp / 2, -candidate.draftM)[0]}
                y1={toSVG(-lpp / 2, -candidate.draftM)[1]}
                x2={toSVG(lpp / 2, -candidate.draftM)[0]}
                y2={toSVG(lpp / 2, -candidate.draftM)[1]}
                stroke="#6b7280"
                strokeWidth="2"
                strokeDasharray="10,5"
                opacity={hoveredLegendItem && hoveredLegendItem !== "baseline" ? 0.3 : 1}
                style={{ transition: "all 0.3s ease" }}
              />
            )}

            {/* Perpendiculars */}
            {visibility.perpendiculars && (
              <>
                {/* Aft Perpendicular (AP) - STERN */}
                <line
                  x1={toSVG(-lpp / 2, -candidate.draftM)[0]}
                  y1={toSVG(-lpp / 2, -candidate.draftM)[1]}
                  x2={toSVG(-lpp / 2, candidate.depthM - candidate.draftM)[0]}
                  y2={toSVG(-lpp / 2, candidate.depthM - candidate.draftM)[1]}
                  stroke="#ef4444"
                  strokeWidth="3"
                  filter="url(#shadow)"
                  opacity={hoveredLegendItem && hoveredLegendItem !== "perpendiculars" ? 0.3 : 1}
                  style={{ transition: "all 0.3s ease" }}
                />
                {/* STERN label at AP */}
                <text
                  x={
                    toSVG(
                      -lpp / 2,
                      candidate.depthM - candidate.draftM + candidate.depthM * 0.15
                    )[0]
                  }
                  y={
                    toSVG(
                      -lpp / 2,
                      candidate.depthM - candidate.draftM + candidate.depthM * 0.15
                    )[1]
                  }
                  textAnchor="middle"
                  className="text-xs fill-red-500 font-semibold"
                  filter="url(#shadow)"
                  opacity={hoveredLegendItem && hoveredLegendItem !== "perpendiculars" ? 0.3 : 1}
                  style={{ transition: "all 0.3s ease" }}
                >
                  STERN (AP)
                </text>

                {/* Forward Perpendicular (FP) - BOW */}
                <line
                  x1={toSVG(lpp / 2, -candidate.draftM)[0]}
                  y1={toSVG(lpp / 2, -candidate.draftM)[1]}
                  x2={toSVG(lpp / 2, candidate.depthM - candidate.draftM)[0]}
                  y2={toSVG(lpp / 2, candidate.depthM - candidate.draftM)[1]}
                  stroke="#10b981"
                  strokeWidth="3"
                  filter="url(#shadow)"
                  opacity={hoveredLegendItem && hoveredLegendItem !== "perpendiculars" ? 0.3 : 1}
                  style={{ transition: "all 0.3s ease" }}
                />
                {/* BOW label at FP */}
                <text
                  x={
                    toSVG(lpp / 2, candidate.depthM - candidate.draftM + candidate.depthM * 0.15)[0]
                  }
                  y={
                    toSVG(lpp / 2, candidate.depthM - candidate.draftM + candidate.depthM * 0.15)[1]
                  }
                  textAnchor="middle"
                  className="text-xs fill-green-500 font-semibold"
                  filter="url(#shadow)"
                  opacity={hoveredLegendItem && hoveredLegendItem !== "perpendiculars" ? 0.3 : 1}
                  style={{ transition: "all 0.3s ease" }}
                >
                  BOW (FP)
                </text>
              </>
            )}

            {/* Waterline */}
            {showWaterline && visibility.waterline && (
              <line
                x1={toSVG(-lpp / 2, 0)[0]}
                y1={toSVG(-lpp / 2, 0)[1]}
                x2={toSVG(lpp / 2, 0)[0]}
                y2={toSVG(lpp / 2, 0)[1]}
                stroke="#06b6d4"
                strokeWidth="2.5"
                strokeDasharray="10,5"
                filter="url(#glow)"
                opacity={hoveredLegendItem && hoveredLegendItem !== "waterline" ? 0.3 : 1}
                style={{ transition: "all 0.3s ease" }}
              />
            )}

            {/* Sheerline (deck) with gradient */}
            {visibility.sheerline && (
              <path
                d={generatePath(sheerline)}
                fill="none"
                stroke="url(#deckGradient)"
                strokeWidth="3.5"
                strokeLinecap="round"
                filter="url(#shadow)"
                opacity={hoveredLegendItem && hoveredLegendItem !== "sheerline" ? 0.3 : 1}
                style={{ transition: "all 0.3s ease" }}
              />
            )}

            <defs>
              <linearGradient id="deckGradient" x1="0%" y1="0%" x2="100%" y2="0%">
                <stop offset="0%" stopColor="#1f2937" />
                <stop offset="50%" stopColor="#374151" />
                <stop offset="100%" stopColor="#1f2937" />
              </linearGradient>
            </defs>

            {/* Buttocks with animations */}
            {showButtocks &&
              visibility.buttocks &&
              buttocks.map((buttock, idx) => {
                const isHovered = hoveredButtock === idx;
                const color = buttock.isCenterline ? "#3b82f6" : isHovered ? "#60a5fa" : "#93c5fd";
                const strokeWidth = buttock.isCenterline ? 2.5 : isHovered ? 2 : 1.2;
                const dimmed = hoveredLegendItem && hoveredLegendItem !== "buttocks";

                return (
                  <g
                    key={idx}
                    onMouseEnter={() => setHoveredButtock(idx)}
                    onMouseLeave={() => setHoveredButtock(null)}
                    style={{
                      opacity: dimmed ? 0.3 : 0,
                      animation: `fadeIn 0.4s ease-in forwards ${idx * 0.1}s`,
                      cursor: "pointer",
                    }}
                  >
                    <path
                      d={generatePath(buttock.points)}
                      fill="none"
                      stroke={color}
                      strokeWidth={strokeWidth}
                      strokeLinecap="round"
                      filter={buttock.isCenterline ? "url(#shadow)" : undefined}
                      style={{ transition: "all 0.3s ease" }}
                    />

                    {(idx % 2 === 0 || isHovered) && (
                      <g>
                        <rect
                          x={toSVG(lpp / 2 + 4, buttock.points[buttock.points.length - 1][1])[0]}
                          y={toSVG(lpp / 2, buttock.points[buttock.points.length - 1][1])[1] - 9}
                          width="60"
                          height="14"
                          rx="2"
                          fill={isHovered ? "#3b82f6" : "#ffffff"}
                          opacity="0.95"
                          stroke={color}
                          strokeWidth="0.5"
                        />
                        <text
                          x={toSVG(lpp / 2 + 34, 0)[0]}
                          y={toSVG(lpp / 2, buttock.points[buttock.points.length - 1][1])[1] + 1}
                          className={isHovered ? "fill-white font-semibold" : "fill-blue-600"}
                          style={{ fontSize: "9px" }}
                        >
                          BL{idx} {buttock.transverseOffset.toFixed(1)}m
                        </text>
                      </g>
                    )}
                  </g>
                );
              })}

            {/* Dimensions with professional styling */}
            {showDimensions && (
              <>
                <defs>
                  <marker
                    id="arrow-prof-l"
                    markerWidth="10"
                    markerHeight="7"
                    refX="0"
                    refY="3.5"
                    orient="auto"
                  >
                    <polygon points="10 0, 10 7, 0 3.5" fill="#374151" />
                  </marker>
                  <marker
                    id="arrow-prof-r"
                    markerWidth="10"
                    markerHeight="7"
                    refX="10"
                    refY="3.5"
                    orient="auto"
                  >
                    <polygon points="0 0, 10 3.5, 0 7" fill="#374151" />
                  </marker>
                </defs>

                {/* Lpp */}
                <line
                  x1={toSVG(-lpp / 2, -candidate.draftM - 5)[0]}
                  y1={toSVG(-lpp / 2, -candidate.draftM - 5)[1]}
                  x2={toSVG(lpp / 2, -candidate.draftM - 5)[0]}
                  y2={toSVG(lpp / 2, -candidate.draftM - 5)[1]}
                  stroke="#374151"
                  strokeWidth="1.2"
                  markerStart="url(#arrow-prof-l)"
                  markerEnd="url(#arrow-prof-r)"
                />
                <rect
                  x={svgWidth / 2 - 50}
                  y={toSVG(0, -candidate.draftM - 5)[1] + 8}
                  width="100"
                  height="18"
                  rx="4"
                  fill="#ffffff"
                  stroke="#3b82f6"
                  strokeWidth="1.5"
                  filter="url(#shadow)"
                />
                <text
                  x={svgWidth / 2}
                  y={toSVG(0, -candidate.draftM - 5)[1] + 20}
                  textAnchor="middle"
                  className="fill-gray-900 font-bold"
                  style={{ fontSize: "11px" }}
                >
                  Lpp = {lpp.toFixed(2)} m
                </text>

                {/* Draft */}
                <line
                  x1={toSVG(-lpp / 2 - 10, -candidate.draftM)[0]}
                  y1={toSVG(-lpp / 2 - 10, -candidate.draftM)[1]}
                  x2={toSVG(-lpp / 2 - 10, 0)[0]}
                  y2={toSVG(-lpp / 2 - 10, 0)[1]}
                  stroke="#374151"
                  strokeWidth="1.2"
                  markerStart="url(#arrow-prof-l)"
                  markerEnd="url(#arrow-prof-r)"
                />
                <rect
                  x={toSVG(-lpp / 2 - 10, -candidate.draftM / 2)[0] - 50}
                  y={toSVG(-lpp / 2 - 10, -candidate.draftM / 2)[1] - 9}
                  width="70"
                  height="18"
                  rx="4"
                  fill="#ffffff"
                  stroke="#06b6d4"
                  strokeWidth="1.5"
                  filter="url(#shadow)"
                />
                <text
                  x={toSVG(-lpp / 2 - 10, -candidate.draftM / 2)[0] - 15}
                  y={toSVG(-lpp / 2 - 10, -candidate.draftM / 2)[1] + 3}
                  textAnchor="middle"
                  className="fill-cyan-700 dark:fill-cyan-400 font-bold"
                  style={{ fontSize: "10px" }}
                >
                  T = {candidate.draftM.toFixed(2)}m
                </text>

                {/* Depth */}
                <line
                  x1={toSVG(-lpp / 2 - 22, -candidate.draftM)[0]}
                  y1={toSVG(-lpp / 2 - 22, -candidate.draftM)[1]}
                  x2={toSVG(-lpp / 2 - 22, candidate.depthM - candidate.draftM)[0]}
                  y2={toSVG(-lpp / 2 - 22, candidate.depthM - candidate.draftM)[1]}
                  stroke="#374151"
                  strokeWidth="1.2"
                  markerStart="url(#arrow-prof-l)"
                  markerEnd="url(#arrow-prof-r)"
                />
                <rect
                  x={
                    toSVG(
                      -lpp / 2 - 22,
                      (candidate.depthM - candidate.draftM - candidate.draftM) / 2
                    )[0] - 50
                  }
                  y={
                    toSVG(
                      -lpp / 2 - 22,
                      (candidate.depthM - candidate.draftM - candidate.draftM) / 2
                    )[1] - 9
                  }
                  width="70"
                  height="18"
                  rx="4"
                  fill="#ffffff"
                  stroke="#8b5cf6"
                  strokeWidth="1.5"
                  filter="url(#shadow)"
                />
                <text
                  x={
                    toSVG(
                      -lpp / 2 - 22,
                      (candidate.depthM - candidate.draftM - candidate.draftM) / 2
                    )[0] - 15
                  }
                  y={
                    toSVG(
                      -lpp / 2 - 22,
                      (candidate.depthM - candidate.draftM - candidate.draftM) / 2
                    )[1] + 3
                  }
                  textAnchor="middle"
                  className="fill-purple-700 dark:fill-purple-400 font-bold"
                  style={{ fontSize: "10px" }}
                >
                  D = {candidate.depthM.toFixed(2)}m
                </text>
              </>
            )}

            <text x={20} y={svgHeight - 10} className="fill-gray-500" style={{ fontSize: "10px" }}>
              Scale 1:{scale > 0 && Number.isFinite(scale) ? Math.round(1 / scale) : "N/A"}
            </text>
          </svg>
        </div>

        {/* Integrated Info Bar - Part of the same panel */}
        <div className="bg-white dark:bg-gray-800 border-t-0 border-l border-r border-b border-gray-300 dark:border-gray-600 rounded-b-lg shadow-lg p-3 flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
          {/* Left: Interactive legend items */}
          <div className="flex items-center gap-3 flex-wrap">
            <button
              onClick={() => setVisibility((prev) => ({ ...prev, sheerline: !prev.sheerline }))}
              onMouseEnter={() => setHoveredLegendItem("sheerline")}
              onMouseLeave={() => setHoveredLegendItem(null)}
              className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all text-xs font-medium ${
                visibility.sheerline
                  ? "bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300"
                  : "bg-gray-200/50 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 line-through"
              }`}
            >
              <div className="w-5 h-1 bg-gradient-to-r from-gray-800 to-gray-600 rounded"></div>
              <span>Sheerline</span>
            </button>

            <button
              onClick={() => setVisibility((prev) => ({ ...prev, buttocks: !prev.buttocks }))}
              onMouseEnter={() => setHoveredLegendItem("buttocks")}
              onMouseLeave={() => setHoveredLegendItem(null)}
              className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all text-xs font-medium ${
                visibility.buttocks
                  ? "bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300"
                  : "bg-gray-200/50 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 line-through"
              }`}
            >
              <div className="w-5 h-0.5 bg-blue-400"></div>
              <span>Buttocks</span>
            </button>

            <button
              onClick={() => setVisibility((prev) => ({ ...prev, waterline: !prev.waterline }))}
              onMouseEnter={() => setHoveredLegendItem("waterline")}
              onMouseLeave={() => setHoveredLegendItem(null)}
              className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all text-xs font-medium ${
                visibility.waterline
                  ? "bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300"
                  : "bg-gray-200/50 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 line-through"
              }`}
            >
              <div className="w-5 h-0.5 bg-cyan-500"></div>
              <span>Waterline</span>
            </button>

            <button
              onClick={() => setVisibility((prev) => ({ ...prev, baseline: !prev.baseline }))}
              onMouseEnter={() => setHoveredLegendItem("baseline")}
              onMouseLeave={() => setHoveredLegendItem(null)}
              className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all text-xs font-medium ${
                visibility.baseline
                  ? "bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300"
                  : "bg-gray-200/50 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 line-through"
              }`}
            >
              <div className="w-5 h-0.5 bg-gray-500"></div>
              <span>Baseline</span>
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
          </div>

          {/* Separator */}
          <div className="hidden md:block h-6 w-px bg-border"></div>

          {/* Right: Dimensions dropdown */}
          <div className="relative">
            <button
              onClick={() => setShowDimensionsPanel(!showDimensionsPanel)}
              className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 rounded-lg shadow-sm px-3 py-1.5 text-xs font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors flex items-center gap-1.5"
            >
              {showDimensionsPanel ? "▼" : "▶"} Dimensions
            </button>
            {showDimensionsPanel && (
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
                  <span className="text-gray-600 dark:text-gray-400">Draft:</span>
                  <span className="font-semibold text-cyan-700 dark:text-cyan-400">
                    {candidate.draftM.toFixed(2)} m
                  </span>
                  <span className="text-gray-600 dark:text-gray-400">Depth:</span>
                  <span className="font-semibold text-purple-700 dark:text-purple-400">
                    {candidate.depthM.toFixed(2)} m
                  </span>
                  <span className="text-gray-600 dark:text-gray-400">Freeboard:</span>
                  <span className="font-semibold text-green-700 dark:text-green-400">
                    {(candidate.depthM - candidate.draftM).toFixed(2)} m
                  </span>
                </div>
              </div>
            )}
          </div>
        </div>

        <style>{`
        @keyframes fadeIn {
          from { opacity: 0; transform: translateX(-10px); }
          to { opacity: 1; transform: translateX(0); }
        }
      `}</style>
      </div>
    );
  }
);

Hull2DProfile.displayName = "Hull2DProfile";
