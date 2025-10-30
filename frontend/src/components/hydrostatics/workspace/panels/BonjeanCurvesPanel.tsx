import { useState, useEffect } from "react";
import { observer } from "mobx-react-lite";
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
import { curvesApi } from "../../../../services/hydrostaticsApi";
import type { BonjeanCurve } from "../../../../types/hydrostatics";
import { settingsStore } from "../../../../stores/SettingsStore";
import { getErrorMessage } from "../../../../types/errors";
import { getUnitSymbol } from "../../../../utils/unitSymbols";

interface BonjeanCurvesPanelProps {
  vesselId: string;
  stationsCount?: number;
  waterlinesCount?: number;
  offsetsCount?: number;
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

export const BonjeanCurvesPanel = observer(
  ({ vesselId, stationsCount, waterlinesCount, offsetsCount }: BonjeanCurvesPanelProps) => {
    const [curves, setCurves] = useState<BonjeanCurve[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [selectedStations, setSelectedStations] = useState<number[]>([]);

    const displayUnits = settingsStore.preferredUnits;
    const lengthUnit = getUnitSymbol(displayUnits, "Length");
    const areaUnit = getUnitSymbol(displayUnits, "Area");

    useEffect(() => {
      const hasGeometry =
        (stationsCount ?? 0) > 0 && (waterlinesCount ?? 0) > 0 && (offsetsCount ?? 0) > 0;
      if (hasGeometry) {
        loadBonjeanCurves();
      } else {
        setCurves([]);
        setError(null);
      }
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [vesselId, stationsCount, waterlinesCount, offsetsCount]);

    const loadBonjeanCurves = async () => {
      try {
        setLoading(true);
        setError(null);
        const data = await curvesApi.getBonjean(vesselId);
        setCurves(data.curves);

        // Select first 5 stations by default (or all if less than 5)
        if (data.curves.length > 0) {
          const stationsToSelect = data.curves.slice(0, Math.min(5, data.curves.length));
          setSelectedStations(stationsToSelect.map((c) => c.stationIndex));
        }
      } catch (err) {
        setError(getErrorMessage(err));
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

    const handleSelectAll = () => {
      if (selectedStations.length === curves.length) {
        setSelectedStations([]);
      } else {
        setSelectedStations(curves.map((c) => c.stationIndex));
      }
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
        <div className="flex items-center justify-center h-full">
          <div className="text-center">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-2"></div>
            <p className="text-xs text-muted-foreground">Loading Bonjean curves...</p>
          </div>
        </div>
      );
    }

    if (error) {
      return (
        <div className="flex items-center justify-center h-full">
          <div className="text-center max-w-sm">
            <svg
              className="mx-auto h-8 w-8 text-destructive mb-2"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
            <h4 className="text-xs font-medium text-foreground mb-1">Error Loading Curves</h4>
            <p className="text-[10px] text-muted-foreground mb-2">{error}</p>
            <button
              onClick={loadBonjeanCurves}
              className="text-[10px] px-2 py-1 bg-primary text-primary-foreground rounded hover:bg-primary/90"
            >
              Retry
            </button>
          </div>
        </div>
      );
    }

    if ((stationsCount ?? 0) === 0 || (waterlinesCount ?? 0) === 0 || (offsetsCount ?? 0) === 0) {
      return (
        <div className="flex items-center justify-center h-full">
          <div className="text-center">
            <svg
              className="mx-auto h-8 w-8 text-muted-foreground mb-2"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M8 10h.01M12 10h.01M16 10h.01M9 16h6m2 5H7a2 2 0 01-2-2V7a2 2 0 012-2h3l2-2h2l2 2h3a2 2 0 012 2v12a2 2 0 01-2 2z"
              />
            </svg>
            <h4 className="text-xs font-medium text-foreground mb-1">Geometry Required</h4>
            <p className="text-[10px] text-muted-foreground">
              Import stations, waterlines, and offsets to view Bonjean curves.
            </p>
          </div>
        </div>
      );
    }

    if (curves.length === 0) {
      return (
        <div className="flex items-center justify-center h-full">
          <div className="text-center">
            <svg
              className="mx-auto h-8 w-8 text-muted-foreground mb-2"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
              />
            </svg>
            <h4 className="text-xs font-medium text-foreground mb-1">No Geometry Data</h4>
            <p className="text-[10px] text-muted-foreground">
              Please import vessel geometry (stations, waterlines, and offsets) to view Bonjean
              curves.
            </p>
          </div>
        </div>
      );
    }

    const chartData = prepareChartData();

    return (
      <div className="h-full flex flex-col">
        {/* Station Selection */}
        <div className="flex-shrink-0 mb-2">
          <div className="flex items-center justify-between mb-1">
            <label className="text-[10px] font-medium text-foreground">Select Stations</label>
            <button
              onClick={handleSelectAll}
              className="text-[10px] text-primary hover:text-primary/80"
            >
              {selectedStations.length === curves.length ? "Deselect All" : "Select All"}
            </button>
          </div>
          <div className="flex flex-wrap gap-1">
            {curves.slice(0, 10).map((curve) => (
              <button
                key={curve.stationIndex}
                onClick={() => handleStationToggle(curve.stationIndex)}
                className={`px-2 py-1 rounded text-[10px] font-medium transition-colors ${
                  selectedStations.includes(curve.stationIndex)
                    ? "bg-primary text-primary-foreground"
                    : "bg-muted text-muted-foreground hover:bg-muted/80"
                }`}
                title={`Station ${curve.stationIndex} at x = ${curve.stationX.toFixed(2)}${lengthUnit}`}
              >
                Stn {curve.stationIndex}
              </button>
            ))}
            {curves.length > 10 && (
              <span className="text-[10px] text-muted-foreground self-center">
                +{curves.length - 10} more
              </span>
            )}
          </div>
        </div>

        {/* Chart */}
        <div className="flex-1 min-h-0">
          {selectedStations.length > 0 ? (
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={chartData}>
                <CartesianGrid
                  strokeDasharray="3 3"
                  stroke="#e5e7eb"
                  className="dark:stroke-gray-700"
                />
                <XAxis
                  dataKey="draft"
                  label={{
                    value: `Draft (${lengthUnit})`,
                    position: "insideBottom",
                    offset: -5,
                    style: { fontSize: "10px" },
                  }}
                  tick={{ fontSize: 9 }}
                  stroke="currentColor"
                  className="text-muted-foreground"
                />
                <YAxis
                  label={{
                    value: `Sectional Area (${areaUnit})`,
                    angle: -90,
                    position: "insideLeft",
                    style: { fontSize: "10px" },
                  }}
                  tick={{ fontSize: 9 }}
                  stroke="currentColor"
                  className="text-muted-foreground"
                />
                <Tooltip
                  contentStyle={{ fontSize: "10px", padding: "4px 6px" }}
                  formatter={(value: number) => value.toFixed(3)}
                  labelFormatter={(label) => `Draft: ${label} ${lengthUnit}`}
                />
                <Legend wrapperStyle={{ fontSize: "9px", paddingTop: "4px" }} iconSize={8} />
                {selectedStations.map((stationIndex) => {
                  const curve = curves.find((c) => c.stationIndex === stationIndex);
                  return (
                    <Line
                      key={stationIndex}
                      type="monotone"
                      dataKey={`station_${stationIndex}`}
                      name={`Stn ${stationIndex} (x=${curve?.stationX.toFixed(1)}${lengthUnit})`}
                      stroke={getStationColor(stationIndex)}
                      strokeWidth={1.5}
                      dot={false}
                    />
                  );
                })}
              </LineChart>
            </ResponsiveContainer>
          ) : (
            <div className="flex items-center justify-center h-full">
              <p className="text-xs text-muted-foreground">Select stations to view curves</p>
            </div>
          )}
        </div>

        {/* Info */}
        <div className="flex-shrink-0 mt-2 pt-2 border-t border-border">
          <p className="text-[10px] text-muted-foreground">
            <strong>Bonjean curves</strong> show immersed cross-sectional area at each station vs
            draft. {curves.length} stations available.
          </p>
        </div>
      </div>
    );
  }
);
