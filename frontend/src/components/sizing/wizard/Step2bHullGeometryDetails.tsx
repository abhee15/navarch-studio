import React, { useMemo, useEffect } from "react";
import type {
  CreateMissionCaseDto,
  ShipDAdditionalParameters,
  ShipDVesselTaxonomy,
} from "../../../types/sizing";
import { Button } from "../../ui/button";
import { Label } from "../../ui/label";
import { Input } from "../../ui/input";
import { Select } from "../../ui/select";
import { ChevronDown, ChevronUp, Info, AlertTriangle } from "lucide-react";

interface Step2bProps {
  formData: Partial<CreateMissionCaseDto>;
  updateFormData: (data: Partial<CreateMissionCaseDto>) => void;
  onNext: () => void;
  onPrevious: () => void;
  onSubmit: () => void;
  isFirstStep: boolean;
  isLastStep: boolean;
  bowFamily?: string;
  midshipFamily?: string;
  sternFamily?: string;
  taxonomyEntry?: ShipDVesselTaxonomy;
}

export const Step2bHullGeometryDetails: React.FC<Step2bProps> = ({
  formData,
  updateFormData,
  onNext,
  onPrevious,
  bowFamily,
  midshipFamily,
  sternFamily,
  taxonomyEntry,
}) => {
  const [expandedSections, setExpandedSections] = React.useState({
    sectionGeometry: true,
    longitudinal: true,
    bulb: false,
  });

  // Parse additionalParameters from formData
  const additionalParams = useMemo(() => {
    try {
      if (formData.shipdInputsJson) {
        const parsed = JSON.parse(formData.shipdInputsJson);
        return parsed.additionalParameters as ShipDAdditionalParameters | undefined;
      }
    } catch {
      // Ignore parse errors
    }
    return undefined;
  }, [formData.shipdInputsJson]);

  // Update additionalParameters
  const updateAdditionalParams = React.useCallback(
    (updates: Partial<ShipDAdditionalParameters>) => {
      const current = additionalParams || {};
      const updated = { ...current, ...updates };
      const shipdInputs = {
        ...(formData.shipdInputsJson ? JSON.parse(formData.shipdInputsJson) : {}),
        additionalParameters: updated,
      };
      updateFormData({
        shipdInputsJson: JSON.stringify(shipdInputs),
      });
    },
    [additionalParams, formData.shipdInputsJson, updateFormData]
  );

  // Extract defaults from taxonomy entry
  const taxonomyDefaults = useMemo(() => {
    if (taxonomyEntry?.additionalParametersJson) {
      try {
        const parsed = JSON.parse(taxonomyEntry.additionalParametersJson);
        return {
          bow: parsed.bowLengthRatio as number | undefined,
          stern: parsed.sternLengthRatio as number | undefined,
        };
      } catch {
        // Ignore parse errors
      }
    }
    return { bow: 0.3, stern: 0.3 }; // Fallback defaults
  }, [taxonomyEntry]);

  // Set defaults when component mounts or vessel type changes (if values not already set)
  useEffect(() => {
    if (taxonomyDefaults.bow !== undefined && taxonomyDefaults.stern !== undefined) {
      const currentBow = additionalParams?.bowLengthRatio;
      const currentStern = additionalParams?.sternLengthRatio;

      // Only set defaults if values are not already set
      if (currentBow === undefined && currentStern === undefined) {
        updateAdditionalParams({
          bowLengthRatio: taxonomyDefaults.bow,
          sternLengthRatio: taxonomyDefaults.stern,
        });
      }
    }
  }, [
    taxonomyDefaults.bow,
    taxonomyDefaults.stern,
    additionalParams?.bowLengthRatio,
    additionalParams?.sternLengthRatio,
    updateAdditionalParams,
  ]); // Only run when defaults change

  // Show bulb section only if bulbous_bow is selected
  const showBulbSection = bowFamily === "bulbous_bow";

  // Calculate mid-body length ratio (derived)
  const midBodyRatio = useMemo(() => {
    const lb = additionalParams?.bowLengthRatio ?? taxonomyDefaults.bow ?? 0.3;
    const ls = additionalParams?.sternLengthRatio ?? taxonomyDefaults.stern ?? 0.3;
    const lm = 1 - lb - ls;
    return Math.max(0, lm);
  }, [additionalParams?.bowLengthRatio, additionalParams?.sternLengthRatio, taxonomyDefaults]);

  // Validation: check if bow + stern >= 1.0
  const isValid = useMemo(() => {
    const lb = additionalParams?.bowLengthRatio ?? taxonomyDefaults.bow ?? 0.3;
    const ls = additionalParams?.sternLengthRatio ?? taxonomyDefaults.stern ?? 0.3;
    return lb + ls < 1.0;
  }, [additionalParams?.bowLengthRatio, additionalParams?.sternLengthRatio, taxonomyDefaults]);

  const toggleSection = (section: keyof typeof expandedSections) => {
    setExpandedSections((prev) => ({ ...prev, [section]: !prev[section] }));
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-foreground">Hull Geometry Details</h2>
        <p className="mt-1 text-sm text-muted-foreground">
          Fine-tune hull geometry parameters based on your selected families. These parameters
          provide granular control over section shapes, longitudinal proportions, and appendages.
        </p>
        <div className="mt-3 flex items-start gap-2 rounded-md bg-blue-50 dark:bg-blue-900/20 p-3 text-sm text-blue-800 dark:text-blue-200">
          <Info className="h-4 w-4 mt-0.5 flex-shrink-0" />
          <div>
            <p className="font-medium">Selected Families:</p>
            <p className="mt-1">
              Bow: <strong>{bowFamily || "—"}</strong> | Mid:{" "}
              <strong>{midshipFamily || "—"}</strong> | Stern: <strong>{sternFamily || "—"}</strong>
            </p>
          </div>
        </div>
      </div>

      {/* Section 1: Section Geometry (Image 1) */}
      <div className="rounded-lg border border-border bg-card">
        <button
          type="button"
          onClick={() => toggleSection("sectionGeometry")}
          className="w-full flex items-center justify-between p-4 text-left hover:bg-accent/10 transition-colors"
        >
          <div>
            <h3 className="font-semibold text-foreground">Section Geometry</h3>
            <p className="text-xs text-muted-foreground mt-1">
              Flare, deadrise, chine type, and curvature controls
            </p>
          </div>
          {expandedSections.sectionGeometry ? (
            <ChevronUp className="h-5 w-5 text-muted-foreground" />
          ) : (
            <ChevronDown className="h-5 w-5 text-muted-foreground" />
          )}
        </button>

        {expandedSections.sectionGeometry && (
          <div className="px-4 pb-4 space-y-4 border-t border-border pt-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {/* Flare Angle */}
              <div className="space-y-2">
                <Label htmlFor="flareAngle">Flare Angle (degrees)</Label>
                <Input
                  id="flareAngle"
                  type="number"
                  min={0}
                  max={45}
                  step={0.5}
                  value={additionalParams?.flareAngleDeg?.toString() || ""}
                  onChange={(e) =>
                    updateAdditionalParams({
                      flareAngleDeg: e.target.value ? parseFloat(e.target.value) : undefined,
                    })
                  }
                  placeholder="0-45"
                />
                <p className="text-xs text-muted-foreground">
                  Outward angle of upper sides (Beta parameter)
                </p>
              </div>

              {/* Deadrise Angle */}
              <div className="space-y-2">
                <Label htmlFor="deadriseAngle">Deadrise Angle (degrees)</Label>
                <Input
                  id="deadriseAngle"
                  type="number"
                  min={0}
                  max={60}
                  step={0.5}
                  value={additionalParams?.deadriseAngleDeg?.toString() || ""}
                  onChange={(e) =>
                    updateAdditionalParams({
                      deadriseAngleDeg: e.target.value ? parseFloat(e.target.value) : undefined,
                    })
                  }
                  placeholder="0-60"
                />
                <p className="text-xs text-muted-foreground">
                  Angle of lower sides relative to horizontal (Cdrft parameter)
                </p>
              </div>

              {/* Chine Type */}
              <div className="space-y-2">
                <Label htmlFor="chineType">Chine Type</Label>
                <Select
                  id="chineType"
                  value={additionalParams?.chineType || ""}
                  onChange={(value) =>
                    updateAdditionalParams({
                      chineType: value === "hard" || value === "soft" ? value : undefined,
                    })
                  }
                  options={[
                    { value: "", label: "Default" },
                    { value: "hard", label: "Hard Chine (Sharp Corner)" },
                    { value: "soft", label: "Soft Chine (Rounded Transition)" },
                  ]}
                  placeholder="Select chine type"
                />
                <p className="text-xs text-muted-foreground">
                  Affects curvature coefficients Rc and Rk
                </p>
              </div>

              {/* Curvature Type */}
              <div className="space-y-2">
                <Label htmlFor="curvatureType">Curvature Type</Label>
                <Select
                  id="curvatureType"
                  value={additionalParams?.curvatureType || ""}
                  onChange={(value) =>
                    updateAdditionalParams({
                      curvatureType: value === "convex" || value === "concave" ? value : undefined,
                    })
                  }
                  options={[
                    { value: "", label: "Default" },
                    { value: "convex", label: "Convex (Rounded Bottom)" },
                    { value: "concave", label: "Concave (Inward Curving)" },
                  ]}
                  placeholder="Select curvature type"
                />
                <p className="text-xs text-muted-foreground">
                  Affects Kappa_bow and Kappa_stern parameters
                </p>
              </div>
            </div>

            {/* Tumblehome - only for fine_midship */}
            {midshipFamily === "fine_midship" && (
              <div className="space-y-2">
                <Label htmlFor="tumblehome" className="flex items-center gap-2">
                  <input
                    id="tumblehome"
                    type="checkbox"
                    checked={additionalParams?.tumblehomeEnabled || false}
                    onChange={(e) =>
                      updateAdditionalParams({ tumblehomeEnabled: e.target.checked || undefined })
                    }
                    className="rounded border-input"
                  />
                  <span>Enable Tumblehome</span>
                </Label>
                <p className="text-xs text-muted-foreground ml-6">
                  Inward curving upper sides (only applicable for fine midship)
                </p>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Section 2: Longitudinal Proportions (Image 2) */}
      <div className="rounded-lg border border-border bg-card">
        <button
          type="button"
          onClick={() => toggleSection("longitudinal")}
          className="w-full flex items-center justify-between p-4 text-left hover:bg-accent/10 transition-colors"
        >
          <div>
            <h3 className="font-semibold text-foreground">Longitudinal Proportions</h3>
            <p className="text-xs text-muted-foreground mt-1">
              Bow, mid-body, and stern length ratios (Lb, Lm, Ls)
            </p>
          </div>
          {expandedSections.longitudinal ? (
            <ChevronUp className="h-5 w-5 text-muted-foreground" />
          ) : (
            <ChevronDown className="h-5 w-5 text-muted-foreground" />
          )}
        </button>

        {expandedSections.longitudinal && (
          <div className="px-4 pb-4 space-y-4 border-t border-border pt-4">
            {/* Visual representation of ratios */}
            <div className="rounded-lg bg-muted/50 p-4">
              <div className="flex items-center justify-between mb-2">
                <Label className="text-sm font-medium">Length Distribution</Label>
                <span className="text-xs text-muted-foreground">
                  Total:{" "}
                  {(
                    midBodyRatio +
                    (additionalParams?.bowLengthRatio ?? taxonomyDefaults.bow ?? 0.3) +
                    (additionalParams?.sternLengthRatio ?? taxonomyDefaults.stern ?? 0.3)
                  ).toFixed(3)}
                </span>
              </div>
              <div className="flex h-6 rounded-md overflow-hidden border border-border">
                <div
                  className="bg-blue-500 flex items-center justify-center text-xs font-medium text-white"
                  style={{
                    width: `${((additionalParams?.bowLengthRatio ?? taxonomyDefaults.bow ?? 0.3) * 100).toFixed(1)}%`,
                  }}
                  title={`Bow: ${((additionalParams?.bowLengthRatio ?? taxonomyDefaults.bow ?? 0.3) * 100).toFixed(1)}%`}
                >
                  {(
                    (additionalParams?.bowLengthRatio ?? taxonomyDefaults.bow ?? 0.3) * 100
                  ).toFixed(0)}
                  %
                </div>
                <div
                  className="bg-green-500 flex items-center justify-center text-xs font-medium text-white"
                  style={{
                    width: `${(midBodyRatio * 100).toFixed(1)}%`,
                  }}
                  title={`Midship: ${(midBodyRatio * 100).toFixed(1)}%`}
                >
                  {midBodyRatio > 0.05 ? `${(midBodyRatio * 100).toFixed(0)}%` : ""}
                </div>
                <div
                  className="bg-purple-500 flex items-center justify-center text-xs font-medium text-white"
                  style={{
                    width: `${((additionalParams?.sternLengthRatio ?? taxonomyDefaults.stern ?? 0.3) * 100).toFixed(1)}%`,
                  }}
                  title={`Stern: ${((additionalParams?.sternLengthRatio ?? taxonomyDefaults.stern ?? 0.3) * 100).toFixed(1)}%`}
                >
                  {(
                    (additionalParams?.sternLengthRatio ?? taxonomyDefaults.stern ?? 0.3) * 100
                  ).toFixed(0)}
                  %
                </div>
              </div>
              <div className="flex justify-between mt-2 text-xs text-muted-foreground">
                <span>Bow</span>
                <span>Midship</span>
                <span>Stern</span>
              </div>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              {/* Bow Length Ratio */}
              <div className="space-y-2">
                <Label htmlFor="bowLengthRatio">Bow Length Ratio (Lb)</Label>
                <Input
                  id="bowLengthRatio"
                  type="number"
                  min={0.05}
                  max={0.9}
                  step={0.01}
                  value={
                    additionalParams?.bowLengthRatio?.toString() ||
                    taxonomyDefaults.bow?.toString() ||
                    ""
                  }
                  onChange={(e) => {
                    const value = e.target.value ? parseFloat(e.target.value) : undefined;
                    const currentStern =
                      additionalParams?.sternLengthRatio ?? taxonomyDefaults.stern ?? 0.3;
                    // Auto-constrain: prevent bow + stern >= 1.0
                    const maxBow = Math.min(0.9, 0.99 - currentStern);
                    const constrainedValue =
                      value !== undefined ? Math.min(value, maxBow) : undefined;
                    updateAdditionalParams({ bowLengthRatio: constrainedValue });
                  }}
                  placeholder={taxonomyDefaults.bow?.toFixed(2) || "0.05-0.90"}
                  className={!isValid ? "border-red-500 dark:border-red-500" : ""}
                />
                <p className="text-xs text-muted-foreground">Relative to LOA</p>
              </div>

              {/* Mid-Body Length Ratio (calculated) */}
              <div className="space-y-2">
                <Label htmlFor="midBodyRatio">Mid-Body Length Ratio (Lm)</Label>
                <Input
                  id="midBodyRatio"
                  type="number"
                  value={midBodyRatio.toFixed(3)}
                  disabled
                  className="bg-muted cursor-not-allowed"
                />
                <p className="text-xs text-muted-foreground">Calculated: 1 - Lb - Ls (read-only)</p>
              </div>

              {/* Stern Length Ratio */}
              <div className="space-y-2">
                <Label htmlFor="sternLengthRatio">Stern Length Ratio (Ls)</Label>
                <Input
                  id="sternLengthRatio"
                  type="number"
                  min={0.05}
                  max={0.9}
                  step={0.01}
                  value={
                    additionalParams?.sternLengthRatio?.toString() ||
                    taxonomyDefaults.stern?.toString() ||
                    ""
                  }
                  onChange={(e) => {
                    const value = e.target.value ? parseFloat(e.target.value) : undefined;
                    const currentBow =
                      additionalParams?.bowLengthRatio ?? taxonomyDefaults.bow ?? 0.3;
                    // Auto-constrain: prevent bow + stern >= 1.0
                    const maxStern = Math.min(0.9, 0.99 - currentBow);
                    const constrainedValue =
                      value !== undefined ? Math.min(value, maxStern) : undefined;
                    updateAdditionalParams({ sternLengthRatio: constrainedValue });
                  }}
                  placeholder={taxonomyDefaults.stern?.toFixed(2) || "0.05-0.90"}
                  className={!isValid ? "border-red-500 dark:border-red-500" : ""}
                />
                <p className="text-xs text-muted-foreground">Relative to LOA</p>
              </div>
            </div>

            {/* Validation warning */}
            {!isValid && (
              <div className="rounded-md bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 p-3 text-sm text-red-800 dark:text-red-200 flex items-start gap-2">
                <AlertTriangle className="h-5 w-5 flex-shrink-0 mt-0.5" />
                <div>
                  <p className="font-medium">Invalid: Lb + Ls must be less than 1.0</p>
                  <p className="mt-1">
                    Please adjust bow or stern length ratios to ensure mid-body length is positive.
                  </p>
                </div>
              </div>
            )}

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {/* Bow Rake Angle */}
              <div className="space-y-2">
                <Label htmlFor="bowRakeAngle">Bow Rake Angle (degrees)</Label>
                <Input
                  id="bowRakeAngle"
                  type="number"
                  min={0}
                  max={45}
                  step={0.5}
                  value={additionalParams?.bowRakeAngleDeg?.toString() || ""}
                  onChange={(e) =>
                    updateAdditionalParams({
                      bowRakeAngleDeg: e.target.value ? parseFloat(e.target.value) : undefined,
                    })
                  }
                  placeholder="0-45"
                />
                <p className="text-xs text-muted-foreground">Forward section rake angle</p>
              </div>

              {/* Stern Rake Angle */}
              <div className="space-y-2">
                <Label htmlFor="sternRakeAngle">Stern Rake Angle (degrees)</Label>
                <Input
                  id="sternRakeAngle"
                  type="number"
                  min={0}
                  max={60}
                  step={0.5}
                  value={additionalParams?.sternRakeAngleDeg?.toString() || ""}
                  onChange={(e) =>
                    updateAdditionalParams({
                      sternRakeAngleDeg: e.target.value ? parseFloat(e.target.value) : undefined,
                    })
                  }
                  placeholder="0-60"
                />
                <p className="text-xs text-muted-foreground">Aft section rake angle (Beta_trans)</p>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Section 3: Bulb Geometry (Image 3) - only if bulbous_bow selected */}
      {showBulbSection && (
        <div className="rounded-lg border border-border bg-card">
          <button
            type="button"
            onClick={() => toggleSection("bulb")}
            className="w-full flex items-center justify-between p-4 text-left hover:bg-accent/10 transition-colors"
          >
            <div>
              <h3 className="font-semibold text-foreground">Bulb Geometry</h3>
              <p className="text-xs text-muted-foreground mt-1">
                Bulbous bow dimensions and shape parameters
              </p>
            </div>
            {expandedSections.bulb ? (
              <ChevronUp className="h-5 w-5 text-gray-400" />
            ) : (
              <ChevronDown className="h-5 w-5 text-gray-400" />
            )}
          </button>

          {expandedSections.bulb && (
            <div className="px-4 pb-4 space-y-4 border-t border-border pt-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* Bulb Length Ratio */}
                <div className="space-y-2">
                  <Label htmlFor="bulbLengthRatio">Bulb Length Ratio (Lbb)</Label>
                  <Input
                    id="bulbLengthRatio"
                    type="number"
                    min={0}
                    max={0.2}
                    step={0.01}
                    value={additionalParams?.bulbLengthRatio?.toString() || ""}
                    onChange={(e) =>
                      updateAdditionalParams({
                        bulbLengthRatio: e.target.value ? parseFloat(e.target.value) : undefined,
                      })
                    }
                    placeholder="0.0-0.2"
                  />
                  <p className="text-xs text-muted-foreground">Relative to LOA</p>
                </div>

                {/* Bulb Width Ratio */}
                <div className="space-y-2">
                  <Label htmlFor="bulbWidthRatio">Bulb Width Ratio (Bbb)</Label>
                  <Input
                    id="bulbWidthRatio"
                    type="number"
                    min={0}
                    max={1.0}
                    step={0.01}
                    value={additionalParams?.bulbWidthRatio?.toString() || ""}
                    onChange={(e) =>
                      updateAdditionalParams({
                        bulbWidthRatio: e.target.value ? parseFloat(e.target.value) : undefined,
                      })
                    }
                    placeholder="0.0-1.0"
                  />
                  <p className="text-xs text-muted-foreground">Relative to beam</p>
                </div>

                {/* Bulb Height Ratio */}
                <div className="space-y-2">
                  <Label htmlFor="bulbHeightRatio">Bulb Height Ratio (Hbb)</Label>
                  <Input
                    id="bulbHeightRatio"
                    type="number"
                    min={0}
                    max={1.0}
                    step={0.01}
                    value={additionalParams?.bulbHeightRatio?.toString() || ""}
                    onChange={(e) =>
                      updateAdditionalParams({
                        bulbHeightRatio: e.target.value ? parseFloat(e.target.value) : undefined,
                      })
                    }
                    placeholder="0.0-1.0"
                  />
                  <p className="text-xs text-muted-foreground">Relative to draft</p>
                </div>

                {/* Bulb Asymmetry Factor */}
                <div className="space-y-2">
                  <Label htmlFor="bulbAsymmetryFactor">Bulb Asymmetry Factor (Lbbm)</Label>
                  <Input
                    id="bulbAsymmetryFactor"
                    type="number"
                    min={-1.0}
                    max={1.0}
                    step={0.01}
                    value={additionalParams?.bulbAsymmetryFactor?.toString() || ""}
                    onChange={(e) =>
                      updateAdditionalParams({
                        bulbAsymmetryFactor: e.target.value
                          ? parseFloat(e.target.value)
                          : undefined,
                      })
                    }
                    placeholder="-1.0 to 1.0"
                  />
                  <p className="text-xs text-muted-foreground">Longitudinal moment coefficient</p>
                </div>

                {/* Bulb Fillet Radius */}
                <div className="space-y-2">
                  <Label htmlFor="bulbFilletRadius">Bulb Fillet Radius (Rbb)</Label>
                  <Input
                    id="bulbFilletRadius"
                    type="number"
                    min={0.05}
                    max={0.33}
                    step={0.01}
                    value={additionalParams?.bulbFilletRadius?.toString() || ""}
                    onChange={(e) =>
                      updateAdditionalParams({
                        bulbFilletRadius: e.target.value ? parseFloat(e.target.value) : undefined,
                      })
                    }
                    placeholder="0.05-0.33"
                  />
                  <p className="text-xs text-muted-foreground">Radius coefficient</p>
                </div>
              </div>
            </div>
          )}
        </div>
      )}

      <div className="flex justify-between pt-6 border-t border-gray-200 dark:border-gray-700">
        <Button variant="outline" onClick={onPrevious}>
          ← Previous
        </Button>
        <Button onClick={onNext}>Next: Speed & Environment →</Button>
      </div>
    </div>
  );
};
