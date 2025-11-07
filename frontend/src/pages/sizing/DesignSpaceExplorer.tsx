import React, { useState } from "react";
import { observer } from "mobx-react-lite";
import { useNavigate, useParams } from "react-router-dom";
import { AppHeader } from "../../components/AppHeader";
import { Footer } from "../../components/Footer";
import { Button } from "../../components/ui/button";
import { Home, Play, Grid3x3, AlertCircle, Loader2, CheckCircle2 } from "lucide-react";
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
import axios from "axios";
import type {
  CandidateDesign,
  DesignSpaceExplorationRequest,
  ExplorationResultsSummary,
} from "../../types/sizing";

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
  const [batchId, setBatchId] = useState<string | null>(null);
  const [explorationStatus, setExplorationStatus] = useState<string>("");
  const [error, setError] = useState<string | null>(null);
  const [paretoFrontIds, setParetoFrontIds] = useState<string[]>([]);

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
   * Generate design variants by sweeping parameter space using real backend API
   */
  const generateVariants = async () => {
    if (!missionId) return;

    setIsGenerating(true);
    setError(null);
    setExplorationStatus("Submitting exploration request...");

    try {
      // Step 1: Start exploration
      const request: DesignSpaceExplorationRequest = {
        missionCaseId: missionId,
        ranges: {
          lppMinM: paramRanges.lppMin,
          lppMaxM: paramRanges.lppMax,
          lppSteps: paramRanges.lppSteps,
          beamMinM: paramRanges.beamMin,
          beamMaxM: paramRanges.beamMax,
          beamSteps: paramRanges.beamSteps,
          draftSteps: 1, // Keep draft fixed for now
          speedSteps: 1, // Keep speed fixed for now
          cbSteps: 1, // Keep Cb fixed for now
        },
        mode: "first_principles",
        maxVariants: 100,
      };

      const apiBaseUrl = import.meta.env.VITE_API_BASE_URL || "http://localhost:5002";

      const startResponse = await axios.post<{
        batchId: string;
        totalVariants: number;
        status: string;
      }>(`${apiBaseUrl}/api/v1/hull-sizing/exploration/start`, request, {
        headers: {
          "Content-Type": "application/json",
        },
      });

      const { batchId: newBatchId, totalVariants } = startResponse.data;
      setBatchId(newBatchId);
      setExplorationStatus(`Generating ${totalVariants} design variants...`);

      // Step 2: Poll for results
      let attempts = 0;
      const maxAttempts = 60; // 5 minutes max (5s intervals)
      const pollInterval = 5000; // 5 seconds

      const pollResults = async (): Promise<void> => {
        attempts++;

        if (attempts > maxAttempts) {
          throw new Error("Exploration timed out after 5 minutes");
        }

        const resultsResponse = await axios.get<ExplorationResultsSummary>(
          `${apiBaseUrl}/api/v1/hull-sizing/exploration/results/${newBatchId}`
        );

        const results = resultsResponse.data;

        setExplorationStatus(
          `Generated ${results.completedVariants}/${results.totalVariants} variants...`
        );

        if (results.status === "completed") {
          // Success!
          setVariants(results.candidates);
          setParetoFrontIds(results.paretoAnalysis?.paretoFrontIds || []);
          setExplorationStatus(`✓ Completed: ${results.candidates.length} designs generated`);
          setIsGenerating(false);
        } else if (results.status === "not_found") {
          throw new Error("Exploration batch not found");
        } else {
          // Still running, poll again
          setTimeout(pollResults, pollInterval);
        }
      };

      await pollResults();
    } catch (err: unknown) {
      console.error("Exploration error:", err);
      const error = err as { response?: { data?: { error?: string } }; message?: string };
      setError(error.response?.data?.error || error.message || "Failed to generate variants");
      setExplorationStatus("Error");
      setIsGenerating(false);
    }
  };

  /**
   * Get color based on whether design is on Pareto front or score
   */
  const getDesignColor = (candidateId: string, score: number) => {
    // Highlight Pareto front designs in gold
    if (paretoFrontIds.includes(candidateId)) {
      return "#FFD700"; // Gold for Pareto front
    }

    // Otherwise color by score
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
              <div className="flex-1">
                <h1 className="text-2xl font-bold text-foreground mb-2">
                  Design Space Exploration
                </h1>
                <p className="text-sm text-muted-foreground mb-3">
                  Generate multiple hull variants by sweeping parameter ranges. Visualize trade-offs
                  and identify Pareto-optimal designs.
                </p>

                {/* Status Message */}
                {explorationStatus && (
                  <div className="flex items-center gap-2 text-sm">
                    {isGenerating ? (
                      <Loader2 className="h-4 w-4 animate-spin text-primary" />
                    ) : error ? (
                      <AlertCircle className="h-4 w-4 text-destructive" />
                    ) : (
                      <CheckCircle2 className="h-4 w-4 text-accent" />
                    )}
                    <span className={error ? "text-destructive" : "text-muted-foreground"}>
                      {explorationStatus}
                    </span>
                  </div>
                )}

                {/* Error Message */}
                {error && (
                  <div className="mt-2 p-3 bg-destructive/10 border border-destructive/20 rounded text-sm text-destructive">
                    {error}
                  </div>
                )}
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
                          <Cell
                            key={`cell-${index}`}
                            fill={getDesignColor(variant.id, variant.score)}
                          />
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
                          <Cell
                            key={`cell-${index}`}
                            fill={getDesignColor(variant.id, variant.score)}
                          />
                        ))}
                      </Scatter>
                    </ScatterChart>
                  </ResponsiveContainer>
                </div>
              </div>

              {/* Legend */}
              <div className="mt-4 flex flex-wrap items-center justify-center gap-6 text-xs">
                <div className="flex items-center gap-2">
                  <div
                    className="w-4 h-4 rounded-full"
                    style={{ backgroundColor: "#FFD700" }}
                  ></div>
                  <span className="text-muted-foreground font-semibold">
                    Pareto Front ({paretoFrontIds.length})
                  </span>
                </div>
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
                Bubble size indicates relative score. Gold markers indicate Pareto-optimal designs
                (not dominated in displacement AND power).
              </p>
            </div>
          )}

          {/* Results Table */}
          {variants.length > 0 && (
            <div className="bg-card border border-border rounded-lg p-6">
              <div className="flex items-center justify-between mb-4">
                <h3 className="text-lg font-semibold text-foreground">
                  Design Variants Summary ({variants.length} designs)
                </h3>
                {batchId && (
                  <div className="text-xs text-muted-foreground">Batch ID: {batchId}</div>
                )}
              </div>

              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="border-b border-border">
                    <tr className="text-left">
                      <th className="pb-2 pr-4 font-medium text-muted-foreground">#</th>
                      <th className="pb-2 pr-4 font-medium text-muted-foreground">Family</th>
                      <th className="pb-2 pr-4 font-medium text-muted-foreground">Lpp (m)</th>
                      <th className="pb-2 pr-4 font-medium text-muted-foreground">Beam (m)</th>
                      <th className="pb-2 pr-4 font-medium text-muted-foreground">Draft (m)</th>
                      <th className="pb-2 pr-4 font-medium text-muted-foreground">Disp (t)</th>
                      <th className="pb-2 pr-4 font-medium text-muted-foreground">Cb</th>
                      <th className="pb-2 pr-4 font-medium text-muted-foreground">Fn</th>
                      <th className="pb-2 pr-4 font-medium text-muted-foreground">EHP (kW)</th>
                      <th className="pb-2 pr-4 font-medium text-muted-foreground">Score</th>
                      <th className="pb-2 pr-4 font-medium text-muted-foreground">Status</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border">
                    {variants.slice(0, 20).map((variant, idx) => {
                      const isPareto = paretoFrontIds.includes(variant.id);
                      return (
                        <tr
                          key={variant.id}
                          className="hover:bg-accent/5 cursor-pointer transition-colors"
                          onClick={() => navigate(`/sizing/candidate/${variant.id}`)}
                        >
                          <td className="py-2 pr-4 text-muted-foreground">{idx + 1}</td>
                          <td className="py-2 pr-4 font-medium">{variant.hullFamily}</td>
                          <td className="py-2 pr-4">{variant.lppM.toFixed(1)}</td>
                          <td className="py-2 pr-4">{variant.beamM.toFixed(1)}</td>
                          <td className="py-2 pr-4">{variant.draftM.toFixed(2)}</td>
                          <td className="py-2 pr-4">{variant.dispT.toFixed(0)}</td>
                          <td className="py-2 pr-4">{variant.cb.toFixed(3)}</td>
                          <td className="py-2 pr-4">{variant.fn.toFixed(3)}</td>
                          <td className="py-2 pr-4">{variant.ehpKw?.toFixed(0) || "N/A"}</td>
                          <td className="py-2 pr-4">
                            <span
                              className={`inline-block px-2 py-0.5 rounded text-xs ${
                                variant.score > 0.8
                                  ? "bg-accent/20 text-accent-foreground"
                                  : variant.score > 0.6
                                    ? "bg-primary/20 text-primary-foreground"
                                    : "bg-destructive/20 text-destructive-foreground"
                              }`}
                            >
                              {variant.score.toFixed(2)}
                            </span>
                          </td>
                          <td className="py-2 pr-4">
                            {isPareto && (
                              <span className="text-xs font-semibold" style={{ color: "#FFD700" }}>
                                ★ Pareto
                              </span>
                            )}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>

              {variants.length > 20 && (
                <p className="mt-3 text-xs text-muted-foreground text-center">
                  Showing first 20 of {variants.length} designs. Click any row to view details.
                </p>
              )}
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
