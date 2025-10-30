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
import type { HydroResult } from "../../../../types/hydrostatics";
import { settingsStore } from "../../../../stores/SettingsStore";
import { getUnitSymbol } from "../../../../utils/unitSymbols";

interface HydrostaticCurvesPanelProps {
  results: HydroResult[];
  onDraftHover?: (draft: number | null) => void;
}

export const HydrostaticCurvesPanel = observer(
  ({ results, onDraftHover }: HydrostaticCurvesPanelProps) => {
    const [selectedCurveType, setSelectedCurveType] = useState<
      "hydrostatic" | "bonjean" | "cross-curves"
    >("hydrostatic");

    const displayUnits = settingsStore.preferredUnits;
    const lengthUnit = getUnitSymbol(displayUnits, "Length");
    const areaUnit = getUnitSymbol(displayUnits, "Area");

    const formatNumber = (value: number): string => {
      return value.toFixed(2);
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
            <p className="mt-1 text-xs text-muted-foreground">Compute hydrostatics to see curves</p>
          </div>
        </div>
      );
    }

    const chartData = results.map((r) => ({
      draft: r.draft,
      displacement: r.dispWeight / 1000, // Convert to tonnes for readability
      kb: r.kBz,
      lcb: r.lCBx,
      bmt: r.bMt,
      gmt: r.gMt,
      wpa: r.awp,
    }));

    return (
      <div className="h-full flex flex-col">
        {/* Curve Type Selector */}
        <div className="flex items-center justify-between mb-3 flex-shrink-0">
          <h4 className="text-sm font-medium text-foreground">Curve Type</h4>
          <select
            value={selectedCurveType}
            onChange={(e) =>
              setSelectedCurveType(e.target.value as "hydrostatic" | "bonjean" | "cross-curves")
            }
            className="border border-border bg-background text-foreground rounded text-xs py-1 px-2 focus:outline-none focus:ring-2 focus:ring-ring"
          >
            <option value="hydrostatic">Hydrostatic Curves</option>
            <option value="bonjean">Bonjean Curves</option>
            <option value="cross-curves">Cross-Curves (KN)</option>
          </select>
        </div>

        {/* Chart */}
        <div className="flex-1 min-h-0">
          {selectedCurveType === "hydrostatic" && (
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
                    offset: -5,
                    style: { fontSize: "11px" },
                  }}
                  tick={{ fontSize: 10 }}
                  stroke="currentColor"
                  className="text-muted-foreground"
                />
                <YAxis
                  yAxisId="left"
                  label={{
                    value: `Displacement (tonnes), KB, LCB, BMt, GMt (${lengthUnit})`,
                    angle: -90,
                    position: "insideLeft",
                    style: { fontSize: "10px" },
                  }}
                  tick={{ fontSize: 10 }}
                  stroke="currentColor"
                  className="text-muted-foreground"
                />
                <YAxis
                  yAxisId="right"
                  orientation="right"
                  label={{
                    value: `WPA (${areaUnit})`,
                    angle: 90,
                    position: "insideRight",
                    style: { fontSize: "10px" },
                  }}
                  tick={{ fontSize: 10 }}
                  stroke="currentColor"
                  className="text-muted-foreground"
                />
                <Tooltip
                  contentStyle={{ fontSize: "11px", padding: "4px 8px" }}
                  formatter={(value: number) => formatNumber(value)}
                />
                <Legend wrapperStyle={{ fontSize: "10px", paddingTop: "8px" }} iconSize={10} />
                <Line
                  yAxisId="left"
                  type="monotone"
                  dataKey="displacement"
                  stroke="#3B82F6"
                  strokeWidth={1.5}
                  dot={false}
                  name="Disp (t)"
                />
                <Line
                  yAxisId="left"
                  type="monotone"
                  dataKey="kb"
                  stroke="#10B981"
                  strokeWidth={1.5}
                  dot={false}
                  name="KB"
                />
                <Line
                  yAxisId="left"
                  type="monotone"
                  dataKey="lcb"
                  stroke="#F59E0B"
                  strokeWidth={1.5}
                  dot={false}
                  name="LCB"
                />
                <Line
                  yAxisId="left"
                  type="monotone"
                  dataKey="bmt"
                  stroke="#8B5CF6"
                  strokeWidth={1.5}
                  dot={false}
                  name="BMt"
                />
                <Line
                  yAxisId="left"
                  type="monotone"
                  dataKey="gmt"
                  stroke="#EF4444"
                  strokeWidth={1.5}
                  dot={false}
                  name="GMt"
                />
                <Line
                  yAxisId="right"
                  type="monotone"
                  dataKey="wpa"
                  stroke="#EC4899"
                  strokeWidth={1.5}
                  dot={false}
                  name="WPA"
                />
              </LineChart>
            </ResponsiveContainer>
          )}

          {selectedCurveType === "bonjean" && (
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
                    d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
                  />
                </svg>
                <p className="text-xs font-medium">Bonjean Curves</p>
                <p className="text-[10px] mt-1">Coming soon</p>
              </div>
            </div>
          )}

          {selectedCurveType === "cross-curves" && (
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
          )}
        </div>
      </div>
    );
  }
);
