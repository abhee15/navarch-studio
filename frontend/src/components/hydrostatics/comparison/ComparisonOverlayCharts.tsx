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
import type { ComparisonReport } from "../../../types/comparison";
import { settingsStore } from "../../../stores/SettingsStore";
import { getUnitSymbol } from "../../../utils/unitSymbols";

interface ComparisonOverlayChartsProps {
  report: ComparisonReport;
}

export const ComparisonOverlayCharts = observer(({ report }: ComparisonOverlayChartsProps) => {
  const displayUnits = settingsStore.preferredUnits;
  const lengthUnit = getUnitSymbol(displayUnits, "Length");
  const massUnit = getUnitSymbol(displayUnits, "Mass");
  const areaUnit = getUnitSymbol(displayUnits, "Area");

  // Prepare data for charts
  const baselineData = report.baseline.results.map((r) => ({
    draft: r.draft,
    displacement: r.dispWeight,
    kb: r.kBz,
    gmt: r.gMt,
    lcb: r.lCBx,
    wpa: r.awp,
    cb: r.cb,
    cp: r.cp,
  }));

  const candidateData = report.candidate.results.map((r) => ({
    draft: r.draft,
    displacement: r.dispWeight,
    kb: r.kBz,
    gmt: r.gMt,
    lcb: r.lCBx,
    wpa: r.awp,
    cb: r.cb,
    cp: r.cp,
  }));

  const charts = [
    {
      title: "Displacement vs Draft",
      dataKey: "displacement",
      yLabel: `Displacement (${massUnit})`,
    },
    {
      title: "KB vs Draft",
      dataKey: "kb",
      yLabel: `KB (${lengthUnit})`,
    },
    {
      title: "GMt vs Draft",
      dataKey: "gmt",
      yLabel: `GMt (${lengthUnit})`,
    },
    {
      title: "LCB vs Draft",
      dataKey: "lcb",
      yLabel: `LCB (${lengthUnit})`,
    },
    {
      title: "WPA vs Draft",
      dataKey: "wpa",
      yLabel: `WPA (${areaUnit})`,
    },
    {
      title: "Block Coefficient vs Draft",
      dataKey: "cb",
      yLabel: "Cb",
    },
  ];

  return (
    <div className="space-y-6">
      <div className="bg-card border border-border rounded-lg p-4">
        <h2 className="text-lg font-semibold text-foreground mb-4">Overlay Charts</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {charts.map((chart) => (
            <div key={chart.dataKey} className="bg-muted/30 rounded-lg p-4">
              <h3 className="text-sm font-medium text-foreground mb-3">{chart.title}</h3>
              <ResponsiveContainer width="100%" height={250}>
                <LineChart>
                  <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
                  <XAxis
                    dataKey="draft"
                    label={{
                      value: `Draft (${lengthUnit})`,
                      position: "insideBottom",
                      offset: -5,
                      style: { fontSize: "12px", fill: "var(--muted-foreground)" },
                    }}
                    tick={{ fontSize: 11, fill: "var(--muted-foreground)" }}
                    stroke="var(--border)"
                  />
                  <YAxis
                    label={{
                      value: chart.yLabel,
                      angle: -90,
                      position: "insideLeft",
                      style: { fontSize: "12px", fill: "var(--muted-foreground)" },
                    }}
                    tick={{ fontSize: 11, fill: "var(--muted-foreground)" }}
                    stroke="var(--border)"
                  />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: "var(--card)",
                      border: "1px solid var(--border)",
                      borderRadius: "8px",
                      fontSize: "12px",
                    }}
                  />
                  <Legend wrapperStyle={{ fontSize: "12px" }} iconType="line" iconSize={16} />
                  <Line
                    data={baselineData}
                    type="monotone"
                    dataKey={chart.dataKey}
                    stroke="#3b82f6"
                    strokeWidth={2}
                    name={report.baseline.runName}
                    dot={{ r: 3 }}
                    activeDot={{ r: 5 }}
                  />
                  <Line
                    data={candidateData}
                    type="monotone"
                    dataKey={chart.dataKey}
                    stroke="#a855f7"
                    strokeWidth={2}
                    name={report.candidate.runName}
                    dot={{ r: 3 }}
                    activeDot={{ r: 5 }}
                  />
                </LineChart>
              </ResponsiveContainer>
            </div>
          ))}
        </div>
      </div>

      {/* Delta Chart - Shows % difference */}
      <div className="bg-card border border-border rounded-lg p-4">
        <h2 className="text-lg font-semibold text-foreground mb-4">
          Percentage Difference (Candidate vs Baseline)
        </h2>
        <div className="bg-muted/30 rounded-lg p-4">
          <ResponsiveContainer width="100%" height={300}>
            <LineChart
              data={report.draftComparisons.map((dc) => ({
                draft: dc.draft,
                displacement:
                  dc.kpiComparisons.find((k) => k.kpiName === "Displacement")?.percentDelta || 0,
                gmt: dc.kpiComparisons.find((k) => k.kpiName === "GMt")?.percentDelta || 0,
              }))}
            >
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis
                dataKey="draft"
                label={{
                  value: `Draft (${lengthUnit})`,
                  position: "insideBottom",
                  offset: -5,
                  style: { fontSize: "12px", fill: "var(--muted-foreground)" },
                }}
                tick={{ fontSize: 11, fill: "var(--muted-foreground)" }}
                stroke="var(--border)"
              />
              <YAxis
                label={{
                  value: "% Change",
                  angle: -90,
                  position: "insideLeft",
                  style: { fontSize: "12px", fill: "var(--muted-foreground)" },
                }}
                tick={{ fontSize: 11, fill: "var(--muted-foreground)" }}
                stroke="var(--border)"
              />
              <Tooltip
                contentStyle={{
                  backgroundColor: "var(--card)",
                  border: "1px solid var(--border)",
                  borderRadius: "8px",
                  fontSize: "12px",
                }}
                formatter={(value: number) => `${value.toFixed(1)}%`}
              />
              <Legend wrapperStyle={{ fontSize: "12px" }} iconType="line" iconSize={16} />
              <Line
                type="monotone"
                dataKey="displacement"
                stroke="#10b981"
                strokeWidth={2}
                name="Displacement %Δ"
                dot={{ r: 3 }}
              />
              <Line
                type="monotone"
                dataKey="gmt"
                stroke="#f59e0b"
                strokeWidth={2}
                name="GMt %Δ"
                dot={{ r: 3 }}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>
      </div>
    </div>
  );
});
