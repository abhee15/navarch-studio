import React from "react";
import { useNavigate } from "react-router-dom";
import type { CreateMissionCaseDto } from "../../../types/sizing";
import { Button } from "../../ui/button";
import { Input } from "../../ui/input";
import { Label } from "../../ui/label";
import { Select } from "../../ui/select";

interface Step1Props {
  formData: Partial<CreateMissionCaseDto>;
  updateFormData: (data: Partial<CreateMissionCaseDto>) => void;
  onNext: () => void;
  onPrevious: () => void;
  onSubmit: () => void;
  isFirstStep: boolean;
  isLastStep: boolean;
  metadataLoading: boolean;
  metadataError: string | null;
  categoryOptions: { value: string; label: string }[];
  vesselTypeOptions: { value: string; label: string; description?: string | null }[];
  onReloadMetadata: () => void;
  nameConflict: boolean;
  nameConflictMessage?: string | null;
}

export const Step1MissionCargo: React.FC<Step1Props> = ({
  formData,
  updateFormData,
  onNext,
  metadataLoading,
  metadataError,
  categoryOptions,
  vesselTypeOptions,
  onReloadMetadata,
  nameConflict,
  nameConflictMessage,
}) => {
  const navigate = useNavigate();
  const trimmedName = formData.name?.trim() ?? "";
  const isValid =
    trimmedName.length > 0 &&
    !!formData.missionCategory &&
    !!formData.missionType &&
    formData.cargoValue !== undefined &&
    formData.cargoValue > 0;

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white">Vessel Requirements</h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          Select vessel taxonomy and define cargo requirements
        </p>
        {metadataLoading && (
          <p className="mt-2 text-xs text-blue-600 dark:text-blue-300">
            Loading ShipD taxonomy metadata…
          </p>
        )}
        {metadataError && (
          <div className="mt-2 rounded-md border border-yellow-400 bg-yellow-50 p-3 text-xs text-yellow-700 dark:border-yellow-700 dark:bg-yellow-900/20 dark:text-yellow-200">
            <p className="font-semibold">Metadata unavailable</p>
            <p className="mt-1">
              {metadataError}. You can continue with fallback taxonomy, or{" "}
              <button
                type="button"
                className="underline decoration-dotted hover:text-yellow-900 dark:hover:text-yellow-100"
                onClick={onReloadMetadata}
              >
                retry loading metadata
              </button>
              .
            </p>
          </div>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="name">Vessel Name *</Label>
        <Input
          id="name"
          placeholder="e.g., 5000 TEU Feeder Container"
          value={formData.name || ""}
          onChange={(e) => updateFormData({ name: e.target.value })}
          aria-invalid={nameConflict}
          aria-describedby={nameConflict ? "mission-name-conflict" : undefined}
        />
        <p className="text-xs text-gray-500 dark:text-gray-400">
          Use a unique brief name to help you find designs later.
        </p>
        {nameConflict && (
          <p
            id="mission-name-conflict"
            className="text-xs font-medium text-red-600 dark:text-red-400"
          >
            {nameConflictMessage ||
              "A brief with this name already exists. Choose a different name."}
          </p>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="missionCategory">Category *</Label>
        <Select
          id="missionCategory"
          value={formData.missionCategory || categoryOptions[0]?.value || ""}
          onChange={(value) =>
            updateFormData({
              missionCategory: value,
              missionType: vesselTypeOptions[0]?.value,
              bowFamily: undefined,
              midshipFamily: undefined,
              sternFamily: undefined,
            })
          }
          options={categoryOptions}
        />
        <p className="text-xs text-gray-500 dark:text-gray-400">
          Select the primary mission category to filter applicable vessel types.
        </p>
      </div>

      <div className="space-y-2">
        <Label htmlFor="missionType">Vessel Type *</Label>
        <Select
          id="missionType"
          value={formData.missionType || vesselTypeOptions[0]?.value || ""}
          onChange={(value) =>
            updateFormData({
              missionType: value,
              bowFamily: undefined,
              midshipFamily: undefined,
              sternFamily: undefined,
            })
          }
          options={vesselTypeOptions.map((type) => ({
            value: type.value,
            label: type.label,
          }))}
        />
        {formData.missionType && (
          <p className="text-xs text-gray-500 dark:text-gray-400">
            {vesselTypeOptions.find((option) => option.value === formData.missionType)?.description}
          </p>
        )}
      </div>

      <div className="space-y-2">
        <Label htmlFor="cargoBasis">Cargo Basis *</Label>
        <Select
          id="cargoBasis"
          value={formData.cargoBasis || "teu"}
          onChange={(value) => updateFormData({ cargoBasis: value as "teu" | "weight" | "volume" })}
          options={[
            { value: "teu", label: "TEU (Twenty-foot Equivalent Units)" },
            { value: "weight", label: "Weight (tonnes)" },
            { value: "volume", label: "Volume (m³)" },
          ]}
        />
        <p className="text-xs text-gray-500 dark:text-gray-400">
          {formData.cargoBasis === "teu" && "Number of standard 20-foot containers"}
          {formData.cargoBasis === "weight" && "Total cargo weight in metric tonnes"}
          {formData.cargoBasis === "volume" && "Total cargo volume in cubic meters"}
        </p>
      </div>

      {formData.cargoBasis === "teu" && (
        <div className="space-y-2">
          <Label htmlFor="teuCount">TEU Count *</Label>
          <Input
            id="teuCount"
            type="number"
            placeholder="e.g., 5000"
            value={formData.teuCount || formData.cargoValue || ""}
            onChange={(e) => {
              const value = parseFloat(e.target.value);
              updateFormData({
                teuCount: value,
                cargoValue: value,
              });
            }}
          />
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Typical: Feeder 500-2000, Regional 2000-5000, Mainline 5000-15000
          </p>
        </div>
      )}

      {formData.cargoBasis === "weight" && (
        <div className="space-y-2">
          <Label htmlFor="cargoWeight">Cargo Weight (tonnes) *</Label>
          <Input
            id="cargoWeight"
            type="number"
            placeholder="e.g., 50000"
            value={formData.cargoValue || ""}
            onChange={(e) => updateFormData({ cargoValue: parseFloat(e.target.value) })}
          />
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Typical: Small cargo 5,000-15,000t, Medium 15,000-50,000t, Large 50,000-150,000t
          </p>
        </div>
      )}

      {formData.cargoBasis === "volume" && (
        <div className="space-y-2">
          <Label htmlFor="cargoVolume">Cargo Volume (m³) *</Label>
          <Input
            id="cargoVolume"
            type="number"
            placeholder="e.g., 10000"
            value={formData.cargoVolumeM3 || formData.cargoValue || ""}
            onChange={(e) => {
              const value = parseFloat(e.target.value);
              updateFormData({
                cargoVolumeM3: value,
                cargoValue: value,
              });
            }}
          />
        </div>
      )}

      {/* Cargo Density - Show for volume and TEU */}
      {(formData.cargoBasis === "volume" || formData.cargoBasis === "teu") && (
        <div className="space-y-2">
          <Label htmlFor="cargoDensity">
            Cargo Density (t/m³) {formData.cargoBasis === "volume" ? "*" : ""}
          </Label>
          <Input
            id="cargoDensity"
            type="number"
            step="0.1"
            placeholder={formData.cargoBasis === "teu" ? "0.5 (default)" : "e.g., 0.8"}
            value={formData.cargoDensityTPerM3 || ""}
            onChange={(e) =>
              updateFormData({ cargoDensityTPerM3: parseFloat(e.target.value) || undefined })
            }
          />
          <p className="text-xs text-gray-500 dark:text-gray-400">
            {formData.cargoBasis === "teu"
              ? "Optional: For cargo holds sizing. Default: 0.5 t/m³ (typical containers)"
              : "Required: Used to convert volume to weight. Typical: Grain 0.6-0.8, Coal 0.8-1.0, Iron ore 2.0-2.5"}
          </p>
        </div>
      )}

      <div className="space-y-2">
        <Label htmlFor="notes">Notes (Optional)</Label>
        <textarea
          id="notes"
          rows={3}
          className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm dark:border-gray-600 dark:bg-gray-700 dark:text-white"
          placeholder="Additional brief requirements or notes..."
          value={formData.notes || ""}
          onChange={(e) => updateFormData({ notes: e.target.value })}
        />
      </div>

      <div className="flex justify-end space-x-4 pt-6 border-t border-gray-200 dark:border-gray-700">
        <Button variant="outline" onClick={() => navigate("/sizing/missions")}>
          Cancel
        </Button>
        <Button onClick={onNext} disabled={!isValid || nameConflict}>
          Next: Hull Families →
        </Button>
      </div>
    </div>
  );
};
