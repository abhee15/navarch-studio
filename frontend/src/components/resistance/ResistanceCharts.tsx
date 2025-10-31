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

export function ResistanceCharts({
  ittc57Result,
  hmResult,
  powerResult,
  kcsBenchmarkResult,
}: ResistanceChartsProps) {
  const [selectedSpeedIndex, setSelectedSpeedIndex] = useState<number | null>(null);

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
                onClick={(data: any) => {
                  if (data?.activeLabel) {
                    const speedValue = parseFloat(data.activeLabel);
                    if (!isNaN(speedValue)) {
                      const idx = hmData.findIndex((d) => Math.abs(d.speedKnots - speedValue) < 0.01);
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
                        dataKey={(d: any) => (d.name === comp.name ? d.value : 0)}
                        stackId="1"
                        fill={comp.color}
                      />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
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
      {kcsBenchmarkResult && (() => {
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
      )}

      {!ittc57Result && !hmResult && !kcsBenchmarkResult && (
        <div className="bg-card border border-border rounded-lg p-8 text-center text-muted-foreground">
          <p>Run calculations to see charts</p>
        </div>
      )}
    </div>
  );
}
