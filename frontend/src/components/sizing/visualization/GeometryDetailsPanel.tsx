import React, { useMemo } from "react";
import type { CandidateDesign } from "../../../types/sizing";
import { ChevronDown, ChevronUp } from "lucide-react";

interface GeometryDetailsPanelProps {
  candidate: CandidateDesign;
  className?: string;
}

/**
 * Geometry Details Panel Component
 *
 * Displays detailed ShipD geometry parameters including:
 * - Section geometry (flare, deadrise, chine type)
 * - Longitudinal segmentation (bow/mid/stern ratios)
 * - Bulb geometry (if applicable)
 */
export const GeometryDetailsPanel: React.FC<GeometryDetailsPanelProps> = ({
  candidate,
  className = "",
}) => {
  const [expandedSections, setExpandedSections] = React.useState({
    sectionGeometry: true,
    longitudinal: true,
    bulb: false,
  });

  // Note: Additional parameters would come from mission case or sizing run
  // For now, we extract geometry info directly from ShipD vector

  // Parse ShipD vector to extract key geometry parameters
  const geometryParams = useMemo(() => {
    if (!candidate.shipdParametersJson) {
      return null;
    }
    try {
      const vector = JSON.parse(candidate.shipdParametersJson);
      if (!Array.isArray(vector) || vector.length !== 45) {
        return null;
      }

      // Extract key parameters
      return {
        // Longitudinal proportions
        lb: vector[1], // Bow length ratio
        ls: vector[2], // Stern length ratio
        lm: 1 - vector[1] - vector[2], // Mid-body length ratio

        // Bow parameters
        beta: vector[8], // Flare angle (normalized)
        cdrft: vector[19], // Deadrise angle (normalized)
        rc: vector[9], // Curvature coefficient
        rk: vector[10], // Knuckle coefficient

        // Midship parameters
        bitEPS: vector[20] > 0.5, // Sheer extrusion
        bitEPT: vector[21] > 0.5, // Tumblehome

        // Stern parameters
        atrans: vector[22], // Transom area ratio
        betaTrans: vector[27], // Stern rake (normalized)
        bcTrans: vector[28], // Transom width ratio

        // Bulb parameters
        hasBulb: vector[31] > 0.5, // bit_BB
        lbb: vector[33], // Bulb length ratio
        hbb: vector[34], // Bulb height ratio
        bbb: vector[35], // Bulb width ratio
      };
    } catch {
      return null;
    }
  }, [candidate.shipdParametersJson]);

  if (!geometryParams) {
    return (
      <div
        className={`rounded-lg border border-gray-200 bg-gray-50 p-4 text-center text-sm text-gray-500 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-400 ${className}`}
      >
        No geometry parameters available
      </div>
    );
  }

  const toggleSection = (section: keyof typeof expandedSections) => {
    setExpandedSections((prev) => ({
      ...prev,
      [section]: !prev[section],
    }));
  };

  return (
    <div className={`space-y-3 ${className}`}>
      {/* Section Geometry */}
      <div className="rounded-lg border border-gray-200 bg-white dark:border-gray-700 dark:bg-gray-800">
        <button
          onClick={() => toggleSection("sectionGeometry")}
          className="flex w-full items-center justify-between p-3 text-left font-medium text-gray-900 hover:bg-gray-50 dark:text-gray-100 dark:hover:bg-gray-700"
        >
          <span>Section Geometry</span>
          {expandedSections.sectionGeometry ? (
            <ChevronUp className="h-4 w-4" />
          ) : (
            <ChevronDown className="h-4 w-4" />
          )}
        </button>
        {expandedSections.sectionGeometry && (
          <div className="border-t border-gray-200 p-3 dark:border-gray-700">
            <div className="grid grid-cols-2 gap-3 text-sm">
              <div>
                <span className="text-gray-500 dark:text-gray-400">Flare Angle (β):</span>
                <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">
                  {(geometryParams.beta * 100).toFixed(1)}% (normalized)
                </span>
              </div>
              <div>
                <span className="text-gray-500 dark:text-gray-400">Deadrise (Cdrft):</span>
                <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">
                  {(geometryParams.cdrft * 100).toFixed(1)}% (normalized)
                </span>
              </div>
              <div>
                <span className="text-gray-500 dark:text-gray-400">Curvature (Rc):</span>
                <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">
                  {geometryParams.rc.toFixed(3)}
                </span>
              </div>
              <div>
                <span className="text-gray-500 dark:text-gray-400">Knuckle (Rk):</span>
                <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">
                  {geometryParams.rk.toFixed(3)}
                </span>
              </div>
              <div>
                <span className="text-gray-500 dark:text-gray-400">Tumblehome:</span>
                <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">
                  {geometryParams.bitEPT ? "Enabled" : "Disabled"}
                </span>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Longitudinal Segmentation */}
      <div className="rounded-lg border border-gray-200 bg-white dark:border-gray-700 dark:bg-gray-800">
        <button
          onClick={() => toggleSection("longitudinal")}
          className="flex w-full items-center justify-between p-3 text-left font-medium text-gray-900 hover:bg-gray-50 dark:text-gray-100 dark:hover:bg-gray-700"
        >
          <span>Longitudinal Segmentation</span>
          {expandedSections.longitudinal ? (
            <ChevronUp className="h-4 w-4" />
          ) : (
            <ChevronDown className="h-4 w-4" />
          )}
        </button>
        {expandedSections.longitudinal && (
          <div className="border-t border-gray-200 p-3 dark:border-gray-700">
            <div className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-gray-500 dark:text-gray-400">Bow Length (Lb):</span>
                <span className="font-medium text-gray-900 dark:text-gray-100">
                  {(geometryParams.lb * 100).toFixed(1)}%
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500 dark:text-gray-400">Mid-Body Length (Lm):</span>
                <span className="font-medium text-gray-900 dark:text-gray-100">
                  {(geometryParams.lm * 100).toFixed(1)}%
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-500 dark:text-gray-400">Stern Length (Ls):</span>
                <span className="font-medium text-gray-900 dark:text-gray-100">
                  {(geometryParams.ls * 100).toFixed(1)}%
                </span>
              </div>
              <div className="mt-2 h-2 w-full overflow-hidden rounded-full bg-gray-200 dark:bg-gray-700">
                <div
                  className="h-full bg-blue-500"
                  style={{ width: `${geometryParams.lb * 100}%` }}
                />
                <div
                  className="h-full bg-green-500"
                  style={{
                    width: `${geometryParams.lm * 100}%`,
                    marginLeft: `${geometryParams.lb * 100}%`,
                  }}
                />
                <div
                  className="h-full bg-orange-500"
                  style={{
                    width: `${geometryParams.ls * 100}%`,
                    marginLeft: `${(geometryParams.lb + geometryParams.lm) * 100}%`,
                  }}
                />
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Bulb Geometry */}
      {geometryParams.hasBulb && (
        <div className="rounded-lg border border-gray-200 bg-white dark:border-gray-700 dark:bg-gray-800">
          <button
            onClick={() => toggleSection("bulb")}
            className="flex w-full items-center justify-between p-3 text-left font-medium text-gray-900 hover:bg-gray-50 dark:text-gray-100 dark:hover:bg-gray-700"
          >
            <span>Bulb Geometry</span>
            {expandedSections.bulb ? (
              <ChevronUp className="h-4 w-4" />
            ) : (
              <ChevronDown className="h-4 w-4" />
            )}
          </button>
          {expandedSections.bulb && (
            <div className="border-t border-gray-200 p-3 dark:border-gray-700">
              <div className="grid grid-cols-2 gap-3 text-sm">
                <div>
                  <span className="text-gray-500 dark:text-gray-400">Length Ratio (Lbb):</span>
                  <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">
                    {geometryParams.lbb.toFixed(3)}
                  </span>
                </div>
                <div>
                  <span className="text-gray-500 dark:text-gray-400">Height Ratio (Hbb):</span>
                  <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">
                    {geometryParams.hbb.toFixed(3)}
                  </span>
                </div>
                <div>
                  <span className="text-gray-500 dark:text-gray-400">Width Ratio (Bbb):</span>
                  <span className="ml-2 font-medium text-gray-900 dark:text-gray-100">
                    {geometryParams.bbb.toFixed(3)}
                  </span>
                </div>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
