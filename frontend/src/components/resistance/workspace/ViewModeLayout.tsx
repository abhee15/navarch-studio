import { Responsive, WidthProvider, Layout as GridLayout } from "react-grid-layout";
import "react-grid-layout/css/styles.css";
import "react-resizable/css/styles.css";
import type { VesselDetails } from "../../../types/hydrostatics";
import type {
  SpeedGrid,
  Ittc57CalculationResult,
  HoltropMennenCalculationResult,
  PowerCurveResult,
  KcsBenchmarkResult,
} from "../../../types/resistance";
import type { PanelId, PanelState } from "../../../types/workspace";
import { PanelWrapper } from "../panels/PanelWrapper";
import { Ittc57ResultsPanel } from "../panels/Ittc57ResultsPanel";
import { HoltropMennenResultsPanel } from "../panels/HoltropMennenResultsPanel";
import { PowerResultsPanel } from "../panels/PowerResultsPanel";
import { ResistanceChartsPanel } from "../panels/ResistanceChartsPanel";

const ResponsiveGridLayout = WidthProvider(Responsive);

interface ViewModeLayoutProps {
  vessel: VesselDetails;
  calculationType: "ittc57" | "holtrop-mennen";
  selectedSpeedGrid: SpeedGrid | undefined;
  // Parameters (read-only summary)
  formFactor: number | undefined;
  applyFormFactor: boolean;
  tempC: number;
  salinityPpt: number;
  serviceMargin: number;
  etaD: number | undefined;
  // Results
  ittc57Result: Ittc57CalculationResult | null;
  hmResult: HoltropMennenCalculationResult | null;
  powerResult: PowerCurveResult | null;
  kcsBenchmarkResult: KcsBenchmarkResult | null;
  // Layout
  gridLayouts: { lg: GridLayout[]; md: GridLayout[]; sm: GridLayout[] };
  panelStates: Partial<Record<PanelId, PanelState>>;
  onLayoutChange: (breakpoint: "lg" | "md" | "sm", layout: GridLayout[]) => void;
  onTogglePanelCollapsed: (panelId: PanelId) => void;
  onSetPanelFullscreen: (panelId: PanelId, fullscreen: boolean) => void;
  onSwitchToEditMode: () => void;
}

/**
 * View mode layout for Resistance workspace
 * Uses React Grid Layout for draggable/resizable panels
 */
export function ViewModeLayout(props: ViewModeLayoutProps) {
  const { gridLayouts, panelStates, onLayoutChange, onTogglePanelCollapsed, onSetPanelFullscreen } =
    props;

  const breakpoints = { lg: 1200, md: 996, sm: 768 };
  const cols = { lg: 12, md: 10, sm: 6 };

  // Get panel state helper
  const getPanelState = (panelId: PanelId): PanelState => {
    return (
      panelStates[panelId] || {
        id: panelId,
        collapsed: false,
        hidden: false,
        fullscreen: false,
      }
    );
  };

  return (
    <div className="h-full overflow-auto bg-background p-4">
      <ResponsiveGridLayout
        className="layout"
        layouts={gridLayouts}
        breakpoints={breakpoints}
        cols={cols}
        rowHeight={60}
        onBreakpointChange={() => {
          // Could handle breakpoint changes if needed
        }}
        onLayoutChange={(layout, _layouts) => {
          // Determine current breakpoint and save
          const width = window.innerWidth;
          const breakpoint = width >= 1200 ? "lg" : width >= 996 ? "md" : "sm";
          onLayoutChange(breakpoint, layout);
        }}
        isDraggable={true}
        isResizable={true}
        compactType="vertical"
        preventCollision={false}
      >
        {/* Parameter Summary Panel */}
        <div key="resistance-parameters">
          <PanelWrapper
            panelId="resistance-parameters"
            title="Parameters Summary"
            panelState={getPanelState("resistance-parameters")}
            onToggleCollapse={() => onTogglePanelCollapsed("resistance-parameters")}
            actions={
              <button
                onClick={props.onSwitchToEditMode}
                className="text-xs px-2 py-1 rounded border border-border hover:bg-accent/10"
              >
                Edit
              </button>
            }
          >
            <div className="space-y-2 text-xs">
              <p>
                <strong>Method:</strong>{" "}
                {props.calculationType === "ittc57" ? "ITTC-57" : "Holtrop-Mennen"}
              </p>
              <p>
                <strong>Speed Grid:</strong> {props.selectedSpeedGrid?.name || "None"}
              </p>
              <p>
                <strong>Form Factor:</strong> {props.formFactor ?? "Default"}{" "}
                {props.applyFormFactor ? "(Applied)" : "(Not applied)"}
              </p>
              <p>
                <strong>Temperature:</strong> {props.tempC}°C
              </p>
              <p>
                <strong>Salinity:</strong> {props.salinityPpt} ppt
              </p>
              {props.calculationType === "holtrop-mennen" && (
                <>
                  <p>
                    <strong>Service Margin:</strong> {props.serviceMargin}%
                  </p>
                  <p>
                    <strong>ηD:</strong> {props.etaD ?? "Default"}
                  </p>
                </>
              )}
            </div>
          </PanelWrapper>
        </div>

        {/* ITTC-57 Results */}
        {props.ittc57Result && !getPanelState("resistance-ittc-results").hidden && (
          <div key="resistance-ittc-results">
            <PanelWrapper
              panelId="resistance-ittc-results"
              title="ITTC-57 Friction Results"
              panelState={getPanelState("resistance-ittc-results")}
              onToggleCollapse={() => onTogglePanelCollapsed("resistance-ittc-results")}
              onToggleFullscreen={() => onSetPanelFullscreen("resistance-ittc-results", true)}
            >
              <Ittc57ResultsPanel result={props.ittc57Result} />
            </PanelWrapper>
          </div>
        )}

        {/* Holtrop-Mennen Results */}
        {props.hmResult && !getPanelState("resistance-hm-results").hidden && (
          <div key="resistance-hm-results">
            <PanelWrapper
              panelId="resistance-hm-results"
              title="Holtrop-Mennen Resistance Results"
              panelState={getPanelState("resistance-hm-results")}
              onToggleCollapse={() => onTogglePanelCollapsed("resistance-hm-results")}
              onToggleFullscreen={() => onSetPanelFullscreen("resistance-hm-results", true)}
            >
              <HoltropMennenResultsPanel result={props.hmResult} />
            </PanelWrapper>
          </div>
        )}

        {/* Power Results */}
        {props.powerResult && !getPanelState("resistance-power-results").hidden && (
          <div key="resistance-power-results">
            <PanelWrapper
              panelId="resistance-power-results"
              title="Power Curves"
              panelState={getPanelState("resistance-power-results")}
              onToggleCollapse={() => onTogglePanelCollapsed("resistance-power-results")}
              onToggleFullscreen={() => onSetPanelFullscreen("resistance-power-results", true)}
            >
              <PowerResultsPanel result={props.powerResult} />
            </PanelWrapper>
          </div>
        )}

        {/* Charts */}
        {(props.ittc57Result || props.hmResult) && !getPanelState("resistance-charts").hidden && (
          <div key="resistance-charts">
            <PanelWrapper
              panelId="resistance-charts"
              title="Resistance & Power Charts"
              panelState={getPanelState("resistance-charts")}
              onToggleCollapse={() => onTogglePanelCollapsed("resistance-charts")}
              onToggleFullscreen={() => onSetPanelFullscreen("resistance-charts", true)}
            >
              <ResistanceChartsPanel
                ittc57Result={props.ittc57Result}
                hmResult={props.hmResult}
                powerResult={props.powerResult}
                kcsBenchmarkResult={props.kcsBenchmarkResult}
              />
            </PanelWrapper>
          </div>
        )}
      </ResponsiveGridLayout>
    </div>
  );
}
