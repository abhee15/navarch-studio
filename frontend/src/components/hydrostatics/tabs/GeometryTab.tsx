import { useState, useEffect, useMemo } from "react";
import { AgGridReact } from "ag-grid-react";
import { ModuleRegistry, AllCommunityModule } from "ag-grid-community";
import type { ColDef, CellValueChangedEvent } from "ag-grid-community";
import type { VesselDetails, OffsetsGrid } from "../../../types/hydrostatics";
import { geometryApi } from "../../../services/hydrostaticsApi";
import CsvImportWizard from "../CsvImportWizard";

// Import AG Grid styles
import "ag-grid-community/styles/ag-grid.css";
import "ag-grid-community/styles/ag-theme-alpine.css";

// Register AG Grid modules
ModuleRegistry.registerModules([AllCommunityModule]);

interface GeometryTabProps {
  vesselId: string;
  vessel: VesselDetails;
}

interface GridRow {
  stationIndex: number;
  stationX: number;
  [key: string]: number; // Dynamic waterline columns
}

export function GeometryTab({ vesselId, vessel }: GeometryTabProps) {
  const [activeView, setActiveView] = useState<"grid" | "import">(
    vessel.offsetsCount > 0 ? "grid" : "import"
  );
  const [isImportWizardOpen, setIsImportWizardOpen] = useState(false);
  const [offsetData, setOffsetData] = useState<OffsetsGrid | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasChanges, setHasChanges] = useState(false);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (vessel.offsetsCount > 0 && activeView === "grid") {
      loadOffsets();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [vesselId, activeView]);

  const loadOffsets = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await geometryApi.getOffsetsGrid(vesselId);
      console.log("Loaded offset data:", data);
      console.log("Stations:", data.stations?.length);
      console.log("Waterlines:", data.waterlines?.length);
      console.log("Offsets:", data.offsets?.length);
      setOffsetData(data);
    } catch (err) {
      console.error("Error loading offsets:", err);
      setError(err instanceof Error ? err.message : "Failed to load offsets");
    } finally {
      setLoading(false);
    }
  };

  const rowData = useMemo(() => {
    if (!offsetData) {
      console.log("No offset data available");
      return [];
    }

    const rows: GridRow[] = [];

    for (let stationIndex = 0; stationIndex < offsetData.stations.length; stationIndex++) {
      const row: GridRow = {
        stationIndex,
        stationX: offsetData.stations[stationIndex],
      };

      for (
        let waterlineIndex = 0;
        waterlineIndex < offsetData.waterlines.length;
        waterlineIndex++
      ) {
        const halfBreadth = offsetData.offsets[stationIndex]?.[waterlineIndex] ?? 0;
        row[`wl_${waterlineIndex}`] = halfBreadth;
      }

      rows.push(row);
    }

    console.log("Generated rows:", rows.length, "Sample:", rows[0]);
    return rows;
  }, [offsetData]);

  const columnDefs = useMemo<ColDef[]>(() => {
    if (!offsetData) {
      console.log("No offset data for columns");
      return [];
    }

    const cols: ColDef[] = [
      {
        field: "stationIndex",
        headerName: "Station #",
        width: 100,
        editable: false,
        pinned: "left",
      },
      {
        field: "stationX",
        headerName: "Station X (m)",
        width: 130,
        editable: false,
        pinned: "left",
      },
    ];

    for (let waterlineIndex = 0; waterlineIndex < offsetData.waterlines.length; waterlineIndex++) {
      const waterlineZ = offsetData.waterlines[waterlineIndex];
      cols.push({
        field: `wl_${waterlineIndex}`,
        headerName: `WL ${waterlineZ}m`,
        width: 110,
        editable: true,
        valueFormatter: (params) => {
          return params.value ? params.value.toFixed(3) : "0.000";
        },
      });
    }

    console.log("Generated columns:", cols.length);
    return cols;
  }, [offsetData]);

  const handleCellValueChanged = (event: CellValueChangedEvent<GridRow>) => {
    const field = event.colDef.field;
    if (!offsetData || !field) return;

    // Update the offsetData with the changed value
    const { data } = event;
    const stationIndex = data.stationIndex;
    const waterlineIndex = parseInt(field.replace("wl_", ""));
    const newValue = event.newValue;

    console.log(
      `Cell changed: Station ${stationIndex}, Waterline ${waterlineIndex}, New value: ${newValue}`
    );

    // Update the offsets array
    const updatedOffsets = [...offsetData.offsets];
    if (!updatedOffsets[stationIndex]) {
      updatedOffsets[stationIndex] = [];
    }
    updatedOffsets[stationIndex][waterlineIndex] = parseFloat(newValue) || 0;

    setOffsetData({
      ...offsetData,
      offsets: updatedOffsets,
    });
    setHasChanges(true);
  };

  const handleSaveChanges = async () => {
    if (!offsetData) return;

    try {
      setSaving(true);
      setError(null);

      // Call the update API
      await geometryApi.updateOffsetsGrid(vesselId, offsetData);

      console.log("Changes saved successfully for vessel:", vesselId);
      setHasChanges(false);

      // Show success message
      alert(`Changes saved successfully for vessel "${vessel.name}"!`);
    } catch (err) {
      console.error("Error saving changes:", err);
      setError(err instanceof Error ? err.message : "Failed to save changes");
    } finally {
      setSaving(false);
    }
  };

  const handleDiscardChanges = () => {
    if (confirm(`Discard all unsaved changes to ${vessel.name}'s geometry?`)) {
      loadOffsets(); // Reload original data from this vessel
      setHasChanges(false);
    }
  };

  return (
    <div className="h-full flex flex-col overflow-hidden bg-gray-50 dark:bg-gray-900">
      {/* Compact Toolbar */}
      <div className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 px-3 py-2 flex items-center justify-between flex-shrink-0">
        <div className="flex items-center gap-2">
          <button
            onClick={() => setActiveView("grid")}
            className={`px-3 py-1.5 rounded text-xs font-medium transition-colors ${
              activeView === "grid"
                ? "bg-blue-600 text-white"
                : "bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600"
            }`}
            disabled={vessel.offsetsCount === 0}
          >
            Offsets Grid ({vessel.offsetsCount})
          </button>
          <button
            onClick={() => setActiveView("import")}
            className={`px-3 py-1.5 rounded text-xs font-medium transition-colors ${
              activeView === "import"
                ? "bg-blue-600 text-white"
                : "bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-600"
            }`}
          >
            Import CSV
          </button>

          {/* Save/Discard buttons - only show when there are changes */}
          {hasChanges && activeView === "grid" && (
            <>
              <div className="h-5 w-px bg-gray-300 dark:bg-gray-600 mx-1"></div>
              <button
                onClick={handleSaveChanges}
                disabled={saving}
                className="inline-flex items-center px-3 py-1.5 rounded text-xs font-medium text-white bg-green-600 hover:bg-green-700 dark:bg-green-700 dark:hover:bg-green-800 disabled:bg-gray-400 disabled:cursor-not-allowed transition-colors"
              >
                {saving ? (
                  <>
                    <svg
                      className="animate-spin -ml-1 mr-1.5 h-3 w-3"
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
                      ></circle>
                      <path
                        className="opacity-75"
                        fill="currentColor"
                        d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                      ></path>
                    </svg>
                    Saving...
                  </>
                ) : (
                  <>
                    <svg
                      className="-ml-0.5 mr-1.5 h-3.5 w-3.5"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M5 13l4 4L19 7"
                      />
                    </svg>
                    Save Changes
                  </>
                )}
              </button>
              <button
                onClick={handleDiscardChanges}
                disabled={saving}
                className="inline-flex items-center px-3 py-1.5 rounded text-xs font-medium text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                <svg
                  className="-ml-0.5 mr-1.5 h-3.5 w-3.5"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
                Discard
              </button>
            </>
          )}
        </div>
        <div className="text-xs text-gray-500">
          {vessel.stationsCount} stations × {vessel.waterlinesCount} waterlines
          {hasChanges && (
            <span
              className="ml-2 text-orange-600 font-medium"
              title={`Unsaved changes for ${vessel.name}`}
            >
              • Unsaved changes (vessel-specific)
            </span>
          )}
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-hidden">
        {activeView === "grid" ? (
          vessel.offsetsCount === 0 ? (
            <div className="flex items-center justify-center h-full p-6">
              <div className="text-center">
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
                <h3 className="mt-3 text-sm font-medium text-gray-900 dark:text-gray-100">
                  No Geometry Data
                </h3>
                <p className="mt-2 text-xs text-gray-500 dark:text-gray-400">
                  Import CSV data to get started with hull offsets
                </p>
                <div className="mt-4">
                  <button
                    onClick={() => setActiveView("import")}
                    className="inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 shadow-sm text-xs font-medium rounded text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700"
                  >
                    Import from CSV
                  </button>
                </div>
              </div>
            </div>
          ) : loading ? (
            <div className="flex items-center justify-center h-full">
              <div className="text-center">
                <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-blue-600 mx-auto"></div>
                <p className="mt-3 text-xs text-gray-500 dark:text-gray-400">
                  Loading offsets grid...
                </p>
              </div>
            </div>
          ) : error ? (
            <div className="flex items-center justify-center h-full p-6">
              <div className="text-center">
                <svg
                  className="mx-auto h-10 w-10 text-red-500"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                  />
                </svg>
                <h3 className="mt-3 text-sm font-medium text-gray-900 dark:text-gray-100">Error</h3>
                <p className="mt-2 text-xs text-gray-500 dark:text-gray-400">{error}</p>
                <div className="mt-4">
                  <button
                    onClick={loadOffsets}
                    className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-xs font-medium rounded text-white bg-blue-600 hover:bg-blue-700"
                  >
                    Retry
                  </button>
                </div>
              </div>
            </div>
          ) : (
            <div className="h-full p-3">
              <div
                className="ag-theme-alpine rounded border border-gray-200"
                style={{ height: "calc(100% - 0px)" }}
              >
                <AgGridReact
                  rowData={rowData}
                  columnDefs={columnDefs}
                  defaultColDef={{
                    sortable: true,
                    resizable: true,
                  }}
                  suppressMovableColumns={true}
                  domLayout="normal"
                  onCellValueChanged={handleCellValueChanged}
                />
              </div>
            </div>
          )
        ) : (
          <div className="flex items-center justify-center h-full p-6">
            <div className="text-center">
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
              <h3 className="mt-3 text-sm font-medium text-gray-900 dark:text-gray-100">
                CSV Import Wizard
              </h3>
              <p className="mt-2 text-xs text-gray-500 dark:text-gray-400">
                Upload a CSV file with hull offset data
              </p>
              <p className="mt-1 text-xs text-gray-400">
                Supported formats: Combined (stations + waterlines + offsets) or Offsets only
              </p>
              <div className="mt-4">
                <button
                  onClick={() => setIsImportWizardOpen(true)}
                  className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-xs font-medium rounded text-white bg-blue-600 hover:bg-blue-700"
                >
                  <svg
                    className="-ml-1 mr-2 h-4 w-4"
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
      </div>

      {/* CSV Import Wizard */}
      <CsvImportWizard
        vesselId={vesselId}
        isOpen={isImportWizardOpen}
        onClose={() => setIsImportWizardOpen(false)}
        onImportComplete={() => {
          setIsImportWizardOpen(false);
          window.location.reload();
        }}
      />
    </div>
  );
}

export default GeometryTab;
