import { useState } from "react";
import { vesselsApi } from "../../services/hydrostaticsApi";
import { getErrorMessage } from "../../types/errors";
import type { CreateVesselDto } from "../../types/hydrostatics";
import { Dialog, DialogHeader, DialogDescription, DialogContent, DialogFooter } from "../ui/dialog";
import { Button } from "../ui/button";
import { Input } from "../ui/input";
import { Label } from "../ui/label";
import { Ship } from "lucide-react";

interface CreateVesselDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onVesselCreated: () => void;
}

export function CreateVesselDialog({ isOpen, onClose, onVesselCreated }: CreateVesselDialogProps) {
  const [formData, setFormData] = useState<CreateVesselDto>({
    name: "",
    description: "",
    lpp: 100,
    beam: 20,
    designDraft: 10,
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    try {
      await vesselsApi.create(formData);
      onVesselCreated();
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  const handleChange = (
    e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>
  ) => {
    const { name, value } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]:
        name === "lpp" || name === "beam" || name === "designDraft"
          ? parseFloat(value) || 0
          : value,
    }));
  };

  return (
    <Dialog isOpen={isOpen} onClose={onClose} maxWidth="lg">
      <DialogHeader icon={<Ship className="h-6 w-6 text-primary" />}>
        Create New Vessel
      </DialogHeader>

      <DialogDescription>Enter the principal particulars for your vessel</DialogDescription>

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
              <Label htmlFor="name">Vessel Name *</Label>
              <Input
                type="text"
                name="name"
                id="name"
                required
                value={formData.name}
                onChange={handleChange}
                placeholder="e.g., MV Example Ship"
                className="mt-1"
              />
            </div>

            {/* Description */}
            <div>
              <Label htmlFor="description">Description</Label>
              <textarea
                name="description"
                id="description"
                rows={2}
                value={formData.description}
                onChange={handleChange}
                className="mt-1 block w-full border border-input bg-background text-foreground rounded-md px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                placeholder="Brief description of the vessel"
              />
            </div>

            {/* Principal Particulars */}
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label htmlFor="lpp">Lpp (m) *</Label>
                <Input
                  type="number"
                  name="lpp"
                  id="lpp"
                  required
                  step="0.1"
                  min="0"
                  value={formData.lpp}
                  onChange={handleChange}
                  className="mt-1"
                />
                <p className="mt-1 text-xs text-muted-foreground">Length between perpendiculars</p>
              </div>

              <div>
                <Label htmlFor="beam">Beam (m) *</Label>
                <Input
                  type="number"
                  name="beam"
                  id="beam"
                  required
                  step="0.1"
                  min="0"
                  value={formData.beam}
                  onChange={handleChange}
                  className="mt-1"
                />
                <p className="mt-1 text-xs text-muted-foreground">Maximum breadth</p>
              </div>
            </div>

            <div>
              <Label htmlFor="designDraft">Design Draft (m) *</Label>
              <Input
                type="number"
                name="designDraft"
                id="designDraft"
                required
                step="0.1"
                min="0"
                value={formData.designDraft}
                onChange={handleChange}
                className="mt-1"
              />
              <p className="mt-1 text-xs text-muted-foreground">
                Values are in your preferred unit system (see Settings)
              </p>
            </div>
          </div>
        </DialogContent>

        <DialogFooter>
          <Button type="submit" variant="default" disabled={loading} className="sm:col-start-2">
            {loading ? "Creating..." : "Create Vessel"}
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

export default CreateVesselDialog;
