import React, { useEffect, useState } from "react";
import type { CreateMissionCaseDto, ShipDVesselTaxonomy } from "../../../types/sizing";
import { getVesselTypeDefaults } from "../../../services/sizingApi";
import { Button } from "../../ui/button";
import { Label } from "../../ui/label";
import { Select } from "../../ui/select";
import { Input } from "../../ui/input";
import { Info } from "lucide-react";

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

  const [defaultsApplied, setDefaultsApplied] = useState(false);
  const [defaultsLoading, setDefaultsLoading] = useState(false);

  const bowOptions = taxonomyEntry?.bowFamilies ?? [];
  const midshipOptions = taxonomyEntry?.midshipFamilies ?? [];
  const sternOptions = taxonomyEntry?.sternFamilies ?? [];
  const bowKey = bowOptions.join("|");
  const midshipKey = midshipOptions.join("|");
  const sternKey = sternOptions.join("|");

  // Auto-select defaults from vessel type mapping service (Phase 1)
  useEffect(() => {
    const category = formData.missionCategory;
    const type = formData.missionType;

    // Only fetch defaults if vessel type is selected and families are not already set
    if (!category || !type || defaultsApplied || defaultsLoading) {
      return;
    }

    // Check if families are already set (user may have manually selected)
    const hasFamilies = formData.bowFamily && formData.midshipFamily && formData.sternFamily;
    if (hasFamilies) {
      setDefaultsApplied(true);
      return;
    }

    // Fetch vessel type defaults
    setDefaultsLoading(true);
    getVesselTypeDefaults(category, type)
      .then((defaults) => {
        if (defaults) {
          const next: Partial<CreateMissionCaseDto> = {};

          // Apply bow family default (only if not set and available in taxonomy options)
          if (!formData.bowFamily && defaults.bowFamily) {
            if (bowOptions.length === 0 || bowOptions.includes(defaults.bowFamily)) {
              next.bowFamily = defaults.bowFamily;
            }
          }

          // Apply midship family default
          if (!formData.midshipFamily && defaults.midshipFamily) {
            if (midshipOptions.length === 0 || midshipOptions.includes(defaults.midshipFamily)) {
              next.midshipFamily = defaults.midshipFamily;
            }
          }

          // Apply stern family default
          if (!formData.sternFamily && defaults.sternFamily) {
            if (sternOptions.length === 0 || sternOptions.includes(defaults.sternFamily)) {
              next.sternFamily = defaults.sternFamily;
            }
          }

          if (Object.keys(next).length > 0) {
            updateFormData(next);
            setDefaultsApplied(true);
          }
        }
      })
      .catch((error) => {
        console.warn("[Step2HullFamilies] Failed to fetch vessel type defaults:", error);
        // Fall through to taxonomy-based defaults
      })
      .finally(() => {
        setDefaultsLoading(false);
      });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [formData.missionCategory, formData.missionType]);

  // Fallback: Auto-select defaults when taxonomy provides families (if vessel type defaults didn't apply)
  useEffect(() => {
    if (!taxonomyEntry || defaultsApplied) {
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
    defaultsApplied,
  ]);

  const handleFamilyChange = (
    key: "bowFamily" | "midshipFamily" | "sternFamily",
    value: string
  ) => {
    updateFormData({
      [key]: value || undefined,
    });
  };

  const isValid = !!formData.bowFamily && !!formData.midshipFamily && !!formData.sternFamily;

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-foreground">Hull Families</h2>
        <p className="mt-1 text-sm text-muted-foreground">
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
          <div className="mt-2 space-y-1">
            <p className="text-xs text-gray-500 dark:text-gray-400">
              Selected vessel type: <strong>{taxonomyEntry.displayName}</strong>
            </p>
            {defaultsApplied && (
              <div className="flex items-start gap-2 rounded-md bg-blue-50 dark:bg-blue-900/20 p-2 text-xs text-blue-800 dark:text-blue-200">
                <Info className="h-4 w-4 mt-0.5 flex-shrink-0" />
                <div>
                  <p className="font-medium">Hull families pre-selected</p>
                  <p className="mt-0.5">
                    Based on your vessel type selection, we've pre-selected appropriate hull
                    families. You can change these if needed.
                  </p>
                </div>
              </div>
            )}
          </div>
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
          <p className="text-xs text-muted-foreground">
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
          <p className="text-xs text-muted-foreground">
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
          <p className="text-xs text-muted-foreground">
            Define stern geometry (e.g., transom, cruiser, canoe).
          </p>
        </div>
      </div>

      <div className="flex justify-between pt-6 border-t border-border">
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
