import React, { useState, useCallback } from "react";
import type { CreateMissionCaseDto } from "../../../types/sizing";
import { Button } from "../../ui/button";
import { Input } from "../../ui/input";
import { Label } from "../../ui/label";
import { FormField } from "../../ui/form-field";
import { AlertTriangle } from "lucide-react";
import {
  validateField,
  isStep2Valid,
  type ValidationErrors,
  type TouchedFields,
} from "../../../utils/wizardValidation";

interface Step2Props {
  formData: Partial<CreateMissionCaseDto>;
  updateFormData: (data: Partial<CreateMissionCaseDto>) => void;
  onNext: () => void;
  onPrevious: () => void;
  onSubmit: () => void;
  isFirstStep: boolean;
  isLastStep: boolean;
}

export const Step2SpeedEnvironment: React.FC<Step2Props> = ({
  formData,
  updateFormData,
  onNext,
  onPrevious,
}) => {
  // Validation state
  const [errors, setErrors] = useState<ValidationErrors>({});
  const [touched, setTouched] = useState<TouchedFields>({});

  // Check if form is valid for Next button
  const isValid = isStep2Valid(formData);

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

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-foreground">Speed & Environment</h2>
        <p className="mt-1 text-sm text-muted-foreground">
          Define operational speed and environmental conditions
        </p>
      </div>

      {/* Service Speed */}
      <FormField
        label="Service Speed (knots)"
        htmlFor="serviceSpeedKn"
        required
        error={errors.serviceSpeedKn}
        touched={touched.serviceSpeedKn}
        helpText="Typical: Tanker 14-16kn, Bulk carrier 14-15kn, Container 20-25kn, Fast ferry 30-40kn"
      >
        <Input
          id="serviceSpeedKn"
          type="number"
          step="0.5"
          placeholder="e.g., 22"
          value={formData.serviceSpeedKn || ""}
          onChange={(e) => handleFieldChange("serviceSpeedKn", parseFloat(e.target.value))}
          onBlur={() => handleFieldBlur("serviceSpeedKn")}
        />
      </FormField>

      {/* Sea Margin */}
      <FormField
        label="Sea Margin (%)"
        htmlFor="seaMarginPct"
        required
        error={errors.seaMarginPct}
        touched={touched.seaMarginPct}
        helpText="Allowance for hull fouling and weather. Typical: 15-20%"
      >
        <Input
          id="seaMarginPct"
          type="number"
          step="1"
          placeholder="e.g., 15"
          value={formData.seaMarginPct || ""}
          onChange={(e) => handleFieldChange("seaMarginPct", parseFloat(e.target.value))}
          onBlur={() => handleFieldBlur("seaMarginPct")}
        />
      </FormField>

      {/* Environment - Significant Wave Height */}
      <div className="space-y-2">
        <Label htmlFor="envHsM">Significant Wave Height Hs (m)</Label>
        <Input
          id="envHsM"
          type="number"
          step="0.5"
          placeholder="e.g., 3.5"
          value={formData.envHsM || ""}
          onChange={(e) => updateFormData({ envHsM: parseFloat(e.target.value) })}
        />
        <p className="text-xs text-muted-foreground">
          Design sea state. Typical: Coastal 2-3m, Ocean 3-5m, North Atlantic 5-7m
        </p>
      </div>

      {/* Environment - Wave Period */}
      <div className="space-y-2">
        <Label htmlFor="envTzS">Zero-Crossing Period Tz (s)</Label>
        <Input
          id="envTzS"
          type="number"
          step="0.5"
          placeholder="e.g., 7.0"
          value={formData.envTzS || ""}
          onChange={(e) => updateFormData({ envTzS: parseFloat(e.target.value) })}
        />
        <p className="text-xs text-muted-foreground">
          Wave period for seakeeping. Typical: 6-9 seconds
        </p>
      </div>

      {/* Endurance */}
      <div className="space-y-2">
        <Label htmlFor="enduranceNm">Endurance (nautical miles)</Label>
        <Input
          id="enduranceNm"
          type="number"
          placeholder="e.g., 8000"
          value={formData.enduranceNm || ""}
          onChange={(e) => updateFormData({ enduranceNm: parseFloat(e.target.value) })}
        />
        <p className="text-xs text-muted-foreground">
          Range at service speed. Used for fuel tank sizing.
        </p>
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
        <Button onClick={onNext} disabled={!isValid}>
          Next: Constraints →
        </Button>
      </div>
    </div>
  );
};
