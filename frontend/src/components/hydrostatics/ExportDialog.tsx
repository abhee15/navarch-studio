import { useState } from "react";
import { exportApi } from "../../services/hydrostaticsApi";
import type { HydroResult } from "../../types/hydrostatics";
import { toast } from "../common/Toast";
import { getErrorMessage } from "../../types/errors";

interface ExportDialogProps {
  isOpen: boolean;
  onClose: () => void;
  vesselId: string;
  vesselName: string;
  loadcaseId?: string;
  results: HydroResult[];
}

type ExportFormat = "csv" | "json" | "pdf" | "excel";

export function ExportDialog({
  isOpen,
  onClose,
  vesselId,
  vesselName,
  loadcaseId,
  results,
}: ExportDialogProps) {
  const [format, setFormat] = useState<ExportFormat>("csv");
  const [includeCurves, setIncludeCurves] = useState(false);
  const [exporting, setExporting] = useState(false);

  if (!isOpen) return null;

  const handleExport = async () => {
    const toastId = toast.loading("Preparing export...");

    try {
      setExporting(true);

      let blob: Blob;
      let filename: string;

      switch (format) {
        case "csv":
          blob = await exportApi.exportCsv(vesselId, results);
          filename = `${vesselName.replace(/\s+/g, "_")}_hydrostatics.csv`;
          break;
        case "json":
          blob = await exportApi.exportJson(vesselId, results);
          filename = `${vesselName.replace(/\s+/g, "_")}_hydrostatics.json`;
          break;
        case "pdf":
          toast.loading("Generating PDF report...", { id: toastId });
          blob = await exportApi.exportPdf(vesselId, loadcaseId, includeCurves);
          filename = `${vesselName.replace(/\s+/g, "_")}_hydrostatics.pdf`;
          break;
        case "excel":
          toast.loading("Generating Excel workbook...", { id: toastId });
          blob = await exportApi.exportExcel(vesselId, loadcaseId, includeCurves);
          filename = `${vesselName.replace(/\s+/g, "_")}_hydrostatics.xlsx`;
          break;
        default:
          throw new Error(`Unsupported format: ${format}`);
      }

      // Trigger download
      const url = URL.createObjectURL(blob);
      const link: HTMLAnchorElement = document.createElement("a");
      link.href = url;
      // Set download attribute in a standards-compliant way
      link.download = filename;
      link.setAttribute("download", filename);
      document.body.appendChild(link);
      link.click();

      toast.success(`Export successful! Downloaded ${filename}`, { id: toastId });

      // Close dialog on success
      onClose();
    } catch (err) {
      console.error("Export failed:", err);
      toast.error(`Export failed: ${getErrorMessage(err)}`, { id: toastId });
    } finally {
      setExporting(false);
    }
  };

  const formatOptions = [
    { value: "csv", label: "CSV", description: "Comma-separated values" },
    { value: "json", label: "JSON", description: "JavaScript object notation" },
    { value: "pdf", label: "PDF", description: "Professional report with charts" },
    { value: "excel", label: "Excel", description: "Multi-sheet workbook" },
  ] as const;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl max-w-md w-full mx-4">
        {/* Header */}
        <div className="px-6 py-4 border-b border-gray-200 dark:border-gray-700">
          <div className="flex items-center justify-between">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
              Export Hydrostatic Data
            </h3>
            <button
              onClick={onClose}
              className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
              disabled={exporting}
              aria-label="Close"
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
        </div>

        {/* Content */}
        <div className="px-6 py-4 space-y-4">
          {/* Vessel info */}
          <div className="text-sm text-gray-600 dark:text-gray-400">
            <p>
              <span className="font-medium">Vessel:</span> {vesselName}
            </p>
            <p>
              <span className="font-medium">Data points:</span> {results.length}
            </p>
          </div>

          {/* Format selection */}
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Export Format
            </label>
            <div className="space-y-2">
              {formatOptions.map((option) => (
                <label
                  key={option.value}
                  className={`flex items-start p-3 border rounded-lg cursor-pointer transition-colors ${
                    format === option.value
                      ? "border-blue-500 bg-blue-50 dark:bg-blue-900/20"
                      : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600"
                  }`}
                >
                  <input
                    type="radio"
                    name="format"
                    value={option.value}
                    checked={format === option.value}
                    onChange={(e) => setFormat(e.target.value as ExportFormat)}
                    className="mt-1"
                    disabled={exporting}
                  />
                  <div className="ml-3 flex-1">
                    <div className="font-medium text-gray-900 dark:text-white">{option.label}</div>
                    <div className="text-sm text-gray-500 dark:text-gray-400">
                      {option.description}
                    </div>
                  </div>
                </label>
              ))}
            </div>
          </div>

          {/* Include curves option (always rendered; disabled for CSV/JSON) */}
          <div className="flex items-start">
            <input
              id="include-curves"
              type="checkbox"
              checked={includeCurves}
              onChange={(e) => setIncludeCurves(e.target.checked)}
              className="mt-1"
              disabled={exporting || format === "csv" || format === "json"}
            />
            <label
              htmlFor="include-curves"
              className="ml-3 text-sm text-gray-700 dark:text-gray-300"
            >
              Include hydrostatic curves
              <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                Adds displacement, KB, LCB, AWP, and GM curves to the export
              </p>
            </label>
          </div>
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-gray-200 dark:border-gray-700 flex justify-end space-x-3">
          <button
            onClick={onClose}
            className="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg"
            disabled={exporting}
          >
            Cancel
          </button>
          <button
            onClick={handleExport}
            className="px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg disabled:opacity-50 disabled:cursor-not-allowed flex items-center"
            disabled={exporting || results.length === 0}
          >
            {exporting ? (
              <>
                <svg
                  className="animate-spin -ml-1 mr-2 h-4 w-4 text-white"
                  fill="none"
                  viewBox="0 0 24 24"
                >
                  <circle
                    className="opacity-25"
                    cx="12"
                    cy="12"
                    r="10"
                    stroke="currentColor"
                    strokeWidth="4"
                  />
                  <path
                    className="opacity-75"
                    fill="currentColor"
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                  />
                </svg>
                Exporting...
              </>
            ) : (
              <>
                <svg className="w-4 h-4 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                  />
                </svg>
                Export
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
}
