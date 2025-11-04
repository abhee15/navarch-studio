import { useState } from "react";
import { Label } from "../../ui/label";
import type { CandidateDesign } from "../../../types/sizing";

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
    beam: candidate.bM,
    draft: candidate.tM,
    cb: candidate.cb,
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
        updates.bM = localValues.beam;
        break;
      case "draft":
        updates.tM = localValues.draft;
        break;
      case "cb":
        updates.cb = localValues.cb;
        break;
    }
    onUpdate(updates);
  };

  const sliders = [
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
      min: candidate.bM * 0.7,
      max: candidate.bM * 1.3,
      step: 0.1,
      unit: "m",
      color: "cyan",
    },
    {
      id: "draft",
      label: "Draft",
      value: localValues.draft,
      min: candidate.tM * 0.7,
      max: candidate.tM * 1.3,
      step: 0.1,
      unit: "m",
      color: "green",
    },
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
  ];

  return (
    <div className="space-y-6">
      <div className="rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800 overflow-hidden shadow-lg">
        <div className="bg-gradient-to-r from-blue-50 to-cyan-50 dark:from-blue-900/20 dark:to-cyan-900/20 px-4 py-3 border-b border-gray-200 dark:border-gray-700">
          <h3 className="font-semibold text-gray-900 dark:text-white flex items-center gap-2">
            <span className="text-blue-600 dark:text-blue-400">🎚️</span>
            Interactive Parameters
          </h3>
          <p className="text-xs text-gray-600 dark:text-gray-400 mt-1">
            Adjust dimensions to explore design space
          </p>
        </div>

        <div className="p-6 space-y-6">
          {sliders.map((slider) => (
            <div key={slider.id} className="space-y-2">
              <div className="flex items-center justify-between">
                <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  {slider.label}
                </Label>
                <span
                  className={`text-sm font-bold tabular-nums text-${slider.color}-600 dark:text-${slider.color}-400`}
                >
                  {slider.value.toFixed(slider.step >= 1 ? 1 : 2)} {slider.unit}
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
                  className={`w-full h-2 rounded-lg appearance-none cursor-pointer
                    bg-gradient-to-r from-gray-200 via-${slider.color}-200 to-${slider.color}-400
                    dark:from-gray-700 dark:via-${slider.color}-900 dark:to-${slider.color}-700
                    disabled:opacity-50 disabled:cursor-not-allowed
                    slider-thumb-${slider.color}
                  `}
                  style={{
                    background: `linear-gradient(to right, 
                      rgb(229, 231, 235) 0%, 
                      var(--${slider.color}-200) 50%, 
                      var(--${slider.color}-400) 100%)`,
                  }}
                />
              </div>

              <div className="flex justify-between text-xs text-gray-500 dark:text-gray-400">
                <span>{slider.min.toFixed(1)}</span>
                <span className="text-gray-400 dark:text-gray-500">
                  ±
                  {(
                    ((slider.max - slider.min) / ((2 * (slider.max + slider.min)) / 2)) *
                    100
                  ).toFixed(0)}
                  %
                </span>
                <span>{slider.max.toFixed(1)}</span>
              </div>
            </div>
          ))}

          {isUpdating && (
            <div className="rounded-lg bg-blue-50 dark:bg-blue-900/20 p-3 flex items-center gap-3">
              <div className="h-4 w-4 animate-spin rounded-full border-2 border-blue-600 border-t-transparent"></div>
              <span className="text-sm text-blue-800 dark:text-blue-300">
                Re-solving with new parameters...
              </span>
            </div>
          )}

          <div className="rounded-lg bg-yellow-50 dark:bg-yellow-900/20 p-4 text-xs text-yellow-800 dark:text-yellow-400">
            <p className="font-medium mb-1">💡 How it works:</p>
            <ul className="space-y-1 ml-4">
              <li>• Drag sliders to adjust dimensions</li>
              <li>• Hull updates in real-time (visual preview)</li>
              <li>• Release to trigger solver re-computation</li>
              <li>• Solver maintains constraints and ratios</li>
            </ul>
          </div>
        </div>
      </div>

      <style>{`
        input[type="range"]::-webkit-slider-thumb {
          appearance: none;
          width: 20px;
          height: 20px;
          border-radius: 50%;
          background: linear-gradient(135deg, #3b82f6, #06b6d4);
          border: 3px solid white;
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
          cursor: pointer;
          transition: transform 0.2s;
        }
        
        input[type="range"]::-webkit-slider-thumb:hover {
          transform: scale(1.2);
          box-shadow: 0 4px 12px rgba(0, 0, 0, 0.3);
        }
        
        input[type="range"]::-webkit-slider-thumb:active {
          transform: scale(1.1);
        }
        
        input[type="range"]::-moz-range-thumb {
          width: 20px;
          height: 20px;
          border-radius: 50%;
          background: linear-gradient(135deg, #3b82f6, #06b6d4);
          border: 3px solid white;
          box-shadow: 0 2px 8px rgba(0, 0, 0, 0.2);
          cursor: pointer;
          transition: transform 0.2s;
        }
        
        input[type="range"]::-moz-range-thumb:hover {
          transform: scale(1.2);
        }
      `}</style>
    </div>
  );
};

