import { useState, useCallback } from "react";
import { useDropzone } from "react-dropzone";
import Papa from "papaparse";
import { geometryApi } from "../../services/hydrostaticsApi";
import { getErrorMessage } from "../../types/errors";

interface CsvImportWizardProps {
  vesselId: string;
  isOpen: boolean;
  onClose: () => void;
  onImportComplete: () => void;
}

type CsvFormat = "combined" | "offsets_only" | null;

interface ParsedRow {
  [key: string]: string;
}

export function CsvImportWizard({
  vesselId,
  isOpen,
  onClose,
  onImportComplete,
}: CsvImportWizardProps) {
  const [step, setStep] = useState<1 | 2 | 3>(1);
  const [file, setFile] = useState<File | null>(null);
  const [format, setFormat] = useState<CsvFormat>(null);
  const [previewData, setPreviewData] = useState<ParsedRow[]>([]);
  const [importing, setImporting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [importResult, setImportResult] = useState<{
    stations_imported: number;
    waterlines_imported: number;
    offsets_imported: number;
  } | null>(null);

  const onDrop = useCallback((acceptedFiles: File[]) => {
    if (acceptedFiles.length > 0) {
      const selectedFile = acceptedFiles[0];
      setFile(selectedFile);
      setError(null);

      // Parse CSV for preview
      Papa.parse(selectedFile, {
        header: true,
        preview: 5,
        complete: (results) => {
          setPreviewData(results.data as ParsedRow[]);
          // Auto-detect format
          const headers = Object.keys((results.data as ParsedRow[])[0] || {});
          if (
            headers.includes("station_index") &&
            headers.includes("station_x") &&
            headers.includes("waterline_index")
          ) {
            setFormat("combined");
          } else if (
            headers.includes("station_index") &&
            headers.includes("waterline_index") &&
            headers.includes("half_breadth_y")
          ) {
            setFormat("offsets_only");
          }
        },
        error: (err) => {
          setError(`Failed to parse CSV: ${err.message}`);
        },
      });
    }
  }, []);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: {
      "text/csv": [".csv"],
    },
    multiple: false,
  });

  const handleImport = async () => {
    if (!file || !format) return;

    try {
      setImporting(true);
      setError(null);
      const result = await geometryApi.uploadCsv(vesselId, file, format);
      setImportResult(result);
      setStep(3);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setImporting(false);
    }
  };

  const handleClose = () => {
    setStep(1);
    setFile(null);
    setFormat(null);
    setPreviewData([]);
    setError(null);
    setImportResult(null);
    onClose();
  };

  const handleComplete = () => {
    onImportComplete();
    handleClose();
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
          onClick={handleClose}
        ></div>

        <span className="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">
          &#8203;
        </span>

        <div className="inline-block align-bottom bg-white dark:bg-gray-800 rounded-lg px-4 pt-5 pb-4 text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-3xl sm:w-full sm:p-6">
          {/* Header */}
          <div className="mb-6">
            <h3
              className="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100"
              id="modal-title"
            >
              Import Hull Geometry from CSV
            </h3>
            <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
              Step {step} of 3:{" "}
              {step === 1 ? "Select File" : step === 2 ? "Review & Import" : "Complete"}
            </p>
          </div>

          {/* Progress Bar */}
          <div className="mb-6">
            <div className="flex items-center">
              <div
                className={`flex-1 h-2 rounded-l-full ${step >= 1 ? "bg-blue-600" : "bg-gray-200 dark:bg-gray-700"}`}
              ></div>
              <div
                className={`flex-1 h-2 ${step >= 2 ? "bg-blue-600" : "bg-gray-200 dark:bg-gray-700"}`}
              ></div>
              <div
                className={`flex-1 h-2 rounded-r-full ${step >= 3 ? "bg-blue-600" : "bg-gray-200 dark:bg-gray-700"}`}
              ></div>
            </div>
          </div>

          {/* Error Message */}
          {error && (
            <div className="mb-4 bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-600 text-red-700 dark:text-red-300 px-3 py-2 rounded text-sm">
              {error}
            </div>
          )}

          {/* Step 1: File Selection */}
          {step === 1 && (
            <div className="space-y-4">
              <div
                {...getRootProps()}
                className={`border-2 border-dashed rounded-lg p-8 text-center cursor-pointer transition-colors ${
                  isDragActive
                    ? "border-blue-500 bg-blue-50 dark:bg-blue-900/20"
                    : "border-gray-300 dark:border-gray-600 hover:border-gray-400 dark:hover:border-gray-500"
                }`}
              >
                <input {...getInputProps()} />
                <svg
                  className="mx-auto h-12 w-12 text-gray-400 dark:text-gray-500"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12"
                  />
                </svg>
                <p className="mt-2 text-sm text-gray-600 dark:text-gray-300">
                  {isDragActive
                    ? "Drop the CSV file here"
                    : "Drag & drop CSV file here, or click to select"}
                </p>
                <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">
                  Supports combined format or offsets-only format
                </p>
              </div>

              {file && (
                <div className="bg-gray-50 dark:bg-gray-700/50 rounded-lg p-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center">
                      <svg
                        className="h-8 w-8 text-green-500"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                        />
                      </svg>
                      <div className="ml-3">
                        <p className="text-sm font-medium text-gray-900 dark:text-gray-100">
                          {file.name}
                        </p>
                        <p className="text-xs text-gray-500 dark:text-gray-400">
                          {(file.size / 1024).toFixed(2)} KB
                          {format &&
                            ` • ${format === "combined" ? "Combined format" : "Offsets only"}`}
                        </p>
                      </div>
                    </div>
                    <button
                      onClick={() => {
                        setFile(null);
                        setFormat(null);
                        setPreviewData([]);
                      }}
                      className="text-red-600 dark:text-red-400 hover:text-red-800 dark:hover:text-red-300"
                    >
                      Remove
                    </button>
                  </div>
                </div>
              )}

              {/* Format Selection */}
              {file && (
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    CSV Format
                  </label>
                  <div className="space-y-2">
                    <label className="flex items-center p-3 border border-gray-200 dark:border-gray-600 rounded-lg cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700/50">
                      <input
                        type="radio"
                        name="format"
                        value="combined"
                        checked={format === "combined"}
                        onChange={(e) => setFormat(e.target.value as CsvFormat)}
                        className="h-4 w-4 text-blue-600"
                      />
                      <div className="ml-3">
                        <p className="text-sm font-medium text-gray-900 dark:text-gray-100">
                          Combined Format
                        </p>
                        <p className="text-xs text-gray-500 dark:text-gray-400">
                          Includes station_x, waterline_z, and half_breadth_y
                        </p>
                      </div>
                    </label>
                    <label className="flex items-center p-3 border border-gray-200 dark:border-gray-600 rounded-lg cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700/50">
                      <input
                        type="radio"
                        name="format"
                        value="offsets_only"
                        checked={format === "offsets_only"}
                        onChange={(e) => setFormat(e.target.value as CsvFormat)}
                        className="h-4 w-4 text-blue-600"
                      />
                      <div className="ml-3">
                        <p className="text-sm font-medium text-gray-900 dark:text-gray-100">
                          Offsets Only
                        </p>
                        <p className="text-xs text-gray-500 dark:text-gray-400">
                          Only half_breadth_y (stations and waterlines must exist)
                        </p>
                      </div>
                    </label>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Step 2: Preview & Confirm */}
          {step === 2 && (
            <div className="space-y-4">
              <div>
                <h4 className="text-sm font-medium text-gray-900 dark:text-gray-100 mb-2">
                  Preview (first 5 rows)
                </h4>
                <div className="overflow-x-auto border border-gray-200 dark:border-gray-600 rounded-lg">
                  <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                    <thead className="bg-gray-50 dark:bg-gray-700/50">
                      <tr>
                        {previewData[0] &&
                          Object.keys(previewData[0]).map((header) => (
                            <th
                              key={header}
                              className="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase"
                            >
                              {header}
                            </th>
                          ))}
                      </tr>
                    </thead>
                    <tbody className="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
                      {previewData.map((row, idx) => (
                        <tr key={idx}>
                          {Object.values(row).map((value, colIdx) => (
                            <td
                              key={colIdx}
                              className="px-4 py-2 text-sm text-gray-900 dark:text-gray-100 whitespace-nowrap"
                            >
                              {value}
                            </td>
                          ))}
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>

              <div className="bg-blue-50 dark:bg-blue-900/30 border border-blue-200 dark:border-blue-700 rounded-md p-4">
                <div className="flex">
                  <svg
                    className="h-5 w-5 text-blue-400 dark:text-blue-300"
                    fill="currentColor"
                    viewBox="0 0 20 20"
                  >
                    <path
                      fillRule="evenodd"
                      d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z"
                      clipRule="evenodd"
                    />
                  </svg>
                  <div className="ml-3">
                    <p className="text-sm text-blue-700 dark:text-blue-300">
                      Format: <strong>{format === "combined" ? "Combined" : "Offsets Only"}</strong>
                    </p>
                    <p className="text-xs text-blue-600 dark:text-blue-400 mt-1">
                      Ready to import. This will {format === "combined" ? "create" : "update"}{" "}
                      geometry data.
                    </p>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Step 3: Success */}
          {step === 3 && importResult && (
            <div className="text-center py-6">
              <svg
                className="mx-auto h-16 w-16 text-green-500"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
              <h3 className="mt-4 text-lg font-medium text-gray-900 dark:text-gray-100">
                Import Successful!
              </h3>
              <div className="mt-4 space-y-2 text-sm text-gray-600 dark:text-gray-300">
                <p>✓ {importResult.stations_imported} stations imported</p>
                <p>✓ {importResult.waterlines_imported} waterlines imported</p>
                <p>✓ {importResult.offsets_imported} offsets imported</p>
              </div>
            </div>
          )}

          {/* Actions */}
          <div className="mt-6 flex justify-between">
            <button
              onClick={step > 1 && step < 3 ? () => setStep((step - 1) as 1 | 2 | 3) : handleClose}
              disabled={importing}
              className="px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 hover:bg-gray-50 dark:hover:bg-gray-600 disabled:opacity-50"
            >
              {step === 3 ? "Close" : step === 1 ? "Cancel" : "Back"}
            </button>
            <button
              onClick={step === 1 ? () => setStep(2) : step === 2 ? handleImport : handleComplete}
              disabled={(step === 1 && (!file || !format)) || (step === 2 && importing)}
              className="px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {importing ? "Importing..." : step === 1 ? "Next" : step === 2 ? "Import" : "Done"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

export default CsvImportWizard;
