import React, { useState } from "react";
import type { CreateMissionCaseDto } from "../../../types/sizing";
import { Button } from "../../ui/button";
import { Label } from "../../ui/label";
import { Input } from "../../ui/input";

interface Step4Props {
  formData: Partial<CreateMissionCaseDto>;
  updateFormData: (data: Partial<CreateMissionCaseDto>) => void;
  onNext: () => void;
  onPrevious: () => void;
  onSubmit: () => void;
  isFirstStep: boolean;
  isLastStep: boolean;
}

export const Step4Options: React.FC<Step4Props> = ({ formData, onPrevious, onSubmit }) => {
  const [maxCandidates, setMaxCandidates] = useState(5);

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white">Options & Review</h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          Review your mission and configure solver options
        </p>
      </div>

      {/* Mission Summary */}
      <div className="rounded-lg bg-blue-50 p-4 dark:bg-blue-900/20">
        <h3 className="font-semibold text-blue-900 dark:text-blue-300">Mission Summary</h3>
        <dl className="mt-3 grid grid-cols-2 gap-3 text-sm">
          <div>
            <dt className="font-medium text-blue-800 dark:text-blue-400">Name:</dt>
            <dd className="text-gray-700 dark:text-gray-300">{formData.name}</dd>
          </div>
          <div>
            <dt className="font-medium text-blue-800 dark:text-blue-400">Type:</dt>
            <dd className="text-gray-700 dark:text-gray-300">{formData.missionType}</dd>
          </div>
          <div>
            <dt className="font-medium text-blue-800 dark:text-blue-400">Cargo:</dt>
            <dd className="text-gray-700 dark:text-gray-300">
              {formData.cargoBasis === "teu" && `${formData.teuCount} TEU`}
              {formData.cargoBasis === "weight" && `${formData.cargoValue} tonnes`}
              {formData.cargoBasis === "volume" && `${formData.cargoVolumeM3} m³`}
            </dd>
          </div>
          <div>
            <dt className="font-medium text-blue-800 dark:text-blue-400">Speed:</dt>
            <dd className="text-gray-700 dark:text-gray-300">{formData.serviceSpeedKn} knots</dd>
          </div>
          {formData.capLoaM && (
            <div>
              <dt className="font-medium text-blue-800 dark:text-blue-400">Max LOA:</dt>
              <dd className="text-gray-700 dark:text-gray-300">{formData.capLoaM} m</dd>
            </div>
          )}
          {formData.capBeamM && (
            <div>
              <dt className="font-medium text-blue-800 dark:text-blue-400">Max Beam:</dt>
              <dd className="text-gray-700 dark:text-gray-300">{formData.capBeamM} m</dd>
            </div>
          )}
          {formData.capDraftM && (
            <div>
              <dt className="font-medium text-blue-800 dark:text-blue-400">Max Draft:</dt>
              <dd className="text-gray-700 dark:text-gray-300">{formData.capDraftM} m</dd>
            </div>
          )}
        </dl>
      </div>

      {/* Solver Options */}
      <div className="space-y-4">
        <h3 className="font-semibold text-gray-900 dark:text-white">Solver Options</h3>

        <div className="space-y-2">
          <Label htmlFor="maxCandidates">Maximum Candidates</Label>
          <Input
            id="maxCandidates"
            type="number"
            min="1"
            max="10"
            value={maxCandidates}
            onChange={(e) => setMaxCandidates(parseInt(e.target.value))}
          />
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Number of hull designs to generate (1-10). More candidates = longer compute time.
          </p>
        </div>

        <div className="rounded-lg bg-yellow-50 p-4 text-sm text-yellow-800 dark:bg-yellow-900/20 dark:text-yellow-400">
          <p className="font-medium">ℹ️ Solver Mode: First-Principles</p>
          <p className="mt-1">
            Our physics-based solver will generate candidates using displacement closure,
            Holtrop-Mennen resistance, and stability screening.
          </p>
          <p className="mt-1 text-xs">
            Expected compute time: ~1-2 seconds for {maxCandidates} candidates
          </p>
        </div>
      </div>

      {/* Navigation */}
      <div className="flex justify-between pt-6 border-t border-gray-200 dark:border-gray-700">
        <Button variant="outline" onClick={onPrevious}>
          ← Previous
        </Button>
        <Button
          onClick={onSubmit}
          className="bg-green-600 hover:bg-green-700 dark:bg-green-600 dark:hover:bg-green-700"
        >
          🚀 Generate Hulls
        </Button>
      </div>
    </div>
  );
};
