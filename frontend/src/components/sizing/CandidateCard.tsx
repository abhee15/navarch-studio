import React from "react";
import type { CandidateDesign } from "../../types/sizing";
import { Button } from "../ui/button";
import { Hull3DThumbnail } from "./visualization/Hull3DThumbnail";

interface CandidateCardProps {
  candidate: CandidateDesign;
  rank: number;
  onSelect: () => void;
  onCompare: () => void;
  isComparing: boolean;
}

export const CandidateCard: React.FC<CandidateCardProps> = ({
  candidate,
  rank,
  onSelect,
  onCompare,
  isComparing,
}) => {
  // Parse flags
  let flags: string[] = [];
  try {
    flags = JSON.parse(candidate.flagsJson);
  } catch {
    // Ignore parse errors
  }

  return (
    <div
      className={`rounded-lg bg-white p-6 shadow transition-all hover:shadow-xl dark:bg-gray-800 ${
        isComparing ? "ring-2 ring-blue-500" : ""
      }`}
    >
      {/* Header with Rank & Family */}
      <div className="flex items-start justify-between">
        <div>
          <div className="flex items-center space-x-2">
            <span className="flex h-8 w-8 items-center justify-center rounded-full bg-blue-600 text-sm font-bold text-white">
              #{rank}
            </span>
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white capitalize">
              {candidate.hullFamily.replace("_", " ")}
            </h3>
          </div>
          <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
            Score: {(candidate.score * 100).toFixed(1)}%
          </p>
        </div>

        {/* Flags */}
        {flags.length > 0 && (
          <div className="flex flex-wrap gap-1">
            {flags.slice(0, 2).map((flag) => (
              <span
                key={flag}
                className={`rounded px-2 py-1 text-xs font-medium ${
                  flag.includes("constrained") || flag.includes("exceeded")
                    ? "bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-400"
                    : "bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-400"
                }`}
              >
                {flag.replace("_", " ")}
              </span>
            ))}
            {flags.length > 2 && (
              <span className="rounded bg-gray-100 px-2 py-1 text-xs font-medium text-gray-600 dark:bg-gray-700 dark:text-gray-400">
                +{flags.length - 2}
              </span>
            )}
          </div>
        )}
      </div>

      {/* 3D Thumbnail */}
      <div className="mt-4">
        <Hull3DThumbnail candidate={candidate} height={180} />
      </div>

      {/* Principal Dimensions */}
      <div className="mt-4 grid grid-cols-2 gap-4 text-sm">
        <div>
          <dt className="font-medium text-gray-500 dark:text-gray-400">Lpp:</dt>
          <dd className="text-lg font-semibold text-gray-900 dark:text-white">
            {candidate.lppM?.toFixed(1) || "N/A"} m
          </dd>
        </div>
        <div>
          <dt className="font-medium text-gray-500 dark:text-gray-400">Beam:</dt>
          <dd className="text-lg font-semibold text-gray-900 dark:text-white">
            {candidate.beamM?.toFixed(1) || "N/A"} m
          </dd>
        </div>
        <div>
          <dt className="font-medium text-gray-500 dark:text-gray-400">Draft:</dt>
          <dd className="text-lg font-semibold text-gray-900 dark:text-white">
            {candidate.draftM?.toFixed(1) || "N/A"} m
          </dd>
        </div>
        <div>
          <dt className="font-medium text-gray-500 dark:text-gray-400">Cb:</dt>
          <dd className="text-lg font-semibold text-gray-900 dark:text-white">
            {candidate.cb?.toFixed(3) || "N/A"}
          </dd>
        </div>
      </div>

      {/* KPIs */}
      <div className="mt-6 space-y-2 border-t border-gray-200 pt-4 text-sm dark:border-gray-700">
        <div className="flex justify-between">
          <span className="text-gray-600 dark:text-gray-400">Displacement:</span>
          <span className="font-medium text-gray-900 dark:text-white">
            {candidate.dispT?.toFixed(0) || "N/A"} t
          </span>
        </div>
        <div className="flex justify-between">
          <span className="text-gray-600 dark:text-gray-400">Froude Number:</span>
          <span className="font-medium text-gray-900 dark:text-white">
            {candidate.fn?.toFixed(3) || "N/A"}
          </span>
        </div>
        {candidate.ehpKw && (
          <div className="flex justify-between">
            <span className="text-gray-600 dark:text-gray-400">EHP:</span>
            <span className="font-medium text-gray-900 dark:text-white">
              {candidate.ehpKw.toFixed(0)} kW
            </span>
          </div>
        )}
        {candidate.gmEstM && (
          <div className="flex justify-between">
            <span className="text-gray-600 dark:text-gray-400">GMt (est):</span>
            <span className="font-medium text-gray-900 dark:text-white">
              {candidate.gmEstM.toFixed(2)} m
            </span>
          </div>
        )}
      </div>

      {/* Actions */}
      <div className="mt-6 flex space-x-2">
        <Button onClick={onSelect} className="flex-1">
          Open Workspace
        </Button>
        <Button
          variant="outline"
          onClick={onCompare}
          className={isComparing ? "ring-2 ring-primary" : ""}
        >
          {isComparing ? "✓" : "Compare"}
        </Button>
      </div>
    </div>
  );
};
