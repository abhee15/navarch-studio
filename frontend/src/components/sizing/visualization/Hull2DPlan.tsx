import { useMemo, useState, forwardRef } from "react";
import type { CandidateDesign } from "../../../types/sizing";

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

    // Calculate waterline curves
    const waterlines = useMemo(() => {
      const lpp = candidate.lppM;
      const beam = candidate.beamM;
      const draft = candidate.draftM;
      const lines = [];

      for (let i = 0; i <= waterlineCount; i++) {
        const z = -(i / waterlineCount) * draft;
        const zNorm = z / draft;
        const points: [number, number][] = [];
        const numPoints = 60;

        for (let j = 0; j <= numPoints; j++) {
          const x = (j / numPoints) * lpp - lpp / 2;
          const xNorm = (2 * j) / numPoints - 1;
          const y = (beam / 2) * (1 - zNorm * zNorm) * (1 - xNorm * xNorm);
          points.push([x, y]);
        }

        lines.push({
          depth: Math.abs(z),
          points,
          isDesignWaterline: i === waterlineCount,
        });
      }

      return lines;
    }, [candidate.lppM, candidate.beamM, candidate.draftM, waterlineCount]);

    const stations = useMemo(() => {
      const lpp = candidate.lppM;
      const numStations = 10;
      return Array.from({ length: numStations + 1 }, (_, i) => ({
        number: i,
        x: (i / numStations) * lpp - lpp / 2,
      }));
    }, [candidate.lppM]);

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
      const svgPoints = points.map(([x, y]) => toSVG(x, y));
      const path = svgPoints
        .map(([x, y], i) => `${i === 0 ? "M" : "L"} ${x.toFixed(2)},${y.toFixed(2)}`)
        .join(" ");
      const mirroredPoints = points.map(([x, y]) => toSVG(x, -y));
      const mirrorPath = mirroredPoints
        .map(([x, y], i) => `${i === 0 ? "M" : "L"} ${x.toFixed(2)},${y.toFixed(2)}`)
        .join(" ");
      return { starboard: path, port: mirrorPath };
    };

    return (
      <div className="w-full h-full bg-gradient-to-b from-blue-50 via-white to-cyan-50 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900 p-4 relative">
        <svg
          ref={ref}
          width="100%"
          height="100%"
          viewBox={`0 0 ${svgWidth} ${svgHeight}`}
          className="border border-gray-300 dark:border-gray-600 rounded-lg shadow-inner"
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
          </text>

          {/* Centerline */}
          {showCenterline && (
            <line
              x1={toSVG(-lpp / 2, 0)[0]}
              y1={toSVG(-lpp / 2, 0)[1]}
              x2={toSVG(lpp / 2, 0)[0]}
              y2={toSVG(lpp / 2, 0)[1]}
              stroke="#9ca3af"
              strokeWidth="1"
              strokeDasharray="5,5"
              opacity="0.6"
            />
          )}

          {/* Perpendiculars */}
          {showStations && (
            <>
              <line
                x1={toSVG(-lpp / 2, -beam / 2)[0]}
                y1={toSVG(-lpp / 2, -beam / 2)[1]}
                x2={toSVG(-lpp / 2, beam / 2)[0]}
                y2={toSVG(-lpp / 2, beam / 2)[1]}
                stroke="#ef4444"
                strokeWidth="2.5"
                filter="url(#dropShadow)"
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
            stations.map((station) => {
              const [sx] = toSVG(station.x, 0);
              const isHovered = hoveredStation === station.number;

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
                    style={{ transition: "all 0.2s ease" }}
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
            waterlines.map((wl, idx) => {
              const paths = waterlinePath(wl.points);
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
                    opacity: 0,
                    animation: `fadeIn 0.4s ease-in forwards ${idx * 0.08}s`,
                  }}
                >
                  {/* Starboard */}
                  <path
                    d={paths.starboard}
                    fill="none"
                    stroke={isHovered ? hoverColor : baseColor}
                    strokeWidth={strokeWidth}
                    strokeLinecap="round"
                    filter={wl.isDesignWaterline ? "url(#dropShadow)" : undefined}
                    style={{
                      transition: "all 0.3s ease",
                      cursor: "pointer",
                    }}
                  />
                  {/* Port */}
                  <path
                    d={paths.port}
                    fill="none"
                    stroke={isHovered ? hoverColor : baseColor}
                    strokeWidth={strokeWidth}
                    strokeLinecap="round"
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
          {showDimensions && (
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
          {candidate.lcbPctLpp && (
            <g>
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

        {/* Enhanced Legend */}
        <div className="absolute top-6 right-6 bg-white/95 dark:bg-gray-800/95 backdrop-blur-md rounded-xl shadow-2xl p-4 text-xs space-y-2 border border-gray-200 dark:border-gray-700">
          <div className="font-bold text-gray-900 dark:text-gray-100 mb-3 text-sm border-b border-gray-200 dark:border-gray-600 pb-2">
            Legend
          </div>
          <div className="flex items-center gap-2 hover:bg-gray-50 dark:hover:bg-gray-700 p-1 rounded transition-colors">
            <div className="w-5 h-0.5 bg-gradient-to-r from-blue-600 to-blue-400 shadow-sm"></div>
            <span className="text-gray-700 dark:text-gray-300">
              Design Waterline (T={candidate.draftM.toFixed(1)}m)
            </span>
          </div>
          <div className="flex items-center gap-2 hover:bg-gray-50 dark:hover:bg-gray-700 p-1 rounded transition-colors">
            <div className="w-5 h-0.5 bg-blue-400"></div>
            <span className="text-gray-700 dark:text-gray-300">Waterlines (surface to keel)</span>
          </div>
          <div className="flex items-center gap-2 hover:bg-gray-50 dark:hover:bg-gray-700 p-1 rounded transition-colors">
            <div className="w-5 h-0.5 bg-red-600"></div>
            <span className="text-gray-700 dark:text-gray-300">AP (Aft Perpendicular)</span>
          </div>
          <div className="flex items-center gap-2 hover:bg-gray-50 dark:hover:bg-gray-700 p-1 rounded transition-colors">
            <div className="w-5 h-0.5 bg-green-600"></div>
            <span className="text-gray-700 dark:text-gray-300">FP (Forward Perpendicular)</span>
          </div>
          {candidate.lcbPctLpp && (
            <div className="flex items-center gap-2 hover:bg-gray-50 dark:hover:bg-gray-700 p-1 rounded transition-colors">
              <div className="w-3 h-3 rounded-full bg-red-500 shadow-md animate-pulse"></div>
              <span className="text-gray-700 dark:text-gray-300 font-medium">
                LCB ({candidate.lcbPctLpp.toFixed(1)}% Lpp)
              </span>
            </div>
          )}
          <div className="pt-2 mt-2 border-t border-gray-200 dark:border-gray-600 text-[10px] text-gray-500 dark:text-gray-400">
            💡 Hover over waterlines and stations for details
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

