import { useMemo } from "react";
import type { CandidateDesign } from "../../../types/sizing";

interface OffsetsTableProps {
  candidate: CandidateDesign;
  stationCount?: number;
  waterlineCount?: number;
}

/**
 * Offsets Table - Traditional Naval Architecture Table of Offsets
 *
 * Shows half-breadths at each station and waterline intersection
 * Professional engineering format
 */
export const OffsetsTable: React.FC<OffsetsTableProps> = ({
  candidate,
  stationCount = 11,
  waterlineCount = 7,
}) => {
  // Generate offsets using Wigley hull form
  const offsets = useMemo(() => {
    const lpp = candidate.lppM;
    const beam = candidate.bM;
    const draft = candidate.tM;

    const stations = Array.from({ length: stationCount }, (_, i) => i / (stationCount - 1));
    const waterlines = Array.from({ length: waterlineCount }, (_, i) => i / (waterlineCount - 1));

    return waterlines.map((wlFraction) => {
      const z = wlFraction * draft; // Depth from keel

      return stations.map((stationFraction) => {
        const x = stationFraction * lpp;

        // Wigley form: y = (B/2) * (1 - z²/T²) * (1 - (2x/L - 1)²)
        const zTerm = 1 - Math.pow(z / draft, 2);
        const xTerm = 1 - Math.pow(2 * (x / lpp) - 1, 2);
        const halfBreadth = (beam / 2) * zTerm * xTerm;

        return halfBreadth;
      });
    });
  }, [candidate.lppM, candidate.bM, candidate.tM, stationCount, waterlineCount]);

  const stationLabels = Array.from({ length: stationCount }, (_, i) => i);
  const waterlineLabels = Array.from({ length: waterlineCount }, (_, i) =>
    ((i / (waterlineCount - 1)) * candidate.tM).toFixed(2)
  );

  return (
    <div className="w-full overflow-auto">
      <div className="rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 shadow-lg">
        {/* Header */}
        <div className="border-b border-gray-300 dark:border-gray-600 bg-gradient-to-r from-slate-50 to-gray-50 dark:from-slate-900 dark:to-gray-900 p-4">
          <h3 className="font-bold text-gray-900 dark:text-white flex items-center gap-2">
            <span className="text-blue-600 dark:text-blue-400">📊</span>
            Table of Offsets
          </h3>
          <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
            Half-breadths (m) at stations and waterlines
          </p>
        </div>

        {/* Table */}
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-gray-300 dark:divide-gray-600">
            <thead className="bg-gray-50 dark:bg-gray-900">
              <tr>
                <th className="sticky left-0 z-10 bg-gray-100 dark:bg-gray-800 px-4 py-3 text-left text-xs font-bold text-gray-700 dark:text-gray-300 border-r-2 border-gray-300 dark:border-gray-600">
                  WL (m)
                </th>
                {stationLabels.map((station) => (
                  <th
                    key={station}
                    className="px-4 py-3 text-center text-xs font-bold text-gray-700 dark:text-gray-300"
                  >
                    {station === 0
                      ? "AP"
                      : station === Math.floor(stationCount / 2)
                        ? "⚓"
                        : station === stationCount - 1
                          ? "FP"
                          : station}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-700 bg-white dark:bg-gray-800">
              {offsets.map((row, wlIdx) => (
                <tr
                  key={wlIdx}
                  className={`hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-colors ${
                    wlIdx === waterlineCount - 1
                      ? "bg-cyan-50 dark:bg-cyan-900/10 font-semibold"
                      : ""
                  }`}
                >
                  <td className="sticky left-0 z-10 bg-gray-50 dark:bg-gray-800 px-4 py-3 text-sm font-bold text-gray-900 dark:text-white border-r-2 border-gray-300 dark:border-gray-600">
                    {wlIdx === waterlineCount - 1 ? (
                      <span className="text-cyan-600 dark:text-cyan-400">
                        DWL: {waterlineLabels[wlIdx]}
                      </span>
                    ) : (
                      waterlineLabels[wlIdx]
                    )}
                  </td>
                  {row.map((offset, stIdx) => (
                    <td
                      key={stIdx}
                      className={`px-4 py-3 text-center text-sm tabular-nums ${
                        stIdx === Math.floor(stationCount / 2)
                          ? "bg-orange-50 dark:bg-orange-900/10 font-semibold text-orange-900 dark:text-orange-300"
                          : "text-gray-900 dark:text-white"
                      }`}
                    >
                      {offset.toFixed(3)}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        {/* Footer Legend */}
        <div className="border-t border-gray-300 dark:border-gray-600 bg-gray-50 dark:bg-gray-900 p-3">
          <div className="flex flex-wrap items-center gap-4 text-xs text-gray-600 dark:text-gray-400">
            <div className="flex items-center gap-2">
              <span className="font-bold">AP</span> = Aft Perpendicular
            </div>
            <div className="flex items-center gap-2">
              <span>⚓</span> = Midship
            </div>
            <div className="flex items-center gap-2">
              <span className="font-bold">FP</span> = Forward Perpendicular
            </div>
            <div className="flex items-center gap-2">
              <span className="text-cyan-600 dark:text-cyan-400 font-bold">DWL</span> = Design
              Waterline
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
