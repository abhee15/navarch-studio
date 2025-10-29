import { useState, useEffect } from "react";
import { loadcasesApi } from "../../../services/hydrostaticsApi";
import type { Loadcase } from "../../../types/hydrostatics";
import axios from "axios";

interface TrimSolverTabProps {
  vesselId: string;
}

interface TrimSolution {
  targetDisplacement: number;
  draftAP: number;
  draftFP: number;
  meanDraft: number;
  trimAngle: number;
  lcf: number;
  mtc: number;
  converged: boolean;
  iterations: number;
}

export function TrimSolverTab({ vesselId }: TrimSolverTabProps) {
  const [loadcases, setLoadcases] = useState<Loadcase[]>([]);
  const [selectedLoadcaseId, setSelectedLoadcaseId] = useState<string>("");
  const [targetDisplacement, setTargetDisplacement] = useState<number>(10000);
  const [initialDraftFwd, setInitialDraftFwd] = useState<number>(5.0);
  const [initialDraftAft, setInitialDraftAft] = useState<number>(5.0);
  const [maxIterations, setMaxIterations] = useState<number>(20);
  const [tolerance, setTolerance] = useState<number>(100);
  const [solution, setSolution] = useState<TrimSolution | null>(null);
  const [loading, setLoading] = useState(false);
  const [solving, setSolving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [computationTime, setComputationTime] = useState<number | null>(null);

  useEffect(() => {
    loadLoadcases();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [vesselId]);

  const loadLoadcases = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await loadcasesApi.list(vesselId);
      setLoadcases(data.loadcases);
      if (data.loadcases.length > 0 && !selectedLoadcaseId) {
        setSelectedLoadcaseId(data.loadcases[0].id);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load loadcases");
    } finally {
      setLoading(false);
    }
  };

  const handleSolve = async () => {
    if (targetDisplacement <= 0) {
      alert("Target displacement must be positive");
      return;
    }

    if (initialDraftFwd <= 0 || initialDraftAft <= 0) {
      alert("Initial drafts must be positive");
      return;
    }

    try {
      setSolving(true);
      setError(null);

      const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5003";
      const response = await axios.post(
        `${API_BASE_URL}/api/v1/hydrostatics/vessels/${vesselId}/compute/trim`,
        {
          loadcaseId: selectedLoadcaseId || null,
          targetDisplacement,
          initialDraftFwd,
          initialDraftAft,
          maxIterations,
          tolerance,
        }
      );

      setSolution(response.data.result);
      setComputationTime(response.data.computation_time_ms);
    } catch (err) {
      if (axios.isAxiosError(err) && err.response) {
        setError(err.response.data.error || "Failed to solve trim");
      } else {
        setError(err instanceof Error ? err.message : "Failed to solve trim");
      }
    } finally {
      setSolving(false);
    }
  };

  const formatNumber = (value: number | undefined, decimals: number = 3): string => {
    if (value === undefined) return "—";
    return value.toFixed(decimals);
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-white dark:bg-gray-800 shadow rounded-lg p-6">
        <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100 mb-2">Trim Solver</h3>
        <p className="text-sm text-gray-500 dark:text-gray-400 dark:text-gray-500">
          Solve for forward and aft drafts to achieve target displacement using Newton-Raphson
          method
        </p>
      </div>

      {/* Input Parameters */}
      <div className="bg-white dark:bg-gray-800 shadow rounded-lg p-6">
        <h4 className="text-md font-medium text-gray-900 dark:text-gray-100 mb-4">Input Parameters</h4>

        {loadcases.length === 0 ? (
          <div className="bg-yellow-50 dark:bg-yellow-900/30 border border-yellow-200 dark:border-yellow-800 rounded-md p-4">
            <p className="text-sm text-yellow-800 dark:text-yellow-200">
              ⚠️ No loadcases found. Please create a loadcase in the Loadcases tab first.
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Loadcase Selection */}
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                Loadcase (for KG, ρ)
              </label>
              <select
                value={selectedLoadcaseId}
                onChange={(e) => setSelectedLoadcaseId(e.target.value)}
                className="block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
              >
                {loadcases.map((lc) => (
                  <option key={lc.id} value={lc.id}>
                    {lc.name} (ρ={lc.rho} kg/m³, KG={lc.kg || "N/A"}m)
                  </option>
                ))}
              </select>
            </div>

            {/* Target Displacement */}
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                Target Displacement (kg)
              </label>
              <input
                type="number"
                value={targetDisplacement}
                onChange={(e) => setTargetDisplacement(Number(e.target.value))}
                step="1000"
                min="0"
                className="block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
              />
            </div>

            {/* Initial Draft Forward */}
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                Initial Draft Forward (m)
              </label>
              <input
                type="number"
                value={initialDraftFwd}
                onChange={(e) => setInitialDraftFwd(Number(e.target.value))}
                step="0.1"
                min="0.1"
                className="block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
              />
              <p className="mt-1 text-xs text-gray-500 dark:text-gray-400 dark:text-gray-500">Initial guess for T_FP</p>
            </div>

            {/* Initial Draft Aft */}
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                Initial Draft Aft (m)
              </label>
              <input
                type="number"
                value={initialDraftAft}
                onChange={(e) => setInitialDraftAft(Number(e.target.value))}
                step="0.1"
                min="0.1"
                className="block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
              />
              <p className="mt-1 text-xs text-gray-500 dark:text-gray-400 dark:text-gray-500">Initial guess for T_AP</p>
            </div>

            {/* Advanced Options */}
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Max Iterations</label>
              <input
                type="number"
                value={maxIterations}
                onChange={(e) => setMaxIterations(Number(e.target.value))}
                min="5"
                max="50"
                className="block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Tolerance (kg)</label>
              <input
                type="number"
                value={tolerance}
                onChange={(e) => setTolerance(Number(e.target.value))}
                min="1"
                max="1000"
                className="block w-full rounded-md border-gray-300 dark:border-gray-600 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm px-3 py-2 border"
              />
              <p className="mt-1 text-xs text-gray-500 dark:text-gray-400 dark:text-gray-500">Convergence tolerance</p>
            </div>
          </div>
        )}

        {/* Solve Button */}
        <div className="mt-6">
          <button
            onClick={handleSolve}
            disabled={solving || loadcases.length === 0}
            className="inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:bg-gray-400 dark:bg-gray-600 disabled:cursor-not-allowed"
          >
            {solving ? (
              <>
                <svg
                  className="animate-spin -ml-1 mr-2 h-4 w-4 text-white"
                  fill="none"
                  viewBox="0 0 24 24"
                >
                  <circle
                    className="opacity-25"
                    cx="12"
                    cy="12"
                    r="10"
                    stroke="currentColor"
                    strokeWidth="4"
                  />
                  <path
                    className="opacity-75"
                    fill="currentColor"
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                  />
                </svg>
                Solving...
              </>
            ) : (
              <>
                <svg
                  className="-ml-1 mr-2 h-5 w-5"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z"
                  />
                </svg>
                Solve Trim
              </>
            )}
          </button>
        </div>
      </div>

      {/* Error Message */}
      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
          <strong className="font-bold">Error: </strong>
          <span>{error}</span>
        </div>
      )}

      {/* Results */}
      {solution && (
        <div className="bg-white dark:bg-gray-800 shadow rounded-lg p-6">
          <div className="flex items-center justify-between mb-4">
            <h4 className="text-md font-medium text-gray-900 dark:text-gray-100">Trim Solution</h4>
            {solution.converged ? (
              <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800">
                ✓ Converged in {solution.iterations} iterations
              </span>
            ) : (
              <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-yellow-100 text-yellow-800 dark:text-yellow-200">
                ⚠️ Did not converge ({solution.iterations} iterations)
              </span>
            )}
          </div>

          {computationTime !== null && (
            <p className="text-xs text-gray-500 dark:text-gray-400 dark:text-gray-500 mb-4">Computed in {computationTime}ms</p>
          )}

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <div className="bg-blue-50 p-4 rounded-lg">
              <div className="text-sm font-medium text-blue-600">Draft Forward (T_FP)</div>
              <div className="text-2xl font-bold text-blue-900">
                {formatNumber(solution.draftFP)} m
              </div>
            </div>

            <div className="bg-blue-50 p-4 rounded-lg">
              <div className="text-sm font-medium text-blue-600">Draft Aft (T_AP)</div>
              <div className="text-2xl font-bold text-blue-900">
                {formatNumber(solution.draftAP)} m
              </div>
            </div>

            <div className="bg-gray-50 dark:bg-gray-900 p-4 rounded-lg">
              <div className="text-sm font-medium text-gray-600">Mean Draft</div>
              <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">
                {formatNumber(solution.meanDraft)} m
              </div>
            </div>

            <div className="bg-gray-50 dark:bg-gray-900 p-4 rounded-lg">
              <div className="text-sm font-medium text-gray-600">Trim Angle</div>
              <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">
                {formatNumber(solution.trimAngle)} m
              </div>
              <div className="text-xs text-gray-500 dark:text-gray-400 dark:text-gray-500 mt-1">
                {solution.trimAngle > 0
                  ? "By the stern"
                  : solution.trimAngle < 0
                    ? "By the bow"
                    : "Even keel"}
              </div>
            </div>

            <div className="bg-gray-50 dark:bg-gray-900 p-4 rounded-lg">
              <div className="text-sm font-medium text-gray-600">LCF</div>
              <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">{formatNumber(solution.lcf)} m</div>
              <div className="text-xs text-gray-500 dark:text-gray-400 dark:text-gray-500 mt-1">Longitudinal Center of Flotation</div>
            </div>

            <div className="bg-gray-50 dark:bg-gray-900 p-4 rounded-lg">
              <div className="text-sm font-medium text-gray-600">MTC</div>
              <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">
                {formatNumber(solution.mtc, 0)} kg·m
              </div>
              <div className="text-xs text-gray-500 dark:text-gray-400 dark:text-gray-500 mt-1">Moment to Change Trim 1cm</div>
            </div>
          </div>

          {/* Diagram */}
          <div className="mt-6 border-t pt-6">
            <h5 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">Trim Diagram</h5>
            <div className="relative h-32 bg-gray-50 dark:bg-gray-900 rounded">
              {/* Simple SVG representation of vessel trim */}
              <svg className="w-full h-full" viewBox="0 0 400 100">
                {/* Waterline */}
                <line
                  x1="50"
                  y1="50"
                  x2="350"
                  y2="50"
                  stroke="#3B82F6"
                  strokeWidth="2"
                  strokeDasharray="5,5"
                />

                {/* Vessel hull (simplified) */}
                <line
                  x1="50"
                  y1={50 - solution.draftFP * 5}
                  x2="350"
                  y2={50 - solution.draftAP * 5}
                  stroke="#1F2937"
                  strokeWidth="3"
                />

                {/* Labels */}
                <text x="45" y={50 - solution.draftFP * 5 - 5} fontSize="12" fill="#1F2937">
                  FP
                </text>
                <text x="355" y={50 - solution.draftAP * 5 - 5} fontSize="12" fill="#1F2937">
                  AP
                </text>
              </svg>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
