import { observer } from "mobx-react-lite";
import type { SectionAreaCurve } from "../../types/linesplan";

interface SectionAreaCurveProps {
  data: SectionAreaCurve | null;
  width: number;
  height: number;
  minX: number;
  maxX: number;
}

export const SectionAreaCurveComponent = observer(
  ({ data, width, height, minX, maxX }: SectionAreaCurveProps) => {
    if (!data || data.stationPositions.length === 0) return null;

    // Find min/max sectional area for scaling
    const minArea = 0;
    const maxArea = Math.max(...data.sectionalAreas);

    // Scale functions
    const scaleX = (x: number) => {
      const rangeX = maxX - minX || 1;
      return ((x - minX) / rangeX) * width;
    };

    // Scale area to fit in a portion of the height (e.g., bottom 30%)
    const scaleArea = (area: number) => {
      const areaHeight = height * 0.3; // Use 30% of height for SAC
      const rangeArea = maxArea - minArea || 1;
      return height - ((area - minArea) / rangeArea) * areaHeight;
    };

    // Generate SAC path
    const generateSACPath = () => {
      let path = "";
      data.stationPositions.forEach((x, idx) => {
        const svgX = scaleX(x);
        const svgY = scaleArea(data.sectionalAreas[idx]);

        if (idx === 0) {
          path = `M ${svgX} ${svgY}`;
        } else {
          path += ` L ${svgX} ${svgY}`;
        }
      });

      return path;
    };

    return (
      <g opacity={0.6}>
        {/* SAC curve */}
        <path d={generateSACPath()} stroke="#F59E0B" strokeWidth="2" fill="none" />

        {/* Label */}
        <text
          x={width - 10}
          y={20}
          textAnchor="end"
          className="text-[8px] fill-amber-600 font-medium"
        >
          Section Area Curve
        </text>

        {/* Optional: Show max area value */}
        <text x={width - 10} y={32} textAnchor="end" className="text-[7px] fill-amber-500">
          Max: {maxArea.toFixed(1)} m²
        </text>
      </g>
    );
  }
);

SectionAreaCurveComponent.displayName = "SectionAreaCurve";
