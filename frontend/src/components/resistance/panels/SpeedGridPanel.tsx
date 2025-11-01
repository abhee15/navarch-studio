import type { SpeedGrid } from "../../../types/resistance";

interface SpeedGridPanelProps {
  speedGrids: SpeedGrid[];
  selectedSpeedGridId: string;
  onChange: (id: string) => void;
  disabled?: boolean;
}

/**
 * Panel for selecting speed grid
 */
export function SpeedGridPanel({
  speedGrids,
  selectedSpeedGridId,
  onChange,
  disabled = false,
}: SpeedGridPanelProps) {
  const selectedGrid = speedGrids.find((g) => g.id === selectedSpeedGridId);

  return (
    <div className="space-y-2">
      <select
        value={selectedSpeedGridId}
        onChange={(e) => onChange(e.target.value)}
        disabled={disabled}
        className="w-full rounded-md border-border bg-background text-sm px-2 py-1.5 disabled:opacity-50"
      >
        <option value="">Select a speed grid...</option>
        {speedGrids.map((grid) => (
          <option key={grid.id} value={grid.id}>
            {grid.name} ({grid.speedPoints.length} points)
          </option>
        ))}
      </select>
      {selectedGrid && (
        <div className="mt-2 text-xs text-muted-foreground space-y-1">
          <p>
            <span className="font-medium">Points:</span> {selectedGrid.speedPoints.length}
          </p>
          <p>
            <span className="font-medium">Range:</span>{" "}
            {selectedGrid.speedPoints.length > 0
              ? `${Math.min(...selectedGrid.speedPoints.map((p) => p.speed)).toFixed(2)} - ${Math.max(...selectedGrid.speedPoints.map((p) => p.speed)).toFixed(2)} m/s`
              : "N/A"}
          </p>
        </div>
      )}
    </div>
  );
}
