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
import { stabilityApi } from "../../../../services/stabilityApi";
import type { VesselDetails, Loadcase } from "../../../../types/hydrostatics";
import { settingsStore } from "../../../../stores/SettingsStore";
import { getErrorMessage } from "../../../../types/errors";
import { getUnitSymbol } from "../../../../utils/unitSymbols";
import { Select } from "../../../ui/select.tsx";

interface CrossCurvesPanelProps {
  vesselId: string;
  vessel?: VesselDetails | null;
}

export const CrossCurvesPanel = observer(({ vesselId, vessel }: CrossCurvesPanelProps) => {
  const [curves, setCurves] = useState<Array<{ angle: number; kn: number }>>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loadcases, setLoadcases] = useState<Loadcase[]>([]);
  const [selectedLoadcaseId, setSelectedLoadcaseId] = useState<string>("");
  const [draft, setDraft] = useState<number>(vessel?.designDraft || 0);
  const [minAngle, setMinAngle] = useState<number>(0);
  const [maxAngle, setMaxAngle] = useState<number>(90);
  const [angleIncrement, setAngleIncrement] = useState<number>(5);

  const displayUnits = settingsStore.preferredUnits;
  const lengthUnit = getUnitSymbol(displayUnits, "Length");

  // Load loadcases on mount
  useEffect(() => {
    const loadLoadcases = async () => {
      try {
        const { loadcasesApi } = await import("../../../../services/hydrostaticsApi");
        const data = await loadcasesApi.list(vesselId);
        setLoadcases(data.loadcases);
        if (data.loadcases.length > 0 && !selectedLoadcaseId) {
          setSelectedLoadcaseId(data.loadcases[0].id);
        }
      } catch (err) {
        console.error("Error loading loadcases:", err);
      }
    };
    loadLoadcases();
  }, [vesselId, selectedLoadcaseId]);

  // Update draft when vessel changes
  useEffect(() => {
    if (vessel?.designDraft && draft === 0) {
      setDraft(vessel.designDraft);
    }
  }, [vessel, draft]);

  const loadKNCurve = async () => {
    if (!selectedLoadcaseId) {
      setError("Please select a loadcase first");
      return;
    }

    if (draft <= 0) {
      setError("Please enter a valid draft");
      return;
    }

    try {
      setLoading(true);
      setError(null);

      const response = await stabilityApi.generateKNCurve(vesselId, {
        loadcaseId: selectedLoadcaseId,
        minAngle,
        maxAngle,
        angleIncrement,
        method: "WallSided",
        draft,
      });

      // Extract KN values from stability curve
      const knData = response.points.map((point) => ({
        angle: point.heelAngle,
        kn: point.kn,
      }));

      setCurves(knData);
    } catch (err) {
      setError(getErrorMessage(err));
      setCurves([]);
    } finally {
      setLoading(false);
    }
  };

  // Auto-load when parameters change (debounced)
  useEffect(() => {
    if (!selectedLoadcaseId || draft <= 0) {
      return;
    }

    const timer = setTimeout(() => {
      loadKNCurve();
    }, 500);

    return () => clearTimeout(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedLoadcaseId, draft, minAngle, maxAngle, angleIncrement]);

  if (loadcases.length === 0) {
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
              d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"
            />
          </svg>
          <h4 className="text-xs font-medium text-foreground mb-1">No Loadcases</h4>
          <p className="text-[10px] text-muted-foreground">
            Create a loadcase to compute cross-curves (KN).
          </p>
        </div>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center h-full">
        <div className="text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary mx-auto mb-2"></div>
          <p className="text-xs text-muted-foreground">Computing KN curve...</p>
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
          <h4 className="text-xs font-medium text-foreground mb-1">Error</h4>
          <p className="text-[10px] text-muted-foreground mb-2">{error}</p>
          <button
            onClick={loadKNCurve}
            className="text-[10px] px-2 py-1 bg-primary text-primary-foreground rounded hover:bg-primary/90"
          >
            Retry
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full flex flex-col">
      {/* Controls */}
      <div className="flex-shrink-0 mb-3 space-y-2">
        <div className="grid grid-cols-2 gap-2">
          <div>
            <label className="text-[10px] font-medium text-foreground block mb-1">Loadcase</label>
            <Select
              value={selectedLoadcaseId}
              onChange={setSelectedLoadcaseId}
              options={loadcases.map((lc) => ({ value: lc.id, label: lc.name }))}
              className="w-full text-xs"
            />
          </div>
          <div>
            <label className="text-[10px] font-medium text-foreground block mb-1">
              Draft ({lengthUnit})
            </label>
            <input
              type="number"
              value={draft}
              onChange={(e) => setDraft(Number(e.target.value))}
              min="0"
              step="0.1"
              className="w-full border border-border bg-background text-foreground rounded text-xs py-1 px-2 focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>
        </div>
        <div className="grid grid-cols-3 gap-2">
          <div>
            <label className="text-[10px] font-medium text-foreground block mb-1">
              Min Angle (°)
            </label>
            <input
              type="number"
              value={minAngle}
              onChange={(e) => setMinAngle(Number(e.target.value))}
              min="0"
              max="90"
              step="1"
              className="w-full border border-border bg-background text-foreground rounded text-xs py-1 px-2 focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>
          <div>
            <label className="text-[10px] font-medium text-foreground block mb-1">
              Max Angle (°)
            </label>
            <input
              type="number"
              value={maxAngle}
              onChange={(e) => setMaxAngle(Number(e.target.value))}
              min="0"
              max="90"
              step="1"
              className="w-full border border-border bg-background text-foreground rounded text-xs py-1 px-2 focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>
          <div>
            <label className="text-[10px] font-medium text-foreground block mb-1">
              Increment (°)
            </label>
            <input
              type="number"
              value={angleIncrement}
              onChange={(e) => setAngleIncrement(Number(e.target.value))}
              min="0.5"
              max="10"
              step="0.5"
              className="w-full border border-border bg-background text-foreground rounded text-xs py-1 px-2 focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>
        </div>
      </div>

      {/* Chart */}
      <div className="flex-1 min-h-0 relative">
        {curves.length > 0 ? (
          <div className="absolute inset-0">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={curves}>
                <CartesianGrid
                  strokeDasharray="3 3"
                  stroke="#e5e7eb"
                  className="dark:stroke-gray-700"
                />
                <XAxis
                  dataKey="angle"
                  label={{
                    value: `Heel Angle (°)`,
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
                    value: `KN (${lengthUnit})`,
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
                  labelFormatter={(label) => `Angle: ${label}°`}
                />
                <Legend wrapperStyle={{ fontSize: "9px", paddingTop: "4px" }} iconSize={8} />
                <Line
                  type="monotone"
                  dataKey="kn"
                  stroke="#3B82F6"
                  strokeWidth={1.5}
                  dot={false}
                  name="KN"
                />
              </LineChart>
            </ResponsiveContainer>
          </div>
        ) : (
          <div className="flex items-center justify-center h-full">
            <div className="text-center text-muted-foreground">
              <p className="text-xs">No curve data</p>
              <p className="text-[10px] mt-1">
                Select a loadcase and draft, then the curve will be computed automatically.
              </p>
            </div>
          </div>
        )}
      </div>

      {/* Info */}
      <div className="flex-shrink-0 mt-2 pt-2 border-t border-border">
        <p className="text-[10px] text-muted-foreground">
          <strong>Cross-curves (KN)</strong> show the righting arm kernel vs. heel angle. KN is the
          distance from keel to the point where the vertical line through the center of buoyancy
          intersects the ship's centerline.
        </p>
      </div>
    </div>
  );
});
