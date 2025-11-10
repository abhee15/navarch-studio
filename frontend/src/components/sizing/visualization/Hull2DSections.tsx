import { useMemo, useState, forwardRef } from "react";
import type { CandidateDesign } from "../../../types/sizing";

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
    const [showLegend, setShowLegend] = useState(false);
    const [showCoefficients, setShowCoefficients] = useState(true);

    const sections = useMemo(() => {
      const beam = candidate.beamM;
      const draft = candidate.draftM;
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
        });
      }

      return sectionCurves;
    }, [candidate.beamM, candidate.draftM, stationCount]);

    const padding = 60;
    const svgWidth = 600;
    const svgHeight = 500;
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
      <div className="w-full h-full bg-gradient-to-r from-slate-50 via-white to-slate-50 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900 p-4 relative">
        <svg
          ref={ref}
          width="100%"
          height="100%"
          viewBox={`0 0 ${svgWidth} ${svgHeight}`}
          className="border border-gray-300 dark:border-gray-600 rounded-lg shadow-lg"
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
          </text>

          {/* Centerline */}
          {showCenterline && (
            <line
              x1={svgWidth / 2}
              y1={padding}
              x2={svgWidth / 2}
              y2={svgHeight - padding}
              stroke="#6b7280"
              strokeWidth="2"
              strokeDasharray="5,5"
            />
          )}

          {/* Baseline */}
          {showBaseline && (
            <line
              x1={padding}
              y1={toSVG(0, -draft, true)[1]}
              x2={svgWidth - padding}
              y2={toSVG(0, -draft, true)[1]}
              stroke="#6b7280"
              strokeWidth="2"
            />
          )}

          {/* Waterline */}
          {showWaterline && (
            <line
              x1={padding}
              y1={toSVG(0, 0, true)[1]}
              x2={svgWidth - padding}
              y2={toSVG(0, 0, true)[1]}
              stroke="#06b6d4"
              strokeWidth="2.5"
              strokeDasharray="10,5"
              filter="url(#sectionGlow)"
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
          {sections.map((section) => {
            if (section.station === 5) return null;
            const isHovered = hoveredSection === section.station;
            const isEndStation = section.station === 0 || section.station === 10;
            const color = isEndStation ? "#3b82f6" : isHovered ? "#60a5fa" : "#93c5fd";
            const strokeWidth = isEndStation ? 2.5 : isHovered ? 2 : 1.2;

            return (
              <g
                key={section.station}
                onMouseEnter={() => setHoveredSection(section.station)}
                onMouseLeave={() => setHoveredSection(null)}
                style={{
                  opacity: 0,
                  animation: `fadeIn 0.3s ease-in forwards ${section.station * 0.05}s`,
                  cursor: "pointer",
                }}
              >
                <path
                  d={generateSectionPath(section.points as [number, number][], section.isAft)}
                  fill="none"
                  stroke={color}
                  strokeWidth={strokeWidth}
                  strokeLinecap="round"
                  filter={isEndStation ? "url(#sectionShadow)" : undefined}
                  style={{ transition: "all 0.3s ease" }}
                />

                {section.points.length > 1 && (
                  <g>
                    <text
                      x={toSVG(section.points[0][0], section.points[0][1], section.isAft)[0]}
                      y={toSVG(section.points[0][0], section.points[0][1], section.isAft)[1] - 10}
                      textAnchor="middle"
                      className={
                        isHovered ? "fill-blue-700 font-bold" : "fill-blue-500 font-semibold"
                      }
                      style={{ fontSize: isHovered ? "12px" : "10px", transition: "all 0.2s ease" }}
                    >
                      {section.station}
                    </text>

                    {isHovered && (
                      <g>
                        <rect
                          x={
                            toSVG(section.points[0][0], section.points[0][1], section.isAft)[0] - 40
                          }
                          y={
                            toSVG(section.points[0][0], section.points[0][1], section.isAft)[1] - 30
                          }
                          width="80"
                          height="16"
                          rx="3"
                          fill="#1f2937"
                          opacity="0.95"
                        />
                        <text
                          x={toSVG(section.points[0][0], section.points[0][1], section.isAft)[0]}
                          y={
                            toSVG(section.points[0][0], section.points[0][1], section.isAft)[1] - 19
                          }
                          textAnchor="middle"
                          fill="#ffffff"
                          style={{ fontSize: "9px" }}
                        >
                          Station {section.station} · {section.isAft ? "AFT" : "FWD"}
                        </text>
                      </g>
                    )}
                  </g>
                )}
              </g>
            );
          })}

          {/* Midship - special highlighting */}
          {sections.find((s) => s.station === 5) && (
            <g
              style={{
                opacity: 0,
                animation: "fadeIn 0.5s ease-in forwards 0.6s",
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

        {/* Collapsible Legend */}
        <div className="absolute top-6 right-6 max-w-[240px]">
          <button
            onClick={() => setShowLegend(!showLegend)}
            className="bg-white/90 dark:bg-gray-800/90 backdrop-blur-sm rounded-lg shadow-lg px-3 py-1.5 text-xs font-medium text-gray-700 dark:text-gray-300 hover:bg-white dark:hover:bg-card transition-colors flex items-center gap-1.5"
          >
            {showLegend ? "▼" : "▶"} Legend
          </button>
          {showLegend && (
            <div className="mt-2 bg-white/95 dark:bg-gray-800/95 backdrop-blur-md rounded-xl shadow-2xl p-4 text-xs space-y-2 border border-gray-200 dark:border-gray-700">
              <div className="font-bold text-gray-900 dark:text-gray-100 mb-3 text-sm border-b border-gray-200 dark:border-gray-600 pb-2">
                Body Plan
              </div>
              <div className="flex items-center gap-2 hover:bg-muted/50 dark:hover:bg-muted p-1 rounded transition-colors">
                <div className="w-5 h-1 bg-gradient-to-r from-orange-600 to-orange-400 rounded shadow-sm"></div>
                <span className="text-gray-700 dark:text-gray-300 font-medium">Midship (⊥)</span>
              </div>
              <div className="flex items-center gap-2 hover:bg-muted/50 dark:hover:bg-muted p-1 rounded transition-colors">
                <div className="w-5 h-0.5 bg-blue-600 shadow-sm"></div>
                <span className="text-gray-700 dark:text-gray-300">AP (0) / FP (10)</span>
              </div>
              <div className="flex items-center gap-2 hover:bg-muted/50 dark:hover:bg-muted p-1 rounded transition-colors">
                <div className="w-5 h-0.5 bg-blue-300"></div>
                <span className="text-gray-700 dark:text-gray-300">Stations 1-9</span>
              </div>
              <div className="pt-2 mt-2 border-t border-gray-200 dark:border-gray-600">
                <div className="text-[10px] text-gray-600 dark:text-gray-400 space-y-1">
                  <div className="flex items-center gap-2">
                    <span className="font-bold text-red-600">←</span>
                    <span>Aft sections (0-4)</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="font-bold text-green-600">→</span>
                    <span>Forward sections (6-10)</span>
                  </div>
                </div>
              </div>
              <div className="pt-2 text-[10px] text-gray-500 dark:text-gray-400">
                Hover sections for station info
              </div>
            </div>
          )}
        </div>

        {/* Collapsible Form Coefficients Panel */}
        <div className="absolute bottom-6 left-6">
          <button
            onClick={() => setShowCoefficients(!showCoefficients)}
            className="bg-white/90 dark:bg-gray-800/90 backdrop-blur-sm rounded-lg shadow-lg px-3 py-1.5 text-xs font-medium text-gray-700 dark:text-gray-300 hover:bg-white dark:hover:bg-card transition-colors flex items-center gap-1.5"
          >
            {showCoefficients ? "▼" : "▶"} Form Coefficients
          </button>
          {showCoefficients && (
            <div className="mt-2 bg-white/95 dark:bg-gray-800/95 backdrop-blur-md rounded-xl shadow-2xl p-4 text-xs border border-gray-200 dark:border-gray-700">
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
