import { useState, useEffect } from "react";
import { CsvImportWizard } from "./CsvImportWizard";
import { OffsetsGridEditor } from "./OffsetsGridEditor";
import type { VesselDetails } from "../../types/hydrostatics";

interface GeometryManagementDialogProps {
  vessel: VesselDetails;
  isOpen: boolean;
  onClose: () => void;
  onGeometryUpdated: () => void;
}

type TabType = "import" | "grid";

export function GeometryManagementDialog({
  vessel,
  isOpen,
  onClose,
  onGeometryUpdated,
}: GeometryManagementDialogProps) {
  const [activeTab, setActiveTab] = useState<TabType>("import");
  const [showCsvWizard, setShowCsvWizard] = useState(false);
  const [showGridEditor, setShowGridEditor] = useState(false);

  // Determine if vessel has geometry
  const hasGeometry =
    (vessel.stationsCount ?? 0) > 0 &&
    (vessel.waterlinesCount ?? 0) > 0 &&
    (vessel.offsetsCount ?? 0) > 0;

  // Set default tab based on geometry status
  useEffect(() => {
    if (isOpen) {
      // If no geometry, default to import tab; otherwise default to grid tab
      setActiveTab(hasGeometry ? "grid" : "import");
    }
  }, [isOpen, hasGeometry]);

  // Reset dialog states when main dialog closes
  useEffect(() => {
    if (!isOpen) {
      setShowCsvWizard(false);
      setShowGridEditor(false);
    }
  }, [isOpen]);

  const handleImportComplete = () => {
    onGeometryUpdated();
    // Close CSV wizard and switch to grid tab after successful import
    setShowCsvWizard(false);
    setActiveTab("grid");
  };

  const handleGridEditorClose = () => {
    setShowGridEditor(false);
    onGeometryUpdated();
  };

  const handleCsvWizardClose = () => {
    setShowCsvWizard(false);
    // Don't close the main dialog when CSV wizard closes - user might want to switch tabs
  };

  if (!isOpen) return null;

  return (
    <>
      {/* Tab Selection UI */}
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

          <div className="inline-block align-bottom bg-white dark:bg-gray-800 rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-4xl sm:w-full">
            {/* Header */}
            <div className="bg-white dark:bg-gray-800 px-4 pt-5 pb-4 sm:p-6 border-b border-gray-200 dark:border-gray-700">
              <div className="flex items-center justify-between mb-4">
                <div>
                  <h3
                    className="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100"
                    id="modal-title"
                  >
                    Manage Geometry
                  </h3>
                  <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
                    {hasGeometry
                      ? "Import new geometry or edit existing offsets grid"
                      : "Import geometry data to get started"}
                  </p>
                </div>
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

              {/* Tab Navigation */}
              <div className="border-b border-gray-200 dark:border-gray-700">
                <nav className="-mb-px flex space-x-8" aria-label="Tabs">
                  <button
                    onClick={() => setActiveTab("import")}
                    className={`whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm ${
                      activeTab === "import"
                        ? "border-blue-500 text-blue-600 dark:text-blue-400"
                        : "border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600"
                    }`}
                  >
                    <svg
                      className="inline-block h-5 w-5 mr-2 -mt-1"
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
                    Import from CSV
                  </button>
                  <button
                    onClick={() => setActiveTab("grid")}
                    disabled={!hasGeometry}
                    className={`whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm ${
                      !hasGeometry
                        ? "border-transparent text-gray-400 dark:text-gray-600 cursor-not-allowed"
                        : activeTab === "grid"
                          ? "border-blue-500 text-blue-600 dark:text-blue-400"
                          : "border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600"
                    }`}
                  >
                    <svg
                      className="inline-block h-5 w-5 mr-2 -mt-1"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M9 17V7m0 10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2h2a2 2 0 012 2m0 10a2 2 0 002 2h2a2 2 0 002-2M9 7a2 2 0 012-2h2a2 2 0 012 2m0 10V7m0 10a2 2 0 002 2h2a2 2 0 002-2V7a2 2 0 00-2-2h-2a2 2 0 00-2 2"
                      />
                    </svg>
                    Edit Grid
                    {!hasGeometry && <span className="ml-2 text-xs">(Import geometry first)</span>}
                  </button>
                </nav>
              </div>
            </div>

            {/* Tab Content Placeholder - actual content handled by separate dialogs */}
            <div className="bg-white dark:bg-gray-800 px-4 py-6 sm:p-6">
              {activeTab === "import" && (
                <div className="text-center py-8">
                  <p className="text-sm text-gray-600 dark:text-gray-400">
                    Click &quot;Open CSV Import Wizard&quot; to import geometry data
                  </p>
                  <button
                    onClick={() => {
                      setShowCsvWizard(true);
                    }}
                    className="mt-4 inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                  >
                    <svg
                      className="h-5 w-5 mr-2"
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
                    Open CSV Import Wizard
                  </button>
                </div>
              )}

              {activeTab === "grid" && hasGeometry && (
                <div className="text-center py-8">
                  <p className="text-sm text-gray-600 dark:text-gray-400">
                    Click &quot;Open Grid Editor&quot; to edit offsets
                  </p>
                  <button
                    onClick={() => {
                      setShowGridEditor(true);
                    }}
                    className="mt-4 inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                  >
                    <svg
                      className="h-5 w-5 mr-2"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
                      />
                    </svg>
                    Open Grid Editor
                  </button>
                </div>
              )}

              {activeTab === "grid" && !hasGeometry && (
                <div className="text-center py-8">
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
                      d="M9 17V7m0 10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2h2a2 2 0 012 2m0 10a2 2 0 002 2h2a2 2 0 002-2M9 7a2 2 0 012-2h2a2 2 0 012 2m0 10V7m0 10a2 2 0 002 2h2a2 2 0 002-2V7a2 2 0 00-2-2h-2a2 2 0 00-2 2"
                    />
                  </svg>
                  <h3 className="mt-2 text-sm font-medium text-gray-900 dark:text-gray-100">
                    No geometry data available
                  </h3>
                  <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
                    Import geometry first using the CSV import wizard
                  </p>
                  <button
                    onClick={() => setActiveTab("import")}
                    className="mt-4 inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700"
                  >
                    Go to Import Tab
                  </button>
                </div>
              )}
            </div>

            {/* Footer */}
            <div className="bg-gray-50 dark:bg-gray-700/50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
              <button
                onClick={onClose}
                className="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:ml-3 sm:w-auto sm:text-sm"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* CSV Import Wizard */}
      {showCsvWizard && (
        <CsvImportWizard
          vesselId={vessel.id}
          isOpen={showCsvWizard}
          onClose={handleCsvWizardClose}
          onImportComplete={handleImportComplete}
        />
      )}

      {/* Grid Editor */}
      {showGridEditor && (
        <OffsetsGridEditor
          vesselId={vessel.id}
          isOpen={showGridEditor}
          onClose={handleGridEditorClose}
        />
      )}
    </>
  );
}
