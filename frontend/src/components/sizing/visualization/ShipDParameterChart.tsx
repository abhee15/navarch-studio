import React, { useMemo } from "react";
import type { CandidateDesign, ShipDParameterMetadata } from "../../../types/sizing";

interface ShipDParameterChartProps {
  candidate: CandidateDesign;
  metadata: ShipDParameterMetadata[];
  showComparison?: CandidateDesign[];
  highlightGroup?: "principal" | "bow" | "midship" | "stern" | "appendages";
  className?: string;
}

/**
 * ShipD Parameter Chart Component
 *
 * Visualizes ShipD 45-parameter vector vs typical ranges.
 * Color-coded bars: green (within range), yellow (near limits), red (out of range).
 * Grouped by category: Principal, Bow, Midship, Stern, Appendages.
 */
export const ShipDParameterChart: React.FC<ShipDParameterChartProps> = ({
  candidate,
  metadata,
  showComparison = [],
  highlightGroup,
  className = "",
}) => {
  // Parse ShipD vector from candidate
  const shipdVector = useMemo(() => {
    if (!candidate.shipdParametersJson) {
      return null;
    }
    try {
      const parsed = JSON.parse(candidate.shipdParametersJson);
      return Array.isArray(parsed) && parsed.length === 45 ? parsed : null;
    } catch {
      return null;
    }
  }, [candidate.shipdParametersJson]);

  // Parse comparison vectors
  const comparisonVectors = useMemo(() => {
    return showComparison
      .map((c) => {
        if (!c.shipdParametersJson) return null;
        try {
          const parsed = JSON.parse(c.shipdParametersJson);
          return Array.isArray(parsed) && parsed.length === 45 ? parsed : null;
        } catch {
          return null;
        }
      })
      .filter((v): v is number[] => v !== null);
  }, [showComparison]);

  // Group parameters by category
  const parameterGroups = useMemo(() => {
    const groups: Record<string, ShipDParameterMetadata[]> = {
      principal: [],
      bow: [],
      midship: [],
      stern: [],
      appendages: [],
    };

    metadata.forEach((param) => {
      const group = param.group?.toLowerCase() || "other";
      if (group.includes("principal") || group.includes("dimension")) {
        groups.principal.push(param);
      } else if (group.includes("bow")) {
        groups.bow.push(param);
      } else if (group.includes("mid")) {
        groups.midship.push(param);
      } else if (group.includes("stern")) {
        groups.stern.push(param);
      } else if (group.includes("appendage") || group.includes("bulb")) {
        groups.appendages.push(param);
      }
    });

    return groups;
  }, [metadata]);

  if (!shipdVector) {
    return (
      <div
        className={`rounded-lg border border-gray-200 bg-gray-50 p-4 text-center text-sm text-gray-500 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-400 ${className}`}
      >
        No ShipD parameters available for this candidate
      </div>
    );
  }

  return (
    <div className={`space-y-4 ${className}`}>
      {Object.entries(parameterGroups).map(([groupName, params]) => {
        if (params.length === 0) return null;

        const isHighlighted = highlightGroup === groupName;

        return (
          <div
            key={groupName}
            className={`rounded-lg border p-4 ${
              isHighlighted
                ? "border-blue-500 bg-blue-50 dark:border-blue-400 dark:bg-blue-900/20"
                : "border-gray-200 bg-white dark:border-gray-700 dark:bg-gray-800"
            }`}
          >
            <h3 className="mb-3 text-sm font-semibold uppercase tracking-wide text-gray-700 dark:text-gray-300">
              {groupName.charAt(0).toUpperCase() + groupName.slice(1)} Parameters
            </h3>
            <div className="space-y-2">
              {params.map((param) => {
                const value = shipdVector[param.parameterIndex];
                const min = param.min ?? 0;
                const max = param.max ?? 1;
                const mean = param.mean ?? (min + max) / 2;
                // const stdDev = param.stdDev ?? (max - min) / 6; // Not used in current implementation

                // Determine color based on value position
                const normalizedValue = (value - min) / (max - min);
                let barColor = "bg-green-500";
                if (normalizedValue < 0.1 || normalizedValue > 0.9) {
                  barColor = "bg-red-500";
                } else if (normalizedValue < 0.2 || normalizedValue > 0.8) {
                  barColor = "bg-yellow-500";
                }

                // Denormalize value for display
                const denormalizedValue = min + (max - min) * value;

                return (
                  <div key={param.parameterIndex} className="space-y-1">
                    <div className="flex items-center justify-between text-xs">
                      <span className="font-medium text-gray-700 dark:text-gray-300">
                        {param.label}
                      </span>
                      <span className="text-gray-500 dark:text-gray-400">
                        {denormalizedValue.toFixed(3)} {param.unit || ""}
                      </span>
                    </div>
                    <div className="relative h-2 w-full overflow-hidden rounded-full bg-gray-200 dark:bg-gray-700">
                      <div
                        className={`h-full ${barColor} transition-all`}
                        style={{ width: `${normalizedValue * 100}%` }}
                      />
                      {/* Mean indicator */}
                      {mean && (
                        <div
                          className="absolute top-0 h-full w-0.5 bg-blue-500"
                          style={{ left: `${((mean - min) / (max - min)) * 100}%` }}
                        />
                      )}
                    </div>
                    <div className="flex justify-between text-xs text-gray-400 dark:text-gray-500">
                      <span>Min: {min.toFixed(2)}</span>
                      {mean && <span>Mean: {mean.toFixed(2)}</span>}
                      <span>Max: {max.toFixed(2)}</span>
                    </div>
                    {/* Comparison bars */}
                    {comparisonVectors.length > 0 && (
                      <div className="mt-1 flex gap-1">
                        {comparisonVectors.map((compVector, idx) => {
                          const compValue = compVector[param.parameterIndex];
                          const compNormalized = (compValue - min) / (max - min);
                          return (
                            <div
                              key={idx}
                              className="h-1 flex-1 rounded bg-gray-300 dark:bg-gray-600"
                              style={{ width: `${compNormalized * 100}%` }}
                              title={`Comparison ${idx + 1}: ${compValue.toFixed(3)}`}
                            />
                          );
                        })}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          </div>
        );
      })}
    </div>
  );
};
