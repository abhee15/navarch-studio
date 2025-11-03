import React, { useMemo } from "react";
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
 * 2D Sections View (Body Plan)
 *
 * Traditional naval architecture body plan:
 * - Forward sections (bow, stations 5-10) shown on RIGHT side
 * - Aft sections (stern, stations 0-5) shown on LEFT side
 * - Vertical centerline in middle
 * - Horizontal waterline
 * - Baseline (keel)
 *
 * Shows transverse sections (hull cross-sections) at different longitudinal positions
 *
 * Coordinate system:
 * - Y: Transverse (starboard = +Y, port = -Y)
 * - Z: Vertical (up = +Z, down = -Z)
 * - X: Longitudinal (not shown in sections)
 */
export const Hull2DSections: React.FC<Hull2DSectionsProps> = ({
  candidate,
  showCenterline = true,
  showWaterline = true,
  showBaseline = true,
  showDimensions = true,
  stationCount = 10,
}) => {
  // Calculate section curves at each station
  const sections = useMemo(() => {
    const beam = candidate.bM;
    const draft = candidate.tM;

    const sectionCurves = [];

    for (let i = 0; i <= stationCount; i++) {
      const stationNum = i;
      const xNorm = (2 * i) / stationCount - 1; // -1 to +1

      // Longitudinal factor from Wigley
      const longitudinalFactor = 1 - xNorm * xNorm;

      if (longitudinalFactor <= 0) {
        // At bow/stern, section is a point
        sectionCurves.push({
          station: stationNum,
          points: [[0, 0]], // Centerline point
          isAft: stationNum <= 5,
        });
        continue;
      }

      const points: [number, number][] = [];
      const numPoints = 40; // Points along section curve

      for (let j = 0; j <= numPoints; j++) {
        const z = -(j / numPoints) * draft; // Vertical: 0 (surface) to -draft (keel)
        const zNorm = z / draft; // 0 to -1

        // Wigley: y = (B/2) * (1 - z²/T²) * (1 - x²)
        const y = (beam / 2) * (1 - zNorm * zNorm) * longitudinalFactor;

        points.push([y, z]); // Half-breadth, height
      }

      sectionCurves.push({
        station: stationNum,
        points,
        isAft: stationNum <= 5, // Stations 0-5 are aft (shown on left)
      });
    }

    return sectionCurves;
  }, [candidate.lppM, candidate.bM, candidate.tM, stationCount]);

  // SVG viewport
  const padding = 60;
  const svgWidth = 600;
  const svgHeight = 500;

  const beam = candidate.bM;
  const draft = candidate.tM;
  const depth = candidate.dM;

  // Scale
  const scaleY = (svgWidth / 2 - padding) / (beam / 2); // Half-breadth scale
  const scaleZ = (svgHeight - 2 * padding) / depth;
  const scale = Math.min(scaleY, scaleZ);

  // Transform: Body plan convention (aft left, forward right)
  const toSVG = (y: number, z: number, isAft: boolean): [number, number] => {
    // Aft sections: mirror to left side (negative y becomes positive X)
    // Forward sections: show on right side (positive y becomes positive X)
    const xOffset = isAft ? -y : y;

    return [
      svgWidth / 2 + xOffset * scale, // Center horizontally
      svgHeight - padding - (z + draft) * scale, // Bottom at baseline
    ];
  };

  // Generate path
  const generateSectionPath = (points: [number, number][], isAft: boolean) => {
    return points
      .map(([y, z], i) => {
        const [sx, sy] = toSVG(y, z, isAft);
        return `${i === 0 ? "M" : "L"} ${sx.toFixed(2)},${sy.toFixed(2)}`;
      })
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
          Sections (Body Plan) - {candidate.hullFamily.replace("_", " ").toUpperCase()}
        </text>

        {/* Centerline (vertical) */}
        {showCenterline && (
          <>
            <line
              x1={svgWidth / 2}
              y1={padding}
              x2={svgWidth / 2}
              y2={svgHeight - padding}
              stroke="#6b7280"
              strokeWidth="1.5"
            />
            <text x={svgWidth / 2 + 5} y={padding + 15} className="fill-gray-500 text-xs">
              CL
            </text>
          </>
        )}

        {/* Baseline (horizontal at keel) */}
        {showBaseline && (
          <>
            <line
              x1={padding}
              y1={toSVG(0, -draft, true)[1]}
              x2={svgWidth - padding}
              y2={toSVG(0, -draft, true)[1]}
              stroke="#6b7280"
              strokeWidth="1.5"
            />
            <text
              x={svgWidth - padding - 25}
              y={toSVG(0, -draft, true)[1] - 5}
              className="fill-gray-500 text-xs"
            >
              BL
            </text>
          </>
        )}

        {/* Waterline (horizontal at design draft) */}
        {showWaterline && (
          <>
            <line
              x1={padding}
              y1={toSVG(0, 0, true)[1]}
              x2={svgWidth - padding}
              y2={toSVG(0, 0, true)[1]}
              stroke="#06b6d4"
              strokeWidth="2"
              strokeDasharray="8,4"
            />
            <text
              x={svgWidth - padding - 35}
              y={toSVG(0, 0, true)[1] - 5}
              className="fill-cyan-600 text-xs font-semibold"
            >
              WL
            </text>
          </>
        )}

        {/* Deck line (horizontal at depth) */}
        <line
          x1={padding}
          y1={toSVG(0, depth - draft, true)[1]}
          x2={svgWidth - padding}
          y2={toSVG(0, depth - draft, true)[1]}
          stroke="#1f2937"
          strokeWidth="1"
          strokeDasharray="4,2"
        />

        {/* Section curves */}
        {sections.map((section) => {
          // Skip midship (station 5) to avoid overlap
          if (section.station === 5) return null;

          const color =
            section.station === 0 || section.station === 10
              ? "#3b82f6" // Darker for AP/FP
              : "#93c5fd"; // Lighter for intermediate
          const strokeWidth = section.station === 0 || section.station === 10 ? 2 : 1;

          return (
            <g key={section.station}>
              <path
                d={generateSectionPath(section.points as [number, number][], section.isAft)}
                fill="none"
                stroke={color}
                strokeWidth={strokeWidth}
                strokeLinecap="round"
              />

              {/* Station label */}
              {section.points.length > 1 && (
                <text
                  x={toSVG(section.points[0][0], section.points[0][1], section.isAft)[0]}
                  y={toSVG(section.points[0][0], section.points[0][1], section.isAft)[1] - 8}
                  textAnchor="middle"
                  className="fill-blue-600 text-[10px] font-medium"
                >
                  {section.station}
                </text>
              )}
            </g>
          );
        })}

        {/* Midship section (station 5) - show on both sides for symmetry */}
        {sections.find((s) => s.station === 5) && (
          <g>
            {/* Right side (as forward) */}
            <path
              d={generateSectionPath(
                sections.find((s) => s.station === 5)!.points as [number, number][],
                false
              )}
              fill="none"
              stroke="#f59e0b"
              strokeWidth="2.5"
              strokeLinecap="round"
            />
            {/* Left side (as aft) */}
            <path
              d={generateSectionPath(
                sections.find((s) => s.station === 5)!.points as [number, number][],
                true
              )}
              fill="none"
              stroke="#f59e0b"
              strokeWidth="2.5"
              strokeLinecap="round"
            />
            <text
              x={svgWidth / 2}
              y={padding + 15}
              textAnchor="middle"
              className="fill-orange-600 text-xs font-semibold"
            >
              ⊥ Midship (Station 5)
            </text>
          </g>
        )}

        {/* Labels for sides */}
        <text x={padding + 40} y={padding + 35} className="fill-gray-500 text-xs font-semibold">
          AFT (Stations 0-4)
        </text>
        <text
          x={svgWidth - padding - 90}
          y={padding + 35}
          className="fill-gray-500 text-xs font-semibold"
        >
          FORWARD (Stations 6-10)
        </text>

        {/* Half-breadth scale */}
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
                <polygon points="0 0, 8 3, 0 6" className="fill-gray-700 dark:fill-gray-300" />
              </marker>
            </defs>

            {/* Half-breadth dimension (from CL to max beam on right side) */}
            <line
              x1={svgWidth / 2}
              y1={svgHeight - padding - 15}
              x2={toSVG(beam / 2, -draft, false)[0]}
              y2={svgHeight - padding - 15}
              className="stroke-gray-700 dark:stroke-gray-300"
              strokeWidth="1"
              markerEnd="url(#arrow-sections)"
            />
            <text
              x={(svgWidth / 2 + toSVG(beam / 2, -draft, false)[0]) / 2}
              y={svgHeight - padding - 20}
              textAnchor="middle"
              className="fill-gray-900 dark:fill-gray-100 text-xs"
            >
              B/2 = {(beam / 2).toFixed(2)} m
            </text>
          </>
        )}
      </svg>

      {/* Legend */}
      <div className="absolute top-6 right-6 bg-white/90 dark:bg-gray-800/90 backdrop-blur-sm rounded-lg shadow-lg p-3 text-xs space-y-1">
        <div className="font-semibold text-gray-900 dark:text-gray-100 mb-2">
          Body Plan Convention
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-0.5 bg-orange-600"></div>
          <span className="text-gray-700 dark:text-gray-300">Midship (⊥)</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-0.5 bg-blue-600"></div>
          <span className="text-gray-700 dark:text-gray-300">AP / FP</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-0.5 bg-blue-300"></div>
          <span className="text-gray-700 dark:text-gray-300">Intermediate Stations</span>
        </div>
        <hr className="my-2 border-gray-300 dark:border-gray-600" />
        <div className="text-[10px] text-gray-600 dark:text-gray-400 space-y-1">
          <div>← Left: Aft (Stations 0-4)</div>
          <div>→ Right: Forward (Stations 6-10)</div>
          <div>Center: Midship overlap</div>
        </div>
      </div>
    </div>
  );
};
