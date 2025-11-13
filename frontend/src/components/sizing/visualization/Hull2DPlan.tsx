import { useMemo, useState, forwardRef } from "react";
import type { CandidateDesign } from "../../../types/sizing";
import { generateHullWaterlines } from "../../../utils/hullShapeGenerator";
import { extractWaterlinesFromShipD } from "../../../utils/shipd2DGeometry";
import { generateShipDSections } from "../../../utils/shipdGeometryGenerator";
import { useStore } from "../../../stores";

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

    // Generate waterlines - prioritize ShipD geometry if available
    const waterlines = useMemo(() => {
      // Check if ShipD geometry is available (from backend)
      if (candidate.geometryJson) {
        try {
          const sections = JSON.parse(candidate.geometryJson) as {
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
              candidate.lppM,
              waterlineHeights,
              candidate.draftM
            );
            console.log("[Hull2DPlan] Extracted waterlines from ShipD geometry", {
              waterlineCount: result.length,
              designWaterlineIndex: result.findIndex((wl) => wl.isDesignWaterline),
            });
            return result;
          } else {
            console.warn("[Hull2DPlan] ShipD geometry has no stations, falling back");
          }
        } catch (error) {
          console.error(
            "[Hull2DPlan] Failed to parse ShipD geometry, falling back to parametric:",
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
            console.log("[Hull2DPlan] Generating ShipD geometry from parameters", {
              hasMetadata: sizingStore.shipdParameters.length > 0,
            });

            const sections = generateShipDSections(
              {
                shipdVector,
                lppM: candidate.lppM,
                beamM: candidate.beamM,
                draftM: candidate.draftM,
                metadata: sizingStore.shipdParameters,
                resolution: 1.0,
              },
              20
            );

            // Heights should be from 0 (keel) to draft (waterline)
            const maxDraft = candidate.draftM || 5;
            const waterlineHeights = Array.from(
              { length: waterlineCount },
              (_, i) => (i / (waterlineCount - 1)) * maxDraft
            );

            const result = extractWaterlinesFromShipD(
              sections,
              candidate.lppM,
              waterlineHeights,
              candidate.draftM
            );
            console.log("[Hull2DPlan] Generated waterlines from ShipD parameters", {
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

      // Fallback: Use vessel-type-specific waterline generator
      const generatedWaterlines = generateHullWaterlines({
        hullFamily: candidate.hullFamily,
        lppM: candidate.lppM,
        beamM: candidate.beamM,
        draftM: candidate.draftM,
        cb: candidate.cb,
        cp: candidate.cp,
        cwp: candidate.cwp,
        cm: candidate.cm,
        lcbPctLpp: candidate.lcbPctLpp,
        waterlineCount,
        pointsPerWaterline: 60,
      });

      // Convert to format expected by rendering code
      return generatedWaterlines.map((wl) => ({
        depth: wl.depth,
        points: wl.points.map((pt) => [pt.x, pt.y] as [number, number]),
        isDesignWaterline: wl.isDesignWaterline,
      }));
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
      candidate.geometryJson,
      candidate.shipdParametersJson,
      sizingStore.shipdParameters,
      waterlineCount,
    ]);

    // Generate stations - use ShipD geometry if available
    const stations = useMemo(() => {
      const lpp = candidate.lppM;

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
    const lpp = candidate.lppM;
    const beam = candidate.beamM;
    const scaleX = (svgWidth - 2 * padding) / lpp;
    const scaleY = (svgHeight - 2 * padding) / beam;
    const scale = Math.min(scaleX, scaleY);

    const toSVG = (x: number, y: number): [number, number] => [
      svgWidth / 2 + x * scale,
      svgHeight / 2 - y * scale,
    ];

    const waterlinePath = (points: [number, number][]) => {
      // Naval architecture Plan View: waterlines should be closed curves
      // Path: stern centerline → starboard side → bow centerline → port side → stern centerline

      // Starboard side: points from stern to bow (x from -lpp/2 to +lpp/2, y >= 0)
      const starboardPoints = points.map(([x, y]) => toSVG(x, y));
      const starboardPath = starboardPoints
        .map(([x, y], i) => `${i === 0 ? "M" : "L"} ${x.toFixed(2)},${y.toFixed(2)}`)
        .join(" ");

      // Port side: reverse the points and negate y to create closed loop
      // We reverse so the path goes: bow centerline → port side → stern centerline
      const portPoints = [...points].reverse().map(([x, y]) => toSVG(x, -y));
      const portPath = portPoints
        .map(([x, y], i) => `${i === 0 ? "L" : "L"} ${x.toFixed(2)},${y.toFixed(2)}`)
        .join(" ");

      // Close the path by connecting back to stern centerline
      const closedPath = `${starboardPath} ${portPath} Z`;

      return { starboard: starboardPath, port: portPath, closed: closedPath };
    };

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
                  · ShipD
                </tspan>
              )}
            </text>

            {/* Centerline */}
            {showCenterline && visibility.centerline && (
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
            {showStations && visibility.perpendiculars && (
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
              waterlines.map((wl, idx) => {
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
              })}

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
