import { useState } from "react";
import type { KcsBenchmarkRequest, KcsBenchmarkResult } from "../../types/resistance";
import { resistanceCalculationsApi } from "../../services/resistanceApi";
import { getErrorMessage } from "../../types/errors";
import toast from "react-hot-toast";

interface KcsBenchmarkPanelProps {
  vesselId: string;
  vesselLWL: number;
  vesselBeam: number;
  vesselDraft: number;
}

export function KcsBenchmarkPanel({
  vesselId,
  vesselLWL,
  vesselBeam,
  vesselDraft,
}: KcsBenchmarkPanelProps) {
  const [loading, setLoading] = useState(false);
  const [benchmarkResult, setBenchmarkResult] = useState<KcsBenchmarkResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  // KCS preset values (typical KCS container ship)
  const kcsInputs: KcsBenchmarkRequest = {
    vesselId,
    speedGridId: "", // Will be generated from reference data
    lWL: vesselLWL || 230.0, // KCS LWL ~230m
    b: vesselBeam || 32.2, // KCS beam ~32.2m
    t: vesselDraft || 10.8, // KCS draft ~10.8m
    cB: 0.651,
    cP: 0.708,
    cM: 0.980,
    lCB_pct: 0.0,
    s: undefined, // Will be estimated
    tempC: 15,
    salinityPpt: 35.0,
    referenceData: [
      { speed: 0.5, rtReference: 42.5, source: "synthetic" },
      { speed: 0.75, rtReference: 101.25, source: "synthetic" },
      { speed: 1.0, rtReference: 190.0, source: "synthetic" },
      { speed: 1.25, rtReference: 312.5, source: "synthetic" },
      { speed: 1.5, rtReference: 472.5, source: "synthetic" },
      { speed: 1.75, rtReference: 673.75, source: "synthetic" },
      { speed: 2.0, rtReference: 920.0, source: "synthetic" },
      { speed: 2.25, rtReference: 1215.0, source: "synthetic" },
      { speed: 2.5, rtReference: 1562.5, source: "synthetic" },
      { speed: 2.75, rtReference: 1966.25, source: "synthetic" },
      { speed: 3.0, rtReference: 2430.0, source: "synthetic" },
    ],
    maeTolerancePercent: 3.0,
    maxTolerancePercent: 5.0,
  };

  const handleRunBenchmark = async () => {
    try {
      setLoading(true);
      setError(null);
      const result = await resistanceCalculationsApi.validateKcsBenchmark(kcsInputs);
      setBenchmarkResult(result);

      if (result.pass) {
        toast.success(
          `Benchmark PASSED: MAE ${result.meanAbsoluteError.toFixed(2)}%, Max ${result.maxError.toFixed(2)}%`
        );
      } else {
        toast.error(
          `Benchmark FAILED: MAE ${result.meanAbsoluteError.toFixed(2)}%, Max ${result.maxError.toFixed(2)}%`
        );
      }
    } catch (err) {
      const errorMsg = getErrorMessage(err);
      setError(errorMsg);
      toast.error(errorMsg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="bg-card border border-border rounded-lg p-4">
      <h2 className="text-sm font-semibold mb-3">KCS Benchmark Validation</h2>

      {error && (
        <div className="mb-3 rounded-md bg-red-50 dark:bg-red-900/20 p-2 text-xs text-red-700 dark:text-red-400">
          {error}
        </div>
      )}

      {/* Benchmark Status */}
      {benchmarkResult && (
        <div className="mb-4">
          <div
            className={`inline-flex items-center px-3 py-1 rounded-full text-xs font-medium ${
              benchmarkResult.pass
                ? "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200"
                : "bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200"
            }`}
          >
            {benchmarkResult.pass ? "✓ PASS" : "✗ FAIL"}
          </div>
          <div className="mt-2 text-xs space-y-1">
            <p>
              <span className="text-muted-foreground">MAE:</span>{" "}
              <span className={benchmarkResult.meanAbsoluteError > benchmarkResult.maeTolerance ? "text-red-600" : ""}>
                {benchmarkResult.meanAbsoluteError.toFixed(2)}%
              </span>
              {" / "}
              {benchmarkResult.maeTolerance.toFixed(1)}% tolerance
            </p>
            <p>
              <span className="text-muted-foreground">Max Error:</span>{" "}
              <span className={benchmarkResult.maxError > benchmarkResult.maxTolerance ? "text-red-600" : ""}>
                {benchmarkResult.maxError.toFixed(2)}%
              </span>
              {" / "}
              {benchmarkResult.maxTolerance.toFixed(1)}% tolerance
            </p>
          </div>
        </div>
      )}

      {/* Run Button */}
      <button
        onClick={handleRunBenchmark}
        disabled={loading}
        className="w-full inline-flex justify-center items-center px-3 py-2 border border-transparent text-xs font-medium rounded shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
      >
        {loading ? "Running..." : "Run KCS Benchmark"}
      </button>

      {/* Results Table */}
      {benchmarkResult && (
        <div className="mt-4">
          <h3 className="text-xs font-medium mb-2">Validation Results</h3>
          <div className="overflow-x-auto max-h-64 overflow-y-auto">
            <table className="min-w-full text-xs">
              <thead className="bg-muted sticky top-0">
                <tr>
                  <th className="px-2 py-1 text-left">Speed (m/s)</th>
                  <th className="px-2 py-1 text-right">RT_ref (N)</th>
                  <th className="px-2 py-1 text-right">RT_calc (N)</th>
                  <th className="px-2 py-1 text-right">Error %</th>
                  <th className="px-2 py-1 text-center">Status</th>
                </tr>
              </thead>
              <tbody>
                {benchmarkResult.speedGrid.map((speed, idx) => {
                  const error = benchmarkResult.errorPercent[idx];
                  const isError = error > benchmarkResult.maxTolerance;
                  return (
                    <tr key={idx} className="border-t border-border">
                      <td className="px-2 py-1">{speed.toFixed(3)}</td>
                      <td className="px-2 py-1 text-right">
                        {benchmarkResult.referenceResistance[idx]?.toFixed(0)}
                      </td>
                      <td className="px-2 py-1 text-right">
                        {benchmarkResult.calculatedResistance[idx]?.toFixed(0)}
                      </td>
                      <td className={`px-2 py-1 text-right ${isError ? "text-red-600 font-medium" : ""}`}>
                        {error.toFixed(2)}%
                      </td>
                      <td className="px-2 py-1 text-center">
                        {isError ? (
                          <span className="text-red-600">✗</span>
                        ) : (
                          <span className="text-green-600">✓</span>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Info */}
      <div className="mt-3 pt-3 border-t border-border text-[10px] text-muted-foreground">
        <p>
          Validates Holtrop-Mennen calculations against KCS reference data. Default tolerances:
          MAE ≤ 3%, Max ≤ 5%.
        </p>
      </div>
    </div>
  );
}

