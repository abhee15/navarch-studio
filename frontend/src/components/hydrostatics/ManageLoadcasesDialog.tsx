import { useState, useEffect } from "react";
import { loadcasesApi } from "../../services/hydrostaticsApi";
import { getErrorMessage } from "../../types/errors";
import type { Loadcase, CreateLoadcaseDto } from "../../types/hydrostatics";
import { CreateLoadcaseDialog } from "./CreateLoadcaseDialog";

interface ManageLoadcasesDialogProps {
  vesselId: string;
  isOpen: boolean;
  onClose: () => void;
  onLoadcasesUpdated: () => void;
}

// Hook to detect mobile
function useIsMobile() {
  const [isMobile, setIsMobile] = useState(false);

  useEffect(() => {
    const checkMobile = () => {
      setIsMobile(window.innerWidth < 768);
    };
    checkMobile();
    window.addEventListener("resize", checkMobile);
    return () => window.removeEventListener("resize", checkMobile);
  }, []);

  return isMobile;
}

export function ManageLoadcasesDialog({
  vesselId,
  isOpen,
  onClose,
  onLoadcasesUpdated,
}: ManageLoadcasesDialogProps) {
  const isMobile = useIsMobile();
  const [loadcases, setLoadcases] = useState<Loadcase[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [editingLoadcase, setEditingLoadcase] = useState<Loadcase | null>(null);
  const [deletingLoadcaseId, setDeletingLoadcaseId] = useState<string | null>(null);

  const loadLoadcases = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await loadcasesApi.list(vesselId);
      setLoadcases(data.loadcases);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isOpen) {
      loadLoadcases();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isOpen, vesselId]);

  const handleCreateClick = () => {
    setShowCreateDialog(true);
  };

  const handleCreateClose = () => {
    setShowCreateDialog(false);
  };

  const handleCreateSuccess = () => {
    setShowCreateDialog(false);
    loadLoadcases();
    onLoadcasesUpdated();
  };

  const handleEditClick = (loadcase: Loadcase) => {
    setEditingLoadcase(loadcase);
  };

  const handleEditCancel = () => {
    setEditingLoadcase(null);
  };

  const handleEditSave = async (updatedData: CreateLoadcaseDto) => {
    if (!editingLoadcase) return;

    try {
      setLoading(true);
      setError(null);
      await loadcasesApi.update(vesselId, editingLoadcase.id, updatedData);
      setEditingLoadcase(null);
      loadLoadcases();
      onLoadcasesUpdated();
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteClick = (loadcaseId: string) => {
    setDeletingLoadcaseId(loadcaseId);
  };

  const handleDeleteConfirm = async () => {
    if (!deletingLoadcaseId) return;

    try {
      setLoading(true);
      setError(null);
      await loadcasesApi.delete(vesselId, deletingLoadcaseId);
      setDeletingLoadcaseId(null);
      loadLoadcases();
      onLoadcasesUpdated();
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteCancel = () => {
    setDeletingLoadcaseId(null);
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

          <div className="inline-block align-bottom bg-white dark:bg-gray-800 rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-4xl sm:w-full w-full max-h-[90vh] flex flex-col">
            {/* Header */}
            <div className="bg-white dark:bg-gray-800 px-4 pt-5 pb-4 sm:p-6 border-b border-gray-200 dark:border-gray-700">
              <div className="flex items-center justify-between">
                <div>
                  <h3
                    className="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100"
                    id="modal-title"
                  >
                    Manage Loadcases
                  </h3>
                  <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
                    Create, edit, or delete load conditions for this vessel
                  </p>
                </div>
                <button
                  onClick={onClose}
                  className="text-gray-400 hover:text-gray-500 dark:hover:text-gray-300"
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
            <div className="bg-white dark:bg-gray-800 px-4 pt-5 pb-4 sm:p-6 flex-1 overflow-y-auto">
              {error && (
                <div className="mb-4 bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 px-3 py-2 rounded text-sm">
                  {error}
                </div>
              )}

              {/* Create Button */}
              <div className="mb-4">
                <button
                  onClick={handleCreateClick}
                  className="w-full sm:w-auto inline-flex items-center justify-center px-4 py-2 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
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
                      d="M12 6v6m0 0v6m0-6h6m-6 0H6"
                    />
                  </svg>
                  Create New Loadcase
                </button>
              </div>

              {/* Loadcases Display */}
              {loading && loadcases.length === 0 ? (
                <div className="text-center py-8">
                  <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600 mx-auto"></div>
                  <p className="mt-2 text-sm text-gray-500 dark:text-gray-400">
                    Loading loadcases...
                  </p>
                </div>
              ) : loadcases.length === 0 ? (
                <div className="text-center py-8">
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
                      d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"
                    />
                  </svg>
                  <h3 className="mt-2 text-sm font-medium text-gray-900 dark:text-gray-100">
                    No loadcases
                  </h3>
                  <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
                    Get started by creating a new loadcase.
                  </p>
                </div>
              ) : isMobile ? (
                /* Mobile: Card-based layout */
                <div className="space-y-3">
                  {loadcases.map((loadcase) => {
                    if (editingLoadcase?.id === loadcase.id) {
                      return (
                        <EditLoadcaseCard
                          key={loadcase.id}
                          loadcase={loadcase}
                          onSave={handleEditSave}
                          onCancel={handleEditCancel}
                        />
                      );
                    }
                    return (
                      <LoadcaseCard
                        key={loadcase.id}
                        loadcase={loadcase}
                        onEdit={handleEditClick}
                        onDelete={handleDeleteClick}
                        isDeleting={deletingLoadcaseId === loadcase.id}
                        onDeleteConfirm={handleDeleteConfirm}
                        onDeleteCancel={handleDeleteCancel}
                      />
                    );
                  })}
                </div>
              ) : (
                /* Desktop: Table layout */
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                    <thead className="bg-gray-50 dark:bg-gray-700">
                      <tr>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                          Name
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                          Density (kg/m³)
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                          KG (m)
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                          Notes
                        </th>
                        <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                          Actions
                        </th>
                      </tr>
                    </thead>
                    <tbody className="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
                      {loadcases.map((loadcase) => {
                        if (editingLoadcase?.id === loadcase.id) {
                          return (
                            <EditLoadcaseRow
                              key={loadcase.id}
                              loadcase={loadcase}
                              onSave={handleEditSave}
                              onCancel={handleEditCancel}
                            />
                          );
                        }
                        return (
                          <LoadcaseRow
                            key={loadcase.id}
                            loadcase={loadcase}
                            onEdit={handleEditClick}
                            onDelete={handleDeleteClick}
                            isDeleting={deletingLoadcaseId === loadcase.id}
                            onDeleteConfirm={handleDeleteConfirm}
                            onDeleteCancel={handleDeleteCancel}
                          />
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
                className="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:ml-3 sm:w-auto sm:text-sm"
              >
                Close
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Create Dialog */}
      {showCreateDialog && (
        <CreateLoadcaseDialog
          vesselId={vesselId}
          isOpen={showCreateDialog}
          onClose={handleCreateClose}
          onLoadcaseCreated={handleCreateSuccess}
        />
      )}
    </>
  );
}

interface LoadcaseRowProps {
  loadcase: Loadcase;
  onEdit: (loadcase: Loadcase) => void;
  onDelete: (id: string) => void;
  isDeleting: boolean;
  onDeleteConfirm: () => void;
  onDeleteCancel: () => void;
}

function LoadcaseRow({
  loadcase,
  onEdit,
  onDelete,
  isDeleting,
  onDeleteConfirm,
  onDeleteCancel,
}: LoadcaseRowProps) {
  if (isDeleting) {
    return (
      <tr className="bg-yellow-50 dark:bg-yellow-900/20">
        <td colSpan={5} className="px-4 py-3">
          <div className="flex items-center justify-between">
            <span className="text-sm text-gray-900 dark:text-gray-100">
              Are you sure you want to delete "{loadcase.name}"?
            </span>
            <div className="flex gap-2">
              <button
                onClick={onDeleteConfirm}
                className="px-3 py-1 text-sm font-medium text-white bg-red-600 hover:bg-red-700 rounded"
              >
                Delete
              </button>
              <button
                onClick={onDeleteCancel}
                className="px-3 py-1 text-sm font-medium text-gray-700 dark:text-gray-300 bg-gray-200 dark:bg-gray-600 hover:bg-gray-300 dark:hover:bg-gray-500 rounded"
              >
                Cancel
              </button>
            </div>
          </div>
        </td>
      </tr>
    );
  }

  return (
    <tr className="hover:bg-gray-50 dark:hover:bg-gray-700">
      <td className="px-4 py-3 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">
        {loadcase.name}
      </td>
      <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
        {loadcase.rho}
      </td>
      <td className="px-4 py-3 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
        {loadcase.kg ?? "—"}
      </td>
      <td className="px-4 py-3 text-sm text-gray-500 dark:text-gray-400">
        {loadcase.notes || "—"}
      </td>
      <td className="px-4 py-3 whitespace-nowrap text-right text-sm font-medium">
        <button
          onClick={() => onEdit(loadcase)}
          className="text-blue-600 hover:text-blue-900 dark:text-blue-400 dark:hover:text-blue-300 mr-4"
        >
          Edit
        </button>
        <button
          onClick={() => onDelete(loadcase.id)}
          className="text-red-600 hover:text-red-900 dark:text-red-400 dark:hover:text-red-300"
        >
          Delete
        </button>
      </td>
    </tr>
  );
}

interface EditLoadcaseRowProps {
  loadcase: Loadcase;
  onSave: (data: CreateLoadcaseDto) => void;
  onCancel: () => void;
}

function EditLoadcaseRow({ loadcase, onSave, onCancel }: EditLoadcaseRowProps) {
  const [formData, setFormData] = useState<CreateLoadcaseDto>({
    name: loadcase.name,
    rho: loadcase.rho,
    kg: loadcase.kg,
    notes: loadcase.notes || "",
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]:
        name === "rho" || name === "kg" ? (value === "" ? undefined : parseFloat(value)) : value,
    }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave(formData);
  };

  return (
    <tr className="bg-blue-50 dark:bg-blue-900/20">
      <td colSpan={5} className="px-4 py-3">
        <form onSubmit={handleSubmit} className="space-y-3">
          <div className="grid grid-cols-4 gap-3 items-end">
            <div>
              <label className="block text-xs font-medium text-gray-700 dark:text-gray-300">
                Name *
              </label>
              <input
                type="text"
                name="name"
                required
                value={formData.name}
                onChange={handleChange}
                className="mt-1 block w-full border border-gray-300 dark:border-gray-600 rounded-md shadow-sm py-1.5 px-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 dark:text-gray-300">
                Density (kg/m³) *
              </label>
              <input
                type="number"
                name="rho"
                required
                step="0.1"
                min="0"
                value={formData.rho || ""}
                onChange={handleChange}
                className="mt-1 block w-full border border-gray-300 dark:border-gray-600 rounded-md shadow-sm py-1.5 px-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 dark:text-gray-300">
                KG (m)
              </label>
              <input
                type="number"
                name="kg"
                step="0.1"
                min="0"
                value={formData.kg || ""}
                onChange={handleChange}
                className="mt-1 block w-full border border-gray-300 dark:border-gray-600 rounded-md shadow-sm py-1.5 px-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
              />
            </div>
            <div className="flex gap-2">
              <button
                type="submit"
                className="px-3 py-1.5 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded"
              >
                Save
              </button>
              <button
                type="button"
                onClick={onCancel}
                className="px-3 py-1.5 text-sm font-medium text-gray-700 dark:text-gray-300 bg-gray-200 dark:bg-gray-600 hover:bg-gray-300 dark:hover:bg-gray-500 rounded"
              >
                Cancel
              </button>
            </div>
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 dark:text-gray-300">
              Notes
            </label>
            <textarea
              name="notes"
              rows={2}
              value={formData.notes}
              onChange={handleChange}
              className="mt-1 block w-full border border-gray-300 dark:border-gray-600 rounded-md shadow-sm py-1.5 px-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        </form>
      </td>
    </tr>
  );
}

// Mobile: Card component for loadcases
interface LoadcaseCardProps {
  loadcase: Loadcase;
  onEdit: (loadcase: Loadcase) => void;
  onDelete: (id: string) => void;
  isDeleting: boolean;
  onDeleteConfirm: () => void;
  onDeleteCancel: () => void;
}

function LoadcaseCard({
  loadcase,
  onEdit,
  onDelete,
  isDeleting,
  onDeleteConfirm,
  onDeleteCancel,
}: LoadcaseCardProps) {
  if (isDeleting) {
    return (
      <div className="bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-4">
        <p className="text-sm text-gray-900 dark:text-gray-100 mb-3">
          Are you sure you want to delete "{loadcase.name}"?
        </p>
        <div className="flex gap-2">
          <button
            onClick={onDeleteConfirm}
            className="flex-1 px-3 py-2 text-sm font-medium text-white bg-red-600 hover:bg-red-700 rounded"
          >
            Delete
          </button>
          <button
            onClick={onDeleteCancel}
            className="flex-1 px-3 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-gray-200 dark:bg-gray-600 hover:bg-gray-300 dark:hover:bg-gray-500 rounded"
          >
            Cancel
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-4">
      <div className="flex items-start justify-between mb-2">
        <h4 className="text-sm font-medium text-gray-900 dark:text-gray-100">{loadcase.name}</h4>
        <div className="flex gap-2">
          <button
            onClick={() => onEdit(loadcase)}
            className="text-blue-600 hover:text-blue-900 dark:text-blue-400 dark:hover:text-blue-300 text-sm"
          >
            Edit
          </button>
          <button
            onClick={() => onDelete(loadcase.id)}
            className="text-red-600 hover:text-red-900 dark:text-red-400 dark:hover:text-red-300 text-sm"
          >
            Delete
          </button>
        </div>
      </div>
      <div className="space-y-1 text-xs text-gray-600 dark:text-gray-400">
        <div>
          <span className="font-medium">Density:</span> {loadcase.rho} kg/m³
        </div>
        {loadcase.kg !== undefined && (
          <div>
            <span className="font-medium">KG:</span> {loadcase.kg} m
          </div>
        )}
        {loadcase.notes && (
          <div>
            <span className="font-medium">Notes:</span> {loadcase.notes}
          </div>
        )}
      </div>
    </div>
  );
}

// Mobile: Card component for editing
interface EditLoadcaseCardProps {
  loadcase: Loadcase;
  onSave: (data: CreateLoadcaseDto) => void;
  onCancel: () => void;
}

function EditLoadcaseCard({ loadcase, onSave, onCancel }: EditLoadcaseCardProps) {
  const [formData, setFormData] = useState<CreateLoadcaseDto>({
    name: loadcase.name,
    rho: loadcase.rho,
    kg: loadcase.kg,
    notes: loadcase.notes || "",
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]:
        name === "rho" || name === "kg" ? (value === "" ? undefined : parseFloat(value)) : value,
    }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSave(formData);
  };

  return (
    <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
      <form onSubmit={handleSubmit} className="space-y-3">
        <div>
          <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">
            Name *
          </label>
          <input
            type="text"
            name="name"
            required
            value={formData.name}
            onChange={handleChange}
            className="block w-full border border-gray-300 dark:border-gray-600 rounded-md shadow-sm py-2 px-3 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          />
        </div>
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">
              Density (kg/m³) *
            </label>
            <input
              type="number"
              name="rho"
              required
              step="0.1"
              min="0"
              value={formData.rho || ""}
              onChange={handleChange}
              className="block w-full border border-gray-300 dark:border-gray-600 rounded-md shadow-sm py-2 px-3 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">
              KG (m)
            </label>
            <input
              type="number"
              name="kg"
              step="0.1"
              min="0"
              value={formData.kg || ""}
              onChange={handleChange}
              className="block w-full border border-gray-300 dark:border-gray-600 rounded-md shadow-sm py-2 px-3 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            />
          </div>
        </div>
        <div>
          <label className="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">
            Notes
          </label>
          <textarea
            name="notes"
            rows={2}
            value={formData.notes}
            onChange={handleChange}
            className="block w-full border border-gray-300 dark:border-gray-600 rounded-md shadow-sm py-2 px-3 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 text-sm focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          />
        </div>
        <div className="flex gap-2 pt-2">
          <button
            type="submit"
            className="flex-1 px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 rounded"
          >
            Save
          </button>
          <button
            type="button"
            onClick={onCancel}
            className="flex-1 px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-gray-200 dark:bg-gray-600 hover:bg-gray-300 dark:hover:bg-gray-500 rounded"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}

export default ManageLoadcasesDialog;
