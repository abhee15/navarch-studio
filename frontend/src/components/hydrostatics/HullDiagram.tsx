import { useState, useMemo, useCallback } from "react";
import { observer } from "mobx-react-lite";
import type { HydroResult } from "../../types/hydrostatics";
import { getUnitSymbol } from "../../utils/unitSymbols";
import { settingsStore } from "../../stores/SettingsStore";

interface HullDiagramProps {
  lpp: number;
  beam?: number; // Reserved for future body plan view
  draft: number;
  designDraft: number;
  kb?: number;
  lcb?: number;
  kg?: number;
  lcg?: number;
  results?: HydroResult[]; // Reserved for future curve overlay
  highlightedDraft?: number | null;
  onDraftHover?: (draft: number | null) => void;
}

interface TooltipData {
  x: number;
  y: number;
  content: {
    title: string;
    items: { label: string; value: string }[];
  };
}

export const HullDiagram = observer(
  ({
    lpp,
    draft,
    designDraft,
    kb,
    lcb,
    kg,
    lcg,
    highlightedDraft,
    onDraftHover,
  }: HullDiagramProps) => {
    const [hoveredStation, setHoveredStation] = useState<number | null>(null);
    const [hoveredDraft, setHoveredDraft] = useState<number | null>(null);
    const [tooltip, setTooltip] = useState<TooltipData | null>(null);

    // Units
    const displayUnits = settingsStore.preferredUnits;
    const lengthUnit = getUnitSymbol(displayUnits, "Length");

    // SVG dimensions and scaling
    const viewBoxWidth = 500;
    const viewBoxHeight = 300;
    const margin = { top: 40, right: 40, bottom: 60, left: 40 };
    const drawWidth = viewBoxWidth - margin.left - margin.right;
    const drawHeight = viewBoxHeight - margin.top - margin.bottom;

    // Scale factors
    const maxDraft = Math.max(draft, designDraft, 10);
    const scaleX = drawWidth / lpp;
    const scaleY = drawHeight / maxDraft;

    // Number of stations to display
    const numStations = 11; // 0, 1, 2, ..., 10
    const stationSpacing = lpp / (numStations - 1);

    // Coordinate transformation: naval arch → SVG
    // eslint-disable-next-line react-hooks/exhaustive-deps
    const toSvgX = useCallback((x: number) => margin.left + x * scaleX, [scaleX]);
    // eslint-disable-next-line react-hooks/exhaustive-deps
    const toSvgY = useCallback(
      (z: number) => margin.top + drawHeight - z * scaleY,
      [drawHeight, scaleY]
    );

    // Generate simplified hull profile (parabolic shape for better visualization)
    const generateHullProfile = useMemo(() => {
      const points: { x: number; y: number }[] = [];
      const steps = 50;

      for (let i = 0; i <= steps; i++) {
        const xPos = (i / steps) * lpp;
        // Parabolic depth profile (deeper at midship)
        const normalizedX = (xPos / lpp - 0.5) * 2; // -1 to 1
        const depthFactor = 1 - normalizedX * normalizedX; // Parabola
        const depth = maxDraft * 1.2 * depthFactor;

        points.push({
          x: toSvgX(xPos),
          y: toSvgY(-depth * 0.3), // Below baseline for keel profile
        });
      }

      return points;
    }, [lpp, maxDraft, toSvgX, toSvgY]);

    // Create path string for hull
    const hullPath = useMemo(() => {
      if (generateHullProfile.length === 0) return "";

      let path = `M ${generateHullProfile[0].x} ${generateHullProfile[0].y}`;
      for (let i = 1; i < generateHullProfile.length; i++) {
        path += ` L ${generateHullProfile[i].x} ${generateHullProfile[i].y}`;
      }

      // Close the path along the baseline
      path += ` L ${toSvgX(lpp)} ${toSvgY(0)}`;
      path += ` L ${toSvgX(0)} ${toSvgY(0)}`;
      path += " Z";

      return path;
    }, [generateHullProfile, lpp, toSvgX, toSvgY]);

    // Handle station hover
    const handleStationHover = (
      stationIndex: number | null,
      event?: React.MouseEvent<SVGRectElement>
    ) => {
      setHoveredStation(stationIndex);

      if (stationIndex !== null && event) {
        const stationX = stationIndex * stationSpacing;

        setTooltip({
          x: event.clientX,
          y: event.clientY,
          content: {
            title: `Station ${stationIndex}`,
            items: [
              { label: "Position", value: `${stationX.toFixed(2)} ${lengthUnit}` },
              { label: "From AP", value: `${(lpp - stationX).toFixed(2)} ${lengthUnit}` },
              { label: "From FP", value: `${stationX.toFixed(2)} ${lengthUnit}` },
            ],
          },
        });
      } else {
        setTooltip(null);
      }
    };

    // Handle draft hover (vertical scrubbing)
    const handleDraftHover = (event: React.MouseEvent<SVGRectElement>) => {
      const rect = event.currentTarget.getBoundingClientRect();
      const svgY = ((event.clientY - rect.top) / rect.height) * viewBoxHeight;

      // Convert SVG Y back to draft
      const draftValue = maxDraft - ((svgY - margin.top) / drawHeight) * maxDraft;
      const clampedDraft = Math.max(0, Math.min(maxDraft, draftValue));

      setHoveredDraft(clampedDraft);
      if (onDraftHover) {
        onDraftHover(clampedDraft);
      }

      setTooltip({
        x: event.clientX,
        y: event.clientY,
        content: {
          title: "Draft Indicator",
          items: [{ label: "Draft", value: `${clampedDraft.toFixed(2)} ${lengthUnit}` }],
        },
      });
    };

    const handleDraftLeave = () => {
      setHoveredDraft(null);
      if (onDraftHover) {
        onDraftHover(null);
      }
      setTooltip(null);
    };

    // Handle key point hover
    const handleKeyPointHover = (
      pointType: "B" | "G",
      event: React.MouseEvent<SVGCircleElement>
    ) => {
      if (pointType === "B" && kb !== undefined && lcb !== undefined) {
        setTooltip({
          x: event.clientX,
          y: event.clientY,
          content: {
            title: "Center of Buoyancy (B)",
            items: [
              { label: "KB", value: `${kb.toFixed(3)} ${lengthUnit}` },
              { label: "LCB", value: `${lcb.toFixed(3)} ${lengthUnit}` },
            ],
          },
        });
      } else if (pointType === "G" && kg !== undefined && lcg !== undefined) {
        setTooltip({
          x: event.clientX,
          y: event.clientY,
          content: {
            title: "Center of Gravity (G)",
            items: [
              { label: "KG", value: `${kg.toFixed(3)} ${lengthUnit}` },
              { label: "LCG", value: `${lcg.toFixed(3)} ${lengthUnit}` },
            ],
          },
        });
      }
    };

    const displayDraft = highlightedDraft ?? hoveredDraft ?? draft;

    return (
      <div className="relative">
        <svg
          viewBox={`0 0 ${viewBoxWidth} ${viewBoxHeight}`}
          className="w-full h-full"
          style={{ minHeight: "300px" }}
        >
          {/* Title */}
          <text
            x={viewBoxWidth / 2}
            y={20}
            textAnchor="middle"
            className="text-[10px] font-semibold fill-gray-700 dark:fill-gray-300"
          >
            Hull Side Profile
          </text>

          {/* Hull outline */}
          <path
            d={hullPath}
            className="fill-gray-200 dark:fill-gray-700 stroke-gray-400 dark:stroke-gray-500"
            strokeWidth="1.5"
          />

          {/* Baseline (BL) */}
          <line
            x1={toSvgX(0)}
            y1={toSvgY(0)}
            x2={toSvgX(lpp)}
            y2={toSvgY(0)}
            className="stroke-gray-400 dark:stroke-gray-500"
            strokeWidth="2"
          />
          <text
            x={toSvgX(lpp) + 5}
            y={toSvgY(0) + 4}
            className="text-[8px] fill-gray-500 dark:fill-gray-400"
          >
            BL
          </text>

          {/* Design waterline (dashed) */}
          <line
            x1={toSvgX(0)}
            y1={toSvgY(designDraft)}
            x2={toSvgX(lpp)}
            y2={toSvgY(designDraft)}
            className="stroke-blue-300 dark:stroke-blue-600"
            strokeWidth="1"
            strokeDasharray="4,4"
          />

          {/* Current/Active waterline */}
          <line
            x1={toSvgX(0)}
            y1={toSvgY(displayDraft)}
            x2={toSvgX(lpp)}
            y2={toSvgY(displayDraft)}
            className="stroke-blue-500 dark:stroke-blue-400"
            strokeWidth="2"
          />
          <text
            x={toSvgX(lpp) + 5}
            y={toSvgY(displayDraft) + 4}
            className="text-[8px] fill-blue-600 dark:fill-blue-400"
          >
            WL
          </text>

          {/* Submerged hull section */}
          <rect
            x={toSvgX(0)}
            y={toSvgY(displayDraft)}
            width={drawWidth}
            height={toSvgY(0) - toSvgY(displayDraft)}
            className="fill-blue-100 dark:fill-blue-900 opacity-20"
          />

          {/* Station markers with hover areas */}
          {Array.from({ length: numStations }, (_, i) => {
            const stationX = i * stationSpacing;
            const svgX = toSvgX(stationX);
            const isHovered = hoveredStation === i;

            return (
              <g key={i}>
                {/* Hoverable area */}
                <rect
                  x={svgX - 10}
                  y={margin.top}
                  width={20}
                  height={drawHeight}
                  className="fill-transparent cursor-pointer"
                  onMouseEnter={(e) => handleStationHover(i, e)}
                  onMouseLeave={() => handleStationHover(null)}
                />

                {/* Station line */}
                <line
                  x1={svgX}
                  y1={toSvgY(0)}
                  x2={svgX}
                  y2={toSvgY(maxDraft)}
                  className={`${
                    isHovered
                      ? "stroke-blue-500 dark:stroke-blue-400"
                      : "stroke-gray-300 dark:stroke-gray-600"
                  } transition-colors`}
                  strokeWidth={isHovered ? "2" : "1"}
                  strokeDasharray="2,2"
                />

                {/* Station label */}
                <text
                  x={svgX}
                  y={toSvgY(0) + 20}
                  textAnchor="middle"
                  className={`text-[7px] ${
                    isHovered
                      ? "fill-blue-600 dark:fill-blue-400 font-semibold"
                      : "fill-gray-500 dark:fill-gray-400"
                  } transition-colors`}
                >
                  {i}
                </text>
              </g>
            );
          })}

          {/* Draft hover area (vertical scrubber) */}
          <rect
            x={margin.left}
            y={margin.top}
            width={drawWidth}
            height={drawHeight}
            className="fill-transparent cursor-ns-resize"
            onMouseMove={handleDraftHover}
            onMouseLeave={handleDraftLeave}
          />

          {/* AP and FP markers */}
          <g>
            {/* AP (Aft Perpendicular) */}
            <line
              x1={toSvgX(0)}
              y1={toSvgY(0)}
              x2={toSvgX(0)}
              y2={toSvgY(maxDraft)}
              className="stroke-gray-600 dark:stroke-gray-400"
              strokeWidth="2"
            />
            <text
              x={toSvgX(0)}
              y={toSvgY(maxDraft) - 5}
              textAnchor="middle"
              className="text-[9px] font-semibold fill-gray-700 dark:fill-gray-300"
            >
              AP
            </text>

            {/* FP (Forward Perpendicular) */}
            <line
              x1={toSvgX(lpp)}
              y1={toSvgY(0)}
              x2={toSvgX(lpp)}
              y2={toSvgY(maxDraft)}
              className="stroke-gray-600 dark:stroke-gray-400"
              strokeWidth="2"
            />
            <text
              x={toSvgX(lpp)}
              y={toSvgY(maxDraft) - 5}
              textAnchor="middle"
              className="text-[9px] font-semibold fill-gray-700 dark:fill-gray-300"
            >
              FP
            </text>

            {/* Midship */}
            <text
              x={toSvgX(lpp / 2)}
              y={toSvgY(maxDraft) - 5}
              textAnchor="middle"
              className="text-[8px] fill-gray-500 dark:fill-gray-400"
            >
              ⊥
            </text>
          </g>

          {/* Center of Buoyancy (B) */}
          {kb !== undefined && lcb !== undefined && (
            <g>
              <circle
                cx={toSvgX(lcb)}
                cy={toSvgY(kb)}
                r="4"
                className="fill-green-500 dark:fill-green-400 stroke-white dark:stroke-gray-800 cursor-pointer hover:r-6 transition-all"
                strokeWidth="1.5"
                onMouseEnter={(e) => handleKeyPointHover("B", e)}
                onMouseLeave={() => setTooltip(null)}
              />
              <text
                x={toSvgX(lcb) + 8}
                y={toSvgY(kb) + 3}
                className="text-[8px] font-semibold fill-green-600 dark:fill-green-400"
              >
                B
              </text>
            </g>
          )}

          {/* Center of Gravity (G) */}
          {kg !== undefined && lcg !== undefined && kg > 0 && lcg > 0 && (
            <g>
              <circle
                cx={toSvgX(lcg)}
                cy={toSvgY(kg)}
                r="4"
                className="fill-red-500 dark:fill-red-400 stroke-white dark:stroke-gray-800 cursor-pointer hover:r-6 transition-all"
                strokeWidth="1.5"
                onMouseEnter={(e) => handleKeyPointHover("G", e)}
                onMouseLeave={() => setTooltip(null)}
              />
              <text
                x={toSvgX(lcg) + 8}
                y={toSvgY(kg) + 3}
                className="text-[8px] font-semibold fill-red-600 dark:fill-red-400"
              >
                G
              </text>
            </g>
          )}

          {/* Dimension annotations */}
          <g>
            {/* Lpp dimension */}
            <line
              x1={toSvgX(0)}
              y1={viewBoxHeight - 25}
              x2={toSvgX(lpp)}
              y2={viewBoxHeight - 25}
              className="stroke-gray-400 dark:stroke-gray-500"
              strokeWidth="1"
              markerStart="url(#arrowLeft)"
              markerEnd="url(#arrowRight)"
            />
            <text
              x={toSvgX(lpp / 2)}
              y={viewBoxHeight - 30}
              textAnchor="middle"
              className="text-[9px] font-medium fill-gray-600 dark:fill-gray-400"
            >
              Lpp = {lpp.toFixed(1)} {lengthUnit}
            </text>

            {/* Draft dimension */}
            <line
              x1={viewBoxWidth - 25}
              y1={toSvgY(0)}
              x2={viewBoxWidth - 25}
              y2={toSvgY(displayDraft)}
              className="stroke-blue-400 dark:stroke-blue-500"
              strokeWidth="1"
              markerStart="url(#arrowUp)"
              markerEnd="url(#arrowDown)"
            />
            <text
              x={viewBoxWidth - 20}
              y={(toSvgY(0) + toSvgY(displayDraft)) / 2}
              textAnchor="start"
              className="text-[8px] font-medium fill-blue-600 dark:fill-blue-400"
            >
              {displayDraft.toFixed(2)} {lengthUnit}
            </text>
          </g>

          {/* Arrow markers */}
          <defs>
            <marker
              id="arrowRight"
              markerWidth="10"
              markerHeight="10"
              refX="9"
              refY="3"
              orient="auto"
              markerUnits="strokeWidth"
            >
              <path d="M0,0 L0,6 L9,3 z" className="fill-gray-400 dark:fill-gray-500" />
            </marker>
            <marker
              id="arrowLeft"
              markerWidth="10"
              markerHeight="10"
              refX="0"
              refY="3"
              orient="auto"
              markerUnits="strokeWidth"
            >
              <path d="M9,0 L9,6 L0,3 z" className="fill-gray-400 dark:fill-gray-500" />
            </marker>
            <marker
              id="arrowUp"
              markerWidth="10"
              markerHeight="10"
              refX="3"
              refY="0"
              orient="auto"
              markerUnits="strokeWidth"
            >
              <path d="M0,9 L6,9 L3,0 z" className="fill-blue-400 dark:fill-blue-500" />
            </marker>
            <marker
              id="arrowDown"
              markerWidth="10"
              markerHeight="10"
              refX="3"
              refY="9"
              orient="auto"
              markerUnits="strokeWidth"
            >
              <path d="M0,0 L6,0 L3,9 z" className="fill-blue-400 dark:fill-blue-500" />
            </marker>
          </defs>
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
    );
  }
);

export default HullDiagram;
