import { useState } from "react";
import type {
  Ittc57CalculationResult,
  HoltropMennenCalculationResult,
  PowerCurveResult,
} from "../../types/resistance";

interface ResistanceExportDialogProps {
  vesselName: string;
  ittc57Result: Ittc57CalculationResult | null;
  hmResult: HoltropMennenCalculationResult | null;
  powerResult: PowerCurveResult | null;
  isOpen: boolean;
  onClose: () => void;
}

type ExportFormat = "csv" | "json";

export function ResistanceExportDialog({
  vesselName,
  ittc57Result,
  hmResult,
  powerResult,
  isOpen,
  onClose,
}: ResistanceExportDialogProps) {
  const [format, setFormat] = useState<ExportFormat>("csv");
  const [exporting, setExporting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const hasResults = ittc57Result || hmResult || powerResult;

  const exportToCSV = () => {
    const rows: string[] = [];

    // ITTC-57 Results
    if (ittc57Result) {
      rows.push("ITTC-57 Friction Calculation Results");
      rows.push("");
      rows.push("Speed (m/s),Speed (knots),Re,Fn,CF,CF_eff");
      ittc57Result.speedGrid.forEach((speed, idx) => {
        const speedKnots = speed / 0.514444;
        const re = ittc57Result.reynoldsNumbers[idx];
        const fn = ittc57Result.froudeNumbers[idx];
        const cf = ittc57Result.frictionCoefficients[idx];
        const cfEff = ittc57Result.effectiveFrictionCoefficients[idx];
        rows.push(
          `${speed.toFixed(3)},${speedKnots.toFixed(3)},${re.toExponential(4)},${fn.toFixed(6)},${cf.toFixed(8)},${cfEff.toFixed(8)}`
        );
      });
      rows.push("");
    }

    // Holtrop-Mennen Results
    if (hmResult) {
      rows.push("Holtrop-Mennen Resistance Calculation Results");
      rows.push("");
      rows.push(
        "Speed (m/s),Speed (knots),Re,Fn,RT (N),RF (N),RR (N),RA (N),RCA (N),RAA (N),EHP (kW)"
      );
      hmResult.speedGrid.forEach((speed, idx) => {
        const speedKnots = speed / 0.514444;
        const re = hmResult.reynoldsNumbers[idx];
        const fn = hmResult.froudeNumbers[idx];
        const rt = hmResult.totalResistance[idx];
        const rf = hmResult.frictionResistance[idx];
        const rr = hmResult.residuaryResistance[idx];
        const ra = hmResult.appendageResistance[idx];
        const rca = hmResult.correlationAllowance[idx];
        const raa = hmResult.airResistance[idx];
        const ehp = hmResult.effectivePower[idx];
        rows.push(
          `${speed.toFixed(3)},${speedKnots.toFixed(3)},${re.toExponential(4)},${fn.toFixed(6)},${rt.toFixed(2)},${rf.toFixed(2)},${rr.toFixed(2)},${ra.toFixed(2)},${rca.toFixed(2)},${raa.toFixed(2)},${ehp.toFixed(2)}`
        );
      });
      rows.push("");
    }

    // Power Curves
    if (powerResult) {
      rows.push("Power Curves");
      rows.push(`Service Margin: ${powerResult.serviceMargin.toFixed(1)}%`);
      rows.push(`Overall Efficiency (ηD): ${(powerResult.etaD ?? 0.65).toFixed(3)}`);
      rows.push("");
      rows.push("Speed (m/s),Speed (knots),EHP (kW),DHP (kW),P_inst (kW)");
      powerResult.speedGrid.forEach((speed, idx) => {
        const speedKnots = speed / 0.514444;
        const ehp = powerResult.effectivePower[idx];
        const dhp = powerResult.deliveredPower[idx];
        const pInst = powerResult.installedPower[idx];
        rows.push(
          `${speed.toFixed(3)},${speedKnots.toFixed(3)},${ehp.toFixed(2)},${dhp.toFixed(2)},${pInst.toFixed(2)}`
        );
      });
    }

    return rows.join("\n");
  };

  const exportToJSON = () => {
    const data: any = {
      vessel: vesselName,
      exportedAt: new Date().toISOString(),
    };

    if (ittc57Result) {
      data.ittc57 = ittc57Result;
    }
    if (hmResult) {
      data.holtropMennen = hmResult;
    }
    if (powerResult) {
      data.powerCurves = powerResult;
    }

    return JSON.stringify(data, null, 2);
  };

  const handleExport = async () => {
    if (!hasResults) {
      setError("No results to export");
      return;
    }

    try {
      setExporting(true);
      setError(null);

      let content: string;
      let filename: string;
      let mimeType: string;

      if (format === "csv") {
        content = exportToCSV();
        filename = `${vesselName.replace(/\s+/g, "_")}_resistance_${new Date().toISOString().split("T")[0]}.csv`;
        mimeType = "text/csv";
      } else {
        content = exportToJSON();
        filename = `${vesselName.replace(/\s+/g, "_")}_resistance_${new Date().toISOString().split("T")[0]}.json`;
        mimeType = "application/json";
      }

      const blob = new Blob([content], { type: mimeType });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = filename;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);

      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Export failed");
    } finally {
      setExporting(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div
      className="fixed z-10 inset-0 overflow-y-auto"
      aria-labelledby="modal-title"
      role="dialog"
      aria-modal="true"
    >
      <div className="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
        <div
          className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity"
          aria-hidden="true"
          onClick={onClose}
        ></div>

        <span className="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">
          &#8203;
        </span>

        <div className="inline-block align-bottom bg-white dark:bg-gray-800 rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
          <div className="bg-white dark:bg-gray-800 px-4 pt-5 pb-4 sm:p-6">
            <div className="flex items-center justify-between mb-4">
              <h3
                className="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100"
                id="modal-title"
              >
                Export Resistance Results
              </h3>
              <button
                onClick={onClose}
                className="text-gray-400 hover:text-gray-500 dark:hover:text-gray-300 focus:outline-none"
              >
                <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
              </button>
            </div>

            {error && (
              <div className="mb-4 rounded-md bg-red-50 dark:bg-red-900/20 p-3">
                <div className="flex">
                  <svg
                    className="h-5 w-5 text-red-400 flex-shrink-0"
                    fill="currentColor"
                    viewBox="0 0 20 20"
                  >
                    <path
                      fillRule="evenodd"
                      d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                      clipRule="evenodd"
                    />
                  </svg>
                  <p className="ml-2 text-sm text-red-700 dark:text-red-400">{error}</p>
                </div>
              </div>
            )}

            {!hasResults && (
              <div className="mb-4 rounded-md bg-yellow-50 dark:bg-yellow-900/20 p-3">
                <p className="text-sm text-yellow-700 dark:text-yellow-400">
                  No calculation results available to export. Please run calculations first.
                </p>
              </div>
            )}

            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  Export Format
                </label>
                <div className="space-y-2">
                  <label className="flex items-center">
                    <input
                      type="radio"
                      name="format"
                      value="csv"
                      checked={format === "csv"}
                      onChange={(e) => setFormat(e.target.value as ExportFormat)}
                      className="mr-2"
                    />
                    <span className="text-sm">CSV (Comma-separated values)</span>
                  </label>
                  <label className="flex items-center">
                    <input
                      type="radio"
                      name="format"
                      value="json"
                      checked={format === "json"}
                      onChange={(e) => setFormat(e.target.value as ExportFormat)}
                      className="mr-2"
                    />
                    <span className="text-sm">JSON (Structured data)</span>
                  </label>
                </div>
              </div>

              <div className="text-sm text-gray-600 dark:text-gray-400">
                <p className="mb-2">This export will include:</p>
                <ul className="list-disc list-inside space-y-1">
                  {ittc57Result && <li>ITTC-57 friction results</li>}
                  {hmResult && <li>Holtrop-Mennen resistance results</li>}
                  {powerResult && <li>Power curves (EHP, DHP, P_inst)</li>}
                </ul>
              </div>
            </div>
          </div>

          <div className="bg-gray-50 dark:bg-gray-700 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
            <button
              onClick={handleExport}
              disabled={exporting || !hasResults}
              className="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:ml-3 sm:w-auto sm:text-sm disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {exporting ? "Exporting..." : "Export"}
            </button>
            <button
              onClick={onClose}
              className="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 dark:border-gray-600 shadow-sm px-4 py-2 bg-white dark:bg-gray-800 text-base font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:mt-0 sm:ml-3 sm:w-auto sm:text-sm"
            >
              Cancel
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

