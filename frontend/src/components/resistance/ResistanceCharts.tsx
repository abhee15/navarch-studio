import { useMemo } from "react";
import { useState, useEffect } from "react";
import {
  LineChart,
  Line,
  AreaChart,
  Area,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  ComposedChart,
  PieChart,
  Pie,
  Cell,
} from "recharts";
import type {
  Ittc57CalculationResult,
  HoltropMennenCalculationResult,
  PowerCurveResult,
  KcsBenchmarkResult,
} from "../../types/resistance";

interface ResistanceChartsProps {
  ittc57Result: Ittc57CalculationResult | null;
  hmResult: HoltropMennenCalculationResult | null;
  powerResult: PowerCurveResult | null;
  kcsBenchmarkResult: KcsBenchmarkResult | null;
}

interface ChartClickEvent {
  activeLabel?: string;
  activePayload?: Array<{ value?: number }>;
}

interface ComponentBreakdownData {
  name: string;
  value: number;
  color: string;
}

interface PieChartData {
  name: string;
  value: number;
  color: string;
  fullName: string;
}

interface PieLabelProps {
  name?: string;
  value?: number;
}

interface TooltipPayload {
  payload?: PieChartData;
}

export function ResistanceCharts({
  ittc57Result,
  hmResult,
  powerResult,
  kcsBenchmarkResult,
}: ResistanceChartsProps) {
  const [selectedSpeedIndex, setSelectedSpeedIndex] = useState<number | null>(null);
  const [compareMode, setCompareMode] = useState<boolean>(false);
  const [compareSpeed1Index, setCompareSpeed1Index] = useState<number | null>(null);
  const [compareSpeed2Index, setCompareSpeed2Index] = useState<number | null>(null);

  // Prepare ITTC-57 data
  const ittc57Data = useMemo(() => {
    if (!ittc57Result) return [];

    return ittc57Result.speedGrid.map((speed, idx) => ({
      speed: speed,
      speedKnots: speed / 0.514444,
      re: ittc57Result.reynoldsNumbers[idx],
      fn: ittc57Result.froudeNumbers[idx],
      cf: ittc57Result.frictionCoefficients[idx],
      cfEff: ittc57Result.effectiveFrictionCoefficients[idx],
    }));
  }, [ittc57Result]);

  // Prepare Holtrop-Mennen data
  const hmData = useMemo(() => {
    if (!hmResult) return [];

    return hmResult.speedGrid.map((speed, idx) => ({
      speed: speed,
      speedKnots: speed / 0.514444,
      rt: hmResult.totalResistance[idx] / 1000, // Convert to kN
      rf: hmResult.frictionResistance[idx] / 1000,
      rr: hmResult.residuaryResistance[idx] / 1000,
      ra: hmResult.appendageResistance[idx] / 1000,
      rca: hmResult.correlationAllowance[idx] / 1000,
      raa: hmResult.airResistance[idx] / 1000,
      ehp: hmResult.effectivePower[idx],
      re: hmResult.reynoldsNumbers[idx],
      fn: hmResult.froudeNumbers[idx],
    }));
  }, [hmResult]);

  // Reset selected speed when HM result changes
  useEffect(() => {
    setSelectedSpeedIndex(null);
    setCompareMode(false);
    setCompareSpeed1Index(null);
    setCompareSpeed2Index(null);
  }, [hmResult]);

  // Prepare power curve data (merge with HM data if available)
  const powerData = useMemo(() => {
    if (!powerResult) return [];

    return powerResult.speedGrid.map((speed, idx) => ({
      speed: speed,
      speedKnots: speed / 0.514444,
      ehp: powerResult.effectivePower[idx],
      dhp: powerResult.deliveredPower[idx],
      pInst: powerResult.installedPower[idx],
    }));
  }, [powerResult]);

  return (
    <div className="space-y-6">
      {/* ITTC-57 Charts */}
      {ittc57Result && (
        <div className="space-y-6">
          <h3 className="text-lg font-semibold text-foreground">ITTC-57 Friction Analysis</h3>

          {/* Friction Coefficient vs Speed */}
          <div className="bg-card border border-border rounded-lg p-4">
            <h4 className="text-sm font-medium mb-3">Friction Coefficient vs Speed</h4>
            <ResponsiveContainer width="100%" height={300}>
              <ComposedChart data={ittc57Data} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />
                <XAxis
                  dataKey="speedKnots"
                  label={{ value: "Speed (knots)", position: "insideBottom", offset: -5 }}
                  stroke="#6B7280"
                />
                <YAxis
                  label={{ value: "Coefficient", angle: -90, position: "insideLeft" }}
                  stroke="#6B7280"
                />
                <Tooltip
                  contentStyle={{
                    backgroundColor: "#FFF",
                    border: "1px solid #E5E7EB",
                    borderRadius: "0.375rem",
                    padding: "8px",
                  }}
                  formatter={(value: number, name: string) => {
                    if (name === "CF" || name === "CF_eff") {
                      return [value.toFixed(6), name];
                    }
                    return [value.toFixed(3), name];
                  }}
                />
                <Legend />
                <Line
                  type="monotone"
                  dataKey="cf"
                  stroke="#3B82F6"
                  strokeWidth={2}
                  name="CF (ITTC-57)"
                  dot={false}
                />
                <Line
                  type="monotone"
                  dataKey="cfEff"
                  stroke="#10B981"
                  strokeWidth={2}
                  name="CF_eff (with form factor)"
                  dot={false}
                />
              </ComposedChart>
            </ResponsiveContainer>
          </div>

          {/* CF vs Reynolds Number */}
          <div className="bg-card border border-border rounded-lg p-4">
            <h4 className="text-sm font-medium mb-3">Friction Coefficient vs Reynolds Number</h4>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={ittc57Data} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />
                <XAxis
                  dataKey="re"
                  type="number"
                  scale="log"
                  label={{
                    value: "Reynolds Number (log scale)",
                    position: "insideBottom",
                    offset: -5,
                  }}
                  stroke="#6B7280"
                  tickFormatter={(value) => value.toExponential(1)}
                />
                <YAxis
                  label={{ value: "CF", angle: -90, position: "insideLeft" }}
                  stroke="#6B7280"
                />
                <Tooltip
                  contentStyle={{
                    backgroundColor: "#FFF",
                    border: "1px solid #E5E7EB",
                    borderRadius: "0.375rem",
                    padding: "8px",
                  }}
                  formatter={(value: number) => value.toFixed(6)}
                />
                <Legend />
                <Line
                  type="monotone"
                  dataKey="cf"
                  stroke="#3B82F6"
                  strokeWidth={2}
                  name="CF (ITTC-57)"
                  dot={false}
                />
                <Line
                  type="monotone"
                  dataKey="cfEff"
                  stroke="#10B981"
                  strokeWidth={2}
                  name="CF_eff"
                  dot={false}
                />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </div>
      )}

      {/* Holtrop-Mennen Charts */}
      {hmResult && (
        <div className="space-y-6">
          <h3 className="text-lg font-semibold text-foreground">
            Holtrop-Mennen Resistance Analysis
          </h3>

          {/* Total Resistance vs Speed */}
          <div className="bg-card border border-border rounded-lg p-4">
            <h4 className="text-sm font-medium mb-3">Total Resistance vs Speed</h4>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={hmData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />
                <XAxis
                  dataKey="speedKnots"
                  label={{ value: "Speed (knots)", position: "insideBottom", offset: -5 }}
                  stroke="#6B7280"
                />
                <YAxis
                  label={{ value: "Resistance (kN)", angle: -90, position: "insideLeft" }}
                  stroke="#6B7280"
                />
                <Tooltip
                  contentStyle={{
                    backgroundColor: "#FFF",
                    border: "1px solid #E5E7EB",
                    borderRadius: "0.375rem",
                    padding: "8px",
                  }}
                  formatter={(value: number) => `${value.toFixed(2)} kN`}
                />
                <Legend />
                <Line
                  type="monotone"
                  dataKey="rt"
                  stroke="#EF4444"
                  strokeWidth={3}
                  name="Total Resistance (RT)"
                  dot={false}
                  activeDot={{ r: 6 }}
                />
              </LineChart>
            </ResponsiveContainer>
          </div>

          {/* Resistance Components Breakdown (Stacked Area) */}
          <div className="bg-card border border-border rounded-lg p-4">
            <h4 className="text-sm font-medium mb-3">Resistance Components vs Speed</h4>
            <ResponsiveContainer width="100%" height={300}>
              <AreaChart
                data={hmData}
                margin={{ top: 5, right: 30, left: 20, bottom: 5 }}
                onClick={(data: ChartClickEvent) => {
                  if (data?.activeLabel) {
                    const speedValue = parseFloat(data.activeLabel);
                    if (!isNaN(speedValue)) {
                      const idx = hmData.findIndex(
                        (d) => Math.abs(d.speedKnots - speedValue) < 0.01
                      );
                      if (idx >= 0) setSelectedSpeedIndex(idx);
                    }
                  }
                }}
              >
                <defs>
                  <linearGradient id="colorRF" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#3B82F6" stopOpacity={0.8} />
                    <stop offset="95%" stopColor="#3B82F6" stopOpacity={0.1} />
                  </linearGradient>
                  <linearGradient id="colorRR" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#10B981" stopOpacity={0.8} />
                    <stop offset="95%" stopColor="#10B981" stopOpacity={0.1} />
                  </linearGradient>
                  <linearGradient id="colorRA" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#F59E0B" stopOpacity={0.8} />
                    <stop offset="95%" stopColor="#F59E0B" stopOpacity={0.1} />
                  </linearGradient>
                  <linearGradient id="colorRCA" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#8B5CF6" stopOpacity={0.8} />
                    <stop offset="95%" stopColor="#8B5CF6" stopOpacity={0.1} />
                  </linearGradient>
                  <linearGradient id="colorRAA" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#EF4444" stopOpacity={0.8} />
                    <stop offset="95%" stopColor="#EF4444" stopOpacity={0.1} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />
                <XAxis
                  dataKey="speedKnots"
                  label={{ value: "Speed (knots)", position: "insideBottom", offset: -5 }}
                  stroke="#6B7280"
                />
                <YAxis
                  label={{ value: "Resistance (kN)", angle: -90, position: "insideLeft" }}
                  stroke="#6B7280"
                />
                <Tooltip
                  contentStyle={{
                    backgroundColor: "#FFF",
                    border: "1px solid #E5E7EB",
                    borderRadius: "0.375rem",
                    padding: "8px",
                  }}
                  formatter={(value: number) => `${value.toFixed(2)} kN`}
                />
                <Legend />
                <Area
                  type="monotone"
                  dataKey="rf"
                  stackId="1"
                  stroke="#3B82F6"
                  fill="url(#colorRF)"
                  name="Friction (RF)"
                />
                <Area
                  type="monotone"
                  dataKey="rr"
                  stackId="1"
                  stroke="#10B981"
                  fill="url(#colorRR)"
                  name="Residuary (RR)"
                />
                <Area
                  type="monotone"
                  dataKey="ra"
                  stackId="1"
                  stroke="#F59E0B"
                  fill="url(#colorRA)"
                  name="Appendage (RA)"
                />
                {hmData.length > 0 && hmData[0].rca !== undefined && (
                  <Area
                    type="monotone"
                    dataKey="rca"
                    stackId="1"
                    stroke="#8B5CF6"
                    fill="url(#colorRCA)"
                    name="Correlation (RCA)"
                  />
                )}
                {hmData.length > 0 && hmData[0].raa !== undefined && (
                  <Area
                    type="monotone"
                    dataKey="raa"
                    stackId="1"
                    stroke="#EF4444"
                    fill="url(#colorRAA)"
                    name="Air (RAA)"
                  />
                )}
              </AreaChart>
            </ResponsiveContainer>
            <p className="text-xs text-muted-foreground mt-2">
              Click on the chart to view component breakdown at that speed
            </p>
          </div>

          {/* Component Breakdown at Selected Speed (Stacked Bar) */}
          {selectedSpeedIndex !== null && hmData[selectedSpeedIndex] && (
            <div className="bg-card border border-border rounded-lg p-4">
              <h4 className="text-sm font-medium mb-3">
                Component Breakdown at {hmData[selectedSpeedIndex].speedKnots.toFixed(2)} knots
                <button
                  onClick={() => setSelectedSpeedIndex(null)}
                  className="ml-2 text-xs text-muted-foreground hover:text-foreground"
                >
                  (Clear)
                </button>
              </h4>
              <ResponsiveContainer width="100%" height={250}>
                <BarChart
                  layout="vertical"
                  data={[
                    {
                      name: "Friction (RF)",
                      value: hmData[selectedSpeedIndex].rf,
                      color: "#3B82F6",
                    },
                    {
                      name: "Residuary (RR)",
                      value: hmData[selectedSpeedIndex].rr,
                      color: "#10B981",
                    },
                    {
                      name: "Appendage (RA)",
                      value: hmData[selectedSpeedIndex].ra,
                      color: "#F59E0B",
                    },
                    ...(hmData[selectedSpeedIndex].rca !== undefined &&
                    hmData[selectedSpeedIndex].rca > 0
                      ? [
                          {
                            name: "Correlation (RCA)",
                            value: hmData[selectedSpeedIndex].rca,
                            color: "#8B5CF6",
                          },
                        ]
                      : []),
                    ...(hmData[selectedSpeedIndex].raa !== undefined &&
                    hmData[selectedSpeedIndex].raa > 0
                      ? [
                          {
                            name: "Air (RAA)",
                            value: hmData[selectedSpeedIndex].raa,
                            color: "#EF4444",
                          },
                        ]
                      : []),
                  ]}
                  margin={{ top: 5, right: 30, left: 80, bottom: 5 }}
                >
                  <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />
                  <XAxis
                    type="number"
                    label={{ value: "Resistance (kN)", position: "insideBottom" }}
                  />
                  <YAxis type="category" dataKey="name" width={75} />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: "#FFF",
                      border: "1px solid #E5E7EB",
                      borderRadius: "0.375rem",
                      padding: "8px",
                    }}
                    formatter={(value: number) => `${value.toFixed(2)} kN`}
                  />
                  <Bar dataKey="value" fill="#3B82F6">
                    {[
                      { name: "Friction (RF)", color: "#3B82F6" },
                      { name: "Residuary (RR)", color: "#10B981" },
                      { name: "Appendage (RA)", color: "#F59E0B" },
                      { name: "Correlation (RCA)", color: "#8B5CF6" },
                      { name: "Air (RAA)", color: "#EF4444" },
                    ].map((comp) => (
                      <Bar
                        key={comp.name}
                        dataKey={(d: ComponentBreakdownData) =>
                          d.name === comp.name ? d.value : 0
                        }
                        stackId="1"
                        fill={comp.color}
                      />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </div>
          )}

          {/* Toggle for Compare Mode */}
          {hmData.length >= 2 && (
            <div className="bg-card border border-border rounded-lg p-4">
              <div className="flex items-center justify-between">
                <div>
                  <h4 className="text-sm font-medium">Component Distribution Analysis</h4>
                  <p className="text-xs text-muted-foreground mt-1">
                    {compareMode
                      ? "Compare resistance components at two different speeds"
                      : "View resistance component breakdown at a single speed"}
                  </p>
                </div>
                <button
                  onClick={() => {
                    setCompareMode(!compareMode);
                    if (!compareMode) {
                      // Initialize with first and last speeds when entering compare mode
                      setCompareSpeed1Index(0);
                      setCompareSpeed2Index(hmData.length - 1);
                      setSelectedSpeedIndex(null);
                    } else {
                      setCompareSpeed1Index(null);
                      setCompareSpeed2Index(null);
                    }
                  }}
                  className={`px-4 py-2 rounded-lg font-medium text-sm transition-colors ${
                    compareMode
                      ? "bg-primary text-primary-foreground hover:bg-primary/90"
                      : "bg-muted text-foreground hover:bg-muted/80"
                  }`}
                >
                  {compareMode ? "Single View" : "Compare Two Speeds"}
                </button>
              </div>
            </div>
          )}

          {/* Component Breakdown Pie Chart */}
          {!compareMode &&
            selectedSpeedIndex !== null &&
            hmData[selectedSpeedIndex] &&
            (() => {
              const selectedData = hmData[selectedSpeedIndex];
              const pieData = [
                {
                  name: "Friction (RF)",
                  value: selectedData.rf,
                  color: "#3B82F6",
                  fullName: "Frictional Resistance",
                },
                {
                  name: "Residuary (RR)",
                  value: selectedData.rr,
                  color: "#10B981",
                  fullName: "Residuary Resistance",
                },
                {
                  name: "Appendage (RA)",
                  value: selectedData.ra,
                  color: "#F59E0B",
                  fullName: "Appendage Resistance",
                },
                ...(selectedData.rca !== undefined && selectedData.rca > 0
                  ? [
                      {
                        name: "Correlation (RCA)",
                        value: selectedData.rca,
                        color: "#8B5CF6",
                        fullName: "Correlation Allowance",
                      },
                    ]
                  : []),
                ...(selectedData.raa !== undefined && selectedData.raa > 0
                  ? [
                      {
                        name: "Air (RAA)",
                        value: selectedData.raa,
                        color: "#EF4444",
                        fullName: "Air Resistance",
                      },
                    ]
                  : []),
              ].filter((item) => item.value > 0);

              const total = pieData.reduce((sum, item) => sum + item.value, 0);

              const exportPieData = () => {
                const csvContent = [
                  "Component,Resistance (kN),Percentage (%)",
                  ...pieData.map(
                    (item) =>
                      `${item.fullName},${item.value.toFixed(3)},${((item.value / total) * 100).toFixed(2)}`
                  ),
                  `Total,${total.toFixed(3)},100.00`,
                ].join("\n");

                const blob = new Blob([csvContent], { type: "text/csv" });
                const url = URL.createObjectURL(blob);
                const a = document.createElement("a");
                a.href = url;
                a.download = `resistance-components-${selectedData.speedKnots.toFixed(2)}kts.csv`;
                a.click();
                URL.revokeObjectURL(url);
              };

              return (
                <div className="bg-card border border-border rounded-lg p-4">
                  <div className="flex items-center justify-between mb-3">
                    <h4 className="text-sm font-medium">
                      Component Distribution at {selectedData.speedKnots.toFixed(2)} knots
                      <button
                        onClick={() => setSelectedSpeedIndex(null)}
                        className="ml-2 text-xs text-muted-foreground hover:text-foreground"
                      >
                        (Clear)
                      </button>
                    </h4>
                    <button
                      onClick={exportPieData}
                      className="text-xs px-2 py-1 bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 hover:bg-blue-100 dark:hover:bg-blue-900/50 rounded border border-blue-200 dark:border-blue-700"
                    >
                      Export CSV
                    </button>
                  </div>
                  <div className="flex flex-col lg:flex-row items-center gap-4">
                    {/* Pie Chart */}
                    <div className="w-full lg:w-1/2">
                      <ResponsiveContainer width="100%" height={300}>
                        <PieChart>
                          <Pie
                            data={pieData}
                            cx="50%"
                            cy="50%"
                            labelLine={false}
                            label={(entry: PieLabelProps) => {
                              const value = typeof entry.value === "number" ? entry.value : 0;
                              const percent = ((value / total) * 100).toFixed(1);
                              const name = entry.name || "";
                              return `${name.split(" ")[0]}: ${percent}%`;
                            }}
                            outerRadius={100}
                            fill="#8884d8"
                            dataKey="value"
                          >
                            {pieData.map((entry, index) => (
                              <Cell key={`cell-${index}`} fill={entry.color} />
                            ))}
                          </Pie>
                          <Tooltip
                            contentStyle={{
                              backgroundColor: "#FFF",
                              border: "1px solid #E5E7EB",
                              borderRadius: "0.375rem",
                              padding: "8px",
                            }}
                            formatter={(value: number, _name: string, props: TooltipPayload) => {
                              const percent = ((value / total) * 100).toFixed(2);
                              return [
                                `${value.toFixed(2)} kN (${percent}%)`,
                                props.payload?.fullName || "",
                              ];
                            }}
                          />
                        </PieChart>
                      </ResponsiveContainer>
                    </div>

                    {/* Legend with Details */}
                    <div className="w-full lg:w-1/2 space-y-2">
                      <div className="text-xs font-medium text-muted-foreground mb-3">
                        Component Details
                      </div>
                      {pieData.map((item) => {
                        const percent = ((item.value / total) * 100).toFixed(2);
                        return (
                          <div
                            key={item.name}
                            className="flex items-center justify-between p-2 rounded bg-gray-50 dark:bg-gray-800/50"
                          >
                            <div className="flex items-center gap-2">
                              <div
                                className="w-3 h-3 rounded-full"
                                style={{ backgroundColor: item.color }}
                              />
                              <span className="text-sm font-medium">{item.fullName}</span>
                            </div>
                            <div className="flex flex-col items-end">
                              <span className="text-sm font-semibold">
                                {item.value.toFixed(2)} kN
                              </span>
                              <span className="text-xs text-muted-foreground">{percent}%</span>
                            </div>
                          </div>
                        );
                      })}
                      <div className="flex items-center justify-between p-2 rounded bg-blue-50 dark:bg-blue-900/30 border-t-2 border-blue-300 dark:border-blue-700">
                        <span className="text-sm font-semibold">Total Resistance</span>
                        <span className="text-sm font-bold">{total.toFixed(2)} kN</span>
                      </div>
                    </div>
                  </div>
                </div>
              );
            })()}

          {/* Two-Speed Comparison View */}
          {compareMode && compareSpeed1Index !== null && compareSpeed2Index !== null && (
            <div className="bg-card border border-border rounded-lg p-4">
              <h4 className="text-sm font-medium mb-4">Component Comparison</h4>

              {/* Speed Selectors */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
                <div>
                  <label className="block text-xs font-medium text-muted-foreground mb-2">
                    Speed 1
                  </label>
                  <select
                    value={compareSpeed1Index}
                    onChange={(e) => setCompareSpeed1Index(parseInt(e.target.value))}
                    className="w-full px-3 py-2 bg-background border border-border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary"
                  >
                    {hmData.map((data, idx) => (
                      <option key={idx} value={idx}>
                        {data.speedKnots.toFixed(2)} knots ({data.speed.toFixed(2)} m/s)
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-medium text-muted-foreground mb-2">
                    Speed 2
                  </label>
                  <select
                    value={compareSpeed2Index}
                    onChange={(e) => setCompareSpeed2Index(parseInt(e.target.value))}
                    className="w-full px-3 py-2 bg-background border border-border rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-primary"
                  >
                    {hmData.map((data, idx) => (
                      <option key={idx} value={idx}>
                        {data.speedKnots.toFixed(2)} knots ({data.speed.toFixed(2)} m/s)
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              {/* Pie Charts Side by Side */}
              {(() => {
                const speed1Data = hmData[compareSpeed1Index];
                const speed2Data = hmData[compareSpeed2Index];

                const createPieData = (data: typeof speed1Data) => {
                  return [
                    {
                      name: "Friction (RF)",
                      value: data.rf,
                      color: "#3B82F6",
                      fullName: "Frictional Resistance",
                    },
                    {
                      name: "Residuary (RR)",
                      value: data.rr,
                      color: "#10B981",
                      fullName: "Residuary Resistance",
                    },
                    {
                      name: "Appendage (RA)",
                      value: data.ra,
                      color: "#F59E0B",
                      fullName: "Appendage Resistance",
                    },
                    ...(data.rca !== undefined && data.rca > 0
                      ? [
                          {
                            name: "Correlation (RCA)",
                            value: data.rca,
                            color: "#8B5CF6",
                            fullName: "Correlation Allowance",
                          },
                        ]
                      : []),
                    ...(data.raa !== undefined && data.raa > 0
                      ? [
                          {
                            name: "Air (RAA)",
                            value: data.raa,
                            color: "#EF4444",
                            fullName: "Air Resistance",
                          },
                        ]
                      : []),
                  ].filter((item) => item.value > 0);
                };

                const pieData1 = createPieData(speed1Data);
                const pieData2 = createPieData(speed2Data);
                const total1 = pieData1.reduce((sum, item) => sum + item.value, 0);
                const total2 = pieData2.reduce((sum, item) => sum + item.value, 0);

                return (
                  <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                    {/* Speed 1 Pie Chart */}
                    <div className="border border-border rounded-lg p-4">
                      <div className="text-center mb-3">
                        <h5 className="text-sm font-semibold">
                          {speed1Data.speedKnots.toFixed(2)} knots
                        </h5>
                        <p className="text-xs text-muted-foreground">
                          Total: {total1.toFixed(2)} kN
                        </p>
                      </div>
                      <ResponsiveContainer width="100%" height={250}>
                        <PieChart>
                          <Pie
                            data={pieData1}
                            cx="50%"
                            cy="50%"
                            labelLine={false}
                            label={(entry: PieLabelProps) => {
                              const value = typeof entry.value === "number" ? entry.value : 0;
                              const percent = ((value / total1) * 100).toFixed(1);
                              const name = entry.name || "";
                              return `${name.split(" ")[0]}: ${percent}%`;
                            }}
                            outerRadius={90}
                            fill="#8884d8"
                            dataKey="value"
                          >
                            {pieData1.map((entry, index) => (
                              <Cell key={`cell-${index}`} fill={entry.color} />
                            ))}
                          </Pie>
                          <Tooltip
                            contentStyle={{
                              backgroundColor: "#FFF",
                              border: "1px solid #E5E7EB",
                              borderRadius: "0.375rem",
                              padding: "8px",
                            }}
                            formatter={(value: number, _name: string, props: TooltipPayload) => {
                              const percent = ((value / total1) * 100).toFixed(2);
                              return [
                                `${value.toFixed(2)} kN (${percent}%)`,
                                props.payload?.fullName || "",
                              ];
                            }}
                          />
                        </PieChart>
                      </ResponsiveContainer>
                      {/* Legend for Speed 1 */}
                      <div className="mt-4 space-y-1">
                        {pieData1.map((item) => {
                          const percent = ((item.value / total1) * 100).toFixed(2);
                          return (
                            <div
                              key={item.name}
                              className="flex items-center justify-between text-xs p-1.5 rounded bg-gray-50 dark:bg-gray-800/50"
                            >
                              <div className="flex items-center gap-2">
                                <div
                                  className="w-2.5 h-2.5 rounded-full"
                                  style={{ backgroundColor: item.color }}
                                />
                                <span>{item.fullName}</span>
                              </div>
                              <span className="font-medium">
                                {item.value.toFixed(2)} kN ({percent}%)
                              </span>
                            </div>
                          );
                        })}
                      </div>
                    </div>

                    {/* Speed 2 Pie Chart */}
                    <div className="border border-border rounded-lg p-4">
                      <div className="text-center mb-3">
                        <h5 className="text-sm font-semibold">
                          {speed2Data.speedKnots.toFixed(2)} knots
                        </h5>
                        <p className="text-xs text-muted-foreground">
                          Total: {total2.toFixed(2)} kN
                        </p>
                      </div>
                      <ResponsiveContainer width="100%" height={250}>
                        <PieChart>
                          <Pie
                            data={pieData2}
                            cx="50%"
                            cy="50%"
                            labelLine={false}
                            label={(entry: PieLabelProps) => {
                              const value = typeof entry.value === "number" ? entry.value : 0;
                              const percent = ((value / total2) * 100).toFixed(1);
                              const name = entry.name || "";
                              return `${name.split(" ")[0]}: ${percent}%`;
                            }}
                            outerRadius={90}
                            fill="#8884d8"
                            dataKey="value"
                          >
                            {pieData2.map((entry, index) => (
                              <Cell key={`cell-${index}`} fill={entry.color} />
                            ))}
                          </Pie>
                          <Tooltip
                            contentStyle={{
                              backgroundColor: "#FFF",
                              border: "1px solid #E5E7EB",
                              borderRadius: "0.375rem",
                              padding: "8px",
                            }}
                            formatter={(value: number, _name: string, props: TooltipPayload) => {
                              const percent = ((value / total2) * 100).toFixed(2);
                              return [
                                `${value.toFixed(2)} kN (${percent}%)`,
                                props.payload?.fullName || "",
                              ];
                            }}
                          />
                        </PieChart>
                      </ResponsiveContainer>
                      {/* Legend for Speed 2 */}
                      <div className="mt-4 space-y-1">
                        {pieData2.map((item) => {
                          const percent = ((item.value / total2) * 100).toFixed(2);
                          return (
                            <div
                              key={item.name}
                              className="flex items-center justify-between text-xs p-1.5 rounded bg-gray-50 dark:bg-gray-800/50"
                            >
                              <div className="flex items-center gap-2">
                                <div
                                  className="w-2.5 h-2.5 rounded-full"
                                  style={{ backgroundColor: item.color }}
                                />
                                <span>{item.fullName}</span>
                              </div>
                              <span className="font-medium">
                                {item.value.toFixed(2)} kN ({percent}%)
                              </span>
                            </div>
                          );
                        })}
                      </div>
                    </div>
                  </div>
                );
              })()}

              {/* Comparison Metrics */}
              {(() => {
                const speed1Data = hmData[compareSpeed1Index];
                const speed2Data = hmData[compareSpeed2Index];
                const total1 =
                  speed1Data.rf +
                  speed1Data.rr +
                  speed1Data.ra +
                  (speed1Data.rca || 0) +
                  (speed1Data.raa || 0);
                const total2 =
                  speed2Data.rf +
                  speed2Data.rr +
                  speed2Data.ra +
                  (speed2Data.rca || 0) +
                  (speed2Data.raa || 0);
                const deltaTotalKN = total2 - total1;
                const deltaTotalPercent = (deltaTotalKN / total1) * 100;

                const componentComparisons = [
                  {
                    name: "Friction (RF)",
                    val1: speed1Data.rf,
                    val2: speed2Data.rf,
                    color: "#3B82F6",
                  },
                  {
                    name: "Residuary (RR)",
                    val1: speed1Data.rr,
                    val2: speed2Data.rr,
                    color: "#10B981",
                  },
                  {
                    name: "Appendage (RA)",
                    val1: speed1Data.ra,
                    val2: speed2Data.ra,
                    color: "#F59E0B",
                  },
                  ...(speed1Data.rca !== undefined && speed2Data.rca !== undefined
                    ? [
                        {
                          name: "Correlation (RCA)",
                          val1: speed1Data.rca,
                          val2: speed2Data.rca,
                          color: "#8B5CF6",
                        },
                      ]
                    : []),
                  ...(speed1Data.raa !== undefined && speed2Data.raa !== undefined
                    ? [
                        {
                          name: "Air (RAA)",
                          val1: speed1Data.raa,
                          val2: speed2Data.raa,
                          color: "#EF4444",
                        },
                      ]
                    : []),
                ];

                return (
                  <div className="mt-6 border-t border-border pt-4">
                    <h5 className="text-sm font-semibold mb-3">Comparison Metrics</h5>
                    <div className="space-y-2">
                      {/* Total Resistance Change */}
                      <div className="bg-blue-50 dark:bg-blue-900/30 p-3 rounded-lg border border-blue-200 dark:border-blue-700">
                        <div className="flex items-center justify-between">
                          <span className="text-sm font-medium">Total Resistance Change</span>
                          <div className="text-right">
                            <div className="text-sm font-bold">
                              {deltaTotalKN > 0 ? "+" : ""}
                              {deltaTotalKN.toFixed(2)} kN
                            </div>
                            <div
                              className={`text-xs ${
                                deltaTotalPercent > 0
                                  ? "text-red-600 dark:text-red-400"
                                  : deltaTotalPercent < 0
                                    ? "text-green-600 dark:text-green-400"
                                    : "text-gray-600"
                              }`}
                            >
                              {deltaTotalPercent > 0 ? "+" : ""}
                              {deltaTotalPercent.toFixed(2)}%
                            </div>
                          </div>
                        </div>
                      </div>

                      {/* Component-wise Changes */}
                      {componentComparisons.map((comp) => {
                        const delta = comp.val2 - comp.val1;
                        const deltaPercent = (delta / comp.val1) * 100;
                        return (
                          <div
                            key={comp.name}
                            className="flex items-center justify-between p-2 rounded bg-gray-50 dark:bg-gray-800/50"
                          >
                            <div className="flex items-center gap-2">
                              <div
                                className="w-2.5 h-2.5 rounded-full"
                                style={{ backgroundColor: comp.color }}
                              />
                              <span className="text-xs font-medium">{comp.name}</span>
                            </div>
                            <div className="text-right">
                              <div className="text-xs font-semibold">
                                {delta > 0 ? "+" : ""}
                                {delta.toFixed(2)} kN
                              </div>
                              <div
                                className={`text-xs ${
                                  deltaPercent > 0
                                    ? "text-red-600 dark:text-red-400"
                                    : deltaPercent < 0
                                      ? "text-green-600 dark:text-green-400"
                                      : "text-gray-600"
                                }`}
                              >
                                {deltaPercent > 0 ? "+" : ""}
                                {deltaPercent.toFixed(2)}%
                              </div>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </div>
                );
              })()}
            </div>
          )}

          {/* Power Curves vs Speed */}
          <div className="bg-card border border-border rounded-lg p-4">
            <h4 className="text-sm font-medium mb-3">
              Power Curves vs Speed
              {powerResult && (
                <span className="ml-2 text-xs text-muted-foreground">
                  (SM: {powerResult.serviceMargin.toFixed(1)}%)
                </span>
              )}
            </h4>
            <ResponsiveContainer width="100%" height={300}>
              <ComposedChart
                data={powerResult ? powerData : hmData}
                margin={{ top: 5, right: 30, left: 20, bottom: 5 }}
              >
                <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />
                <XAxis
                  dataKey="speedKnots"
                  label={{ value: "Speed (knots)", position: "insideBottom", offset: -5 }}
                  stroke="#6B7280"
                />
                <YAxis
                  label={{ value: "Power (kW)", angle: -90, position: "insideLeft" }}
                  stroke="#6B7280"
                />
                <Tooltip
                  contentStyle={{
                    backgroundColor: "#FFF",
                    border: "1px solid #E5E7EB",
                    borderRadius: "0.375rem",
                    padding: "8px",
                  }}
                  formatter={(value: number) => `${value.toFixed(2)} kW`}
                />
                <Legend />
                <Line
                  type="monotone"
                  dataKey={powerResult ? "ehp" : "ehp"}
                  stroke="#8B5CF6"
                  strokeWidth={2}
                  name="Effective Power (EHP)"
                  dot={false}
                />
                {powerResult && (
                  <>
                    <Line
                      type="monotone"
                      dataKey="dhp"
                      stroke="#3B82F6"
                      strokeWidth={2}
                      name="Delivered Power (DHP)"
                      dot={false}
                    />
                    <Line
                      type="monotone"
                      dataKey="pInst"
                      stroke="#EF4444"
                      strokeWidth={3}
                      name="Installed Power (P_inst)"
                      dot={false}
                      activeDot={{ r: 6 }}
                    />
                  </>
                )}
              </ComposedChart>
            </ResponsiveContainer>
          </div>

          {/* Resistance vs Froude Number */}
          <div className="bg-card border border-border rounded-lg p-4">
            <h4 className="text-sm font-medium mb-3">Resistance vs Froude Number</h4>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={hmData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />
                <XAxis
                  dataKey="fn"
                  label={{ value: "Froude Number", position: "insideBottom", offset: -5 }}
                  stroke="#6B7280"
                />
                <YAxis
                  label={{ value: "Resistance (kN)", angle: -90, position: "insideLeft" }}
                  stroke="#6B7280"
                />
                <Tooltip
                  contentStyle={{
                    backgroundColor: "#FFF",
                    border: "1px solid #E5E7EB",
                    borderRadius: "0.375rem",
                    padding: "8px",
                  }}
                  formatter={(value: number) => `${value.toFixed(2)} kN`}
                />
                <Legend />
                <Line
                  type="monotone"
                  dataKey="rt"
                  stroke="#EF4444"
                  strokeWidth={2}
                  name="Total Resistance (RT)"
                  dot={false}
                />
                <Line
                  type="monotone"
                  dataKey="rf"
                  stroke="#3B82F6"
                  strokeWidth={2}
                  name="Friction (RF)"
                  dot={false}
                />
                <Line
                  type="monotone"
                  dataKey="rr"
                  stroke="#10B981"
                  strokeWidth={2}
                  name="Residuary (RR)"
                  dot={false}
                />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </div>
      )}

      {/* KCS Benchmark Overlay Chart */}
      {kcsBenchmarkResult &&
        (() => {
          const kcsData = kcsBenchmarkResult.speedGrid.map((speed, idx) => ({
            speed: speed,
            speedKnots: speed / 0.514444,
            rtCalc: kcsBenchmarkResult.calculatedResistance[idx] / 1000, // Convert to kN
            rtRef: kcsBenchmarkResult.referenceResistance[idx] / 1000,
            errorPercent: kcsBenchmarkResult.errorPercent[idx],
          }));

          return (
            <div className="space-y-6">
              <h3 className="text-lg font-semibold text-foreground">KCS Benchmark Validation</h3>

              {/* Reference vs Calculated Resistance Overlay */}
              <div className="bg-card border border-border rounded-lg p-4">
                <h4 className="text-sm font-medium mb-3">
                  Resistance Comparison (Reference vs Calculated)
                  <span
                    className={`ml-2 inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${
                      kcsBenchmarkResult.pass
                        ? "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200"
                        : "bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200"
                    }`}
                  >
                    {kcsBenchmarkResult.pass ? "✓ PASS" : "✗ FAIL"}
                  </span>
                  <span className="ml-2 text-xs text-muted-foreground">
                    MAE: {kcsBenchmarkResult.meanAbsoluteError.toFixed(2)}%, Max:{" "}
                    {kcsBenchmarkResult.maxError.toFixed(2)}%
                  </span>
                </h4>
                <ResponsiveContainer width="100%" height={300}>
                  <ComposedChart data={kcsData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />
                    <XAxis
                      dataKey="speedKnots"
                      label={{ value: "Speed (knots)", position: "insideBottom", offset: -5 }}
                      stroke="#6B7280"
                    />
                    <YAxis
                      label={{ value: "Resistance (kN)", angle: -90, position: "insideLeft" }}
                      stroke="#6B7280"
                    />
                    <Tooltip
                      contentStyle={{
                        backgroundColor: "#FFF",
                        border: "1px solid #E5E7EB",
                        borderRadius: "0.375rem",
                        padding: "8px",
                      }}
                      formatter={(value: number, name: string) => {
                        if (name === "Error %") {
                          return [`${value.toFixed(2)}%`, name];
                        }
                        return [`${value.toFixed(2)} kN`, name];
                      }}
                    />
                    <Legend />
                    <Line
                      type="monotone"
                      dataKey="rtRef"
                      stroke="#6B7280"
                      strokeWidth={3}
                      strokeDasharray="5 5"
                      name="Reference (RT_ref)"
                      dot={{ r: 4 }}
                    />
                    <Line
                      type="monotone"
                      dataKey="rtCalc"
                      stroke="#EF4444"
                      strokeWidth={2}
                      name="Calculated (RT_calc)"
                      dot={{ r: 4 }}
                      activeDot={{ r: 6 }}
                    />
                  </ComposedChart>
                </ResponsiveContainer>
              </div>

              {/* Error Percentage vs Speed */}
              <div className="bg-card border border-border rounded-lg p-4">
                <h4 className="text-sm font-medium mb-3">Error Percentage vs Speed</h4>
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={kcsData} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />
                    <XAxis
                      dataKey="speedKnots"
                      label={{ value: "Speed (knots)", position: "insideBottom", offset: -5 }}
                      stroke="#6B7280"
                    />
                    <YAxis
                      label={{ value: "Error (%)", angle: -90, position: "insideLeft" }}
                      stroke="#6B7280"
                    />
                    <Tooltip
                      contentStyle={{
                        backgroundColor: "#FFF",
                        border: "1px solid #E5E7EB",
                        borderRadius: "0.375rem",
                        padding: "8px",
                      }}
                      formatter={(value: number) => `${value.toFixed(2)}%`}
                    />
                    <Legend />
                    <Line
                      type="monotone"
                      dataKey="errorPercent"
                      stroke="#F59E0B"
                      strokeWidth={2}
                      name="Error %"
                      dot={{ r: 4 }}
                    />
                    {/* Tolerance line */}
                    <Line
                      type="monotone"
                      dataKey={() => kcsBenchmarkResult.maxTolerance}
                      stroke="#EF4444"
                      strokeWidth={1}
                      strokeDasharray="3 3"
                      name={`Max Tolerance (${kcsBenchmarkResult.maxTolerance.toFixed(1)}%)`}
                      dot={false}
                    />
                  </LineChart>
                </ResponsiveContainer>
              </div>
            </div>
          );
        })()}

      {!ittc57Result && !hmResult && !kcsBenchmarkResult && (
        <div className="bg-card border border-border rounded-lg p-8 text-center text-muted-foreground">
          <p>Run calculations to see charts</p>
        </div>
      )}
    </div>
  );
}
