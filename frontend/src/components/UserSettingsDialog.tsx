import { useState, useEffect } from "react";
import { observer } from "mobx-react-lite";
import { settingsStore, type UnitSystem } from "../stores/SettingsStore";
import { getErrorMessage } from "../types/errors";
import { Select } from "./ui/select";
import { Dialog, DialogHeader, DialogDescription, DialogContent, DialogFooter } from "./ui/dialog";
import { Button } from "./ui/button";
import { Settings } from "lucide-react";

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

  return (
    <Dialog isOpen={isOpen} onClose={onClose} maxWidth="lg">
      <DialogHeader icon={<Settings className="h-6 w-6 text-primary" />}>
        User Settings
      </DialogHeader>

      <DialogDescription>
        Configure your display preferences and units of measurement
      </DialogDescription>

      <DialogContent>
        {error && (
          <div className="mb-4 bg-destructive/10 border border-destructive/50 text-destructive px-4 py-3 rounded">
            {error}
          </div>
        )}

        <div className="space-y-4">
          {/* Unit System Selection */}
          <div>
            <label
              htmlFor="preferredUnits"
              className="block text-sm font-medium text-foreground mb-2"
            >
              Preferred Unit System
            </label>
            <p className="text-sm text-muted-foreground mb-3">
              Choose how you want to view measurements throughout the application. Vessels will
              display in your preferred units regardless of their native unit system.
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
          <div className="bg-muted p-4 rounded-md">
            <h4 className="text-sm font-medium text-foreground mb-2">Preview</h4>
            <div className="text-sm text-muted-foreground space-y-1">
              <div className="flex justify-between">
                <span>Length:</span>
                <span className="font-mono text-foreground">
                  {preferredUnits === "SI" ? "10.0 m" : "32.81 ft"}
                </span>
              </div>
              <div className="flex justify-between">
                <span>Mass:</span>
                <span className="font-mono text-foreground">
                  {preferredUnits === "SI" ? "1000 kg" : "2204.62 lb"}
                </span>
              </div>
              <div className="flex justify-between">
                <span>Area:</span>
                <span className="font-mono text-foreground">
                  {preferredUnits === "SI" ? "50.0 m²" : "538.20 ft²"}
                </span>
              </div>
              <div className="flex justify-between">
                <span>Density:</span>
                <span className="font-mono text-foreground">
                  {preferredUnits === "SI" ? "1025 kg/m³" : "63.99 lb/ft³"}
                </span>
              </div>
            </div>
          </div>
        </div>
      </DialogContent>

      <DialogFooter>
        <Button variant="default" onClick={handleSave} disabled={saving} className="sm:col-start-2">
          {saving ? "Saving..." : "Save Settings"}
        </Button>
        <Button variant="outline" onClick={onClose} disabled={saving} className="sm:col-start-1">
          Cancel
        </Button>
      </DialogFooter>
    </Dialog>
  );
});
