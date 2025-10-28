import { useState, useEffect, useMemo } from "react";
import { AgGridReact } from "ag-grid-react";
import type { ColDef } from "ag-grid-community";
import { geometryApi } from "../../services/hydrostaticsApi";

// Import AG Grid styles
import "ag-grid-community/styles/ag-grid.css";
import "ag-grid-community/styles/ag-theme-alpine.css";

interface OffsetsGridEditorProps {
  vesselId: string;
  isOpen: boolean;
  onClose: () => void;
}

import type { OffsetsGrid, Offset } from "../../types/hydrostatics";

interface GridRow {
  stationIndex: number;
  stationX: number;
  [key: string]: number; // Dynamic waterline columns
}

export function OffsetsGridEditor({ vesselId, isOpen, onClose }: OffsetsGridEditorProps) {
  const [offsetData, setOffsetData] = useState<OffsetsGrid | null>(null);
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      loadOffsets();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, vesselId]);

  const loadOffsets = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await geometryApi.getOffsetsGrid(vesselId);
      setOffsetData(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load offsets");
    } finally {
      setLoading(false);
    }
  };

  const rowData = useMemo(() => {
    if (!offsetData) return [];

    const rows: GridRow[] = [];

    // Each station is at index i with X position offsetData.stations[i]
    for (let stationIndex = 0; stationIndex < offsetData.stations.length; stationIndex++) {
      const row: GridRow = {
        stationIndex,
        stationX: offsetData.stations[stationIndex],
      };

      // Add offset values for each waterline
      for (
        let waterlineIndex = 0;
        waterlineIndex < offsetData.waterlines.length;
        waterlineIndex++
      ) {
        // offsets[stationIndex][waterlineIndex]
        const halfBreadth = offsetData.offsets[stationIndex]?.[waterlineIndex] ?? 0;
        row[`wl_${waterlineIndex}`] = halfBreadth;
      }

      rows.push(row);
    }

    return rows;
  }, [offsetData]);

  const columnDefs = useMemo<ColDef[]>(() => {
    if (!offsetData) return [];

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

    // Add columns for each waterline
    for (let waterlineIndex = 0; waterlineIndex < offsetData.waterlines.length; waterlineIndex++) {
      const waterlineZ = offsetData.waterlines[waterlineIndex];
      cols.push({
        field: `wl_${waterlineIndex}`,
        headerName: `WL ${waterlineIndex} (z=${waterlineZ.toFixed(2)}m)`,
        width: 150,
        editable: true,
        valueParser: (params) => {
          const value = parseFloat(params.newValue);
          return isNaN(value) ? 0 : value;
        },
        valueFormatter: (params) => {
          return params.value !== null && params.value !== undefined
            ? params.value.toFixed(3)
            : "0.000";
        },
      });
    }

    return cols;
  }, [offsetData]);

  const handleSave = async () => {
    if (!offsetData) return;

    try {
      setSaving(true);
      setError(null);

      // Convert grid data back to offsets array
      const updatedOffsets: Offset[] = [];
      for (const row of rowData) {
        for (
          let waterlineIndex = 0;
          waterlineIndex < offsetData.waterlines.length;
          waterlineIndex++
        ) {
          const halfBreadthY = row[`wl_${waterlineIndex}`];
          updatedOffsets.push({
            stationIndex: row.stationIndex,
            waterlineIndex,
            halfBreadthY,
          });
        }
      }

      await geometryApi.bulkImportOffsets(vesselId, updatedOffsets);
      alert("Offsets saved successfully!");
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save offsets");
    } finally {
      setSaving(false);
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

        <div className="inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-7xl sm:w-full">
          {/* Header */}
          <div className="bg-white px-4 pt-5 pb-4 sm:p-6">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h3 className="text-lg leading-6 font-medium text-gray-900" id="modal-title">
                  Offsets Grid Editor
                </h3>
                <p className="mt-1 text-sm text-gray-500">
                  Edit half-breadth values for each station and waterline
                </p>
              </div>
              <button
                onClick={onClose}
                className="text-gray-400 hover:text-gray-500 focus:outline-none"
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

            {/* Error Message */}
            {error && (
              <div className="mb-4 bg-red-50 border border-red-200 text-red-700 px-3 py-2 rounded text-sm">
                {error}
              </div>
            )}

            {/* Loading State */}
            {loading && (
              <div className="flex items-center justify-center py-12">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
              </div>
            )}

            {/* Grid */}
            {!loading && offsetData && (
              <>
                {offsetData.stations.length === 0 ? (
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
                    <h3 className="mt-2 text-sm font-medium text-gray-900">
                      No geometry data available
                    </h3>
                    <p className="mt-1 text-sm text-gray-500">
                      Import stations and waterlines first using the CSV import wizard
                    </p>
                  </div>
                ) : (
                  <>
                    {/* Debug Info */}
                    <div className="mb-2 text-xs text-gray-500">
                      Rows: {rowData.length}, Columns: {columnDefs.length}
                    </div>
                    <div className="ag-theme-alpine" style={{ height: "500px", width: "100%" }}>
                      <AgGridReact
                        rowData={rowData}
                        columnDefs={columnDefs}
                        defaultColDef={{
                          sortable: true,
                          filter: true,
                          resizable: true,
                        }}
                        enableCellTextSelection={true}
                        ensureDomOrder={true}
                        animateRows={true}
                      />
                    </div>
                  </>
                )}

                {/* Info */}
                {offsetData.stations.length > 0 && (
                  <div className="mt-4 bg-blue-50 border border-blue-200 rounded-md p-4">
                    <div className="flex">
                      <svg
                        className="h-5 w-5 text-blue-400"
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
                        <p className="text-sm text-blue-700">
                          <strong>Grid Info:</strong> {offsetData.stations.length} stations ×{" "}
                          {offsetData.waterlines.length} waterlines ={" "}
                          {offsetData.stations.length * offsetData.waterlines.length} offsets
                        </p>
                        <p className="text-xs text-blue-600 mt-1">
                          Double-click cells to edit. Values are half-breadth (m) from centerline.
                        </p>
                      </div>
                    </div>
                  </div>
                )}
              </>
            )}
          </div>

          {/* Actions */}
          {!loading && offsetData && offsetData.stations.length > 0 && (
            <div className="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
              <button
                onClick={handleSave}
                disabled={saving}
                className="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:ml-3 sm:w-auto sm:text-sm disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {saving ? "Saving..." : "Save Changes"}
              </button>
              <button
                onClick={onClose}
                disabled={saving}
                className="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:mt-0 sm:w-auto sm:text-sm disabled:opacity-50"
              >
                Cancel
              </button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default OffsetsGridEditor;
