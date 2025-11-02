import { useState } from "react";
import { Button } from "../../ui/button";
import { Input } from "../../ui/input";
import { Label } from "../../ui/label";
import { Loader2, Zap } from "lucide-react";
import { resistanceCalculationsApi } from "../../../services/resistanceApi";
import { SpeedPowerDraftHeatmap } from "../SpeedPowerDraftHeatmap";
import { getErrorMessage } from "../../../types/errors";
import type { VesselDetails } from "../../../types/hydrostatics";
import type { SpeedDraftMatrixResult } from "../../../types/resistance";
import toast from "react-hot-toast";

interface SpeedDraftMatrixPanelProps {
  vessel: VesselDetails;
}

export function SpeedDraftMatrixPanel({ vessel }: SpeedDraftMatrixPanelProps) {
  const [computing, setComputing] = useState(false);
  const [matrixResult, setMatrixResult] = useState<SpeedDraftMatrixResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Matrix configuration
  const [minSpeed, setMinSpeed] = useState(2.0); // m/s
  const [maxSpeed, setMaxSpeed] = useState(10.0); // m/s
  const [speedSteps, setSpeedSteps] = useState(20);

  const [minDraft, setMinDraft] = useState(vessel.designDraft * 0.7); // m
  const [maxDraft, setMaxDraft] = useState(vessel.designDraft * 1.3); // m
  const [draftSteps, setDraftSteps] = useState(15);

  const handleComputeMatrix = async () => {
    if (minSpeed >= maxSpeed) {
      toast.error("Max speed must be greater than min speed");
      return;
    }

    if (minDraft >= maxDraft) {
      toast.error("Max draft must be greater than min draft");
      return;
    }

    if (speedSteps < 2 || speedSteps > 100) {
      toast.error("Speed steps must be between 2 and 100");
      return;
    }

    if (draftSteps < 2 || draftSteps > 100) {
      toast.error("Draft steps must be between 2 and 100");
      return;
    }

    try {
      setComputing(true);
      setError(null);

      const result = await resistanceCalculationsApi.calculateSpeedDraftMatrix({
        vesselId: vessel.id,
        minSpeed,
        maxSpeed,
        speedSteps,
        minDraft,
        maxDraft,
        draftSteps,
        // Use vessel defaults for other parameters
        applyFormFactor: true,
        tempC: 15,
        salinityPpt: 35.0,
      });

      setMatrixResult(result);
      toast.success(`Matrix calculated: ${result.totalPoints} points`);
    } catch (err) {
      const message = getErrorMessage(err);
      setError(message);
      toast.error(`Failed to compute matrix: ${message}`);
    } finally {
      setComputing(false);
    }
  };

  return (
    <>
      <div className="space-y-4">
        <div className="flex items-center gap-2 mb-4">
          <Zap className="h-5 w-5 text-primary" />
          <h3 className="text-lg font-semibold">Speed-Power-Draft Matrix (Heatmap)</h3>
        </div>

        <p className="text-sm text-muted-foreground">
          Generate a comprehensive matrix showing how power and resistance vary across different
          speeds and drafts. Click any point on the heatmap to see detailed breakdown.
        </p>

        {/* Speed Range Configuration */}
        <div className="grid grid-cols-3 gap-4">
          <div>
            <Label htmlFor="minSpeed">Min Speed (m/s)</Label>
            <Input
              id="minSpeed"
              type="number"
              value={minSpeed}
              onChange={(e) => setMinSpeed(parseFloat(e.target.value) || 0)}
              step="0.5"
              min="0.1"
              max={maxSpeed - 0.5}
            />
            <p className="text-xs text-muted-foreground mt-1">
              {(minSpeed / 0.514444).toFixed(1)} kts
            </p>
          </div>
          <div>
            <Label htmlFor="maxSpeed">Max Speed (m/s)</Label>
            <Input
              id="maxSpeed"
              type="number"
              value={maxSpeed}
              onChange={(e) => setMaxSpeed(parseFloat(e.target.value) || 0)}
              step="0.5"
              min={minSpeed + 0.5}
            />
            <p className="text-xs text-muted-foreground mt-1">
              {(maxSpeed / 0.514444).toFixed(1)} kts
            </p>
          </div>
          <div>
            <Label htmlFor="speedSteps">Speed Steps</Label>
            <Input
              id="speedSteps"
              type="number"
              value={speedSteps}
              onChange={(e) => setSpeedSteps(parseInt(e.target.value) || 10)}
              min="2"
              max="100"
            />
            <p className="text-xs text-muted-foreground mt-1">
              Resolution: {((maxSpeed - minSpeed) / (speedSteps - 1)).toFixed(2)} m/s
            </p>
          </div>
        </div>

        {/* Draft Range Configuration */}
        <div className="grid grid-cols-3 gap-4">
          <div>
            <Label htmlFor="minDraft">Min Draft (m)</Label>
            <Input
              id="minDraft"
              type="number"
              value={minDraft}
              onChange={(e) => setMinDraft(parseFloat(e.target.value) || 0)}
              step="0.1"
              min="0.1"
              max={maxDraft - 0.1}
            />
            <p className="text-xs text-muted-foreground mt-1">
              {((minDraft / vessel.designDraft) * 100).toFixed(0)}% of design
            </p>
          </div>
          <div>
            <Label htmlFor="maxDraft">Max Draft (m)</Label>
            <Input
              id="maxDraft"
              type="number"
              value={maxDraft}
              onChange={(e) => setMaxDraft(parseFloat(e.target.value) || 0)}
              step="0.1"
              min={minDraft + 0.1}
            />
            <p className="text-xs text-muted-foreground mt-1">
              {((maxDraft / vessel.designDraft) * 100).toFixed(0)}% of design
            </p>
          </div>
          <div>
            <Label htmlFor="draftSteps">Draft Steps</Label>
            <Input
              id="draftSteps"
              type="number"
              value={draftSteps}
              onChange={(e) => setDraftSteps(parseInt(e.target.value) || 10)}
              min="2"
              max="100"
            />
            <p className="text-xs text-muted-foreground mt-1">
              Resolution: {((maxDraft - minDraft) / (draftSteps - 1)).toFixed(3)} m
            </p>
          </div>
        </div>

        {/* Info Card */}
        <div className="rounded-lg bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 p-4">
          <div className="flex items-start gap-2">
            <div className="mt-0.5">
              <svg
                className="h-5 w-5 text-blue-600 dark:text-blue-400"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            </div>
            <div className="text-sm text-blue-900 dark:text-blue-100">
              <p className="font-medium mb-1">Calculation Details</p>
              <ul className="list-disc list-inside space-y-1 text-xs">
                <li>Total calculation points: {speedSteps * draftSteps}</li>
                <li>
                  Each point computes full resistance breakdown using Holtrop-Mennen 1982 method
                </li>
                <li>Form coefficients are computed at each draft using hydrostatic analysis</li>
                <li>Typical computation time: 5-30 seconds depending on grid size</li>
              </ul>
            </div>
          </div>
        </div>

        {error && (
          <div className="rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 p-3">
            <p className="text-sm text-red-900 dark:text-red-100">{error}</p>
          </div>
        )}

        {/* Compute Button */}
        <Button onClick={handleComputeMatrix} disabled={computing} className="w-full" size="lg">
          {computing ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              Computing {speedSteps * draftSteps} points...
            </>
          ) : (
            <>
              <Zap className="mr-2 h-4 w-4" />
              Compute Speed-Power-Draft Matrix
            </>
          )}
        </Button>

        {/* Result Info */}
        {matrixResult && !computing && (
          <div className="rounded-lg bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 p-4">
            <div className="flex items-start gap-2">
              <div className="mt-0.5">
                <svg
                  className="h-5 w-5 text-green-600 dark:text-green-400"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
                  />
                </svg>
              </div>
              <div className="text-sm text-green-900 dark:text-green-100 flex-1">
                <p className="font-medium mb-1">Matrix Calculated Successfully</p>
                <p className="text-xs">
                  {matrixResult.totalPoints} points computed • Power range:{" "}
                  {Math.min(...matrixResult.powerMatrix.flat()).toFixed(0)} -{" "}
                  {Math.max(...matrixResult.powerMatrix.flat()).toFixed(0)} kW
                </p>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Heatmap Modal */}
      {matrixResult && (
        <SpeedPowerDraftHeatmap matrixResult={matrixResult} onClose={() => setMatrixResult(null)} />
      )}
    </>
  );
}
