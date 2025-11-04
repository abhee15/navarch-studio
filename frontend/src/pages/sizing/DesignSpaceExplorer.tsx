import React, { useState } from "react";
import { observer } from "mobx-react-lite";
import { useNavigate, useParams } from "react-router-dom";
import { AppHeader } from "../../components/AppHeader";
import { Footer } from "../../components/Footer";
import { Button } from "../../components/ui/button";
import { Home, Play, Grid3x3, AlertCircle, Loader2 } from "lucide-react";
import {
  ScatterChart,
  Scatter,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  ZAxis,
  Cell,
} from "recharts";
import type { CandidateDesign } from "../../types/sizing";

/**
 * Design Space Exploration
 *
 * Generate and visualize multiple hull variants by varying parameters
 * Shows trade-off spaces and Pareto fronts
 */
export const DesignSpaceExplorer: React.FC = observer(() => {
  const { missionId } = useParams<{ missionId: string }>();
  const navigate = useNavigate();

  const [isGenerating, setIsGenerating] = useState(false);
  const [variants, setVariants] = useState<CandidateDesign[]>([]);

  // Parameter ranges for exploration
  const [paramRanges, setParamRanges] = useState({
    lppMin: 40,
    lppMax: 60,
    lppSteps: 5,
    beamMin: 8,
    beamMax: 12,
    beamSteps: 5,
  });

  /**
   * Generate design variants by sweeping parameter space
   */
  const generateVariants = async () => {
    if (!missionId) return;

    setIsGenerating(true);
    const newVariants: CandidateDesign[] = [];

    // Sweep Lpp and Beam
    const lppStep = (paramRanges.lppMax - paramRanges.lppMin) / (paramRanges.lppSteps - 1);
    const beamStep = (paramRanges.beamMax - paramRanges.beamMin) / (paramRanges.beamSteps - 1);

    for (let i = 0; i < paramRanges.lppSteps; i++) {
      for (let j = 0; j < paramRanges.beamSteps; j++) {
        const lpp = paramRanges.lppMin + i * lppStep;
        const beam = paramRanges.beamMin + j * beamStep;

        // Create a variant (in real app, this would call backend API)
        // For now, simulate with mock data
        const variant: CandidateDesign = {
          id: `variant-${i}-${j}`,
          sizingRunId: missionId || "",
          userId: "",
          tenantId: "",
          rank: 0,
          score: 0.5 + Math.random() * 0.5,
          hullFamily: "Wigley",
          lppM: lpp,
          lwlM: lpp * 1.02,
          loaM: lpp * 1.05,
          beamM: beam,
          draftM: beam / 2.5,
          depthM: beam / 2,
          dispT: ((lpp * beam * beam) / 2.5) * 0.7,
          cb: 0.6 + Math.random() * 0.1,
          cp: 0.75 + Math.random() * 0.05,
          cwp: 0.8 + Math.random() * 0.05,
          fn: 0.25 + Math.random() * 0.1,
          ehpKw: 500 + Math.random() * 500,
          shpKw: 600 + Math.random() * 600,
          kbM: (beam / 2.5) * 0.53,
          lcbPctLpp: -2 + Math.random() * 4,
          gmEstM: 1 + Math.random() * 2,
          flagsJson: "[]",
          isSelected: false,
          createdAt: new Date().toISOString(),
        };

        newVariants.push(variant);
      }
    }

    setVariants(newVariants);
    setIsGenerating(false);
  };

  /**
   * Get color based on score (green = high, red = low)
   */
  const getScoreColor = (score: number) => {
    if (score > 0.8) return "hsl(var(--accent))"; // Green
    if (score > 0.6) return "hsl(var(--primary))"; // Blue
    return "hsl(var(--destructive))"; // Red
  };

  return (
    <>
      <AppHeader
        left={<h1 className="text-xl font-semibold text-foreground">Design Space Explorer</h1>}
        right={
          <Button variant="ghost" size="sm" onClick={() => navigate("/sizing/missions")}>
            <Home className="h-4 w-4 mr-2" />
            Back to Briefs
          </Button>
        }
      />

      <main className="min-h-screen bg-background py-8 px-4 sm:px-6 lg:px-8">
        <div className="max-w-7xl mx-auto space-y-8">
          {/* Header */}
          <div className="bg-card border border-border rounded-lg p-6">
            <div className="flex items-start justify-between">
              <div>
                <h1 className="text-2xl font-bold text-foreground mb-2">
                  Design Space Exploration
                </h1>
                <p className="text-sm text-muted-foreground">
                  Generate multiple hull variants by sweeping parameter ranges. Visualize trade-offs
                  and identify Pareto-optimal designs.
                </p>
              </div>
              <Button
                onClick={generateVariants}
                disabled={isGenerating}
                className="flex items-center gap-2"
              >
                {isGenerating ? (
                  <>
                    <Loader2 className="h-4 w-4 animate-spin" />
                    Generating...
                  </>
                ) : (
                  <>
                    <Play className="h-4 w-4" />
                    Generate Variants
                  </>
                )}
              </Button>
            </div>
          </div>

          {/* Parameter Configuration */}
          <div className="bg-card border border-border rounded-lg p-6">
            <div className="flex items-center gap-2 mb-4">
              <Grid3x3 className="h-5 w-5 text-primary" />
              <h3 className="text-lg font-semibold text-foreground">Parameter Ranges</h3>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {/* Lpp Range */}
              <div className="space-y-3">
                <h4 className="text-sm font-medium text-foreground">Length (Lpp)</h4>
                <div className="grid grid-cols-3 gap-2">
                  <div>
                    <label className="text-xs text-muted-foreground">Min (m)</label>
                    <input
                      type="number"
                      value={paramRanges.lppMin}
                      onChange={(e) =>
                        setParamRanges({ ...paramRanges, lppMin: Number(e.target.value) })
                      }
                      className="w-full px-3 py-2 bg-background border border-border rounded text-sm"
                    />
                  </div>
                  <div>
                    <label className="text-xs text-muted-foreground">Max (m)</label>
                    <input
                      type="number"
                      value={paramRanges.lppMax}
                      onChange={(e) =>
                        setParamRanges({ ...paramRanges, lppMax: Number(e.target.value) })
                      }
                      className="w-full px-3 py-2 bg-background border border-border rounded text-sm"
                    />
                  </div>
                  <div>
                    <label className="text-xs text-muted-foreground">Steps</label>
                    <input
                      type="number"
                      value={paramRanges.lppSteps}
                      onChange={(e) =>
                        setParamRanges({ ...paramRanges, lppSteps: Number(e.target.value) })
                      }
                      className="w-full px-3 py-2 bg-background border border-border rounded text-sm"
                    />
                  </div>
                </div>
              </div>

              {/* Beam Range */}
              <div className="space-y-3">
                <h4 className="text-sm font-medium text-foreground">Beam (B)</h4>
                <div className="grid grid-cols-3 gap-2">
                  <div>
                    <label className="text-xs text-muted-foreground">Min (m)</label>
                    <input
                      type="number"
                      value={paramRanges.beamMin}
                      onChange={(e) =>
                        setParamRanges({ ...paramRanges, beamMin: Number(e.target.value) })
                      }
                      className="w-full px-3 py-2 bg-background border border-border rounded text-sm"
                    />
                  </div>
                  <div>
                    <label className="text-xs text-muted-foreground">Max (m)</label>
                    <input
                      type="number"
                      value={paramRanges.beamMax}
                      onChange={(e) =>
                        setParamRanges({ ...paramRanges, beamMax: Number(e.target.value) })
                      }
                      className="w-full px-3 py-2 bg-background border border-border rounded text-sm"
                    />
                  </div>
                  <div>
                    <label className="text-xs text-muted-foreground">Steps</label>
                    <input
                      type="number"
                      value={paramRanges.beamSteps}
                      onChange={(e) =>
                        setParamRanges({ ...paramRanges, beamSteps: Number(e.target.value) })
                      }
                      className="w-full px-3 py-2 bg-background border border-border rounded text-sm"
                    />
                  </div>
                </div>
              </div>
            </div>

            <div className="mt-4 p-3 bg-accent/10 border border-accent/20 rounded-lg">
              <div className="flex items-start gap-2">
                <AlertCircle className="h-4 w-4 text-accent-foreground flex-shrink-0 mt-0.5" />
                <div className="text-xs text-accent-foreground">
                  <strong>Total variants:</strong> {paramRanges.lppSteps * paramRanges.beamSteps}{" "}
                  designs will be generated. Higher steps = better resolution but longer compute
                  time.
                </div>
              </div>
            </div>
          </div>

          {/* Visualization */}
          {variants.length > 0 && (
            <div className="bg-card border border-border rounded-lg p-6">
              <h3 className="text-lg font-semibold text-foreground mb-4">Trade-Off Space</h3>

              {/* Power vs Displacement */}
              <div className="space-y-6">
                <div>
                  <h4 className="text-sm font-medium text-muted-foreground mb-3">
                    Power vs Displacement
                  </h4>
                  <ResponsiveContainer width="100%" height={400}>
                    <ScatterChart margin={{ top: 20, right: 80, bottom: 20, left: 20 }}>
                      <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
                      <XAxis
                        type="number"
                        dataKey="dispT"
                        name="Displacement"
                        unit=" t"
                        label={{
                          value: "Displacement (tonnes)",
                          position: "insideBottom",
                          offset: -10,
                          style: { fontSize: "12px", fill: "hsl(var(--muted-foreground))" },
                        }}
                        tick={{ fontSize: 11, fill: "hsl(var(--muted-foreground))" }}
                        stroke="hsl(var(--border))"
                      />
                      <YAxis
                        type="number"
                        dataKey="ehpKw"
                        name="Power"
                        unit=" kW"
                        label={{
                          value: "EHP (kW)",
                          angle: -90,
                          position: "insideLeft",
                          style: { fontSize: "12px", fill: "hsl(var(--muted-foreground))" },
                        }}
                        tick={{ fontSize: 11, fill: "hsl(var(--muted-foreground))" }}
                        stroke="hsl(var(--border))"
                      />
                      <ZAxis type="number" dataKey="score" range={[50, 400]} name="Score" />
                      <Tooltip
                        contentStyle={{
                          backgroundColor: "hsl(var(--card))",
                          border: "1px solid hsl(var(--border))",
                          borderRadius: "8px",
                          fontSize: "12px",
                        }}
                        formatter={(value: number) => value.toFixed(2)}
                      />
                      <Scatter name="Variants" data={variants}>
                        {variants.map((variant, index) => (
                          <Cell key={`cell-${index}`} fill={getScoreColor(variant.score)} />
                        ))}
                      </Scatter>
                    </ScatterChart>
                  </ResponsiveContainer>
                </div>

                {/* Lpp vs Beam */}
                <div>
                  <h4 className="text-sm font-medium text-muted-foreground mb-3">
                    Length vs Beam (colored by score)
                  </h4>
                  <ResponsiveContainer width="100%" height={400}>
                    <ScatterChart margin={{ top: 20, right: 80, bottom: 20, left: 20 }}>
                      <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
                      <XAxis
                        type="number"
                        dataKey="lppM"
                        name="Lpp"
                        unit=" m"
                        label={{
                          value: "Lpp (m)",
                          position: "insideBottom",
                          offset: -10,
                          style: { fontSize: "12px", fill: "hsl(var(--muted-foreground))" },
                        }}
                        tick={{ fontSize: 11, fill: "hsl(var(--muted-foreground))" }}
                        stroke="hsl(var(--border))"
                      />
                      <YAxis
                        type="number"
                        dataKey="beamM"
                        name="Beam"
                        unit=" m"
                        label={{
                          value: "Beam (m)",
                          angle: -90,
                          position: "insideLeft",
                          style: { fontSize: "12px", fill: "hsl(var(--muted-foreground))" },
                        }}
                        tick={{ fontSize: 11, fill: "hsl(var(--muted-foreground))" }}
                        stroke="hsl(var(--border))"
                      />
                      <ZAxis type="number" dataKey="score" range={[100, 500]} name="Score" />
                      <Tooltip
                        contentStyle={{
                          backgroundColor: "hsl(var(--card))",
                          border: "1px solid hsl(var(--border))",
                          borderRadius: "8px",
                          fontSize: "12px",
                        }}
                        formatter={(value: number) => value.toFixed(2)}
                      />
                      <Scatter name="Design Space" data={variants}>
                        {variants.map((variant, index) => (
                          <Cell key={`cell-${index}`} fill={getScoreColor(variant.score)} />
                        ))}
                      </Scatter>
                    </ScatterChart>
                  </ResponsiveContainer>
                </div>
              </div>

              {/* Legend */}
              <div className="mt-4 flex items-center justify-center gap-6 text-xs">
                <div className="flex items-center gap-2">
                  <div className="w-4 h-4 rounded-full bg-accent"></div>
                  <span className="text-muted-foreground">High Score (&gt;0.8)</span>
                </div>
                <div className="flex items-center gap-2">
                  <div className="w-4 h-4 rounded-full bg-primary"></div>
                  <span className="text-muted-foreground">Medium Score (0.6-0.8)</span>
                </div>
                <div className="flex items-center gap-2">
                  <div className="w-4 h-4 rounded-full bg-destructive"></div>
                  <span className="text-muted-foreground">Low Score (&lt;0.6)</span>
                </div>
              </div>

              <p className="mt-4 text-xs text-muted-foreground text-center">
                Bubble size indicates relative score. Look for Pareto front (lower-left in Power vs
                Displacement).
              </p>
            </div>
          )}

          {/* Empty State */}
          {variants.length === 0 && !isGenerating && (
            <div className="bg-muted/30 border border-dashed border-border rounded-lg p-12 text-center">
              <Grid3x3 className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <h3 className="text-lg font-semibold text-foreground mb-2">
                No Variants Generated Yet
              </h3>
              <p className="text-sm text-muted-foreground max-w-md mx-auto">
                Configure parameter ranges above and click "Generate Variants" to explore the design
                space.
              </p>
            </div>
          )}
        </div>
      </main>

      <Footer />
    </>
  );
});
