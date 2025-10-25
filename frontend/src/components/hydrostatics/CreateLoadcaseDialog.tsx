import { useState } from "react";
import { loadcasesApi } from "../../services/hydrostaticsApi";
import type { CreateLoadcaseDto } from "../../types/hydrostatics";

interface CreateLoadcaseDialogProps {
  vesselId: string;
  isOpen: boolean;
  onClose: () => void;
  onLoadcaseCreated: () => void;
}

export function CreateLoadcaseDialog({
  vesselId,
  isOpen,
  onClose,
  onLoadcaseCreated,
}: CreateLoadcaseDialogProps) {
  const [formData, setFormData] = useState<CreateLoadcaseDto>({
    name: "",
    rho: 1025, // Seawater density
    kg: undefined,
    notes: "",
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      await loadcasesApi.create(vesselId, formData);
      onLoadcaseCreated();
      // Reset form
      setFormData({
        name: "",
        rho: 1025,
        kg: undefined,
        notes: "",
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create loadcase");
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]:
        name === "rho" || name === "kg" ? (value === "" ? undefined : parseFloat(value)) : value,
    }));
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

        <div className="inline-block align-bottom bg-white rounded-lg px-4 pt-5 pb-4 text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full sm:p-6">
          <div>
            <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-green-100">
              <svg
                className="h-6 w-6 text-green-600"
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
            </div>
            <div className="mt-3 text-center sm:mt-5">
              <h3 className="text-lg leading-6 font-medium text-gray-900" id="modal-title">
                Create New Loadcase
              </h3>
              <div className="mt-2">
                <p className="text-sm text-gray-500">
                  Define a load condition for hydrostatic analysis
                </p>
              </div>
            </div>
          </div>

          <form onSubmit={handleSubmit} className="mt-5 sm:mt-6">
            {error && (
              <div className="mb-4 bg-red-50 border border-red-200 text-red-700 px-3 py-2 rounded text-sm">
                {error}
              </div>
            )}

            <div className="space-y-4">
              {/* Name */}
              <div>
                <label htmlFor="name" className="block text-sm font-medium text-gray-700">
                  Loadcase Name *
                </label>
                <input
                  type="text"
                  name="name"
                  id="name"
                  required
                  value={formData.name}
                  onChange={handleChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                  placeholder="e.g., Full Load, Ballast, Design"
                />
              </div>

              {/* Rho (density) */}
              <div>
                <label htmlFor="rho" className="block text-sm font-medium text-gray-700">
                  Water Density (ρ) *
                </label>
                <input
                  type="number"
                  name="rho"
                  id="rho"
                  required
                  step="0.1"
                  min="0"
                  value={formData.rho || ""}
                  onChange={handleChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                />
                <p className="mt-1 text-xs text-gray-500">
                  kg/m³ (1025 for seawater, 1000 for freshwater)
                </p>
              </div>

              {/* KG (center of gravity) */}
              <div>
                <label htmlFor="kg" className="block text-sm font-medium text-gray-700">
                  Vertical Center of Gravity (KG)
                </label>
                <input
                  type="number"
                  name="kg"
                  id="kg"
                  step="0.1"
                  min="0"
                  value={formData.kg || ""}
                  onChange={handleChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                  placeholder="Optional"
                />
                <p className="mt-1 text-xs text-gray-500">
                  Meters from keel. Required for GM calculations.
                </p>
              </div>

              {/* Notes */}
              <div>
                <label htmlFor="notes" className="block text-sm font-medium text-gray-700">
                  Notes
                </label>
                <textarea
                  name="notes"
                  id="notes"
                  rows={3}
                  value={formData.notes}
                  onChange={handleChange}
                  className="mt-1 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                  placeholder="Optional description or notes"
                />
              </div>
            </div>

            {/* Action buttons */}
            <div className="mt-5 sm:mt-6 sm:grid sm:grid-cols-2 sm:gap-3 sm:grid-flow-row-dense">
              <button
                type="submit"
                disabled={loading}
                className="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:col-start-2 sm:text-sm disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {loading ? "Creating..." : "Create Loadcase"}
              </button>
              <button
                type="button"
                onClick={onClose}
                disabled={loading}
                className="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:mt-0 sm:col-start-1 sm:text-sm disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Cancel
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

export default CreateLoadcaseDialog;
