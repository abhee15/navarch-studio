import { useState, useEffect } from "react";
import { speedGridsApi } from "../../services/resistanceApi";
import { getErrorMessage } from "../../types/errors";
import type { SpeedGrid } from "../../types/resistance";
import { SpeedGridEditor } from "./SpeedGridEditor";

interface ManageSpeedGridsDialogProps {
  vesselId: string;
  isOpen: boolean;
  onClose: () => void;
  onGridsUpdated: () => void;
}

export function ManageSpeedGridsDialog({
  vesselId,
  isOpen,
  onClose,
  onGridsUpdated,
}: ManageSpeedGridsDialogProps) {
  const [grids, setGrids] = useState<SpeedGrid[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showEditor, setShowEditor] = useState(false);
  const [editingGridId, setEditingGridId] = useState<string | null>(null);
  const [deletingGridId, setDeletingGridId] = useState<string | null>(null);

  const loadGrids = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await speedGridsApi.list(vesselId);
      setGrids(data.speedGrids);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isOpen) {
      loadGrids();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, vesselId]);

  const handleCreateClick = () => {
    setEditingGridId(null);
    setShowEditor(true);
  };

  const handleEditClick = (grid: SpeedGrid) => {
    setEditingGridId(grid.id);
    setShowEditor(true);
  };

  const handleEditorClose = () => {
    setShowEditor(false);
    setEditingGridId(null);
  };

  const handleEditorSave = () => {
    setShowEditor(false);
    setEditingGridId(null);
    loadGrids();
    onGridsUpdated();
  };

  const handleDelete = async (grid: SpeedGrid) => {
    if (!confirm(`Are you sure you want to delete "${grid.name}"?`)) {
      return;
    }

    try {
      setDeletingGridId(grid.id);
      await speedGridsApi.delete(vesselId, grid.id);
      loadGrids();
      onGridsUpdated();
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setDeletingGridId(null);
    }
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString();
  };

  if (!isOpen) return null;

  return (
    <>
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
            <div className="bg-white dark:bg-gray-800 px-4 pt-5 pb-4 sm:p-6">
              <div className="flex items-center justify-between">
                <div>
                  <h3
                    className="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100"
                    id="modal-title"
                  >
                    Manage Speed Grids
                  </h3>
                  <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
                    Create and manage speed grids for resistance calculations
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
            </div>

            {/* Content */}
            <div className="bg-white dark:bg-gray-800 px-4 pt-0 pb-4 sm:px-6">
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

              {/* Create Button */}
              <div className="mb-4">
                <button
                  onClick={handleCreateClick}
                  className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                >
                  <svg
                    className="h-4 w-4 mr-2"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M12 4v16m8-8H4"
                    />
                  </svg>
                  Create Speed Grid
                </button>
              </div>

              {/* Grids List */}
              {loading ? (
                <div className="flex justify-center py-8">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
                </div>
              ) : grids.length === 0 ? (
                <div className="text-center py-8 text-gray-500 dark:text-gray-400">
                  <p>No speed grids yet. Create one to get started.</p>
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                    <thead className="bg-gray-50 dark:bg-gray-700">
                      <tr>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                          Name
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                          Points
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                          Speed Range
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                          Created
                        </th>
                        <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                          Actions
                        </th>
                      </tr>
                    </thead>
                    <tbody className="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
                      {grids.map((grid) => {
                        const speeds = grid.speedPoints
                          .map((p) => p.speed)
                          .filter((s) => s > 0)
                          .sort((a, b) => a - b);
                        const minSpeed = speeds.length > 0 ? speeds[0] : 0;
                        const maxSpeed = speeds.length > 0 ? speeds[speeds.length - 1] : 0;

                        return (
                          <tr
                            key={grid.id}
                            className="hover:bg-gray-50 dark:hover:bg-gray-700 cursor-pointer"
                            onClick={() => handleEditClick(grid)}
                          >
                            <td className="px-4 py-3 whitespace-nowrap">
                              <div className="text-sm font-medium text-gray-900 dark:text-gray-100">
                                {grid.name}
                              </div>
                              {grid.description && (
                                <div className="text-xs text-gray-500 dark:text-gray-400">
                                  {grid.description}
                                </div>
                              )}
                            </td>
                            <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
                              {grid.speedPoints.length}
                            </td>
                            <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
                              {minSpeed.toFixed(2)} - {maxSpeed.toFixed(2)} m/s
                              <br />
                              <span className="text-xs">
                                ({(minSpeed / 0.514444).toFixed(1)} -{" "}
                                {(maxSpeed / 0.514444).toFixed(1)} knots)
                              </span>
                            </td>
                            <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
                              {formatDate(grid.createdAt)}
                            </td>
                            <td className="px-4 py-3 whitespace-nowrap text-right text-sm font-medium">
                              <button
                                onClick={(e) => {
                                  e.stopPropagation();
                                  handleEditClick(grid);
                                }}
                                className="text-blue-600 hover:text-blue-900 dark:text-blue-400 dark:hover:text-blue-300 mr-4"
                              >
                                Edit
                              </button>
                              <button
                                onClick={(e) => {
                                  e.stopPropagation();
                                  handleDelete(grid);
                                }}
                                disabled={deletingGridId === grid.id}
                                className="text-red-600 hover:text-red-900 dark:text-red-400 dark:hover:text-red-300 disabled:opacity-50"
                              >
                                {deletingGridId === grid.id ? "Deleting..." : "Delete"}
                              </button>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}
            </div>

            {/* Footer */}
            <div className="bg-gray-50 dark:bg-gray-700 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse">
              <button
                onClick={onClose}
                className="w-full inline-flex justify-center rounded-md border border-gray-300 dark:border-gray-600 shadow-sm px-4 py-2 bg-white dark:bg-gray-800 text-base font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:ml-3 sm:w-auto sm:text-sm"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Speed Grid Editor */}
      <SpeedGridEditor
        vesselId={vesselId}
        gridId={editingGridId || undefined}
        isOpen={showEditor}
        onClose={handleEditorClose}
        onSave={handleEditorSave}
      />
    </>
  );
}
