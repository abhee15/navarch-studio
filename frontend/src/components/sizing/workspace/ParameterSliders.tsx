import { useState } from "react";
import { Label } from "../../ui/label";
import type { CandidateDesign } from "../../../types/sizing";
import { Sliders, Ruler, BarChart2, Triangle } from "lucide-react";

interface ParameterSlidersProps {
  candidate: CandidateDesign;
  onUpdate: (updates: Partial<CandidateDesign>) => void;
  isUpdating?: boolean;
}

/**
 * Interactive Parameter Sliders
 *
 * Allows real-time adjustment of hull dimensions
 * with live preview updates
 */
export const ParameterSliders: React.FC<ParameterSlidersProps> = ({
  candidate,
  onUpdate,
  isUpdating = false,
}) => {
  const [localValues, setLocalValues] = useState({
    lpp: candidate.lppM,
    beam: candidate.beamM,
    draft: candidate.draftM,
    depth: candidate.depthM,
    cb: candidate.cb,
    cp: candidate.cp,
    cwp: candidate.cwp,
  });

  const handleSliderChange = (param: keyof typeof localValues, value: number) => {
    setLocalValues((prev) => ({ ...prev, [param]: value }));
  };

  const handleSliderRelease = (param: keyof typeof localValues) => {
    // Trigger update to backend/solver
    const updates: Partial<CandidateDesign> = {};
    switch (param) {
      case "lpp":
        updates.lppM = localValues.lpp;
        break;
      case "beam":
        updates.beamM = localValues.beam;
        break;
      case "draft":
        updates.draftM = localValues.draft;
        break;
      case "depth":
        updates.depthM = localValues.depth;
        break;
      case "cb":
        updates.cb = localValues.cb;
        break;
      case "cp":
        updates.cp = localValues.cp;
        break;
      case "cwp":
        updates.cwp = localValues.cwp;
        break;
    }
    onUpdate(updates);
  };

  const dimensionSliders = [
    {
      id: "lpp",
      label: "Length (Lpp)",
      value: localValues.lpp,
      min: candidate.lppM * 0.7,
      max: candidate.lppM * 1.3,
      step: 0.5,
      unit: "m",
      color: "blue",
    },
    {
      id: "beam",
      label: "Beam",
      value: localValues.beam,
      min: candidate.beamM * 0.7,
      max: candidate.beamM * 1.3,
      step: 0.1,
      unit: "m",
      color: "cyan",
    },
    {
      id: "draft",
      label: "Draft",
      value: localValues.draft,
      min: candidate.draftM * 0.7,
      max: candidate.draftM * 1.3,
      step: 0.1,
      unit: "m",
      color: "green",
    },
    {
      id: "depth",
      label: "Depth",
      value: localValues.depth,
      min: candidate.depthM * 0.8,
      max: candidate.depthM * 1.2,
      step: 0.1,
      unit: "m",
      color: "amber",
    },
  ];

  const formSliders = [
    {
      id: "cb",
      label: "Block Coefficient (Cb)",
      value: localValues.cb,
      min: Math.max(0.4, candidate.cb - 0.15),
      max: Math.min(0.9, candidate.cb + 0.15),
      step: 0.01,
      unit: "",
      color: "purple",
    },
    {
      id: "cp",
      label: "Prismatic Coefficient (Cp)",
      value: localValues.cp,
      min: Math.max(0.5, candidate.cp - 0.1),
      max: Math.min(0.95, candidate.cp + 0.1),
      step: 0.01,
      unit: "",
      color: "indigo",
    },
    {
      id: "cwp",
      label: "Waterplane Coefficient (Cwp)",
      value: localValues.cwp,
      min: Math.max(0.6, candidate.cwp - 0.1),
      max: Math.min(0.98, candidate.cwp + 0.1),
      step: 0.01,
      unit: "",
      color: "violet",
    },
  ];

  // Calculate derived ratios
  const lOverB = (localValues.lpp / localValues.beam).toFixed(2);
  const bOverT = (localValues.beam / localValues.draft).toFixed(2);
  const dOverT = (localValues.depth / localValues.draft).toFixed(2);

  return (
    <div className="space-y-6">
      <div className="rounded-lg border border-border bg-card overflow-hidden shadow-lg">
        <div className="bg-gradient-to-r from-blue-50 to-cyan-50 dark:from-blue-900/20 dark:to-cyan-900/20 px-4 py-3 border-b border-border">
          <h3 className="font-semibold text-foreground flex items-center gap-2">
            <Sliders className="h-4 w-4 text-primary" />
            Interactive Parameters
          </h3>
          <p className="text-xs text-muted-foreground mt-1">
            Adjust dimensions to explore design space
          </p>
        </div>

        <div className="p-6 space-y-8">
          {/* Principal Dimensions Section */}
          <div>
            <h4 className="text-sm md:text-xs lg:text-sm font-semibold text-foreground mb-4 flex items-center gap-2">
              <Ruler className="h-4 w-4 text-blue-500" />
              Principal Dimensions
            </h4>
            <div className="space-y-4">
              {dimensionSliders.map((slider) => (
                <div key={slider.id} className="space-y-2">
                  <div className="flex items-center justify-between">
                    <Label className="text-sm md:text-xs lg:text-sm font-medium text-foreground">
                      {slider.label}
                    </Label>
                    <span
                      className={`text-sm md:text-xs lg:text-sm font-bold tabular-nums text-${slider.color}-600 dark:text-${slider.color}-400`}
                    >
                      {slider.value.toFixed(slider.step >= 1 ? 1 : 2)} {slider.unit}
                      <span className="text-[10px] ml-1 text-muted-foreground font-normal">
                        (±
                        {(
                          ((slider.max - slider.min) / ((2 * (slider.max + slider.min)) / 2)) *
                          100
                        ).toFixed(0)}
                        %)
                      </span>
                    </span>
                  </div>

                  <div className="relative">
                    <input
                      type="range"
                      min={slider.min}
                      max={slider.max}
                      step={slider.step}
                      value={slider.value}
                      onChange={(e) =>
                        handleSliderChange(
                          slider.id as keyof typeof localValues,
                          parseFloat(e.target.value)
                        )
                      }
                      onMouseUp={() => handleSliderRelease(slider.id as keyof typeof localValues)}
                      onTouchEnd={() => handleSliderRelease(slider.id as keyof typeof localValues)}
                      disabled={isUpdating}
                      className="w-full h-2.5 rounded-lg appearance-none cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed bg-gray-200 dark:bg-gray-700"
                    />
                  </div>

                  <div className="flex justify-between text-xs text-muted-foreground">
                    <span>{slider.min.toFixed(1)}</span>
                    <span>{slider.max.toFixed(1)}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Derived Ratios */}
          <div className="rounded-lg bg-gradient-to-r from-gray-50 to-blue-50 dark:from-gray-800 dark:to-blue-900/20 p-4 border border-border">
            <h4 className="text-xs md:text-[10px] lg:text-xs font-semibold text-foreground mb-3 flex items-center gap-2">
              <BarChart2 className="h-4 w-4 text-emerald-500" />
              <span className="hidden md:inline">Ratios (Live)</span>
              <span className="md:hidden">Derived Ratios (Live)</span>
            </h4>
            <div className="grid grid-cols-3 gap-2 md:gap-3 lg:gap-4 text-center">
              <div>
                <div className="text-xs text-muted-foreground mb-1">L/B</div>
                <div className="text-base md:text-sm lg:text-lg font-bold text-blue-600 dark:text-blue-400">
                  {lOverB}
                </div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground mb-1">B/T</div>
                <div className="text-base md:text-sm lg:text-lg font-bold text-cyan-600 dark:text-cyan-400">
                  {bOverT}
                </div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground mb-1">D/T</div>
                <div className="text-base md:text-sm lg:text-lg font-bold text-green-600 dark:text-green-400">
                  {dOverT}
                </div>
              </div>
            </div>
          </div>

          {/* Form Coefficients Section */}
          <div>
            <h4 className="text-sm md:text-xs lg:text-sm font-semibold text-foreground mb-4 flex items-center gap-2">
              <Triangle className="h-4 w-4 text-purple-500" />
              Form Coefficients
            </h4>
            <div className="space-y-4">
              {formSliders.map((slider) => (
                <div key={slider.id} className="space-y-2">
                  <div className="flex items-center justify-between">
                    <Label className="text-sm md:text-xs lg:text-sm font-medium text-foreground">
                      {slider.label}
                    </Label>
                    <span
                      className={`text-sm md:text-xs lg:text-sm font-bold tabular-nums text-${slider.color}-600 dark:text-${slider.color}-400`}
                    >
                      {slider.value.toFixed(slider.step >= 1 ? 1 : 2)} {slider.unit}
                      <span className="text-[10px] ml-1 text-muted-foreground font-normal">
                        (±
                        {(
                          ((slider.max - slider.min) / ((2 * (slider.max + slider.min)) / 2)) *
                          100
                        ).toFixed(0)}
                        %)
                      </span>
                    </span>
                  </div>

                  <div className="relative">
                    <input
                      type="range"
                      min={slider.min}
                      max={slider.max}
                      step={slider.step}
                      value={slider.value}
                      onChange={(e) =>
                        handleSliderChange(
                          slider.id as keyof typeof localValues,
                          parseFloat(e.target.value)
                        )
                      }
                      onMouseUp={() => handleSliderRelease(slider.id as keyof typeof localValues)}
                      onTouchEnd={() => handleSliderRelease(slider.id as keyof typeof localValues)}
                      disabled={isUpdating}
                      className="w-full h-2.5 rounded-lg appearance-none cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed bg-gray-200 dark:bg-gray-700"
                    />
                  </div>

                  <div className="flex justify-between text-xs text-muted-foreground">
                    <span>{slider.min.toFixed(2)}</span>
                    <span>{slider.max.toFixed(2)}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {isUpdating && (
            <div className="rounded-lg bg-blue-50 dark:bg-blue-900/20 p-3 flex items-center gap-3">
              <div className="h-4 w-4 animate-spin rounded-full border-2 border-blue-600 border-t-transparent"></div>
              <span className="text-sm text-blue-800 dark:text-blue-300">
                Re-solving with new parameters...
              </span>
            </div>
          )}
        </div>
      </div>

      <style>{`
        /* Webkit (Chrome, Safari, Edge) */
        input[type="range"]::-webkit-slider-thumb {
          appearance: none;
          width: 20px;
          height: 20px;
          border-radius: 50%;
          background: linear-gradient(135deg, #3b82f6, #06b6d4);
          border: 3px solid white;
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.25);
          cursor: pointer;
          transition: transform 0.2s, box-shadow 0.2s;
        }

        input[type="range"]::-webkit-slider-thumb:hover {
          transform: scale(1.15);
          box-shadow: 0 4px 12px rgba(59, 130, 246, 0.4);
        }

        input[type="range"]::-webkit-slider-thumb:active {
          transform: scale(1.05);
        }

        /* Firefox */
        input[type="range"]::-moz-range-thumb {
          width: 20px;
          height: 20px;
          border-radius: 50%;
          background: linear-gradient(135deg, #3b82f6, #06b6d4);
          border: 3px solid white;
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.25);
          cursor: pointer;
          transition: transform 0.2s, box-shadow 0.2s;
        }

        input[type="range"]::-moz-range-thumb:hover {
          transform: scale(1.15);
          box-shadow: 0 4px 12px rgba(59, 130, 246, 0.4);
        }

        input[type="range"]::-moz-range-thumb:active {
          transform: scale(1.05);
        }

        /* Ensure track is visible in both light and dark modes */
        input[type="range"]::-webkit-slider-runnable-track {
          width: 100%;
          height: 10px;
          border-radius: 5px;
        }

        input[type="range"]::-moz-range-track {
          width: 100%;
          height: 10px;
          border-radius: 5px;
        }
      `}</style>
    </div>
  );
};
