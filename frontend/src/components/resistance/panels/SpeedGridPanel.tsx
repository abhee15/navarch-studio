import type { SpeedGrid } from "../../../types/resistance";
import { Select } from "../../ui/select";

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

  const options = [
    { value: "", label: "Select a speed grid..." },
    ...speedGrids.map((grid) => ({
      value: grid.id,
      label: `${grid.name} (${grid.speedPoints.length} points)`,
    })),
  ];

  return (
    <div className="space-y-2">
      <Select
        value={selectedSpeedGridId}
        onChange={onChange}
        options={options}
        disabled={disabled}
        className="w-full text-sm"
      />
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
