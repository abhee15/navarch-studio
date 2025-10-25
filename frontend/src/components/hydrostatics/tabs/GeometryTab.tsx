import { useState } from "react";
import type { VesselDetails } from "../../../types/hydrostatics";
import CsvImportWizard from "../CsvImportWizard";

interface GeometryTabProps {
  vesselId: string;
  vessel: VesselDetails;
}

export function GeometryTab({ vesselId, vessel }: GeometryTabProps) {
  const [activeView, setActiveView] = useState<"grid" | "import">("grid");
  const [isImportWizardOpen, setIsImportWizardOpen] = useState(false);

  return (
    <div className="space-y-6">
      {/* Action Bar */}
      <div className="bg-white shadow rounded-lg p-4">
        <div className="flex items-center justify-between">
          <div className="flex space-x-2">
            <button
              onClick={() => setActiveView("grid")}
              className={`px-4 py-2 rounded-md text-sm font-medium ${
                activeView === "grid"
                  ? "bg-blue-600 text-white"
                  : "bg-gray-100 text-gray-700 hover:bg-gray-200"
              }`}
            >
              Offsets Grid
            </button>
            <button
              onClick={() => setActiveView("import")}
              className={`px-4 py-2 rounded-md text-sm font-medium ${
                activeView === "import"
                  ? "bg-blue-600 text-white"
                  : "bg-gray-100 text-gray-700 hover:bg-gray-200"
              }`}
            >
              Import CSV
            </button>
          </div>
          <div className="text-sm text-gray-500">
            {vessel.stationsCount} stations × {vessel.waterlinesCount} waterlines ={" "}
            {vessel.offsetsCount} offsets
          </div>
        </div>
      </div>

      {/* Content */}
      {activeView === "grid" ? (
        <div className="bg-white shadow rounded-lg p-6">
          <div className="text-center py-12">
            <svg
              className="mx-auto h-12 w-12 text-gray-400"
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
            <h3 className="mt-2 text-sm font-medium text-gray-900">Offsets Grid Editor</h3>
            <p className="mt-1 text-sm text-gray-500">
              {vessel.offsetsCount === 0
                ? "No geometry defined yet. Import CSV data to get started."
                : "Interactive grid editor coming soon"}
            </p>
            <div className="mt-6">
              <button
                onClick={() => setActiveView("import")}
                className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700"
              >
                Import from CSV
              </button>
            </div>
          </div>
        </div>
      ) : (
        <div className="bg-white shadow rounded-lg p-6">
          <div className="text-center py-12">
            <svg
              className="mx-auto h-12 w-12 text-gray-400"
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
            <h3 className="mt-2 text-sm font-medium text-gray-900">CSV Import Wizard</h3>
            <p className="mt-1 text-sm text-gray-500">Upload a CSV file with hull offset data</p>
            <p className="mt-2 text-xs text-gray-400">
              Supported formats: Combined (stations + waterlines + offsets) or Offsets only
            </p>
            <div className="mt-6">
              <button
                onClick={() => setIsImportWizardOpen(true)}
                className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
              >
                <svg
                  className="-ml-1 mr-2 h-5 w-5"
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
                Import CSV File
              </button>
            </div>
          </div>
        </div>
      )}

      {/* CSV Import Wizard */}
      <CsvImportWizard
        vesselId={vesselId}
        isOpen={isImportWizardOpen}
        onClose={() => setIsImportWizardOpen(false)}
        onImportComplete={() => {
          setIsImportWizardOpen(false);
          window.location.reload(); // Reload to update geometry counts
        }}
      />
    </div>
  );
}

export default GeometryTab;
