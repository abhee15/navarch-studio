import { observer } from "mobx-react-lite";
import { settingsStore } from "../../stores/SettingsStore";
import { getUnitSymbol } from "../../utils/unitSymbols";

interface OffsetsTableViewProps {
  stations: number[];
  waterlines: number[];
  width: number;
  height: number;
}

export const OffsetsTableView = observer(
  ({ stations, waterlines, width, height }: OffsetsTableViewProps) => {
    const displayUnits = settingsStore.preferredUnits;
    const lengthUnit = getUnitSymbol(displayUnits, "Length");

    const maxItemsPerColumn = 25; // Show first 25 stations and waterlines (increased to show all BSRA stations)

    return (
      <g>
        {/* Border */}
        <rect
          x={0}
          y={0}
          width={width}
          height={height}
          fill="none"
          stroke="currentColor"
          strokeWidth="1"
          className="stroke-border"
        />

        {/* Title */}
        <text x={10} y={15} className="text-xs font-medium fill-foreground">
          Reference Data
        </text>

        {/* Two-column layout */}
        <g transform="translate(10, 30)">
          <text className="text-[9px] font-medium fill-foreground">Stations (X)</text>
          {stations.slice(0, maxItemsPerColumn).map((stationX, idx) => (
            <text key={idx} x={0} y={15 + idx * 12} className="text-[8px] fill-muted-foreground">
              {idx}: {stationX.toFixed(2)} {lengthUnit}
            </text>
          ))}
          {stations.length > maxItemsPerColumn && (
            <text
              x={0}
              y={15 + maxItemsPerColumn * 12}
              className="text-[7px] fill-muted-foreground italic"
            >
              ... ({stations.length} total)
            </text>
          )}
        </g>

        <g transform="translate(210, 30)">
          <text className="text-[9px] font-medium fill-foreground">Waterlines (Z)</text>
          {waterlines.slice(0, maxItemsPerColumn).map((wlZ, idx) => (
            <text key={idx} x={0} y={15 + idx * 12} className="text-[8px] fill-muted-foreground">
              {idx}: {wlZ.toFixed(2)} {lengthUnit}
            </text>
          ))}
          {waterlines.length > maxItemsPerColumn && (
            <text
              x={0}
              y={15 + maxItemsPerColumn * 12}
              className="text-[7px] fill-muted-foreground italic"
            >
              ... ({waterlines.length} total)
            </text>
          )}
        </g>

        {/* Footer note */}
        {(stations.length > maxItemsPerColumn || waterlines.length > maxItemsPerColumn) && (
          <text x={10} y={height - 10} className="text-[7px] fill-muted-foreground italic">
            {stations.length} stations × {waterlines.length} waterlines total
          </text>
        )}
      </g>
    );
  }
);

OffsetsTableView.displayName = "OffsetsTableView";
