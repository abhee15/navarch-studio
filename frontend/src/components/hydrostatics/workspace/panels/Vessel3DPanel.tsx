import { useState, useMemo } from "react";
import { observer } from "mobx-react-lite";
import { Vessel3DViewer } from "../../Vessel3DViewer";
import { settingsStore } from "../../../../stores/SettingsStore";
import { getUnitSymbol } from "../../../../utils/unitSymbols";
import type { VesselDetails, HydroResult } from "../../../../types/hydrostatics";

interface Vessel3DPanelProps {
  vessel: VesselDetails | null;
  currentResult: HydroResult | null;
  kg?: number;
  lcg?: number;
}

/**
 * Convert value from SI (meters) to display units
 */
function convertFromSI(value: number, unitSystem: "SI" | "Imperial"): number {
  if (unitSystem === "SI") return value;
  // Convert meters to feet
  return value * 3.28084;
}

/**
 * Convert value from display units to SI (meters)
 */
function convertToSI(value: number, unitSystem: "SI" | "Imperial"): number {
  if (unitSystem === "SI") return value;
  // Convert feet to meters
  return value / 3.28084;
}

export const Vessel3DPanel = observer(function Vessel3DPanel({
  vessel,
  currentResult,
  kg = 0,
  lcg = 0,
}: Vessel3DPanelProps) {
  const displayUnits = settingsStore.preferredUnits;
  const lengthUnit = getUnitSymbol(displayUnits, "Length");

  // Convert vessel dimensions from SI (stored) to display units for sliders
  const [localLpp, setLocalLpp] = useState<number>(
    vessel ? convertFromSI(vessel.lpp, displayUnits) : 100
  );
  const [localBeam, setLocalBeam] = useState<number>(
    vessel ? convertFromSI(vessel.beam, displayUnits) : 20
  );
  const [localDesignDraft, setLocalDesignDraft] = useState<number>(
    vessel ? convertFromSI(vessel.designDraft, displayUnits) : 10
  );

  // Update local state when vessel changes
  useMemo(() => {
    if (vessel) {
      setLocalLpp(convertFromSI(vessel.lpp, displayUnits));
      setLocalBeam(convertFromSI(vessel.beam, displayUnits));
      setLocalDesignDraft(convertFromSI(vessel.designDraft, displayUnits));
    }
  }, [vessel?.id, vessel?.lpp, vessel?.beam, vessel?.designDraft, displayUnits]);

  // Convert local values back to SI for 3D viewer (always uses SI internally)
  const lppSI = useMemo(() => convertToSI(localLpp, displayUnits), [localLpp, displayUnits]);
  const beamSI = useMemo(() => convertToSI(localBeam, displayUnits), [localBeam, displayUnits]);
  const designDraftSI = useMemo(
    () => convertToSI(localDesignDraft, displayUnits),
    [localDesignDraft, displayUnits]
  );

  // Slider ranges based on unit system
  const lppRange = displayUnits === "SI" ? { min: 10, max: 500, step: 1 } : { min: 33, max: 1640, step: 1 };
  const beamRange = displayUnits === "SI" ? { min: 5, max: 100, step: 0.1 } : { min: 16, max: 328, step: 0.1 };
  const draftRange = displayUnits === "SI" ? { min: 1, max: 50, step: 0.1 } : { min: 3, max: 164, step: 0.1 };

  const isReadOnly = vessel?.isTemplate ?? false;

  if (!vessel) {
    return (
      <div className="flex items-center justify-center h-full">
        <p className="text-sm text-muted-foreground">No vessel data</p>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full">
      {/* 3D Viewer */}
      <div className="flex-1 min-h-0">
        <Vessel3DViewer
          lpp={lppSI}
          beam={beamSI}
          designDraft={designDraftSI}
          draft={currentResult?.draft}
          kb={currentResult?.kBz}
          lcb={currentResult?.lCBx}
          kg={kg}
          lcg={lcg}
          currentResult={currentResult}
        />
      </div>

      {/* Parameter Sliders */}
      <div className="border-t border-border bg-card p-4 space-y-4 flex-shrink-0">
        <div className="space-y-2">
          <label className="block text-xs font-medium text-foreground">
            Length Between Perpendiculars (LPP)
          </label>
          <div className="flex items-center gap-3">
            <input
              type="range"
              min={lppRange.min}
              max={lppRange.max}
              step={lppRange.step}
              value={localLpp}
              onChange={(e) => setLocalLpp(parseFloat(e.target.value))}
              disabled={isReadOnly}
              className="flex-1 h-2 bg-gray-200 dark:bg-gray-700 rounded-lg appearance-none cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
            />
            <div className="w-24 text-right text-sm font-medium text-foreground">
              {localLpp.toFixed(1)} {lengthUnit}
            </div>
          </div>
        </div>

        <div className="space-y-2">
          <label className="block text-xs font-medium text-foreground">Beam (B)</label>
          <div className="flex items-center gap-3">
            <input
              type="range"
              min={beamRange.min}
              max={beamRange.max}
              step={beamRange.step}
              value={localBeam}
              onChange={(e) => setLocalBeam(parseFloat(e.target.value))}
              disabled={isReadOnly}
              className="flex-1 h-2 bg-gray-200 dark:bg-gray-700 rounded-lg appearance-none cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
            />
            <div className="w-24 text-right text-sm font-medium text-foreground">
              {localBeam.toFixed(1)} {lengthUnit}
            </div>
          </div>
        </div>

        <div className="space-y-2">
          <label className="block text-xs font-medium text-foreground">Design Draft (D)</label>
          <div className="flex items-center gap-3">
            <input
              type="range"
              min={draftRange.min}
              max={draftRange.max}
              step={draftRange.step}
              value={localDesignDraft}
              onChange={(e) => setLocalDesignDraft(parseFloat(e.target.value))}
              disabled={isReadOnly}
              className="flex-1 h-2 bg-gray-200 dark:bg-gray-700 rounded-lg appearance-none cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
            />
            <div className="w-24 text-right text-sm font-medium text-foreground">
              {localDesignDraft.toFixed(1)} {lengthUnit}
            </div>
          </div>
        </div>

        {isReadOnly && (
          <p className="text-xs text-muted-foreground italic">
            Template vessels are read-only. Create your own vessel to modify parameters.
          </p>
        )}
      </div>
    </div>
  );
});

