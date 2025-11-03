import React, { useState, useRef } from "react";
import { Hull3DScene } from "./Hull3DScene";
import { Hull2DPlan } from "./Hull2DPlan";
import { Hull2DProfile } from "./Hull2DProfile";
import { Hull2DSections } from "./Hull2DSections";
import type { CandidateDesign } from "../../../types/sizing";
import { exportSVG, exportSVGToPNG, exportCanvasToPNG, generateFilename } from "../../../utils/exportViewport";

interface ViewportQuadLayoutProps {
  candidate: CandidateDesign;
}

type ViewportMode = "quad" | "plan" | "profile" | "sections" | "3d";

/**
 * CAD-Style Quad Viewport Layout
 *
 * Layout:
 * ┌─────────────┬─────────────┐
 * │  Plan (Top) │ Profile     │
 * │             │ (Side)      │
 * ├─────────────┼─────────────┤
 * │  Sections   │ 3D          │
 * │  (Body Plan)│ (Isometric) │
 * └─────────────┴─────────────┘
 *
 * Features:
 * - Click viewport header to maximize
 * - Synchronized highlighting across views
 * - Responsive (collapses to 1-up on mobile)
 */
export const ViewportQuadLayout: React.FC<ViewportQuadLayoutProps> = ({ candidate }) => {
  const [mode, setMode] = useState<ViewportMode>("quad");
  const [show3DWaterplane] = useState(true);
  const [show3DCenters] = useState(true);
  const [show3DGrid] = useState(true);

  // Refs for export
  const planRef = useRef<SVGSVGElement>(null);
  const profileRef = useRef<SVGSVGElement>(null);
  const sectionsRef = useRef<SVGSVGElement>(null);

  // Export handlers
  const handleExportSVG = () => {
    let svgElement: SVGSVGElement | null = null;
    let viewType: "plan" | "profile" | "sections" | "3d" = "plan";

    switch (mode) {
      case "plan":
        svgElement = planRef.current;
        viewType = "plan";
        break;
      case "profile":
        svgElement = profileRef.current;
        viewType = "profile";
        break;
      case "sections":
        svgElement = sectionsRef.current;
        viewType = "sections";
        break;
      default:
        return;
    }

    if (svgElement) {
      const filename = generateFilename(candidate.id, viewType, candidate.hullFamily);
      exportSVG(svgElement, filename);
    }
  };

  const handleExportPNG = async () => {
    // For 3D view, export canvas
    if (mode === "3d") {
      const canvas = document.querySelector("canvas");
      if (canvas) {
        const filename = generateFilename(candidate.id, "3d", candidate.hullFamily);
        exportCanvasToPNG(canvas, filename);
      }
      return;
    }

    // For 2D views, export SVG to PNG
    let svgElement: SVGSVGElement | null = null;
    let viewType: "plan" | "profile" | "sections" | "3d" = "plan";

    switch (mode) {
      case "plan":
        svgElement = planRef.current;
        viewType = "plan";
        break;
      case "profile":
        svgElement = profileRef.current;
        viewType = "profile";
        break;
      case "sections":
        svgElement = sectionsRef.current;
        viewType = "sections";
        break;
      default:
        return;
    }

    if (svgElement) {
      const filename = generateFilename(candidate.id, viewType, candidate.hullFamily);
      try {
        await exportSVGToPNG(svgElement, filename, 2);
      } catch (error) {
        console.error("Failed to export PNG:", error);
      }
    }
  };

  // Maximized view with smooth transition
  if (mode !== "quad") {
    return (
      <div className="w-full h-full flex flex-col animate-zoomIn">
        {/* Enhanced Toolbar */}
        <div className="bg-gradient-to-r from-gray-50 via-white to-gray-50 dark:from-gray-900 dark:via-gray-800 dark:to-gray-900 border-b border-gray-200 dark:border-gray-700 p-3 flex items-center justify-between shadow-sm">
          <div className="flex items-center gap-3">
            <button
              onClick={() => setMode("quad")}
              className="px-4 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg shadow-md hover:shadow-lg transition-all duration-200 flex items-center gap-2 font-medium"
            >
              <span>←</span>
              <span>Quad View</span>
            </button>
            <div className="h-6 w-px bg-gray-300 dark:bg-gray-600"></div>
            <span className="text-base font-bold text-gray-900 dark:text-white capitalize flex items-center gap-2">
              <span className="w-2 h-2 rounded-full bg-blue-600 animate-pulse"></span>
              {mode === "3d" ? "3D Isometric" : `${mode} View`}
            </span>
          </div>
          
          {/* Export Buttons */}
          <div className="flex items-center gap-2">
            {mode !== "3d" && (
              <button
                onClick={handleExportSVG}
                className="px-3 py-2 text-sm bg-green-600 hover:bg-green-700 text-white rounded-lg shadow hover:shadow-md transition-all duration-200 flex items-center gap-2"
                title="Export as SVG (vector)"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M9 19l3 3m0 0l3-3m-3 3V10" />
                </svg>
                <span>SVG</span>
              </button>
            )}
            <button
              onClick={handleExportPNG}
              className="px-3 py-2 text-sm bg-purple-600 hover:bg-purple-700 text-white rounded-lg shadow hover:shadow-md transition-all duration-200 flex items-center gap-2"
              title="Export as PNG (raster, 2x resolution)"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
              <span>PNG</span>
            </button>
          </div>
        </div>

        {/* Maximized viewport */}
        <div className="flex-1">
          {mode === "plan" && <Hull2DPlan candidate={candidate} ref={planRef} />}
          {mode === "profile" && <Hull2DProfile candidate={candidate} ref={profileRef} />}
          {mode === "sections" && <Hull2DSections candidate={candidate} ref={sectionsRef} />}
          {mode === "3d" && (
            <Hull3DScene
              candidate={candidate}
              showWaterplane={show3DWaterplane}
              showCenters={show3DCenters}
              showGrid={show3DGrid}
            />
          )}
        </div>
      </div>
    );
  }

  // Quad view
  return (
    <div className="w-full h-full grid grid-cols-1 md:grid-cols-2 gap-1 bg-gray-200 dark:bg-gray-700">
      {/* Top-Left: Plan View */}
      <div className="bg-white dark:bg-gray-800 rounded-lg overflow-hidden flex flex-col min-h-[300px]">
        <div
          className="bg-gray-100 dark:bg-gray-900 px-3 py-2 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between cursor-pointer hover:bg-gray-200 dark:hover:bg-gray-800"
          onClick={() => setMode("plan")}
        >
          <span className="text-sm font-semibold text-gray-900 dark:text-white">
            Plan View (Top)
          </span>
          <button className="text-xs text-blue-600 dark:text-blue-400 hover:underline">
            Maximize →
          </button>
        </div>
        <div className="flex-1">
          <Hull2DPlan candidate={candidate} />
        </div>
      </div>

      {/* Top-Right: Profile View */}
      <div className="bg-white dark:bg-gray-800 rounded-lg overflow-hidden flex flex-col min-h-[300px]">
        <div
          className="bg-gray-100 dark:bg-gray-900 px-3 py-2 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between cursor-pointer hover:bg-gray-200 dark:hover:bg-gray-800"
          onClick={() => setMode("profile")}
        >
          <span className="text-sm font-semibold text-gray-900 dark:text-white">
            Profile View (Side)
          </span>
          <button className="text-xs text-blue-600 dark:text-blue-400 hover:underline">
            Maximize →
          </button>
        </div>
        <div className="flex-1">
          <Hull2DProfile candidate={candidate} />
        </div>
      </div>

      {/* Bottom-Left: Sections View */}
      <div className="bg-white dark:bg-gray-800 rounded-lg overflow-hidden flex flex-col min-h-[300px]">
        <div
          className="bg-gray-100 dark:bg-gray-900 px-3 py-2 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between cursor-pointer hover:bg-gray-200 dark:hover:bg-gray-800"
          onClick={() => setMode("sections")}
        >
          <span className="text-sm font-semibold text-gray-900 dark:text-white">
            Sections (Body Plan)
          </span>
          <button className="text-xs text-blue-600 dark:text-blue-400 hover:underline">
            Maximize →
          </button>
        </div>
        <div className="flex-1">
          <Hull2DSections candidate={candidate} />
        </div>
      </div>

      {/* Bottom-Right: 3D View */}
      <div className="bg-white dark:bg-gray-800 rounded-lg overflow-hidden flex flex-col min-h-[300px]">
        <div
          className="bg-gray-100 dark:bg-gray-900 px-3 py-2 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between cursor-pointer hover:bg-gray-200 dark:hover:bg-gray-800"
          onClick={() => setMode("3d")}
        >
          <span className="text-sm font-semibold text-gray-900 dark:text-white">3D Isometric</span>
          <button className="text-xs text-blue-600 dark:text-blue-400 hover:underline">
            Maximize →
          </button>
        </div>
        <div className="flex-1">
          <Hull3DScene
            candidate={candidate}
            showWaterplane={show3DWaterplane}
            showCenters={show3DCenters}
            showGrid={show3DGrid}
          />
        </div>
      </div>
    </div>
  );
};
