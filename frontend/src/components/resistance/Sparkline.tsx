import { useMemo } from "react";

interface SparklineProps {
  data: number[];
  width?: number;
  height?: number;
  color?: string;
  highlightMin?: boolean;
  highlightMax?: boolean;
  className?: string;
}

/**
 * Sparkline component for inline data visualization
 * Shows a small line chart with optional min/max highlighting
 */
export function Sparkline({
  data,
  width = 60,
  height = 20,
  color = "#3B82F6",
  highlightMin = false,
  highlightMax = false,
  className = "",
}: SparklineProps) {
  const { points, minIndex, maxIndex, minValue, maxValue } = useMemo(() => {
    if (!data || data.length === 0) {
      return { points: "", minIndex: -1, maxIndex: -1, minValue: 0, maxValue: 0 };
    }

    const min = Math.min(...data);
    const max = Math.max(...data);
    const range = max - min || 1; // Avoid division by zero

    const padding = 2;
    const usableHeight = height - padding * 2;
    const usableWidth = width - padding * 2;
    const stepX = data.length > 1 ? usableWidth / (data.length - 1) : 0;

    const normalizedPoints = data.map((value, index) => {
      const x = padding + index * stepX;
      const y = padding + usableHeight - ((value - min) / range) * usableHeight;
      return { x, y, value };
    });

    const pathData = normalizedPoints
      .map((p, i) => (i === 0 ? `M ${p.x} ${p.y}` : `L ${p.x} ${p.y}`))
      .join(" ");

    const minIdx = data.indexOf(min);
    const maxIdx = data.indexOf(max);

    return {
      points: pathData,
      minIndex: minIdx,
      maxIndex: maxIdx,
      minValue: min,
      maxValue: max,
    };
  }, [data, width, height]);

  if (!data || data.length === 0) {
    return (
      <svg width={width} height={height} className={className}>
        <text x={width / 2} y={height / 2} textAnchor="middle" fontSize="8" fill="#999">
          N/A
        </text>
      </svg>
    );
  }

  const stepX = data.length > 1 ? (width - 4) / (data.length - 1) : 0;
  const padding = 2;
  const usableHeight = height - padding * 2;

  return (
    <svg width={width} height={height} className={className}>
      {/* Main line */}
      <path d={points} fill="none" stroke={color} strokeWidth="1.5" />

      {/* Highlight min point */}
      {highlightMin && minIndex >= 0 && (
        <circle
          cx={padding + minIndex * stepX}
          cy={
            padding +
            usableHeight -
            ((data[minIndex] - minValue) / (maxValue - minValue || 1)) * usableHeight
          }
          r="2"
          fill="#EF4444"
          stroke="#FFF"
          strokeWidth="0.5"
        />
      )}

      {/* Highlight max point */}
      {highlightMax && maxIndex >= 0 && (
        <circle
          cx={padding + maxIndex * stepX}
          cy={
            padding +
            usableHeight -
            ((data[maxIndex] - minValue) / (maxValue - minValue || 1)) * usableHeight
          }
          r="2"
          fill="#10B981"
          stroke="#FFF"
          strokeWidth="0.5"
        />
      )}
    </svg>
  );
}
