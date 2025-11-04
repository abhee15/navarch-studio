import type { CreateVesselDto } from "../../../types/hydrostatics";
import { Select } from "../../ui/select";

interface BuilderSectionsProps {
  formData: CreateVesselDto;
  onChange: (data: Partial<CreateVesselDto>) => void;
  quickMode: boolean;
}

const VESSEL_TYPES = ["Boat", "Yacht", "Ship"] as const;
const SIZES = ["Small", "Medium", "Large"] as const;
const HULL_FAMILIES = ["Wigley", "Series 60", "NPL", "Prismatic"] as const;
const HULL_MATERIALS = ["Steel", "Aluminium", "FRP", "Wood"] as const;
const SUPERSTRUCTURE_MATERIALS = ["Aluminium", "Composite", "Steel"] as const;

export function BuilderSections({ formData, onChange, quickMode }: BuilderSectionsProps) {
  return (
    <div className="bg-card rounded-2xl shadow border border-border p-4">
      <div className="space-y-4">
        {/* Vessel Name */}
        <div>
          <label className="block text-xs font-medium text-muted-foreground">Vessel Name</label>
          <input
            value={formData.name}
            onChange={(e) => onChange({ name: e.target.value })}
            className="mt-1 w-full rounded-xl border border-input bg-background px-3 py-2 focus:outline-none focus:ring-2 focus:ring-ring"
            placeholder="Enter vessel name"
          />
        </div>

        {/* Type and Size */}
        <div className="grid grid-cols-2 gap-3">
          <div>
            <label className="block text-xs font-medium text-muted-foreground">Type</label>
            <Select
              value={formData.metadata?.vesselType || "Boat"}
              onChange={(value) =>
                onChange({
                  metadata: {
                    ...formData.metadata,
                    vesselType: value as "Boat" | "Yacht" | "Ship",
                  },
                })
              }
              options={VESSEL_TYPES.map((type) => ({ value: type, label: type }))}
              className="mt-1 w-full"
            />
          </div>
          <div>
            <label className="block text-xs font-medium text-muted-foreground">Size</label>
            <Select
              value={formData.metadata?.size || "Small"}
              onChange={(value) =>
                onChange({
                  metadata: {
                    ...formData.metadata,
                    size: value as "Small" | "Medium" | "Large",
                  },
                })
              }
              options={SIZES.map((size) => ({ value: size, label: size }))}
              className="mt-1 w-full"
            />
          </div>
        </div>

        {/* Principal Dimensions */}
        <div className="rounded-xl border border-border p-3">
          <h3 className="text-sm font-semibold mb-2">Principal Dimensions</h3>
          <div className="grid grid-cols-3 gap-3">
            <div>
              <label className="block text-[11px] text-muted-foreground">L (m)</label>
              <input
                type="number"
                step="0.1"
                value={formData.lpp}
                onChange={(e) => onChange({ lpp: parseFloat(e.target.value) || 0 })}
                className="w-full rounded-lg border border-input bg-background px-2 py-1.5"
              />
            </div>
            <div>
              <label className="block text-[11px] text-muted-foreground">B (m)</label>
              <input
                type="number"
                step="0.1"
                value={formData.beam}
                onChange={(e) => onChange({ beam: parseFloat(e.target.value) || 0 })}
                className="w-full rounded-lg border border-input bg-background px-2 py-1.5"
              />
            </div>
            <div>
              <label className="block text-[11px] text-muted-foreground">T (m)</label>
              <input
                type="number"
                step="0.1"
                value={formData.designDraft}
                onChange={(e) => onChange({ designDraft: parseFloat(e.target.value) || 0 })}
                className="w-full rounded-lg border border-input bg-background px-2 py-1.5"
              />
            </div>
          </div>

          <div className="mt-2">
            <label className="block text-[11px] text-muted-foreground">
              Block Coefficient (Cb)
            </label>
            <input
              type="number"
              step="0.01"
              min={0.3}
              max={0.9}
              value={formData.metadata?.blockCoefficient || 0.5}
              onChange={(e) =>
                onChange({
                  metadata: {
                    ...formData.metadata,
                    blockCoefficient: parseFloat(e.target.value) || 0.5,
                  },
                })
              }
              className="w-full rounded-lg border border-input bg-background px-2 py-1.5"
            />
            <p className="text-[11px] text-muted-foreground mt-1">
              Seeded from Type/Size. Adjust as needed.
            </p>
          </div>
        </div>

        {/* Optional Sections (hidden in Quick Mode) */}
        {!quickMode && (
          <div className="space-y-3">
            {/* Offsets */}
            <div className="rounded-xl border border-border p-3">
              <div className="flex items-center justify-between mb-2">
                <h3 className="text-sm font-semibold">Offsets</h3>
              </div>
              <div className="space-y-2">
                <div>
                  <label className="block text-[11px] text-muted-foreground">Hull Family</label>
                  <Select
                    value={formData.metadata?.hullFamily || "Wigley"}
                    onChange={(value) =>
                      onChange({
                        metadata: {
                          ...formData.metadata,
                          hullFamily: value as "Wigley" | "Series 60" | "NPL" | "Prismatic",
                        },
                      })
                    }
                    options={HULL_FAMILIES.map((family) => ({ value: family, label: family }))}
                    className="mt-1 w-full text-xs"
                  />
                </div>
                <div>
                  <label className="block text-[11px] text-muted-foreground">
                    Upload Offsets (CSV)
                  </label>
                  <input type="file" accept=".csv" className="w-full text-xs mt-1" />
                  <p className="text-[11px] text-muted-foreground mt-1">
                    Pick a hull family or upload CSV later
                  </p>
                </div>
              </div>
            </div>

            {/* Loading Conditions */}
            <div className="rounded-xl border border-border p-3">
              <h3 className="text-sm font-semibold mb-2">Loading Conditions</h3>
              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className="block text-[11px] text-muted-foreground">Lightship (t)</label>
                  <input
                    type="number"
                    value={formData.loading?.lightshipTonnes || ""}
                    onChange={(e) =>
                      onChange({
                        loading: {
                          ...formData.loading,
                          lightshipTonnes: parseFloat(e.target.value) || undefined,
                        },
                      })
                    }
                    className="w-full rounded-lg border border-input bg-background px-2 py-1.5"
                    placeholder="e.g., 1200"
                  />
                </div>
                <div>
                  <label className="block text-[11px] text-muted-foreground">Deadweight (t)</label>
                  <input
                    type="number"
                    value={formData.loading?.deadweightTonnes || ""}
                    onChange={(e) =>
                      onChange({
                        loading: {
                          ...formData.loading,
                          deadweightTonnes: parseFloat(e.target.value) || undefined,
                        },
                      })
                    }
                    className="w-full rounded-lg border border-input bg-background px-2 py-1.5"
                    placeholder="e.g., 800"
                  />
                </div>
              </div>
            </div>

            {/* Materials */}
            <div className="rounded-xl border border-border p-3">
              <h3 className="text-sm font-semibold mb-2">Materials</h3>
              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className="block text-[11px] text-muted-foreground">Hull</label>
                  <Select
                    value={formData.materials?.hullMaterial || ""}
                    onChange={(value) =>
                      onChange({
                        materials: {
                          ...formData.materials,
                          hullMaterial: (value || undefined) as
                            | "Steel"
                            | "Aluminium"
                            | "FRP"
                            | "Wood"
                            | undefined,
                        },
                      })
                    }
                    options={[
                      { value: "", label: "Select..." },
                      ...HULL_MATERIALS.map((material) => ({ value: material, label: material })),
                    ]}
                    className="w-full text-xs"
                  />
                </div>
                <div>
                  <label className="block text-[11px] text-muted-foreground">Superstructure</label>
                  <Select
                    value={formData.materials?.superstructureMaterial || ""}
                    onChange={(value) =>
                      onChange({
                        materials: {
                          ...formData.materials,
                          superstructureMaterial: (value || undefined) as
                            | "Aluminium"
                            | "Composite"
                            | "Steel"
                            | undefined,
                        },
                      })
                    }
                    options={[
                      { value: "", label: "Select..." },
                      ...SUPERSTRUCTURE_MATERIALS.map((material) => ({
                        value: material,
                        label: material,
                      })),
                    ]}
                    className="w-full text-xs"
                  />
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
