import { useMemo, useState, forwardRef } from "react";
import type { CandidateDesign } from "../../../types/sizing";
import { extractSectionsFromShipD } from "../../../utils/shipd2DGeometry";
import { useStore } from "../../../stores";

interface Hull2DSectionsProps {
  candidate: CandidateDesign;
  showCenterline?: boolean;
  showWaterline?: boolean;
  showBaseline?: boolean;
  showDimensions?: boolean;
  stationCount?: number;
}

/**
 * 2D Sections View (Body Plan) - Traditional Naval Architecture
 *
 * Professional body plan with:
 * - Aft sections (0-4) mirrored to LEFT
 * - Forward sections (6-10) on RIGHT
 * - Midship (5) shown on BOTH sides
 * - Gradient backgrounds and animations
 * - Hover effects and tooltips
 * - Engineering drawing aesthetic
 */
export const Hull2DSections = forwardRef<SVGSVGElement, Hull2DSectionsProps>(
  (
    {
      candidate,
      showCenterline = true,
      showWaterline = true,
      showBaseline = true,
      showDimensions = true,
      stationCount = 10,
    },
    ref
  ) => {
    const [hoveredSection, setHoveredSection] = useState<number | null>(null);
    const [showCoefficients, setShowCoefficients] = useState(false);
    const [visibility, setVisibility] = useState({
      midship: true,
      sections: true,
      waterline: true,
      baseline: true,
      centerline: true,
    });
    const [hoveredLegendItem, setHoveredLegendItem] = useState<string | null>(null);

    const { sizingStore } = useStore();

    // Generate sections - prioritize ShipD geometry if available
    const sections = useMemo(() => {
      const beam = candidate.beamM;
      const draft = candidate.draftM;

      // Check if ShipD geometry is available (from backend)
      if (candidate.geometryJson) {
        try {
          const sectionsData = JSON.parse(candidate.geometryJson) as {
            stations?: Array<{
              position: number;
              offsets: Record<number, number>;
              hasBulb?: boolean;
              bulbOffsets?: Record<number, number>;
            }>;
            stationPositions?: number[];
          };

          if (sectionsData && sectionsData.stations && Array.isArray(sectionsData.stations) && sectionsData.stations.length > 0) {
            console.log("[Hull2DSections] Using ShipD geometry from backend", {
              stationCount: sectionsData.stations.length,
              hasBulb: sectionsData.stations.some((s) => s.hasBulb),
            });

            const shipdSections = {
              stations: sectionsData.stations.map((s) => ({
                position: s.position,
                offsets: s.offsets,
                hasBulb: s.hasBulb || false,
                bulbOffsets: s.bulbOffsets,
              })),
              stationPositions: sectionsData.stationPositions || [],
            };

            // Extract sections for body plan (use all stations or sample)
            const stationIndices = Array.from({ length: stationCount + 1 }, (_, i) => i);
            const result = extractSectionsFromShipD(shipdSections, stationIndices);
            console.log("[Hull2DSections] Extracted sections from ShipD geometry", {
              sectionCount: result.length,
              hasBulb: result.some((s) => s.hasBulb),
            });
            return result;
          } else {
            console.warn("[Hull2DSections] ShipD geometry has no stations, falling back");
          }
        } catch (error) {
          console.error("[Hull2DSections] Failed to parse ShipD geometry, falling back to parametric:", error);
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
            console.log("[Hull2DSections] Generating ShipD geometry from parameters", {
              hasMetadata: sizingStore.shipdParameters.length > 0,
            });

            const { generateShipDSections } = require("../../../utils/shipdGeometryGenerator");
            const shipdSections = generateShipDSections({
              shipdVector,
              lppM: candidate.lppM,
              beamM: beam,
              draftM: draft,
              metadata: sizingStore.shipdParameters,
              resolution: 1.0,
            }, stationCount + 1);

            const stationIndices = Array.from({ length: stationCount + 1 }, (_, i) => i);
            const result = extractSectionsFromShipD(shipdSections, stationIndices);
            console.log("[Hull2DSections] Generated sections from ShipD parameters", {
              sectionCount: result.length,
            });
            return result;
          } else {
            console.warn("[Hull2DSections] Invalid ShipD vector format", {
              isArray: Array.isArray(shipdVector),
              length: Array.isArray(shipdVector) ? shipdVector.length : 0,
            });
          }
        } catch (error) {
          console.error("[Hull2DSections] Failed to generate ShipD geometry, falling back to parametric:", error);
        }
      } else {
        console.log("[Hull2DSections] No ShipD data available", {
          hasParametersJson: !!candidate.shipdParametersJson,
          hasMetadata: !!(sizingStore.shipdParameters && sizingStore.shipdParameters.length > 0),
        });
      }

      // Fallback: Use parametric generation
      const sectionCurves = [];

      for (let i = 0; i <= stationCount; i++) {
        const stationNum = i;
        const xNorm = (2 * i) / stationCount - 1;
        const longitudinalFactor = 1 - xNorm * xNorm;

        if (longitudinalFactor <= 0) {
          sectionCurves.push({
            station: stationNum,
            points: [[0, 0]],
            isAft: stationNum <= 5,
            hasBulb: false,
          });
          continue;
        }

        const points: [number, number][] = [];
        const numPoints = 40;

        for (let j = 0; j <= numPoints; j++) {
          const z = -(j / numPoints) * draft;
          const zNorm = z / draft;
          const y = (beam / 2) * (1 - zNorm * zNorm) * longitudinalFactor;
          points.push([y, z]);
        }

        sectionCurves.push({
          station: stationNum,
          points,
          isAft: stationNum <= 5,
          hasBulb: false,
        });
      }

      return sectionCurves;
    }, [
      candidate.beamM,
      candidate.draftM,
      candidate.lppM,
      candidate.geometryJson,
      candidate.shipdParametersJson,
      sizingStore.shipdParameters,
      stationCount,
    ]);

    const padding = 60;
    const svgWidth = 600;
    const svgHeight = 600;
    const beam = candidate.beamM;
    const draft = candidate.draftM;
    const depth = candidate.depthM;
    const scaleY = (svgWidth / 2 - padding) / (beam / 2);
    const scaleZ = (svgHeight - 2 * padding) / depth;
    const scale = Math.min(scaleY, scaleZ);

    const toSVG = (y: number, z: number, isAft: boolean): [number, number] => {
      const xOffset = isAft ? -y : y;
      return [svgWidth / 2 + xOffset * scale, svgHeight - padding - (z + draft) * scale];
    };

    const generateSectionPath = (points: [number, number][], isAft: boolean) =>
      points
        .map(([y, z], i) => {
          const [sx, sy] = toSVG(y, z, isAft);
          return `${i === 0 ? "M" : "L"} ${sx.toFixed(2)},${sy.toFixed(2)}`;
        })
        .join(" ");

    return (
      <div className="w-full h-full p-4 relative flex flex-col">
        <div className="flex-1 bg-gradient-to-r from-slate-50 via-white to-slate-50 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900 border border-gray-300 dark:border-gray-600 rounded-t-lg shadow-lg flex flex-col">
          <svg
            ref={ref}
            width="100%"
            height="100%"
            viewBox={`0 0 ${svgWidth} ${svgHeight}`}
            className="flex-1"
          >
            <defs>
              <linearGradient id="sectionsGradient" x1="0%" y1="0%" x2="100%" y2="0%">
                <stop offset="0%" stopColor="#f1f5f9" stopOpacity="0.5" />
                <stop offset="50%" stopColor="#ffffff" stopOpacity="0.2" />
                <stop offset="100%" stopColor="#f1f5f9" stopOpacity="0.5" />
              </linearGradient>
              <filter id="sectionGlow">
                <feGaussianBlur stdDeviation="1.5" result="coloredBlur" />
                <feMerge>
                  <feMergeNode in="coloredBlur" />
                  <feMergeNode in="SourceGraphic" />
                </feMerge>
              </filter>
              <filter id="sectionShadow">
                <feDropShadow dx="0" dy="1" stdDeviation="2" floodOpacity="0.25" />
              </filter>
            </defs>

            <rect x="0" y="0" width={svgWidth} height={svgHeight} fill="url(#sectionsGradient)" />

            <text
              x={svgWidth / 2}
              y={25}
              textAnchor="middle"
              className="fill-gray-900 dark:fill-gray-100 font-bold"
              style={{ fontSize: "14px", letterSpacing: "0.5px" }}
            >
              SECTIONS VIEW (BODY PLAN)
            </text>
            <text
              x={svgWidth / 2}
              y={40}
              textAnchor="middle"
              className="fill-gray-600 dark:fill-gray-400"
              style={{ fontSize: "10px" }}
            >
              {candidate.hullFamily.replace("_", " ").toUpperCase()} · Stations 0-10
              {candidate.geometryJson && (
                <tspan className="fill-green-600 dark:fill-green-400" style={{ fontSize: "9px" }}>
                  {" "}
                  · ShipD
                </tspan>
              )}
            </text>

            {/* Centerline */}
            {showCenterline && visibility.centerline && (
              <line
                x1={svgWidth / 2}
                y1={padding}
                x2={svgWidth / 2}
                y2={svgHeight - padding}
                stroke="#6b7280"
                strokeWidth="2"
                strokeDasharray="5,5"
                opacity={hoveredLegendItem && hoveredLegendItem !== "centerline" ? 0.3 : 1}
                style={{ transition: "all 0.3s ease" }}
              />
            )}

            {/* Baseline */}
            {showBaseline && visibility.baseline && (
              <line
                x1={padding}
                y1={toSVG(0, -draft, true)[1]}
                x2={svgWidth - padding}
                y2={toSVG(0, -draft, true)[1]}
                stroke="#6b7280"
                strokeWidth="2"
                opacity={hoveredLegendItem && hoveredLegendItem !== "baseline" ? 0.3 : 1}
                style={{ transition: "all 0.3s ease" }}
              />
            )}

            {/* Waterline */}
            {showWaterline && visibility.waterline && (
              <line
                x1={padding}
                y1={toSVG(0, 0, true)[1]}
                x2={svgWidth - padding}
                y2={toSVG(0, 0, true)[1]}
                stroke="#06b6d4"
                strokeWidth="2.5"
                strokeDasharray="10,5"
                filter="url(#sectionGlow)"
                opacity={hoveredLegendItem && hoveredLegendItem !== "waterline" ? 0.3 : 1}
                style={{ transition: "all 0.3s ease" }}
              />
            )}

            {/* Deck line */}
            <line
              x1={padding}
              y1={toSVG(0, depth - draft, true)[1]}
              x2={svgWidth - padding}
              y2={toSVG(0, depth - draft, true)[1]}
              stroke="#1f2937"
              strokeWidth="1.5"
              strokeDasharray="5,3"
              opacity="0.7"
            />

            {/* Section curves with animations */}
            {visibility.sections &&
              sections.map((section) => {
                if (section.station === 5) return null;
                const isHovered = hoveredSection === section.station;
                const isEndStation = section.station === 0 || section.station === 10;
                const color = isEndStation ? "#3b82f6" : isHovered ? "#60a5fa" : "#93c5fd";
                const strokeWidth = isEndStation ? 2.5 : isHovered ? 2 : 1.2;
                const dimmed = hoveredLegendItem && hoveredLegendItem !== "sections";

                return (
                  <g
                    key={section.station}
                    onMouseEnter={() => setHoveredSection(section.station)}
                    onMouseLeave={() => setHoveredSection(null)}
                    style={{
                      opacity: dimmed ? 0.3 : 0,
                      animation: `fadeIn 0.3s ease-in forwards ${section.station * 0.05}s`,
                      cursor: "pointer",
                    }}
                  >
                    {/* Main hull section */}
                    {section.points.length > 1 && (
                      <path
                        d={generateSectionPath(section.points as [number, number][], section.isAft)}
                        fill="none"
                        stroke={color}
                        strokeWidth={strokeWidth}
                        strokeLinecap="round"
                        filter={isEndStation ? "url(#sectionShadow)" : undefined}
                        style={{ transition: "all 0.3s ease" }}
                      />
                    )}

                    {/* Bulb indicator (if present) - overlay on main section */}
                    {section.hasBulb && (
                      <circle
                        cx={toSVG(section.points[section.points.length - 1]?.[0] || 0, section.points[section.points.length - 1]?.[1] || -draft, section.isAft)[0]}
                        cy={toSVG(section.points[section.points.length - 1]?.[0] || 0, section.points[section.points.length - 1]?.[1] || -draft, section.isAft)[1]}
                        r="4"
                        fill="#f59e0b"
                        stroke="#ffffff"
                        strokeWidth="1.5"
                        opacity="0.9"
                        filter="url(#sectionGlow)"
                      />
                    )}

                    {section.points.length > 1 && (
                      <g>
                        <text
                          x={toSVG(section.points[0][0], section.points[0][1], section.isAft)[0]}
                          y={
                            toSVG(section.points[0][0], section.points[0][1], section.isAft)[1] - 10
                          }
                          textAnchor="middle"
                          className={
                            isHovered ? "fill-blue-700 font-bold" : "fill-blue-500 font-semibold"
                          }
                          style={{
                            fontSize: isHovered ? "12px" : "10px",
                            transition: "all 0.2s ease",
                          }}
                        >
                          {section.station}
                          {section.hasBulb && (
                            <tspan className="fill-orange-600" style={{ fontSize: "8px" }}>
                              {" "}
                              ●
                            </tspan>
                          )}
                        </text>

                        {isHovered && (
                          <g>
                            <rect
                              x={
                                toSVG(
                                  section.points[0][0],
                                  section.points[0][1],
                                  section.isAft
                                )[0] - 50
                              }
                              y={
                                toSVG(
                                  section.points[0][0],
                                  section.points[0][1],
                                  section.isAft
                                )[1] - 30
                              }
                              width={section.hasBulb ? "100" : "80"}
                              height="16"
                              rx="3"
                              fill="#1f2937"
                              opacity="0.95"
                            />
                            <text
                              x={
                                toSVG(section.points[0][0], section.points[0][1], section.isAft)[0]
                              }
                              y={
                                toSVG(
                                  section.points[0][0],
                                  section.points[0][1],
                                  section.isAft
                                )[1] - 19
                              }
                              textAnchor="middle"
                              fill="#ffffff"
                              style={{ fontSize: "9px" }}
                            >
                              Station {section.station} · {section.isAft ? "AFT" : "FWD"}
                              {section.hasBulb && " · Bulb"}
                            </text>
                          </g>
                        )}
                      </g>
                    )}
                  </g>
                );
              })}

            {/* Midship - special highlighting */}
            {visibility.midship && sections.find((s) => s.station === 5) && (
              <g
                style={{
                  opacity: hoveredLegendItem && hoveredLegendItem !== "midship" ? 0.3 : 0,
                  animation: "fadeIn 0.5s ease-in forwards 0.6s",
                  transition: "opacity 0.3s ease",
                }}
              >
                <path
                  d={generateSectionPath(
                    sections.find((s) => s.station === 5)!.points as [number, number][],
                    false
                  )}
                  fill="none"
                  stroke="url(#midshipGradient)"
                  strokeWidth="3.5"
                  strokeLinecap="round"
                  filter="url(#sectionGlow)"
                />
                <path
                  d={generateSectionPath(
                    sections.find((s) => s.station === 5)!.points as [number, number][],
                    true
                  )}
                  fill="none"
                  stroke="url(#midshipGradient)"
                  strokeWidth="3.5"
                  strokeLinecap="round"
                  filter="url(#sectionGlow)"
                />
                <defs>
                  <linearGradient id="midshipGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                    <stop offset="0%" stopColor="#f59e0b" />
                    <stop offset="50%" stopColor="#fb923c" />
                    <stop offset="100%" stopColor="#f59e0b" />
                  </linearGradient>
                </defs>
                <text
                  x={svgWidth / 2}
                  y={padding + 18}
                  textAnchor="middle"
                  className="fill-orange-600 dark:fill-orange-400 font-bold"
                  style={{ fontSize: "12px" }}
                >
                  ⊥ MIDSHIP (Station 5)
                </text>
              </g>
            )}

            {/* Side labels with styling */}
            <g>
              <rect
                x={padding + 5}
                y={padding + 22}
                width="110"
                height="18"
                rx="4"
                fill="#ef4444"
                opacity="0.1"
              />
              <text
                x={padding + 60}
                y={padding + 35}
                textAnchor="middle"
                className="fill-red-700 dark:fill-red-400 font-bold"
                style={{ fontSize: "11px" }}
              >
                ← AFT (0-4)
              </text>

              <rect
                x={svgWidth - padding - 115}
                y={padding + 22}
                width="110"
                height="18"
                rx="4"
                fill="#10b981"
                opacity="0.1"
              />
              <text
                x={svgWidth - padding - 60}
                y={padding + 35}
                textAnchor="middle"
                className="fill-green-700 dark:fill-green-400 font-bold"
                style={{ fontSize: "11px" }}
              >
                FORWARD (6-10) →
              </text>
            </g>

            {/* Half-breadth dimension */}
            {showDimensions && (
              <>
                <defs>
                  <marker
                    id="arrow-sections"
                    markerWidth="8"
                    markerHeight="6"
                    refX="8"
                    refY="3"
                    orient="auto"
                  >
                    <polygon points="0 0, 8 3, 0 6" fill="#374151" />
                  </marker>
                </defs>

                <line
                  x1={svgWidth / 2}
                  y1={svgHeight - padding - 15}
                  x2={toSVG(beam / 2, -draft, false)[0]}
                  y2={svgHeight - padding - 15}
                  stroke="#374151"
                  strokeWidth="1.2"
                  markerEnd="url(#arrow-sections)"
                />
                <rect
                  x={(svgWidth / 2 + toSVG(beam / 2, -draft, false)[0]) / 2 - 40}
                  y={svgHeight - padding - 28}
                  width="80"
                  height="16"
                  rx="3"
                  fill="#ffffff"
                  stroke="#3b82f6"
                  strokeWidth="1"
                  filter="url(#sectionShadow)"
                />
                <text
                  x={(svgWidth / 2 + toSVG(beam / 2, -draft, false)[0]) / 2}
                  y={svgHeight - padding - 17}
                  textAnchor="middle"
                  className="fill-gray-900 dark:fill-gray-100 font-bold"
                  style={{ fontSize: "10px" }}
                >
                  B/2 = {(beam / 2).toFixed(2)}m
                </text>
              </>
            )}
          </svg>
        </div>

        {/* Integrated Info Bar - Part of the same panel */}
        <div className="bg-white dark:bg-gray-800 border-t-0 border-l border-r border-b border-gray-300 dark:border-gray-600 rounded-b-lg shadow-lg p-3 flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
          {/* Left: Interactive legend items */}
          <div className="flex items-center gap-3 flex-wrap">
            <button
              onClick={() => setVisibility((prev) => ({ ...prev, midship: !prev.midship }))}
              onMouseEnter={() => setHoveredLegendItem("midship")}
              onMouseLeave={() => setHoveredLegendItem(null)}
              className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all text-xs font-medium ${
                visibility.midship
                  ? "bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300"
                  : "bg-gray-200/50 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 line-through"
              }`}
            >
              <div className="w-5 h-1 bg-gradient-to-r from-orange-600 to-orange-400 rounded"></div>
              <span>Midship</span>
            </button>

            <button
              onClick={() => setVisibility((prev) => ({ ...prev, sections: !prev.sections }))}
              onMouseEnter={() => setHoveredLegendItem("sections")}
              onMouseLeave={() => setHoveredLegendItem(null)}
              className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all text-xs font-medium ${
                visibility.sections
                  ? "bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300"
                  : "bg-gray-200/50 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 line-through"
              }`}
            >
              <div className="w-5 h-0.5 bg-blue-400"></div>
              <span>Sections</span>
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
              onClick={() => setVisibility((prev) => ({ ...prev, centerline: !prev.centerline }))}
              onMouseEnter={() => setHoveredLegendItem("centerline")}
              onMouseLeave={() => setHoveredLegendItem(null)}
              className={`flex items-center gap-2 px-3 py-1.5 rounded-md transition-all text-xs font-medium ${
                visibility.centerline
                  ? "bg-transparent hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300"
                  : "bg-gray-200/50 dark:bg-gray-700/50 text-gray-400 dark:text-gray-500 line-through"
              }`}
            >
              <div className="w-5 h-0.5 bg-gray-600"></div>
              <span>Centerline</span>
            </button>
          </div>

          {/* Separator */}
          <div className="hidden md:block h-6 w-px bg-border"></div>

          {/* Right: Form Coefficients dropdown */}
          <div className="relative">
            <button
              onClick={() => setShowCoefficients(!showCoefficients)}
              className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-600 rounded-lg shadow-sm px-3 py-1.5 text-xs font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors flex items-center gap-1.5"
            >
              {showCoefficients ? "▼" : "▶"} Form Coefficients
            </button>
            {showCoefficients && (
              <div className="absolute bottom-full right-0 mb-2 bg-white/95 dark:bg-gray-800/95 backdrop-blur-md rounded-xl shadow-2xl p-4 text-xs border border-gray-200 dark:border-gray-700 min-w-[200px]">
                <div className="font-bold text-gray-900 dark:text-gray-100 mb-3 text-sm border-b border-gray-200 dark:border-gray-600 pb-2">
                  Form Coefficients
                </div>
                <div className="grid grid-cols-2 gap-x-3 gap-y-1.5">
                  <span className="text-gray-600 dark:text-gray-400">Cb:</span>
                  <span className="font-semibold text-blue-700 dark:text-blue-400">
                    {candidate.cb.toFixed(4)}
                  </span>
                  <span className="text-gray-600 dark:text-gray-400">Cp:</span>
                  <span className="font-semibold text-blue-700 dark:text-blue-400">
                    {candidate.cp.toFixed(4)}
                  </span>
                  <span className="text-gray-600 dark:text-gray-400">Cwp:</span>
                  <span className="font-semibold text-blue-700 dark:text-blue-400">
                    {candidate.cwp.toFixed(4)}
                  </span>
                  {candidate.cm && (
                    <>
                      <span className="text-gray-600 dark:text-gray-400">Cm:</span>
                      <span className="font-semibold text-orange-700 dark:text-orange-400">
                        {candidate.cm.toFixed(4)}
                      </span>
                    </>
                  )}
                </div>
                <div className="mt-3 pt-2 border-t border-gray-200 dark:border-gray-600">
                  <div className="text-[10px] text-gray-500 dark:text-gray-400">
                    Beam: {beam.toFixed(2)}m · Draft: {draft.toFixed(2)}m
                  </div>
                </div>
              </div>
            )}
          </div>
        </div>

        <style>{`
        @keyframes fadeIn {
          from { opacity: 0; transform: scale(0.95); }
          to { opacity: 1; transform: scale(1); }
        }
      `}</style>
      </div>
    );
  }
);

Hull2DSections.displayName = "Hull2DSections";
