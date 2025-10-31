import { useState, useEffect, useMemo } from "react";
import { AgGridReact } from "ag-grid-react";
import type { ColDef, GridApi, GridReadyEvent, ICellRendererParams } from "ag-grid-community";
import { speedGridsApi } from "../../services/resistanceApi";
import { getErrorMessage } from "../../types/errors";
import type { SpeedGrid, SpeedPoint, CreateSpeedGridRequest } from "../../types/resistance";

// Import AG Grid styles
import "ag-grid-community/styles/ag-grid.css";
import "ag-grid-community/styles/ag-theme-alpine.css";

interface SpeedGridEditorProps {
  vesselId: string;
  gridId?: string; // If provided, edit existing grid; otherwise create new
  isOpen: boolean;
  onClose: () => void;
  onSave: () => void;
}

interface GridRow {
  id: string;
  speed: number; // m/s
  speedKnots: number;
  froudeNumber?: number;
  notes?: string;
  displayOrder: number;
}

export function SpeedGridEditor({
  vesselId,
  gridId,
  isOpen,
  onClose,
  onSave,
}: SpeedGridEditorProps) {
  const [grid, setGrid] = useState<SpeedGrid | null>(null);
  const [gridName, setGridName] = useState("");
  const [gridDescription, setGridDescription] = useState("");
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [gridApi, setGridApi] = useState<GridApi | null>(null);

  useEffect(() => {
    if (isOpen) {
      if (gridId) {
        loadGrid();
      } else {
        // New grid - initialize with empty data
        setGridName("");
        setGridDescription("");
        setGrid({
          id: "",
          vesselId,
          name: "",
          description: "",
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          speedPoints: [],
        });
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, gridId, vesselId]);

  const loadGrid = async () => {
    if (!gridId) return;

    try {
      setLoading(true);
      setError(null);
      const data = await speedGridsApi.get(vesselId, gridId);
      setGrid(data);
      setGridName(data.name);
      setGridDescription(data.description || "");
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  const rowData = useMemo(() => {
    if (!grid) return [];

    return grid.speedPoints
      .sort((a, b) => a.displayOrder - b.displayOrder)
      .map((point) => ({
        id: point.id,
        speed: point.speed,
        speedKnots: point.speedKnots ?? point.speed / 0.514444, // Convert m/s to knots
        froudeNumber: point.froudeNumber,
        notes: point.notes || "",
        displayOrder: point.displayOrder,
      }));
  }, [grid]);

  const columnDefs = useMemo<ColDef[]>(() => {
    return [
      {
        field: "displayOrder",
        headerName: "#",
        width: 60,
        editable: false,
        pinned: "left",
        cellRenderer: (params: ICellRendererParams) => (params.node.rowIndex ?? 0) + 1,
      },
      {
        field: "speedKnots",
        headerName: "Speed (knots)",
        width: 140,
        editable: true,
        valueParser: (params) => {
          const value = parseFloat(params.newValue);
          return isNaN(value) ? 0 : value;
        },
        valueFormatter: (params) => {
          return params.value !== null && params.value !== undefined
            ? params.value.toFixed(2)
            : "0.00";
        },
        cellStyle: { textAlign: "right" },
      },
      {
        field: "speed",
        headerName: "Speed (m/s)",
        width: 130,
        editable: false,
        valueFormatter: (params) => {
          return params.value !== null && params.value !== undefined
            ? params.value.toFixed(3)
            : "0.000";
        },
        cellStyle: { textAlign: "right" },
      },
      {
        field: "froudeNumber",
        headerName: "Fn",
        width: 100,
        editable: false,
        valueFormatter: (params) => {
          return params.value !== null && params.value !== undefined ? params.value.toFixed(4) : "";
        },
        cellStyle: { textAlign: "right" },
      },
      {
        field: "notes",
        headerName: "Notes",
        width: 200,
        editable: true,
        flex: 1,
      },
    ];
  }, []);

  const handleAddRow = () => {
    if (!grid || !gridApi) return;

    const newPoints = [...grid.speedPoints];
    const maxOrder = newPoints.length > 0 ? Math.max(...newPoints.map((p) => p.displayOrder)) : -1;

    const newPoint: SpeedPoint = {
      id: `new-${Date.now()}`,
      speedGridId: grid.id || "",
      speed: 0,
      speedKnots: 0,
      displayOrder: maxOrder + 1,
    };

    newPoints.push(newPoint);
    setGrid({ ...grid, speedPoints: newPoints });
  };

  const handleDeleteSelected = () => {
    if (!grid || !gridApi) return;

    const selectedRows = gridApi.getSelectedRows();
    if (selectedRows.length === 0) {
      alert("Please select rows to delete");
      return;
    }

    const selectedIds = selectedRows.map((row: GridRow) => row.id);
    const updatedPoints = grid.speedPoints.filter((p) => !selectedIds.includes(p.id));
    setGrid({ ...grid, speedPoints: updatedPoints });
    gridApi.deselectAll();
  };

  const handleSave = async () => {
    if (!grid) return;

    if (!gridName.trim()) {
      setError("Grid name is required");
      return;
    }

    if (grid.speedPoints.length === 0) {
      setError("At least one speed point is required");
      return;
    }

    try {
      setSaving(true);
      setError(null);

      // Convert grid rows to speed points
      const speedPoints: CreateSpeedGridRequest["speedPoints"] = rowData.map((row, index) => ({
        speed: row.speedKnots * 0.514444, // Convert knots to m/s
        speedKnots: row.speedKnots,
        notes: row.notes || undefined,
        displayOrder: index,
      }));

      if (gridId) {
        // Update existing grid
        await speedGridsApi.update(vesselId, gridId, {
          name: gridName,
          description: gridDescription || undefined,
          speedPoints: [],
        });

        // Update points separately
        await speedGridsApi.updatePoints(vesselId, gridId, { speedPoints });
      } else {
        // Create new grid
        await speedGridsApi.create(vesselId, {
          name: gridName,
          description: gridDescription || undefined,
          speedPoints,
        });
      }

      onSave();
      onClose();
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setSaving(false);
    }
  };

  // Update speed (m/s) when knots changes
  useEffect(() => {
    if (!grid || !gridApi) return;

    const updateSpeedFromKnots = () => {
      const updatedPoints = grid.speedPoints.map((point) => {
        const row = rowData.find((r) => r.id === point.id);
        if (row) {
          return {
            ...point,
            speed: row.speedKnots * 0.514444, // Convert knots to m/s
            speedKnots: row.speedKnots,
          };
        }
        return point;
      });
      setGrid({ ...grid, speedPoints: updatedPoints });
    };

    // Debounce updates
    const timeoutId = setTimeout(updateSpeedFromKnots, 300);
    return () => clearTimeout(timeoutId);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [rowData]);

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

        <div className="inline-block align-bottom bg-white dark:bg-gray-800 rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-5xl sm:w-full">
          {/* Header */}
          <div className="bg-white dark:bg-gray-800 px-4 pt-5 pb-4 sm:p-6">
            <div className="flex items-center justify-between mb-4">
              <div>
                <h3
                  className="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100"
                  id="modal-title"
                >
                  {gridId ? "Edit Speed Grid" : "Create Speed Grid"}
                </h3>
                <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
                  Define speed points for resistance calculations
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

            {/* Grid Info */}
            <div className="mb-4 space-y-3">
              <div>
                <label
                  htmlFor="grid-name"
                  className="block text-sm font-medium text-gray-700 dark:text-gray-300"
                >
                  Grid Name *
                </label>
                <input
                  id="grid-name"
                  type="text"
                  value={gridName}
                  onChange={(e) => setGridName(e.target.value)}
                  className="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-blue-500 focus:ring-blue-500 dark:bg-gray-700 dark:text-gray-100 sm:text-sm"
                  placeholder="e.g., Standard Speed Grid 0-15 m/s"
                />
              </div>
              <div>
                <label
                  htmlFor="grid-description"
                  className="block text-sm font-medium text-gray-700 dark:text-gray-300"
                >
                  Description
                </label>
                <textarea
                  id="grid-description"
                  value={gridDescription}
                  onChange={(e) => setGridDescription(e.target.value)}
                  rows={2}
                  className="mt-1 block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-blue-500 focus:ring-blue-500 dark:bg-gray-700 dark:text-gray-100 sm:text-sm"
                  placeholder="Optional description"
                />
              </div>
            </div>

            {/* Error Message */}
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

            {/* Action Buttons */}
            <div className="mb-4 flex items-center gap-2">
              <button
                onClick={handleAddRow}
                className="inline-flex items-center px-3 py-1.5 border border-transparent text-xs font-medium rounded text-blue-700 bg-blue-100 hover:bg-blue-200 dark:bg-blue-900 dark:text-blue-200 dark:hover:bg-blue-800 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
              >
                <svg className="h-4 w-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 4v16m8-8H4"
                  />
                </svg>
                Add Row
              </button>
              <button
                onClick={handleDeleteSelected}
                className="inline-flex items-center px-3 py-1.5 border border-transparent text-xs font-medium rounded text-red-700 bg-red-100 hover:bg-red-200 dark:bg-red-900 dark:text-red-200 dark:hover:bg-red-800 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-red-500"
              >
                <svg className="h-4 w-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                  />
                </svg>
                Delete Selected
              </button>
            </div>

            {/* AG Grid */}
            <div className="ag-theme-alpine dark:ag-theme-alpine-dark" style={{ height: "400px" }}>
              <AgGridReact
                rowData={rowData}
                columnDefs={columnDefs}
                onGridReady={(params: GridReadyEvent) => setGridApi(params.api)}
                rowSelection="multiple"
                animateRows={true}
                defaultColDef={{
                  sortable: true,
                  filter: true,
                  resizable: true,
                }}
              />
            </div>
          </div>

          {/* Footer */}
          <div className="bg-gray-50 dark:bg-gray-700 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
            <button
              onClick={handleSave}
              disabled={saving || loading}
              className="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:ml-3 sm:w-auto sm:text-sm disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {saving ? "Saving..." : gridId ? "Update Grid" : "Create Grid"}
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
