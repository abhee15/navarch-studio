import { useState, useMemo } from "react";
import {
  ScatterChart,
  Scatter,
  XAxis,
  YAxis,
  ZAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Cell,
  Legend,
} from "recharts";
import type {
  SpeedDraftMatrixResult,
  DesignPoint,
  TrialPoint,
  MatrixPointDetails,
} from "../../types/resistance";
import { Button } from "../ui/button";
import { X } from "lucide-react";

interface SpeedPowerDraftHeatmapProps {
  matrixResult: SpeedDraftMatrixResult;
  onClose?: () => void;
}

interface HeatmapPoint {
  speedKnots: number;
  draft: number;
  power: number;
  resistance: number;
  froudeNumber: number;
  speedIndex: number;
  draftIndex: number;
}

interface CustomTooltipProps {
  active?: boolean;
  payload?: Array<{
    payload: HeatmapPoint;
  }>;
}

export function SpeedPowerDraftHeatmap({ matrixResult, onClose }: SpeedPowerDraftHeatmapProps) {
  const [selectedPoint, setSelectedPoint] = useState<MatrixPointDetails | null>(null);
  const [colorBy, setColorBy] = useState<"power" | "resistance">("power");
  const [showDesignPoints, setShowDesignPoints] = useState(true);
  const [showTrialPoints, setShowTrialPoints] = useState(true);

  // Transform matrix data into heatmap points
  const heatmapData = useMemo(() => {
    const points: HeatmapPoint[] = [];

    for (let draftIdx = 0; draftIdx < matrixResult.draftGrid.length; draftIdx++) {
      for (let speedIdx = 0; speedIdx < matrixResult.speedGrid.length; speedIdx++) {
        const speed = matrixResult.speedGrid[speedIdx];
        const draft = matrixResult.draftGrid[draftIdx];
        const power = matrixResult.powerMatrix[draftIdx][speedIdx];
        const resistance = matrixResult.resistanceMatrix[draftIdx][speedIdx];
        const froudeNumber = matrixResult.froudeNumberMatrix[draftIdx][speedIdx];

        points.push({
          speedKnots: speed / 0.514444, // Convert m/s to knots
          draft: draft,
          power: power,
          resistance: resistance / 1000, // Convert N to kN
          froudeNumber: froudeNumber,
          speedIndex: speedIdx,
          draftIndex: draftIdx,
        });
      }
    }

    return points;
  }, [matrixResult]);

  // Calculate color scale
  const colorScale = useMemo(() => {
    const values = heatmapData.map((p) => (colorBy === "power" ? p.power : p.resistance));
    const min = Math.min(...values);
    const max = Math.max(...values);

    return { min, max };
  }, [heatmapData, colorBy]);

  // Get color for a value
  const getColor = (value: number) => {
    const { min, max } = colorScale;
    const normalized = (value - min) / (max - min);

    // Heat map gradient: blue -> green -> yellow -> orange -> red
    if (normalized < 0.25) {
      const t = normalized / 0.25;
      return `rgb(${Math.round(33 + (16 - 33) * t)}, ${Math.round(150 + (185 - 150) * t)}, ${Math.round(243 + (129 - 243) * t)})`;
    } else if (normalized < 0.5) {
      const t = (normalized - 0.25) / 0.25;
      return `rgb(${Math.round(16 + (34 - 16) * t)}, ${Math.round(185 + (197 - 185) * t)}, ${Math.round(129 + (94 - 129) * t)})`;
    } else if (normalized < 0.75) {
      const t = (normalized - 0.5) / 0.25;
      return `rgb(${Math.round(34 + (251 - 34) * t)}, ${Math.round(197 + (191 - 197) * t)}, ${Math.round(94 + (36 - 94) * t)})`;
    } else {
      const t = (normalized - 0.75) / 0.25;
      return `rgb(${Math.round(251 + (239 - 251) * t)}, ${Math.round(191 + (68 - 191) * t)}, ${Math.round(36 + (68 - 36) * t)})`;
    }
  };

  // Handle point click
  const handlePointClick = (data: HeatmapPoint) => {
    const pointIndex = data.draftIndex * matrixResult.speedGrid.length + data.speedIndex;
    const details = matrixResult.pointDetails[pointIndex];
    setSelectedPoint(details);
  };

  // Convert design/trial points to chart coordinates
  const designPointsData = useMemo(() => {
    if (!matrixResult.designPoints || !showDesignPoints) return [];
    return matrixResult.designPoints.map((p: DesignPoint) => ({
      speedKnots: p.speed / 0.514444,
      draft: p.draft,
      name: p.name,
    }));
  }, [matrixResult.designPoints, showDesignPoints]);

  const trialPointsData = useMemo(() => {
    if (!matrixResult.trialPoints || !showTrialPoints) return [];
    return matrixResult.trialPoints.map((p: TrialPoint) => ({
      speedKnots: p.speed / 0.514444,
      draft: p.draft,
      name: p.name,
      power: p.measuredPower,
    }));
  }, [matrixResult.trialPoints, showTrialPoints]);

  // Custom tooltip
  const CustomTooltip = ({ active, payload }: CustomTooltipProps) => {
    if (!active || !payload || payload.length === 0) return null;

    const data = payload[0].payload;

    return (
      <div className="bg-card border border-border rounded-lg p-3 shadow-lg">
        <p className="text-sm font-medium mb-1">
          Speed: {data.speedKnots.toFixed(2)} kts ({(data.speedKnots * 0.514444).toFixed(2)} m/s)
        </p>
        <p className="text-sm font-medium mb-1">Draft: {data.draft.toFixed(2)} m</p>
        <p className="text-sm font-medium mb-1">Power: {data.power.toFixed(1)} kW</p>
        <p className="text-sm font-medium mb-1">Resistance: {data.resistance.toFixed(1)} kN</p>
        <p className="text-sm text-muted-foreground">Froude: {data.froudeNumber.toFixed(3)}</p>
        <p className="text-xs text-muted-foreground mt-2 italic">Click for detailed breakdown</p>
      </div>
    );
  };

  return (
    <div className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4">
      <div className="bg-background rounded-lg shadow-xl max-w-7xl w-full max-h-[90vh] overflow-hidden flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-border">
          <div>
            <h2 className="text-xl font-bold text-foreground">Speed-Power-Draft Matrix</h2>
            <p className="text-sm text-muted-foreground">
              {matrixResult.totalPoints} calculation points • {matrixResult.calculationMethod}
            </p>
          </div>
          <div className="flex items-center gap-4">
            {/* Color by selector */}
            <div className="flex items-center gap-2">
              <label className="text-sm font-medium">Color by:</label>
              <select
                value={colorBy}
                onChange={(e) => setColorBy(e.target.value as "power" | "resistance")}
                className="px-3 py-1.5 text-sm border border-border rounded bg-card"
              >
                <option value="power">Power (kW)</option>
                <option value="resistance">Resistance (kN)</option>
              </select>
            </div>

            {/* Overlay toggles */}
            {matrixResult.designPoints && matrixResult.designPoints.length > 0 && (
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={showDesignPoints}
                  onChange={(e) => setShowDesignPoints(e.target.checked)}
                  className="rounded"
                />
                Design Points
              </label>
            )}
            {matrixResult.trialPoints && matrixResult.trialPoints.length > 0 && (
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={showTrialPoints}
                  onChange={(e) => setShowTrialPoints(e.target.checked)}
                  className="rounded"
                />
                Trial Points
              </label>
            )}

            {onClose && (
              <Button variant="ghost" size="sm" onClick={onClose}>
                <X className="h-4 w-4" />
              </Button>
            )}
          </div>
        </div>

        {/* Main Content */}
        <div className="flex flex-1 overflow-hidden">
          {/* Heatmap */}
          <div className="flex-1 p-6 overflow-auto">
            <ResponsiveContainer width="100%" height={600}>
              <ScatterChart margin={{ top: 20, right: 80, bottom: 60, left: 60 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />

                <XAxis
                  type="number"
                  dataKey="speedKnots"
                  name="Speed"
                  unit=" kts"
                  label={{
                    value: "Speed (knots)",
                    position: "insideBottom",
                    offset: -10,
                    style: { fontSize: 14, fontWeight: 600 },
                  }}
                />

                <YAxis
                  type="number"
                  dataKey="draft"
                  name="Draft"
                  unit=" m"
                  label={{
                    value: "Draft (m)",
                    angle: -90,
                    position: "insideLeft",
                    style: { fontSize: 14, fontWeight: 600 },
                  }}
                />

                <ZAxis
                  type="number"
                  dataKey={colorBy}
                  range={[100, 400]}
                  name={colorBy === "power" ? "Power" : "Resistance"}
                  unit={colorBy === "power" ? " kW" : " kN"}
                />

                <Tooltip content={<CustomTooltip />} cursor={{ strokeDasharray: "3 3" }} />

                {/* Main heatmap scatter */}
                <Scatter
                  name={colorBy === "power" ? "Power (kW)" : "Resistance (kN)"}
                  data={heatmapData}
                  shape="square"
                  onClick={handlePointClick}
                  style={{ cursor: "pointer" }}
                >
                  {heatmapData.map((point, index) => {
                    const value = colorBy === "power" ? point.power : point.resistance;
                    return <Cell key={`cell-${index}`} fill={getColor(value)} />;
                  })}
                </Scatter>

                {/* Design points overlay */}
                {designPointsData.length > 0 && (
                  <Scatter
                    name="Design Points"
                    data={designPointsData}
                    shape="diamond"
                    fill="#8B5CF6"
                    stroke="#6D28D9"
                    strokeWidth={2}
                  >
                    {designPointsData.map((_, index) => (
                      <Cell key={`design-${index}`} />
                    ))}
                  </Scatter>
                )}

                {/* Trial points overlay */}
                {trialPointsData.length > 0 && (
                  <Scatter
                    name="Trial Points"
                    data={trialPointsData}
                    shape="triangle"
                    fill="#EF4444"
                    stroke="#DC2626"
                    strokeWidth={2}
                  >
                    {trialPointsData.map((_, index) => (
                      <Cell key={`trial-${index}`} />
                    ))}
                  </Scatter>
                )}

                <Legend
                  wrapperStyle={{ paddingTop: "20px" }}
                  iconSize={12}
                  verticalAlign="bottom"
                />
              </ScatterChart>
            </ResponsiveContainer>

            {/* Color legend */}
            <div className="mt-4 flex items-center justify-center gap-2">
              <span className="text-xs font-medium">Low</span>
              <div className="flex h-6 w-64 rounded overflow-hidden">
                {Array.from({ length: 50 }, (_, i) => {
                  const value = colorScale.min + (i / 49) * (colorScale.max - colorScale.min);
                  return (
                    <div
                      key={i}
                      className="flex-1"
                      style={{ backgroundColor: getColor(value) }}
                      title={value.toFixed(1)}
                    />
                  );
                })}
              </div>
              <span className="text-xs font-medium">High</span>
              <span className="text-xs text-muted-foreground ml-2">
                {colorScale.min.toFixed(0)} - {colorScale.max.toFixed(0)}{" "}
                {colorBy === "power" ? "kW" : "kN"}
              </span>
            </div>
          </div>

          {/* Detail Panel */}
          {selectedPoint && (
            <div className="w-96 border-l border-border bg-card/50 p-6 overflow-auto">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-semibold">Point Details</h3>
                <Button variant="ghost" size="sm" onClick={() => setSelectedPoint(null)}>
                  <X className="h-4 w-4" />
                </Button>
              </div>

              <div className="space-y-4">
                {/* Operating Condition */}
                <div>
                  <h4 className="text-sm font-semibold mb-2 text-muted-foreground">
                    Operating Condition
                  </h4>
                  <div className="space-y-1">
                    <div className="flex justify-between text-sm">
                      <span>Speed:</span>
                      <span className="font-medium">
                        {(selectedPoint.speed / 0.514444).toFixed(2)} kts (
                        {selectedPoint.speed.toFixed(2)} m/s)
                      </span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span>Draft:</span>
                      <span className="font-medium">{selectedPoint.draft.toFixed(2)} m</span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span>Froude Number:</span>
                      <span className="font-medium">{selectedPoint.froudeNumber.toFixed(4)}</span>
                    </div>
                  </div>
                </div>

                {/* Resistance Breakdown */}
                <div>
                  <h4 className="text-sm font-semibold mb-2 text-muted-foreground">
                    Resistance Components
                  </h4>
                  <div className="space-y-1">
                    <div className="flex justify-between text-sm">
                      <span>Friction (RF):</span>
                      <span className="font-medium">
                        {(selectedPoint.frictionResistance / 1000).toFixed(2)} kN
                      </span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span>Residuary (RR):</span>
                      <span className="font-medium">
                        {(selectedPoint.residuaryResistance / 1000).toFixed(2)} kN
                      </span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span>Appendage (RA):</span>
                      <span className="font-medium">
                        {(selectedPoint.appendageResistance / 1000).toFixed(2)} kN
                      </span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span>Correlation (RCA):</span>
                      <span className="font-medium">
                        {(selectedPoint.correlationAllowance / 1000).toFixed(2)} kN
                      </span>
                    </div>
                    {selectedPoint.airResistance > 0 && (
                      <div className="flex justify-between text-sm">
                        <span>Air (RAA):</span>
                        <span className="font-medium">
                          {(selectedPoint.airResistance / 1000).toFixed(2)} kN
                        </span>
                      </div>
                    )}
                    <div className="flex justify-between text-sm pt-2 border-t border-border">
                      <span className="font-semibold">Total (RT):</span>
                      <span className="font-bold">
                        {(selectedPoint.totalResistance / 1000).toFixed(2)} kN
                      </span>
                    </div>
                  </div>
                </div>

                {/* Power */}
                <div>
                  <h4 className="text-sm font-semibold mb-2 text-muted-foreground">Power</h4>
                  <div className="space-y-1">
                    <div className="flex justify-between text-sm">
                      <span>Effective Power (EHP):</span>
                      <span className="font-bold text-lg">
                        {selectedPoint.effectivePower.toFixed(1)} kW
                      </span>
                    </div>
                  </div>
                </div>

                {/* Form Coefficients */}
                <div>
                  <h4 className="text-sm font-semibold mb-2 text-muted-foreground">
                    Form Coefficients (at this draft)
                  </h4>
                  <div className="space-y-1">
                    <div className="flex justify-between text-sm">
                      <span>Block (CB):</span>
                      <span className="font-medium">{selectedPoint.cB.toFixed(4)}</span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span>Prismatic (CP):</span>
                      <span className="font-medium">{selectedPoint.cP.toFixed(4)}</span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span>Midship (CM):</span>
                      <span className="font-medium">{selectedPoint.cM.toFixed(4)}</span>
                    </div>
                  </div>
                </div>

                {/* Non-dimensional */}
                <div>
                  <h4 className="text-sm font-semibold mb-2 text-muted-foreground">
                    Non-dimensional
                  </h4>
                  <div className="space-y-1">
                    <div className="flex justify-between text-sm">
                      <span>Reynolds:</span>
                      <span className="font-medium">
                        {selectedPoint.reynoldsNumber.toExponential(3)}
                      </span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span>CF (ITTC-57):</span>
                      <span className="font-medium">
                        {selectedPoint.frictionCoefficient.toFixed(6)}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
