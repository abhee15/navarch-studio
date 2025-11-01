import { useState, useEffect, useCallback } from "react";
import toast from "react-hot-toast";
import type { VesselDetails } from "../../types/hydrostatics";
import { speedGridsApi, resistanceCalculationsApi } from "../../services/resistanceApi";
import { ManageSpeedGridsDialog } from "./ManageSpeedGridsDialog";
import { getErrorMessage } from "../../types/errors";
import type {
  SpeedGrid,
  Ittc57CalculationResult,
  HoltropMennenCalculationResult,
  PowerCurveResult,
  KcsBenchmarkResult,
} from "../../types/resistance";
import { ResistanceExportDialog } from "./ResistanceExportDialog";
import { useWorkspaceLayout } from "../../hooks/useWorkspaceLayout";
import { ModeToggle } from "../hydrostatics/workspace/ModeToggle";
import { EditModeLayout } from "./workspace/EditModeLayout";
import { ViewModeLayout } from "./workspace/ViewModeLayout";

interface ResistanceWorkspaceLayoutProps {
  vessel: VesselDetails;
  onBack: () => void;
  onVesselUpdated?: () => void;
}

export function ResistanceWorkspaceLayout({ vessel, onBack }: ResistanceWorkspaceLayoutProps) {
  const vesselId = vessel.id;

  // Layout management
  const {
    layout,
    setMode,
    updateGridLayout,
    togglePanelCollapsed,
    setPanelFullscreen,
    resetLayout,
    loadPreset,
    getPresets,
  } = useWorkspaceLayout(vesselId, "resistance");

  // Mobile detection
  const [isMobile, setIsMobile] = useState(false);
  useEffect(() => {
    const checkMobile = () => {
      setIsMobile(window.innerWidth < 768);
    };
    checkMobile();
    window.addEventListener("resize", checkMobile);
    return () => window.removeEventListener("resize", checkMobile);
  }, []);

  // Data state
  const [speedGrids, setSpeedGrids] = useState<SpeedGrid[]>([]);
  const [selectedSpeedGridId, setSelectedSpeedGridId] = useState<string>("");
  const [showSpeedGridsDialog, setShowSpeedGridsDialog] = useState(false);
  const [showExportDialog, setShowExportDialog] = useState(false);

  // Calculation state
  const [calculationType, setCalculationType] = useState<"ittc57" | "holtrop-mennen">("ittc57");
  const [computing, setComputing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Results
  const [ittc57Result, setIttc57Result] = useState<Ittc57CalculationResult | null>(null);
  const [hmResult, setHmResult] = useState<HoltropMennenCalculationResult | null>(null);
  const [powerResult, setPowerResult] = useState<PowerCurveResult | null>(null);
  const [kcsBenchmarkResult, setKcsBenchmarkResult] = useState<KcsBenchmarkResult | null>(null);

  // Parameters
  const [formFactor, setFormFactor] = useState<number | undefined>(undefined);
  const [applyFormFactor, setApplyFormFactor] = useState(true);
  const [tempC, setTempC] = useState(15);
  const [salinityPpt, setSalinityPpt] = useState(35.0);

  // Power calculation parameters
  const [serviceMargin, setServiceMargin] = useState(15); // Percentage
  const [etaD, setEtaD] = useState<number | undefined>(0.65);
  const [etaH, setEtaH] = useState<number | undefined>(undefined);
  const [etaR, setEtaR] = useState<number | undefined>(undefined);
  const [etaO, setEtaO] = useState<number | undefined>(undefined);
  const [useDecomposedEfficiency, setUseDecomposedEfficiency] = useState(false);

  // Load speed grids
  useEffect(() => {
    const loadGrids = async () => {
      try {
        const data = await speedGridsApi.list(vesselId);
        setSpeedGrids(data.speedGrids);
        if (data.speedGrids.length > 0 && !selectedSpeedGridId) {
          setSelectedSpeedGridId(data.speedGrids[0].id);
        }
      } catch (err) {
        console.error("Error loading speed grids:", err);
      }
    };
    loadGrids();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [vesselId]);

  // Handle calculation
  const handleCalculate = useCallback(async () => {
    if (!selectedSpeedGridId) {
      toast.error("Please select a speed grid");
      return;
    }

    try {
      setComputing(true);
      setError(null);

      if (calculationType === "ittc57") {
        const result = await resistanceCalculationsApi.calculateIttc57({
          vesselId,
          speedGridId: selectedSpeedGridId,
          formFactor,
          applyFormFactor,
          tempC,
          salinityPpt,
        });
        setIttc57Result(result);
        setHmResult(null);
        setPowerResult(null);
      } else {
        const result = await resistanceCalculationsApi.calculateHoltropMennen({
          vesselId,
          speedGridId: selectedSpeedGridId,
          k: formFactor,
          applyFormFactor,
          tempC,
          salinityPpt,
        });
        setHmResult(result);
        setIttc57Result(null);

        // Auto-calculate power curves if EHP is available
        if (result.effectivePower.length > 0) {
          const powerCurves = await resistanceCalculationsApi.calculatePowerCurves({
            effectivePower: result.effectivePower,
            speedGrid: result.speedGrid,
            etaD: useDecomposedEfficiency ? undefined : etaD,
            etaH: useDecomposedEfficiency ? etaH : undefined,
            etaR: useDecomposedEfficiency ? etaR : undefined,
            etaO: useDecomposedEfficiency ? etaO : undefined,
            serviceMargin,
          });
          setPowerResult(powerCurves);
        }
      }

      toast.success("Calculation completed successfully");

      // Auto-switch to view mode after successful calculation
      setMode("view");
    } catch (err) {
      const errorMsg = getErrorMessage(err);
      setError(errorMsg);
      toast.error(errorMsg);
    } finally {
      setComputing(false);
    }
  }, [
    vesselId,
    selectedSpeedGridId,
    calculationType,
    formFactor,
    applyFormFactor,
    tempC,
    salinityPpt,
    serviceMargin,
    etaD,
    etaH,
    etaR,
    etaO,
    useDecomposedEfficiency,
    setMode,
  ]);

  // Recalculate power curves when service margin or efficiency changes (if HM result exists)
  useEffect(() => {
    if (hmResult && hmResult.effectivePower.length > 0) {
      const recalculatePower = async () => {
        try {
          const powerCurves = await resistanceCalculationsApi.calculatePowerCurves({
            effectivePower: hmResult.effectivePower,
            speedGrid: hmResult.speedGrid,
            etaD: useDecomposedEfficiency ? undefined : etaD,
            etaH: useDecomposedEfficiency ? etaH : undefined,
            etaR: useDecomposedEfficiency ? etaR : undefined,
            etaO: useDecomposedEfficiency ? etaO : undefined,
            serviceMargin,
          });
          setPowerResult(powerCurves);
        } catch (err) {
          console.error("Error recalculating power curves:", err);
        }
      };
      recalculatePower();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [serviceMargin, etaD, etaH, etaR, etaO, useDecomposedEfficiency]);

  const selectedGrid = speedGrids.find((g) => g.id === selectedSpeedGridId);

  return (
    <div className="h-screen flex flex-col bg-background overflow-hidden">
      {/* Top Toolbar */}
      <div className="border-b border-border bg-card/80 backdrop-blur-sm flex-shrink-0">
        <div className="h-14 px-4 flex items-center justify-between gap-4">
          {/* Left: Back button + Vessel name */}
          <div className="flex items-center gap-3 min-w-0">
            <button
              onClick={onBack}
              className="inline-flex items-center text-xs text-muted-foreground hover:text-foreground flex-shrink-0"
              title="Back to Vessels"
            >
              <svg className="h-4 w-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M15 19l-7-7 7-7"
                />
              </svg>
              Back
            </button>
            <div className="h-4 w-px bg-border"></div>
            <h1 className="text-base font-bold text-foreground truncate">{vessel.name}</h1>
            <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-medium bg-primary/10 text-primary flex-shrink-0">
              Resistance & Powering
            </span>
          </div>

          {/* Center: Mode toggle (hide on mobile) */}
          {!isMobile && (
            <ModeToggle
              mode={layout.mode}
              onModeChange={setMode}
              canSwitchToView={ittc57Result !== null || hmResult !== null}
            />
          )}

          {/* Right: Actions */}
          <div className="flex items-center gap-2 flex-shrink-0">
            {/* Preset Layouts Dropdown (view mode only) */}
            {layout.mode === "view" && !isMobile && (
              <select
                onChange={(e) => {
                  if (e.target.value === "reset") {
                    resetLayout();
                  } else {
                    loadPreset(e.target.value);
                  }
                  e.target.value = "";
                }}
                className="border border-border bg-background text-foreground rounded text-xs py-1.5 px-2 focus:outline-none focus:ring-2 focus:ring-ring"
                defaultValue=""
              >
                <option value="" disabled>
                  Layout Presets
                </option>
                <option value="reset">Reset to Default</option>
                {getPresets().map((preset) => (
                  <option key={preset.id} value={preset.id}>
                    {preset.name}
                  </option>
                ))}
              </select>
            )}

            <button
              onClick={() => setShowSpeedGridsDialog(true)}
              className="inline-flex items-center px-3 py-1.5 border border-border text-xs font-medium rounded hover:bg-accent/10"
            >
              <svg className="h-4 w-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M4 6h16M4 12h16M4 18h16"
                />
              </svg>
              Manage Speed Grids
            </button>
            {(ittc57Result || hmResult || powerResult) && (
              <button
                onClick={() => setShowExportDialog(true)}
                className="inline-flex items-center px-3 py-1.5 border border-border text-xs font-medium rounded hover:bg-accent/10"
              >
                <svg className="h-4 w-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                  />
                </svg>
                Export
              </button>
            )}
            <button
              onClick={handleCalculate}
              disabled={computing || !selectedSpeedGridId}
              className="inline-flex items-center px-4 py-1.5 border border-transparent text-xs font-semibold rounded shadow-sm text-primary-foreground bg-primary hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {computing ? (
                <>
                  <svg
                    className="animate-spin -ml-1 mr-1.5 h-3.5 w-3.5"
                    fill="none"
                    viewBox="0 0 24 24"
                  >
                    <circle
                      className="opacity-25"
                      cx="12"
                      cy="12"
                      r="10"
                      stroke="currentColor"
                      strokeWidth="4"
                    ></circle>
                    <path
                      className="opacity-75"
                      fill="currentColor"
                      d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                    ></path>
                  </svg>
                  Calculating...
                </>
              ) : (
                "Calculate"
              )}
            </button>
          </div>
        </div>
      </div>

      {/* Error Message */}
      {error && (
        <div className="bg-destructive/10 border-l-4 border-destructive p-2 mx-3 mt-2 flex-shrink-0">
          <div className="flex">
            <svg
              className="h-3.5 w-3.5 text-destructive flex-shrink-0"
              fill="currentColor"
              viewBox="0 0 20 20"
            >
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                clipRule="evenodd"
              />
            </svg>
            <p className="ml-2 text-xs text-destructive">{error}</p>
          </div>
        </div>
      )}

      {/* Main Content - Mode-based rendering */}
      <div className="flex-1 overflow-hidden">
        {layout.mode === "view" ? (
          <ViewModeLayout
            vessel={vessel}
            calculationType={calculationType}
            selectedSpeedGrid={selectedGrid}
            formFactor={formFactor}
            applyFormFactor={applyFormFactor}
            tempC={tempC}
            salinityPpt={salinityPpt}
            serviceMargin={serviceMargin}
            etaD={etaD}
            ittc57Result={ittc57Result}
            hmResult={hmResult}
            powerResult={powerResult}
            kcsBenchmarkResult={kcsBenchmarkResult}
            gridLayouts={layout.gridLayouts}
            panelStates={layout.panelStates}
            onLayoutChange={updateGridLayout}
            onTogglePanelCollapsed={togglePanelCollapsed}
            onSetPanelFullscreen={setPanelFullscreen}
            onSwitchToEditMode={() => setMode("edit")}
          />
        ) : (
          <EditModeLayout
            vessel={vessel}
            speedGrids={speedGrids}
            selectedSpeedGridId={selectedSpeedGridId}
            calculationType={calculationType}
            formFactor={formFactor}
            applyFormFactor={applyFormFactor}
            tempC={tempC}
            salinityPpt={salinityPpt}
            serviceMargin={serviceMargin}
            etaD={etaD}
            etaH={etaH}
            etaR={etaR}
            etaO={etaO}
            useDecomposedEfficiency={useDecomposedEfficiency}
            onCalculationTypeChange={setCalculationType}
            onSpeedGridChange={setSelectedSpeedGridId}
            onFormFactorChange={setFormFactor}
            onApplyFormFactorChange={setApplyFormFactor}
            onTempChange={setTempC}
            onSalinityChange={setSalinityPpt}
            onServiceMarginChange={setServiceMargin}
            onEtaDChange={setEtaD}
            onEtaHChange={setEtaH}
            onEtaRChange={setEtaR}
            onEtaOChange={setEtaO}
            onUseDecomposedEfficiencyChange={setUseDecomposedEfficiency}
            onManageSpeedGrids={() => setShowSpeedGridsDialog(true)}
            onBenchmarkComplete={(result) => setKcsBenchmarkResult(result)}
          />
        )}
      </div>

      {/* Dialogs */}
      <ManageSpeedGridsDialog
        vesselId={vesselId}
        isOpen={showSpeedGridsDialog}
        onClose={() => setShowSpeedGridsDialog(false)}
        onGridsUpdated={() => {
          // Reload grids
          speedGridsApi.list(vesselId).then((data) => {
            setSpeedGrids(data.speedGrids);
            if (!selectedSpeedGridId && data.speedGrids.length > 0) {
              setSelectedSpeedGridId(data.speedGrids[0].id);
            }
          });
        }}
      />

      {/* Export Dialog */}
      <ResistanceExportDialog
        vesselName={vessel.name}
        ittc57Result={ittc57Result}
        hmResult={hmResult}
        powerResult={powerResult}
        isOpen={showExportDialog}
        onClose={() => setShowExportDialog(false)}
      />
    </div>
  );
}
