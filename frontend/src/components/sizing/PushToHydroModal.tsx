import { useEffect, useMemo, useState } from "react";
import { Ship, AlertTriangle } from "lucide-react";
import type { CandidateDesign, ShipDVesselTaxonomy } from "../../types/sizing";
import type { PushToHydroForm } from "../../stores/SizingStore";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader } from "../ui/dialog";
import { Input } from "../ui/input";
import { Label } from "../ui/label";
import { Button } from "../ui/button";
import { Select } from "../ui/select";
import { cn } from "../../lib/utils";

interface PushToHydroModalProps {
  isOpen: boolean;
  onClose: () => void;
  candidate: CandidateDesign;
  missionName?: string;
  missionCategory?: string;
  taxonomy: ShipDVesselTaxonomy[];
  isSubmitting: boolean;
  error?: string | null;
  onSubmit: (form: PushToHydroForm) => Promise<void>;
}

const formatNumber = (value: number | undefined, digits = 2) => {
  if (value === undefined || Number.isNaN(value)) return "—";
  return Number(value).toFixed(digits);
};

const buildDefaultName = (candidate: CandidateDesign, missionName?: string) => {
  const familyLabel = candidate.hullFamily.replace(/_/g, " ");
  if (missionName) {
    return `${missionName} • ${familyLabel}`;
  }
  return `${familyLabel} Design`;
};

const buildDefaultForm = (
  candidate: CandidateDesign,
  missionName?: string,
  missionCategory?: string
): PushToHydroForm => ({
  vesselName: buildDefaultName(candidate, missionName),
  description: `Generated from Hull Sizing run ${candidate.sizingRunId}`,
  shipdCategory: candidate.vesselCategory ?? missionCategory ?? undefined,
  shipdType: candidate.vesselType ?? undefined,
  shipdTypeDisplayName: candidate.vesselType ? candidate.vesselType.replace(/_/g, " ") : undefined,
  shipdBowFamily: candidate.bowFamily ?? undefined,
  shipdMidshipFamily: candidate.midshipFamily ?? undefined,
  shipdSternFamily: candidate.sternFamily ?? undefined,
  shipdMaskVersion: candidate.familyMaskVersion ?? undefined,
});

export function PushToHydroModal({
  isOpen,
  onClose,
  candidate,
  missionName,
  missionCategory,
  taxonomy,
  isSubmitting,
  error,
  onSubmit,
}: PushToHydroModalProps) {
  const [form, setForm] = useState<PushToHydroForm>(() =>
    buildDefaultForm(candidate, missionName, missionCategory)
  );
  const shipdParameterCount = useMemo(() => {
    if (!candidate.shipdParametersJson) return null;
    try {
      const parsed = JSON.parse(candidate.shipdParametersJson);
      return Array.isArray(parsed) ? parsed.length : null;
    } catch {
      return null;
    }
  }, [candidate.shipdParametersJson]);

  useEffect(() => {
    if (isOpen) {
      setForm(buildDefaultForm(candidate, missionName, missionCategory));
    }
  }, [candidate, missionCategory, missionName, isOpen]);

  const categoryOptions = useMemo(() => {
    const unique = Array.from(new Set(taxonomy.map((entry) => entry.category)));
    return unique
      .filter(Boolean)
      .sort((a, b) => a.localeCompare(b))
      .map((category) => ({
        value: category,
        label: category,
      }));
  }, [taxonomy]);

  const vesselTypeOptions = useMemo(() => {
    if (!form.shipdCategory) return [];
    return taxonomy
      .filter((entry) => entry.category.toLowerCase() === form.shipdCategory!.toLowerCase())
      .map((entry) => ({
        value: entry.type,
        label: entry.displayName ?? entry.type,
      }))
      .sort((a, b) => a.label.localeCompare(b.label));
  }, [form.shipdCategory, taxonomy]);

  const handleFormChange = <K extends keyof PushToHydroForm>(key: K, value: PushToHydroForm[K]) => {
    setForm((prev) => ({
      ...prev,
      [key]: value,
    }));
  };

  const handleCategoryChange = (value: string) => {
    const normalized = value || undefined;
    setForm((prev) => ({
      ...prev,
      shipdCategory: normalized,
      shipdType: undefined,
      shipdTypeDisplayName: undefined,
    }));
  };

  const handleTypeChange = (value: string) => {
    const normalized = value || undefined;
    const taxonomyEntry = taxonomy.find(
      (entry) =>
        entry.category.toLowerCase() === (form.shipdCategory ?? "").toLowerCase() &&
        entry.type === value
    );
    setForm((prev) => ({
      ...prev,
      shipdType: normalized,
      shipdTypeDisplayName: taxonomyEntry?.displayName ?? prev.shipdTypeDisplayName,
      shipdMaskVersion:
        prev.shipdMaskVersion ?? taxonomyEntry?.maskVersion ?? prev.shipdMaskVersion,
    }));
  };

  const taxonomyLoaded = taxonomy.length > 0;
  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    await onSubmit(form);
  };

  return (
    <Dialog isOpen={isOpen} onClose={isSubmitting ? () => undefined : onClose} maxWidth="3xl">
      <form onSubmit={handleSubmit}>
        <DialogHeader
          icon={<Ship className="h-6 w-6 text-primary" />}
          onClose={isSubmitting ? undefined : onClose}
        >
          Push Design to Hydrostatics
        </DialogHeader>
        <DialogDescription>
          Review the vessel metadata and taxonomy details before creating the Hydrostatics vessel.
        </DialogDescription>
        <DialogContent className="space-y-6">
          {error && (
            <div className="flex items-start gap-2 rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
              <AlertTriangle className="mt-0.5 h-4 w-4 flex-shrink-0" />
              <span>{error}</span>
            </div>
          )}

          <div className="grid gap-4 md:grid-cols-2">
            <div className="rounded-lg border border-border bg-muted/30 p-4">
              <h3 className="text-sm font-semibold text-foreground">Candidate Summary</h3>
              <p className="text-xs text-muted-foreground mt-1">
                Hull family{" "}
                <span className="font-medium text-foreground">{candidate.hullFamily}</span> • Rank #
                {candidate.rank}
              </p>
              <div className="mt-3 grid grid-cols-3 gap-3 text-xs">
                <div>
                  <span className="text-muted-foreground block">Lpp (m)</span>
                  <span className="font-semibold text-foreground">
                    {formatNumber(candidate.lppM)}
                  </span>
                </div>
                <div>
                  <span className="text-muted-foreground block">Beam (m)</span>
                  <span className="font-semibold text-foreground">
                    {formatNumber(candidate.beamM)}
                  </span>
                </div>
                <div>
                  <span className="text-muted-foreground block">Draft (m)</span>
                  <span className="font-semibold text-foreground">
                    {formatNumber(candidate.draftM)}
                  </span>
                </div>
              </div>
              <div className="mt-3 grid grid-cols-3 gap-3 text-xs">
                <div>
                  <span className="text-muted-foreground block">Cb</span>
                  <span className="font-semibold text-foreground">
                    {formatNumber(candidate.cb, 3)}
                  </span>
                </div>
                <div>
                  <span className="text-muted-foreground block">Cp</span>
                  <span className="font-semibold text-foreground">
                    {formatNumber(candidate.cp, 3)}
                  </span>
                </div>
                <div>
                  <span className="text-muted-foreground block">Cwp</span>
                  <span className="font-semibold text-foreground">
                    {formatNumber(candidate.cwp, 3)}
                  </span>
                </div>
              </div>
              {shipdParameterCount !== null && (
                <p className="mt-3 text-[11px] text-muted-foreground">
                  ShipD vector available • {shipdParameterCount} parameters
                </p>
              )}
            </div>

            <div className="rounded-lg border border-primary/30 bg-primary/5 p-4">
              <h3 className="text-sm font-semibold text-primary">What gets created?</h3>
              <p className="text-xs text-primary/80 mt-1 leading-relaxed">
                We will create a new Hydrostatics vessel with geometry, taxonomy, and provenance
                details linked back to this candidate. Your geometry and metadata can still be
                edited once inside Hydrostatics if needed.
              </p>
              {!taxonomyLoaded && (
                <div className="mt-4 rounded-md border border-amber-300 bg-amber-50 px-3 py-2 text-xs text-amber-900">
                  ShipD taxonomy metadata hasn&apos;t been loaded yet. Fields will remain editable,
                  but you may want to reload the page if the dropdown options stay empty.
                </div>
              )}
            </div>
          </div>

          <div className="space-y-4">
            <div>
              <Label htmlFor="vesselName">Vessel Name *</Label>
              <Input
                id="vesselName"
                value={form.vesselName ?? ""}
                onChange={(event) => handleFormChange("vesselName", event.target.value)}
                placeholder="e.g., Mission Alpha • Wigley"
                className="mt-1"
                required
                maxLength={120}
              />
            </div>

            <div>
              <Label htmlFor="description">Description</Label>
              <textarea
                id="description"
                value={form.description ?? ""}
                onChange={(event) => handleFormChange("description", event.target.value)}
                rows={3}
                placeholder="Optional summary that will appear in Hydrostatics"
                className={cn(
                  "mt-1 block w-full rounded-md border border-input bg-background px-3 py-2 text-sm text-foreground shadow-sm transition-colors",
                  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                )}
              />
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <Label>ShipD Category</Label>
                <Select
                  value={form.shipdCategory ?? ""}
                  onChange={handleCategoryChange}
                  options={[
                    {
                      value: "",
                      label: taxonomyLoaded ? "Select category" : "Loading categories...",
                    },
                    ...categoryOptions,
                  ]}
                  disabled={!taxonomyLoaded}
                  className="mt-1"
                />
              </div>
              <div>
                <Label>ShipD Vessel Type</Label>
                <Select
                  value={form.shipdType ?? ""}
                  onChange={handleTypeChange}
                  options={[
                    {
                      value: "",
                      label:
                        !form.shipdCategory || vesselTypeOptions.length === 0
                          ? "Select category first"
                          : "Select vessel type",
                    },
                    ...vesselTypeOptions,
                  ]}
                  disabled={!taxonomyLoaded || !form.shipdCategory}
                  className="mt-1"
                />
              </div>
            </div>

            <div className="grid gap-4 md:grid-cols-2">
              <div>
                <Label htmlFor="shipdTypeDisplayName">Display Name</Label>
                <Input
                  id="shipdTypeDisplayName"
                  value={form.shipdTypeDisplayName ?? ""}
                  onChange={(event) => handleFormChange("shipdTypeDisplayName", event.target.value)}
                  placeholder="e.g., Panamax Bulk Carrier"
                  className="mt-1"
                />
              </div>
              <div>
                <Label htmlFor="shipdMaskVersion">Mask Version</Label>
                <Input
                  id="shipdMaskVersion"
                  type="number"
                  min={0}
                  value={form.shipdMaskVersion ?? ""}
                  onChange={(event) =>
                    handleFormChange(
                      "shipdMaskVersion",
                      event.target.value ? Number(event.target.value) : undefined
                    )
                  }
                  placeholder="Auto"
                  className="mt-1"
                />
              </div>
            </div>

            <div className="grid gap-4 md:grid-cols-3">
              <div>
                <Label htmlFor="shipdBowFamily">Bow Family</Label>
                <Input
                  id="shipdBowFamily"
                  value={form.shipdBowFamily ?? ""}
                  onChange={(event) => handleFormChange("shipdBowFamily", event.target.value)}
                  placeholder="e.g., fine_bow"
                  className="mt-1"
                />
              </div>
              <div>
                <Label htmlFor="shipdMidshipFamily">Midship Family</Label>
                <Input
                  id="shipdMidshipFamily"
                  value={form.shipdMidshipFamily ?? ""}
                  onChange={(event) => handleFormChange("shipdMidshipFamily", event.target.value)}
                  placeholder="e.g., prismatic"
                  className="mt-1"
                />
              </div>
              <div>
                <Label htmlFor="shipdSternFamily">Stern Family</Label>
                <Input
                  id="shipdSternFamily"
                  value={form.shipdSternFamily ?? ""}
                  onChange={(event) => handleFormChange("shipdSternFamily", event.target.value)}
                  placeholder="e.g., cruiser_stern"
                  className="mt-1"
                />
              </div>
            </div>
          </div>
        </DialogContent>
        <DialogFooter>
          <Button
            type="submit"
            className="sm:col-start-2"
            disabled={isSubmitting || !form.vesselName}
          >
            {isSubmitting ? "Pushing..." : "Push to Hydrostatics"}
          </Button>
          <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
        </DialogFooter>
      </form>
    </Dialog>
  );
}
