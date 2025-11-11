import { useState, useEffect, useCallback, useRef } from "react";
import { observer } from "mobx-react-lite";
import { LinesPlanTitleBlock } from "../../LinesPlanTitleBlock";
import { LinesPlanGrid } from "../../LinesPlanGrid";
import { DiagonalsView } from "../../DiagonalsView";
import { SectionAreaCurveComponent } from "../../SectionAreaCurve";
import { OffsetsTableView } from "../../OffsetsTableView";
import { LinesPlanExportDialog } from "../../LinesPlanExportDialog";
import { BodyPlanViewer } from "../../BodyPlanViewer";
import type { VesselDetails } from "../../../../types/hydrostatics";
import type { BodyPlanData } from "../../../../types/bodyplan";
import type {
  DiagonalsData,
  SectionAreaCurve,
  FairingQuality,
  LinesPlanVisibility,
  LinesPlanExportOptions,
} from "../../../../types/linesplan";
import {
  geometryApi,
  projectionsApi,
  curvesApi,
  exportApi,
} from "../../../../services/hydrostaticsApi";
import { getErrorMessage } from "../../../../types/errors";

interface LinesPlanPanelProps {
  vesselId: string;
  vessel: VesselDetails;
}

export const LinesPlanPanel = observer(({ vesselId, vessel }: LinesPlanPanelProps) => {
  // Data state
  const [bodyPlanData, setBodyPlanData] = useState<BodyPlanData | null>(null);
  const [diagonals, setDiagonals] = useState<DiagonalsData | null>(null);
  const [sectionAreaCurve, setSectionAreaCurve] = useState<SectionAreaCurve | null>(null);
  const [fairingQuality, setFairingQuality] = useState<FairingQuality | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // UI state
  const [showExportDialog, setShowExportDialog] = useState(false);
  const [visibility, setVisibility] = useState<LinesPlanVisibility>({
    bodyPlan: true,
    waterlines: true,
    buttocks: true,
    diagonals: true,
    sectionAreaCurve: true,
    grid: true,
    titleBlock: true,
    offsetsTable: true,
  });

  const svgRef = useRef<SVGSVGElement>(null);

  // Load all data in parallel
  useEffect(() => {
    loadLinesPlanData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [vesselId]);

  const loadLinesPlanData = async () => {
    try {
      setLoading(true);
      setError(null);

      const [bodyPlan, diag, sac, fq] = await Promise.all([
        geometryApi.getOffsetsGrid(vesselId),
        projectionsApi.getDiagonals(vesselId, 3),
        curvesApi.getSectionAreaCurve(vesselId),
        curvesApi.getFairingQuality(vesselId),
      ]);

      // Transform offsets grid to body plan data
      const bodyPlanDataTransformed: BodyPlanData = {
        stations: bodyPlan.stations,
        waterlines: bodyPlan.waterlines,
        offsets: bodyPlan.offsets,
      };

      setBodyPlanData(bodyPlanDataTransformed);
      setDiagonals(diag);
      setSectionAreaCurve(sac);
      setFairingQuality(fq);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  // Export handler
  const handleExport = useCallback(
    async (options: LinesPlanExportOptions) => {
      try {
        // Server-side PDF export
        const blob = await exportApi.exportLinesPlanPdf(vesselId, options);

        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = `lines_plan_${vessel.name.replace(/\s+/g, "_")}_${Date.now()}.pdf`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
      } catch (err) {
        console.error("Export failed:", err);
        setError(getErrorMessage(err));
      }
    },
    [vesselId, vessel.name, setError]
  );

  // Calculate bounds for scaling
  const minX = bodyPlanData ? Math.min(...bodyPlanData.stations) : 0;
  const maxX = bodyPlanData ? Math.max(...bodyPlanData.stations) : 100;
  const minZ = bodyPlanData ? Math.min(...bodyPlanData.waterlines, 0) : 0;
  const maxZ = bodyPlanData ? Math.max(...bodyPlanData.waterlines) : 10;
  const maxY = vessel.beam / 2; // Half-breadth

  if (loading) {
    return (
      <div className="flex items-center justify-center h-full">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-2"></div>
          <p className="text-xs text-muted-foreground">Loading lines plan...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center justify-center h-full">
        <div className="text-center max-w-sm">
          <svg
            className="mx-auto h-8 w-8 text-destructive mb-2"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
            />
          </svg>
          <h4 className="text-xs font-medium text-foreground mb-1">Error Loading Lines Plan</h4>
          <p className="text-[10px] text-muted-foreground mb-2">{error}</p>
          <button
            onClick={loadLinesPlanData}
            className="text-[10px] px-2 py-1 bg-primary text-primary-foreground rounded hover:bg-primary/90"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  if (!bodyPlanData || bodyPlanData.stations.length === 0) {
    return (
      <div className="flex items-center justify-center h-full">
        <div className="text-center">
          <svg
            className="mx-auto h-8 w-8 text-muted-foreground mb-2"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M8 10h.01M12 10h.01M16 10h.01M9 16h6m2 5H7a2 2 0 01-2-2V7a2 2 0 012-2h3l2-2h2l2 2h3a2 2 0 012 2v12a2 2 0 01-2 2z"
            />
          </svg>
          <h4 className="text-xs font-medium text-foreground mb-1">Geometry Required</h4>
          <p className="text-[10px] text-muted-foreground">
            Import stations, waterlines, and offsets to view lines plan.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full bg-background">
      {/* Toolbar */}
      <div className="flex-shrink-0 border-b border-border p-2 space-y-2">
        {/* Top row: Title, Stats, Quality, Export */}
        <div className="flex items-center justify-between gap-2">
          <div className="flex items-center gap-2 min-w-0">
            <h3 className="text-sm font-medium text-foreground whitespace-nowrap">Lines Plan</h3>
            <span className="hidden sm:inline text-xs text-muted-foreground whitespace-nowrap">
              {bodyPlanData.stations.length} × {bodyPlanData.waterlines.length}
            </span>
            {fairingQuality && (
              <span
                className={`text-[10px] sm:text-xs px-1.5 py-0.5 rounded whitespace-nowrap ${
                  fairingQuality.overallScore >= 80
                    ? "bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400"
                    : fairingQuality.overallScore >= 60
                      ? "bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400"
                      : "bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400"
                }`}
              >
                {fairingQuality.overallScore.toFixed(0)}%
              </span>
            )}
          </div>

          {/* Export button */}
          <button
            onClick={() => setShowExportDialog(true)}
            className="flex items-center gap-1 px-2 py-1 text-xs bg-primary text-primary-foreground rounded hover:bg-primary/90 transition-colors whitespace-nowrap"
            title="Export Lines Plan"
          >
            <svg className="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"
              />
            </svg>
            <span className="hidden sm:inline">Export</span>
          </button>
        </div>

        {/* Bottom row: Visibility toggles as compact icon buttons */}
        <div className="flex items-center gap-1 flex-wrap">
          {Object.entries(visibility).map(([key, value]) => {
            const icons: Record<string, { path: string; label: string }> = {
              bodyPlan: {
                path: "M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2",
                label: "Body",
              },
              waterlines: {
                path: "M4 8h16M4 16h16",
                label: "WL",
              },
              buttocks: {
                path: "M8 4v16M16 4v16",
                label: "BL",
              },
              diagonals: {
                path: "M4 20L20 4M4 4l16 16",
                label: "Diag",
              },
              sectionAreaCurve: {
                path: "M7 21a4 4 0 01-4-4V5a2 2 0 012-2h4a2 2 0 012 2v12a4 4 0 01-4 4zm0 0h12a2 2 0 002-2v-4a2 2 0 00-2-2h-2.343M11 7.343l1.657-1.657a2 2 0 012.828 0l2.829 2.829a2 2 0 010 2.828l-8.486 8.485M7 17h.01",
                label: "SAC",
              },
              grid: {
                path: "M4 5a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM14 5a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1V5zM4 15a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1v-4zM14 15a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1v-4z",
                label: "Grid",
              },
              titleBlock: {
                path: "M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z",
                label: "Title",
              },
              offsetsTable: {
                path: "M3 10h18M3 14h18m-9-4v8m-7 0h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z",
                label: "Table",
              },
            };

            const icon = icons[key];
            return (
              <button
                key={key}
                onClick={() =>
                  setVisibility({ ...visibility, [key as keyof LinesPlanVisibility]: !value })
                }
                className={`flex items-center gap-1 px-1.5 sm:px-2 py-1 rounded text-[10px] sm:text-xs transition-colors ${
                  value
                    ? "bg-primary/10 text-primary hover:bg-primary/20"
                    : "bg-muted text-muted-foreground hover:bg-muted/80"
                }`}
                title={`Toggle ${icon?.label || key}`}
              >
                <svg
                  className="w-3 h-3 sm:w-3.5 sm:h-3.5 flex-shrink-0"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d={icon?.path || ""}
                  />
                </svg>
                <span className="hidden md:inline">{icon?.label || key}</span>
              </button>
            );
          })}
        </div>
      </div>

      {/* Lines Plan Canvas */}
      <div className="flex-1 overflow-auto p-4 bg-gradient-to-b from-background to-muted/20">
        <svg
          ref={svgRef}
          viewBox="0 0 1200 900"
          className="w-full h-full"
          style={{ minHeight: "800px" }}
        >
          {/* Title Block */}
          {visibility.titleBlock && (
            <LinesPlanTitleBlock
              vessel={vessel}
              x={0}
              y={0}
              width={1200}
              height={80}
              scale="1:100"
            />
          )}

          {/* Main Layout - Traditional 3-view */}
          <g transform="translate(0, 100)">
            {/* Body Plan (Left) */}
            <g transform="translate(50, 0)">
              {visibility.grid && (
                <LinesPlanGrid
                  type="body-plan"
                  stations={bodyPlanData.stations}
                  waterlines={bodyPlanData.waterlines}
                  width={400}
                  height={300}
                  lpp={vessel.lpp}
                />
              )}
              {visibility.bodyPlan && (
                <BodyPlanViewer data={bodyPlanData} lpp={vessel.lpp} height={300} />
              )}
            </g>

            {/* Profile Plan (Top Right) */}
            <g transform="translate(500, 0)">
              {visibility.grid && (
                <LinesPlanGrid
                  type="profile"
                  stations={bodyPlanData.stations}
                  waterlines={bodyPlanData.waterlines}
                  width={650}
                  height={300}
                  lpp={vessel.lpp}
                />
              )}
              {/* Waterlines would be rendered here as profile curves */}
              {/* Buttocks would be rendered here */}
              {visibility.diagonals && diagonals && (
                <DiagonalsView
                  diagonals={diagonals.diagonals}
                  view="profile"
                  width={650}
                  height={300}
                  minX={minX}
                  maxX={maxX}
                  minZ={minZ}
                  maxZ={maxZ}
                  maxY={maxY}
                />
              )}
              {visibility.sectionAreaCurve && sectionAreaCurve && (
                <SectionAreaCurveComponent
                  data={sectionAreaCurve}
                  width={650}
                  height={300}
                  minX={minX}
                  maxX={maxX}
                />
              )}
            </g>

            {/* Half-Breadth Plan (Bottom Right) */}
            <g transform="translate(500, 350)">
              {visibility.grid && (
                <LinesPlanGrid
                  type="half-breadth"
                  stations={bodyPlanData.stations}
                  waterlines={bodyPlanData.waterlines}
                  width={650}
                  height={300}
                  lpp={vessel.lpp}
                />
              )}
              {/* Waterlines in plan view would be rendered here */}
              {visibility.diagonals && diagonals && (
                <DiagonalsView
                  diagonals={diagonals.diagonals}
                  view="plan"
                  width={650}
                  height={300}
                  minX={minX}
                  maxX={maxX}
                  minZ={minZ}
                  maxZ={maxZ}
                  maxY={maxY}
                />
              )}
            </g>

            {/* Offsets Table (Bottom Left) */}
            {visibility.offsetsTable && (
              <g transform="translate(50, 350)">
                <OffsetsTableView
                  stations={bodyPlanData.stations}
                  waterlines={bodyPlanData.waterlines}
                  width={400}
                  height={300}
                />
              </g>
            )}
          </g>
        </svg>
      </div>

      {/* Export Dialog */}
      {showExportDialog && (
        <LinesPlanExportDialog
          vesselId={vesselId}
          vessel={vessel}
          onExport={handleExport}
          onClose={() => setShowExportDialog(false)}
        />
      )}
    </div>
  );
});

LinesPlanPanel.displayName = "LinesPlanPanel";
