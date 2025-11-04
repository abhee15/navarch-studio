import { useMemo, useState, forwardRef } from "react";
import type { CandidateDesign } from "../../../types/sizing";

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

    const buttocks = useMemo(() => {
      const lpp = candidate.lppM;
      const beam = candidate.beamM;
      const draft = candidate.draftM;
      const lines = [];

      for (let i = 0; i <= buttockCount; i++) {
        const yPos = (i / buttockCount) * (beam / 2);
        const points: [number, number][] = [];
        const numPoints = 60;

        for (let j = 0; j <= numPoints; j++) {
          const x = (j / numPoints) * lpp - lpp / 2;
          const xNorm = (2 * j) / numPoints - 1;
          const longitudinalFactor = 1 - xNorm * xNorm;

          if (longitudinalFactor <= 0) {
            points.push([x, 0]);
            continue;
          }

          const maxYAtX = (beam / 2) * longitudinalFactor;
          if (yPos > maxYAtX) {
            points.push([x, 0]);
            continue;
          }

          const ratio = yPos / maxYAtX;
          const zOverT = Math.sqrt(Math.max(0, 1 - ratio));
          const z = -draft * zOverT;
          points.push([x, z]);
        }

        lines.push({
          transverseOffset: yPos,
          points,
          isCenterline: i === 0,
        });
      }

      return lines;
    }, [candidate.lppM, candidate.beamM, candidate.draftM, buttockCount]);

    const sheerline = useMemo(() => {
      const lpp = candidate.lppM;
      const depth = candidate.depthM;
      const draft = candidate.draftM;
      const freeboard = depth - draft;
      const points: [number, number][] = [];
      const numPoints = 60;

      for (let i = 0; i <= numPoints; i++) {
        const x = (i / numPoints) * lpp - lpp / 2;
        const xNorm = (2 * i) / numPoints - 1;
        // Subtle sheer (higher at bow/stern)
        const sheer = 0.01 * lpp * (xNorm * xNorm);
        const y = freeboard + sheer;
        points.push([x, y]);
      }

      return points;
    }, [candidate.lppM, candidate.depthM, candidate.draftM]);

    const padding = 80;
    const svgWidth = 900;
    const svgHeight = 400;
    const lpp = candidate.lppM;
    const depth = candidate.depthM;
    const scaleX = (svgWidth - 2 * padding) / lpp;
    const scaleY = (svgHeight - 2 * padding) / depth;
    const scale = Math.min(scaleX, scaleY);

    const toSVG = (x: number, y: number): [number, number] => [
      svgWidth / 2 + x * scale,
      svgHeight - padding - (y + candidate.draftM) * scale,
    ];

    const generatePath = (points: [number, number][]) =>
      points
        .map(
          ([x, y], i) =>
            `${i === 0 ? "M" : "L"} ${toSVG(x, y)[0].toFixed(2)},${toSVG(x, y)[1].toFixed(2)}`
        )
        .join(" ");

    return (
      <div className="w-full h-full bg-gradient-to-b from-sky-100 via-blue-50 to-cyan-100 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900 p-4 relative">
        <svg
          ref={ref}
          width="100%"
          height="100%"
          viewBox={`0 0 ${svgWidth} ${svgHeight}`}
          className="border border-gray-300 dark:border-gray-600 rounded-lg shadow-lg"
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
            {candidate.hullFamily.replace("_", " ").toUpperCase()} · T {candidate.draftM.toFixed(2)}m ×
            D {candidate.depthM.toFixed(2)}m
          </text>

          {/* Baseline */}
          {showBaseline && (
            <line
              x1={toSVG(-lpp / 2, -candidate.draftM)[0]}
              y1={toSVG(-lpp / 2, -candidate.draftM)[1]}
              x2={toSVG(lpp / 2, -candidate.draftM)[0]}
              y2={toSVG(lpp / 2, -candidate.draftM)[1]}
              stroke="#6b7280"
              strokeWidth="2"
              strokeDasharray="10,5"
            />
          )}

          {/* Perpendiculars */}
          <line
            x1={toSVG(-lpp / 2, -candidate.draftM)[0]}
            y1={toSVG(-lpp / 2, -candidate.draftM)[1]}
            x2={toSVG(-lpp / 2, candidate.depthM - candidate.draftM)[0]}
            y2={toSVG(-lpp / 2, candidate.depthM - candidate.draftM)[1]}
            stroke="#ef4444"
            strokeWidth="3"
            filter="url(#shadow)"
          />
          <line
            x1={toSVG(lpp / 2, -candidate.draftM)[0]}
            y1={toSVG(lpp / 2, -candidate.draftM)[1]}
            x2={toSVG(lpp / 2, candidate.depthM - candidate.draftM)[0]}
            y2={toSVG(lpp / 2, candidate.depthM - candidate.draftM)[1]}
            stroke="#10b981"
            strokeWidth="3"
            filter="url(#shadow)"
          />

          {/* Waterline */}
          {showWaterline && (
            <line
              x1={toSVG(-lpp / 2, 0)[0]}
              y1={toSVG(-lpp / 2, 0)[1]}
              x2={toSVG(lpp / 2, 0)[0]}
              y2={toSVG(lpp / 2, 0)[1]}
              stroke="#06b6d4"
              strokeWidth="2.5"
              strokeDasharray="10,5"
              filter="url(#glow)"
            />
          )}

          {/* Sheerline (deck) with gradient */}
          <path
            d={generatePath(sheerline)}
            fill="none"
            stroke="url(#deckGradient)"
            strokeWidth="3.5"
            strokeLinecap="round"
            filter="url(#shadow)"
          />

          <defs>
            <linearGradient id="deckGradient" x1="0%" y1="0%" x2="100%" y2="0%">
              <stop offset="0%" stopColor="#1f2937" />
              <stop offset="50%" stopColor="#374151" />
              <stop offset="100%" stopColor="#1f2937" />
            </linearGradient>
          </defs>

          {/* Buttocks with animations */}
          {showButtocks &&
            buttocks.map((buttock, idx) => {
              const isHovered = hoveredButtock === idx;
              const color = buttock.isCenterline ? "#3b82f6" : isHovered ? "#60a5fa" : "#93c5fd";
              const strokeWidth = buttock.isCenterline ? 2.5 : isHovered ? 2 : 1.2;

              return (
                <g
                  key={idx}
                  onMouseEnter={() => setHoveredButtock(idx)}
                  onMouseLeave={() => setHoveredButtock(null)}
                  style={{
                    opacity: 0,
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
                x={toSVG(-lpp / 2 - 22, (candidate.depthM - candidate.draftM - candidate.draftM) / 2)[0] - 50}
                y={toSVG(-lpp / 2 - 22, (candidate.depthM - candidate.draftM - candidate.draftM) / 2)[1] - 9}
                width="70"
                height="18"
                rx="4"
                fill="#ffffff"
                stroke="#8b5cf6"
                strokeWidth="1.5"
                filter="url(#shadow)"
              />
              <text
                x={toSVG(-lpp / 2 - 22, (candidate.depthM - candidate.draftM - candidate.draftM) / 2)[0] - 15}
                y={toSVG(-lpp / 2 - 22, (candidate.depthM - candidate.draftM - candidate.draftM) / 2)[1] + 3}
                textAnchor="middle"
                className="fill-purple-700 dark:fill-purple-400 font-bold"
                style={{ fontSize: "10px" }}
              >
                D = {candidate.depthM.toFixed(2)}m
              </text>
            </>
          )}

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
            <div className="w-5 h-1 bg-gradient-to-r from-gray-800 to-gray-600 rounded"></div>
            <span className="text-gray-700 dark:text-gray-300">Sheerline (Deck)</span>
          </div>
          <div className="flex items-center gap-2 hover:bg-gray-50 dark:hover:bg-gray-700 p-1 rounded transition-colors">
            <div className="w-5 h-0.5 bg-blue-600 shadow-sm"></div>
            <span className="text-gray-700 dark:text-gray-300">Centerline Buttock</span>
          </div>
          <div className="flex items-center gap-2 hover:bg-gray-50 dark:hover:bg-gray-700 p-1 rounded transition-colors">
            <div className="w-5 h-0.5 bg-blue-300"></div>
            <span className="text-gray-700 dark:text-gray-300">
              Buttocks (0-{(candidate.beamM / 2).toFixed(1)}m)
            </span>
          </div>
          <div className="flex items-center gap-2 hover:bg-gray-50 dark:hover:bg-gray-700 p-1 rounded transition-colors">
            <div
              className="w-5 h-0.5 bg-cyan-600"
              style={{
                backgroundImage: "linear-gradient(90deg, #06b6d4 50%, transparent 50%)",
                backgroundSize: "10px 1px",
              }}
            ></div>
            <span className="text-gray-700 dark:text-gray-300">Design Waterline</span>
          </div>
          <div className="pt-2 mt-2 border-t border-gray-200 dark:border-gray-600 text-[10px] text-gray-500">
            💡 Hover buttocks for offset details
          </div>
        </div>

        {/* Dimensions Panel */}
        <div className="absolute bottom-6 left-6 bg-white/95 dark:bg-gray-800/95 backdrop-blur-md rounded-xl shadow-2xl p-4 text-xs border border-gray-200 dark:border-gray-700">
          <div className="font-bold text-gray-900 dark:text-gray-100 mb-2 text-sm">Dimensions</div>
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

