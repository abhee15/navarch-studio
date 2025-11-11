import { observer } from "mobx-react-lite";
import type { DiagonalCurve } from "../../types/linesplan";

interface DiagonalsViewProps {
  diagonals: DiagonalCurve[];
  view: "profile" | "plan";
  width: number;
  height: number;
  minX: number;
  maxX: number;
  minZ: number;
  maxZ: number;
  maxY: number;
}

const DIAGONAL_COLORS = [
  "#F59E0B", // Amber
  "#8B5CF6", // Purple
  "#EC4899", // Pink
  "#14B8A6", // Teal
  "#F97316", // Orange
];

export const DiagonalsView = observer(
  ({ diagonals, view, width, height, minX, maxX, minZ, maxZ, maxY }: DiagonalsViewProps) => {
    // Scale functions
    const scaleX = (x: number) => {
      const rangeX = maxX - minX || 1;
      return ((x - minX) / rangeX) * width;
    };

    const scaleY = (value: number) => {
      if (view === "profile") {
        // Profile: Y-axis is Z (height)
        const rangeZ = maxZ - minZ || 1;
        return height - ((value - minZ) / rangeZ) * height;
      } else {
        // Plan: Y-axis is Y (half-breadth)
        return height - (value / (maxY || 1)) * height;
      }
    };

    // Generate SVG path for a diagonal curve
    const generateDiagonalPath = (diagonal: DiagonalCurve) => {
      if (diagonal.points.length === 0) return "";

      let path = "";
      diagonal.points.forEach((point, idx) => {
        const x = scaleX(point.x);
        const y = view === "profile" ? scaleY(point.z) : scaleY(point.y);

        if (idx === 0) {
          path = `M ${x} ${y}`;
        } else {
          path += ` L ${x} ${y}`;
        }
      });

      return path;
    };

    return (
      <g>
        {diagonals.map((diag, idx) => (
          <g key={`diag-${idx}`}>
            <path
              d={generateDiagonalPath(diag)}
              stroke={DIAGONAL_COLORS[idx % DIAGONAL_COLORS.length]}
              strokeWidth="1.5"
              fill="none"
              opacity={0.7}
              strokeDasharray="5,5"
            />
            {/* Label at end of diagonal */}
            {diag.points.length > 0 && (
              <text
                x={scaleX(diag.points[diag.points.length - 1].x) + 5}
                y={
                  view === "profile"
                    ? scaleY(diag.points[diag.points.length - 1].z)
                    : scaleY(diag.points[diag.points.length - 1].y)
                }
                className="text-[7px] fill-amber-600"
              >
                D{idx}
              </text>
            )}
          </g>
        ))}
      </g>
    );
  }
);

DiagonalsView.displayName = "DiagonalsView";
