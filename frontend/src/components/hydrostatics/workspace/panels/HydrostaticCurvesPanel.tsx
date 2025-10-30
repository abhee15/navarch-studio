import { useState } from "react";
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
} from "recharts";
import type { HydroResult, CurveData } from "../../../../types/hydrostatics";
import { settingsStore } from "../../../../stores/SettingsStore";
import { getUnitSymbol } from "../../../../utils/unitSymbols";
import { BonjeanCurvesPanel } from "./BonjeanCurvesPanel";

interface HydrostaticCurvesPanelProps {
  vesselId: string;
  results: HydroResult[];
  curves: Record<string, CurveData>;
  onDraftHover?: (draft: number | null) => void;
}

export const HydrostaticCurvesPanel = observer(
  ({ vesselId, results, curves, onDraftHover }: HydrostaticCurvesPanelProps) => {
    const [selectedCurveType, setSelectedCurveType] = useState<
      "displacement" | "kb" | "lcb" | "awp" | "gmt" | "bonjean" | "cross-curves"
    >("displacement");

    const displayUnits = settingsStore.preferredUnits;
    const lengthUnit = getUnitSymbol(displayUnits, "Length");
    const areaUnit = getUnitSymbol(displayUnits, "Area");
    const massUnit = getUnitSymbol(displayUnits, "Mass");

    const formatNumber = (value: number): string => {
      return value.toFixed(2);
    };

    // Get the selected curve data
    const selectedCurve = curves[selectedCurveType];

    if (results.length === 0 && Object.keys(curves).length === 0) {
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
            <p className="mt-1 text-xs text-muted-foreground">Compute hydrostatics to see curves</p>
          </div>
        </div>
      );
    }

    // Prepare chart data from curve points
    const chartData =
      selectedCurve?.points.map((point) => ({
        x: point.x,
        y: point.y,
      })) || [];

    // Get Y-axis label and unit based on curve type
    const getYAxisLabel = (): string => {
      if (!selectedCurve) return "";
      const unitMap: Record<string, string> = {
        displacement: massUnit,
        kb: lengthUnit,
        lcb: lengthUnit,
        awp: areaUnit,
        gmt: lengthUnit,
      };
      return `${selectedCurve.yLabel} (${unitMap[selectedCurveType] || ""})`;
    };

    return (
      <div className="h-full flex flex-col">
        {/* Curve Type Selector */}
        <div className="flex items-center justify-between mb-3 flex-shrink-0">
          <h4 className="text-sm font-medium text-foreground">Curve Type</h4>
          <select
            value={selectedCurveType}
            onChange={(e) =>
              setSelectedCurveType(
                e.target.value as
                  | "displacement"
                  | "kb"
                  | "lcb"
                  | "awp"
                  | "gmt"
                  | "bonjean"
                  | "cross-curves"
              )
            }
            className="border border-border bg-background text-foreground rounded text-xs py-1 px-2 focus:outline-none focus:ring-2 focus:ring-ring"
          >
            <option value="displacement">Displacement</option>
            <option value="kb">KB (Center of Buoyancy Height)</option>
            <option value="lcb">LCB (Longitudinal Center of Buoyancy)</option>
            <option value="awp">Waterplane Area (AWP)</option>
            <option value="gmt">GMt (Transverse Metacentric Height)</option>
            <option value="bonjean">Bonjean Curves</option>
            <option value="cross-curves">Cross-Curves (KN)</option>
          </select>
        </div>

        {/* Chart */}
        <div className="flex-1 min-h-0">
          {selectedCurveType === "bonjean" ? (
            <BonjeanCurvesPanel vesselId={vesselId} />
          ) : selectedCurveType === "cross-curves" ? (
            <div className="flex items-center justify-center h-full">
              <div className="text-center text-muted-foreground">
                <svg
                  className="mx-auto h-8 w-8 mb-2"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6"
                  />
                </svg>
                <p className="text-xs font-medium">Cross-Curves of Stability (KN)</p>
                <p className="text-[10px] mt-1">Coming soon</p>
              </div>
            </div>
          ) : selectedCurve ? (
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
                      | { payload: { x: number } }
                      | undefined;
                    if (
                      firstPayload &&
                      "payload" in firstPayload &&
                      firstPayload.payload &&
                      "x" in firstPayload.payload
                    ) {
                      onDraftHover(firstPayload.payload.x);
                    }
                  }
                }}
                onMouseLeave={() => onDraftHover && onDraftHover(null)}
              >
                <CartesianGrid
                  strokeDasharray="3 3"
                  stroke="#e5e7eb"
                  className="dark:stroke-gray-700"
                />
                <XAxis
                  dataKey="x"
                  label={{
                    value: selectedCurve.xLabel || `Draft (${lengthUnit})`,
                    position: "insideBottom",
                    offset: -5,
                    style: { fontSize: "11px" },
                  }}
                  tick={{ fontSize: 10 }}
                  stroke="currentColor"
                  className="text-muted-foreground"
                />
                <YAxis
                  label={{
                    value: getYAxisLabel(),
                    angle: -90,
                    position: "insideLeft",
                    style: { fontSize: "10px" },
                  }}
                  tick={{ fontSize: 10 }}
                  stroke="currentColor"
                  className="text-muted-foreground"
                />
                <Tooltip
                  contentStyle={{ fontSize: "11px", padding: "4px 8px" }}
                  formatter={(value: number) => formatNumber(value)}
                  labelFormatter={(label) =>
                    `${selectedCurve.xLabel || "Draft"}: ${formatNumber(Number(label))} ${lengthUnit}`
                  }
                />
                <Legend wrapperStyle={{ fontSize: "10px", paddingTop: "8px" }} iconSize={10} />
                <Line
                  type="monotone"
                  dataKey="y"
                  stroke="#3B82F6"
                  strokeWidth={1.5}
                  dot={false}
                  name={selectedCurve.yLabel}
                />
              </LineChart>
            </ResponsiveContainer>
          ) : (
            <div className="flex items-center justify-center h-full">
              <div className="text-center text-muted-foreground">
                <p className="text-xs">Curve data not available</p>
                <p className="text-[10px] mt-1">Please compute to generate curves</p>
              </div>
            </div>
          )}
        </div>
      </div>
    );
  }
);
