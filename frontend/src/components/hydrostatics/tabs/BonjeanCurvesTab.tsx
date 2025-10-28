import { useState, useEffect } from "react";
import { curvesApi } from "../../../services/hydrostaticsApi";
import type { BonjeanCurve } from "../../../types/hydrostatics";
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

interface BonjeanCurvesTabProps {
  vesselId: string;
}

const STATION_COLORS = [
  "#3B82F6", // Blue
  "#10B981", // Green
  "#F59E0B", // Amber
  "#EF4444", // Red
  "#8B5CF6", // Purple
  "#EC4899", // Pink
  "#14B8A6", // Teal
  "#F97316", // Orange
  "#6366F1", // Indigo
  "#84CC16", // Lime
];

export function BonjeanCurvesTab({ vesselId }: BonjeanCurvesTabProps) {
  const [curves, setCurves] = useState<BonjeanCurve[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedStations, setSelectedStations] = useState<number[]>([]);
  const [showAllStations, setShowAllStations] = useState(true);

  useEffect(() => {
    loadBonjeanCurves();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [vesselId]);

  const loadBonjeanCurves = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await curvesApi.getBonjean(vesselId);
      setCurves(data.curves);
      
      // Select all stations by default
      if (data.curves.length > 0) {
        setSelectedStations(data.curves.map((c) => c.stationIndex));
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load Bonjean curves");
    } finally {
      setLoading(false);
    }
  };

  const handleStationToggle = (stationIndex: number) => {
    if (selectedStations.includes(stationIndex)) {
      setSelectedStations(selectedStations.filter((s) => s !== stationIndex));
    } else {
      setSelectedStations([...selectedStations, stationIndex]);
    }
  };

  const handleToggleAll = () => {
    if (showAllStations) {
      setSelectedStations([]);
    } else {
      setSelectedStations(curves.map((c) => c.stationIndex));
    }
    setShowAllStations(!showAllStations);
  };

  const getStationColor = (stationIndex: number) => {
    return STATION_COLORS[stationIndex % STATION_COLORS.length];
  };

  const prepareChartData = () => {
    if (curves.length === 0 || selectedStations.length === 0) {
      return [];
    }

    // Get all unique draft values
    const drafts = new Set<number>();
    curves.forEach((curve) => {
      curve.points.forEach((point) => {
        drafts.add(point.x);
      });
    });

    const sortedDrafts = Array.from(drafts).sort((a, b) => a - b);

    // Create data points with sectional areas for each station
    return sortedDrafts.map((draft) => {
      const dataPoint: Record<string, number> = { draft };

      selectedStations.forEach((stationIndex) => {
        const curve = curves.find((c) => c.stationIndex === stationIndex);
        if (curve) {
          const point = curve.points.find((p) => Math.abs(p.x - draft) < 0.001);
          if (point) {
            dataPoint[`station_${stationIndex}`] = point.y;
          }
        }
      });

      return dataPoint;
    });
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-red-50 border border-red-200 rounded-lg p-6">
        <div className="flex items-start">
          <div className="flex-shrink-0">
            <svg
              className="h-5 w-5 text-red-400"
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 20 20"
              fill="currentColor"
            >
              <path
                fillRule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z"
                clipRule="evenodd"
              />
            </svg>
          </div>
          <div className="ml-3">
            <h3 className="text-sm font-medium text-red-800">Error Loading Curves</h3>
            <div className="mt-2 text-sm text-red-700">{error}</div>
            <button
              onClick={loadBonjeanCurves}
              className="mt-4 bg-red-600 text-white px-4 py-2 rounded hover:bg-red-700"
            >
              Retry
            </button>
          </div>
        </div>
      </div>
    );
  }

  if (curves.length === 0) {
    return (
      <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-6">
        <div className="flex">
          <div className="flex-shrink-0">
            <svg
              className="h-5 w-5 text-yellow-400"
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 20 20"
              fill="currentColor"
            >
              <path
                fillRule="evenodd"
                d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z"
                clipRule="evenodd"
              />
            </svg>
          </div>
          <div className="ml-3">
            <h3 className="text-sm font-medium text-yellow-800">No Geometry Data</h3>
            <div className="mt-2 text-sm text-yellow-700">
              Please import vessel geometry (stations, waterlines, and offsets) before generating
              Bonjean curves.
            </div>
          </div>
        </div>
      </div>
    );
  }

  const chartData = prepareChartData();

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-white shadow rounded-lg p-6">
        <div className="flex items-center justify-between mb-4">
          <div>
            <h3 className="text-lg font-medium text-gray-900">Bonjean Curves</h3>
            <p className="mt-1 text-sm text-gray-500">
              Sectional area vs draft for each station along the vessel length
            </p>
          </div>
          <button
            onClick={loadBonjeanCurves}
            className="inline-flex items-center px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50"
          >
            <svg
              className="h-4 w-4 mr-2"
              xmlns="http://www.w3.org/2000/svg"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"
              />
            </svg>
            Refresh
          </button>
        </div>

        {/* Station selection */}
        <div className="border-t border-gray-200 pt-4">
          <div className="flex items-center justify-between mb-2">
            <label className="text-sm font-medium text-gray-700">Select Stations</label>
            <button
              onClick={handleToggleAll}
              className="text-sm text-blue-600 hover:text-blue-800"
            >
              {showAllStations ? "Deselect All" : "Select All"}
            </button>
          </div>
          <div className="grid grid-cols-5 gap-2">
            {curves.map((curve) => (
              <button
                key={curve.stationIndex}
                onClick={() => handleStationToggle(curve.stationIndex)}
                className={`px-3 py-2 rounded text-sm font-medium transition-colors ${
                  selectedStations.includes(curve.stationIndex)
                    ? "bg-blue-600 text-white"
                    : "bg-gray-100 text-gray-700 hover:bg-gray-200"
                }`}
              >
                Station {curve.stationIndex}
                <div className="text-xs opacity-80">x = {curve.stationX.toFixed(2)}m</div>
              </button>
            ))}
          </div>
        </div>
      </div>

      {/* Chart */}
      {selectedStations.length > 0 && (
        <div className="bg-white shadow rounded-lg p-6">
          <h4 className="text-lg font-medium text-gray-900 mb-4">
            Sectional Area vs Draft
          </h4>
          <ResponsiveContainer width="100%" height={600}>
            <LineChart data={chartData}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis
                dataKey="draft"
                label={{ value: "Draft (m)", position: "insideBottom", offset: -5 }}
              />
              <YAxis
                label={{
                  value: "Sectional Area (m²)",
                  angle: -90,
                  position: "insideLeft",
                }}
              />
              <Tooltip
                formatter={(value: number) => value.toFixed(3)}
                labelFormatter={(label) => `Draft: ${label} m`}
              />
              <Legend />
              {selectedStations.map((stationIndex) => {
                const curve = curves.find((c) => c.stationIndex === stationIndex);
                return (
                  <Line
                    key={stationIndex}
                    type="monotone"
                    dataKey={`station_${stationIndex}`}
                    name={`Station ${stationIndex} (x=${curve?.stationX.toFixed(1)}m)`}
                    stroke={getStationColor(stationIndex)}
                    strokeWidth={2}
                    dot={false}
                  />
                );
              })}
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}

      {/* Information panel */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
        <h4 className="text-sm font-medium text-blue-900 mb-2">About Bonjean Curves</h4>
        <div className="text-sm text-blue-800 space-y-2">
          <p>
            Bonjean curves show the immersed cross-sectional area at each station as a function
            of draft. They are fundamental to naval architecture and are used to:
          </p>
          <ul className="list-disc list-inside ml-2 space-y-1">
            <li>Calculate displacement and buoyancy distribution</li>
            <li>Analyze trim and stability</li>
            <li>Determine loading conditions</li>
            <li>Calculate shear forces and bending moments</li>
          </ul>
          <p className="mt-3">
            <strong>Reading the curves:</strong> Each curve represents a station along the vessel
            length. Higher curves indicate greater sectional area, typically found at the midship
            section of the vessel.
          </p>
        </div>
      </div>
    </div>
  );
}

export default BonjeanCurvesTab;

