import { useState, useCallback, useMemo, useEffect } from "react";
import { Responsive, WidthProvider, Layout as GridLayout } from "react-grid-layout";
import "react-grid-layout/css/styles.css";
import "react-resizable/css/styles.css";
import type { PanelId } from "../../../types/workspace";
import type { HydroResult, VesselDetails, Loadcase, CurveData } from "../../../types/hydrostatics";
import { PanelWrapper } from "./panels/PanelWrapper";
import { KPISummaryPanel } from "./panels/KPISummaryPanel";
import { HydrostaticCurvesPanel } from "./panels/HydrostaticCurvesPanel";
import { HullDiagramPanel } from "./panels/HullDiagramPanel";
import { BodyPlanPanel } from "./panels/BodyPlanPanel";
import { HydrostaticsTablePanel } from "./panels/HydrostaticsTablePanel";
import { GeometryEditorPanel } from "./panels/GeometryEditorPanel";
import { ParametersSummaryPanel } from "./panels/ParametersSummaryPanel";
import { ComputationStatusPanel } from "./panels/ComputationStatusPanel";
import { Vessel3DPanel } from "./panels/Vessel3DPanel";

const ResponsiveGridLayout = WidthProvider(Responsive);

interface ViewModeLayoutProps {
  vessel: VesselDetails | null;
  results: HydroResult[];
  curves: Record<string, CurveData>;
  loadcases: Loadcase[];
  selectedLoadcaseId: string;
  waterType: string;
  minDraft: number;
  maxDraft: number;
  draftStep: number;
  integrationRule: string;
  smoothing: string;
  kg: number;
  lcg: number;
  computationTime: number | null;
  error: string | null;
  isMobile: boolean;
  gridLayouts: {
    lg: GridLayout[];
    md: GridLayout[];
    sm: GridLayout[];
  };
  panelStates: Partial<
    Record<PanelId, { collapsed: boolean; hidden: boolean; fullscreen: boolean }>
  >;
  onLayoutChange: (breakpoint: "lg" | "md" | "sm", layout: GridLayout[]) => void;
  onTogglePanelCollapsed: (panelId: PanelId) => void;
  onSetPanelFullscreen: (panelId: PanelId, fullscreen: boolean) => void;
  onSwitchToEditMode: () => void;
}

export function ViewModeLayout({
  vessel,
  results,
  curves,
  loadcases,
  selectedLoadcaseId,
  waterType,
  minDraft,
  maxDraft,
  draftStep,
  integrationRule,
  smoothing,
  kg,
  lcg,
  computationTime,
  error,
  isMobile,
  gridLayouts,
  panelStates,
  onLayoutChange,
  onTogglePanelCollapsed,
  onSetPanelFullscreen,
  onSwitchToEditMode,
}: ViewModeLayoutProps) {
  const [highlightedDraft, setHighlightedDraft] = useState<number | null>(null);
  const [currentBreakpoint, setCurrentBreakpoint] = useState<"lg" | "md" | "sm">("lg");

  // Track original heights for collapsed panels
  const [originalHeights, setOriginalHeights] = useState<
    Record<string, Partial<Record<PanelId, number>>>
  >({
    lg: {},
    md: {},
    sm: {},
  });

  // Get current result (middle draft as reference)
  const currentResult = results.length > 0 ? results[Math.floor(results.length / 2)] : null;

  // Find selected loadcase
  const selectedLoadcase = loadcases.find((lc) => lc.id === selectedLoadcaseId);

  // Initialize original heights from grid layouts
  useEffect(() => {
    const newOriginalHeights: Record<string, Partial<Record<PanelId, number>>> = {
      lg: {},
      md: {},
      sm: {},
    };

    (["lg", "md", "sm"] as const).forEach((breakpoint) => {
      gridLayouts[breakpoint].forEach((item) => {
        const panelId = item.i as PanelId;
        if (!originalHeights[breakpoint][panelId]) {
          newOriginalHeights[breakpoint][panelId] = item.h;
        } else {
          newOriginalHeights[breakpoint][panelId] = originalHeights[breakpoint][panelId];
        }
      });
    });

    setOriginalHeights(newOriginalHeights);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Adjust layouts based on collapsed state
  const adjustedLayouts = useMemo(() => {
    const adjusted: {
      lg: GridLayout[];
      md: GridLayout[];
      sm: GridLayout[];
    } = {
      lg: [],
      md: [],
      sm: [],
    };

    (["lg", "md", "sm"] as const).forEach((breakpoint) => {
      adjusted[breakpoint] = gridLayouts[breakpoint].map((item) => {
        const panelId = item.i as PanelId;
        const isCollapsed = panelStates[panelId]?.collapsed;

        if (isCollapsed) {
          // Store original height if not already stored
          if (!originalHeights[breakpoint][panelId]) {
            setOriginalHeights((prev) => ({
              ...prev,
              [breakpoint]: {
                ...prev[breakpoint],
                [panelId]: item.h,
              },
            }));
          }

          // Return collapsed height (just header)
          return {
            ...item,
            h: 1,
            minH: 1,
          };
        } else {
          // Restore original height if it was collapsed before
          const originalHeight = originalHeights[breakpoint][panelId];
          if (originalHeight && item.h === 1) {
            return {
              ...item,
              h: originalHeight,
              minH: item.minH || 2,
            };
          }
        }

        return item;
      });
    });

    return adjusted;
  }, [gridLayouts, panelStates, originalHeights]);

  // Handle breakpoint change
  const handleBreakpointChange = useCallback((breakpoint: string) => {
    setCurrentBreakpoint(breakpoint as "lg" | "md" | "sm");
  }, []);

  // Handle layout change
  const handleLayoutChange = useCallback(
    (_layout: GridLayout[], layouts: { lg: GridLayout[]; md: GridLayout[]; sm: GridLayout[] }) => {
      // Save the layout for the current breakpoint
      onLayoutChange(currentBreakpoint, layouts[currentBreakpoint]);
    },
    [currentBreakpoint, onLayoutChange]
  );

  // Get visible panels
  const visiblePanels = Object.entries(panelStates)
    .filter(([, state]) => !state.hidden)
    .map(([id]) => id as PanelId);

  // Empty state
  if (results.length === 0) {
    return (
      <div className="flex items-center justify-center h-full bg-background">
        <div className="text-center max-w-md">
          <svg
            className="mx-auto h-16 w-16 text-muted-foreground"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
            />
          </svg>
          <h3 className="mt-4 text-lg font-medium text-foreground">No Results Yet</h3>
          <p className="mt-2 text-sm text-muted-foreground">
            Switch to Edit mode to configure parameters and compute hydrostatics.
          </p>
          <button
            onClick={onSwitchToEditMode}
            className="mt-6 inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
          >
            <svg className="w-4 h-4 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
              />
            </svg>
            Configure & Compute
          </button>
        </div>
      </div>
    );
  }

  // Render panel content based on panel ID
  const renderPanelContent = (panelId: PanelId) => {
    switch (panelId) {
      case "kpis":
        return <KPISummaryPanel result={currentResult} />;

      case "curves":
        return (
          <HydrostaticCurvesPanel
            vesselId={vessel?.id || ""}
            vessel={vessel}
            results={results}
            curves={curves}
            onDraftHover={setHighlightedDraft}
          />
        );

      case "hull":
        return (
          <HullDiagramPanel
            vessel={vessel}
            currentResult={currentResult}
            results={results}
            highlightedDraft={highlightedDraft}
            kg={kg}
            lcg={lcg}
            onDraftHover={setHighlightedDraft}
          />
        );

      case "bodyplan":
        return <BodyPlanPanel vesselId={vessel?.id || ""} lpp={vessel?.lpp || 0} />;

      case "table":
        return (
          <HydrostaticsTablePanel
            results={results}
            highlightedDraft={highlightedDraft}
            onDraftHover={setHighlightedDraft}
          />
        );

      case "geometry":
        return (
          <GeometryEditorPanel
            vessel={vessel}
            onEditClick={() => onSetPanelFullscreen("geometry", true)}
          />
        );

      case "parameters":
        return (
          <ParametersSummaryPanel
            loadcaseName={selectedLoadcase?.name}
            waterType={waterType}
            minDraft={minDraft}
            maxDraft={maxDraft}
            draftStep={draftStep}
            integrationRule={integrationRule}
            smoothing={smoothing}
            onEditClick={onSwitchToEditMode}
          />
        );

      case "status":
        return (
          <ComputationStatusPanel
            computationTime={computationTime}
            resultsCount={results.length}
            error={error}
          />
        );

      case "vessel3d":
        return <Vessel3DPanel vessel={vessel} currentResult={currentResult} kg={kg} lcg={lcg} />;

      default:
        return <div className="text-sm text-muted-foreground">Unknown panel: {panelId}</div>;
    }
  };

  // Get panel title
  const getPanelTitle = (panelId: PanelId): string => {
    const titles: Partial<Record<PanelId, string>> = {
      kpis: "Key Performance Indicators",
      curves: "Hydrostatic Curves",
      hull: "Hull Diagram",
      bodyplan: "Body Plan (2D Sections)",
      table: "Hydrostatics Table",
      geometry: "Geometry Editor",
      parameters: "Parameters Summary",
      status: "Computation Status",
      vessel3d: "3D Vessel Visualization",
    };
    return titles[panelId] || "Panel";
  };

  return (
    <div className="h-full bg-background overflow-auto p-4">
      <ResponsiveGridLayout
        className="layout"
        layouts={adjustedLayouts}
        breakpoints={{ lg: 1200, md: 996, sm: 768 }}
        cols={{ lg: 12, md: 10, sm: 6 }}
        rowHeight={80}
        onBreakpointChange={handleBreakpointChange}
        onLayoutChange={handleLayoutChange}
        isDraggable={!isMobile}
        isResizable={!isMobile}
        draggableHandle=".cursor-move"
        compactType={null}
        margin={[12, 12]}
      >
        {visiblePanels.map((panelId) => {
          const state = panelStates[panelId];
          if (!state) return null; // Skip panels without state

          return (
            <div key={panelId}>
              <PanelWrapper
                panelId={panelId}
                title={getPanelTitle(panelId)}
                collapsed={state.collapsed}
                fullscreen={state.fullscreen}
                onToggleCollapse={() => onTogglePanelCollapsed(panelId)}
                onToggleFullscreen={(fullscreen) => onSetPanelFullscreen(panelId, fullscreen)}
                showDragHandle={!isMobile}
              >
                {renderPanelContent(panelId)}
              </PanelWrapper>
            </div>
          );
        })}
      </ResponsiveGridLayout>
    </div>
  );
}
