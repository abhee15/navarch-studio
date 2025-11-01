import type { VesselDetails } from "../../../types/hydrostatics";
import type { SpeedGrid, KcsBenchmarkResult } from "../../../types/resistance";
import { CalculationTypePanel } from "../panels/CalculationTypePanel";
import { SpeedGridPanel } from "../panels/SpeedGridPanel";
import { ResistanceParametersPanel } from "../panels/ResistanceParametersPanel";
import { PowerParametersPanel } from "../panels/PowerParametersPanel";
import { KcsBenchmarkPanel } from "../KcsBenchmarkPanel";

interface EditModeLayoutProps {
  vessel: VesselDetails;
  speedGrids: SpeedGrid[];
  selectedSpeedGridId: string;
  calculationType: "ittc57" | "holtrop-mennen";
  formFactor: number | undefined;
  applyFormFactor: boolean;
  tempC: number;
  salinityPpt: number;
  serviceMargin: number;
  etaD: number | undefined;
  etaH: number | undefined;
  etaR: number | undefined;
  etaO: number | undefined;
  useDecomposedEfficiency: boolean;
  onCalculationTypeChange: (type: "ittc57" | "holtrop-mennen") => void;
  onSpeedGridChange: (id: string) => void;
  onFormFactorChange: (value: number | undefined) => void;
  onApplyFormFactorChange: (value: boolean) => void;
  onTempChange: (value: number) => void;
  onSalinityChange: (value: number) => void;
  onServiceMarginChange: (value: number) => void;
  onEtaDChange: (value: number | undefined) => void;
  onEtaHChange: (value: number | undefined) => void;
  onEtaRChange: (value: number | undefined) => void;
  onEtaOChange: (value: number | undefined) => void;
  onUseDecomposedEfficiencyChange: (value: boolean) => void;
  onManageSpeedGrids: () => void;
  onBenchmarkComplete?: (result: KcsBenchmarkResult) => void;
}

/**
 * Edit mode layout for Resistance workspace
 * Displays all parameter panels in a simple vertical stack
 */
export function EditModeLayout(props: EditModeLayoutProps) {
  return (
    <div className="h-full overflow-auto bg-background">
      <div className="container mx-auto px-4 py-6 max-w-2xl">
        <div className="space-y-6">
          {/* Calculation Type */}
          <div className="bg-card border border-border rounded-lg p-4">
            <h2 className="text-sm font-semibold mb-3">Calculation Type</h2>
            <CalculationTypePanel
              calculationType={props.calculationType}
              onChange={props.onCalculationTypeChange}
            />
          </div>

          {/* Speed Grid */}
          <div className="bg-card border border-border rounded-lg p-4">
            <div className="flex items-center justify-between mb-3">
              <h2 className="text-sm font-semibold">Speed Grid</h2>
              <button
                onClick={props.onManageSpeedGrids}
                className="text-xs text-primary hover:text-primary/80"
              >
                Manage Grids
              </button>
            </div>
            <SpeedGridPanel
              speedGrids={props.speedGrids}
              selectedSpeedGridId={props.selectedSpeedGridId}
              onChange={props.onSpeedGridChange}
            />
          </div>

          {/* Resistance Parameters */}
          <div className="bg-card border border-border rounded-lg p-4">
            <h2 className="text-sm font-semibold mb-3">Parameters</h2>
            <ResistanceParametersPanel
              formFactor={props.formFactor}
              applyFormFactor={props.applyFormFactor}
              tempC={props.tempC}
              salinityPpt={props.salinityPpt}
              onFormFactorChange={props.onFormFactorChange}
              onApplyFormFactorChange={props.onApplyFormFactorChange}
              onTempChange={props.onTempChange}
              onSalinityChange={props.onSalinityChange}
            />
          </div>

          {/* Power Parameters (Holtrop-Mennen only) */}
          {props.calculationType === "holtrop-mennen" && (
            <>
              <div className="bg-card border border-border rounded-lg p-4">
                <h2 className="text-sm font-semibold mb-3">Power Calculation</h2>
                <PowerParametersPanel
                  serviceMargin={props.serviceMargin}
                  etaD={props.etaD}
                  etaH={props.etaH}
                  etaR={props.etaR}
                  etaO={props.etaO}
                  useDecomposedEfficiency={props.useDecomposedEfficiency}
                  onServiceMarginChange={props.onServiceMarginChange}
                  onEtaDChange={props.onEtaDChange}
                  onEtaHChange={props.onEtaHChange}
                  onEtaRChange={props.onEtaRChange}
                  onEtaOChange={props.onEtaOChange}
                  onUseDecomposedEfficiencyChange={props.onUseDecomposedEfficiencyChange}
                />
              </div>

              {/* KCS Benchmark */}
              <KcsBenchmarkPanel
                vesselId={props.vessel.id}
                vesselLWL={props.vessel.lpp}
                vesselBeam={props.vessel.beam}
                vesselDraft={props.vessel.designDraft}
                onBenchmarkComplete={props.onBenchmarkComplete || (() => {})}
              />
            </>
          )}

          {/* Info/Help Section */}
          <div className="bg-blue-50 dark:bg-blue-900/30 border border-blue-200 dark:border-blue-700 rounded-md p-4">
            <p className="text-xs text-blue-800 dark:text-blue-200">
              Configure all parameters above, then click <strong>Calculate</strong> in the toolbar
              to see results. The workspace will automatically switch to View mode to display
              results and charts.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
