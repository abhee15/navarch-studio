import { observer } from "mobx-react-lite";
import { useState } from "react";
import { seakeepingStore } from "../../../stores/SeakeepingStore";
import { Button } from "../../ui/button";
import toast from "react-hot-toast";

export const RaoSetupPanel = observer(() => {
  const [frequencyMin, setFrequencyMin] = useState(0.2);
  const [frequencyMax, setFrequencyMax] = useState(3.0);
  const [frequencyStep, setFrequencyStep] = useState(0.05);

  const pointCount = Math.ceil((frequencyMax - frequencyMin) / frequencyStep) + 1;

  const handleCalculate = async () => {
    if (!seakeepingStore.vesselSnapshot?.loadcase.id) {
      toast.error("No loadcase available");
      return;
    }

    try {
      await seakeepingStore.calculateRaos({
        loadcaseId: seakeepingStore.vesselSnapshot.loadcase.id,
        frequencyRange: {
          min: frequencyMin,
          max: frequencyMax,
          step: frequencyStep,
        },
        motionModes: ["heave", "pitch", "roll"],
      });
      toast.success("RAO calculation complete");
    } catch (error) {
      console.error("RAO calculation error:", error);
      toast.error("RAO calculation failed");
    }
  };

  return (
    <div className="p-4 space-y-4 border-t border-border">
      <div>
        <h3 className="font-semibold text-sm mb-3">RAO Calculation Setup</h3>

        <div className="space-y-3">
          {/* Frequency Min */}
          <div>
            <label className="text-xs font-medium text-muted-foreground">
              Min Frequency (rad/s)
            </label>
            <input
              type="number"
              value={frequencyMin}
              onChange={(e) => setFrequencyMin(Number(e.target.value))}
              step={0.1}
              min={0.1}
              max={2.0}
              className="w-full px-3 py-2 mt-1 border border-input rounded-md text-sm"
            />
          </div>

          {/* Frequency Max */}
          <div>
            <label className="text-xs font-medium text-muted-foreground">
              Max Frequency (rad/s)
            </label>
            <input
              type="number"
              value={frequencyMax}
              onChange={(e) => setFrequencyMax(Number(e.target.value))}
              step={0.1}
              min={0.5}
              max={5.0}
              className="w-full px-3 py-2 mt-1 border border-input rounded-md text-sm"
            />
          </div>

          {/* Frequency Step */}
          <div>
            <label className="text-xs font-medium text-muted-foreground">
              Frequency Step (rad/s)
            </label>
            <input
              type="number"
              value={frequencyStep}
              onChange={(e) => setFrequencyStep(Number(e.target.value))}
              step={0.01}
              min={0.01}
              max={0.5}
              className="w-full px-3 py-2 mt-1 border border-input rounded-md text-sm"
            />
          </div>

          {/* Info */}
          <div className="text-xs text-muted-foreground bg-muted p-2 rounded">
            <div>Frequency points: {pointCount}</div>
            <div>Estimated time: {Math.ceil(pointCount * 0.1)} seconds</div>
          </div>

          {/* Calculate Button */}
          <Button
            onClick={handleCalculate}
            disabled={seakeepingStore.isCalculating}
            className="w-full"
          >
            {seakeepingStore.isCalculating ? (
              <>
                <span className="animate-spin mr-2">⏳</span>
                Calculating...
              </>
            ) : (
              "Calculate RAOs"
            )}
          </Button>
        </div>
      </div>
    </div>
  );
});
