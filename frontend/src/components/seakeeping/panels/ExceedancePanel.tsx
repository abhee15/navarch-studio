import { observer } from "mobx-react-lite";
import { seakeepingStore } from "../../../stores/SeakeepingStore";
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Cell,
} from "recharts";

export const ExceedancePanel = observer(() => {
  const motion = seakeepingStore.motionResponse;

  if (!motion) {
    return (
      <div className="flex items-center justify-center h-96 bg-card border border-border rounded-lg">
        <div className="text-center space-y-2">
          <p className="text-lg font-medium text-muted-foreground">No Exceedance Data</p>
          <p className="text-sm text-muted-foreground">Run motion analysis first</p>
        </div>
      </div>
    );
  }

  // Extract exceedance data
  const exceedances = motion.exceedanceProbabilities;

  // Prepare chart data
  const heaveData = [
    { threshold: "1m", probability: (exceedances.heave1m || 0) * 100, limit: 1.0 },
    { threshold: "2m", probability: (exceedances.heave2m || 0) * 100, limit: 2.0 },
    { threshold: "3m", probability: (exceedances.heave3m || 0) * 100, limit: 3.0 },
  ];

  const pitchData = [
    { threshold: "3°", probability: (exceedances.pitch3deg || 0) * 100, limit: 3.0 },
    { threshold: "5°", probability: (exceedances.pitch5deg || 0) * 100, limit: 5.0 },
    { threshold: "7°", probability: (exceedances.pitch7deg || 0) * 100, limit: 7.0 },
  ];

  const rollData = [
    { threshold: "5°", probability: (exceedances.roll5deg || 0) * 100, limit: 5.0 },
    { threshold: "10°", probability: (exceedances.roll10deg || 0) * 100, limit: 10.0 },
    { threshold: "15°", probability: (exceedances.roll15deg || 0) * 100, limit: 15.0 },
  ];

  // Color based on operability: Green (<10%), Yellow (10-30%), Red (>30%)
  const getBarColor = (prob: number) => {
    if (prob < 10) return "#10b981"; // Green
    if (prob < 30) return "#f59e0b"; // Yellow
    return "#ef4444"; // Red
  };

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold mb-2">Motion Exceedance Analysis</h2>
        <p className="text-sm text-muted-foreground">
          Probability of exceeding motion thresholds (Rayleigh distribution)
        </p>
      </div>

      {/* Operability Legend */}
      <div className="flex space-x-4 text-sm">
        <div className="flex items-center space-x-2">
          <div className="w-4 h-4 bg-green-500 rounded"></div>
          <span>Good (&lt;10%)</span>
        </div>
        <div className="flex items-center space-x-2">
          <div className="w-4 h-4 bg-yellow-500 rounded"></div>
          <span>Moderate (10-30%)</span>
        </div>
        <div className="flex items-center space-x-2">
          <div className="w-4 h-4 bg-red-500 rounded"></div>
          <span>Severe (&gt;30%)</span>
        </div>
      </div>

      {/* Heave Exceedance */}
      <div className="bg-card border border-border rounded-lg p-6">
        <h3 className="text-lg font-semibold mb-4">Heave Exceedance</h3>
        <ResponsiveContainer width="100%" height={250}>
          <BarChart data={heaveData}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="threshold" />
            <YAxis label={{ value: "Probability (%)", angle: -90, position: "insideLeft" }} />
            <Tooltip />
            <Bar dataKey="probability" fill="#3b82f6">
              {heaveData.map((entry, index) => (
                <Cell key={`cell-${index}`} fill={getBarColor(entry.probability)} />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>

      {/* Pitch Exceedance */}
      <div className="bg-card border border-border rounded-lg p-6">
        <h3 className="text-lg font-semibold mb-4">Pitch Exceedance</h3>
        <ResponsiveContainer width="100%" height={250}>
          <BarChart data={pitchData}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="threshold" />
            <YAxis label={{ value: "Probability (%)", angle: -90, position: "insideLeft" }} />
            <Tooltip />
            <Bar dataKey="probability" fill="#10b981">
              {pitchData.map((entry, index) => (
                <Cell key={`cell-${index}`} fill={getBarColor(entry.probability)} />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>

      {/* Roll Exceedance */}
      <div className="bg-card border border-border rounded-lg p-6">
        <h3 className="text-lg font-semibold mb-4">Roll Exceedance</h3>
        <ResponsiveContainer width="100%" height={250}>
          <BarChart data={rollData}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="threshold" />
            <YAxis label={{ value: "Probability (%)", angle: -90, position: "insideLeft" }} />
            <Tooltip />
            <Bar dataKey="probability" fill="#ef4444">
              {rollData.map((entry, index) => (
                <Cell key={`cell-${index}`} fill={getBarColor(entry.probability)} />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>

      {/* Summary Table */}
      <div className="bg-card border border-border rounded-lg p-6">
        <h3 className="text-lg font-semibold mb-4">Operability Summary</h3>
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b">
              <th className="text-left pb-2">Motion</th>
              <th className="text-left pb-2">Threshold</th>
              <th className="text-right pb-2">Exceedance %</th>
              <th className="text-right pb-2">Operability %</th>
            </tr>
          </thead>
          <tbody className="divide-y">
            <tr>
              <td className="py-2">Heave</td>
              <td>2m</td>
              <td className="text-right">{((exceedances.heave2m || 0) * 100).toFixed(1)}%</td>
              <td className="text-right">{((1 - (exceedances.heave2m || 0)) * 100).toFixed(1)}%</td>
            </tr>
            <tr>
              <td className="py-2">Pitch</td>
              <td>5°</td>
              <td className="text-right">{((exceedances.pitch5deg || 0) * 100).toFixed(1)}%</td>
              <td className="text-right">
                {((1 - (exceedances.pitch5deg || 0)) * 100).toFixed(1)}%
              </td>
            </tr>
            <tr>
              <td className="py-2">Roll</td>
              <td>10°</td>
              <td className="text-right">{((exceedances.roll10deg || 0) * 100).toFixed(1)}%</td>
              <td className="text-right">
                {((1 - (exceedances.roll10deg || 0)) * 100).toFixed(1)}%
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      {/* Technical Notes */}
      <div className="bg-muted/50 border border-border rounded-lg p-4 text-sm">
        <div className="font-semibold mb-2">Technical Notes</div>
        <ul className="list-disc list-inside space-y-1 text-muted-foreground">
          <li>Exceedance probabilities calculated using Rayleigh distribution</li>
          <li>Operability % = percentage of time motion stays below threshold</li>
          <li>Green (&lt;10%) indicates good operability in this sea state</li>
          <li>Red (&gt;30%) indicates severe limitations, consider route/speed changes</li>
        </ul>
      </div>
    </div>
  );
});
