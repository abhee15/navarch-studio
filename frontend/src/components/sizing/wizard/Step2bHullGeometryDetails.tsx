import React, { useMemo } from "react";
import type { CreateMissionCaseDto, ShipDAdditionalParameters } from "../../../types/sizing";
import { Button } from "../../ui/button";
import { Label } from "../../ui/label";
import { Input } from "../../ui/input";
import { Select } from "../../ui/select";
import { ChevronDown, ChevronUp, Info } from "lucide-react";

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
}

export const Step2bHullGeometryDetails: React.FC<Step2bProps> = ({
  formData,
  updateFormData,
  onNext,
  onPrevious,
  bowFamily,
  midshipFamily,
  sternFamily,
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
  const updateAdditionalParams = (updates: Partial<ShipDAdditionalParameters>) => {
    const current = additionalParams || {};
    const updated = { ...current, ...updates };
    const shipdInputs = {
      ...(formData.shipdInputsJson ? JSON.parse(formData.shipdInputsJson) : {}),
      additionalParameters: updated,
    };
    updateFormData({
      shipdInputsJson: JSON.stringify(shipdInputs),
    });
  };

  // Show bulb section only if bulbous_bow is selected
  const showBulbSection = bowFamily === "bulbous_bow";

  // Calculate mid-body length ratio (derived)
  const midBodyRatio = useMemo(() => {
    const lb = additionalParams?.bowLengthRatio ?? 0.316;
    const ls = additionalParams?.sternLengthRatio ?? 0.42;
    const lm = 1 - lb - ls;
    return Math.max(0, lm);
  }, [additionalParams?.bowLengthRatio, additionalParams?.sternLengthRatio]);

  const toggleSection = (section: keyof typeof expandedSections) => {
    setExpandedSections((prev) => ({ ...prev, [section]: !prev[section] }));
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
          Hull Geometry Details
        </h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
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
      <div className="rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
        <button
          type="button"
          onClick={() => toggleSection("sectionGeometry")}
          className="w-full flex items-center justify-between p-4 text-left hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
        >
          <div>
            <h3 className="font-semibold text-gray-900 dark:text-white">Section Geometry</h3>
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
              Flare, deadrise, chine type, and curvature controls
            </p>
          </div>
          {expandedSections.sectionGeometry ? (
            <ChevronUp className="h-5 w-5 text-gray-400" />
          ) : (
            <ChevronDown className="h-5 w-5 text-gray-400" />
          )}
        </button>

        {expandedSections.sectionGeometry && (
          <div className="px-4 pb-4 space-y-4 border-t border-gray-200 dark:border-gray-700 pt-4">
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
                <p className="text-xs text-gray-500 dark:text-gray-400">
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
                <p className="text-xs text-gray-500 dark:text-gray-400">
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
                <p className="text-xs text-gray-500 dark:text-gray-400">
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
                <p className="text-xs text-gray-500 dark:text-gray-400">
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
                    className="rounded border-gray-300 dark:border-gray-600"
                  />
                  <span>Enable Tumblehome</span>
                </Label>
                <p className="text-xs text-gray-500 dark:text-gray-400 ml-6">
                  Inward curving upper sides (only applicable for fine midship)
                </p>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Section 2: Longitudinal Proportions (Image 2) */}
      <div className="rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
        <button
          type="button"
          onClick={() => toggleSection("longitudinal")}
          className="w-full flex items-center justify-between p-4 text-left hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
        >
          <div>
            <h3 className="font-semibold text-gray-900 dark:text-white">
              Longitudinal Proportions
            </h3>
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
              Bow, mid-body, and stern length ratios (Lb, Lm, Ls)
            </p>
          </div>
          {expandedSections.longitudinal ? (
            <ChevronUp className="h-5 w-5 text-gray-400" />
          ) : (
            <ChevronDown className="h-5 w-5 text-gray-400" />
          )}
        </button>

        {expandedSections.longitudinal && (
          <div className="px-4 pb-4 space-y-4 border-t border-gray-200 dark:border-gray-700 pt-4">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              {/* Bow Length Ratio */}
              <div className="space-y-2">
                <Label htmlFor="bowLengthRatio">Bow Length Ratio (Lb)</Label>
                <Input
                  id="bowLengthRatio"
                  type="number"
                  min={0.05}
                  max={0.9}
                  step={0.01}
                  value={additionalParams?.bowLengthRatio?.toString() || ""}
                  onChange={(e) => {
                    const value = e.target.value ? parseFloat(e.target.value) : undefined;
                    updateAdditionalParams({ bowLengthRatio: value });
                  }}
                  placeholder="0.05-0.90"
                />
                <p className="text-xs text-gray-500 dark:text-gray-400">Relative to LOA</p>
              </div>

              {/* Mid-Body Length Ratio (calculated) */}
              <div className="space-y-2">
                <Label htmlFor="midBodyRatio">Mid-Body Length Ratio (Lm)</Label>
                <Input
                  id="midBodyRatio"
                  type="number"
                  value={midBodyRatio.toFixed(3)}
                  disabled
                  className="bg-gray-100 dark:bg-gray-700 cursor-not-allowed"
                />
                <p className="text-xs text-gray-500 dark:text-gray-400">
                  Calculated: 1 - Lb - Ls (read-only)
                </p>
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
                  value={additionalParams?.sternLengthRatio?.toString() || ""}
                  onChange={(e) => {
                    const value = e.target.value ? parseFloat(e.target.value) : undefined;
                    updateAdditionalParams({ sternLengthRatio: value });
                  }}
                  placeholder="0.05-0.90"
                />
                <p className="text-xs text-gray-500 dark:text-gray-400">Relative to LOA</p>
              </div>
            </div>

            {/* Validation warning */}
            {midBodyRatio <= 0 && (
              <div className="rounded-md bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 p-3 text-sm text-yellow-800 dark:text-yellow-200">
                <p className="font-medium">Warning: Lb + Ls must be less than 1.0</p>
                <p className="mt-1">
                  Please adjust bow or stern length ratios to ensure mid-body length is positive.
                </p>
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
                <p className="text-xs text-gray-500 dark:text-gray-400">
                  Forward section rake angle
                </p>
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
                <p className="text-xs text-gray-500 dark:text-gray-400">
                  Aft section rake angle (Beta_trans)
                </p>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Section 3: Bulb Geometry (Image 3) - only if bulbous_bow selected */}
      {showBulbSection && (
        <div className="rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800">
          <button
            type="button"
            onClick={() => toggleSection("bulb")}
            className="w-full flex items-center justify-between p-4 text-left hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
          >
            <div>
              <h3 className="font-semibold text-gray-900 dark:text-white">Bulb Geometry</h3>
              <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
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
            <div className="px-4 pb-4 space-y-4 border-t border-gray-200 dark:border-gray-700 pt-4">
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
                  <p className="text-xs text-gray-500 dark:text-gray-400">Relative to LOA</p>
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
                  <p className="text-xs text-gray-500 dark:text-gray-400">Relative to beam</p>
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
                  <p className="text-xs text-gray-500 dark:text-gray-400">Relative to draft</p>
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
                  <p className="text-xs text-gray-500 dark:text-gray-400">
                    Longitudinal moment coefficient
                  </p>
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
                  <p className="text-xs text-gray-500 dark:text-gray-400">Radius coefficient</p>
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
