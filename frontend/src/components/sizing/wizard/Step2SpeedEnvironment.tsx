import React from "react";
import type { CreateMissionCaseDto } from "../../../types/sizing";
import { Button } from "../../ui/button";
import { Input } from "../../ui/input";
import { Label } from "../../ui/label";

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
  const isValid = formData.serviceSpeedKn && formData.serviceSpeedKn > 0;

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-foreground">Speed & Environment</h2>
        <p className="mt-1 text-sm text-muted-foreground">
          Define operational speed and environmental conditions
        </p>
      </div>

      {/* Service Speed */}
      <div className="space-y-2">
        <Label htmlFor="serviceSpeedKn">Service Speed (knots) *</Label>
        <Input
          id="serviceSpeedKn"
          type="number"
          step="0.5"
          placeholder="e.g., 22"
          value={formData.serviceSpeedKn || ""}
          onChange={(e) => updateFormData({ serviceSpeedKn: parseFloat(e.target.value) })}
        />
        <p className="text-xs text-muted-foreground">
          Typical: Tanker 14-16kn, Bulk carrier 14-15kn, Container 20-25kn, Fast ferry 30-40kn
        </p>
      </div>

      {/* Sea Margin */}
      <div className="space-y-2">
        <Label htmlFor="seaMarginPct">Sea Margin (%)</Label>
        <Input
          id="seaMarginPct"
          type="number"
          step="1"
          placeholder="e.g., 15"
          value={formData.seaMarginPct || ""}
          onChange={(e) => updateFormData({ seaMarginPct: parseFloat(e.target.value) })}
        />
        <p className="text-xs text-muted-foreground">
          Allowance for hull fouling and weather. Typical: 15-20%
        </p>
      </div>

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
