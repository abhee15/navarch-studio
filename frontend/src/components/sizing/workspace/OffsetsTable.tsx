import { useMemo } from "react";
import type { CandidateDesign } from "../../../types/sizing";
import { Table } from "lucide-react";
import { normalizeGeometry, type OffsetsGrid } from "../../../utils/geometryFormatConverter";
import {
  generateFormCoefficientHull,
  validateNonIsometric,
  type HullDimensions,
} from "../../../utils/formCoefficientHullGenerator";

interface OffsetsTableProps {
  candidate: CandidateDesign;
  stationCount?: number;
  waterlineCount?: number;
}

// OffsetsGrid interface moved to geometryFormatConverter

/**
 * Offsets Table - Traditional Naval Architecture Table of Offsets
 *
 * Shows half-breadths at each station and waterline intersection
 * Professional engineering format
 * Uses actual generated geometry if available, otherwise generates using FormCoefficientHullGenerator (solver logic)
 */
export const OffsetsTable: React.FC<OffsetsTableProps> = ({
  candidate,
  stationCount,
  waterlineCount,
}) => {
  // Normalize geometry to OffsetsGrid format (handles both ShipD and OffsetsGrid formats)
  const geometryData = useMemo<OffsetsGrid | null>(() => {
    if (!candidate.geometryJson) return null;
    try {
      // Use geometry format converter to normalize any format to OffsetsGrid
      return normalizeGeometry(candidate.geometryJson);
    } catch (e) {
      console.error("Failed to normalize geometry:", e);
      return null;
    }
  }, [candidate.geometryJson]);

  // Check geometry generation status - show error if generation failed
  const geometryGenerationFailed =
    candidate.geometryGenerationStatus === "BothFailed" ||
    candidate.geometryGenerationStatus === "FormCoefficientFailed";

  // Use actual geometry if available, otherwise show error or generate using fallback
  const { offsets, waterlines, stationLabels, waterlineLabels } = useMemo(() => {
    // If geometry generation failed, return empty data (will show error message)
    if (geometryGenerationFailed) {
      return {
        offsets: [],
        waterlines: [],
        stationLabels: [],
        waterlineLabels: [],
      };
    }

    if (geometryData) {
      // Use actual generated geometry

      // Transpose offsets: geometry has [stationIndex][waterlineIndex], but table needs [waterlineIndex][stationIndex]
      const transposedOffsets = geometryData.waterlines.map((_, wlIdx) =>
        geometryData.stations.map((_, stIdx) => geometryData.offsets[stIdx]?.[wlIdx] ?? 0)
      );

      // Create station labels (AP, 1, 2, ..., ⚓, ..., FP)
      const actualStationCount = geometryData.stations.length;
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

    // Fallback: Generate using FormCoefficientHullGenerator (solver logic)
    // This ensures non-isometric geometry matching solver output
    const effectiveStationCount = stationCount ?? 23; // Match BSRA-compatible generation
    const effectiveWaterlineCount = waterlineCount ?? 13; // Match generation

    const lpp = candidate.lppM ?? 200;
    const beam = candidate.beamM ?? 30;
    const draft = candidate.draftM ?? 12;

    // Use form coefficients from candidate
    const cb = candidate.cb ?? 0.68;
    const cp = candidate.cp ?? 0.73;
    const cm = candidate.cm ?? 0.93;
    const cwp = candidate.cwp ?? 0.8;
    const lcbPercent = candidate.lcbPctLpp ?? 0.5;

    try {
      // Generate using FormCoefficientHullGenerator (solver logic)
      const dims: HullDimensions = {
        length: lpp,
        beam,
        draft,
        lcbPercent,
      };

      const generated = generateFormCoefficientHull(
        dims,
        cb,
        cp,
        cm,
        cwp,
        effectiveStationCount,
        effectiveWaterlineCount,
        candidate.bowFamily,
        candidate.midshipFamily,
        candidate.sternFamily,
        candidate.vesselType
      );

      // Validate non-isometric (should always pass for FormCoefficientHullGenerator)
      const isValid = validateNonIsometric(generated);
      if (!isValid) {
        console.warn(
          "[OffsetsTable] Generated geometry appears isometric - this should not happen with FormCoefficientHullGenerator"
        );
      }

      // Create station labels (AP, 1, 2, ..., ⚓, ..., FP)
      const labels = generated.stations.map((_, idx) => {
        if (idx === 0) return "AP";
        if (idx === Math.floor(generated.stations.length / 2)) return "⚓︎";
        if (idx === generated.stations.length - 1) return "FP";
        return idx.toString();
      });

      // Transpose offsets: generator has [stationIndex][waterlineIndex], but table needs [waterlineIndex][stationIndex]
      const transposedOffsets = generated.waterlines.map((_, wlIdx) =>
        generated.stations.map((_, stIdx) => generated.offsets[stIdx]?.[wlIdx] ?? 0)
      );

      return {
        offsets: transposedOffsets,
        stations: generated.stations,
        waterlines: generated.waterlines,
        stationLabels: labels,
        waterlineLabels: generated.waterlines.map((z) => z.toFixed(2)),
      };
    } catch (error) {
      console.error(
        "[OffsetsTable] Failed to generate geometry using FormCoefficientHullGenerator:",
        error
      );
      // Return empty data - will show error message
      return {
        offsets: [],
        stations: [],
        waterlines: [],
        stationLabels: [],
        waterlineLabels: [],
      };
    }
  }, [
    geometryData,
    geometryGenerationFailed,
    candidate.lppM,
    candidate.beamM,
    candidate.draftM,
    candidate.cb,
    candidate.cp,
    candidate.cm,
    candidate.cwp,
    candidate.lcbPctLpp,
    candidate.bowFamily,
    candidate.midshipFamily,
    candidate.sternFamily,
    candidate.vesselType,
    stationCount,
    waterlineCount,
  ]);

  // Show error message if geometry generation failed
  if (geometryGenerationFailed) {
    return (
      <div className="w-full overflow-auto">
        <div className="rounded-lg border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20 shadow-lg">
          <div className="p-6">
            <h3 className="text-red-800 dark:text-red-200 font-bold flex items-center gap-2 mb-2">
              <Table className="h-4 w-4" />
              Geometry Generation Failed
            </h3>
            <p className="text-red-700 dark:text-red-300 text-sm mb-3">
              Unable to generate hull geometry for this candidate. The offsets table cannot be
              displayed.
            </p>
            {candidate.geometryGenerationError && (
              <div className="mt-3 p-3 bg-red-100 dark:bg-red-900/30 rounded border border-red-200 dark:border-red-800">
                <p className="text-xs font-mono text-red-800 dark:text-red-200">
                  {candidate.geometryGenerationError}
                </p>
              </div>
            )}
            <p className="text-red-600 dark:text-red-400 text-xs mt-3">
              Please try adjusting parameters or contact support if this issue persists.
            </p>
          </div>
        </div>
      </div>
    );
  }

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
                  wlIdx === waterlines.length - 1;

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
