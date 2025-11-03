import React, { useState } from "react";
import { Hull3DScene } from "./Hull3DScene";
import { Hull2DPlan } from "./Hull2DPlan";
import { Hull2DProfile } from "./Hull2DProfile";
import { Hull2DSections } from "./Hull2DSections";
import type { CandidateDesign } from "../../../types/sizing";

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

  // Maximized view
  if (mode !== "quad") {
    return (
      <div className="w-full h-full flex flex-col">
        {/* Toolbar */}
        <div className="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 p-2 flex items-center justify-between">
          <div className="flex items-center gap-2">
            <button
              onClick={() => setMode("quad")}
              className="px-3 py-1 text-sm bg-gray-200 dark:bg-gray-700 rounded hover:bg-gray-300 dark:hover:bg-gray-600"
            >
              ← Back to Quad View
            </button>
            <span className="text-sm font-semibold text-gray-900 dark:text-white capitalize">
              {mode === "3d" ? "3D Isometric" : mode} View
            </span>
          </div>
        </div>

        {/* Maximized viewport */}
        <div className="flex-1">
          {mode === "plan" && <Hull2DPlan candidate={candidate} />}
          {mode === "profile" && <Hull2DProfile candidate={candidate} />}
          {mode === "sections" && <Hull2DSections candidate={candidate} />}
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
