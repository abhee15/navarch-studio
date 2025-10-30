import { useState, useEffect, useCallback } from "react";
import { useNavigate } from "react-router-dom";
import type { VesselDetails, Loadcase, HydroResult } from "../../../types/hydrostatics";
import type { PanelId, WorkspaceMode } from "../../../types/workspace";
import type { Layout as GridLayout } from "react-grid-layout";
import { useWorkspaceLayout } from "../../../hooks/useWorkspaceLayout";
import { ModeToggle } from "./ModeToggle";
import { ViewModeLayout } from "./ViewModeLayout";
import { EditModeLayout } from "./EditModeLayout";
import { loadcasesApi, hydrostaticsApi, curvesApi } from "../../../services/hydrostaticsApi";
import { OffsetsGridEditor } from "../OffsetsGridEditor";

interface WorkspaceLayoutProps {
  vessel: VesselDetails;
  onBack: () => void;
}

export function WorkspaceLayout({ vessel, onBack }: WorkspaceLayoutProps) {
  const navigate = useNavigate();
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
  } = useWorkspaceLayout(vesselId);

  // Data state
  const [loadcases, setLoadcases] = useState<Loadcase[]>([]);
  const [selectedLoadcaseId, setSelectedLoadcaseId] = useState<string>("");
  const [results, setResults] = useState<HydroResult[]>([]);

  // Computation parameters
  const [waterType, setWaterType] = useState<string>("Salt Water");
  const [minDraft, setMinDraft] = useState<number>(3);
  const [maxDraft, setMaxDraft] = useState<number>(9);
  const [draftStep, setDraftStep] = useState<number>(0.5);
  const [integrationRule, setIntegrationRule] = useState<string>("Simpson 1/3");
  const [smoothing, setSmoothing] = useState<string>("None");
  const [kg, setKg] = useState<number>(0);
  const [lcg, setLcg] = useState<number>(0);
  const [tcg, setTcg] = useState<number>(0);

  // UI state
  const [loading, setLoading] = useState(false);
  const [computing, setComputing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [computationTime, setComputationTime] = useState<number | null>(null);
  const [showGeometryEditor, setShowGeometryEditor] = useState(false);
  const [isMobile, setIsMobile] = useState(false);

  // Detect mobile
  useEffect(() => {
    const checkMobile = () => {
      setIsMobile(window.innerWidth < 768);
    };
    checkMobile();
    window.addEventListener("resize", checkMobile);
    return () => window.removeEventListener("resize", checkMobile);
  }, []);

  // Load loadcases
  useEffect(() => {
    const loadLoadcases = async () => {
      try {
        setLoading(true);
        const data = await loadcasesApi.list(vesselId);
        setLoadcases(data.loadcases);
        if (data.loadcases.length > 0 && !selectedLoadcaseId) {
          setSelectedLoadcaseId(data.loadcases[0].id);
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to load loadcases");
      } finally {
        setLoading(false);
      }
    };
    loadLoadcases();
  }, [vesselId, selectedLoadcaseId]);

  // Smart mode switching based on results
  useEffect(() => {
    if (results.length > 0 && layout.mode === "edit") {
      // Auto-switch to view mode after successful computation
      setMode("view");
    } else if (results.length === 0 && layout.mode === "view") {
      // If no results, switch to edit mode
      setMode("edit");
    }
  }, [results.length, layout.mode, setMode]); // Dependencies for smart mode switching

  // Handle computation
  const handleCompute = useCallback(async () => {
    if (minDraft >= maxDraft) {
      setError("Min draft must be less than max draft");
      return;
    }

    try {
      setComputing(true);
      setError(null);

      // Generate draft array
      const drafts: number[] = [];
      for (let d = minDraft; d <= maxDraft; d += draftStep) {
        drafts.push(Number(d.toFixed(2)));
      }

      // Fetch both hydrostatic table and Bonjean curves in parallel
      // Bonjean curves are geometry-dependent only, so they can be pre-loaded
      // This ensures all curve types are available immediately after compute
      const bonjeanShouldFetch =
        (vessel.stationsCount ?? 0) > 0 &&
        (vessel.waterlinesCount ?? 0) > 0 &&
        (vessel.offsetsCount ?? 0) > 0;

      const promises: Promise<unknown>[] = [
        hydrostaticsApi.computeTable(vesselId, {
          loadcaseId: selectedLoadcaseId || undefined,
          drafts,
        }),
      ];

      if (bonjeanShouldFetch) {
        // Prefetch Bonjean only if geometry exists
        promises.push(curvesApi.getBonjean(vesselId).catch(() => ({ curves: [] })));
      }

      const [tableResponse] = (await Promise.all(promises)) as [
        {
          results: HydroResult[];
          computation_time_ms: number | null;
        },
      ];

      setResults(tableResponse.results);
      setComputationTime(tableResponse.computation_time_ms);

      // Auto-switch to view mode
      setMode("view");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to compute hydrostatics");
    } finally {
      setComputing(false);
    }
  }, [
    vesselId,
    selectedLoadcaseId,
    minDraft,
    maxDraft,
    draftStep,
    setMode,
    vessel.offsetsCount,
    vessel.stationsCount,
    vessel.waterlinesCount,
  ]);

  // Handle layout change
  const handleLayoutChange = useCallback(
    (breakpoint: "lg" | "md" | "sm", newLayout: GridLayout[]) => {
      updateGridLayout(breakpoint, newLayout);
    },
    [updateGridLayout]
  );

  // Handle panel state changes
  const handleTogglePanelCollapsed = useCallback(
    (panelId: PanelId) => {
      togglePanelCollapsed(panelId);
    },
    [togglePanelCollapsed]
  );

  const handleSetPanelFullscreen = useCallback(
    (panelId: PanelId, fullscreen: boolean) => {
      if (panelId === "geometry" && fullscreen) {
        setShowGeometryEditor(true);
      } else {
        setPanelFullscreen(panelId, fullscreen);
      }
    },
    [setPanelFullscreen]
  );

  const handleModeChange = useCallback(
    (mode: WorkspaceMode) => {
      setMode(mode);
    },
    [setMode]
  );

  const canSwitchToView = results.length > 0;

  return (
    <div className="h-screen flex flex-col bg-background overflow-hidden">
      {/* Top Toolbar */}
      <div className="border-b border-border bg-card/80 backdrop-blur-sm flex-shrink-0">
        <div className="px-4 py-3 flex items-center justify-between gap-4">
          {/* Left: Back button + Vessel name */}
          <div className="flex items-center gap-3 min-w-0">
            <button
              onClick={onBack}
              className="inline-flex items-center text-xs text-muted-foreground hover:text-foreground flex-shrink-0"
              title="Back to Classic View"
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
              Workspace
            </span>
          </div>

          {/* Center: Mode toggle (hide on mobile) */}
          {!isMobile && (
            <ModeToggle
              mode={layout.mode}
              onModeChange={handleModeChange}
              canSwitchToView={canSwitchToView}
            />
          )}

          {/* Right: Actions */}
          <div className="flex items-center gap-2 flex-shrink-0">
            {/* Preset Layouts Dropdown */}
            {layout.mode === "view" && !isMobile && (
              <select
                onChange={(e) => {
                  if (e.target.value === "reset") {
                    resetLayout();
                  } else {
                    loadPreset(e.target.value);
                  }
                  e.target.value = ""; // Reset dropdown
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

            {/* Compute Button */}
            <button
              onClick={handleCompute}
              disabled={computing || loading}
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
                  Computing...
                </>
              ) : (
                "Compute"
              )}
            </button>

            {computationTime && (
              <span className="text-[10px] text-muted-foreground">
                {computationTime.toFixed(0)}ms
              </span>
            )}
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
      <div className="flex-1 overflow-hidden">
        {layout.mode === "view" ? (
          <ViewModeLayout
            vessel={vessel}
            results={results}
            loadcases={loadcases}
            selectedLoadcaseId={selectedLoadcaseId}
            waterType={waterType}
            minDraft={minDraft}
            maxDraft={maxDraft}
            draftStep={draftStep}
            integrationRule={integrationRule}
            smoothing={smoothing}
            kg={kg}
            lcg={lcg}
            computationTime={computationTime}
            error={error}
            isMobile={isMobile}
            gridLayouts={layout.gridLayouts}
            panelStates={layout.panelStates}
            onLayoutChange={handleLayoutChange}
            onTogglePanelCollapsed={handleTogglePanelCollapsed}
            onSetPanelFullscreen={handleSetPanelFullscreen}
            onSwitchToEditMode={() => setMode("edit")}
          />
        ) : (
          <EditModeLayout
            vessel={vessel}
            loadcases={loadcases}
            selectedLoadcaseId={selectedLoadcaseId}
            waterType={waterType}
            minDraft={minDraft}
            maxDraft={maxDraft}
            draftStep={draftStep}
            integrationRule={integrationRule}
            smoothing={smoothing}
            kg={kg}
            lcg={lcg}
            tcg={tcg}
            onLoadcaseChange={setSelectedLoadcaseId}
            onWaterTypeChange={setWaterType}
            onMinDraftChange={setMinDraft}
            onMaxDraftChange={setMaxDraft}
            onDraftStepChange={setDraftStep}
            onIntegrationRuleChange={setIntegrationRule}
            onSmoothingChange={setSmoothing}
            onKgChange={setKg}
            onLcgChange={setLcg}
            onTcgChange={setTcg}
            onEditGeometry={() => setShowGeometryEditor(true)}
            onManageLoadcases={() =>
              navigate(`/hydrostatics/vessels/${vesselId}`, { state: { tab: "loadcases" } })
            }
          />
        )}
      </div>

      {/* Geometry Editor Modal */}
      {showGeometryEditor && (
        <OffsetsGridEditor
          vesselId={vesselId}
          isOpen={showGeometryEditor}
          onClose={() => setShowGeometryEditor(false)}
        />
      )}
    </div>
  );
}
