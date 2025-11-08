import { observer } from "mobx-react-lite";
import { seakeepingStore } from "../../../stores/SeakeepingStore";

export const MotionResponsePanel = observer(() => {
  const motion = seakeepingStore.motionResponse;

  if (!motion) {
    return (
      <div className="flex items-center justify-center h-96 bg-card border border-border rounded-lg">
        <div className="text-center space-y-2">
          <p className="text-lg font-medium text-muted-foreground">No Motion Response Data</p>
          <p className="text-sm text-muted-foreground">
            Configure sea state and click "Analyze Motion"
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-bold">Motion Response in Irregular Seas</h2>

      {/* Motion Cards */}
      <div className="grid grid-cols-3 gap-4">
        <div className="bg-card border border-border rounded-lg p-4">
          <div className="text-xs text-muted-foreground mb-1">Significant Heave</div>
          <div className="text-2xl font-bold text-blue-600">
            {motion.significantHeave.toFixed(2)} m
          </div>
        </div>

        <div className="bg-card border border-border rounded-lg p-4">
          <div className="text-xs text-muted-foreground mb-1">Significant Pitch</div>
          <div className="text-2xl font-bold text-green-600">
            {motion.significantPitch.toFixed(2)}°
          </div>
        </div>

        <div className="bg-card border border-border rounded-lg p-4">
          <div className="text-xs text-muted-foreground mb-1">Significant Roll</div>
          <div className="text-2xl font-bold text-red-600">
            {motion.significantRoll.toFixed(2)}°
          </div>
        </div>
      </div>

      {/* Sea State Info */}
      <div className="bg-muted/50 border border-border rounded-lg p-4">
        <div className="font-semibold mb-2">Sea State</div>
        <div className="grid grid-cols-2 gap-2 text-sm">
          <div>Hs: {motion.seaState.significantHeight.toFixed(1)} m</div>
          <div>Tp: {motion.seaState.peakPeriod.toFixed(1)} s</div>
          <div>Heading: {motion.seaState.heading}°</div>
          <div>Spectrum: {motion.seaState.spectrum}</div>
        </div>
      </div>

      {/* Phase 2: Add response spectra charts, mean periods table */}
    </div>
  );
});
