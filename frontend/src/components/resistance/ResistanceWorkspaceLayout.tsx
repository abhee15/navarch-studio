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
} from "../../types/resistance";
import { ResistanceCharts } from "./ResistanceCharts";
import { ResistanceExportDialog } from "./ResistanceExportDialog";
import { KcsBenchmarkPanel } from "./KcsBenchmarkPanel";

interface ResistanceWorkspaceLayoutProps {
  vessel: VesselDetails;
  onBack: () => void;
  onVesselUpdated?: () => void;
}

export function ResistanceWorkspaceLayout({ vessel, onBack }: ResistanceWorkspaceLayoutProps) {
  const vesselId = vessel.id;

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
  const [, setKcsBenchmarkResult] = useState<
    import("../../types/resistance").KcsBenchmarkResult | null
  >(null);

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

          {/* Right: Actions */}
          <div className="flex items-center gap-2 flex-shrink-0">
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

      {/* Main Content */}
      <div className="flex-1 overflow-auto">
        <div className="container mx-auto px-4 py-6 max-w-7xl">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            {/* Left Panel: Parameters */}
            <div className="lg:col-span-1 space-y-6">
              {/* Calculation Type */}
              <div className="bg-card border border-border rounded-lg p-4">
                <h2 className="text-sm font-semibold mb-3">Calculation Type</h2>
                <div className="space-y-2">
                  <label className="flex items-center">
                    <input
                      type="radio"
                      name="calcType"
                      value="ittc57"
                      checked={calculationType === "ittc57"}
                      onChange={() => setCalculationType("ittc57")}
                      className="mr-2"
                    />
                    <span className="text-sm">ITTC-57 Friction Only</span>
                  </label>
                  <label className="flex items-center">
                    <input
                      type="radio"
                      name="calcType"
                      value="holtrop-mennen"
                      checked={calculationType === "holtrop-mennen"}
                      onChange={() => setCalculationType("holtrop-mennen")}
                      className="mr-2"
                    />
                    <span className="text-sm">Holtrop-Mennen (Full)</span>
                  </label>
                </div>
              </div>

              {/* Speed Grid Selection */}
              <div className="bg-card border border-border rounded-lg p-4">
                <h2 className="text-sm font-semibold mb-3">Speed Grid</h2>
                <select
                  value={selectedSpeedGridId}
                  onChange={(e) => setSelectedSpeedGridId(e.target.value)}
                  className="w-full rounded-md border-border bg-background text-sm"
                >
                  <option value="">Select a speed grid...</option>
                  {speedGrids.map((grid) => (
                    <option key={grid.id} value={grid.id}>
                      {grid.name} ({grid.speedPoints.length} points)
                    </option>
                  ))}
                </select>
                {selectedGrid && (
                  <div className="mt-2 text-xs text-muted-foreground">
                    <p>Points: {selectedGrid.speedPoints.length}</p>
                    <p>
                      Range:{" "}
                      {selectedGrid.speedPoints.length > 0
                        ? `${Math.min(...selectedGrid.speedPoints.map((p) => p.speed)).toFixed(2)} - ${Math.max(...selectedGrid.speedPoints.map((p) => p.speed)).toFixed(2)} m/s`
                        : "N/A"}
                    </p>
                  </div>
                )}
              </div>

              {/* Parameters */}
              <div className="bg-card border border-border rounded-lg p-4">
                <h2 className="text-sm font-semibold mb-3">Parameters</h2>
                <div className="space-y-3">
                  <div>
                    <label className="block text-xs text-muted-foreground mb-1">
                      Form Factor (1+k)
                    </label>
                    <input
                      type="number"
                      step="0.01"
                      value={formFactor ?? ""}
                      onChange={(e) =>
                        setFormFactor(e.target.value ? parseFloat(e.target.value) : undefined)
                      }
                      placeholder="Default: 0.20"
                      className="w-full rounded-md border-border bg-background text-sm px-2 py-1"
                    />
                  </div>
                  <label className="flex items-center">
                    <input
                      type="checkbox"
                      checked={applyFormFactor}
                      onChange={(e) => setApplyFormFactor(e.target.checked)}
                      className="mr-2"
                    />
                    <span className="text-xs">Apply Form Factor</span>
                  </label>
                  <div>
                    <label className="block text-xs text-muted-foreground mb-1">
                      Temperature (°C)
                    </label>
                    <input
                      type="number"
                      value={tempC}
                      onChange={(e) => setTempC(parseInt(e.target.value) || 15)}
                      className="w-full rounded-md border-border bg-background text-sm px-2 py-1"
                    />
                  </div>
                  <div>
                    <label className="block text-xs text-muted-foreground mb-1">
                      Salinity (ppt)
                    </label>
                    <input
                      type="number"
                      step="0.1"
                      value={salinityPpt}
                      onChange={(e) => setSalinityPpt(parseFloat(e.target.value) || 35.0)}
                      className="w-full rounded-md border-border bg-background text-sm px-2 py-1"
                    />
                  </div>
                </div>
              </div>

              {/* KCS Benchmark Panel */}
              {calculationType === "holtrop-mennen" && (
                <KcsBenchmarkPanel
                  vesselId={vesselId}
                  vesselLWL={vessel.lpp}
                  vesselBeam={vessel.beam}
                  vesselDraft={vessel.designDraft}
                  onBenchmarkComplete={(result) => setKcsBenchmarkResult(result)}
                />
              )}

              {/* Power Calculation Parameters (only show for Holtrop-Mennen) */}
              {calculationType === "holtrop-mennen" && (
                <div className="bg-card border border-border rounded-lg p-4">
                  <h2 className="text-sm font-semibold mb-3">Power Calculation</h2>
                  <div className="space-y-3">
                    {/* Service Margin Slider */}
                    <div>
                      <div className="flex items-center justify-between mb-2">
                        <label className="block text-xs text-muted-foreground">
                          Service Margin
                        </label>
                        <span className="text-xs font-medium">{serviceMargin}%</span>
                      </div>
                      <input
                        type="range"
                        min="0"
                        max="30"
                        step="0.5"
                        value={serviceMargin}
                        onChange={(e) => setServiceMargin(parseFloat(e.target.value))}
                        className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer dark:bg-gray-700"
                      />
                      <div className="flex justify-between text-[10px] text-muted-foreground mt-1">
                        <span>0%</span>
                        <span>15%</span>
                        <span>30%</span>
                      </div>
                    </div>

                    {/* Efficiency Mode Toggle */}
                    <div>
                      <label className="flex items-center">
                        <input
                          type="checkbox"
                          checked={useDecomposedEfficiency}
                          onChange={(e) => setUseDecomposedEfficiency(e.target.checked)}
                          className="mr-2"
                        />
                        <span className="text-xs">Use Decomposed Efficiencies</span>
                      </label>
                    </div>

                    {/* Efficiency Inputs */}
                    {useDecomposedEfficiency ? (
                      <div className="space-y-2">
                        <div>
                          <label className="block text-xs text-muted-foreground mb-1">
                            ηH (Hull Efficiency)
                          </label>
                          <input
                            type="number"
                            step="0.01"
                            min="0.9"
                            max="1.05"
                            value={etaH ?? ""}
                            onChange={(e) =>
                              setEtaH(e.target.value ? parseFloat(e.target.value) : undefined)
                            }
                            placeholder="0.98"
                            className="w-full rounded-md border-border bg-background text-sm px-2 py-1"
                          />
                        </div>
                        <div>
                          <label className="block text-xs text-muted-foreground mb-1">
                            ηR (Relative Rotative)
                          </label>
                          <input
                            type="number"
                            step="0.01"
                            min="0.95"
                            max="1.05"
                            value={etaR ?? ""}
                            onChange={(e) =>
                              setEtaR(e.target.value ? parseFloat(e.target.value) : undefined)
                            }
                            placeholder="1.02"
                            className="w-full rounded-md border-border bg-background text-sm px-2 py-1"
                          />
                        </div>
                        <div>
                          <label className="block text-xs text-muted-foreground mb-1">
                            ηO (Open Water)
                          </label>
                          <input
                            type="number"
                            step="0.01"
                            min="0.5"
                            max="0.8"
                            value={etaO ?? ""}
                            onChange={(e) =>
                              setEtaO(e.target.value ? parseFloat(e.target.value) : undefined)
                            }
                            placeholder="0.65"
                            className="w-full rounded-md border-border bg-background text-sm px-2 py-1"
                          />
                        </div>
                      </div>
                    ) : (
                      <div>
                        <label className="block text-xs text-muted-foreground mb-1">
                          ηD (Overall Propulsive Efficiency)
                        </label>
                        <input
                          type="number"
                          step="0.01"
                          min="0.5"
                          max="0.8"
                          value={etaD ?? ""}
                          onChange={(e) =>
                            setEtaD(e.target.value ? parseFloat(e.target.value) : undefined)
                          }
                          className="w-full rounded-md border-border bg-background text-sm px-2 py-1"
                        />
                      </div>
                    )}

                    {/* Power Results Summary */}
                    {powerResult && (
                      <div className="mt-3 pt-3 border-t border-border text-xs">
                        <p className="text-muted-foreground mb-1">Current Settings:</p>
                        <p>
                          ηD = {(powerResult.etaD ?? 0.65).toFixed(3)}, SM ={" "}
                          {powerResult.serviceMargin.toFixed(1)}%
                        </p>
                        {powerResult.deliveredPower.length > 0 && (
                          <p className="mt-1">
                            Max DHP: {Math.max(...powerResult.deliveredPower).toFixed(0)} kW
                            <br />
                            Max P_inst: {Math.max(...powerResult.installedPower).toFixed(0)} kW
                          </p>
                        )}
                      </div>
                    )}
                  </div>
                </div>
              )}
            </div>

            {/* Right Panel: Results */}
            <div className="lg:col-span-2 space-y-6">
              {/* ITTC-57 Results */}
              {ittc57Result && (
                <div className="bg-card border border-border rounded-lg p-4">
                  <h2 className="text-sm font-semibold mb-3">ITTC-57 Friction Results</h2>
                  <div className="overflow-x-auto">
                    <table className="min-w-full text-xs">
                      <thead className="bg-muted">
                        <tr>
                          <th className="px-2 py-1 text-left">Speed (m/s)</th>
                          <th className="px-2 py-1 text-right">Re</th>
                          <th className="px-2 py-1 text-right">Fn</th>
                          <th className="px-2 py-1 text-right">CF</th>
                          <th className="px-2 py-1 text-right">CF_eff</th>
                        </tr>
                      </thead>
                      <tbody>
                        {ittc57Result.speedGrid.map((speed, idx) => (
                          <tr key={idx} className="border-t border-border">
                            <td className="px-2 py-1">{speed.toFixed(3)}</td>
                            <td className="px-2 py-1 text-right">
                              {ittc57Result.reynoldsNumbers[idx]?.toExponential(2)}
                            </td>
                            <td className="px-2 py-1 text-right">
                              {ittc57Result.froudeNumbers[idx]?.toFixed(4)}
                            </td>
                            <td className="px-2 py-1 text-right">
                              {ittc57Result.frictionCoefficients[idx]?.toFixed(6)}
                            </td>
                            <td className="px-2 py-1 text-right">
                              {ittc57Result.effectiveFrictionCoefficients[idx]?.toFixed(6)}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}

              {/* Holtrop-Mennen Results */}
              {hmResult && (
                <div className="bg-card border border-border rounded-lg p-4">
                  <h2 className="text-sm font-semibold mb-3">Holtrop-Mennen Resistance Results</h2>
                  <div className="overflow-x-auto">
                    <table className="min-w-full text-xs">
                      <thead className="bg-muted">
                        <tr>
                          <th className="px-2 py-1 text-left">Speed (m/s)</th>
                          <th className="px-2 py-1 text-right">RT (N)</th>
                          <th className="px-2 py-1 text-right">RF (N)</th>
                          <th className="px-2 py-1 text-right">RR (N)</th>
                          <th className="px-2 py-1 text-right">EHP (kW)</th>
                        </tr>
                      </thead>
                      <tbody>
                        {hmResult.speedGrid.map((speed, idx) => (
                          <tr key={idx} className="border-t border-border">
                            <td className="px-2 py-1">{speed.toFixed(3)}</td>
                            <td className="px-2 py-1 text-right">
                              {hmResult.totalResistance[idx]?.toFixed(0)}
                            </td>
                            <td className="px-2 py-1 text-right">
                              {hmResult.frictionResistance[idx]?.toFixed(0)}
                            </td>
                            <td className="px-2 py-1 text-right">
                              {hmResult.residuaryResistance[idx]?.toFixed(0)}
                            </td>
                            <td className="px-2 py-1 text-right">
                              {hmResult.effectivePower[idx]?.toFixed(2)}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}

              {/* Power Results Table */}
              {powerResult && (
                <div className="bg-card border border-border rounded-lg p-4">
                  <h2 className="text-sm font-semibold mb-3">Power Curves</h2>
                  <div className="overflow-x-auto">
                    <table className="min-w-full text-xs">
                      <thead className="bg-muted">
                        <tr>
                          <th className="px-2 py-1 text-left">Speed (m/s)</th>
                          <th className="px-2 py-1 text-right">EHP (kW)</th>
                          <th className="px-2 py-1 text-right">DHP (kW)</th>
                          <th className="px-2 py-1 text-right">P_inst (kW)</th>
                        </tr>
                      </thead>
                      <tbody>
                        {powerResult.speedGrid.map((speed, idx) => (
                          <tr key={idx} className="border-t border-border">
                            <td className="px-2 py-1">{speed.toFixed(3)}</td>
                            <td className="px-2 py-1 text-right">
                              {powerResult.effectivePower[idx]?.toFixed(2)}
                            </td>
                            <td className="px-2 py-1 text-right">
                              {powerResult.deliveredPower[idx]?.toFixed(2)}
                            </td>
                            <td className="px-2 py-1 text-right">
                              {powerResult.installedPower[idx]?.toFixed(2)}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}

              {!ittc57Result && !hmResult && (
                <div className="bg-card border border-border rounded-lg p-8 text-center text-muted-foreground">
                  <p>Select parameters and click Calculate to see results</p>
                </div>
              )}
            </div>
          </div>

          {/* Charts Section */}
          {(ittc57Result || hmResult || kcsBenchmarkResult) && (
            <div className="mt-6">
              <ResistanceCharts
                ittc57Result={ittc57Result}
                hmResult={hmResult}
                powerResult={powerResult}
                kcsBenchmarkResult={kcsBenchmarkResult}
              />
            </div>
          )}
        </div>
      </div>

      {/* Speed Grids Dialog */}
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
