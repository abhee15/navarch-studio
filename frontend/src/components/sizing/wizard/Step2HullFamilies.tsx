import React, { useEffect } from "react";
import type { CreateMissionCaseDto, ShipDVesselTaxonomy } from "../../../types/sizing";
import { Button } from "../../ui/button";
import { Label } from "../../ui/label";
import { Select } from "../../ui/select";
import { Input } from "../../ui/input";
import { CollapsibleSection } from "../../hydrostatics/CollapsibleSection";

interface Step2Props {
  formData: Partial<CreateMissionCaseDto>;
  updateFormData: (data: Partial<CreateMissionCaseDto>) => void;
  onNext: () => void;
  onPrevious: () => void;
  onSubmit: () => void;
  isFirstStep: boolean;
  isLastStep: boolean;
  taxonomyEntry?: ShipDVesselTaxonomy;
  metadataLoading: boolean;
  metadataError: string | null;
}

export const Step2HullFamilies: React.FC<Step2Props> = ({
  formData,
  updateFormData,
  onNext,
  onPrevious,
  taxonomyEntry,
  metadataLoading,
  metadataError,
}) => {
  // Debug: Log taxonomy entry
  React.useEffect(() => {
    if (taxonomyEntry) {
      console.log("[Step2HullFamilies] Taxonomy entry:", {
        type: taxonomyEntry.type,
        bowFamilies: taxonomyEntry.bowFamilies,
        midshipFamilies: taxonomyEntry.midshipFamilies,
        sternFamilies: taxonomyEntry.sternFamilies,
      });
    } else {
      console.warn("[Step2HullFamilies] No taxonomy entry found for:", {
        category: formData.missionCategory,
        type: formData.missionType,
      });
    }
  }, [taxonomyEntry, formData.missionCategory, formData.missionType]);

  const bowOptions = taxonomyEntry?.bowFamilies ?? [];
  const midshipOptions = taxonomyEntry?.midshipFamilies ?? [];
  const sternOptions = taxonomyEntry?.sternFamilies ?? [];
  const bowKey = bowOptions.join("|");
  const midshipKey = midshipOptions.join("|");
  const sternKey = sternOptions.join("|");

  // Auto-select defaults when taxonomy provides families
  useEffect(() => {
    if (!taxonomyEntry) {
      return;
    }

    const next: Partial<CreateMissionCaseDto> = {};
    if (!formData.bowFamily && bowOptions.length > 0) {
      next.bowFamily = bowOptions[0];
    }
    if (!formData.midshipFamily && midshipOptions.length > 0) {
      next.midshipFamily = midshipOptions[0];
    }
    if (!formData.sternFamily && sternOptions.length > 0) {
      next.sternFamily = sternOptions[0];
    }
    if (!formData.familyMaskVersion) {
      next.familyMaskVersion = taxonomyEntry.maskVersion;
    }

    if (Object.keys(next).length > 0) {
      updateFormData(next);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [
    taxonomyEntry?.id,
    bowKey,
    midshipKey,
    sternKey,
    formData.bowFamily,
    formData.midshipFamily,
    formData.sternFamily,
    formData.familyMaskVersion,
  ]);

  const handleFamilyChange = (
    key: "bowFamily" | "midshipFamily" | "sternFamily",
    value: string
  ) => {
    updateFormData({
      [key]: value || undefined,
    });
  };

  const handleVectorChange = (value: string) => {
    updateFormData({
      shipdInputVectorJson: value || undefined,
      shipdInputsJson: value || undefined,
    });
  };

  const isValid =
    !!formData.bowFamily &&
    !!formData.midshipFamily &&
    !!formData.sternFamily;

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white">Hull Families</h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          Select bow, midship, and stern families to tailor the ShipD parameterization.
        </p>
        {metadataLoading && (
          <p className="mt-2 text-xs text-blue-600 dark:text-blue-300">Loading taxonomy…</p>
        )}
        {metadataError && (
          <p className="mt-2 text-xs text-yellow-700 dark:text-yellow-200">
            {metadataError}. You can proceed with manual entries.
          </p>
        )}
        {taxonomyEntry && (
          <p className="mt-2 text-xs text-gray-500 dark:text-gray-400">
            Selected vessel type: <strong>{taxonomyEntry.displayName}</strong>
          </p>
        )}
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-3">
        <div className="space-y-2">
          <Label htmlFor="bowFamily">Bow / Forward Family *</Label>
          {bowOptions.length > 0 ? (
            <Select
              id="bowFamily"
              value={formData.bowFamily || bowOptions[0] || ""}
              onChange={(value) => handleFamilyChange("bowFamily", value)}
              options={bowOptions.map((family) => ({ value: family, label: family }))}
            />
          ) : (
            <Input
              id="bowFamily"
              placeholder="e.g., bulbous_bow"
              value={formData.bowFamily || ""}
              onChange={(e) => handleFamilyChange("bowFamily", e.target.value)}
            />
          )}
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Choose the bow archetype (e.g., bulbous, fine entry, axe bow).
          </p>
        </div>

        <div className="space-y-2">
          <Label htmlFor="midshipFamily">Midship Family *</Label>
          {midshipOptions.length > 0 ? (
            <Select
              id="midshipFamily"
              value={formData.midshipFamily || midshipOptions[0] || ""}
              onChange={(value) => handleFamilyChange("midshipFamily", value)}
              options={midshipOptions.map((family) => ({ value: family, label: family }))}
            />
          ) : (
            <Input
              id="midshipFamily"
              placeholder="e.g., fine_midship"
              value={formData.midshipFamily || ""}
              onChange={(e) => handleFamilyChange("midshipFamily", e.target.value)}
            />
          )}
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Select the section shape (e.g., fine midship, barge type, deep V).
          </p>
        </div>

        <div className="space-y-2">
          <Label htmlFor="sternFamily">Stern / Aft Family *</Label>
          {sternOptions.length > 0 ? (
            <Select
              id="sternFamily"
              value={formData.sternFamily || sternOptions[0] || ""}
              onChange={(value) => handleFamilyChange("sternFamily", value)}
              options={sternOptions.map((family) => ({ value: family, label: family }))}
            />
          ) : (
            <Input
              id="sternFamily"
              placeholder="e.g., transom_stern"
              value={formData.sternFamily || ""}
              onChange={(e) => handleFamilyChange("sternFamily", e.target.value)}
            />
          )}
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Define stern geometry (e.g., transom, cruiser, canoe).
          </p>
        </div>
      </div>

      <CollapsibleSection title="Advanced Options" defaultExpanded={false}>
        <div className="space-y-2">
          <Label htmlFor="shipdInputVectorJson">
            Custom Hull Form Parameters Vector (optional)
          </Label>
          <textarea
            id="shipdInputVectorJson"
            rows={4}
            placeholder="Paste 45-value JSON array, e.g., [0.0, 0.12, ...]"
            value={formData.shipdInputVectorJson || ""}
            onChange={(e) => handleVectorChange(e.target.value)}
            className="font-mono text-xs w-full p-2 border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
          />
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Override the autogenerated ShipD parameter vector (advanced).
          </p>
        </div>
      </CollapsibleSection>

      <div className="flex justify-between pt-6 border-t border-gray-200 dark:border-gray-700">
        <Button variant="outline" onClick={onPrevious}>
          ← Previous
        </Button>
        <Button onClick={onNext} disabled={!isValid}>
          Next: Speed & Environment →
        </Button>
      </div>
    </div>
  );
};
