import { useState, useEffect } from "react";
import { observer } from "mobx-react-lite";
import { hydrostaticsApi, loadcasesApi } from "../../../services/hydrostaticsApi";
import type { HydroResult, Loadcase, VesselDetails } from "../../../types/hydrostatics";
import { settingsStore } from "../../../stores/SettingsStore";
import { getUnitSymbol } from "../../../utils/unitSymbols";
import { CollapsibleSection } from "../CollapsibleSection";

interface ComputationsTabProps {
  vesselId: string;
  vessel?: VesselDetails;
}

export const ComputationsTab = observer(({ vesselId, vessel }: ComputationsTabProps) => {
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
  const lengthUnit = getUnitSymbol(displayUnits, "Length");
  const massUnit = getUnitSymbol(displayUnits, "Mass");
  const areaUnit = getUnitSymbol(displayUnits, "Area");

  const formatNumber = (value: number | undefined | null, decimals: number = 2): string => {
    if (value === null || value === undefined) return "—";
    return value.toFixed(decimals);
  };

  const draftCount = minDraft < maxDraft ? Math.ceil((maxDraft - minDraft) / draftStep) + 1 : 0;

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col overflow-hidden">
      {/* Workspace Toolbar */}
      <div className="bg-white border-b border-gray-200 px-2.5 py-1.5 flex items-center justify-between flex-shrink-0">
        <div className="flex items-center gap-3">
          <div className="flex items-center gap-1.5">
            <label className="text-[11px] font-medium text-gray-600">Vessel:</label>
            <div className="text-xs font-medium text-gray-900">{vessel?.name || "Unknown"}</div>
          </div>
          <div className="flex items-center gap-1.5">
            <label htmlFor="condition-select" className="text-[11px] font-medium text-gray-600">
              Condition:
            </label>
            <select
              id="condition-select"
              value={selectedLoadcaseId}
              onChange={(e) => setSelectedLoadcaseId(e.target.value)}
              className="border-gray-300 rounded text-xs py-0.5 px-1.5 focus:outline-none focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="">Design Waterline (Geometry Only)</option>
              {loadcases.map((lc) => (
                <option key={lc.id} value={lc.id}>
                  {lc.name}
                </option>
              ))}
            </select>
          </div>
        </div>
        <div className="flex items-center gap-2">{/* Future: Export, Settings buttons */}</div>
      </div>

      {/* Error Message */}
      {error && (
        <div className="bg-red-50 border-l-4 border-red-400 p-2 mx-2 mt-2 flex-shrink-0">
          <div className="flex">
            <div className="flex-shrink-0">
              <svg className="h-3.5 w-3.5 text-red-400" fill="currentColor" viewBox="0 0 20 20">
                <path
                  fillRule="evenodd"
                  d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                  clipRule="evenodd"
                />
              </svg>
            </div>
            <div className="ml-2">
              <p className="text-xs text-red-700">{error}</p>
            </div>
          </div>
        </div>
      )}

      {/* Main Content: Two-column layout with single scroll */}
      <div className="flex-1 grid grid-cols-1 lg:grid-cols-[260px_1fr] xl:grid-cols-[340px_1fr] gap-0 overflow-auto">
        {/* Left Panel: Inputs */}
        <div className="bg-white border-r border-gray-200">
          {/* Setup Section */}
          <CollapsibleSection title="Setup" defaultExpanded={true}>
            {vessel && (
              <div className="space-y-1.5">
                <div className="grid grid-cols-2 gap-1.5 text-[11px]">
                  <div className="bg-gray-50 px-1.5 py-0.5 rounded">
                    <span className="text-gray-500">Lpp:</span>
                    <span className="ml-1 font-semibold text-gray-900">
                      {vessel.lpp} {lengthUnit}
                    </span>
                  </div>
                  <div className="bg-gray-50 px-1.5 py-0.5 rounded">
                    <span className="text-gray-500">Beam:</span>
                    <span className="ml-1 font-semibold text-gray-900">
                      {vessel.beam} {lengthUnit}
                    </span>
                  </div>
                </div>
                <div className="bg-gray-50 px-1.5 py-0.5 rounded text-[11px]">
                  <span className="text-gray-500">Design Draft:</span>
                  <span className="ml-1 font-semibold text-gray-900">
                    {vessel.designDraft} {lengthUnit}
                  </span>
                </div>
                {results.length > 0 && (
                  <div className="mt-2 pt-2 border-t border-gray-200 text-[11px] text-gray-600 space-y-0.5">
                    <div>
                      Draft range: {formatNumber(results[0].draft)} -{" "}
                      {formatNumber(results[results.length - 1].draft)} {lengthUnit}
                    </div>
                    <div>{results.length} computed points</div>
                  </div>
                )}
              </div>
            )}
          </CollapsibleSection>

          {/* Computation Range Section */}
          <CollapsibleSection
            title="Computation Range"
            defaultExpanded={true}
            badge={draftCount > 0 ? `${draftCount} pts` : undefined}
          >
            {loadcases.length === 0 ? (
              <div className="bg-yellow-50 border border-yellow-200 rounded p-2 text-[11px]">
                <div className="flex items-start">
                  <svg
                    className="h-3.5 w-3.5 text-yellow-400 mt-0.5 flex-shrink-0"
                    fill="currentColor"
                    viewBox="0 0 20 20"
                  >
                    <path
                      fillRule="evenodd"
                      d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
                      clipRule="evenodd"
                    />
                  </svg>
                  <div className="ml-1.5">
                    <p className="font-medium text-yellow-800">No loadcases defined</p>
                    <p className="mt-0.5 text-yellow-700">
                      Create at least one loadcase in the <strong>Loadcases</strong> tab.
                    </p>
                  </div>
                </div>
              </div>
            ) : (
              <div className="space-y-2">
                {/* Min Draft */}
                <div>
                  <label
                    htmlFor="minDraft"
                    className="block text-[11px] font-medium text-gray-600 mb-0.5"
                  >
                    Min Draft ({lengthUnit})
                  </label>
                  <input
                    type="number"
                    id="minDraft"
                    value={minDraft}
                    onChange={(e) => setMinDraft(parseFloat(e.target.value) || 0)}
                    step="0.1"
                    min="0"
                    className="block w-full border-gray-300 rounded shadow-sm py-0.5 px-1.5 text-xs focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                  />
                </div>

                {/* Max Draft */}
                <div>
                  <label
                    htmlFor="maxDraft"
                    className="block text-[11px] font-medium text-gray-600 mb-0.5"
                  >
                    Max Draft ({lengthUnit})
                  </label>
                  <input
                    type="number"
                    id="maxDraft"
                    value={maxDraft}
                    onChange={(e) => setMaxDraft(parseFloat(e.target.value) || 0)}
                    step="0.1"
                    min="0"
                    className="block w-full border-gray-300 rounded shadow-sm py-0.5 px-1.5 text-xs focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                  />
                </div>

                {/* Draft Step */}
                <div>
                  <label
                    htmlFor="draftStep"
                    className="block text-[11px] font-medium text-gray-600 mb-0.5"
                  >
                    Draft Step ({lengthUnit})
                  </label>
                  <input
                    type="number"
                    id="draftStep"
                    value={draftStep}
                    onChange={(e) => setDraftStep(parseFloat(e.target.value) || 0.1)}
                    step="0.1"
                    min="0.1"
                    className="block w-full border-gray-300 rounded shadow-sm py-0.5 px-1.5 text-xs focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                  />
                </div>

                {/* Compute Button */}
                <button
                  onClick={handleCompute}
                  disabled={computing || loadcases.length === 0}
                  className="w-full inline-flex items-center justify-center px-3 py-1.5 border border-transparent text-xs font-medium rounded shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {computing ? (
                    <>
                      <svg
                        className="animate-spin -ml-1 mr-1.5 h-3.5 w-3.5 text-white"
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
                        className="-ml-1 mr-1.5 h-3.5 w-3.5"
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
            )}
          </CollapsibleSection>

          {/* Hull Parameters Section */}
          {vessel && (
            <CollapsibleSection title="Hull Parameters" defaultExpanded={false}>
              <div className="space-y-1 text-[11px]">
                <div className="flex justify-between">
                  <span className="text-gray-600">Stations:</span>
                  <span className="font-medium text-gray-900">{vessel.stationsCount || 0}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Waterlines:</span>
                  <span className="font-medium text-gray-900">{vessel.waterlinesCount || 0}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-600">Offsets:</span>
                  <span className="font-medium text-gray-900">{vessel.offsetsCount || 0}</span>
                </div>
              </div>
            </CollapsibleSection>
          )}
        </div>

        {/* Right Panel: Results */}
        <div className="bg-gray-50">
          {results.length > 0 ? (
            <div className="flex flex-col">
              {/* Results Header */}
              <div className="bg-white border-b border-gray-200 px-2 py-1.5 sticky top-0 z-10">
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-xs font-medium text-gray-900">Hydrostatic Table</h3>
                    {computationTime && (
                      <p className="text-[10px] text-gray-500">
                        Computed in {computationTime.toFixed(0)} ms
                      </p>
                    )}
                  </div>
                  <span className="text-[10px] text-gray-500">{results.length} rows</span>
                </div>
              </div>

              {/* Results Table */}
              <div className="bg-white">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50 sticky top-[44px] z-10">
                    <tr>
                      <th className="px-1.5 py-1 text-left text-[10px] font-medium text-gray-500 uppercase tracking-wider">
                        Draft ({lengthUnit})
                      </th>
                      <th className="px-1.5 py-1 text-left text-[10px] font-medium text-gray-500 uppercase tracking-wider">
                        ∆ ({massUnit})
                      </th>
                      <th className="px-1.5 py-1 text-left text-[10px] font-medium text-gray-500 uppercase tracking-wider">
                        KB ({lengthUnit})
                      </th>
                      <th className="px-1.5 py-1 text-left text-[10px] font-medium text-gray-500 uppercase tracking-wider">
                        LCB ({lengthUnit})
                      </th>
                      <th className="px-1.5 py-1 text-left text-[10px] font-medium text-gray-500 uppercase tracking-wider">
                        BMt ({lengthUnit})
                      </th>
                      <th className="px-1.5 py-1 text-left text-[10px] font-medium text-gray-500 uppercase tracking-wider">
                        GMt ({lengthUnit})
                      </th>
                      <th className="px-1.5 py-1 text-left text-[10px] font-medium text-gray-500 uppercase tracking-wider">
                        Awp ({areaUnit})
                      </th>
                      <th className="px-1.5 py-1 text-left text-[10px] font-medium text-gray-500 uppercase tracking-wider">
                        Cb
                      </th>
                      <th className="px-1.5 py-1 text-left text-[10px] font-medium text-gray-500 uppercase tracking-wider">
                        Cp
                      </th>
                      <th className="px-1.5 py-1 text-left text-[10px] font-medium text-gray-500 uppercase tracking-wider">
                        Cwp
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-100">
                    {results.map((result, idx) => (
                      <tr key={idx} className="hover:bg-gray-50">
                        <td className="px-1.5 py-0.5 text-[11px] text-gray-900 font-medium whitespace-nowrap">
                          {formatNumber(result.draft)}
                        </td>
                        <td className="px-1.5 py-0.5 text-[11px] text-gray-900 whitespace-nowrap">
                          {formatNumber(result.dispWeight, 0)}
                        </td>
                        <td className="px-1.5 py-0.5 text-[11px] text-gray-900 whitespace-nowrap">
                          {formatNumber(result.kBz)}
                        </td>
                        <td className="px-1.5 py-0.5 text-[11px] text-gray-900 whitespace-nowrap">
                          {formatNumber(result.lCBx)}
                        </td>
                        <td className="px-1.5 py-0.5 text-[11px] text-gray-900 whitespace-nowrap">
                          {formatNumber(result.bMt)}
                        </td>
                        <td className="px-1.5 py-0.5 text-[11px] text-gray-900 whitespace-nowrap">
                          {formatNumber(result.gMt)}
                        </td>
                        <td className="px-1.5 py-0.5 text-[11px] text-gray-900 whitespace-nowrap">
                          {formatNumber(result.awp, 1)}
                        </td>
                        <td className="px-1.5 py-0.5 text-[11px] text-gray-900 whitespace-nowrap">
                          {formatNumber(result.cb, 3)}
                        </td>
                        <td className="px-1.5 py-0.5 text-[11px] text-gray-900 whitespace-nowrap">
                          {formatNumber(result.cp, 3)}
                        </td>
                        <td className="px-1.5 py-0.5 text-[11px] text-gray-900 whitespace-nowrap">
                          {formatNumber(result.cwp, 3)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              {/* Hydrostatic Curves Placeholder */}
              <div className="bg-white border-t border-gray-200 p-2 mt-1">
                <h3 className="text-xs font-medium text-gray-900 mb-1.5">Hydrostatic Curves</h3>
                <div className="bg-gray-50 rounded border-2 border-dashed border-gray-300 p-6 text-center">
                  <svg
                    className="mx-auto h-6 w-6 text-gray-400"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M7 12l3-3 3 3 4-4M8 21l4-4 4 4M3 4h18M4 4h16v12a1 1 0 01-1 1H5a1 1 0 01-1-1V4z"
                    />
                  </svg>
                  <p className="mt-1.5 text-[10px] text-gray-500">
                    Curve visualization will be available here
                  </p>
                </div>
              </div>
            </div>
          ) : (
            <div className="flex items-center justify-center p-6">
              <div className="text-center">
                <svg
                  className="mx-auto h-10 w-10 text-gray-400"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                  />
                </svg>
                <h3 className="mt-2 text-xs font-medium text-gray-900">No results yet</h3>
                <p className="mt-1 text-[11px] text-gray-500">
                  Set your computation parameters and click Compute to generate hydrostatic table
                </p>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
});

export default ComputationsTab;
