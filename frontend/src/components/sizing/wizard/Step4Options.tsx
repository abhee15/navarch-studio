import React, { useState } from "react";
import type { CreateMissionCaseDto } from "../../../types/sizing";
import { Button } from "../../ui/button";
import { Label } from "../../ui/label";
import { Input } from "../../ui/input";
import {
  Package,
  Anchor,
  Ship,
  Fish,
  Sailboat,
  Waves,
  Calculator,
  BarChart3,
  Bot,
  Lightbulb,
  Fuel,
  Zap,
} from "lucide-react";

interface Step4Props {
  formData: Partial<CreateMissionCaseDto>;
  updateFormData: (data: Partial<CreateMissionCaseDto>) => void;
  solverMode?: "first_principles" | "data_driven_real" | "data_driven_ml";
  setSolverMode?: (mode: "first_principles" | "data_driven_real" | "data_driven_ml") => void;
  onNext: () => void;
  onPrevious: () => void;
  onSubmit: () => void;
  isFirstStep: boolean;
  isLastStep: boolean;
  isGenerating?: boolean;
}

export const Step4Options: React.FC<Step4Props> = ({
  formData,
  solverMode = "first_principles",
  setSolverMode = () => {},
  onPrevious,
  onSubmit,
  isGenerating,
}) => {
  const [maxCandidates, setMaxCandidates] = useState(5);
  const [minFn, setMinFn] = useState(0.15);
  const [maxFn, setMaxFn] = useState(0.35);

  // Locks (dimensional ratios to keep fixed)
  const [keepFn, setKeepFn] = useState(false);
  const [keepLOverB, setKeepLOverB] = useState(false);
  const [keepBOverT, setKeepBOverT] = useState(false);
  const [keepDOverT, setKeepDOverT] = useState(false);
  const [keepCbBand, setKeepCbBand] = useState(false);

  // Hull family hints
  const [familyHints, setFamilyHints] = useState<string[]>([]);

  const availableFamilies = [
    { value: "container", label: "Container Ship", icon: Package },
    { value: "tanker", label: "Tanker", icon: Fuel },
    { value: "bulk", label: "Bulk Carrier", icon: Anchor },
    { value: "general_cargo", label: "General Cargo", icon: Ship },
    { value: "fishing", label: "Fishing Vessel", icon: Fish },
    { value: "yacht_disp", label: "Displacement Yacht", icon: Sailboat },
    { value: "yacht_planing", label: "Planing Yacht", icon: Waves },
  ];

  const toggleFamily = (family: string) => {
    setFamilyHints((prev) =>
      prev.includes(family) ? prev.filter((f) => f !== family) : [...prev, family]
    );
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold text-gray-900 dark:text-white">Options & Review</h2>
        <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
          Review your brief and configure solver options
        </p>
      </div>

      {/* Brief Summary */}
      <div className="rounded-lg bg-blue-50 p-4 dark:bg-blue-900/20">
        <h3 className="font-semibold text-blue-900 dark:text-blue-300">Brief Summary</h3>
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

      {/* Solver Mode Selection */}
      <div className="space-y-3">
        <Label>Solver Mode</Label>
        <div className="grid grid-cols-3 gap-3">
          <button
            type="button"
            onClick={() => setSolverMode("first_principles")}
            className={`
              relative rounded-lg border-2 p-4 text-left transition-all
              ${
                solverMode === "first_principles"
                  ? "border-blue-500 bg-blue-50 dark:bg-blue-900/30"
                  : "border-gray-300 bg-white hover:border-gray-400 dark:border-gray-600 dark:bg-gray-800"
              }
            `}
          >
            <div className="flex items-start space-x-3">
              <Calculator className="h-8 w-8 text-gray-700 dark:text-gray-300" />
              <div className="flex-1">
                <h4 className="font-semibold text-gray-900 dark:text-white">First-Principles</h4>
                <p className="mt-1 text-xs text-gray-600 dark:text-gray-400">
                  Physics-based solver from brief requirements
                </p>
              </div>
            </div>
            {solverMode === "first_principles" && (
              <div className="absolute top-2 right-2">
                <div className="h-5 w-5 rounded-full bg-blue-500 flex items-center justify-center">
                  <div className="h-2 w-2 rounded-full bg-white"></div>
                </div>
              </div>
            )}
          </button>

          <button
            type="button"
            onClick={() => setSolverMode("data_driven_real")}
            className={`
              relative rounded-lg border-2 p-4 text-left transition-all
              ${
                solverMode === "data_driven_real"
                  ? "border-green-500 bg-green-50 dark:bg-green-900/30"
                  : "border-gray-300 bg-white hover:border-gray-400 dark:border-gray-600 dark:bg-gray-800"
              }
            `}
          >
            <div className="flex items-start space-x-3">
              <BarChart3 className="h-8 w-8 text-gray-700 dark:text-gray-300" />
              <div className="flex-1">
                <h4 className="font-semibold text-gray-900 dark:text-white">
                  Data-Driven{" "}
                  <span className="text-xs text-green-600 dark:text-green-400 font-bold">NEW</span>
                </h4>
                <p className="mt-1 text-xs text-gray-600 dark:text-gray-400">
                  KNN search on 600 real-world vessels
                </p>
              </div>
            </div>
            {solverMode === "data_driven_real" && (
              <div className="absolute top-2 right-2">
                <div className="h-5 w-5 rounded-full bg-green-500 flex items-center justify-center">
                  <div className="h-2 w-2 rounded-full bg-white"></div>
                </div>
              </div>
            )}
          </button>

          <button
            type="button"
            onClick={() => setSolverMode("data_driven_ml")}
            className={`
              relative rounded-lg border-2 p-4 text-left transition-all
              ${
                solverMode === "data_driven_ml"
                  ? "border-purple-500 bg-purple-50 dark:bg-purple-900/30"
                  : "border-gray-300 bg-white hover:border-gray-400 dark:border-gray-600 dark:bg-gray-800"
              }
            `}
          >
            <div className="flex items-start space-x-3">
              <Bot className="h-8 w-8 text-gray-700 dark:text-gray-300" />
              <div className="flex-1">
                <h4 className="font-semibold text-gray-900 dark:text-white">
                  ML/Parametric{" "}
                  <span className="text-xs text-purple-600 dark:text-purple-400 font-bold">
                    BETA
                  </span>
                </h4>
                <p className="mt-1 text-xs text-gray-600 dark:text-gray-400">
                  82K synthetic hulls from MIT ShipD
                </p>
              </div>
            </div>
            {solverMode === "data_driven_ml" && (
              <div className="absolute top-2 right-2">
                <div className="h-5 w-5 rounded-full bg-purple-500 flex items-center justify-center">
                  <div className="h-2 w-2 rounded-full bg-white"></div>
                </div>
              </div>
            )}
          </button>
        </div>
        <div className="flex items-start gap-2 text-xs text-gray-500 dark:text-gray-400">
          <Lightbulb className="h-4 w-4 mt-0.5 flex-shrink-0" />
          <p>
            <strong>First-Principles</strong>: Pure physics. <strong>Data-Driven</strong>: Real
            vessels (600). <strong>ML/Parametric</strong>: Massive design space (82K hulls).
          </p>
        </div>
      </div>

      {/* Solver Options */}
      <div className="space-y-6">
        <h3 className="font-semibold text-gray-900 dark:text-white">Solver Options</h3>

        {/* Basic Options */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
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
              Number of hull designs (1-10)
            </p>
          </div>

          <div className="space-y-2">
            <Label>Froude Number Range</Label>
            <div className="flex items-center space-x-2">
              <Input
                type="number"
                step="0.01"
                min="0.10"
                max="0.50"
                value={minFn}
                onChange={(e) => setMinFn(parseFloat(e.target.value))}
                className="w-20"
              />
              <span className="text-sm text-gray-500">to</span>
              <Input
                type="number"
                step="0.01"
                min="0.10"
                max="0.50"
                value={maxFn}
                onChange={(e) => setMaxFn(parseFloat(e.target.value))}
                className="w-20"
              />
            </div>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              Typical: 0.15-0.35 (displacement), 0.40+ (planing)
            </p>
          </div>
        </div>

        {/* Hull Family Hints */}
        <div className="space-y-2">
          <Label>Hull Family Hints (Optional)</Label>
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-2">
            {availableFamilies.map((family) => {
              const IconComponent = family.icon;
              return (
                <button
                  key={family.value}
                  type="button"
                  onClick={() => toggleFamily(family.value)}
                  className={`
                  flex items-center justify-center space-x-2 rounded-lg border-2 p-3 transition-all
                  ${
                    familyHints.includes(family.value)
                      ? "border-blue-500 bg-blue-50 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300"
                      : "border-gray-300 bg-white hover:border-gray-400 dark:border-gray-600 dark:bg-gray-800 dark:hover:border-gray-500"
                  }
                `}
                >
                  <IconComponent className="h-5 w-5" />
                  <span className="text-xs font-medium">{family.label}</span>
                </button>
              );
            })}
          </div>
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Select hull types to guide the solver (leave empty for automatic selection)
          </p>
        </div>

        {/* Dimensional Locks */}
        <div className="space-y-3">
          <Label>Dimensional Locks (Advanced)</Label>
          <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 rounded-lg border border-gray-300 p-4 dark:border-gray-600">
            <label className="flex items-center space-x-2 cursor-pointer">
              <input
                type="checkbox"
                checked={keepFn}
                onChange={(e) => setKeepFn(e.target.checked)}
                className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <span className="text-sm text-gray-700 dark:text-gray-300">Keep Fn</span>
            </label>
            <label className="flex items-center space-x-2 cursor-pointer">
              <input
                type="checkbox"
                checked={keepLOverB}
                onChange={(e) => setKeepLOverB(e.target.checked)}
                className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <span className="text-sm text-gray-700 dark:text-gray-300">Keep L/B</span>
            </label>
            <label className="flex items-center space-x-2 cursor-pointer">
              <input
                type="checkbox"
                checked={keepBOverT}
                onChange={(e) => setKeepBOverT(e.target.checked)}
                className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <span className="text-sm text-gray-700 dark:text-gray-300">Keep B/T</span>
            </label>
            <label className="flex items-center space-x-2 cursor-pointer">
              <input
                type="checkbox"
                checked={keepDOverT}
                onChange={(e) => setKeepDOverT(e.target.checked)}
                className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <span className="text-sm text-gray-700 dark:text-gray-300">Keep D/T</span>
            </label>
            <label className="flex items-center space-x-2 cursor-pointer">
              <input
                type="checkbox"
                checked={keepCbBand}
                onChange={(e) => setKeepCbBand(e.target.checked)}
                className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <span className="text-sm text-gray-700 dark:text-gray-300">Keep Cb Band</span>
            </label>
          </div>
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Lock dimensional ratios to preferred values (reduces design space exploration)
          </p>
        </div>

        {/* Solver Info */}
        {solverMode === "first_principles" ? (
          <div className="rounded-lg bg-gradient-to-r from-blue-50 to-cyan-50 p-4 text-sm dark:from-blue-900/20 dark:to-cyan-900/20">
            <p className="font-medium text-blue-900 dark:text-blue-300 flex items-center gap-2">
              <Calculator className="h-4 w-4" />
              Solver Mode: First-Principles
            </p>
            <p className="mt-1 text-blue-800 dark:text-blue-400">
              Physics-based solver using displacement closure, Holtrop-Mennen resistance, and
              stability screening.
            </p>
            <p className="mt-2 text-xs text-blue-700 dark:text-blue-500 flex items-center gap-1">
              <Zap className="h-3 w-3" />
              Expected compute time: ~1-2 seconds for {maxCandidates} candidates
            </p>
          </div>
        ) : solverMode === "data_driven_real" ? (
          <div className="rounded-lg bg-gradient-to-r from-green-50 to-emerald-50 p-4 text-sm dark:from-green-900/20 dark:to-emerald-900/20">
            <p className="font-medium text-green-900 dark:text-green-300 flex items-center gap-2">
              <BarChart3 className="h-4 w-4" />
              Solver Mode: Data-Driven (Real-World Catalog)
            </p>
            <p className="mt-1 text-green-800 dark:text-green-400">
              KNN search on 600 real-world vessels → Scaling → Physics refinement
            </p>
            <ul className="mt-2 space-y-1 text-xs text-green-700 dark:text-green-500">
              <li>✓ Faster convergence (~50% faster)</li>
              <li>✓ Proven hull forms (KCS, KVLCC2, etc.)</li>
              <li>✓ Shows reference vessel & similarity score</li>
            </ul>
            <p className="mt-2 text-xs text-green-700 dark:text-green-500 flex items-center gap-1">
              <Zap className="h-3 w-3" />
              Expected compute time: {"<"}1 second for {maxCandidates} candidates
            </p>
          </div>
        ) : (
          <div className="rounded-lg bg-gradient-to-r from-purple-50 to-violet-50 p-4 text-sm dark:from-purple-900/20 dark:to-violet-900/20">
            <p className="font-medium text-purple-900 dark:text-purple-300 flex items-center gap-2">
              <Bot className="h-4 w-4" />
              Solver Mode: ML/Parametric (ShipD Dataset - BETA)
            </p>
            <p className="mt-1 text-purple-800 dark:text-purple-400">
              KNN on 82K parametric hulls → Convert & scale → Physics refinement
            </p>
            <ul className="mt-2 space-y-1 text-xs text-purple-700 dark:text-purple-500">
              <li>✓ Massive design space (82K unique forms)</li>
              <li>✓ Explore unconventional geometries</li>
              <li>✓ Shows parametric hull ID & similarity</li>
            </ul>
            <p className="mt-2 text-xs text-purple-700 dark:text-purple-500 flex items-center gap-1">
              <Zap className="h-3 w-3" />
              Expected compute time: ~1 second for {maxCandidates} candidates
            </p>
          </div>
        )}
      </div>

      {/* Navigation */}
      <div className="flex justify-between pt-6 border-t border-gray-200 dark:border-gray-700">
        <Button variant="outline" onClick={onPrevious} disabled={isGenerating}>
          ← Previous
        </Button>
        <Button
          onClick={onSubmit}
          disabled={isGenerating}
          className="bg-green-600 hover:bg-green-700 dark:bg-green-600 dark:hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isGenerating ? (
            <>
              <div className="mr-2 h-4 w-4 animate-spin rounded-full border-2 border-white border-t-transparent"></div>
              Generating...
            </>
          ) : (
            <>
              <Ship className="mr-2 h-4 w-4" />
              Generate Hulls
            </>
          )}
        </Button>
      </div>
    </div>
  );
};
