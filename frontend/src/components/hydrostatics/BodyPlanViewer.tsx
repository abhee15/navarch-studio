import { useState, useMemo, useCallback, useRef } from "react";
import { observer } from "mobx-react-lite";
import type { BodyPlanData, StationCurve, BodyPlanViewOptions } from "../../types/bodyplan";
import { generateSmoothCurve, type Point2D } from "../../utils/splineInterpolation";
import { settingsStore } from "../../stores/SettingsStore";
import { getUnitSymbol } from "../../utils/unitSymbols";

interface BodyPlanViewerProps {
  data: BodyPlanData;
  lpp?: number; // Length between perpendiculars for region calculation
  onStationHover?: (stationIndex: number | null) => void;
  onStationSelect?: (stationIndex: number | null) => void;
  onSync3DView?: (stationIndex: number) => void;
  height?: number;
}

// Color schemes for different regions
const REGION_COLORS = {
  forward: "#3B82F6", // Blue
  midship: "#10B981", // Green
  aft: "#EF4444", // Red
};

export const BodyPlanViewer = observer(
  ({
    data,
    lpp,
    onStationHover,
    onStationSelect,
    onSync3DView,
    height = 600,
  }: BodyPlanViewerProps) => {
    const svgRef = useRef<SVGSVGElement>(null);
    const [hoveredStation, setHoveredStation] = useState<number | null>(null);
    const [selectedStation, setSelectedStation] = useState<number | null>(null);
    const [viewOptions, setViewOptions] = useState<BodyPlanViewOptions>({
      showMirrored: true,
      showGrid: true,
      showStationLabels: true,
      enableFairing: true,
      fairingResolution: 50,
      colorScheme: "region",
    });
    const [tooltip, setTooltip] = useState<{
      x: number;
      y: number;
      content: { title: string; items: { label: string; value: string }[] };
    } | null>(null);

    // Units
    const displayUnits = settingsStore.preferredUnits;
    const lengthUnit = getUnitSymbol(displayUnits, "Length");

    // SVG dimensions and scaling
    const viewBoxWidth = 800;
    const viewBoxHeight = height;
    const margin = { top: 60, right: 120, bottom: 60, left: 60 };
    const drawWidth = viewBoxWidth - margin.left - margin.right;
    const drawHeight = viewBoxHeight - margin.top - margin.bottom;

    // Calculate bounds
    const maxHalfBreadth = useMemo(() => {
      let max = 0;
      for (const stationOffsets of data.offsets) {
        for (const offset of stationOffsets) {
          max = Math.max(max, Math.abs(offset));
        }
      }
      return max * 1.1; // Add 10% padding
    }, [data.offsets]);

    const maxWaterline = useMemo(() => {
      return Math.max(...data.waterlines) * 1.05; // Add 5% padding
    }, [data.waterlines]);

    const minWaterline = useMemo(() => {
      return Math.min(...data.waterlines, 0);
    }, [data.waterlines]);

    // Scale factors
    const scaleY = drawHeight / (maxWaterline - minWaterline);
    const scaleX = drawWidth / (2 * maxHalfBreadth); // Divided by 2 for mirroring

    // Coordinate transformations
    const toSvgX = useCallback(
      (halfBreadth: number, side: "port" | "starboard" = "starboard") => {
        const centerX = margin.left + drawWidth / 2;
        return side === "starboard"
          ? centerX + halfBreadth * scaleX
          : centerX - halfBreadth * scaleX;
      },
      [scaleX, margin.left, drawWidth]
    );

    const toSvgY = useCallback(
      (z: number) => {
        return margin.top + drawHeight - (z - minWaterline) * scaleY;
      },
      [scaleY, margin.top, drawHeight, minWaterline]
    );

    // Determine station region (forward/midship/aft)
    const getStationRegion = useCallback(
      (stationIndex: number): "forward" | "midship" | "aft" => {
        if (!lpp || data.stations.length === 0) return "midship";

        const stationX = data.stations[stationIndex];
        const forwardThird = lpp * 0.67;
        const aftThird = lpp * 0.33;

        if (stationX >= forwardThird) return "forward";
        if (stationX <= aftThird) return "aft";
        return "midship";
      },
      [lpp, data.stations]
    );

    // Generate station curves with optional fairing
    const stationCurves = useMemo<StationCurve[]>(() => {
      const curves: StationCurve[] = [];

      for (let stationIdx = 0; stationIdx < data.stations.length; stationIdx++) {
        const stationOffsets = data.offsets[stationIdx];
        if (!stationOffsets) continue;

        const region = getStationRegion(stationIdx);
        const color = REGION_COLORS[region];

        // Create points for this station
        const points: Point2D[] = data.waterlines.map((z, wlIdx) => ({
          x: z, // Use z as x for spline calculation
          y: stationOffsets[wlIdx] || 0,
        }));

        // Apply fairing if enabled
        let finalPoints: { z: number; y: number }[];
        if (viewOptions.enableFairing && points.length > 2) {
          const smoothed = generateSmoothCurve(points, viewOptions.fairingResolution);
          finalPoints = smoothed.map((p) => ({ z: p.x, y: p.y }));
        } else {
          finalPoints = points.map((p) => ({ z: p.x, y: p.y }));
        }

        curves.push({
          stationIndex: stationIdx,
          stationX: data.stations[stationIdx],
          points: finalPoints,
          color,
          region,
        });
      }

      return curves;
    }, [
      data.stations,
      data.waterlines,
      data.offsets,
      getStationRegion,
      viewOptions.enableFairing,
      viewOptions.fairingResolution,
    ]);

    // Handle station interactions
    const handleStationHover = useCallback(
      (stationIndex: number | null, event?: React.MouseEvent) => {
        setHoveredStation(stationIndex);
        if (onStationHover) {
          onStationHover(stationIndex);
        }

        if (stationIndex !== null && event) {
          const curve = stationCurves[stationIndex];
          if (curve) {
            setTooltip({
              x: event.clientX,
              y: event.clientY,
              content: {
                title: `Station ${stationIndex}`,
                items: [
                  { label: "Position", value: `${curve.stationX.toFixed(2)} ${lengthUnit}` },
                  {
                    label: "Region",
                    value: curve.region.charAt(0).toUpperCase() + curve.region.slice(1),
                  },
                  { label: "Points", value: `${curve.points.length}` },
                ],
              },
            });
          }
        } else {
          setTooltip(null);
        }
      },
      [onStationHover, stationCurves, lengthUnit]
    );

    const handleStationClick = useCallback(
      (stationIndex: number) => {
        setSelectedStation(stationIndex);
        if (onStationSelect) {
          onStationSelect(stationIndex);
        }
        if (onSync3DView) {
          onSync3DView(stationIndex);
        }
      },
      [onStationSelect, onSync3DView]
    );

    // Generate SVG path for a station curve
    const generateCurvePath = useCallback(
      (curve: StationCurve, side: "port" | "starboard") => {
        if (curve.points.length === 0) return "";

        const firstPoint = curve.points[0];
        let path = `M ${toSvgX(firstPoint.y, side)} ${toSvgY(firstPoint.z)}`;

        for (let i = 1; i < curve.points.length; i++) {
          const point = curve.points[i];
          path += ` L ${toSvgX(point.y, side)} ${toSvgY(point.z)}`;
        }

        return path;
      },
      [toSvgX, toSvgY]
    );

    // Export as SVG
    const exportAsSVG = useCallback(() => {
      if (!svgRef.current) return;

      const svgElement = svgRef.current.cloneNode(true) as SVGElement;
      const serializer = new XMLSerializer();
      const svgString = serializer.serializeToString(svgElement);

      const blob = new Blob([svgString], { type: "image/svg+xml" });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `body_plan_${Date.now()}.svg`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    }, []);

    // Export as DXF
    const exportAsDXF = useCallback(() => {
      // Generate DXF file content
      let dxfContent = "0\nSECTION\n2\nHEADER\n";
      dxfContent += "9\n$ACADVER\n1\nAC1015\n"; // AutoCAD 2000 format
      dxfContent += "0\nENDSEC\n";

      // TABLES section
      dxfContent += "0\nSECTION\n2\nTABLES\n";

      // Layer table
      dxfContent += "0\nTABLE\n2\nLAYER\n70\n" + stationCurves.length + "\n";

      stationCurves.forEach((curve) => {
        dxfContent += "0\nLAYER\n2\nSTATION_" + curve.stationIndex + "\n";
        dxfContent += "70\n0\n62\n";
        // Color codes: 5=blue (forward), 3=green (midship), 1=red (aft)
        const colorCode = curve.region === "forward" ? "5" : curve.region === "midship" ? "3" : "1";
        dxfContent += colorCode + "\n";
        dxfContent += "6\nCONTINUOUS\n";
      });

      dxfContent += "0\nENDTAB\n0\nENDSEC\n";

      // ENTITIES section
      dxfContent += "0\nSECTION\n2\nENTITIES\n";

      stationCurves.forEach((curve) => {
        // Starboard side
        if (curve.points.length > 1) {
          dxfContent += "0\nLWPOLYLINE\n8\nSTATION_" + curve.stationIndex + "\n";
          dxfContent += "90\n" + curve.points.length + "\n";
          dxfContent += "70\n0\n"; // Not closed

          curve.points.forEach((point) => {
            // In DXF: X = half-breadth, Y = waterline height
            dxfContent += "10\n" + point.y.toFixed(4) + "\n";
            dxfContent += "20\n" + point.z.toFixed(4) + "\n";
          });
        }

        // Port side (mirrored)
        if (viewOptions.showMirrored && curve.points.length > 1) {
          dxfContent += "0\nLWPOLYLINE\n8\nSTATION_" + curve.stationIndex + "_PORT\n";
          dxfContent += "90\n" + curve.points.length + "\n";
          dxfContent += "70\n0\n";

          curve.points.forEach((point) => {
            dxfContent += "10\n" + (-point.y).toFixed(4) + "\n";
            dxfContent += "20\n" + point.z.toFixed(4) + "\n";
          });
        }
      });

      dxfContent += "0\nENDSEC\n0\nEOF\n";

      const blob = new Blob([dxfContent], { type: "application/dxf" });
      const url = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = `body_plan_${Date.now()}.dxf`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    }, [stationCurves, viewOptions.showMirrored]);

    return (
      <div className="bg-white dark:bg-gray-800 shadow rounded-lg p-6">
        {/* Header with controls */}
        <div className="flex justify-between items-start mb-4">
          <h4 className="text-lg font-medium text-gray-900 dark:text-gray-100">Body Plan</h4>

          <div className="flex items-center space-x-2">
            {/* View options */}
            <div className="flex items-center space-x-3 text-sm mr-4">
              <label className="flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  checked={viewOptions.showMirrored}
                  onChange={(e) =>
                    setViewOptions({ ...viewOptions, showMirrored: e.target.checked })
                  }
                  className="w-4 h-4 text-blue-600 rounded focus:ring-blue-500"
                />
                <span className="ml-2 text-gray-700 dark:text-gray-300">Mirror</span>
              </label>

              <label className="flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  checked={viewOptions.showGrid}
                  onChange={(e) => setViewOptions({ ...viewOptions, showGrid: e.target.checked })}
                  className="w-4 h-4 text-blue-600 rounded focus:ring-blue-500"
                />
                <span className="ml-2 text-gray-700 dark:text-gray-300">Grid</span>
              </label>

              <label className="flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  checked={viewOptions.enableFairing}
                  onChange={(e) =>
                    setViewOptions({ ...viewOptions, enableFairing: e.target.checked })
                  }
                  className="w-4 h-4 text-blue-600 rounded focus:ring-blue-500"
                />
                <span className="ml-2 text-gray-700 dark:text-gray-300">Smooth</span>
              </label>
            </div>

            {/* Export buttons */}
            <button
              onClick={exportAsSVG}
              className="inline-flex items-center px-2 py-1 text-xs font-medium rounded border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 hover:bg-gray-50 dark:hover:bg-gray-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
              title="Export as SVG"
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
              onClick={exportAsDXF}
              className="inline-flex items-center px-2 py-1 text-xs font-medium rounded border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 hover:bg-gray-50 dark:hover:bg-gray-600 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
              title="Export as DXF"
            >
              <svg className="w-4 h-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z"
                />
              </svg>
              DXF
            </button>
          </div>
        </div>

        {/* SVG Canvas */}
        <div className="relative">
          <svg
            ref={svgRef}
            viewBox={`0 0 ${viewBoxWidth} ${viewBoxHeight}`}
            className="w-full h-full"
            style={{ minHeight: `${height}px` }}
          >
            {/* Title */}
            <text
              x={viewBoxWidth / 2}
              y={30}
              textAnchor="middle"
              className="text-[12px] font-semibold fill-gray-700 dark:fill-gray-300"
            >
              Body Plan - Sectional View
            </text>

            {/* Centerline */}
            <line
              x1={margin.left + drawWidth / 2}
              y1={margin.top}
              x2={margin.left + drawWidth / 2}
              y2={margin.top + drawHeight}
              className="stroke-gray-800 dark:stroke-gray-200"
              strokeWidth="2"
            />
            <text
              x={margin.left + drawWidth / 2}
              y={margin.top - 10}
              textAnchor="middle"
              className="text-[8px] fill-gray-600 dark:fill-gray-400"
            >
              ⊥ Centerline
            </text>

            {/* Baseline (waterline at z=0 or minimum) */}
            <line
              x1={margin.left}
              y1={toSvgY(0)}
              x2={margin.left + drawWidth}
              y2={toSvgY(0)}
              className="stroke-gray-400 dark:stroke-gray-500"
              strokeWidth="1.5"
              strokeDasharray="4,4"
            />

            {/* Grid lines (waterlines) */}
            {viewOptions.showGrid &&
              data.waterlines.map((z, idx) => (
                <line
                  key={`wl-${idx}`}
                  x1={margin.left}
                  y1={toSvgY(z)}
                  x2={margin.left + drawWidth}
                  y2={toSvgY(z)}
                  className="stroke-gray-200 dark:stroke-gray-700"
                  strokeWidth="0.5"
                  strokeDasharray="2,2"
                />
              ))}

            {/* Station curves - Starboard side */}
            {stationCurves.map((curve) => {
              const isHovered = hoveredStation === curve.stationIndex;
              const isSelected = selectedStation === curve.stationIndex;
              const opacity = isHovered || isSelected ? 1 : 0.6;
              const strokeWidth = isHovered || isSelected ? 2.5 : 1.5;

              return (
                <g key={`station-${curve.stationIndex}`}>
                  {/* Starboard curve */}
                  <path
                    d={generateCurvePath(curve, "starboard")}
                    stroke={curve.color}
                    strokeWidth={strokeWidth}
                    fill="none"
                    opacity={opacity}
                    className="transition-all cursor-pointer"
                    onMouseEnter={(e) => handleStationHover(curve.stationIndex, e)}
                    onMouseLeave={() => handleStationHover(null)}
                    onClick={() => handleStationClick(curve.stationIndex)}
                  />

                  {/* Port curve (mirrored) */}
                  {viewOptions.showMirrored && (
                    <path
                      d={generateCurvePath(curve, "port")}
                      stroke={curve.color}
                      strokeWidth={strokeWidth}
                      fill="none"
                      opacity={opacity}
                      className="transition-all cursor-pointer"
                      onMouseEnter={(e) => handleStationHover(curve.stationIndex, e)}
                      onMouseLeave={() => handleStationHover(null)}
                      onClick={() => handleStationClick(curve.stationIndex)}
                    />
                  )}
                </g>
              );
            })}

            {/* Axes labels */}
            <text
              x={viewBoxWidth - margin.right + 40}
              y={margin.top + drawHeight / 2}
              textAnchor="middle"
              className="text-[10px] fill-gray-600 dark:fill-gray-400"
              transform={`rotate(90 ${viewBoxWidth - margin.right + 40} ${margin.top + drawHeight / 2})`}
            >
              Height (Z) [{lengthUnit}]
            </text>

            <text
              x={margin.left + drawWidth / 2}
              y={viewBoxHeight - 20}
              textAnchor="middle"
              className="text-[10px] fill-gray-600 dark:fill-gray-400"
            >
              Half-Breadth (Y) [{lengthUnit}]
            </text>

            {/* Side labels */}
            <text
              x={margin.left + 10}
              y={margin.top - 10}
              className="text-[9px] font-medium fill-gray-700 dark:fill-gray-300"
            >
              PORT
            </text>
            <text
              x={margin.left + drawWidth - 10}
              y={margin.top - 10}
              textAnchor="end"
              className="text-[9px] font-medium fill-gray-700 dark:fill-gray-300"
            >
              STARBOARD
            </text>
          </svg>

          {/* Tooltip */}
          {tooltip && (
            <div
              className="fixed z-50 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg p-3 pointer-events-none"
              style={{
                left: tooltip.x + 15,
                top: tooltip.y + 15,
              }}
            >
              <div className="text-xs font-semibold text-gray-900 dark:text-gray-100 mb-1">
                {tooltip.content.title}
              </div>
              {tooltip.content.items.map((item, idx) => (
                <div key={idx} className="text-[11px] text-gray-600 dark:text-gray-400">
                  <span className="font-medium">{item.label}:</span> {item.value}
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Legend */}
        <div className="mt-4 pt-4 border-t border-gray-200 dark:border-gray-700">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-4 text-sm">
              <div className="flex items-center">
                <div className="w-4 h-1 mr-2" style={{ backgroundColor: REGION_COLORS.forward }} />
                <span className="text-gray-700 dark:text-gray-300">Forward</span>
              </div>
              <div className="flex items-center">
                <div className="w-4 h-1 mr-2" style={{ backgroundColor: REGION_COLORS.midship }} />
                <span className="text-gray-700 dark:text-gray-300">Midship</span>
              </div>
              <div className="flex items-center">
                <div className="w-4 h-1 mr-2" style={{ backgroundColor: REGION_COLORS.aft }} />
                <span className="text-gray-700 dark:text-gray-300">Aft</span>
              </div>
            </div>

            <div className="text-sm text-gray-600 dark:text-gray-400">
              <span className="font-medium">{stationCurves.length}</span> stations •{" "}
              <span className="font-medium">{data.waterlines.length}</span> waterlines
            </div>
          </div>
        </div>
      </div>
    );
  }
);

export default BodyPlanViewer;
