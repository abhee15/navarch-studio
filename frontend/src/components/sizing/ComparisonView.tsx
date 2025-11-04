import { observer } from "mobx-react-lite";
import { useNavigate } from "react-router-dom";
import type { CandidateDesign } from "../../types/sizing";
import { Button } from "../ui/button";
import { Hull3DThumbnail } from "./visualization/Hull3DThumbnail";
import { Check, Lightbulb, Layers } from "lucide-react";
import {
  RadarChart,
  PolarGrid,
  PolarAngleAxis,
  PolarRadiusAxis,
  Radar,
  Legend,
  ResponsiveContainer,
} from "recharts";

interface ComparisonViewProps {
  candidates: CandidateDesign[];
  onClose: () => void;
}

/**
 * Side-by-Side Comparison View for Hull Candidates
 *
 * Features:
 * - Up to 3 candidates side-by-side
 * - 3D thumbnails
 * - Key dimensions comparison
 * - Performance metrics comparison
 * - Color-coded best/worst values
 * - Responsive layout
 */
export const ComparisonView: React.FC<ComparisonViewProps> = observer(({ candidates, onClose }) => {
  const navigate = useNavigate();

  // Limit to 3 candidates
  const compareCandidates = candidates.slice(0, 3);

  const renderMetric = (
    label: string,
    values: (number | undefined)[],
    unit: string,
    format: (v: number) => string = (v) => v.toFixed(2),
    lowerIsBetter: boolean = false
  ) => {
    const validValues = values.filter((v) => v !== undefined) as number[];
    const min = Math.min(...validValues);
    const max = Math.max(...validValues);

    return (
      <div className="grid grid-cols-4 gap-4 py-3 border-b border-gray-200 dark:border-gray-700">
        <div className="font-medium text-gray-700 dark:text-gray-300">{label}</div>
        {values.map((value, idx) => {
          if (value === undefined) return <div key={idx}>-</div>;

          const isBest = lowerIsBetter ? value === min : value === max;
          const isWorst = lowerIsBetter ? value === max : value === min;

          return (
            <div
              key={idx}
              className={`text-center font-semibold ${
                isBest
                  ? "text-green-600 dark:text-green-400"
                  : isWorst
                    ? "text-red-600 dark:text-red-400"
                    : "text-gray-900 dark:text-white"
              }`}
            >
              {format(value)} {unit}
              {isBest && <Check className="h-3 w-3 ml-1 inline" />}
            </div>
          );
        })}
      </div>
    );
  };

  if (compareCandidates.length === 0) {
    return (
      <div className="p-8 text-center">
        <p className="text-gray-600 dark:text-gray-400">No candidates selected for comparison.</p>
        <Button onClick={onClose} className="mt-4">
          Close
        </Button>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900">
      {/* Header */}
      <div className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 p-4 sticky top-0 z-10 shadow-sm">
        <div className="flex items-center justify-between max-w-7xl mx-auto">
          <div>
            <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
              Candidate Comparison
            </h2>
            <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
              Comparing {compareCandidates.length} hull design
              {compareCandidates.length > 1 ? "s" : ""}
            </p>
          </div>
          <Button onClick={onClose} variant="outline">
            ← Back to Results
          </Button>
        </div>
      </div>

      <div className="max-w-7xl mx-auto p-6">
        {/* 3D Thumbnails */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
          {compareCandidates.map((candidate) => (
            <div
              key={candidate.id}
              className="bg-white dark:bg-gray-800 rounded-lg shadow-lg overflow-hidden"
            >
              <div className="p-4 bg-gradient-to-r from-blue-50 to-cyan-50 dark:from-blue-900/20 dark:to-cyan-900/20">
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    Rank #{candidate.rank}
                  </span>
                  <span className="px-2 py-1 bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300 text-xs font-semibold rounded">
                    {candidate.hullFamily}
                  </span>
                </div>
                <div className="flex items-center justify-between">
                  <span className="text-lg font-bold text-gray-900 dark:text-white">
                    Score: {candidate.score.toFixed(1)}
                  </span>
                  {candidate.isSelected && (
                    <span className="px-2 py-1 bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300 text-xs font-semibold rounded">
                      ★ Selected
                    </span>
                  )}
                </div>
              </div>

              <Hull3DThumbnail candidate={candidate} height={350} />

              <div className="p-4">
                <Button
                  onClick={() => navigate(`/sizing/workspace/${candidate.id}`)}
                  className="w-full"
                  variant="outline"
                >
                  Open Workspace →
                </Button>
              </div>
            </div>
          ))}
        </div>

        {/* Comparison Table */}
        <div className="bg-white dark:bg-gray-800 rounded-lg shadow-lg overflow-hidden">
          <div className="p-4 bg-gradient-to-r from-gray-50 to-slate-50 dark:from-gray-900 dark:to-slate-900 border-b border-gray-200 dark:border-gray-700">
            <h3 className="text-lg font-bold text-gray-900 dark:text-white">Detailed Comparison</h3>
            <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
              Green with checkmark = Best value • Red = Worst value
            </p>
          </div>

          <div className="p-6">
            {/* Principal Dimensions */}
            <div className="mb-6">
              <h4 className="text-md font-bold text-gray-900 dark:text-white mb-3 flex items-center gap-2">
                <span className="w-2 h-2 bg-blue-600 rounded-full"></span>
                Principal Dimensions
              </h4>
              {renderMetric(
                "Length (Lpp)",
                compareCandidates.map((c) => c.lppM),
                "m"
              )}
              {renderMetric(
                "Beam",
                compareCandidates.map((c) => c.beamM),
                "m"
              )}
              {renderMetric(
                "Draft",
                compareCandidates.map((c) => c.draftM),
                "m",
                (v) => v.toFixed(2),
                true
              )}
              {renderMetric(
                "Depth",
                compareCandidates.map((c) => c.depthM),
                "m"
              )}
              {renderMetric(
                "LOA (est.)",
                compareCandidates.map((c) => c.loaM),
                "m"
              )}
            </div>

            {/* Form Coefficients */}
            <div className="mb-6">
              <h4 className="text-md font-bold text-gray-900 dark:text-white mb-3 flex items-center gap-2">
                <span className="w-2 h-2 bg-purple-600 rounded-full"></span>
                Form Coefficients
              </h4>
              {renderMetric(
                "Block Coeff. (Cb)",
                compareCandidates.map((c) => c.cb),
                "",
                (v) => v.toFixed(3)
              )}
              {renderMetric(
                "Prismatic (Cp)",
                compareCandidates.map((c) => c.cp),
                "",
                (v) => v.toFixed(3)
              )}
              {renderMetric(
                "Waterplane (Cwp)",
                compareCandidates.map((c) => c.cwp),
                "",
                (v) => v.toFixed(3)
              )}
              {renderMetric(
                "Midship (Cm)",
                compareCandidates.map((c) => c.cm),
                "",
                (v) => v.toFixed(3)
              )}
            </div>

            {/* Performance */}
            <div className="mb-6">
              <h4 className="text-md font-bold text-gray-900 dark:text-white mb-3 flex items-center gap-2">
                <span className="w-2 h-2 bg-green-600 rounded-full"></span>
                Performance Metrics
              </h4>
              {renderMetric(
                "Displacement",
                compareCandidates.map((c) => c.dispT),
                "tonnes",
                (v) => v.toFixed(0)
              )}
              {renderMetric(
                "Froude Number",
                compareCandidates.map((c) => c.fn),
                "",
                (v) => v.toFixed(3)
              )}
              {renderMetric(
                "EHP",
                compareCandidates.map((c) => c.ehpKw),
                "kW",
                (v) => v.toFixed(0),
                true
              )}
              {renderMetric(
                "SHP (est.)",
                compareCandidates.map((c) => c.shpKw),
                "kW",
                (v) => v.toFixed(0),
                true
              )}
            </div>

            {/* Stability Estimates */}
            <div>
              <h4 className="text-md font-bold text-gray-900 dark:text-white mb-3 flex items-center gap-2">
                <span className="w-2 h-2 bg-orange-600 rounded-full"></span>
                Stability Estimates
              </h4>
              {renderMetric(
                "KB",
                compareCandidates.map((c) => c.kbM),
                "m",
                (v) => v.toFixed(2)
              )}
              {renderMetric(
                "LCB (% Lpp)",
                compareCandidates.map((c) => c.lcbPctLpp),
                "%",
                (v) => v.toFixed(1)
              )}
              {renderMetric(
                "GM (est.)",
                compareCandidates.map((c) => c.gmEstM),
                "m",
                (v) => v.toFixed(2)
              )}
            </div>
          </div>
        </div>

        {/* Radar Chart - Multi-Attribute Visualization */}
        <div className="mt-6 bg-card border border-border rounded-lg p-6">
          <div className="flex items-center gap-2 mb-4">
            <Layers className="h-5 w-5 text-primary" />
            <h4 className="font-bold text-foreground">Multi-Attribute Comparison</h4>
          </div>
          <ResponsiveContainer width="100%" height={400}>
            <RadarChart data={generateRadarData(compareCandidates)}>
              <PolarGrid stroke="hsl(var(--border))" />
              <PolarAngleAxis
                dataKey="attribute"
                tick={{ fontSize: 11, fill: "hsl(var(--foreground))" }}
              />
              <PolarRadiusAxis
                angle={90}
                domain={[0, 100]}
                tick={{ fontSize: 10, fill: "hsl(var(--muted-foreground))" }}
              />
              {compareCandidates.map((candidate, idx) => (
                <Radar
                  key={candidate.id}
                  name={`Rank #${candidate.rank}`}
                  dataKey={`candidate${idx}`}
                  stroke={getRadarColor(idx)}
                  fill={getRadarColor(idx)}
                  fillOpacity={0.3}
                  strokeWidth={2}
                />
              ))}
              <Legend wrapperStyle={{ fontSize: "12px" }} iconType="circle" />
            </RadarChart>
          </ResponsiveContainer>
          <p className="text-xs text-muted-foreground mt-4 text-center">
            Normalized scores (0-100) across key attributes. Larger area = better overall
            performance.
          </p>
        </div>

        {/* Summary */}
        <div className="mt-6 bg-blue-50 dark:bg-blue-900/20 rounded-lg p-6">
          <h4 className="font-bold text-blue-900 dark:text-blue-300 mb-2 flex items-center gap-2">
            <Lightbulb className="h-4 w-4" />
            Comparison Tips
          </h4>
          <ul className="text-sm text-blue-800 dark:text-blue-400 space-y-1">
            <li>
              • <strong>Green values</strong> indicate the best performing candidate for that metric
            </li>
            <li>
              • <strong>Lower EHP/SHP</strong> means better fuel efficiency
            </li>
            <li>
              • <strong>Higher GM</strong> generally means better initial stability
            </li>
            <li>• Consider trade-offs between dimensions, efficiency, and constraints</li>
            <li>• Click "Open Workspace" to explore each design in detail</li>
          </ul>
        </div>
      </div>
    </div>
  );
});

/**
 * Generate radar chart data from candidates
 * Normalizes metrics to 0-100 scale for comparison
 */
function generateRadarData(candidates: CandidateDesign[]) {
  // Define attributes to compare
  const attributes = [
    { key: "score", label: "Overall Score", higherIsBetter: true },
    { key: "efficiency", label: "Efficiency", higherIsBetter: true },
    { key: "stability", label: "Stability", higherIsBetter: true },
    { key: "speed", label: "Speed Cap.", higherIsBetter: true },
    { key: "buildability", label: "Buildability", higherIsBetter: true },
  ];

  // Extract values and normalize
  const radarData = attributes.map((attr) => {
    const dataPoint: any = { attribute: attr.label };

    candidates.forEach((candidate, idx) => {
      let value = 0;

      // Map attributes to candidate properties
      switch (attr.key) {
        case "score":
          value = candidate.score * 100; // Already 0-1, convert to 0-100
          break;
        case "efficiency":
          // Inverse of Froude number (lower Fn = more efficient)
          value = candidate.fn ? Math.max(0, 100 - candidate.fn * 300) : 50;
          break;
        case "stability":
          // GM estimate (normalize to 0-100)
          value = candidate.gmEstM ? Math.min(100, (candidate.gmEstM / 5) * 100) : 50;
          break;
        case "speed":
          // Max speed capability (hull speed)
          const hullSpeed = Math.sqrt(candidate.lwlM) * 2.43; // knots
          value = Math.min(100, (hullSpeed / 30) * 100);
          break;
        case "buildability":
          // Simpler form = easier to build (inverse of L/B ratio complexity)
          const lOverB = candidate.lppM / candidate.beamM;
          value = Math.max(0, 100 - Math.abs(lOverB - 6) * 10); // Optimal L/B ≈ 6
          break;
      }

      dataPoint[`candidate${idx}`] = Math.max(0, Math.min(100, value));
    });

    return dataPoint;
  });

  return radarData;
}

/**
 * Get distinct color for each candidate in radar chart
 */
function getRadarColor(index: number): string {
  const colors = [
    "hsl(var(--primary))", // Blue
    "hsl(var(--accent))", // Teal
    "hsl(var(--secondary))", // Purple
  ];
  return colors[index % colors.length];
}
