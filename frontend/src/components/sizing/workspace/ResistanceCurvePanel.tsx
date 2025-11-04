import { useMemo } from "react";
import type { CandidateDesign } from "../../../types/sizing";

interface ResistanceCurvePanelProps {
  candidate: CandidateDesign;
}

/**
 * Resistance Curve Panel
 *
 * Shows EHP vs Speed curve based on Holtrop-Mennen calculation
 */
export const ResistanceCurvePanel: React.FC<ResistanceCurvePanelProps> = ({ candidate }) => {
  // Generate resistance curve data points
  const curveData = useMemo(() => {
    if (!candidate.ehpKw) return [];

    // Calculate speed from Froude number: V = Fn * sqrt(g * Lwl)
    const g = 9.81;
    const lwl = candidate.lwlM;

    // Generate points from 0.5 Fn to 1.5 Fn
    const points = [];
    for (let fnFactor = 0.5; fnFactor <= 1.5; fnFactor += 0.1) {
      const fn = candidate.fn * fnFactor;
      const speed = fn * Math.sqrt(g * lwl); // m/s
      const speedKn = speed * 1.944;

      // Simple scaling: EHP ∝ V³ (rough approximation)
      const ehp = candidate.ehpKw * Math.pow(fnFactor, 3);

      points.push({ speedKn, ehp, fn, isDesign: Math.abs(fnFactor - 1.0) < 0.05 });
    }

    return points;
  }, [candidate.ehpKw, candidate.fn, candidate.lwlM]);

  if (!candidate.ehpKw) {
    return (
      <div className="rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 p-6 shadow">
        <h3 className="font-semibold text-gray-900 dark:text-white mb-4">Resistance Curve</h3>
        <p className="text-sm text-gray-500 dark:text-gray-400">
          Resistance data not available for this candidate.
        </p>
      </div>
    );
  }

  const maxEhp = Math.max(...curveData.map((d) => d.ehp));
  const maxSpeed = Math.max(...curveData.map((d) => d.speedKn));

  return (
    <div className="rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 overflow-hidden shadow-lg">
      <div className="bg-gradient-to-r from-green-50 to-emerald-50 dark:from-green-900/20 dark:to-emerald-900/20 px-4 py-3 border-b border-gray-200 dark:border-gray-700">
        <h3 className="font-semibold text-gray-900 dark:text-white flex items-center gap-2">
          <span className="text-green-600 dark:text-green-400">⚡</span>
          Resistance Curve
        </h3>
        <p className="text-xs text-gray-600 dark:text-gray-400 mt-1">
          EHP vs Speed (Holtrop-Mennen approximation)
        </p>
      </div>

      <div className="p-6">
        {/* SVG Chart */}
        <svg
          width="100%"
          height="300"
          viewBox="0 0 600 300"
          className="bg-gradient-to-br from-white to-gray-50 dark:from-gray-900 dark:to-gray-800 rounded-lg"
        >
          <defs>
            <linearGradient id="curveGradient" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stopColor="#10b981" />
              <stop offset="100%" stopColor="#059669" />
            </linearGradient>
            <filter id="glow">
              <feGaussianBlur stdDeviation="2" result="coloredBlur" />
              <feMerge>
                <feMergeNode in="coloredBlur" />
                <feMergeNode in="SourceGraphic" />
              </feMerge>
            </filter>
          </defs>

          {/* Grid */}
          <g stroke="#e5e7eb" strokeWidth="1" opacity="0.3">
            {[0, 1, 2, 3, 4].map((i) => (
              <line key={`h${i}`} x1="50" y1={50 + i * 50} x2="550" y2={50 + i * 50} />
            ))}
            {[0, 1, 2, 3, 4, 5].map((i) => (
              <line key={`v${i}`} x1={50 + i * 100} y1="50" x2={50 + i * 100} y2="250" />
            ))}
          </g>

          {/* Axes */}
          <line x1="50" y1="250" x2="550" y2="250" stroke="#374151" strokeWidth="2" />
          <line x1="50" y1="50" x2="50" y2="250" stroke="#374151" strokeWidth="2" />

          {/* Y-axis label */}
          <text
            x="20"
            y="150"
            fill="#6b7280"
            fontSize="12"
            textAnchor="middle"
            transform="rotate(-90, 20, 150)"
          >
            EHP (kW)
          </text>

          {/* X-axis label */}
          <text x="300" y="280" fill="#6b7280" fontSize="12" textAnchor="middle">
            Speed (knots)
          </text>

          {/* Y-axis ticks and labels */}
          {[0, 1, 2, 3, 4].map((i) => {
            const value = ((4 - i) / 4) * maxEhp;
            return (
              <g key={`ytick${i}`}>
                <line
                  x1="45"
                  y1={50 + i * 50}
                  x2="50"
                  y2={50 + i * 50}
                  stroke="#374151"
                  strokeWidth="2"
                />
                <text x="40" y={50 + i * 50 + 4} fill="#6b7280" fontSize="10" textAnchor="end">
                  {value.toFixed(0)}
                </text>
              </g>
            );
          })}

          {/* X-axis ticks and labels */}
          {[0, 1, 2, 3, 4, 5].map((i) => {
            const value = (i / 5) * maxSpeed;
            return (
              <g key={`xtick${i}`}>
                <line
                  x1={50 + i * 100}
                  y1="250"
                  x2={50 + i * 100}
                  y2="255"
                  stroke="#374151"
                  strokeWidth="2"
                />
                <text x={50 + i * 100} y="270" fill="#6b7280" fontSize="10" textAnchor="middle">
                  {value.toFixed(1)}
                </text>
              </g>
            );
          })}

          {/* Resistance curve */}
          <path
            d={curveData
              .map((d, i) => {
                const x = 50 + (d.speedKn / maxSpeed) * 500;
                const y = 250 - (d.ehp / maxEhp) * 200;
                return `${i === 0 ? "M" : "L"} ${x},${y}`;
              })
              .join(" ")}
            stroke="url(#curveGradient)"
            strokeWidth="3"
            fill="none"
            filter="url(#glow)"
          />

          {/* Data points */}
          {curveData.map((d, i) => {
            const x = 50 + (d.speedKn / maxSpeed) * 500;
            const y = 250 - (d.ehp / maxEhp) * 200;

            return (
              <g key={i}>
                <circle
                  cx={x}
                  cy={y}
                  r={d.isDesign ? 6 : 4}
                  fill={d.isDesign ? "#f59e0b" : "#10b981"}
                  stroke="white"
                  strokeWidth="2"
                />
                {d.isDesign && (
                  <>
                    <circle
                      cx={x}
                      cy={y}
                      r="8"
                      fill="none"
                      stroke="#f59e0b"
                      strokeWidth="1"
                      opacity="0.5"
                    >
                      <animate
                        attributeName="r"
                        from="8"
                        to="12"
                        dur="1.5s"
                        repeatCount="indefinite"
                      />
                      <animate
                        attributeName="opacity"
                        from="0.5"
                        to="0"
                        dur="1.5s"
                        repeatCount="indefinite"
                      />
                    </circle>
                    <text
                      x={x}
                      y={y - 15}
                      fill="#f59e0b"
                      fontSize="10"
                      fontWeight="bold"
                      textAnchor="middle"
                    >
                      Design Point
                    </text>
                  </>
                )}
              </g>
            );
          })}
        </svg>

        {/* Legend */}
        <div className="mt-4 flex items-center justify-center gap-6 text-sm">
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded-full bg-gradient-to-r from-green-500 to-emerald-600"></div>
            <span className="text-gray-700 dark:text-gray-300">Resistance Curve</span>
          </div>
          <div className="flex items-center gap-2">
            <div className="w-3 h-3 rounded-full bg-orange-500 ring-2 ring-orange-300"></div>
            <span className="text-gray-700 dark:text-gray-300">Design Speed</span>
          </div>
        </div>

        {/* Key Metrics */}
        <div className="mt-4 grid grid-cols-2 gap-4 p-4 bg-gray-50 dark:bg-gray-900 rounded-lg">
          <div>
            <p className="text-xs text-gray-500 dark:text-gray-400">Design Speed</p>
            <p className="text-lg font-bold text-gray-900 dark:text-white">
              {candidate.fn && candidate.lwlM
                ? (candidate.fn * Math.sqrt(9.81 * candidate.lwlM) * 1.944).toFixed(1)
                : "N/A"}{" "}
              kn
            </p>
          </div>
          <div>
            <p className="text-xs text-gray-500 dark:text-gray-400">EHP @ Design Speed</p>
            <p className="text-lg font-bold text-green-600 dark:text-green-400">
              {candidate.ehpKw?.toFixed(0) || "N/A"} kW
            </p>
          </div>
          <div>
            <p className="text-xs text-gray-500 dark:text-gray-400">SHP (est. η=0.65)</p>
            <p className="text-lg font-bold text-orange-600 dark:text-orange-400">
              {candidate.shpKw?.toFixed(0) || "N/A"} kW
            </p>
          </div>
          <div>
            <p className="text-xs text-gray-500 dark:text-gray-400">Froude Number</p>
            <p className="text-lg font-bold text-blue-600 dark:text-blue-400">
              {candidate.fn?.toFixed(3) || "N/A"}
            </p>
          </div>
        </div>

        <div className="mt-4 text-xs text-gray-500 dark:text-gray-400 bg-yellow-50 dark:bg-yellow-900/20 p-3 rounded">
          <p className="font-medium text-yellow-800 dark:text-yellow-300 mb-1">⚠️ Note:</p>
          <p className="text-yellow-700 dark:text-yellow-400">
            This is a simplified approximation based on Holtrop-Mennen with EHP ∝ V³ scaling. For
            accurate resistance analysis, push this design to the Resistance module.
          </p>
        </div>
      </div>
    </div>
  );
};

