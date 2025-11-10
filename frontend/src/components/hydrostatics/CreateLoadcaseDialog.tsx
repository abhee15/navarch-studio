import { useState } from "react";
import { loadcasesApi } from "../../services/hydrostaticsApi";
import { getErrorMessage } from "../../types/errors";
import type { CreateLoadcaseDto } from "../../types/hydrostatics";
import { Dialog, DialogHeader, DialogDescription, DialogContent, DialogFooter } from "../ui/dialog";
import { Button } from "../ui/button";
import { Input } from "../ui/input";
import { Label } from "../ui/label";
import { Anchor } from "lucide-react";

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
      setError(getErrorMessage(err));
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

  return (
    <Dialog isOpen={isOpen} onClose={onClose} maxWidth="lg">
      <DialogHeader icon={<Anchor className="h-6 w-6 text-primary" />}>
        Create New Loadcase
      </DialogHeader>

      <DialogDescription>Define a load condition for hydrostatic analysis</DialogDescription>

      <form onSubmit={handleSubmit}>
        <DialogContent>
          {error && (
            <div className="mb-4 bg-destructive/10 border border-destructive/50 text-destructive px-3 py-2 rounded text-sm">
              {error}
            </div>
          )}

          <div className="space-y-4">
            {/* Name */}
            <div>
              <Label htmlFor="name">Loadcase Name *</Label>
              <Input
                type="text"
                name="name"
                id="name"
                required
                value={formData.name}
                onChange={handleChange}
                placeholder="e.g., Full Load, Ballast, Design"
                className="mt-1"
              />
            </div>

            {/* Rho (density) */}
            <div>
              <Label htmlFor="rho">Water Density (ρ) *</Label>
              <Input
                type="number"
                name="rho"
                id="rho"
                required
                step="0.1"
                min="0"
                value={formData.rho || ""}
                onChange={handleChange}
                className="mt-1"
              />
              <p className="mt-1 text-xs text-muted-foreground">
                kg/m³ (1025 for seawater, 1000 for freshwater)
              </p>
            </div>

            {/* KG (center of gravity) */}
            <div>
              <Label htmlFor="kg">Vertical Center of Gravity (KG)</Label>
              <Input
                type="number"
                name="kg"
                id="kg"
                step="0.1"
                min="0"
                value={formData.kg || ""}
                onChange={handleChange}
                placeholder="Optional"
                className="mt-1"
              />
              <p className="mt-1 text-xs text-muted-foreground">
                Meters from keel. Required for GM calculations.
              </p>
            </div>

            {/* Notes */}
            <div>
              <Label htmlFor="notes">Notes</Label>
              <textarea
                name="notes"
                id="notes"
                rows={3}
                value={formData.notes}
                onChange={handleChange}
                className="mt-1 block w-full border border-input bg-background text-foreground rounded-md px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                placeholder="Optional description or notes"
              />
            </div>
          </div>
        </DialogContent>

        <DialogFooter>
          <Button type="submit" variant="default" disabled={loading} className="sm:col-start-2">
            {loading ? "Creating..." : "Create Loadcase"}
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={onClose}
            disabled={loading}
            className="sm:col-start-1"
          >
            Cancel
          </Button>
        </DialogFooter>
      </form>
    </Dialog>
  );
}

export default CreateLoadcaseDialog;
