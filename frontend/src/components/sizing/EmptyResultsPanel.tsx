import React, { useState } from "react";
import { AlertTriangle, ChevronDown, ChevronUp, HelpCircle } from "lucide-react";
import type { SolverDiagnostics } from "../../types/sizing";

interface EmptyResultsPanelProps {
  diagnostics?: SolverDiagnostics;
  onAdjustParameters?: () => void;
  onCloneBrief?: () => void;
}

export const EmptyResultsPanel: React.FC<EmptyResultsPanelProps> = ({
  diagnostics,
  onAdjustParameters,
  onCloneBrief,
}) => {
  const [showDetails, setShowDetails] = useState(false);

  if (!diagnostics) {
    // Fallback for when diagnostics aren't available
    return (
      <div className="rounded-lg bg-yellow-50 dark:bg-yellow-900/20 p-8 text-center border border-yellow-200 dark:border-yellow-800">
        <AlertTriangle className="mx-auto h-12 w-12 text-yellow-600 dark:text-yellow-400 mb-4" />
        <h3 className="text-lg font-semibold text-foreground mb-2">No Designs Generated</h3>
        <p className="text-muted-foreground mb-4">
          The solver was unable to generate any hull designs with the current parameters.
        </p>
        <p className="text-sm text-muted-foreground mb-6">
          Try adjusting your speed, cargo capacity, or relaxing physical constraints.
        </p>
        {onAdjustParameters && (
          <button
            onClick={onAdjustParameters}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
          >
            Adjust Parameters
          </button>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Main Status Card */}
      <div className="rounded-lg bg-gradient-to-br from-yellow-50 to-orange-50 dark:from-yellow-900/20 dark:to-orange-900/20 p-6 border border-yellow-200 dark:border-yellow-800">
        <div className="flex items-start gap-4">
          <div className="flex-shrink-0">
            <AlertTriangle className="h-8 w-8 text-yellow-600 dark:text-yellow-400" />
          </div>
          <div className="flex-1">
            <h3 className="text-lg font-semibold text-foreground mb-2">No Designs Generated</h3>
            <p className="text-foreground mb-4">{diagnostics.summary}</p>

            {/* Key Metrics */}
            <div className="grid grid-cols-2 md:grid-cols-3 gap-3 mb-4">
              <div className="bg-card/50 rounded p-2">
                <div className="text-xs text-muted-foreground">Families Tried</div>
                <div className="text-lg font-semibold text-foreground">
                  {diagnostics.totalFamiliesConsidered}
                </div>
              </div>
              <div className="bg-card/50 rounded p-2">
                <div className="text-xs text-muted-foreground">Failed Closure</div>
                <div className="text-lg font-semibold text-red-600 dark:text-red-400">
                  {diagnostics.familiesFailedClosure}
                </div>
              </div>
              <div className="bg-card/50 rounded p-2">
                <div className="text-xs text-muted-foreground">Target Δ</div>
                <div className="text-lg font-semibold text-foreground">
                  {diagnostics.targetDisplacementT.toFixed(0)}t
                </div>
              </div>
            </div>

            {/* Suggestions */}
            {diagnostics.suggestions.length > 0 && (
              <div className="space-y-2">
                <h4 className="text-sm font-semibold text-foreground flex items-center gap-2">
                  <HelpCircle className="h-4 w-4" />
                  Suggestions
                </h4>
                <ul className="space-y-2">
                  {diagnostics.suggestions.map((suggestion, idx) => (
                    <li key={idx} className="flex items-start gap-2 text-sm text-foreground">
                      <span className="text-blue-600 dark:text-blue-400 flex-shrink-0 mt-0.5">
                        •
                      </span>
                      <span>{suggestion}</span>
                    </li>
                  ))}
                </ul>
              </div>
            )}

            {/* Actions */}
            <div className="flex flex-wrap gap-3 mt-6">
              {onAdjustParameters && (
                <button
                  onClick={onAdjustParameters}
                  className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors font-medium"
                >
                  Adjust Parameters
                </button>
              )}
              {onCloneBrief && (
                <button
                  onClick={onCloneBrief}
                  className="px-4 py-2 bg-muted text-foreground rounded-lg hover:bg-muted/80 transition-colors font-medium"
                >
                  Clone & Edit Brief
                </button>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Technical Details (Collapsible) */}
      <button
        onClick={() => setShowDetails(!showDetails)}
        className="w-full flex items-center justify-between px-4 py-3 bg-muted rounded-lg hover:bg-muted/80 transition-colors"
      >
        <span className="text-sm font-medium text-foreground">Technical Details</span>
        {showDetails ? (
          <ChevronUp className="h-4 w-4 text-muted-foreground" />
        ) : (
          <ChevronDown className="h-4 w-4 text-muted-foreground" />
        )}
      </button>

      {showDetails && (
        <div className="rounded-lg bg-muted/50 p-4 space-y-3 text-sm">
          <div>
            <span className="font-semibold text-foreground">Mission Type:</span>{" "}
            <span className="text-muted-foreground">{diagnostics.missionType}</span>
          </div>
          <div>
            <span className="font-semibold text-foreground">Target Displacement:</span>{" "}
            <span className="text-muted-foreground">
              {diagnostics.targetDisplacementT.toFixed(1)} tonnes
            </span>
          </div>
          {diagnostics.estimatedFroudeNumber && (
            <div>
              <span className="font-semibold text-foreground">Estimated Froude Number:</span>{" "}
              <span className="text-muted-foreground">
                {diagnostics.estimatedFroudeNumber.toFixed(3)}
              </span>
            </div>
          )}
          <div>
            <span className="font-semibold text-foreground">Total Families Considered:</span>{" "}
            <span className="text-muted-foreground">{diagnostics.totalFamiliesConsidered}</span>
          </div>
          <div>
            <span className="font-semibold text-foreground">After Fn Filtering:</span>{" "}
            <span className="text-muted-foreground">{diagnostics.familiesAfterFnFiltering}</span>
          </div>
          <div>
            <span className="font-semibold text-foreground">After Hints Filtering:</span>{" "}
            <span className="text-muted-foreground">{diagnostics.familiesAfterHintsFiltering}</span>
          </div>
          <div>
            <span className="font-semibold text-foreground">Failed Closure:</span>{" "}
            <span className="text-red-600 dark:text-red-400">
              {diagnostics.familiesFailedClosure}
            </span>
          </div>
          {diagnostics.failureReasons.length > 0 && (
            <div>
              <div className="font-semibold text-foreground mb-1">Failure Reasons:</div>
              <div className="flex flex-wrap gap-2">
                {diagnostics.failureReasons.map((reason, idx) => (
                  <span
                    key={idx}
                    className="px-2 py-1 bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400 rounded text-xs"
                  >
                    {reason}
                  </span>
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
