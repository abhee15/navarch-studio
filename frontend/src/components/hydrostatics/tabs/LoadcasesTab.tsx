import { useState, useEffect } from "react";
import { observer } from "mobx-react-lite";
import { loadcasesApi } from "../../../services/hydrostaticsApi";
import type { Loadcase } from "../../../types/hydrostatics";
import CreateLoadcaseDialog from "../CreateLoadcaseDialog";
import { settingsStore } from "../../../stores/SettingsStore";
import { getUnitSymbol } from "../../../utils/unitSymbols";

interface LoadcasesTabProps {
  vesselId: string;
}

export const LoadcasesTab = observer(({ vesselId }: LoadcasesTabProps) => {
  const [loadcases, setLoadcases] = useState<Loadcase[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const loadLoadcases = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await loadcasesApi.list(vesselId);
      setLoadcases(data.loadcases);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load loadcases");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadLoadcases();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [vesselId]);

  const handleDelete = async (loadcaseId: string) => {
    if (!confirm("Are you sure you want to delete this loadcase?")) {
      return;
    }

    try {
      setDeletingId(loadcaseId);
      await loadcasesApi.delete(vesselId, loadcaseId);
      await loadLoadcases();
    } catch (err) {
      alert(err instanceof Error ? err.message : "Failed to delete loadcase");
    } finally {
      setDeletingId(null);
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  // Backend automatically converts values to user's preferred units
  const displayUnits = settingsStore.preferredUnits;
  const densityUnit = getUnitSymbol(displayUnits, "Density");
  const lengthUnit = getUnitSymbol(displayUnits, "Length");

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-white dark:bg-gray-800 shadow rounded-lg p-4">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100">Load Conditions</h3>
            <p className="mt-1 text-sm text-gray-500 dark:text-gray-400 dark:text-gray-500">
              Define water density and center of gravity for computations
            </p>
          </div>
          <button
            onClick={() => setIsCreateDialogOpen(true)}
            className="inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
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
                d="M12 6v6m0 0v6m0-6h6m-6 0H6"
              />
            </svg>
            New Loadcase
          </button>
        </div>
      </div>

      {/* Error Message */}
      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
          <strong className="font-bold">Error: </strong>
          <span>{error}</span>
        </div>
      )}

      {/* Loadcases List */}
      {loadcases.length === 0 ? (
        <div className="bg-white dark:bg-gray-800 shadow rounded-lg p-6">
          <div className="text-center py-12">
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
                d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01"
              />
            </svg>
            <h3 className="mt-2 text-sm font-medium text-gray-900 dark:text-gray-100">No loadcases</h3>
            <p className="mt-1 text-sm text-gray-500 dark:text-gray-400 dark:text-gray-500">Get started by creating a new loadcase.</p>
            <div className="mt-6">
              <button
                onClick={() => setIsCreateDialogOpen(true)}
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
                    d="M12 6v6m0 0v6m0-6h6m-6 0H6"
                  />
                </svg>
                Create Loadcase
              </button>
            </div>
          </div>
        </div>
      ) : (
        <div className="bg-white dark:bg-gray-800 shadow rounded-lg overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50 dark:bg-gray-900">
              <tr>
                <th
                  scope="col"
                  className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 dark:text-gray-500 uppercase tracking-wider"
                >
                  Name
                </th>
                <th
                  scope="col"
                  className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 dark:text-gray-500 uppercase tracking-wider"
                >
                  Density (ρ)
                </th>
                <th
                  scope="col"
                  className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 dark:text-gray-500 uppercase tracking-wider"
                >
                  KG
                </th>
                <th
                  scope="col"
                  className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 dark:text-gray-500 uppercase tracking-wider"
                >
                  Created
                </th>
                <th scope="col" className="relative px-6 py-3">
                  <span className="sr-only">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {loadcases.map((loadcase) => (
                <tr key={loadcase.id} className="hover:bg-gray-50 dark:bg-gray-900">
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm font-medium text-gray-900 dark:text-gray-100">{loadcase.name}</div>
                    {loadcase.notes && (
                      <div className="text-sm text-gray-500 dark:text-gray-400 dark:text-gray-500 line-clamp-1">{loadcase.notes}</div>
                    )}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="text-sm text-gray-900 dark:text-gray-100">
                      {loadcase.rho.toFixed(2)} {densityUnit}
                    </div>
                    <div className="text-xs text-gray-500 dark:text-gray-400 dark:text-gray-500">
                      {Math.abs(loadcase.rho - 1025) < 1
                        ? "Seawater"
                        : Math.abs(loadcase.rho - 1000) < 1
                          ? "Freshwater"
                          : "Custom"}
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-gray-100">
                    {loadcase.kg !== null && loadcase.kg !== undefined
                      ? `${loadcase.kg.toFixed(2)} ${lengthUnit}`
                      : "—"}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400 dark:text-gray-500">
                    {formatDate(loadcase.createdAt)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    <button
                      onClick={() => handleDelete(loadcase.id)}
                      disabled={deletingId === loadcase.id}
                      className="text-red-600 hover:text-red-900 disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      {deletingId === loadcase.id ? "Deleting..." : "Delete"}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Create Loadcase Dialog */}
      {isCreateDialogOpen && (
        <CreateLoadcaseDialog
          vesselId={vesselId}
          isOpen={isCreateDialogOpen}
          onClose={() => setIsCreateDialogOpen(false)}
          onLoadcaseCreated={() => {
            setIsCreateDialogOpen(false);
            loadLoadcases();
          }}
        />
      )}
    </div>
  );
});

export default LoadcasesTab;
