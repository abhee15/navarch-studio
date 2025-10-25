import { useState, useEffect } from "react";
import { curvesApi, loadcasesApi } from "../../../services/hydrostaticsApi";
import type { Loadcase, CurveData } from "../../../types/hydrostatics";
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

interface CurvesTabProps {
  vesselId: string;
}

const CURVE_TYPES = [
  { id: "displacement", label: "Displacement", color: "#3B82F6" },
  { id: "kb", label: "KB (Center of Buoyancy)", color: "#10B981" },
  { id: "lcb", label: "LCB (Longitudinal CB)", color: "#F59E0B" },
  { id: "gmt", label: "GMt (Metacentric Height)", color: "#EF4444" },
  { id: "awp", label: "Awp (Waterplane Area)", color: "#8B5CF6" },
];

export function CurvesTab({ vesselId }: CurvesTabProps) {
  const [loadcases, setLoadcases] = useState<Loadcase[]>([]);
  const [selectedLoadcaseId, setSelectedLoadcaseId] = useState<string>("");
  const [selectedCurves, setSelectedCurves] = useState<string[]>(["displacement", "kb"]);
  const [minDraft, setMinDraft] = useState<number>(1);
  const [maxDraft, setMaxDraft] = useState<number>(10);
  const [points, setPoints] = useState<number>(50);
  const [curves, setCurves] = useState<Record<string, CurveData>>({});
  const [loading, setLoading] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadLoadcases();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [vesselId]);

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

  const handleGenerateCurves = async () => {
    if (selectedCurves.length === 0) {
      alert("Please select at least one curve to generate");
      return;
    }

    try {
      setGenerating(true);
      setError(null);

      const response = await curvesApi.generate(vesselId, {
        loadcaseId: selectedLoadcaseId || undefined,
        types: selectedCurves,
        minDraft,
        maxDraft,
        points,
      });

      setCurves(response.curves);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to generate curves");
    } finally {
      setGenerating(false);
    }
  };

  const toggleCurve = (curveId: string) => {
    setSelectedCurves((prev) =>
      prev.includes(curveId) ? prev.filter((id) => id !== curveId) : [...prev, curveId]
    );
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
      {/* Parameters */}
      <div className="bg-white shadow rounded-lg p-6">
        <h3 className="text-lg font-medium text-gray-900 mb-4">Curve Generation Parameters</h3>

        {loadcases.length === 0 ? (
          <div className="bg-yellow-50 border border-yellow-200 rounded-md p-4">
            <div className="flex">
              <svg className="h-5 w-5 text-yellow-400" fill="currentColor" viewBox="0 0 20 20">
                <path
                  fillRule="evenodd"
                  d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
                  clipRule="evenodd"
                />
              </svg>
              <div className="ml-3">
                <p className="text-sm text-yellow-700">
                  Create at least one loadcase in the <strong>Loadcases</strong> tab (optional for
                  curves).
                </p>
              </div>
            </div>
          </div>
        ) : (
          <div className="space-y-4">
            {/* Loadcase Selection */}
            <div>
              <label htmlFor="loadcase" className="block text-sm font-medium text-gray-700 mb-2">
                Loadcase (Optional)
              </label>
              <select
                id="loadcase"
                value={selectedLoadcaseId}
                onChange={(e) => setSelectedLoadcaseId(e.target.value)}
                className="block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
              >
                <option value="">None (geometry only)</option>
                {loadcases.map((lc) => (
                  <option key={lc.id} value={lc.id}>
                    {lc.name}
                  </option>
                ))}
              </select>
            </div>

            {/* Curve Selection */}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Select Curves to Generate
              </label>
              <div className="grid grid-cols-2 md:grid-cols-3 gap-2">
                {CURVE_TYPES.map((curve) => (
                  <label
                    key={curve.id}
                    className="flex items-center p-3 border rounded-lg cursor-pointer hover:bg-gray-50"
                  >
                    <input
                      type="checkbox"
                      checked={selectedCurves.includes(curve.id)}
                      onChange={() => toggleCurve(curve.id)}
                      className="h-4 w-4 text-blue-600 rounded"
                    />
                    <div className="ml-2 flex items-center">
                      <div
                        className="w-3 h-3 rounded-full mr-2"
                        style={{ backgroundColor: curve.color }}
                      ></div>
                      <span className="text-sm">{curve.label}</span>
                    </div>
                  </label>
                ))}
              </div>
            </div>

            {/* Draft Range */}
            <div className="grid grid-cols-3 gap-4">
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
              <div>
                <label htmlFor="points" className="block text-sm font-medium text-gray-700">
                  Points
                </label>
                <input
                  type="number"
                  id="points"
                  value={points}
                  onChange={(e) => setPoints(parseInt(e.target.value) || 50)}
                  step="10"
                  min="10"
                  max="200"
                  className="mt-1 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm"
                />
              </div>
            </div>

            {/* Generate Button */}
            <div className="flex justify-end">
              <button
                onClick={handleGenerateCurves}
                disabled={generating || selectedCurves.length === 0}
                className="inline-flex items-center px-6 py-3 border border-transparent text-base font-medium rounded-md shadow-sm text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {generating ? (
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
                    Generating...
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
                        d="M7 12l3-3 3 3 4-4M8 21l4-4 4 4M3 4h18M4 4h16v12a1 1 0 01-1 1H5a1 1 0 01-1-1V4z"
                      />
                    </svg>
                    Generate Curves
                  </>
                )}
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Error Message */}
      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded">
          <strong className="font-bold">Error: </strong>
          <span>{error}</span>
        </div>
      )}

      {/* Curves Display */}
      {Object.keys(curves).length > 0 && (
        <div className="space-y-6">
          {Object.entries(curves).map(([curveType, curveData]) => {
            const curveInfo = CURVE_TYPES.find((t) => t.id === curveType);
            return (
              <div key={curveType} className="bg-white shadow rounded-lg p-6">
                <h4 className="text-lg font-medium text-gray-900 mb-4">
                  {curveInfo?.label || curveType}
                </h4>
                <ResponsiveContainer width="100%" height={400}>
                  <LineChart data={curveData.points}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis
                      dataKey="x"
                      label={{ value: curveData.xLabel, position: "insideBottom", offset: -5 }}
                    />
                    <YAxis
                      label={{ value: curveData.yLabel, angle: -90, position: "insideLeft" }}
                    />
                    <Tooltip />
                    <Legend />
                    <Line
                      type="monotone"
                      dataKey="y"
                      stroke={curveInfo?.color || "#3B82F6"}
                      strokeWidth={2}
                      dot={false}
                      name={curveData.yLabel}
                    />
                  </LineChart>
                </ResponsiveContainer>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

export default CurvesTab;
