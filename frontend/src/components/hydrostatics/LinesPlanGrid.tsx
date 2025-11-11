import { useCallback } from "react";
import { observer } from "mobx-react-lite";
import { settingsStore } from "../../stores/SettingsStore";
import { getUnitSymbol } from "../../utils/unitSymbols";
import type { LinesPlanView } from "../../types/linesplan";

interface LinesPlanGridProps {
  type: LinesPlanView;
  stations: number[]; // X positions
  waterlines: number[]; // Z positions
  width: number;
  height: number;
  lpp?: number;
}

export const LinesPlanGrid = observer(
  ({ type, stations, waterlines, width, height, lpp }: LinesPlanGridProps) => {
    const displayUnits = settingsStore.preferredUnits;
    const lengthUnit = getUnitSymbol(displayUnits, "Length");

    // Calculate bounds
    const minX = Math.min(...stations);
    const maxX = Math.max(...stations);
    const minZ = Math.min(...waterlines, 0);
    const maxZ = Math.max(...waterlines);

    // For body plan, we need half-breadth range (Y)
    // For now, use a simple estimate based on typical proportions
    const maxY = type === "body-plan" ? (lpp || maxX) * 0.15 : maxX; // ~15% of length as beam estimate

    // Scale factors
    const scaleX = useCallback(
      (x: number) => {
        if (type === "body-plan") {
          // Center at width/2
          return width / 2;
        } else {
          // Map X range to width
          const rangeX = maxX - minX || 1;
          return ((x - minX) / rangeX) * width;
        }
      },
      [type, minX, maxX, width]
    );

    const scaleY = useCallback(
      (value: number) => {
        if (type === "body-plan") {
          // Body plan: Y-axis is half-breadth, invert for SVG coordinates
          const rangeZ = maxZ - minZ || 1;
          return height - ((value - minZ) / rangeZ) * height;
        } else {
          // Profile/Half-breadth: varies
          const range = type === "profile" ? maxZ - minZ : maxY;
          return height - (value / (range || 1)) * height;
        }
      },
      [type, minZ, maxZ, maxY, height]
    );

    return (
      <g>
        {/* Grid lines for stations */}
        {type !== "body-plan" &&
          stations.map((stationX, idx) => (
            <line
              key={`station-${idx}`}
              x1={scaleX(stationX)}
              y1={0}
              x2={scaleX(stationX)}
              y2={height}
              stroke="currentColor"
              strokeWidth="0.5"
              strokeDasharray="2,2"
              className="stroke-border"
              opacity={0.3}
            />
          ))}

        {/* Grid lines for waterlines */}
        {waterlines.map((wlZ, idx) => (
          <line
            key={`wl-${idx}`}
            x1={0}
            y1={scaleY(wlZ)}
            x2={width}
            y2={scaleY(wlZ)}
            stroke="currentColor"
            strokeWidth="0.5"
            strokeDasharray="2,2"
            className="stroke-border"
            opacity={0.3}
          />
        ))}

        {/* Perpendiculars (bold vertical lines for profile/half-breadth) */}
        {type !== "body-plan" && (
          <>
            <line
              x1={scaleX(minX)}
              y1={0}
              x2={scaleX(minX)}
              y2={height}
              stroke="#EF4444"
              strokeWidth="2"
              opacity={0.8}
            />
            <text x={scaleX(minX)} y={-5} textAnchor="middle" className="text-[8px] fill-red-500">
              AP
            </text>

            <line
              x1={scaleX(maxX)}
              y1={0}
              x2={scaleX(maxX)}
              y2={height}
              stroke="#10B981"
              strokeWidth="2"
              opacity={0.8}
            />
            <text x={scaleX(maxX)} y={-5} textAnchor="middle" className="text-[8px] fill-green-500">
              FP
            </text>
          </>
        )}

        {/* Centerline (for body plan) */}
        {type === "body-plan" && (
          <>
            <line
              x1={width / 2}
              y1={0}
              x2={width / 2}
              y2={height}
              stroke="currentColor"
              strokeWidth="2"
              className="stroke-foreground"
            />
            <text
              x={width / 2}
              y={-5}
              textAnchor="middle"
              className="text-[8px] fill-muted-foreground"
            >
              ⊥ CL
            </text>
          </>
        )}

        {/* Baseline (horizontal) */}
        <line
          x1={0}
          y1={scaleY(0)}
          x2={width}
          y2={scaleY(0)}
          stroke="currentColor"
          strokeWidth="2"
          className="stroke-foreground"
        />

        {/* Station labels (bottom) */}
        {type !== "body-plan" &&
          stations.map((stationX, idx) => {
            // Only label every other station if there are many
            if (stations.length > 15 && idx % 2 !== 0) return null;

            return (
              <text
                key={`label-${idx}`}
                x={scaleX(stationX)}
                y={height + 15}
                textAnchor="middle"
                className="text-[8px] fill-muted-foreground"
              >
                {idx}
              </text>
            );
          })}

        {/* Axis labels */}
        <text
          x={width / 2}
          y={height + 30}
          textAnchor="middle"
          className="text-[10px] font-medium fill-foreground"
        >
          {type === "body-plan"
            ? `Half-Breadth (${lengthUnit})`
            : type === "profile"
              ? `Length (${lengthUnit})`
              : `Length (${lengthUnit})`}
        </text>

        <text
          x={-10}
          y={height / 2}
          textAnchor="middle"
          className="text-[10px] font-medium fill-foreground"
          transform={`rotate(-90, -10, ${height / 2})`}
        >
          {type === "body-plan" || type === "profile"
            ? `Height (${lengthUnit})`
            : `Half-Breadth (${lengthUnit})`}
        </text>
      </g>
    );
  }
);

LinesPlanGrid.displayName = "LinesPlanGrid";
