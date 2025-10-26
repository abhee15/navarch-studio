import { useState, useEffect } from "react";
import { observer } from "mobx-react-lite";
import { hydrostaticsApi, loadcasesApi } from "../../../services/hydrostaticsApi";
import type { HydroResult, Loadcase } from "../../../types/hydrostatics";
import { settingsStore } from "../../../stores/SettingsStore";
import { unitConverter } from "@navarch/unit-conversion";

interface ComputationsTabProps {
  vesselId: string;
}

export const ComputationsTab = observer(({ vesselId }: ComputationsTabProps) => {
  const [loadcases, setLoadcases] = useState<Loadcase[]>([]);
  const [selectedLoadcaseId, setSelectedLoadcaseId] = useState<string>("");
  const [minDraft, setMinDraft] = useState<number>(1);
  const [maxDraft, setMaxDraft] = useState<number>(10);
  const [draftStep, setDraftStep] = useState<number>(0.5);
  const [results, setResults] = useState<HydroResult[]>([]);
  const [loading, setLoading] = useState(false);
  const [computing, setComputing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [computationTime, setComputationTime] = useState<number | null>(null);

  const loadLoadcases = async () => {
    try {
      setLoading(true);
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

  useEffect(() => {
    loadLoadcases();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [vesselId]);

  const handleCompute = async () => {
    if (minDraft >= maxDraft) {
      alert("Min draft must be less than max draft");
      return;
    }

    try {
      setComputing(true);
      setError(null);

      // Generate draft array
      const drafts: number[] = [];
      for (let d = minDraft; d <= maxDraft; d += draftStep) {
        drafts.push(Number(d.toFixed(2)));
      }

      const response = await hydrostaticsApi.computeTable(vesselId, {
        loadcaseId: selectedLoadcaseId || undefined,
        drafts,
      });

      setResults(response.results);
      setComputationTime(response.computation_time_ms);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to compute hydrostatics");
    } finally {
      setComputing(false);
    }
  };

  // Backend automatically converts values to user's preferred units
  const displayUnits = settingsStore.preferredUnits;
  const lengthUnit = unitConverter.getUnitSymbol(displayUnits, "Length");
  const massUnit = unitConverter.getUnitSymbol(displayUnits, "Mass");
  const areaUnit = unitConverter.getUnitSymbol(displayUnits, "Area");

  const formatNumber = (value: number | undefined | null, decimals: number = 2): string => {
    if (value === null || value === undefined) return "—";
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
      {/* Computation Parameters */}
      <div className="bg-white shadow rounded-lg p-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">Computation Parameters</h3>

        {loadcases.length === 0 ? (
          <div className="bg-yellow-50 border border-yellow-200 rounded-md p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-yellow-400" fill="currentColor" viewBox="0 0 20 20">
                  <path
                    fillRule="evenodd"
                    d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
                    clipRule="evenodd"
                  />
                </svg>
              </div>
              <div className="ml-3">
                <h3 className="text-sm font-medium text-yellow-800">No loadcases defined</h3>
                <div className="mt-2 text-sm text-yellow-700">
                  <p>
                    Create at least one loadcase in the <strong>Loadcases</strong> tab to enable
                    computations.
                  </p>
                </div>
              </div>
            </div>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            {/* Loadcase Selection */}
            <div>
              <label htmlFor="loadcase" className="block text-sm font-medium text-gray-700">
                Loadcase (Optional)
              </label>
              <select
                id="loadcase"
                value={selectedLoadcaseId}
                onChange={(e) => setSelectedLoadcaseId(e.target.value)}
                className="mt-1 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
              >
                <option value="">None (geometry only)</option>
                {loadcases.map((lc) => (
                  <option key={lc.id} value={lc.id}>
                    {lc.name}
                  </option>
                ))}
              </select>
            </div>

            {/* Min Draft */}
            <div>
              <label htmlFor="minDraft" className="block text-sm font-medium text-gray-700">
                Min Draft (m)
              </label>
              <input
                type="number"
                id="minDraft"
                value={minDraft}
                onChange={(e) => setMinDraft(parseFloat(e.target.value) || 0)}
                step="0.1"
                min="0"
                className="mt-1 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
              />
            </div>

            {/* Max Draft */}
            <div>
              <label htmlFor="maxDraft" className="block text-sm font-medium text-gray-700">
                Max Draft (m)
              </label>
              <input
                type="number"
                id="maxDraft"
                value={maxDraft}
                onChange={(e) => setMaxDraft(parseFloat(e.target.value) || 0)}
                step="0.1"
                min="0"
                className="mt-1 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
              />
            </div>

            {/* Draft Step */}
            <div>
              <label htmlFor="draftStep" className="block text-sm font-medium text-gray-700">
                Draft Step (m)
              </label>
              <input
                type="number"
                id="draftStep"
                value={draftStep}
                onChange={(e) => setDraftStep(parseFloat(e.target.value) || 0.1)}
                step="0.1"
                min="0.1"
                className="mt-1 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
              />
            </div>
          </div>
        )}

        {/* Compute Button */}
        <div className="mt-6 flex items-center justify-between">
          <div className="text-sm text-gray-500">
            {minDraft < maxDraft &&
              `Will compute ${Math.ceil((maxDraft - minDraft) / draftStep) + 1} drafts`}
          </div>
          <button
            onClick={handleCompute}
            disabled={computing || loadcases.length === 0}
            className="inline-flex items-center px-6 py-3 border border-transparent text-base font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {computing ? (
              <>
                <svg
                  className="animate-spin -ml-1 mr-3 h-5 w-5 text-white"
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
                  ></circle>
                  <path
                    className="opacity-75"
                    fill="currentColor"
                    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                  ></path>
                </svg>
                Computing...
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
                Compute
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

      {/* Results Table */}
      {results.length > 0 && (
        <div className="bg-white shadow rounded-lg overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
            <div>
              <h3 className="text-lg font-medium text-gray-900">Hydrostatic Table</h3>
              {computationTime && (
                <p className="text-sm text-gray-500">Computed in {computationTime.toFixed(0)} ms</p>
              )}
            </div>
            <span className="text-sm text-gray-500">{results.length} rows</span>
          </div>
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Draft ({lengthUnit})
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    ∆ ({massUnit})
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    KB ({lengthUnit})
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    LCB ({lengthUnit})
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    BMt ({lengthUnit})
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    GMt ({lengthUnit})
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Awp ({areaUnit})
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Cb
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Cp
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                    Cwp
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {results.map((result, idx) => (
                  <tr key={idx} className="hover:bg-gray-50">
                    <td className="px-4 py-3 text-sm text-gray-900 font-medium">
                      {formatNumber(result.draft)}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-900">
                      {formatNumber(result.dispWeight, 0)}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-900">
                      {formatNumber(result.kBz)}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-900">
                      {formatNumber(result.lCBx)}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-900">
                      {formatNumber(result.bMt)}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-900">
                      {formatNumber(result.gMt)}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-900">
                      {formatNumber(result.awp, 1)}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-900">
                      {formatNumber(result.cb, 3)}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-900">
                      {formatNumber(result.cp, 3)}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-900">
                      {formatNumber(result.cwp, 3)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
});

export default ComputationsTab;
