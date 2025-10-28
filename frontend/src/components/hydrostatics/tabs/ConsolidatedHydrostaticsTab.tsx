import { useState, useEffect } from "react";
import { observer } from "mobx-react-lite";
import { hydrostaticsApi, loadcasesApi } from "../../../services/hydrostaticsApi";
import type { HydroResult, Loadcase, VesselDetails } from "../../../types/hydrostatics";
import { settingsStore } from "../../../stores/SettingsStore";
import { getUnitSymbol } from "../../../utils/unitSymbols";
import { CollapsibleSection } from "../CollapsibleSection";
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

interface ConsolidatedHydrostaticsTabProps {
  vesselId: string;
  vessel?: VesselDetails;
}

// Water types with their densities
const waterTypes = [
  { label: "Fresh Water", density: 1000 },
  { label: "Salt Water", density: 1025 },
  { label: "Brackish Water", density: 1012 },
];

const integrationRules = ["Simpson 1/3", "Trapezoidal", "Simpson 3/8"];
const smoothingOptions = ["None", "Moving Average", "Spline"];

export const ConsolidatedHydrostaticsTab = observer(({ vesselId, vessel }: ConsolidatedHydrostaticsTabProps) => {
  // State
  const [loadcases, setLoadcases] = useState<Loadcase[]>([]);
  const [selectedLoadcaseId, setSelectedLoadcaseId] = useState<string>("");
  const [waterType, setWaterType] = useState<string>("Salt Water");
  const [minDraft, setMinDraft] = useState<number>(3);
  const [maxDraft, setMaxDraft] = useState<number>(9);
  const [draftStep, setDraftStep] = useState<number>(0.5);
  const [integrationRule, setIntegrationRule] = useState<string>("Simpson 1/3");
  const [smoothing, setSmoothing] = useState<string>("None");
  const [kg, setKg] = useState<number>(0);
  const [lcg, setLcg] = useState<number>(0);
  const [tcg, setTcg] = useState<number>(0);
  const [results, setResults] = useState<HydroResult[]>([]);
  const [loading, setLoading] = useState(false);
  const [computing, setComputing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [computationTime, setComputationTime] = useState<number | null>(null);
  const [selectedCurveType, setSelectedCurveType] = useState<"hydrostatic" | "bonjean" | "cross-curves">("hydrostatic");

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

  // Units
  const displayUnits = settingsStore.preferredUnits;
  const lengthUnit = getUnitSymbol(displayUnits, "Length");
  const massUnit = getUnitSymbol(displayUnits, "Mass");
  const areaUnit = getUnitSymbol(displayUnits, "Area");

  const formatNumber = (value: number | undefined | null, decimals: number = 2): string => {
    if (value === null || value === undefined) return "—";
    return value.toFixed(decimals);
  };

  const draftCount = minDraft < maxDraft ? Math.ceil((maxDraft - minDraft) / draftStep) + 1 : 0;

  // Get current result for display (use middle draft as reference)
  const currentResult = results.length > 0 ? results[Math.floor(results.length / 2)] : null;

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col overflow-hidden bg-gray-50">
      {/* TOP TOOLBAR */}
      <div className="bg-white border-b border-gray-200 px-3 py-2 flex items-center gap-3 flex-shrink-0">
        {/* Vessel Selector */}
        <div className="flex items-center gap-1.5">
          <label className="text-xs font-medium text-gray-600">Vessel:</label>
          <div className="px-2 py-1 bg-gray-50 rounded text-xs font-medium text-gray-900 border border-gray-200">
            {vessel?.name || "Unknown"}
          </div>
        </div>

        {/* Condition Selector */}
        <div className="flex items-center gap-1.5">
          <label className="text-xs font-medium text-gray-600">Condition:</label>
          <select
            value={selectedLoadcaseId}
            onChange={(e) => setSelectedLoadcaseId(e.target.value)}
            className="border-gray-300 rounded text-xs py-1 px-2 focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          >
            <option value="">Design Waterline</option>
            {loadcases.map((lc) => (
              <option key={lc.id} value={lc.id}>
                {lc.name}
              </option>
            ))}
          </select>
        </div>

        {/* Water Type */}
        <div className="flex items-center gap-1.5">
          <label className="text-xs font-medium text-gray-600">Water Type:</label>
          <select
            value={waterType}
            onChange={(e) => setWaterType(e.target.value)}
            className="border-gray-300 rounded text-xs py-1 px-2 focus:outline-none focus:ring-blue-500 focus:border-blue-500"
          >
            {waterTypes.map((wt) => (
              <option key={wt.label} value={wt.label}>
                {wt.label}
              </option>
            ))}
          </select>
        </div>

        {/* Presets */}
        <div className="flex items-center gap-1.5">
          <label className="text-xs font-medium text-gray-600">Presets:</label>
          <select className="border-gray-300 rounded text-xs py-1 px-2 focus:outline-none focus:ring-blue-500 focus:border-blue-500">
            <option>Standard</option>
            <option>Detailed</option>
            <option>Quick</option>
          </select>
        </div>

        {/* Compute Button */}
        <button
          onClick={handleCompute}
          disabled={computing}
          className="ml-auto inline-flex items-center px-4 py-1.5 border border-transparent text-xs font-semibold rounded shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {computing ? (
            <>
              <svg className="animate-spin -ml-1 mr-1.5 h-3.5 w-3.5 text-white" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              Computing...
            </>
          ) : (
            "Compute"
          )}
        </button>

        {computationTime && (
          <span className="text-[10px] text-gray-500">{computationTime.toFixed(0)}ms</span>
        )}
      </div>

      {/* Error Message */}
      {error && (
        <div className="bg-red-50 border-l-4 border-red-400 p-2 mx-3 mt-2 flex-shrink-0">
          <div className="flex">
            <svg className="h-3.5 w-3.5 text-red-400 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
              <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
            </svg>
            <p className="ml-2 text-xs text-red-700">{error}</p>
          </div>
        </div>
      )}

      {/* TWO-COLUMN LAYOUT */}
      <div className="flex-1 flex gap-0 overflow-hidden">
        
        {/* LEFT PANEL: Inputs */}
        <div className="w-64 bg-white border-r border-gray-200 overflow-y-auto overflow-x-hidden">
          
          {/* Geometry & Reference */}
          <CollapsibleSection title="Geometry & Reference" defaultExpanded={true}>
            <div className="space-y-2">
              <div className="min-w-0">
                <label className="block text-[11px] font-medium text-gray-600 mb-0.5 truncate">Hull Source</label>
                <select className="w-full border-gray-300 rounded text-xs py-0.5 px-1.5 focus:outline-none focus:ring-blue-500 focus:border-blue-500">
                  <option>Offsets</option>
                  <option>3D Model</option>
                  <option>Lines Plan</option>
                </select>
              </div>
              
              <div className="grid grid-cols-2 gap-2 min-w-0">
                <div className="min-w-0">
                  <label className="block text-[11px] font-medium text-gray-600 mb-0.5 truncate">Lpp ({lengthUnit})</label>
                  <input
                    type="number"
                    value={vessel?.lpp || 0}
                    readOnly
                    className="w-full border-gray-300 rounded text-xs py-0.5 px-1.5 bg-gray-50"
                  />
                </div>
                <div className="min-w-0">
                  <label className="block text-[11px] font-medium text-gray-600 mb-0.5 truncate">Beam ({lengthUnit})</label>
                  <input
                    type="number"
                    value={vessel?.beam || 0}
                    readOnly
                    className="w-full border-gray-300 rounded text-xs py-0.5 px-1.5 bg-gray-50"
                  />
                </div>
              </div>

              <div className="min-w-0">
                <label className="block text-[11px] font-medium text-gray-600 mb-0.5 truncate">Draft ({lengthUnit})</label>
                <div className="flex gap-1 min-w-0">
                  <input
                    type="number"
                    value={vessel?.designDraft || 0}
                    readOnly
                    className="flex-1 min-w-0 border-gray-300 rounded text-xs py-0.5 px-1.5 bg-gray-50"
                  />
                  <select className="flex-shrink-0 border-gray-300 rounded text-xs py-0.5 px-1 focus:outline-none focus:ring-blue-500 focus:border-blue-500">
                    <option>Design</option>
                  </select>
                </div>
              </div>

              <div className="min-w-0">
                <label className="block text-[11px] font-medium text-gray-600 mb-0.5 truncate">Trim</label>
                <div className="flex gap-1 min-w-0">
                  <input
                    type="number"
                    value={0}
                    className="flex-1 min-w-0 border-gray-300 rounded text-xs py-0.5 px-1.5"
                  />
                  <select className="flex-shrink-0 border-gray-300 rounded text-xs py-0.5 px-1 focus:outline-none focus:ring-blue-500 focus:border-blue-500">
                    <option>Fixed</option>
                  </select>
                </div>
              </div>
            </div>
          </CollapsibleSection>

          {/* Mass Properties */}
          <CollapsibleSection title="Mass Properties" defaultExpanded={true}>
            <div className="space-y-2">
              <div className="min-w-0">
                <label className="block text-[11px] font-medium text-gray-600 mb-0.5 truncate">KG ({lengthUnit})</label>
                <div className="flex gap-1 min-w-0">
                  <input
                    type="number"
                    value={kg}
                    onChange={(e) => setKg(parseFloat(e.target.value) || 0)}
                    step="0.1"
                    className="flex-1 min-w-0 border-gray-300 rounded text-xs py-0.5 px-1.5"
                  />
                  <select className="flex-shrink-0 border-gray-300 rounded text-xs py-0.5 px-1 focus:outline-none focus:ring-blue-500 focus:border-blue-500">
                    <option>Above BL</option>
                  </select>
                </div>
              </div>

              <div className="min-w-0">
                <label className="block text-[11px] font-medium text-gray-600 mb-0.5 truncate">LCG ({lengthUnit})</label>
                <div className="flex gap-1 min-w-0">
                  <input
                    type="number"
                    value={lcg}
                    onChange={(e) => setLcg(parseFloat(e.target.value) || 0)}
                    step="0.1"
                    className="flex-1 min-w-0 border-gray-300 rounded text-xs py-0.5 px-1.5"
                  />
                  <select className="flex-shrink-0 border-gray-300 rounded text-xs py-0.5 px-1 focus:outline-none focus:ring-blue-500 focus:border-blue-500">
                    <option>Midship</option>
                  </select>
                </div>
              </div>

              <div className="min-w-0">
                <label className="block text-[11px] font-medium text-gray-600 mb-0.5 truncate">TCG ({lengthUnit})</label>
                <div className="flex gap-1 min-w-0">
                  <input
                    type="number"
                    value={tcg}
                    onChange={(e) => setTcg(parseFloat(e.target.value) || 0)}
                    step="0.1"
                    className="flex-1 min-w-0 border-gray-300 rounded text-xs py-0.5 px-1.5"
                  />
                  <select className="flex-shrink-0 border-gray-300 rounded text-xs py-0.5 px-1 focus:outline-none focus:ring-blue-500 focus:border-blue-500">
                    <option>From CL</option>
                  </select>
                </div>
              </div>
            </div>
          </CollapsibleSection>

          {/* Computation Controls */}
          <CollapsibleSection title="Computation Controls" defaultExpanded={true}>
            <div className="space-y-2 min-w-0">
              <div className="min-w-0">
                <label className="block text-[11px] font-medium text-gray-600 mb-0.5 truncate">
                  Range ({lengthUnit}) - {draftCount} pts
                </label>
                <div className="flex gap-0.5 items-center text-xs min-w-0">
                  <input
                    type="number"
                    value={minDraft}
                    onChange={(e) => setMinDraft(parseFloat(e.target.value) || 0)}
                    step="0.1"
                    placeholder="Min"
                    className="flex-1 min-w-0 border-gray-300 rounded py-0.5 px-1 text-[11px]"
                  />
                  <span className="text-gray-400 text-[10px]">to</span>
                  <input
                    type="number"
                    value={maxDraft}
                    onChange={(e) => setMaxDraft(parseFloat(e.target.value) || 0)}
                    step="0.1"
                    placeholder="Max"
                    className="flex-1 min-w-0 border-gray-300 rounded py-0.5 px-1 text-[11px]"
                  />
                  <span className="text-gray-400 text-[10px]">by</span>
                  <input
                    type="number"
                    value={draftStep}
                    onChange={(e) => setDraftStep(parseFloat(e.target.value) || 0.1)}
                    step="0.1"
                    placeholder="Step"
                    className="flex-1 min-w-0 border-gray-300 rounded py-0.5 px-1 text-[11px]"
                  />
                </div>
              </div>

              <div className="min-w-0">
                <label className="block text-[11px] font-medium text-gray-600 mb-0.5 truncate">Integration</label>
                <select
                  value={integrationRule}
                  onChange={(e) => setIntegrationRule(e.target.value)}
                  className="w-full border-gray-300 rounded text-xs py-0.5 px-1.5 focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                >
                  {integrationRules.map((rule) => (
                    <option key={rule} value={rule}>
                      {rule}
                    </option>
                  ))}
                </select>
              </div>

              <div className="min-w-0">
                <label className="block text-[11px] font-medium text-gray-600 mb-0.5 truncate">Smoothing</label>
                <select
                  value={smoothing}
                  onChange={(e) => setSmoothing(e.target.value)}
                  className="w-full border-gray-300 rounded text-xs py-0.5 px-1.5 focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                >
                  {smoothingOptions.map((opt) => (
                    <option key={opt} value={opt}>
                      {opt}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          </CollapsibleSection>

          {/* Environment */}
          <CollapsibleSection title="Environment" defaultExpanded={false}>
            <div className="space-y-2">
              <div className="min-w-0">
                <label className="block text-[11px] font-medium text-gray-600 mb-0.5 truncate">Density (kg/m³)</label>
                <input
                  type="number"
                  value={waterTypes.find((wt) => wt.label === waterType)?.density || 1025}
                  readOnly
                  className="w-full border-gray-300 rounded text-xs py-0.5 px-1.5 bg-gray-50"
                />
              </div>
            </div>
          </CollapsibleSection>
        </div>

        {/* RIGHT PANEL: Results */}
        <div className="flex-1 overflow-y-auto bg-gray-50">
          {results.length > 0 && currentResult ? (
            <div className="flex flex-col">
              {/* Summary Bar */}
              <div className="bg-white border-b border-gray-200 px-3 py-2">
                <div className="grid grid-cols-6 gap-3 text-center">
                  <div>
                    <div className="text-[10px] text-gray-500 uppercase">Displacement</div>
                    <div className="text-xs font-semibold text-gray-900">{formatNumber(currentResult.dispWeight, 0)} {massUnit}</div>
                  </div>
                  <div>
                    <div className="text-[10px] text-gray-500 uppercase">Draft</div>
                    <div className="text-xs font-semibold text-gray-900">{formatNumber(currentResult.draft)} {lengthUnit}</div>
                  </div>
                  <div>
                    <div className="text-[10px] text-gray-500 uppercase">BMt</div>
                    <div className="text-xs font-semibold text-gray-900">{formatNumber(currentResult.bMt)} {lengthUnit}</div>
                  </div>
                  <div>
                    <div className="text-[10px] text-gray-500 uppercase">GMt</div>
                    <div className="text-xs font-semibold text-gray-900">{formatNumber(currentResult.gMt)} {lengthUnit}</div>
                  </div>
                  <div>
                    <div className="text-[10px] text-gray-500 uppercase">LCB</div>
                    <div className="text-xs font-semibold text-gray-900">{formatNumber(currentResult.lCBx)} {lengthUnit}</div>
                  </div>
                  <div>
                    <div className="text-[10px] text-gray-500 uppercase">WPA</div>
                    <div className="text-xs font-semibold text-gray-900">{formatNumber(currentResult.awp, 1)} {areaUnit}</div>
                  </div>
                </div>
              </div>

              {/* Curves Visualization with Type Selector */}
              <div className="bg-white m-3 p-3 rounded border border-gray-200">
                <div className="flex items-center justify-between mb-2">
                  <h3 className="text-xs font-semibold text-gray-900">Curves Visualization</h3>
                  <select
                    value={selectedCurveType}
                    onChange={(e) => setSelectedCurveType(e.target.value as "hydrostatic" | "bonjean" | "cross-curves")}
                    className="border-gray-300 rounded text-xs py-0.5 px-2 focus:outline-none focus:ring-blue-500 focus:border-blue-500"
                  >
                    <option value="hydrostatic">Hydrostatic Curves</option>
                    <option value="bonjean">Bonjean Curves</option>
                    <option value="cross-curves">Cross-Curves (KN)</option>
                  </select>
                </div>
                
                {/* Curve Display Area */}
                <div className="bg-gray-50 rounded border border-gray-200 p-2">
                  {selectedCurveType === "hydrostatic" && (
                    <div>
                      <ResponsiveContainer width="100%" height={300}>
                        <LineChart data={results.map(r => ({
                          draft: r.draft,
                          displacement: r.dispWeight / 1000, // Convert to tonnes for readability
                          kb: r.kBz,
                          lcb: r.lCBx,
                          bmt: r.bMt,
                          gmt: r.gMt,
                          wpa: r.awp,
                        }))}>
                          <CartesianGrid strokeDasharray="3 3" stroke="#e5e7eb" />
                          <XAxis 
                            dataKey="draft" 
                            label={{ value: `Draft (${lengthUnit})`, position: 'insideBottom', offset: -5, style: { fontSize: '11px' } }}
                            tick={{ fontSize: 10 }}
                            stroke="#6b7280"
                          />
                          <YAxis 
                            yAxisId="left"
                            label={{ value: `Displacement (tonnes), KB, LCB, BMt, GMt (${lengthUnit})`, angle: -90, position: 'insideLeft', style: { fontSize: '10px' } }}
                            tick={{ fontSize: 10 }}
                            stroke="#6b7280"
                          />
                          <YAxis 
                            yAxisId="right" 
                            orientation="right"
                            label={{ value: `WPA (${areaUnit})`, angle: 90, position: 'insideRight', style: { fontSize: '10px' } }}
                            tick={{ fontSize: 10 }}
                            stroke="#6b7280"
                          />
                          <Tooltip 
                            contentStyle={{ fontSize: '11px', padding: '4px 8px' }}
                            formatter={(value: number) => formatNumber(value, 2)}
                          />
                          <Legend 
                            wrapperStyle={{ fontSize: '10px', paddingTop: '8px' }}
                            iconSize={10}
                          />
                          <Line 
                            yAxisId="left"
                            type="monotone" 
                            dataKey="displacement" 
                            stroke="#3B82F6" 
                            strokeWidth={1.5}
                            dot={false}
                            name="Disp (t)"
                          />
                          <Line 
                            yAxisId="left"
                            type="monotone" 
                            dataKey="kb" 
                            stroke="#10B981" 
                            strokeWidth={1.5}
                            dot={false}
                            name="KB"
                          />
                          <Line 
                            yAxisId="left"
                            type="monotone" 
                            dataKey="lcb" 
                            stroke="#F59E0B" 
                            strokeWidth={1.5}
                            dot={false}
                            name="LCB"
                          />
                          <Line 
                            yAxisId="left"
                            type="monotone" 
                            dataKey="bmt" 
                            stroke="#8B5CF6" 
                            strokeWidth={1.5}
                            dot={false}
                            name="BMt"
                          />
                          <Line 
                            yAxisId="left"
                            type="monotone" 
                            dataKey="gmt" 
                            stroke="#EF4444" 
                            strokeWidth={1.5}
                            dot={false}
                            name="GMt"
                          />
                          <Line 
                            yAxisId="right"
                            type="monotone" 
                            dataKey="wpa" 
                            stroke="#EC4899" 
                            strokeWidth={1.5}
                            dot={false}
                            name="WPA"
                          />
                        </LineChart>
                      </ResponsiveContainer>
                    </div>
                  )}
                  
                  {selectedCurveType === "bonjean" && (
                    <div className="flex items-center justify-center h-64">
                      <div className="text-center text-gray-500">
                        <svg className="mx-auto h-8 w-8 text-gray-400 mb-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                        </svg>
                        <p className="text-xs font-medium">Bonjean Curves</p>
                        <p className="text-[10px] text-gray-400 mt-1">Sectional areas vs waterline height at each station</p>
                        <p className="text-[10px] text-gray-400 mt-2">Requires station-level computation</p>
                      </div>
                    </div>
                  )}
                  
                  {selectedCurveType === "cross-curves" && (
                    <div className="flex items-center justify-center h-64">
                      <div className="text-center text-gray-500">
                        <svg className="mx-auto h-8 w-8 text-gray-400 mb-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6" />
                        </svg>
                        <p className="text-xs font-medium">Cross-Curves of Stability (KN)</p>
                        <p className="text-[10px] text-gray-400 mt-1">Righting lever vs heel angle for various displacements</p>
                        <p className="text-[10px] text-gray-400 mt-2">Requires stability computation</p>
                      </div>
                    </div>
                  )}
                </div>
              </div>

              {/* Hydrostatic Table */}
              <div className="bg-white m-3 mt-0 p-3 rounded border border-gray-200">
                <h3 className="text-xs font-semibold text-gray-900 mb-2">Hydrostatic Table</h3>
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200 text-xs">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-2 py-1 text-left text-[10px] font-medium text-gray-500 uppercase">Draft</th>
                        <th className="px-2 py-1 text-left text-[10px] font-medium text-gray-500 uppercase">Displ.</th>
                        <th className="px-2 py-1 text-left text-[10px] font-medium text-gray-500 uppercase">KB</th>
                        <th className="px-2 py-1 text-left text-[10px] font-medium text-gray-500 uppercase">BMt</th>
                        <th className="px-2 py-1 text-left text-[10px] font-medium text-gray-500 uppercase">GMt</th>
                        <th className="px-2 py-1 text-left text-[10px] font-medium text-gray-500 uppercase">LCB</th>
                        <th className="px-2 py-1 text-left text-[10px] font-medium text-gray-500 uppercase">WPA</th>
                        <th className="px-2 py-1 text-left text-[10px] font-medium text-gray-500 uppercase">Cb</th>
                        <th className="px-2 py-1 text-left text-[10px] font-medium text-gray-500 uppercase">Cp</th>
                        <th className="px-2 py-1 text-left text-[10px] font-medium text-gray-500 uppercase">Cwp</th>
                      </tr>
                    </thead>
                    <tbody className="bg-white divide-y divide-gray-100">
                      {results.map((result, idx) => (
                        <tr key={idx} className="hover:bg-gray-50">
                          <td className="px-2 py-1 whitespace-nowrap font-medium">{formatNumber(result.draft)}</td>
                          <td className="px-2 py-1 whitespace-nowrap">{formatNumber(result.dispWeight, 0)}</td>
                          <td className="px-2 py-1 whitespace-nowrap">{formatNumber(result.kBz)}</td>
                          <td className="px-2 py-1 whitespace-nowrap">{formatNumber(result.bMt)}</td>
                          <td className="px-2 py-1 whitespace-nowrap">{formatNumber(result.gMt)}</td>
                          <td className="px-2 py-1 whitespace-nowrap">{formatNumber(result.lCBx)}</td>
                          <td className="px-2 py-1 whitespace-nowrap">{formatNumber(result.awp, 1)}</td>
                          <td className="px-2 py-1 whitespace-nowrap">{formatNumber(result.cb, 3)}</td>
                          <td className="px-2 py-1 whitespace-nowrap">{formatNumber(result.cp, 3)}</td>
                          <td className="px-2 py-1 whitespace-nowrap">{formatNumber(result.cwp, 3)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          ) : (
            <div className="flex items-center justify-center h-full p-8">
              <div className="text-center">
                <svg className="mx-auto h-12 w-12 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
                <h3 className="mt-2 text-sm font-medium text-gray-900">Ready to Compute</h3>
                <p className="mt-1 text-xs text-gray-500">
                  Configure parameters in the left panel and click "Compute" in the toolbar
                </p>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
});

export default ConsolidatedHydrostaticsTab;
