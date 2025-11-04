import { useRef } from "react";
import { observer } from "mobx-react-lite";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  ReferenceLine,
} from "recharts";
import { Download } from "lucide-react";
import type { HydroResult, VesselDetails } from "../../../../types/hydrostatics";
import { settingsStore } from "../../../../stores/SettingsStore";
import { getUnitSymbol } from "../../../../utils/unitSymbols";

interface CoefficientsEvolutionPanelProps {
  vessel?: VesselDetails | null;
  results: HydroResult[];
  onDraftHover?: (draft: number | null) => void;
}

// Typical coefficient ranges by vessel type (for reference bands)
const TYPICAL_RANGES = {
  Boat: { cb: [0.35, 0.5], cp: [0.6, 0.75], cm: [0.75, 0.85], cwp: [0.7, 0.85] },
  Yacht: { cb: [0.3, 0.45], cp: [0.55, 0.7], cm: [0.7, 0.8], cwp: [0.65, 0.8] },
  Ship: { cb: [0.55, 0.85], cp: [0.7, 0.85], cm: [0.85, 0.98], cwp: [0.75, 0.9] },
};

export const CoefficientsEvolutionPanel = observer(
  ({ vessel, results, onDraftHover }: CoefficientsEvolutionPanelProps) => {
    const chartRef = useRef<HTMLDivElement>(null);
    const displayUnits = settingsStore.preferredUnits;
    const lengthUnit = getUnitSymbol(displayUnits, "Length");

    // Transform results into chart data
    const chartData = results
      .map((result) => ({
        draft: result.draft,
        CB: result.cb,
        CP: result.cp,
        CM: result.cm,
        CWP: result.cwp,
      }))
      .sort((a, b) => a.draft - b.draft);

    const designDraft = vessel?.designDraft;

    const formatNumber = (value: number): string => {
      if (value === null || value === undefined) return "N/A";
      return value.toFixed(3);
    };

    // Export chart as PNG
    const exportAsPNG = () => {
      if (!chartRef.current) return;

      // Use html2canvas or similar library
      // For now, we'll use a simple approach with the SVG
      const svgElement = chartRef.current.querySelector("svg");
      if (!svgElement) return;

      const svgData = new XMLSerializer().serializeToString(svgElement);
      const canvas = document.createElement("canvas");
      const ctx = canvas.getContext("2d");
      if (!ctx) return;

      const img = new Image();
      const svgBlob = new Blob([svgData], { type: "image/svg+xml;charset=utf-8" });
      const url = URL.createObjectURL(svgBlob);

      img.onload = () => {
        canvas.width = img.width;
        canvas.height = img.height;
        ctx.drawImage(img, 0, 0);
        URL.revokeObjectURL(url);

        canvas.toBlob((blob) => {
          if (blob) {
            const link = document.createElement("a");
            link.download = `coefficients_evolution_${Date.now()}.png`;
            link.href = URL.createObjectURL(blob);
            link.click();
          }
        });
      };

      img.src = url;
    };

    // Export chart as SVG
    const exportAsSVG = () => {
      if (!chartRef.current) return;

      const svgElement = chartRef.current.querySelector("svg");
      if (!svgElement) return;

      const svgData = new XMLSerializer().serializeToString(svgElement);
      const svgBlob = new Blob([svgData], { type: "image/svg+xml;charset=utf-8" });
      const url = URL.createObjectURL(svgBlob);

      const link = document.createElement("a");
      link.download = `coefficients_evolution_${Date.now()}.svg`;
      link.href = url;
      link.click();

      URL.revokeObjectURL(url);
    };

    if (results.length === 0) {
      return (
        <div className="flex items-center justify-center h-full">
          <div className="text-center">
            <svg
              className="mx-auto h-12 w-12 text-muted-foreground"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
              />
            </svg>
            <h3 className="mt-2 text-sm font-medium text-foreground">No Data</h3>
            <p className="mt-1 text-xs text-muted-foreground">
              Compute hydrostatics to see coefficients evolution
            </p>
          </div>
        </div>
      );
    }

    return (
      <div className="h-full flex flex-col">
        {/* Header with export buttons */}
        <div className="flex items-center justify-between mb-3 flex-shrink-0">
          <div>
            <h4 className="text-sm font-medium text-foreground">Coefficients Evolution</h4>
            <p className="text-xs text-muted-foreground mt-0.5">
              Form coefficients (CB, CP, CM, CWP) vs Draft
            </p>
          </div>
          <div className="flex gap-2">
            <button
              onClick={exportAsSVG}
              className="flex items-center gap-1 px-2 py-1 text-xs bg-background border border-border rounded hover:bg-accent transition-colors"
              title="Export as SVG"
            >
              <Download className="w-3 h-3" />
              SVG
            </button>
            <button
              onClick={exportAsPNG}
              className="flex items-center gap-1 px-2 py-1 text-xs bg-background border border-border rounded hover:bg-accent transition-colors"
              title="Export as PNG"
            >
              <Download className="w-3 h-3" />
              PNG
            </button>
          </div>
        </div>

        {/* Chart */}
        <div className="flex-1 min-h-0" ref={chartRef}>
          <ResponsiveContainer width="100%" height="100%">
            <LineChart
              data={chartData}
              onMouseMove={(e) => {
                if (
                  e &&
                  "activePayload" in e &&
                  e.activePayload &&
                  Array.isArray(e.activePayload) &&
                  e.activePayload.length > 0 &&
                  onDraftHover
                ) {
                  const firstPayload = e.activePayload[0] as
                    | { payload: { draft: number } }
                    | undefined;
                  if (
                    firstPayload &&
                    "payload" in firstPayload &&
                    firstPayload.payload &&
                    "draft" in firstPayload.payload
                  ) {
                    onDraftHover(firstPayload.payload.draft);
                  }
                }
              }}
              onMouseLeave={() => onDraftHover && onDraftHover(null)}
              margin={{ top: 10, right: 30, left: 10, bottom: 20 }}
            >
              <CartesianGrid
                strokeDasharray="3 3"
                stroke="#e5e7eb"
                className="dark:stroke-gray-700"
              />
              <XAxis
                dataKey="draft"
                label={{
                  value: `Draft (${lengthUnit})`,
                  position: "insideBottom",
                  offset: -10,
                  style: { fontSize: "11px", fill: "currentColor" },
                }}
                tick={{ fontSize: 10 }}
                stroke="currentColor"
                className="text-muted-foreground"
              />
              <YAxis
                domain={[0, 1]}
                label={{
                  value: "Coefficient Value",
                  angle: -90,
                  position: "insideLeft",
                  style: { fontSize: "10px", fill: "currentColor" },
                }}
                tick={{ fontSize: 10 }}
                stroke="currentColor"
                className="text-muted-foreground"
              />
              <Tooltip
                contentStyle={{
                  fontSize: "11px",
                  padding: "8px",
                  backgroundColor: "rgba(255, 255, 255, 0.95)",
                  border: "1px solid #e5e7eb",
                  borderRadius: "4px",
                }}
                formatter={(value: number, name: string) => [formatNumber(value), name]}
                labelFormatter={(label) => `Draft: ${formatNumber(Number(label))} ${lengthUnit}`}
              />
              <Legend
                wrapperStyle={{ fontSize: "10px", paddingTop: "8px" }}
                iconSize={10}
                formatter={(value) => {
                  const labels: Record<string, string> = {
                    CB: "Block Coefficient (CB)",
                    CP: "Prismatic Coefficient (CP)",
                    CM: "Midship Coefficient (CM)",
                    CWP: "Waterplane Coefficient (CWP)",
                  };
                  return labels[value] || value;
                }}
              />

              {/* Design draft reference line */}
              {designDraft && (
                <ReferenceLine
                  x={designDraft}
                  stroke="#EF4444"
                  strokeWidth={2}
                  strokeDasharray="5 5"
                  label={{
                    value: "Design Draft",
                    position: "top",
                    fill: "#EF4444",
                    fontSize: 10,
                  }}
                />
              )}

              {/* Coefficient lines */}
              <Line
                type="monotone"
                dataKey="CB"
                stroke="#3B82F6"
                strokeWidth={2}
                dot={false}
                name="CB"
              />
              <Line
                type="monotone"
                dataKey="CP"
                stroke="#10B981"
                strokeWidth={2}
                dot={false}
                name="CP"
              />
              <Line
                type="monotone"
                dataKey="CM"
                stroke="#F59E0B"
                strokeWidth={2}
                dot={false}
                name="CM"
              />
              <Line
                type="monotone"
                dataKey="CWP"
                stroke="#8B5CF6"
                strokeWidth={2}
                dot={false}
                name="CWP"
              />
            </LineChart>
          </ResponsiveContainer>
        </div>

        {/* Legend/Info about typical ranges */}
        {vessel?.metadata?.vesselType && TYPICAL_RANGES[vessel.metadata.vesselType] && (
          <div className="mt-3 pt-3 border-t border-border flex-shrink-0">
            <p className="text-xs text-muted-foreground mb-2">
              <strong>Typical ranges for {vessel.metadata.vesselType}:</strong>
            </p>
            <div className="grid grid-cols-4 gap-2 text-[10px]">
              {Object.entries(TYPICAL_RANGES[vessel.metadata.vesselType]).map(([coeff, range]) => (
                <div key={coeff} className="text-center">
                  <span className="font-medium text-foreground">{coeff.toUpperCase()}:</span>{" "}
                  <span className="text-muted-foreground">
                    {range[0].toFixed(2)} - {range[1].toFixed(2)}
                  </span>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    );
  }
);
