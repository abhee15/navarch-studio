import { useState, useEffect, useRef } from "react";
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
  ReferenceDot,
  ReferenceArea,
} from "recharts";
import { stabilityApi, type StabilityCurve } from "../../../../services/stabilityApi";
import type { VesselDetails, Loadcase } from "../../../../types/hydrostatics";
import { settingsStore } from "../../../../stores/SettingsStore";
import { getErrorMessage } from "../../../../types/errors";
import { getUnitSymbol } from "../../../../utils/unitSymbols";

interface GZCurvePanelProps {
  vesselId: string;
  vessel?: VesselDetails | null;
}

interface StabilityMetrics {
  maxGZ: number;
  angleAtMaxGZ: number;
  rangeOfPositiveStability: [number, number];
  areaA1: number; // Area under GZ curve from 0° to 30°
  areaA2: number; // Area under GZ curve from 30° to vanishing angle
  vanishingAngle: number; // Angle where GZ becomes zero/negative
}

export const GZCurvePanel = observer(({ vesselId, vessel }: GZCurvePanelProps) => {
  const chartRef = useRef<HTMLDivElement>(null);
  const [curve, setCurve] = useState<StabilityCurve | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loadcases, setLoadcases] = useState<Loadcase[]>([]);
  const [selectedLoadcaseId, setSelectedLoadcaseId] = useState<string>("");
  const [draft, setDraft] = useState<number>(vessel?.designDraft || 0);
  const [minAngle, setMinAngle] = useState<number>(0);
  const [maxAngle, setMaxAngle] = useState<number>(90);
  const [angleIncrement, setAngleIncrement] = useState<number>(1);
  const [method, setMethod] = useState<"WallSided" | "FullImmersion">("WallSided");
  const [showIMOCriteria, setShowIMOCriteria] = useState(false);
  const [metrics, setMetrics] = useState<StabilityMetrics | null>(null);

  const displayUnits = settingsStore.preferredUnits;
  const lengthUnit = getUnitSymbol(displayUnits, "Length");

  // Load loadcases on mount
  useEffect(() => {
    const loadLoadcases = async () => {
      try {
        const { loadcasesApi } = await import("../../../../services/hydrostaticsApi");
        const data = await loadcasesApi.list(vesselId);
        setLoadcases(data.loadcases);
        if (data.loadcases.length > 0 && !selectedLoadcaseId) {
          setSelectedLoadcaseId(data.loadcases[0].id);
        }
      } catch (err) {
        console.error("Error loading loadcases:", err);
      }
    };
    loadLoadcases();
  }, [vesselId, selectedLoadcaseId]);

  // Update draft when vessel changes
  useEffect(() => {
    if (vessel?.designDraft && draft === 0) {
      setDraft(vessel.designDraft);
    }
  }, [vessel, draft]);

  const computeStabilityMetrics = (curveData: StabilityCurve): StabilityMetrics => {
    const points = curveData.points;

    // Find max GZ
    const maxGZ = curveData.maxGZ;
    const angleAtMaxGZ = curveData.angleAtMaxGZ;

    // Find vanishing angle (where GZ becomes zero or negative after initial positive region)
    let vanishingAngle = maxAngle;
    for (let i = 1; i < points.length; i++) {
      if (points[i].gz <= 0 && points[i - 1].gz > 0) {
        // Linear interpolation to find exact zero crossing
        const gz1 = points[i - 1].gz;
        const gz2 = points[i].gz;
        const angle1 = points[i - 1].heelAngle;
        const angle2 = points[i].heelAngle;
        vanishingAngle = angle1 - (gz1 * (angle2 - angle1)) / (gz2 - gz1);
        break;
      }
    }

    // Find range of positive stability (typically from 0 to vanishing angle)
    const rangeOfPositiveStability: [number, number] = [0, vanishingAngle];

    // Compute area A1 (0° to 30° or angle of deck edge immersion)
    const angle30Index = points.findIndex((p) => p.heelAngle >= 30);
    const areaA1 = computeArea(points.slice(0, angle30Index + 1));

    // Compute area A2 (30° to vanishing angle)
    const vanishingIndex = points.findIndex((p) => p.heelAngle >= vanishingAngle);
    const areaA2 = computeArea(
      points.slice(angle30Index, vanishingIndex > 0 ? vanishingIndex : points.length)
    );

    return {
      maxGZ,
      angleAtMaxGZ,
      rangeOfPositiveStability,
      areaA1,
      areaA2,
      vanishingAngle,
    };
  };

  const computeArea = (points: Array<{ heelAngle: number; gz: number }>): number => {
    // Trapezoidal rule for integration
    // Convert angle to radians and integrate: Area = ∫ GZ dθ
    if (points.length < 2) return 0;

    let area = 0;
    for (let i = 1; i < points.length; i++) {
      const angle1Rad = (points[i - 1].heelAngle * Math.PI) / 180;
      const angle2Rad = (points[i].heelAngle * Math.PI) / 180;
      const gz1 = points[i - 1].gz;
      const gz2 = points[i].gz;
      const deltaAngle = angle2Rad - angle1Rad;
      area += 0.5 * (gz1 + gz2) * deltaAngle;
    }
    return area;
  };

  const loadGZCurve = async () => {
    if (!selectedLoadcaseId) {
      setError("Please select a loadcase first");
      return;
    }

    if (draft <= 0) {
      setError("Please enter a valid draft");
      return;
    }

    try {
      setLoading(true);
      setError(null);

      const response = await stabilityApi.generateGZCurve(vesselId, {
        loadcaseId: selectedLoadcaseId,
        minAngle,
        maxAngle,
        angleIncrement,
        method,
        draft,
      });

      setCurve(response);

      // Compute metrics
      const computedMetrics = computeStabilityMetrics(response);
      setMetrics(computedMetrics);
    } catch (err) {
      setError(getErrorMessage(err));
      setCurve(null);
      setMetrics(null);
    } finally {
      setLoading(false);
    }
  };

  // Auto-load when parameters change (debounced)
  useEffect(() => {
    if (!selectedLoadcaseId || draft <= 0) {
      return;
    }

    const timer = setTimeout(() => {
      loadGZCurve();
    }, 500);

    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedLoadcaseId, draft, minAngle, maxAngle, angleIncrement, method]);

  const exportChart = async (format: "png" | "svg" | "pdf") => {
    if (!curve || !chartRef.current) {
      setError("No chart to export");
      return;
    }

    try {
      const vesselName = vessel?.name || "Vessel";
      const displacement = curve.displacement.toFixed(0);
      const date = new Date().toISOString().split("T")[0];
      const filename = `GZ_${vesselName}_${displacement}kg_${date}.${format}`;

      if (format === "svg") {
        // Export SVG
        const svgElement = chartRef.current.querySelector("svg");
        if (svgElement) {
          const svgData = new XMLSerializer().serializeToString(svgElement);
          const blob = new Blob([svgData], { type: "image/svg+xml" });
          downloadBlob(blob, filename);
        }
      } else if (format === "png") {
        // Export PNG
        const svgElement = chartRef.current.querySelector("svg");
        if (svgElement) {
          const canvas = document.createElement("canvas");
          const ctx = canvas.getContext("2d");
          const svgData = new XMLSerializer().serializeToString(svgElement);
          const img = new Image();
          const svgBlob = new Blob([svgData], { type: "image/svg+xml;charset=utf-8" });
          const url = URL.createObjectURL(svgBlob);

          img.onload = () => {
            canvas.width = svgElement.clientWidth * 2; // 2x for better quality
            canvas.height = svgElement.clientHeight * 2;
            ctx?.scale(2, 2);
            ctx?.drawImage(img, 0, 0);
            URL.revokeObjectURL(url);
            canvas.toBlob((blob) => {
              if (blob) {
                downloadBlob(blob, filename);
              }
            });
          };
          img.src = url;
        }
      } else if (format === "pdf") {
        // For PDF export, we'd need a library like jsPDF
        // For now, show a message
        setError("PDF export requires additional setup. Please use PNG or SVG for now.");
      }
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const downloadBlob = (blob: Blob, filename: string) => {
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  if (loadcases.length === 0) {
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
              d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"
            />
          </svg>
          <h4 className="text-xs font-medium text-foreground mb-1">No Loadcases</h4>
          <p className="text-[10px] text-muted-foreground">
            Create a loadcase with KG defined to compute GZ curve.
          </p>
        </div>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center h-full">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-2"></div>
          <p className="text-xs text-muted-foreground">Computing GZ curve...</p>
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
          <h4 className="text-xs font-medium text-foreground mb-1">Error</h4>
          <p className="text-[10px] text-muted-foreground mb-2">{error}</p>
          <button
            onClick={loadGZCurve}
            className="text-[10px] px-2 py-1 bg-primary text-primary-foreground rounded hover:bg-primary/90"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col">
      {/* Controls */}
      <div className="flex-shrink-0 mb-3 space-y-2">
        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="text-[10px] font-medium text-foreground block mb-1">Loadcase</label>
            <select
              value={selectedLoadcaseId}
              onChange={(e) => setSelectedLoadcaseId(e.target.value)}
              className="w-full border border-border bg-background text-foreground rounded text-xs py-1 px-2 focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {loadcases.map((lc) => (
                <option key={lc.id} value={lc.id}>
                  {lc.name}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label className="text-[10px] font-medium text-foreground block mb-1">
              Draft ({lengthUnit})
            </label>
            <input
              type="number"
              value={draft}
              onChange={(e) => setDraft(Number(e.target.value))}
              min="0"
              step="0.1"
              className="w-full border border-border bg-background text-foreground rounded text-xs py-1 px-2 focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>
        </div>
        <div className="grid grid-cols-4 gap-2">
          <div>
            <label className="text-[10px] font-medium text-foreground block mb-1">
              Min Angle (°)
            </label>
            <input
              type="number"
              value={minAngle}
              onChange={(e) => setMinAngle(Number(e.target.value))}
              min="0"
              max="90"
              step="1"
              className="w-full border border-border bg-background text-foreground rounded text-xs py-1 px-2 focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>
          <div>
            <label className="text-[10px] font-medium text-foreground block mb-1">
              Max Angle (°)
            </label>
            <input
              type="number"
              value={maxAngle}
              onChange={(e) => setMaxAngle(Number(e.target.value))}
              min="0"
              max="90"
              step="1"
              className="w-full border border-border bg-background text-foreground rounded text-xs py-1 px-2 focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>
          <div>
            <label className="text-[10px] font-medium text-foreground block mb-1">Step (°)</label>
            <input
              type="number"
              value={angleIncrement}
              onChange={(e) => setAngleIncrement(Number(e.target.value))}
              min="0.1"
              max="5"
              step="0.1"
              className="w-full border border-border bg-background text-foreground rounded text-xs py-1 px-2 focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>
          <div>
            <label className="text-[10px] font-medium text-foreground block mb-1">Method</label>
            <select
              value={method}
              onChange={(e) => setMethod(e.target.value as "WallSided" | "FullImmersion")}
              className="w-full border border-border bg-background text-foreground rounded text-xs py-1 px-2 focus:outline-none focus:ring-2 focus:ring-ring"
            >
              <option value="WallSided">Wall-Sided</option>
              <option value="FullImmersion">Full Immersion</option>
            </select>
          </div>
        </div>

        {/* Export and IMO Toggle */}
        <div className="flex items-center justify-between pt-1">
          <div className="flex items-center gap-2">
            <label className="flex items-center gap-1 text-[10px] text-foreground cursor-pointer">
              <input
                type="checkbox"
                checked={showIMOCriteria}
                onChange={(e) => setShowIMOCriteria(e.target.checked)}
                className="w-3 h-3"
              />
              Show IMO Criteria
            </label>
          </div>
          <div className="flex gap-1">
            <button
              onClick={() => exportChart("svg")}
              disabled={!curve}
              className="text-[10px] px-2 py-1 bg-secondary text-secondary-foreground rounded hover:bg-secondary/80 disabled:opacity-50 disabled:cursor-not-allowed"
              title="Export as SVG"
            >
              SVG
            </button>
            <button
              onClick={() => exportChart("png")}
              disabled={!curve}
              className="text-[10px] px-2 py-1 bg-secondary text-secondary-foreground rounded hover:bg-secondary/80 disabled:opacity-50 disabled:cursor-not-allowed"
              title="Export as PNG"
            >
              PNG
            </button>
          </div>
        </div>
      </div>

      {/* Chart */}
      <div className="flex-1 min-h-0" ref={chartRef}>
        {curve && curve.points.length > 0 ? (
          <ResponsiveContainer width="100%" height="100%">
            <LineChart
              data={curve.points.map((p) => ({
                angle: p.heelAngle,
                gz: p.gz,
                kn: p.kn,
              }))}
            >
              <CartesianGrid
                strokeDasharray="3 3"
                stroke="#e5e7eb"
                className="dark:stroke-gray-700"
              />
              <XAxis
                dataKey="angle"
                label={{
                  value: `Heel Angle (°)`,
                  position: "insideBottom",
                  offset: -5,
                  style: { fontSize: "10px" },
                }}
                tick={{ fontSize: 9 }}
                stroke="currentColor"
                className="text-muted-foreground"
              />
              <YAxis
                label={{
                  value: `GZ (${lengthUnit})`,
                  angle: -90,
                  position: "insideLeft",
                  style: { fontSize: "10px" },
                }}
                tick={{ fontSize: 9 }}
                stroke="currentColor"
                className="text-muted-foreground"
              />
              <Tooltip
                contentStyle={{ fontSize: "10px", padding: "4px 6px" }}
                formatter={(value: number) => value.toFixed(3)}
                labelFormatter={(label) => `Angle: ${label}°`}
              />
              <Legend wrapperStyle={{ fontSize: "9px", paddingTop: "4px" }} iconSize={8} />

              {/* GZ Curve */}
              <Line
                type="monotone"
                dataKey="gz"
                stroke="#3B82F6"
                strokeWidth={2}
                dot={false}
                name="GZ"
              />

              {/* KN Curve (optional, lighter) */}
              <Line
                type="monotone"
                dataKey="kn"
                stroke="#9CA3AF"
                strokeWidth={1}
                dot={false}
                name="KN"
                strokeDasharray="5 5"
              />

              {/* Max GZ Marker */}
              {metrics && (
                <ReferenceDot
                  x={metrics.angleAtMaxGZ}
                  y={metrics.maxGZ}
                  r={4}
                  fill="#EF4444"
                  stroke="#fff"
                  strokeWidth={1}
                  label={{
                    value: `Max GZ: ${metrics.maxGZ.toFixed(3)}${lengthUnit} @ ${metrics.angleAtMaxGZ.toFixed(1)}°`,
                    position: "top",
                    fontSize: 9,
                    fill: "#EF4444",
                  }}
                />
              )}

              {/* Vanishing Angle Marker */}
              {metrics && metrics.vanishingAngle < maxAngle && (
                <ReferenceLine
                  x={metrics.vanishingAngle}
                  stroke="#F59E0B"
                  strokeDasharray="3 3"
                  label={{
                    value: `Vanishing: ${metrics.vanishingAngle.toFixed(1)}°`,
                    position: "top",
                    fontSize: 9,
                    fill: "#F59E0B",
                  }}
                />
              )}

              {/* IMO Criteria Overlays */}
              {showIMOCriteria && (
                <>
                  {/* IMO A.749(18) - Area under GZ curve */}
                  <ReferenceArea
                    x1={0}
                    x2={30}
                    fill="#10B981"
                    fillOpacity={0.1}
                    label={{
                      value: "A1 (0-30°)",
                      position: "insideTop",
                      fontSize: 8,
                      fill: "#10B981",
                    }}
                  />
                  <ReferenceArea
                    x1={30}
                    x2={metrics?.vanishingAngle || 40}
                    fill="#3B82F6"
                    fillOpacity={0.1}
                    label={{
                      value: "A2 (30°-vanish)",
                      position: "insideTop",
                      fontSize: 8,
                      fill: "#3B82F6",
                    }}
                  />

                  {/* IMO minimum GM line (reference at 0.15m typical) */}
                  <ReferenceLine
                    y={0.15}
                    stroke="#9CA3AF"
                    strokeDasharray="2 2"
                    label={{
                      value: "IMO Min GZ (0.15m typical)",
                      position: "right",
                      fontSize: 8,
                      fill: "#9CA3AF",
                    }}
                  />
                </>
              )}

              {/* Zero line */}
              <ReferenceLine y={0} stroke="#000" strokeWidth={1} />
            </LineChart>
          </ResponsiveContainer>
        ) : (
          <div className="flex items-center justify-center h-full">
            <div className="text-center text-muted-foreground">
              <p className="text-xs">No curve data</p>
              <p className="text-[10px] mt-1">
                Select a loadcase and draft, then the curve will be computed automatically.
              </p>
            </div>
          </div>
        )}
      </div>

      {/* Metrics Display */}
      {metrics && curve && (
        <div className="flex-shrink-0 mt-2 pt-2 border-t border-border">
          <div className="grid grid-cols-3 gap-2 text-[10px]">
            <div>
              <span className="text-muted-foreground">Max GZ:</span>{" "}
              <span className="font-medium text-foreground">
                {metrics.maxGZ.toFixed(3)} {lengthUnit}
              </span>
            </div>
            <div>
              <span className="text-muted-foreground">@ Angle:</span>{" "}
              <span className="font-medium text-foreground">
                {metrics.angleAtMaxGZ.toFixed(1)}°
              </span>
            </div>
            <div>
              <span className="text-muted-foreground">Vanishing:</span>{" "}
              <span className="font-medium text-foreground">
                {metrics.vanishingAngle.toFixed(1)}°
              </span>
            </div>
            <div>
              <span className="text-muted-foreground">Area A1:</span>{" "}
              <span className="font-medium text-foreground">
                {metrics.areaA1.toFixed(4)} {lengthUnit}·rad
              </span>
            </div>
            <div>
              <span className="text-muted-foreground">Area A2:</span>{" "}
              <span className="font-medium text-foreground">
                {metrics.areaA2.toFixed(4)} {lengthUnit}·rad
              </span>
            </div>
            <div>
              <span className="text-muted-foreground">GMT:</span>{" "}
              <span className="font-medium text-foreground">
                {curve.initialGMT.toFixed(3)} {lengthUnit}
              </span>
            </div>
          </div>
          <p className="text-[10px] text-muted-foreground mt-2">
            <strong>GZ curve</strong> shows the righting arm (GZ = KN - KG·sin θ) vs. heel angle.
            Areas A1 and A2 are used in IMO stability criteria.
          </p>
        </div>
      )}
    </div>
  );
});
