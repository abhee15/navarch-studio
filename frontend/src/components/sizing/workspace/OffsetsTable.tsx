import { useMemo } from "react";
import type { CandidateDesign } from "../../../types/sizing";
import { Table } from "lucide-react";

interface OffsetsTableProps {
  candidate: CandidateDesign;
  stationCount?: number;
  waterlineCount?: number;
}

interface OffsetsGrid {
  stations: number[];
  waterlines: number[];
  offsets: number[][]; // [stationIndex][waterlineIndex]
}

/**
 * Offsets Table - Traditional Naval Architecture Table of Offsets
 *
 * Shows half-breadths at each station and waterline intersection
 * Professional engineering format
 * Uses actual generated geometry if available, otherwise falls back to Wigley formula
 */
export const OffsetsTable: React.FC<OffsetsTableProps> = ({
  candidate,
  stationCount,
  waterlineCount,
}) => {
  // Try to parse geometry from GeometryJson
  const geometryData = useMemo<OffsetsGrid | null>(() => {
    if (!candidate.geometryJson) return null;
    try {
      const parsed = JSON.parse(candidate.geometryJson);
      if (parsed.stations && parsed.waterlines && parsed.offsets) {
        return {
          stations: parsed.stations,
          waterlines: parsed.waterlines,
          offsets: parsed.offsets,
        };
      }
    } catch (e) {
      console.warn("Failed to parse geometryJson:", e);
    }
    return null;
  }, [candidate.geometryJson]);

  // Use actual geometry if available, otherwise generate using Wigley formula
  const { offsets, waterlines, stationLabels, waterlineLabels } = useMemo(() => {
    if (geometryData) {
      // Use actual generated geometry

      // Transpose offsets: geometry has [stationIndex][waterlineIndex], but table needs [waterlineIndex][stationIndex]
      const transposedOffsets = geometryData.waterlines.map((_, wlIdx) =>
        geometryData.stations.map((_, stIdx) => geometryData.offsets[stIdx]?.[wlIdx] ?? 0)
      );

      // Create station labels (AP, 1, 2, ..., ⚓, ..., FP)
      const labels = geometryData.stations.map((_, idx) => {
        if (idx === 0) return "AP";
        if (idx === Math.floor(actualStationCount / 2)) return "⚓︎";
        if (idx === actualStationCount - 1) return "FP";
        return idx.toString();
      });

      return {
        offsets: transposedOffsets,
        stations: geometryData.stations,
        waterlines: geometryData.waterlines,
        stationLabels: labels,
        waterlineLabels: geometryData.waterlines.map((z) => z.toFixed(2)),
      };
    }

    // Fallback: Generate using Wigley hull form
    const effectiveStationCount = stationCount ?? 23; // Match BSRA-compatible generation
    const effectiveWaterlineCount = waterlineCount ?? 13; // Match generation

    const lpp = candidate.lppM;
    const beam = candidate.beamM;
    const draft = candidate.draftM;

    const stationFractions = Array.from(
      { length: effectiveStationCount },
      (_, i) => i / (effectiveStationCount - 1)
    );
    const waterlineFractions = Array.from(
      { length: effectiveWaterlineCount },
      (_, i) => i / (effectiveWaterlineCount - 1)
    );

    const generatedOffsets = waterlineFractions.map((wlFraction) => {
      const z = wlFraction * draft; // Depth from keel

      return stationFractions.map((stationFraction) => {
        const x = stationFraction * lpp;

        // Wigley form: y = (B/2) * (1 - z²/T²) * (1 - (2x/L - 1)²)
        const zTerm = 1 - Math.pow(z / draft, 2);
        const xTerm = 1 - Math.pow(2 * (x / lpp) - 1, 2);
        const halfBreadth = (beam / 2) * zTerm * xTerm;

        return halfBreadth;
      });
    });

    const labels = stationFractions.map((_, idx) => {
      if (idx === 0) return "AP";
      if (idx === Math.floor(effectiveStationCount / 2)) return "⚓︎";
      if (idx === effectiveStationCount - 1) return "FP";
      return idx.toString();
    });

    return {
      offsets: generatedOffsets,
      stations: stationFractions.map((f) => f * lpp),
      waterlines: waterlineFractions.map((f) => f * draft),
      stationLabels: labels,
      waterlineLabels: waterlineFractions.map((f) => (f * draft).toFixed(2)),
    };
  }, [
    geometryData,
    candidate.lppM,
    candidate.beamM,
    candidate.draftM,
    stationCount,
    waterlineCount,
  ]);

  const effectiveWaterlineCount = waterlines.length;

  return (
    <div className="w-full overflow-auto">
      <div className="rounded-lg border border-border bg-card shadow-lg">
        {/* Header */}
        <div className="border-b border-border bg-gradient-to-r from-slate-50 to-gray-50 dark:from-slate-900 dark:to-gray-900 p-4">
          <h3 className="font-bold text-gray-900 dark:text-white flex items-center gap-2">
            <Table className="h-4 w-4 text-primary" />
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
                <th className="sticky left-0 z-10 bg-gray-100 dark:bg-gray-800 px-4 py-3 text-left text-xs font-bold text-gray-700 dark:text-gray-300 border-r-2 border-border">
                  WL (m)
                </th>
                {stationLabels.map((station, idx) => (
                  <th
                    key={idx}
                    className="px-4 py-3 text-center text-xs font-bold text-gray-700 dark:text-gray-300"
                  >
                    {station}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200 dark:divide-gray-700 bg-card">
              {offsets.map((row, wlIdx) => {
                // Check if this is the design waterline (closest to design draft)
                const designDraft = candidate.draftM;
                const isDesignWaterline =
                  Math.abs(waterlines[wlIdx] - designDraft) < 0.01 ||
                  wlIdx === effectiveWaterlineCount - 1;

                return (
                  <tr
                    key={wlIdx}
                    className={`hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-colors ${
                      isDesignWaterline ? "bg-cyan-50 dark:bg-cyan-900/10 font-semibold" : ""
                    }`}
                  >
                    <td className="sticky left-0 z-10 bg-gray-50 dark:bg-gray-800 px-4 py-3 text-sm font-bold text-gray-900 dark:text-white border-r-2 border-border">
                      {isDesignWaterline ? (
                        <span className="text-cyan-600 dark:text-cyan-400">
                          DWL: {waterlineLabels[wlIdx]}
                        </span>
                      ) : (
                        waterlineLabels[wlIdx]
                      )}
                    </td>
                    {row.map((offset, stIdx) => {
                      const isMidship = stationLabels[stIdx] === "⚓︎";
                      return (
                        <td
                          key={stIdx}
                          className={`px-4 py-3 text-center text-sm tabular-nums ${
                            isMidship
                              ? "bg-orange-50 dark:bg-orange-900/10 font-semibold text-orange-900 dark:text-orange-300"
                              : "text-gray-900 dark:text-white"
                          }`}
                        >
                          {offset.toFixed(3)}
                        </td>
                      );
                    })}
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>

        {/* Footer Legend */}
        <div className="border-t border-border bg-gray-50 dark:bg-gray-900 p-3">
          <div className="flex flex-wrap items-center gap-4 text-xs text-gray-600 dark:text-gray-400">
            <div className="flex items-center gap-2">
              <span className="font-bold">AP</span> = Aft Perpendicular
            </div>
            <div className="flex items-center gap-2">
              <span>⚓︎</span> = Midship
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
