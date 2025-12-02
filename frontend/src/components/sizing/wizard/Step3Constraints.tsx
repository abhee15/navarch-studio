import React, { useState, useCallback } from "react";
import type { CreateMissionCaseDto } from "../../../types/sizing";
import { Button } from "../../ui/button";
import { Input } from "../../ui/input";
import { Label } from "../../ui/label";
import { FormField } from "../../ui/form-field";
import { AlertTriangle, Info } from "lucide-react";
import {
  validateField,
  isStep3Valid,
  type ValidationErrors,
  type TouchedFields,
} from "../../../utils/wizardValidation";
import { isConstraintApplicable, getConstraintTooltip } from "../../../utils/vesselConstraintRules";

interface Step3Props {
  formData: Partial<CreateMissionCaseDto>;
  updateFormData: (data: Partial<CreateMissionCaseDto>) => void;
  onNext: () => void;
  onPrevious: () => void;
  onSubmit: () => void;
  isFirstStep: boolean;
  isLastStep: boolean;
}

const CANAL_PRESETS = [
  { key: "none", name: "None", loa: null, beam: null, draft: null, airdraft: null },
  { key: "panamax", name: "Panamax", loa: 294.1, beam: 32.3, draft: 12.0, airdraft: 57.91 },
  { key: "neopanamax", name: "Neo-Panamax", loa: 366.0, beam: 49.0, draft: 15.2, airdraft: 57.91 },
  { key: "suezmax", name: "Suezmax", loa: null, beam: 50.0, draft: 20.1, airdraft: 68.0 },
  { key: "malaccamax", name: "Malaccamax", loa: 400.0, beam: 59.0, draft: 20.5, airdraft: null },
];

export const Step3Constraints: React.FC<Step3Props> = ({
  formData,
  updateFormData,
  onNext,
  onPrevious,
}) => {
  // Validation state
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [touched, setTouched] = useState<TouchedFields>({});

  // Check if form is valid for Next button
  const isValid = isStep3Valid(formData);

  // Handle field change with validation
  const handleFieldChange = useCallback(
    (fieldName: string, value: string | number | undefined) => {
      // Update form data
      updateFormData({ [fieldName]: value });

      // Mark field as touched
      setTouched((prev) => ({ ...prev, [fieldName]: true }));

      // Validate field
      const error = validateField(fieldName, value, { ...formData, [fieldName]: value });
      setErrors((prev) => ({
        ...prev,
        [fieldName]: error,
      }));
    },
    [formData, updateFormData]
  );

  // Handle field blur (mark as touched)
  const handleFieldBlur = useCallback((fieldName: string) => {
    setTouched((prev) => ({ ...prev, [fieldName]: true }));
  }, []);

  // Count active errors
  const activeErrors = Object.values(errors).filter(
    (error) => error !== null && error !== undefined
  );
  const hasErrors = activeErrors.length > 0;

  const applyPreset = (preset: (typeof CANAL_PRESETS)[0]) => {
    updateFormData({
      capLoaM: preset.loa || undefined,
      capBeamM: preset.beam || undefined,
      capDraftM: preset.draft || undefined,
      capAirdraftM: preset.airdraft || undefined,
    });

    // Clear touched state and errors when applying preset
    setTouched({});
    setErrors({});
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-foreground">Physical Constraints</h2>
        <p className="mt-1 text-sm text-muted-foreground">
          Define maximum dimensions (optional - leave blank for unconstrained)
        </p>
      </div>

      {/* Canal Presets */}
      <div className="space-y-2">
        <Label>Canal/Lock Presets</Label>
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
          {CANAL_PRESETS.map((preset) => {
            const applicability = isConstraintApplicable(
              preset.key,
              formData.missionCategory,
              formData.missionType
            );
            const isApplicable = preset.key === "none" || applicability.applicable;
            const tooltip = getConstraintTooltip(preset.key, isApplicable, applicability.reason);

            return (
              <div key={preset.name} className="relative group">
                <button
                  type="button"
                  onClick={() => applyPreset(preset)}
                  disabled={!isApplicable}
                  className={`w-full rounded-md border px-3 py-2 text-sm transition-colors ${
                    isApplicable
                      ? "border-input hover:bg-accent/10 cursor-pointer"
                      : "border-muted bg-muted/30 text-muted-foreground cursor-not-allowed opacity-60"
                  }`}
                  title={tooltip}
                >
                  {preset.name}
                  {!isApplicable && <Info className="inline-block ml-1 h-3 w-3" />}
                </button>
                {!isApplicable && tooltip && (
                  <div className="hidden group-hover:block absolute z-10 w-64 p-2 mt-1 text-xs bg-popover border border-border rounded-md shadow-lg">
                    <p className="text-muted-foreground whitespace-pre-line">{tooltip}</p>
                  </div>
                )}
              </div>
            );
          })}
        </div>
        <p className="text-xs text-muted-foreground">
          Quick apply standard canal/lock constraints
          {formData.missionType && (
            <span className="ml-2 text-blue-600 dark:text-blue-400">
              • Filtered for {formData.missionType}
            </span>
          )}
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        {/* Max LOA */}
        <div className="space-y-2">
          <Label htmlFor="capLoaM">Max Length Overall (m)</Label>
          <Input
            id="capLoaM"
            type="number"
            step="1"
            placeholder="e.g., 294"
            value={formData.capLoaM || ""}
            onChange={(e) => updateFormData({ capLoaM: parseFloat(e.target.value) || undefined })}
          />
        </div>

        {/* Max Beam */}
        <FormField
          label="Max Beam (m)"
          htmlFor="capBeamM"
          error={errors.capBeamM}
          touched={touched.capBeamM}
          helpText="Maximum beam constraint (optional)"
        >
          <Input
            id="capBeamM"
            type="number"
            step="0.1"
            placeholder="e.g., 32.3"
            value={formData.capBeamM || ""}
            onChange={(e) => handleFieldChange("capBeamM", parseFloat(e.target.value) || undefined)}
            onBlur={() => handleFieldBlur("capBeamM")}
          />
        </FormField>

        {/* Max Draft */}
        <FormField
          label="Max Draft (m)"
          htmlFor="capDraftM"
          error={errors.capDraftM}
          touched={touched.capDraftM}
          helpText="Maximum draft constraint (optional)"
        >
          <Input
            id="capDraftM"
            type="number"
            step="0.1"
            placeholder="e.g., 12.0"
            value={formData.capDraftM || ""}
            onChange={(e) =>
              handleFieldChange("capDraftM", parseFloat(e.target.value) || undefined)
            }
            onBlur={() => handleFieldBlur("capDraftM")}
          />
        </FormField>

        {/* Max Airdraft */}
        <div className="space-y-2">
          <Label htmlFor="capAirdraftM">Max Air Draft (m)</Label>
          <Input
            id="capAirdraftM"
            type="number"
            step="0.1"
            placeholder="e.g., 57.9"
            value={formData.capAirdraftM || ""}
            onChange={(e) =>
              updateFormData({ capAirdraftM: parseFloat(e.target.value) || undefined })
            }
          />
          <p className="text-xs text-muted-foreground">Height from waterline to highest point</p>
        </div>
      </div>

      {/* Error Summary Panel */}
      {hasErrors && (
        <div className="rounded-md border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20 p-4 animate-in fade-in slide-in-from-top-2 duration-300">
          <div className="flex items-start gap-3">
            <AlertTriangle className="h-5 w-5 text-red-600 dark:text-red-400 flex-shrink-0 mt-0.5" />
            <div className="flex-1">
              <h3 className="text-sm font-semibold text-red-800 dark:text-red-200">
                Please fix the following {activeErrors.length === 1 ? "error" : "errors"}:
              </h3>
              <ul className="mt-2 text-sm text-red-700 dark:text-red-300 list-disc list-inside space-y-1">
                {Object.entries(errors).map(
                  ([field, error]) =>
                    error && (
                      <li key={field} className="leading-relaxed">
                        {error}
                      </li>
                    )
                )}
              </ul>
            </div>
          </div>
        </div>
      )}

      {/* Navigation */}
      <div className="flex justify-between pt-6 border-t border-border">
        <Button variant="outline" onClick={onPrevious}>
          ← Previous
        </Button>
        <Button onClick={onNext} disabled={!isValid || hasErrors}>
          Next: Options & Review →
        </Button>
      </div>
    </div>
  );
};
