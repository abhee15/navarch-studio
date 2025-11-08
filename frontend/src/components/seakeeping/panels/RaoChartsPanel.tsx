import { observer } from "mobx-react-lite";
import { seakeepingStore } from "../../../stores/SeakeepingStore";
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

export const RaoChartsPanel = observer(() => {
  const raoResults = seakeepingStore.raoResults;

  if (!raoResults) {
    return (
      <div className="flex items-center justify-center h-96 bg-card border border-border rounded-lg">
        <div className="text-center space-y-2">
          <p className="text-lg font-medium text-muted-foreground">No RAO Results</p>
          <p className="text-sm text-muted-foreground">
            Configure parameters in the sidebar and click "Calculate RAOs"
          </p>
        </div>
      </div>
    );
  }

  // Transform data for Recharts
  const chartData = raoResults.frequency.map((freq, idx) => ({
    frequency: freq,
    heave: raoResults.heaveRao[idx],
    pitch: raoResults.pitchRao[idx],
    roll: raoResults.rollRao[idx],
  }));

  // Find peaks
  const heaveMax = Math.max(...raoResults.heaveRao);
  const pitchMax = Math.max(...raoResults.pitchRao);
  const rollMax = Math.max(...raoResults.rollRao);

  const heaveMaxIdx = raoResults.heaveRao.indexOf(heaveMax);
  const pitchMaxIdx = raoResults.pitchRao.indexOf(pitchMax);

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold mb-2">Response Amplitude Operators (RAOs)</h2>
        <p className="text-sm text-muted-foreground">
          Motion response per unit wave amplitude across frequency range
        </p>
      </div>

      {/* Statistics Cards */}
      <div className="grid grid-cols-3 gap-4">
        <div className="bg-card border border-border rounded-lg p-4">
          <div className="text-xs text-muted-foreground mb-1">Peak Heave RAO</div>
          <div className="text-2xl font-bold text-blue-600">{heaveMax.toFixed(3)}</div>
          <div className="text-xs text-muted-foreground mt-1">
            at ω = {raoResults.frequency[heaveMaxIdx].toFixed(2)} rad/s
          </div>
        </div>

        <div className="bg-card border border-border rounded-lg p-4">
          <div className="text-xs text-muted-foreground mb-1">Peak Pitch RAO</div>
          <div className="text-2xl font-bold text-green-600">{pitchMax.toFixed(3)}</div>
          <div className="text-xs text-muted-foreground mt-1">
            at ω = {raoResults.frequency[pitchMaxIdx].toFixed(2)} rad/s
          </div>
        </div>

        <div className="bg-card border border-border rounded-lg p-4">
          <div className="text-xs text-muted-foreground mb-1">Peak Roll RAO</div>
          <div className="text-2xl font-bold text-red-600">{rollMax.toFixed(3)}</div>
          <div className="text-xs text-muted-foreground mt-1">Simplified model</div>
        </div>
      </div>

      {/* RAO Chart */}
      <div className="bg-card border border-border rounded-lg p-6">
        <h3 className="text-lg font-semibold mb-4">RAO vs Frequency</h3>
        <ResponsiveContainer width="100%" height={400}>
          <LineChart data={chartData}>
            <CartesianGrid strokeDasharray="3 3" className="stroke-muted" />
            <XAxis
              dataKey="frequency"
              label={{ value: "Frequency (rad/s)", position: "insideBottom", offset: -5 }}
              className="text-xs"
            />
            <YAxis
              label={{ value: "RAO (m/m or rad/m)", angle: -90, position: "insideLeft" }}
              className="text-xs"
            />
            <Tooltip
              contentStyle={{
                backgroundColor: "hsl(var(--card))",
                border: "1px solid hsl(var(--border))",
              }}
            />
            <Legend />
            <Line
              type="monotone"
              dataKey="heave"
              stroke="#3b82f6"
              name="Heave (m/m)"
              strokeWidth={2}
              dot={false}
            />
            <Line
              type="monotone"
              dataKey="pitch"
              stroke="#10b981"
              name="Pitch (rad/m)"
              strokeWidth={2}
              dot={false}
            />
            <Line
              type="monotone"
              dataKey="roll"
              stroke="#ef4444"
              name="Roll (rad/m)"
              strokeWidth={2}
              dot={false}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>

      {/* Technical Notes */}
      <div className="bg-muted/50 border border-border rounded-lg p-4 text-sm">
        <div className="font-semibold mb-2">Technical Notes</div>
        <ul className="list-disc list-inside space-y-1 text-muted-foreground">
          <li>RAOs calculated using strip theory (simplified elliptic formulas)</li>
          <li>Valid for slender hulls (L/B &gt; 5) and moderate speeds (Fn &lt; 0.35)</li>
          <li>Resonance peaks indicate natural frequencies of the vessel</li>
          <li>Roll RAO uses simplified damping (Phase 5 will add Ikeda method)</li>
        </ul>
      </div>
    </div>
  );
});
