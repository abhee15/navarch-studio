import React, { useMemo } from "react";
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
 * 2D Plan View (Top-Down Projection)
 *
 * Shows:
 * - Waterlines (horizontal sections at different depths)
 * - Stations (vertical transverse sections)
 * - Centerline (longitudinal axis)
 * - Perpendiculars (AP, FP)
 * - Dimensions (Lpp, Lwl, LOA, B)
 * - Maximum beam line
 *
 * Coordinate system:
 * - X: Longitudinal (bow = +X, stern = -X)
 * - Y: Transverse (starboard = +Y, port = -Y)
 * - Z: Vertical (up = +Z, down = -Z) [not shown in plan]
 */
export const Hull2DPlan: React.FC<Hull2DPlanProps> = ({
  candidate,
  showWaterlines = true,
  showStations = true,
  showDimensions = true,
  showCenterline = true,
  waterlineCount = 7,
}) => {
  // Calculate waterline curves using Wigley formula
  const waterlines = useMemo(() => {
    const lpp = candidate.lppM;
    const beam = candidate.bM;
    const draft = candidate.tM;

    const lines = [];

    for (let i = 0; i <= waterlineCount; i++) {
      const z = -(i / waterlineCount) * draft; // Depth: 0 (surface) to -draft (keel)
      const zNorm = z / draft; // Normalized: 0 to -1

      const points: [number, number][] = [];
      const numPoints = 60; // Points along length

      for (let j = 0; j <= numPoints; j++) {
        const x = (j / numPoints) * lpp - lpp / 2; // x: -L/2 to +L/2
        const xNorm = (2 * j) / numPoints - 1; // x: -1 to +1

        // Wigley formula: y = (B/2) * (1 - z²/T²) * (1 - x²)
        const y = (beam / 2) * (1 - zNorm * zNorm) * (1 - xNorm * xNorm);

        points.push([x, y]);
      }

      lines.push({
        depth: Math.abs(z),
        points,
        isDesignWaterline: i === waterlineCount, // Bottom waterline = design draft
      });
    }

    return lines;
  }, [candidate.lppM, candidate.bM, candidate.tM, waterlineCount]);

  // Calculate station positions (vertical sections)
  const stations = useMemo(() => {
    const lpp = candidate.lppM;
    const numStations = 10; // 0 (AP) to 10 (FP)

    const stationPositions = [];
    for (let i = 0; i <= numStations; i++) {
      const x = (i / numStations) * lpp - lpp / 2;
      stationPositions.push({ number: i, x });
    }

    return stationPositions;
  }, [candidate.lppM]);

  // SVG viewport dimensions
  const padding = 60; // Padding for dimensions/labels
  const svgWidth = 800;
  const svgHeight = 400;

  // Calculate scale (fit hull to viewport)
  const lpp = candidate.lppM;
  const beam = candidate.bM;
  const scaleX = (svgWidth - 2 * padding) / lpp;
  const scaleY = (svgHeight - 2 * padding) / beam;
  const scale = Math.min(scaleX, scaleY); // Uniform scale

  // Transform: Convert ship coordinates to SVG coordinates
  const toSVG = (x: number, y: number): [number, number] => {
    return [
      svgWidth / 2 + x * scale, // Center horizontally
      svgHeight / 2 - y * scale, // Center vertically, flip Y (SVG Y down, ship Y up)
    ];
  };

  // Generate SVG path for waterline
  const waterlinePath = (points: [number, number][]) => {
    const svgPoints = points.map(([x, y]) => toSVG(x, y));

    // Create path: M (move to first point) + L (line to subsequent points)
    const path = svgPoints
      .map(([x, y], i) => `${i === 0 ? "M" : "L"} ${x.toFixed(2)},${y.toFixed(2)}`)
      .join(" ");

    // Mirror for port side
    const mirroredPoints = points.map(([x, y]) => toSVG(x, -y));
    const mirrorPath = mirroredPoints
      .map(([x, y], i) => `${i === 0 ? "M" : "L"} ${x.toFixed(2)},${y.toFixed(2)}`)
      .join(" ");

    return { starboard: path, port: mirrorPath };
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
          Plan View (Top-Down) - {candidate.hullFamily.replace("_", " ").toUpperCase()}
        </text>

        {/* Centerline */}
        {showCenterline && (
          <>
            <line
              x1={toSVG(-lpp / 2, 0)[0]}
              y1={toSVG(-lpp / 2, 0)[1]}
              x2={toSVG(lpp / 2, 0)[0]}
              y2={toSVG(lpp / 2, 0)[1]}
              stroke="#9ca3af"
              strokeWidth="1"
              strokeDasharray="5,5"
            />
            <text
              x={toSVG(lpp / 2 + 5, 0)[0]}
              y={toSVG(lpp / 2, 0)[1]}
              className="fill-gray-500 text-xs"
            >
              CL
            </text>
          </>
        )}

        {/* Perpendiculars (AP, FP, Midship) */}
        {showStations && (
          <>
            {/* AP (Aft Perpendicular) */}
            <line
              x1={toSVG(-lpp / 2, -beam / 2)[0]}
              y1={toSVG(-lpp / 2, -beam / 2)[1]}
              x2={toSVG(-lpp / 2, beam / 2)[0]}
              y2={toSVG(-lpp / 2, beam / 2)[1]}
              stroke="#ef4444"
              strokeWidth="2"
            />
            <text
              x={toSVG(-lpp / 2, -beam / 2 - 5)[0]}
              y={toSVG(-lpp / 2, -beam / 2 - 5)[1]}
              textAnchor="middle"
              className="fill-red-600 text-xs font-semibold"
            >
              AP
            </text>

            {/* FP (Forward Perpendicular) */}
            <line
              x1={toSVG(lpp / 2, -beam / 2)[0]}
              y1={toSVG(lpp / 2, -beam / 2)[1]}
              x2={toSVG(lpp / 2, beam / 2)[0]}
              y2={toSVG(lpp / 2, beam / 2)[1]}
              stroke="#10b981"
              strokeWidth="2"
            />
            <text
              x={toSVG(lpp / 2, -beam / 2 - 5)[0]}
              y={toSVG(lpp / 2, -beam / 2 - 5)[1]}
              textAnchor="middle"
              className="fill-green-600 text-xs font-semibold"
            >
              FP
            </text>

            {/* Midship */}
            <line
              x1={toSVG(0, -beam / 2)[0]}
              y1={toSVG(0, -beam / 2)[1]}
              x2={toSVG(0, beam / 2)[0]}
              y2={toSVG(0, beam / 2)[1]}
              stroke="#f59e0b"
              strokeWidth="1.5"
              strokeDasharray="8,4"
            />
            <text
              x={toSVG(0, -beam / 2 - 5)[0]}
              y={toSVG(0, -beam / 2 - 5)[1]}
              textAnchor="middle"
              className="fill-orange-600 text-xs"
            >
              ⊥ (Midship)
            </text>
          </>
        )}

        {/* Station markers (every 10% Lpp) */}
        {showStations &&
          stations.map((station) => {
            const [sx] = toSVG(station.x, 0);
            return (
              <g key={station.number}>
                <line
                  x1={toSVG(station.x, -beam / 2)[0]}
                  y1={toSVG(station.x, -beam / 2)[1]}
                  x2={toSVG(station.x, beam / 2)[0]}
                  y2={toSVG(station.x, beam / 2)[1]}
                  stroke="#d1d5db"
                  strokeWidth="0.5"
                  strokeDasharray="2,2"
                />
                <text
                  x={sx}
                  y={toSVG(station.x, beam / 2 + 3)[1]}
                  textAnchor="middle"
                  className="fill-gray-400 text-[10px]"
                >
                  {station.number}
                </text>
              </g>
            );
          })}

        {/* Waterlines (horizontal sections at different depths) */}
        {showWaterlines &&
          waterlines.map((wl, idx) => {
            const paths = waterlinePath(wl.points);
            const color = wl.isDesignWaterline ? "#3b82f6" : "#60a5fa"; // Design WL darker
            const strokeWidth = wl.isDesignWaterline ? 2 : 1;

            return (
              <g key={idx}>
                {/* Starboard side */}
                <path
                  d={paths.starboard}
                  fill="none"
                  stroke={color}
                  strokeWidth={strokeWidth}
                  strokeLinecap="round"
                />
                {/* Port side */}
                <path
                  d={paths.port}
                  fill="none"
                  stroke={color}
                  strokeWidth={strokeWidth}
                  strokeLinecap="round"
                />

                {/* Label */}
                {idx % 2 === 0 && ( // Label every other waterline to avoid clutter
                  <text
                    x={toSVG(lpp / 2 + 8, 0)[0]}
                    y={toSVG(lpp / 2, wl.points[wl.points.length - 1][1])[1]}
                    className="fill-blue-600 text-[10px]"
                  >
                    WL{idx} ({wl.depth.toFixed(1)}m)
                  </text>
                )}
              </g>
            );
          })}

        {/* Dimensions */}
        {showDimensions && (
          <>
            {/* Lpp dimension (with arrows) */}
            <g>
              <defs>
                <marker
                  id="arrowhead-left"
                  markerWidth="10"
                  markerHeight="7"
                  refX="0"
                  refY="3.5"
                  orient="auto"
                >
                  <polygon
                    points="10 0, 10 7, 0 3.5"
                    className="fill-gray-700 dark:fill-gray-300"
                  />
                </marker>
                <marker
                  id="arrowhead-right"
                  markerWidth="10"
                  markerHeight="7"
                  refX="10"
                  refY="3.5"
                  orient="auto"
                >
                  <polygon points="0 0, 10 3.5, 0 7" className="fill-gray-700 dark:fill-gray-300" />
                </marker>
              </defs>

              {/* Lpp dimension line */}
              <line
                x1={toSVG(-lpp / 2, -beam / 2 - 8)[0]}
                y1={toSVG(-lpp / 2, -beam / 2 - 8)[1]}
                x2={toSVG(lpp / 2, -beam / 2 - 8)[0]}
                y2={toSVG(lpp / 2, -beam / 2 - 8)[1]}
                stroke="#374151"
                strokeWidth="1"
                markerStart="url(#arrowhead-left)"
                markerEnd="url(#arrowhead-right)"
                className="stroke-gray-700 dark:stroke-gray-300"
              />
              <text
                x={svgWidth / 2}
                y={toSVG(0, -beam / 2 - 8)[1] - 8}
                textAnchor="middle"
                className="fill-gray-900 dark:fill-gray-100 text-xs font-semibold"
              >
                Lpp = {lpp.toFixed(2)} m
              </text>
            </g>

            {/* Beam dimension */}
            <g>
              <line
                x1={toSVG(-lpp / 2 - 8, -beam / 2)[0]}
                y1={toSVG(-lpp / 2 - 8, -beam / 2)[1]}
                x2={toSVG(-lpp / 2 - 8, beam / 2)[0]}
                y2={toSVG(-lpp / 2 - 8, beam / 2)[1]}
                stroke="#374151"
                strokeWidth="1"
                markerStart="url(#arrowhead-left)"
                markerEnd="url(#arrowhead-right)"
                className="stroke-gray-700 dark:stroke-gray-300"
              />
              <text
                x={toSVG(-lpp / 2 - 8, 0)[0] - 25}
                y={toSVG(-lpp / 2 - 8, 0)[1] + 4}
                textAnchor="middle"
                className="fill-gray-900 dark:fill-gray-100 text-xs font-semibold"
              >
                B = {beam.toFixed(2)} m
              </text>
            </g>
          </>
        )}

        {/* LCB marker */}
        {candidate.lcbPctLpp && (
          <g>
            <circle
              cx={toSVG((candidate.lcbPctLpp / 100 - 0.5) * lpp, 0)[0]}
              cy={toSVG((candidate.lcbPctLpp / 100 - 0.5) * lpp, 0)[1]}
              r="4"
              fill="#ef4444"
              stroke="#ffffff"
              strokeWidth="1.5"
            />
            <text
              x={toSVG((candidate.lcbPctLpp / 100 - 0.5) * lpp, 0)[0]}
              y={toSVG((candidate.lcbPctLpp / 100 - 0.5) * lpp, 0)[1] - 10}
              textAnchor="middle"
              className="fill-red-600 text-xs font-semibold"
            >
              LCB
            </text>
          </g>
        )}

        {/* Scale ruler */}
        <g>
          <text x={20} y={svgHeight - 10} className="fill-gray-500 text-[10px]">
            Scale: 1:{Math.round(1 / scale)} | Grid: 1m
          </text>
        </g>
      </svg>

      {/* Legend */}
      <div className="absolute top-6 right-6 bg-white/90 dark:bg-gray-800/90 backdrop-blur-sm rounded-lg shadow-lg p-3 text-xs space-y-1">
        <div className="font-semibold text-gray-900 dark:text-gray-100 mb-2">Legend</div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-0.5 bg-blue-600"></div>
          <span className="text-gray-700 dark:text-gray-300">Design Waterline</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-0.5 bg-blue-400"></div>
          <span className="text-gray-700 dark:text-gray-300">Waterlines</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-0.5 bg-red-600"></div>
          <span className="text-gray-700 dark:text-gray-300">AP (Stern)</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-0.5 bg-green-600"></div>
          <span className="text-gray-700 dark:text-gray-300">FP (Bow)</span>
        </div>
        {candidate.lcbPctLpp && (
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded-full bg-red-500"></div>
            <span className="text-gray-700 dark:text-gray-300">
              LCB ({candidate.lcbPctLpp.toFixed(1)}% Lpp)
            </span>
          </div>
        )}
      </div>

      {/* View controls */}
      <div className="absolute bottom-6 left-6 bg-white/90 dark:bg-gray-800/90 backdrop-blur-sm rounded-lg shadow-lg p-3 space-y-2">
        <label className="flex items-center gap-2 text-xs cursor-pointer">
          <input
            type="checkbox"
            checked={showWaterlines}
            onChange={() => {
              /* Handle in parent */
            }}
            className="rounded"
            disabled
          />
          <span className="text-gray-700 dark:text-gray-300">Waterlines ({waterlineCount})</span>
        </label>
        <label className="flex items-center gap-2 text-xs cursor-pointer">
          <input
            type="checkbox"
            checked={showStations}
            onChange={() => {
              /* Handle in parent */
            }}
            className="rounded"
            disabled
          />
          <span className="text-gray-700 dark:text-gray-300">Stations (0-10)</span>
        </label>
        <label className="flex items-center gap-2 text-xs cursor-pointer">
          <input
            type="checkbox"
            checked={showDimensions}
            onChange={() => {
              /* Handle in parent */
            }}
            className="rounded"
            disabled
          />
          <span className="text-gray-700 dark:text-gray-300">Dimensions</span>
        </label>
      </div>
    </div>
  );
};
