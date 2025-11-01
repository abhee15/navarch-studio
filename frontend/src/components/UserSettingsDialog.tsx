import { useState, useEffect } from "react";
import { observer } from "mobx-react-lite";
import { settingsStore, type UnitSystem } from "../stores/SettingsStore";
import { getErrorMessage } from "../types/errors";
import { Select } from "./ui/select";

interface UserSettingsDialogProps {
  isOpen: boolean;
  onClose: () => void;
}

export const UserSettingsDialog = observer(({ isOpen, onClose }: UserSettingsDialogProps) => {
  const [preferredUnits, setPreferredUnits] = useState<UnitSystem>(settingsStore.preferredUnits);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      setPreferredUnits(settingsStore.preferredUnits);
      setError(null);
    }
  }, [isOpen]);

  const handleSave = async () => {
    setSaving(true);
    setError(null);

    try {
      await settingsStore.updatePreferredUnits(preferredUnits);
      onClose();
    } catch (err) {
      setError(getErrorMessage(err));
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
        {/* Background overlay */}
        <div
          className="fixed inset-0 bg-gray-50 dark:bg-gray-9000 bg-opacity-75 transition-opacity"
          aria-hidden="true"
          onClick={onClose}
        ></div>

        {/* Center modal */}
        <span className="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">
          &#8203;
        </span>

        {/* Modal panel */}
        <div className="inline-block align-bottom bg-white dark:bg-gray-800 rounded-lg px-4 pt-5 pb-4 text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full sm:p-6">
          <div>
            <div className="mt-3 text-center sm:mt-0 sm:text-left">
              <h3
                className="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100 mb-4"
                id="modal-title"
              >
                User Settings
              </h3>

              {error && (
                <div className="mb-4 bg-red-50 dark:bg-red-900/30 border border-red-400 dark:border-red-600 text-red-700 dark:text-red-300 px-4 py-3 rounded">
                  {error}
                </div>
              )}

              <div className="space-y-4">
                {/* Unit System Selection */}
                <div>
                  <label
                    htmlFor="preferredUnits"
                    className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2"
                  >
                    Preferred Unit System
                  </label>
                  <p className="text-sm text-gray-500 dark:text-gray-400 mb-3">
                    Choose how you want to view measurements throughout the application. Vessels
                    will display in your preferred units regardless of their native unit system.
                  </p>
                  <Select
                    id="preferredUnits"
                    name="preferredUnits"
                    value={preferredUnits}
                    onChange={(value) => setPreferredUnits(value as UnitSystem)}
                    options={[
                      { value: "SI", label: "SI (Metric) - meters, kg, m², m³" },
                      { value: "Imperial", label: "Imperial - feet, lb, ft², ft³" },
                    ]}
                    className="mt-1 w-full"
                  />
                </div>

                {/* Preview */}
                <div className="bg-gray-50 dark:bg-gray-900 p-4 rounded-md">
                  <h4 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    Preview
                  </h4>
                  <div className="text-sm text-gray-600 space-y-1">
                    <div className="flex justify-between">
                      <span>Length:</span>
                      <span className="font-mono">
                        {preferredUnits === "SI" ? "10.0 m" : "32.81 ft"}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span>Mass:</span>
                      <span className="font-mono">
                        {preferredUnits === "SI" ? "1000 kg" : "2204.62 lb"}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span>Area:</span>
                      <span className="font-mono">
                        {preferredUnits === "SI" ? "50.0 m²" : "538.20 ft²"}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span>Density:</span>
                      <span className="font-mono">
                        {preferredUnits === "SI" ? "1025 kg/m³" : "63.99 lb/ft³"}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Action buttons */}
          <div className="mt-5 sm:mt-6 sm:grid sm:grid-cols-2 sm:gap-3 sm:grid-flow-row-dense">
            <button
              type="button"
              disabled={saving}
              onClick={handleSave}
              className="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-blue-600 text-base font-medium text-white hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:col-start-2 sm:text-sm disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {saving ? "Saving..." : "Save Settings"}
            </button>
            <button
              type="button"
              disabled={saving}
              onClick={onClose}
              className="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 dark:border-gray-600 shadow-sm px-4 py-2 bg-white text-base font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:bg-gray-900 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 sm:mt-0 sm:col-start-1 sm:text-sm disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Cancel
            </button>
          </div>
        </div>
      </div>
    </div>
  );
});
