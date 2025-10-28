import { useState } from "react";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer,
  Brush,
} from "recharts";

interface ChartData {
  x: number;
  y: number;
}

interface InteractiveChartProps {
  data: ChartData[];
  title: string;
  xLabel: string;
  yLabel: string;
  color?: string;
  height?: number;
}

export function InteractiveChart({
  data,
  title,
  xLabel,
  yLabel,
  color = "#3B82F6",
  height = 400,
}: InteractiveChartProps) {
  const [showGrid, setShowGrid] = useState(true);
  const [showDots, setShowDots] = useState(false);
  const [showBrush, setShowBrush] = useState(false);

  const downloadChartAsSVG = () => {
    const svgElement = document.querySelector(`#chart-${title.replace(/\s+/g, "-")} svg`);
    if (svgElement) {
      const serializer = new XMLSerializer();
      const svgString = serializer.serializeToString(svgElement);
      const blob = new Blob([svgString], { type: "image/svg+xml" });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `${title.replace(/\s+/g, "_")}.svg`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    }
  };

  const downloadChartData = () => {
    const csvContent = [
      `${xLabel},${yLabel}`,
      ...data.map((point) => `${point.x},${point.y}`),
    ].join("\n");
    const blob = new Blob([csvContent], { type: "text/csv" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `${title.replace(/\s+/g, "_")}_data.csv`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  return (
    <div className="bg-white dark:bg-gray-800 shadow rounded-lg p-6">
      {/* Header with Title and Controls */}
      <div className="flex justify-between items-start mb-4">
        <h4 className="text-lg font-medium text-gray-900 dark:text-gray-100">{title}</h4>
        
        <div className="flex items-center space-x-2">
          {/* Chart Controls */}
          <div className="flex items-center space-x-3 text-sm mr-4">
            <label className="flex items-center cursor-pointer">
              <input
                type="checkbox"
                checked={showGrid}
                onChange={(e) => setShowGrid(e.target.checked)}
                className="w-4 h-4 text-blue-600 rounded focus:ring-blue-500"
              />
              <span className="ml-2 text-gray-700 dark:text-gray-300">Grid</span>
            </label>
            
            <label className="flex items-center cursor-pointer">
              <input
                type="checkbox"
                checked={showDots}
                onChange={(e) => setShowDots(e.target.checked)}
                className="w-4 h-4 text-blue-600 rounded focus:ring-blue-500"
              />
              <span className="ml-2 text-gray-700 dark:text-gray-300">Points</span>
            </label>
            
            <label className="flex items-center cursor-pointer">
              <input
                type="checkbox"
                checked={showBrush}
                onChange={(e) => setShowBrush(e.target.checked)}
                className="w-4 h-4 text-blue-600 rounded focus:ring-blue-500"
              />
              <span className="ml-2 text-gray-700 dark:text-gray-300">Zoom</span>
            </label>
          </div>

          {/* Action Buttons */}
          <button
            onClick={downloadChartAsSVG}
            className="inline-flex items-center px-2 py-1 text-xs font-medium rounded border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 hover:bg-gray-50 dark:hover:bg-gray-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
            title="Download chart as SVG"
          >
            <svg className="w-4 h-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"
              />
            </svg>
            SVG
          </button>
          
          <button
            onClick={downloadChartData}
            className="inline-flex items-center px-2 py-1 text-xs font-medium rounded border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 hover:bg-gray-50 dark:hover:bg-gray-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
            title="Download data as CSV"
          >
            <svg className="w-4 h-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
              />
            </svg>
            CSV
          </button>
        </div>
      </div>

      {/* Chart */}
      <div id={`chart-${title.replace(/\s+/g, "-")}`}>
        <ResponsiveContainer width="100%" height={height}>
          <LineChart data={data} margin={{ top: 5, right: 30, left: 20, bottom: 5 }}>
            {showGrid && <CartesianGrid strokeDasharray="3 3" stroke="#E5E7EB" />}
            <XAxis
              dataKey="x"
              label={{ value: xLabel, position: "insideBottom", offset: -5 }}
              stroke="#6B7280"
            />
            <YAxis
              label={{ value: yLabel, angle: -90, position: "insideLeft" }}
              stroke="#6B7280"
            />
            <Tooltip
              contentStyle={{
                backgroundColor: "#FFF",
                border: "1px solid #E5E7EB",
                borderRadius: "0.375rem",
                padding: "8px",
              }}
              formatter={(value: number) => value.toFixed(2)}
            />
            <Legend />
            <Line
              type="monotone"
              dataKey="y"
              stroke={color}
              strokeWidth={2}
              dot={showDots}
              name={yLabel}
              activeDot={{ r: 6 }}
            />
            {showBrush && (
              <Brush
                dataKey="x"
                height={30}
                stroke={color}
                fill="#F3F4F6"
                travellerWidth={10}
              />
            )}
          </LineChart>
        </ResponsiveContainer>
      </div>

      {/* Stats */}
      <div className="mt-4 pt-4 border-t border-gray-200 dark:border-gray-700">
        <div className="grid grid-cols-4 gap-4 text-sm">
          <div>
            <span className="text-gray-600 dark:text-gray-400">Points:</span>
            <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">
              {data.length}
            </span>
          </div>
          <div>
            <span className="text-gray-600 dark:text-gray-400">Min Y:</span>
            <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">
              {Math.min(...data.map((d) => d.y)).toFixed(2)}
            </span>
          </div>
          <div>
            <span className="text-gray-600 dark:text-gray-400">Max Y:</span>
            <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">
              {Math.max(...data.map((d) => d.y)).toFixed(2)}
            </span>
          </div>
          <div>
            <span className="text-gray-600 dark:text-gray-400">Range:</span>
            <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">
              {(Math.max(...data.map((d) => d.y)) - Math.min(...data.map((d) => d.y))).toFixed(2)}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}

