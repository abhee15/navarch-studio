import { useState, useEffect, useRef, useCallback } from "react";
import { observer } from "mobx-react-lite";
import { projectionsApi } from "../../../../services/hydrostaticsApi";
import type { WaterlineCurve, ButtockCurve } from "../../../../types/hydrostatics";
import { settingsStore } from "../../../../stores/SettingsStore";
import { getErrorMessage } from "../../../../types/errors";
import { getUnitSymbol } from "../../../../utils/unitSymbols";

interface WaterlinesButtocksPanelProps {
  vesselId: string;
  stationsCount?: number;
  waterlinesCount?: number;
  offsetsCount?: number;
}

type ViewMode = "waterlines" | "buttocks" | "combined";

const COLORS = [
  "#3B82F6", // Blue
  "#10B981", // Green
  "#F59E0B", // Amber
  "#EF4444", // Red
  "#8B5CF6", // Purple
  "#EC4899", // Pink
  "#14B8A6", // Teal
  "#F97316", // Orange
  "#6366F1", // Indigo
  "#84CC16", // Lime
];

export const WaterlinesButtocksPanel = observer(
  ({ vesselId, stationsCount, waterlinesCount, offsetsCount }: WaterlinesButtocksPanelProps) => {
    const svgRef = useRef<SVGSVGElement>(null);
    const [waterlines, setWaterlines] = useState<WaterlineCurve[]>([]);
    const [buttocks, setButtocks] = useState<ButtockCurve[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [viewMode, setViewMode] = useState<ViewMode>("waterlines");
    const [selectedWaterlines, setSelectedWaterlines] = useState<number[]>([]);
    const [selectedButtocks, setSelectedButtocks] = useState<number[]>([]);
    const [showMirrored, setShowMirrored] = useState(true);

    const displayUnits = settingsStore.preferredUnits;
    const lengthUnit = getUnitSymbol(displayUnits, "Length");

    useEffect(() => {
      const hasGeometry =
        (stationsCount ?? 0) > 0 && (waterlinesCount ?? 0) > 0 && (offsetsCount ?? 0) > 0;
      if (hasGeometry) {
        loadProjections();
      } else {
        setWaterlines([]);
        setButtocks([]);
        setError(null);
      }
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [vesselId, stationsCount, waterlinesCount, offsetsCount]);

    const loadProjections = async () => {
      try {
        setLoading(true);
        setError(null);

        // Load both waterlines and buttocks
        const [waterlinesData, buttocksData] = await Promise.all([
          projectionsApi.getWaterlines(vesselId),
          projectionsApi.getButtocks(vesselId, 5),
        ]);

        setWaterlines(waterlinesData.waterlines);
        setButtocks(buttocksData.buttocks);

        // Select first 3 of each by default
        if (waterlinesData.waterlines.length > 0) {
          const wlToSelect = waterlinesData.waterlines.slice(
            0,
            Math.min(3, waterlinesData.waterlines.length)
          );
          setSelectedWaterlines(wlToSelect.map((wl) => wl.waterlineIndex));
        }

        if (buttocksData.buttocks.length > 0) {
          const btToSelect = buttocksData.buttocks.slice(
            0,
            Math.min(3, buttocksData.buttocks.length)
          );
          setSelectedButtocks(btToSelect.map((bt) => bt.buttockIndex));
        }
      } catch (err) {
        setError(getErrorMessage(err));
      } finally {
        setLoading(false);
      }
    };

    const handleWaterlineToggle = (wlIndex: number) => {
      if (selectedWaterlines.includes(wlIndex)) {
        setSelectedWaterlines(selectedWaterlines.filter((i) => i !== wlIndex));
      } else {
        setSelectedWaterlines([...selectedWaterlines, wlIndex]);
      }
    };

    const handleButtockToggle = (btIndex: number) => {
      if (selectedButtocks.includes(btIndex)) {
        setSelectedButtocks(selectedButtocks.filter((i) => i !== btIndex));
      } else {
        setSelectedButtocks([...selectedButtocks, btIndex]);
      }
    };

    const getColor = (index: number) => {
      return COLORS[index % COLORS.length];
    };

    // SVG dimensions
    const viewBoxWidth = 800;
    const viewBoxHeight = 600;
    const margin = { top: 60, right: 120, bottom: 60, left: 60 };
    const drawWidth = viewBoxWidth - margin.left - margin.right;
    const drawHeight = viewBoxHeight - margin.top - margin.bottom;

    // Calculate bounds based on view mode
    const getBounds = () => {
      let minX = Infinity,
        maxX = -Infinity,
        minY = Infinity,
        maxY = -Infinity;

      if (viewMode === "waterlines" || viewMode === "combined") {
        waterlines
          .filter((wl) => selectedWaterlines.includes(wl.waterlineIndex))
          .forEach((wl) => {
            wl.points.forEach((p) => {
              minX = Math.min(minX, p.x);
              maxX = Math.max(maxX, p.x);
              minY = Math.min(minY, -Math.abs(p.y), Math.abs(p.y));
              maxY = Math.max(maxY, -Math.abs(p.y), Math.abs(p.y));
            });
          });
      }

      if (viewMode === "buttocks" || viewMode === "combined") {
        buttocks
          .filter((bt) => selectedButtocks.includes(bt.buttockIndex))
          .forEach((bt) => {
            bt.points.forEach((p) => {
              minX = Math.min(minX, p.x);
              maxX = Math.max(maxX, p.x);
              minY = Math.min(minY, p.z);
              maxY = Math.max(maxY, p.z);
            });
          });
      }

      // Add padding
      const padX = (maxX - minX) * 0.1;
      const padY = (maxY - minY) * 0.1;

      return {
        minX: minX - padX,
        maxX: maxX + padX,
        minY: minY - padY,
        maxY: maxY + padY,
      };
    };

    const bounds = getBounds();
    const rangeX = bounds.maxX - bounds.minX || 1;
    const rangeY = bounds.maxY - bounds.minY || 1;

    const toSvgX = (x: number) => margin.left + ((x - bounds.minX) / rangeX) * drawWidth;
    const toSvgY = (y: number) =>
      margin.top + drawHeight - ((y - bounds.minY) / rangeY) * drawHeight;

    const generateWaterlinePath = (wl: WaterlineCurve, side: "port" | "starboard") => {
      if (wl.points.length === 0) return "";

      const points = wl.points.map((p) => ({
        x: p.x,
        y: side === "port" ? -Math.abs(p.y) : Math.abs(p.y),
      }));

      let path = `M ${toSvgX(points[0].x)} ${toSvgY(points[0].y)}`;
      for (let i = 1; i < points.length; i++) {
        path += ` L ${toSvgX(points[i].x)} ${toSvgY(points[i].y)}`;
      }
      return path;
    };

    const generateButtockPath = (bt: ButtockCurve) => {
      if (bt.points.length === 0) return "";

      let path = `M ${toSvgX(bt.points[0].x)} ${toSvgY(bt.points[0].z)}`;
      for (let i = 1; i < bt.points.length; i++) {
        path += ` L ${toSvgX(bt.points[i].x)} ${toSvgY(bt.points[i].z)}`;
      }
      return path;
    };

    // Export as SVG
    const exportAsSVG = useCallback(() => {
      if (!svgRef.current) return;

      const svgElement = svgRef.current.cloneNode(true) as SVGElement;
      const serializer = new XMLSerializer();
      const svgString = serializer.serializeToString(svgElement);

      const blob = new Blob([svgString], { type: "image/svg+xml" });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `waterlines_buttocks_${Date.now()}.svg`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    }, []);

    // Export as CSV
    const exportAsCSV = useCallback(() => {
      let csvContent = "";

      if (viewMode === "waterlines" || viewMode === "combined") {
        csvContent += "Waterlines\n";
        csvContent += "WaterlineIndex,Z,X,Y\n";
        waterlines
          .filter((wl) => selectedWaterlines.includes(wl.waterlineIndex))
          .forEach((wl) => {
            wl.points.forEach((p) => {
              csvContent += `${wl.waterlineIndex},${wl.z},${p.x},${p.y}\n`;
            });
          });
        csvContent += "\n";
      }

      if (viewMode === "buttocks" || viewMode === "combined") {
        csvContent += "Buttocks\n";
        csvContent += "ButtockIndex,Y,X,Z\n";
        buttocks
          .filter((bt) => selectedButtocks.includes(bt.buttockIndex))
          .forEach((bt) => {
            bt.points.forEach((p) => {
              csvContent += `${bt.buttockIndex},${bt.y},${p.x},${p.z}\n`;
            });
          });
      }

      const blob = new Blob([csvContent], { type: "text/csv" });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `waterlines_buttocks_${Date.now()}.csv`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    }, [viewMode, waterlines, selectedWaterlines, buttocks, selectedButtocks]);

    if (loading) {
      return (
        <div className="flex items-center justify-center h-full">
          <div className="text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-2"></div>
            <p className="text-xs text-muted-foreground">Loading projections...</p>
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
            <h4 className="text-xs font-medium text-foreground mb-1">Error Loading Projections</h4>
            <p className="text-[10px] text-muted-foreground mb-2">{error}</p>
            <button
              onClick={loadProjections}
              className="text-[10px] px-2 py-1 bg-primary text-primary-foreground rounded hover:bg-primary/90"
            >
              Retry
            </button>
          </div>
        </div>
      );
    }

    if ((stationsCount ?? 0) === 0 || (waterlinesCount ?? 0) === 0 || (offsetsCount ?? 0) === 0) {
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
              Import stations, waterlines, and offsets to view projections.
            </p>
          </div>
        </div>
      );
    }

    return (
      <div className="h-full flex flex-col">
        {/* Controls */}
        <div className="flex-shrink-0 mb-2 space-y-2">
          {/* View Mode Selector */}
          <div className="flex items-center gap-2">
            <label className="text-[10px] font-medium text-foreground">View:</label>
            <div className="flex gap-1">
              <button
                onClick={() => setViewMode("waterlines")}
                className={`px-2 py-1 rounded text-[10px] font-medium transition-colors ${
                  viewMode === "waterlines"
                    ? "bg-primary text-primary-foreground"
                    : "bg-muted text-muted-foreground hover:bg-muted/80"
                }`}
              >
                Waterlines
              </button>
              <button
                onClick={() => setViewMode("buttocks")}
                className={`px-2 py-1 rounded text-[10px] font-medium transition-colors ${
                  viewMode === "buttocks"
                    ? "bg-primary text-primary-foreground"
                    : "bg-muted text-muted-foreground hover:bg-muted/80"
                }`}
              >
                Buttocks
              </button>
              <button
                onClick={() => setViewMode("combined")}
                className={`px-2 py-1 rounded text-[10px] font-medium transition-colors ${
                  viewMode === "combined"
                    ? "bg-primary text-primary-foreground"
                    : "bg-muted text-muted-foreground hover:bg-muted/80"
                }`}
              >
                Combined
              </button>
            </div>

            {viewMode === "waterlines" && (
              <label className="flex items-center ml-4">
                <input
                  type="checkbox"
                  checked={showMirrored}
                  onChange={(e) => setShowMirrored(e.target.checked)}
                  className="w-3 h-3 rounded"
                />
                <span className="ml-1 text-[10px] text-muted-foreground">Mirror</span>
              </label>
            )}

            <div className="ml-auto flex gap-1">
              <button
                onClick={exportAsSVG}
                className="px-2 py-1 text-[10px] bg-muted text-muted-foreground rounded hover:bg-muted/80"
                title="Export as SVG"
              >
                SVG
              </button>
              <button
                onClick={exportAsCSV}
                className="px-2 py-1 text-[10px] bg-muted text-muted-foreground rounded hover:bg-muted/80"
                title="Export as CSV"
              >
                CSV
              </button>
            </div>
          </div>

          {/* Waterlines Selection */}
          {(viewMode === "waterlines" || viewMode === "combined") && (
            <div>
              <label className="text-[10px] font-medium text-foreground">Select Waterlines:</label>
              <div className="flex flex-wrap gap-1 mt-1">
                {waterlines.slice(0, 10).map((wl) => (
                  <button
                    key={wl.waterlineIndex}
                    onClick={() => handleWaterlineToggle(wl.waterlineIndex)}
                    className={`px-2 py-1 rounded text-[10px] font-medium transition-colors ${
                      selectedWaterlines.includes(wl.waterlineIndex)
                        ? "bg-primary text-primary-foreground"
                        : "bg-muted text-muted-foreground hover:bg-muted/80"
                    }`}
                    title={`Waterline ${wl.waterlineIndex} at z = ${wl.z.toFixed(2)}${lengthUnit}`}
                  >
                    WL {wl.waterlineIndex}
                  </button>
                ))}
              </div>
            </div>
          )}

          {/* Buttocks Selection */}
          {(viewMode === "buttocks" || viewMode === "combined") && (
            <div>
              <label className="text-[10px] font-medium text-foreground">Select Buttocks:</label>
              <div className="flex flex-wrap gap-1 mt-1">
                {buttocks.map((bt) => (
                  <button
                    key={bt.buttockIndex}
                    onClick={() => handleButtockToggle(bt.buttockIndex)}
                    className={`px-2 py-1 rounded text-[10px] font-medium transition-colors ${
                      selectedButtocks.includes(bt.buttockIndex)
                        ? "bg-primary text-primary-foreground"
                        : "bg-muted text-muted-foreground hover:bg-muted/80"
                    }`}
                    title={`Buttock ${bt.buttockIndex} at y = ${bt.y.toFixed(2)}${lengthUnit}`}
                  >
                    BT {bt.buttockIndex}
                  </button>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* SVG Canvas */}
        <div className="flex-1 min-h-0 relative">
          <div className="absolute inset-0">
            <svg
              ref={svgRef}
              viewBox={`0 0 ${viewBoxWidth} ${viewBoxHeight}`}
              className="w-full h-full"
            >
              {/* Title */}
              <text
                x={viewBoxWidth / 2}
                y={30}
                textAnchor="middle"
                className="text-[12px] font-semibold fill-foreground"
              >
                {viewMode === "waterlines" && "Waterlines Plan (XY Projection)"}
                {viewMode === "buttocks" && "Buttocks Plan (XZ Projection)"}
                {viewMode === "combined" && "Waterlines & Buttocks Plan"}
              </text>

              {/* Axes */}
              <line
                x1={margin.left}
                y1={margin.top + drawHeight}
                x2={margin.left + drawWidth}
                y2={margin.top + drawHeight}
                className="stroke-muted-foreground"
                strokeWidth="2"
              />
              <line
                x1={margin.left}
                y1={margin.top}
                x2={margin.left}
                y2={margin.top + drawHeight}
                className="stroke-muted-foreground"
                strokeWidth="2"
              />

              {/* Waterlines */}
              {(viewMode === "waterlines" || viewMode === "combined") &&
                waterlines
                  .filter((wl) => selectedWaterlines.includes(wl.waterlineIndex))
                  .map((wl) => (
                    <g key={`wl-${wl.waterlineIndex}`}>
                      {/* Starboard */}
                      <path
                        d={generateWaterlinePath(wl, "starboard")}
                        stroke={getColor(wl.waterlineIndex)}
                        strokeWidth={1.5}
                        fill="none"
                        opacity={0.8}
                      />
                      {/* Port (mirrored) */}
                      {showMirrored && (
                        <path
                          d={generateWaterlinePath(wl, "port")}
                          stroke={getColor(wl.waterlineIndex)}
                          strokeWidth={1.5}
                          fill="none"
                          opacity={0.8}
                          strokeDasharray="4,4"
                        />
                      )}
                    </g>
                  ))}

              {/* Buttocks */}
              {(viewMode === "buttocks" || viewMode === "combined") &&
                buttocks
                  .filter((bt) => selectedButtocks.includes(bt.buttockIndex))
                  .map((bt) => (
                    <path
                      key={`bt-${bt.buttockIndex}`}
                      d={generateButtockPath(bt)}
                      stroke={getColor(bt.buttockIndex)}
                      strokeWidth={1.5}
                      fill="none"
                      opacity={0.8}
                    />
                  ))}

              {/* Axis Labels */}
              <text
                x={margin.left + drawWidth / 2}
                y={viewBoxHeight - 20}
                textAnchor="middle"
                className="text-[10px] fill-muted-foreground"
              >
                X - Longitudinal ({lengthUnit})
              </text>
              <text
                x={20}
                y={margin.top + drawHeight / 2}
                textAnchor="middle"
                className="text-[10px] fill-muted-foreground"
                transform={`rotate(-90 20 ${margin.top + drawHeight / 2})`}
              >
                {viewMode === "waterlines"
                  ? `Y - Transverse (${lengthUnit})`
                  : `Z - Vertical (${lengthUnit})`}
              </text>
            </svg>
          </div>
        </div>

        {/* Info */}
        <div className="flex-shrink-0 mt-2 pt-2 border-t border-border">
          <p className="text-[10px] text-muted-foreground">
            <strong>Waterlines:</strong> XY projections at constant Z-levels (horizontal slices).{" "}
            <strong>Buttocks:</strong> XZ projections at constant Y-offsets (longitudinal vertical
            slices).
          </p>
        </div>
      </div>
    );
  }
);
