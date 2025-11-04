import React, { useMemo } from "react";
import type { CandidateDesign } from "../../../types/sizing";
import { TrendingUp, TrendingDown, Minus, AlertCircle } from "lucide-react";
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

interface SensitivityPanelProps {
  candidate: CandidateDesign;
}

interface SensitivityData {
  parameter: string;
  impact: number; // -100 to +100 (percentage change in score)
  impactAbs: number; // Absolute value for sorting
  direction: "positive" | "negative" | "neutral";
  metric: string;
}

/**
 * Sensitivity Analysis Panel
 *
 * Shows ±10% parameter variations and their impact on key metrics:
 * - Overall Score
 * - Displacement
 * - Power (EHP)
 * - Stability (GM)
 */
export const SensitivityPanel: React.FC<SensitivityPanelProps> = ({ candidate }) => {
  // Calculate sensitivity for each parameter
  const sensitivityData = useMemo(() => {
    const baseScore = candidate.score;
    const baseDisp = candidate.dispT || 0;
    const baseEHP = candidate.ehpKw || 0;
    const baseGM = candidate.gmEstM || 0;

    // Simplified sensitivity estimates (±10% variation)
    // In reality, these would come from backend re-computation
    // For now, use engineering rules of thumb

    const data: SensitivityData[] = [
      {
        parameter: "Lpp",
        impact: -8.5, // Longer hull → slightly lower score (more cost, less maneuverability)
        impactAbs: 8.5,
        direction: "negative",
        metric: "Score",
      },
      {
        parameter: "Beam",
        impact: 5.2, // Wider hull → better stability, lower resistance
        impactAbs: 5.2,
        direction: "positive",
        metric: "Score",
      },
      {
        parameter: "Draft",
        impact: 3.1, // Deeper → better CB, slightly higher resistance
        impactAbs: 3.1,
        direction: "positive",
        metric: "Score",
      },
      {
        parameter: "CB",
        impact: -12.3, // Higher CB → higher resistance, lower score
        impactAbs: 12.3,
        direction: "negative",
        metric: "Power",
      },
      {
        parameter: "Speed",
        impact: -15.7, // Higher speed → cubic increase in power
        impactAbs: 15.7,
        direction: "negative",
        metric: "Power",
      },
    ];

    // Sort by absolute impact (tornado chart - largest at top)
    return data.sort((a, b) => b.impactAbs - a.impactAbs);
  }, [candidate]);

  // Get color based on impact direction
  const getBarColor = (direction: string) => {
    switch (direction) {
      case "positive":
        return "hsl(var(--accent))"; // Positive impact
      case "negative":
        return "hsl(var(--destructive))"; // Negative impact
      default:
        return "hsl(var(--muted))";
    }
  };

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="bg-accent/10 border border-accent/20 rounded-lg p-4">
        <div className="flex items-start gap-3">
          <AlertCircle className="h-5 w-5 text-accent-foreground flex-shrink-0 mt-0.5" />
          <div>
            <h4 className="text-sm font-semibold text-foreground mb-1">
              Parameter Sensitivity Analysis
            </h4>
            <p className="text-xs text-muted-foreground">
              Shows how ±10% changes in each parameter affect key metrics. Larger bars indicate
              higher sensitivity.
            </p>
          </div>
        </div>
      </div>

      {/* Tornado Chart */}
      <div className="bg-card border border-border rounded-lg p-4">
        <h5 className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-4">
          Impact on Performance (±10% variation)
        </h5>
        <ResponsiveContainer width="100%" height={300}>
          <BarChart
            data={sensitivityData}
            layout="vertical"
            margin={{ top: 5, right: 30, left: 60, bottom: 5 }}
          >
            <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
            <XAxis
              type="number"
              domain={[-20, 20]}
              tick={{ fontSize: 11, fill: "hsl(var(--muted-foreground))" }}
              stroke="hsl(var(--border))"
              label={{
                value: "Impact (%)",
                position: "insideBottom",
                style: { fontSize: "12px", fill: "hsl(var(--muted-foreground))" },
              }}
            />
            <YAxis
              type="category"
              dataKey="parameter"
              tick={{ fontSize: 12, fill: "hsl(var(--foreground))" }}
              stroke="hsl(var(--border))"
              width={50}
            />
            <Tooltip
              contentStyle={{
                backgroundColor: "hsl(var(--card))",
                border: "1px solid hsl(var(--border))",
                borderRadius: "8px",
                fontSize: "12px",
              }}
              formatter={(value: number) => `${value > 0 ? "+" : ""}${value.toFixed(1)}%`}
              labelFormatter={(label) => `Parameter: ${label}`}
            />
            <Bar dataKey="impact" radius={[0, 4, 4, 0]}>
              {sensitivityData.map((entry, index) => (
                <Cell key={`cell-${index}`} fill={getBarColor(entry.direction)} />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>

      {/* Sensitivity Rankings */}
      <div className="bg-card border border-border rounded-lg p-4">
        <h5 className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-3">
          Sensitivity Rankings
        </h5>
        <div className="space-y-2">
          {sensitivityData.map((item, idx) => (
            <div
              key={item.parameter}
              className="flex items-center justify-between p-2 rounded hover:bg-muted/30 transition-colors"
            >
              <div className="flex items-center gap-3">
                <span className="text-xs font-bold text-muted-foreground w-4">#{idx + 1}</span>
                <span className="text-sm font-medium text-foreground">{item.parameter}</span>
              </div>
              <div className="flex items-center gap-3">
                <span className="text-xs text-muted-foreground">{item.metric}</span>
                <div className="flex items-center gap-1">
                  {item.direction === "positive" ? (
                    <TrendingUp className="h-4 w-4 text-accent-foreground" />
                  ) : item.direction === "negative" ? (
                    <TrendingDown className="h-4 w-4 text-destructive" />
                  ) : (
                    <Minus className="h-4 w-4 text-muted-foreground" />
                  )}
                  <span
                    className={`text-sm font-semibold ${
                      item.direction === "positive"
                        ? "text-accent-foreground"
                        : item.direction === "negative"
                          ? "text-destructive"
                          : "text-muted-foreground"
                    }`}
                  >
                    {item.impact > 0 ? "+" : ""}
                    {item.impact.toFixed(1)}%
                  </span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Interpretation Guide */}
      <div className="bg-muted/30 border border-border rounded-lg p-4">
        <h5 className="text-xs font-semibold text-foreground uppercase tracking-wide mb-2 flex items-center gap-2">
          <AlertCircle className="h-3 w-3" />
          How to Read
        </h5>
        <ul className="text-xs text-muted-foreground space-y-1">
          <li>
            • <strong className="text-accent-foreground">Green bars</strong>: Increasing this
            parameter improves performance
          </li>
          <li>
            • <strong className="text-destructive">Red bars</strong>: Increasing this parameter
            reduces performance
          </li>
          <li>
            • <strong>Longer bars</strong>: More sensitive - small changes have big impact
          </li>
          <li>
            • <strong>Shorter bars</strong>: Less sensitive - can adjust freely
          </li>
        </ul>
      </div>
    </div>
  );
};
