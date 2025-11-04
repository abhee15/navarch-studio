import { observer } from "mobx-react-lite";
import type { HydroResult } from "../../types/hydrostatics";
import { settingsStore } from "../../stores/SettingsStore";
import { getUnitSymbol } from "../../utils/unitSymbols";

interface HydrostaticsHUDProps {
  currentResult: HydroResult | null;
  draft: number;
  isAnimating?: boolean;
}

/**
 * HUD overlay component that displays key hydrostatic properties
 * Displays: Draft, Displacement (Δ), KB, and GM
 */
export const HydrostaticsHUD = observer(function HydrostaticsHUD({
  currentResult,
  draft,
  isAnimating = false,
}: HydrostaticsHUDProps) {
  const displayUnits = settingsStore.preferredUnits;
  const lengthUnit = getUnitSymbol(displayUnits, "Length");
  const weightUnit = getUnitSymbol(displayUnits, "Weight");

  // Convert from SI to display units
  const convertLength = (value: number): number => {
    if (displayUnits === "SI") return value;
    return value * 3.28084; // meters to feet
  };

  const convertWeight = (value: number): number => {
    if (displayUnits === "SI") return value;
    return value * 0.984207; // tonnes to long tons
  };

  const formatLength = (value: number): string => {
    return convertLength(value).toFixed(3);
  };

  const formatWeight = (value: number): string => {
    return convertWeight(value).toFixed(1);
  };

  return (
    <div className="absolute top-4 left-4 z-10 pointer-events-none">
      <div className="bg-background/90 backdrop-blur-sm border border-border rounded-lg shadow-lg p-4 space-y-2 min-w-[240px]">
        {/* Title */}
        <div className="flex items-center justify-between mb-2 pb-2 border-b border-border">
          <h3 className="text-sm font-semibold text-foreground">Hydrostatics</h3>
          {isAnimating && (
            <div className="flex items-center gap-1">
              <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse" />
              <span className="text-xs text-muted-foreground">Recording</span>
            </div>
          )}
        </div>

        {/* Draft */}
        <div className="flex items-center justify-between">
          <span className="text-xs font-medium text-muted-foreground">Draft</span>
          <span className="text-sm font-mono font-bold text-foreground">
            {formatLength(draft)} {lengthUnit}
          </span>
        </div>

        {currentResult && (
          <>
            {/* Displacement (Δ) */}
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium text-muted-foreground">Displacement (Δ)</span>
              <span className="text-sm font-mono font-bold text-blue-600 dark:text-blue-400">
                {formatWeight(currentResult.dispWeight)} {weightUnit}
              </span>
            </div>

            {/* KB (Center of Buoyancy) */}
            <div className="flex items-center justify-between">
              <span className="text-xs font-medium text-muted-foreground">KB</span>
              <span className="text-sm font-mono font-bold text-green-600 dark:text-green-400">
                {formatLength(currentResult.kBz)} {lengthUnit}
              </span>
            </div>

            {/* GM (Metacentric Height) */}
            {currentResult.gMt !== undefined && (
              <div className="flex items-center justify-between">
                <span className="text-xs font-medium text-muted-foreground">GM</span>
                <span
                  className={`text-sm font-mono font-bold ${
                    currentResult.gMt > 0
                      ? "text-purple-600 dark:text-purple-400"
                      : "text-red-600 dark:text-red-400"
                  }`}
                >
                  {formatLength(currentResult.gMt)} {lengthUnit}
                </span>
              </div>
            )}

            {/* Additional info */}
            <div className="pt-2 mt-2 border-t border-border space-y-1">
              <div className="flex items-center justify-between text-xs">
                <span className="text-muted-foreground">LCB</span>
                <span className="font-mono text-foreground">
                  {formatLength(currentResult.lCBx)} {lengthUnit}
                </span>
              </div>
              <div className="flex items-center justify-between text-xs">
                <span className="text-muted-foreground">Cb</span>
                <span className="font-mono text-foreground">{currentResult.cb.toFixed(3)}</span>
              </div>
            </div>
          </>
        )}

        {!currentResult && (
          <div className="text-xs text-muted-foreground italic py-2">
            No hydrostatic data available
          </div>
        )}
      </div>
    </div>
  );
});
