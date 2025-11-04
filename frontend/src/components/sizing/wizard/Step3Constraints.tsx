import React from "react";
import type { CreateMissionCaseDto } from "../../../types/sizing";
import { Button } from "../../ui/button";
import { Input } from "../../ui/input";
import { Label } from "../../ui/label";

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
  { name: "None", loa: null, beam: null, draft: null, airdraft: null },
  { name: "Panamax", loa: 294.1, beam: 32.3, draft: 12.0, airdraft: 57.91 },
  { name: "Neo-Panamax", loa: 366.0, beam: 49.0, draft: 15.2, airdraft: 57.91 },
  { name: "Suezmax", loa: null, beam: 50.0, draft: 20.1, airdraft: 68.0 },
  { name: "Malaccamax", loa: 400.0, beam: 59.0, draft: 20.5, airdraft: null },
];

export const Step3Constraints: React.FC<Step3Props> = ({
  formData,
  updateFormData,
  onNext,
  onPrevious,
}) => {
  const applyPreset = (preset: (typeof CANAL_PRESETS)[0]) => {
    updateFormData({
      capLoaM: preset.loa || undefined,
      capBeamM: preset.beam || undefined,
      capDraftM: preset.draft || undefined,
      capAirdraftM: preset.airdraft || undefined,
    });
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white">
          Physical Constraints
        </h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          Define maximum dimensions (optional - leave blank for unconstrained)
        </p>
      </div>

      {/* Canal Presets */}
      <div className="space-y-2">
        <Label>Canal/Lock Presets</Label>
        <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
          {CANAL_PRESETS.map((preset) => (
            <button
              key={preset.name}
              type="button"
              onClick={() => applyPreset(preset)}
              className="rounded-md border border-gray-300 px-3 py-2 text-sm hover:bg-gray-50 dark:border-gray-600 dark:hover:bg-gray-700"
            >
              {preset.name}
            </button>
          ))}
        </div>
        <p className="text-xs text-gray-500 dark:text-gray-400">
          Quick apply standard canal/lock constraints
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
        <div className="space-y-2">
          <Label htmlFor="capBeamM">Max Beam (m)</Label>
          <Input
            id="capBeamM"
            type="number"
            step="0.1"
            placeholder="e.g., 32.3"
            value={formData.capBeamM || ""}
            onChange={(e) => updateFormData({ capBeamM: parseFloat(e.target.value) || undefined })}
          />
        </div>

        {/* Max Draft */}
        <div className="space-y-2">
          <Label htmlFor="capDraftM">Max Draft (m)</Label>
          <Input
            id="capDraftM"
            type="number"
            step="0.1"
            placeholder="e.g., 12.0"
            value={formData.capDraftM || ""}
            onChange={(e) => updateFormData({ capDraftM: parseFloat(e.target.value) || undefined })}
          />
        </div>

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
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Height from waterline to highest point
          </p>
        </div>
      </div>

      {/* Navigation */}
      <div className="flex justify-between pt-6 border-t border-gray-200 dark:border-gray-700">
        <Button variant="outline" onClick={onPrevious}>
          ← Previous
        </Button>
        <Button onClick={onNext}>Next: Options & Review →</Button>
      </div>
    </div>
  );
};


