import React, { useMemo } from "react";
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
 * 2D Profile View (Side Elevation)
 *
 * Shows:
 * - Sheerline (deck line from bow to stern)
 * - Keel line (bottom of hull)
 * - Buttocks (vertical longitudinal sections at different transverse positions)
 * - Waterline (at design draft)
 * - Baseline (reference line at keel)
 * - Dimensions (Lpp, Lwl, LOA, T, D, freeboard)
 * - Perpendiculars (AP, FP)
 *
 * Coordinate system:
 * - X: Longitudinal (bow = +X, stern = -X)
 * - Y: Vertical (up = +Y, down = -Y)
 * - Z: Transverse (not shown in profile)
 */
export const Hull2DProfile: React.FC<Hull2DProfileProps> = ({
  candidate,
  showButtocks = true,
  showWaterline = true,
  showDimensions = true,
  showBaseline = true,
  buttockCount = 5,
}) => {
  // Calculate buttock curves (vertical longitudinal sections)
  const buttocks = useMemo(() => {
    const lpp = candidate.lppM;
    const beam = candidate.bM;
    const draft = candidate.tM;

    const lines = [];

    for (let i = 0; i <= buttockCount; i++) {
      // y positions from centerline to max beam: 0, 0.25B, 0.5B, 0.75B, B
      const yPos = (i / buttockCount) * (beam / 2);

      const points: [number, number][] = [];
      const numPoints = 60;

      for (let j = 0; j <= numPoints; j++) {
        const x = (j / numPoints) * lpp - lpp / 2; // x: -L/2 to +L/2
        const xNorm = (2 * j) / numPoints - 1; // x: -1 to +1

        // For each x, calculate the vertical position of the buttock at transverse offset yPos
        // Wigley: y_max(x,z) = (B/2) * (1 - z²/T²) * (1 - x²)
        // For buttock at yPos, solve for z:
        // yPos = (B/2) * (1 - z²/T²) * (1 - x²)
        // yPos / [(B/2) * (1 - x²)] = 1 - z²/T²
        // z²/T² = 1 - yPos / [(B/2) * (1 - x²)]

        const longitudinalFactor = 1 - xNorm * xNorm;

        if (longitudinalFactor <= 0) {
          // At bow/stern (x = ±L/2), hull narrows to zero
          points.push([x, 0]); // Surface
          continue;
        }

        const maxYAtX = (beam / 2) * longitudinalFactor;

        if (yPos > maxYAtX) {
          // Buttock is outside hull at this x position
          points.push([x, 0]); // Surface
          continue;
        }

        // Solve for z
        const ratio = yPos / maxYAtX;
        const zOverT = Math.sqrt(Math.max(0, 1 - ratio));
        const z = -draft * zOverT; // Negative (below waterline)

        points.push([x, z]);
      }

      lines.push({
        transverseOffset: yPos,
        points,
        isCenterline: i === 0,
      });
    }

    return lines;
  }, [candidate.lppM, candidate.bM, candidate.tM, candidate.dM, buttockCount]);

  // Sheerline (deck line) - approximate as flat for now
  const sheerline = useMemo(() => {
    const lpp = candidate.lppM;
    const depth = candidate.dM;
    const draft = candidate.tM;
    const freeboard = depth - draft;

    const points: [number, number][] = [];
    const numPoints = 60;

    for (let i = 0; i <= numPoints; i++) {
      const x = (i / numPoints) * lpp - lpp / 2;
      // Simplified: flat deck with slight sheer at ends (optional)
      const sheer = 0; // Could add: 0.01 * lpp * (1 - (2*x/lpp)²) for realistic sheer
      const y = freeboard + sheer;
      points.push([x, y]);
    }

    return points;
  }, [candidate.lppM, candidate.dM, candidate.tM]);

  // SVG viewport
  const padding = 80;
  const svgWidth = 900;
  const svgHeight = 400;

  const lpp = candidate.lppM;
  const depth = candidate.dM;

  // Scale to fit
  const scaleX = (svgWidth - 2 * padding) / lpp;
  const scaleY = (svgHeight - 2 * padding) / depth;
  const scale = Math.min(scaleX, scaleY);

  // Transform
  const toSVG = (x: number, y: number): [number, number] => {
    return [
      svgWidth / 2 + x * scale,
      svgHeight - padding - (y + candidate.tM) * scale, // Baseline at bottom
    ];
  };

  // Generate SVG path
  const generatePath = (points: [number, number][]) => {
    return points
      .map(
        ([x, y], i) =>
          `${i === 0 ? "M" : "L"} ${toSVG(x, y)[0].toFixed(2)},${toSVG(x, y)[1].toFixed(2)}`
      )
      .join(" ");
  };

  return (
    <div className="w-full h-full bg-white dark:bg-gray-900 p-4 relative">
      <svg
        width="100%"
        height="100%"
        viewBox={`0 0 ${svgWidth} ${svgHeight}`}
        className="border border-gray-300 dark:border-gray-700 rounded"
      >
        {/* Title */}
        <text
          x={svgWidth / 2}
          y={20}
          textAnchor="middle"
          className="fill-gray-900 dark:fill-gray-100 text-sm font-semibold"
        >
          Profile View (Side Elevation) - {candidate.hullFamily.replace("_", " ").toUpperCase()}
        </text>

        {/* Baseline (keel reference line) */}
        {showBaseline && (
          <>
            <line
              x1={toSVG(-lpp / 2, -candidate.tM)[0]}
              y1={toSVG(-lpp / 2, -candidate.tM)[1]}
              x2={toSVG(lpp / 2, -candidate.tM)[0]}
              y2={toSVG(lpp / 2, -candidate.tM)[1]}
              stroke="#6b7280"
              strokeWidth="1.5"
            />
            <text
              x={toSVG(lpp / 2 + 5, -candidate.tM)[0]}
              y={toSVG(lpp / 2, -candidate.tM)[1] + 4}
              className="fill-gray-500 text-xs"
            >
              BL (Baseline)
            </text>
          </>
        )}

        {/* Perpendiculars */}
        <>
          {/* AP */}
          <line
            x1={toSVG(-lpp / 2, -candidate.tM)[0]}
            y1={toSVG(-lpp / 2, -candidate.tM)[1]}
            x2={toSVG(-lpp / 2, candidate.dM - candidate.tM)[0]}
            y2={toSVG(-lpp / 2, candidate.dM - candidate.tM)[1]}
            stroke="#ef4444"
            strokeWidth="2"
          />
          <text
            x={toSVG(-lpp / 2, candidate.dM - candidate.tM)[0] - 10}
            y={toSVG(-lpp / 2, candidate.dM - candidate.tM)[1] - 5}
            className="fill-red-600 text-xs font-semibold"
          >
            AP
          </text>

          {/* FP */}
          <line
            x1={toSVG(lpp / 2, -candidate.tM)[0]}
            y1={toSVG(lpp / 2, -candidate.tM)[1]}
            x2={toSVG(lpp / 2, candidate.dM - candidate.tM)[0]}
            y2={toSVG(lpp / 2, candidate.dM - candidate.tM)[1]}
            stroke="#10b981"
            strokeWidth="2"
          />
          <text
            x={toSVG(lpp / 2, candidate.dM - candidate.tM)[0] + 10}
            y={toSVG(lpp / 2, candidate.dM - candidate.tM)[1] - 5}
            className="fill-green-600 text-xs font-semibold"
          >
            FP
          </text>
        </>

        {/* Waterline (design draft) */}
        {showWaterline && (
          <>
            <line
              x1={toSVG(-lpp / 2, 0)[0]}
              y1={toSVG(-lpp / 2, 0)[1]}
              x2={toSVG(lpp / 2, 0)[0]}
              y2={toSVG(lpp / 2, 0)[1]}
              stroke="#06b6d4"
              strokeWidth="2"
              strokeDasharray="8,4"
            />
            <text
              x={toSVG(lpp / 2 + 5, 0)[0]}
              y={toSVG(lpp / 2, 0)[1] + 4}
              className="fill-cyan-600 text-xs font-semibold"
            >
              Design WL (T = {candidate.tM.toFixed(2)}m)
            </text>
          </>
        )}

        {/* Sheerline (deck line) */}
        <path
          d={generatePath(sheerline)}
          fill="none"
          stroke="#1f2937"
          strokeWidth="2.5"
          strokeLinecap="round"
        />

        {/* Buttocks (vertical longitudinal sections) */}
        {showButtocks &&
          buttocks.map((buttock, idx) => {
            const color = buttock.isCenterline ? "#3b82f6" : "#93c5fd";
            const strokeWidth = buttock.isCenterline ? 2 : 1;

            return (
              <g key={idx}>
                <path
                  d={generatePath(buttock.points)}
                  fill="none"
                  stroke={color}
                  strokeWidth={strokeWidth}
                  strokeLinecap="round"
                />

                {/* Label */}
                {idx % 2 === 0 && (
                  <text
                    x={toSVG(lpp / 2 + 5, buttock.points[buttock.points.length - 1][1])[0]}
                    y={toSVG(lpp / 2, buttock.points[buttock.points.length - 1][1])[1] + 4}
                    className="fill-blue-600 text-[10px]"
                  >
                    BL{idx} ({buttock.transverseOffset.toFixed(1)}m)
                  </text>
                )}
              </g>
            );
          })}

        {/* Dimensions */}
        {showDimensions && (
          <>
            <defs>
              <marker
                id="arrow-left-profile"
                markerWidth="10"
                markerHeight="7"
                refX="0"
                refY="3.5"
                orient="auto"
              >
                <polygon points="10 0, 10 7, 0 3.5" className="fill-gray-700 dark:fill-gray-300" />
              </marker>
              <marker
                id="arrow-right-profile"
                markerWidth="10"
                markerHeight="7"
                refX="10"
                refY="3.5"
                orient="auto"
              >
                <polygon points="0 0, 10 3.5, 0 7" className="fill-gray-700 dark:fill-gray-300" />
              </marker>
            </defs>

            {/* Lpp dimension */}
            <line
              x1={toSVG(-lpp / 2, -candidate.tM - 5)[0]}
              y1={toSVG(-lpp / 2, -candidate.tM - 5)[1]}
              x2={toSVG(lpp / 2, -candidate.tM - 5)[0]}
              y2={toSVG(lpp / 2, -candidate.tM - 5)[1]}
              className="stroke-gray-700 dark:stroke-gray-300"
              strokeWidth="1"
              markerStart="url(#arrow-left-profile)"
              markerEnd="url(#arrow-right-profile)"
            />
            <text
              x={svgWidth / 2}
              y={toSVG(0, -candidate.tM - 5)[1] + 15}
              textAnchor="middle"
              className="fill-gray-900 dark:fill-gray-100 text-xs font-semibold"
            >
              Lpp = {lpp.toFixed(2)} m
            </text>

            {/* Draft dimension (vertical) */}
            <line
              x1={toSVG(-lpp / 2 - 10, -candidate.tM)[0]}
              y1={toSVG(-lpp / 2 - 10, -candidate.tM)[1]}
              x2={toSVG(-lpp / 2 - 10, 0)[0]}
              y2={toSVG(-lpp / 2 - 10, 0)[1]}
              className="stroke-gray-700 dark:stroke-gray-300"
              strokeWidth="1"
              markerStart="url(#arrow-left-profile)"
              markerEnd="url(#arrow-right-profile)"
            />
            <text
              x={toSVG(-lpp / 2 - 10, -candidate.tM / 2)[0] - 35}
              y={toSVG(-lpp / 2 - 10, -candidate.tM / 2)[1] + 4}
              textAnchor="middle"
              className="fill-gray-900 dark:fill-gray-100 text-xs font-semibold"
            >
              T = {candidate.tM.toFixed(2)} m
            </text>

            {/* Depth dimension */}
            <line
              x1={toSVG(-lpp / 2 - 20, -candidate.tM)[0]}
              y1={toSVG(-lpp / 2 - 20, -candidate.tM)[1]}
              x2={toSVG(-lpp / 2 - 20, candidate.dM - candidate.tM)[0]}
              y2={toSVG(-lpp / 2 - 20, candidate.dM - candidate.tM)[1]}
              className="stroke-gray-700 dark:stroke-gray-300"
              strokeWidth="1"
              markerStart="url(#arrow-left-profile)"
              markerEnd="url(#arrow-right-profile)"
            />
            <text
              x={toSVG(-lpp / 2 - 20, (candidate.dM - candidate.tM - candidate.tM) / 2)[0] - 35}
              y={toSVG(-lpp / 2 - 20, (candidate.dM - candidate.tM - candidate.tM) / 2)[1] + 4}
              textAnchor="middle"
              className="fill-gray-900 dark:fill-gray-100 text-xs font-semibold"
            >
              D = {candidate.dM.toFixed(2)} m
            </text>

            {/* Freeboard annotation */}
            <text
              x={toSVG(-lpp / 2 - 10, (candidate.dM - candidate.tM) / 2)[0] - 35}
              y={toSVG(-lpp / 2 - 10, (candidate.dM - candidate.tM) / 2)[1] + 4}
              textAnchor="middle"
              className="fill-green-700 dark:fill-green-400 text-[10px]"
            >
              FB = {(candidate.dM - candidate.tM).toFixed(2)}m
            </text>
          </>
        )}

        {/* Scale ruler */}
        <text x={20} y={svgHeight - 10} className="fill-gray-500 text-[10px]">
          Scale: 1:{Math.round(1 / scale)}
        </text>
      </svg>

      {/* Legend */}
      <div className="absolute top-6 right-6 bg-white/90 dark:bg-gray-800/90 backdrop-blur-sm rounded-lg shadow-lg p-3 text-xs space-y-1">
        <div className="font-semibold text-gray-900 dark:text-gray-100 mb-2">Legend</div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-0.5 bg-gray-800 dark:bg-gray-200"></div>
          <span className="text-gray-700 dark:text-gray-300">Sheerline (Deck)</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-0.5 bg-blue-600"></div>
          <span className="text-gray-700 dark:text-gray-300">Centerline Buttock</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-0.5 bg-blue-300"></div>
          <span className="text-gray-700 dark:text-gray-300">
            Buttocks (0-{(candidate.bM / 2).toFixed(1)}m)
          </span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-0.5 bg-cyan-600" style={{ strokeDasharray: "4,2" }}></div>
          <span className="text-gray-700 dark:text-gray-300">Waterline (Design)</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-0.5 bg-gray-500"></div>
          <span className="text-gray-700 dark:text-gray-300">Baseline (Keel)</span>
        </div>
      </div>

      {/* Dimensions summary */}
      <div className="absolute bottom-6 left-6 bg-white/90 dark:bg-gray-800/90 backdrop-blur-sm rounded-lg shadow-lg p-3 text-xs space-y-1">
        <div className="font-semibold text-gray-900 dark:text-gray-100 mb-1">Dimensions</div>
        <div className="grid grid-cols-2 gap-x-3 gap-y-1">
          <span className="text-gray-600 dark:text-gray-400">Lpp:</span>
          <span className="font-medium text-gray-900 dark:text-gray-100">{lpp.toFixed(2)} m</span>
          <span className="text-gray-600 dark:text-gray-400">Lwl:</span>
          <span className="font-medium text-gray-900 dark:text-gray-100">
            {candidate.lwlM.toFixed(2)} m
          </span>
          <span className="text-gray-600 dark:text-gray-400">LOA:</span>
          <span className="font-medium text-gray-900 dark:text-gray-100">
            {candidate.loaM.toFixed(2)} m
          </span>
          <span className="text-gray-600 dark:text-gray-400">Draft:</span>
          <span className="font-medium text-gray-900 dark:text-gray-100">
            {candidate.tM.toFixed(2)} m
          </span>
          <span className="text-gray-600 dark:text-gray-400">Depth:</span>
          <span className="font-medium text-gray-900 dark:text-gray-100">
            {candidate.dM.toFixed(2)} m
          </span>
          <span className="text-gray-600 dark:text-gray-400">Freeboard:</span>
          <span className="font-medium text-green-700 dark:text-green-400">
            {(candidate.dM - candidate.tM).toFixed(2)} m
          </span>
        </div>
      </div>
    </div>
  );
};
