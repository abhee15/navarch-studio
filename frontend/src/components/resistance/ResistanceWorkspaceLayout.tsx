import { useState, useEffect, useCallback } from "react";
import toast from "react-hot-toast";
import type { VesselDetails, Loadcase, HydroResult } from "../../types/hydrostatics";
import { speedGridsApi, resistanceCalculationsApi } from "../../services/resistanceApi";
import { loadcasesApi, hydrostaticsApi } from "../../services/hydrostaticsApi";
import { defaultValuesApi } from "../../api/defaultValues";
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
import { Select } from "../ui/select";

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
    updatePanelState,
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

  // Loadcase and hydrostatics for summary
  const [loadcases, setLoadcases] = useState<Loadcase[]>([]);
  const [selectedLoadcaseId, setSelectedLoadcaseId] = useState<string>("");
  const [designDraftHydro, setDesignDraftHydro] = useState<HydroResult | null>(null);
  const [serviceSpeedIndex, setServiceSpeedIndex] = useState<number | null>(null);

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

  // Provenance tracking for default values
  const [formFactorProvenance, setFormFactorProvenance] = useState<string | undefined>(undefined);
  const [etaDProvenance, setEtaDProvenance] = useState<string | undefined>(undefined);
  const [etaHProvenance, setEtaHProvenance] = useState<string | undefined>(undefined);
  const [etaRProvenance, setEtaRProvenance] = useState<string | undefined>(undefined);
  const [etaOProvenance, setEtaOProvenance] = useState<string | undefined>(undefined);

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

  // Load loadcases
  useEffect(() => {
    const loadLoadcases = async () => {
      try {
        const data = await loadcasesApi.list(vesselId);
        setLoadcases(data.loadcases);
        if (data.loadcases.length > 0 && !selectedLoadcaseId) {
          setSelectedLoadcaseId(data.loadcases[0].id);
        }
      } catch (err) {
        console.error("Error loading loadcases:", err);
      }
    };
    loadLoadcases();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [vesselId]);

  // Load design draft hydrostatics when loadcase is selected
  useEffect(() => {
    const loadHydrostatics = async () => {
      if (!selectedLoadcaseId) {
        setDesignDraftHydro(null);
        return;
      }

      try {
        const result = await hydrostaticsApi.computeSingle(
          vesselId,
          selectedLoadcaseId,
          vessel.designDraft
        );
        setDesignDraftHydro(result);
      } catch (err) {
        console.error("Error loading hydrostatics:", err);
        setDesignDraftHydro(null);
      }
    };
    loadHydrostatics();
  }, [vesselId, selectedLoadcaseId, vessel.designDraft]);

  // Handle "Use Typical Values" for resistance parameters
  const handleUseTypicalValuesResistance = useCallback(async () => {
    try {
      const request = {
        cb: designDraftHydro?.cb,
        lb_Ratio: vessel.lpp / vessel.beam,
        bt_Ratio: vessel.beam / vessel.designDraft,
        lpp: vessel.lpp,
        beam: vessel.beam,
        draft: vessel.designDraft,
      };

      const result = await defaultValuesApi.getTypicalValues(request);

      if (result.formFactor) {
        setFormFactor(result.formFactor.value);
        setFormFactorProvenance(result.formFactor.provenance);
      }

      toast.success("Typical resistance values applied!");
    } catch (err) {
      console.error("Error fetching typical values:", err);
      toast.error(getErrorMessage(err));
    }
  }, [vessel, designDraftHydro]);

  // Handle "Use Typical Values" for power parameters
  const handleUseTypicalValuesPower = useCallback(async () => {
    try {
      const request = {
        cb: designDraftHydro?.cb,
        lb_Ratio: vessel.lpp / vessel.beam,
        bt_Ratio: vessel.beam / vessel.designDraft,
        lpp: vessel.lpp,
        beam: vessel.beam,
        draft: vessel.designDraft,
      };

      const result = await defaultValuesApi.getTypicalValues(request);

      if (useDecomposedEfficiency) {
        // Apply decomposed efficiencies
        if (result.etaH) {
          setEtaH(result.etaH.value);
          setEtaHProvenance(result.etaH.provenance);
        }
        if (result.etaR) {
          setEtaR(result.etaR.value);
          setEtaRProvenance(result.etaR.provenance);
        }
        if (result.etaO) {
          setEtaO(result.etaO.value);
          setEtaOProvenance(result.etaO.provenance);
        }
      } else {
        // Apply overall efficiency
        if (result.etaD) {
          setEtaD(result.etaD.value);
          setEtaDProvenance(result.etaD.provenance);
        }
      }

      toast.success("Typical power values applied!");
    } catch (err) {
      console.error("Error fetching typical values:", err);
      toast.error(getErrorMessage(err));
    }
  }, [vessel, designDraftHydro, useDecomposedEfficiency]);

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

        // Unhide ITTC-57 results and charts panels
        updatePanelState("resistance-ittc-results", { hidden: false });
        updatePanelState("resistance-charts", { hidden: false });
        // Hide HM and power panels
        updatePanelState("resistance-hm-results", { hidden: true });
        updatePanelState("resistance-power-results", { hidden: true });
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

        // Unhide HM results and charts panels
        updatePanelState("resistance-hm-results", { hidden: false });
        updatePanelState("resistance-charts", { hidden: false });
        // Hide ITTC-57 panel
        updatePanelState("resistance-ittc-results", { hidden: true });
        // Show unified summary if we have all needed data
        if (selectedLoadcaseId && designDraftHydro) {
          updatePanelState("resistance-unified-summary", { hidden: false });
        }

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
          // Unhide power results panel
          updatePanelState("resistance-power-results", { hidden: false });
          // Show unified summary if we have all needed data
          if (selectedLoadcaseId && designDraftHydro) {
            updatePanelState("resistance-unified-summary", { hidden: false });
          }
        } else {
          // Hide power panel if no power curves
          updatePanelState("resistance-power-results", { hidden: true });
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
    updatePanelState,
    selectedLoadcaseId,
    designDraftHydro,
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
  const selectedLoadcase = loadcases.find((lc) => lc.id === selectedLoadcaseId);

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
            {/* Loadcase Selector */}
            {loadcases.length > 0 && (
              <Select
                value={selectedLoadcaseId}
                onChange={setSelectedLoadcaseId}
                options={[
                  { value: "", label: "Select Loadcase" },
                  ...loadcases.map((lc) => ({
                    value: lc.id,
                    label: lc.name,
                  })),
                ]}
                className="text-xs min-w-[140px]"
              />
            )}

            {/* Service Speed Selector */}
            {hmResult && hmResult.speedGrid.length > 0 && (
              <Select
                value={serviceSpeedIndex !== null ? String(serviceSpeedIndex) : ""}
                onChange={(value) => setServiceSpeedIndex(value ? parseInt(value) : null)}
                options={[
                  { value: "", label: "Service Speed" },
                  ...hmResult.speedGrid.map((speed, idx) => ({
                    value: String(idx),
                    label: `${speed.toFixed(2)} m/s (${(speed * 1.94384).toFixed(1)} kn)`,
                  })),
                ]}
                className="text-xs min-w-[160px]"
              />
            )}

            {/* Preset Layouts Dropdown (view mode only) */}
            {layout.mode === "view" && !isMobile && (
              <Select
                value=""
                onChange={(value) => {
                  if (value === "reset") {
                    resetLayout();
                  } else if (value) {
                    loadPreset(value);
                  }
                }}
                options={[
                  { value: "", label: "Layout Presets" },
                  { value: "reset", label: "Reset to Default" },
                  ...getPresets().map((preset) => ({
                    value: preset.id,
                    label: preset.name,
                  })),
                ]}
                className="text-xs"
              />
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
            designDraftHydro={designDraftHydro}
            selectedLoadcase={selectedLoadcase}
            serviceSpeedIndex={serviceSpeedIndex}
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
            formFactorProvenance={formFactorProvenance}
            etaDProvenance={etaDProvenance}
            etaHProvenance={etaHProvenance}
            etaRProvenance={etaRProvenance}
            etaOProvenance={etaOProvenance}
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
            onUseTypicalValuesResistance={handleUseTypicalValuesResistance}
            onUseTypicalValuesPower={handleUseTypicalValuesPower}
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
