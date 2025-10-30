interface ComputationStatusPanelProps {
  computationTime: number | null;
  resultsCount: number;
  error: string | null;
}

export function ComputationStatusPanel({
  computationTime,
  resultsCount,
  error,
}: ComputationStatusPanelProps) {
  return (
    <div className="h-full flex flex-col space-y-3">
      <h4 className="text-sm font-semibold text-foreground">Computation Status</h4>

      <div className="space-y-3 flex-1">
        {/* Status */}
        <div>
          <div className="text-[10px] font-medium text-muted-foreground uppercase mb-1">
            Status
          </div>
          {error ? (
            <span className="inline-flex items-center px-2.5 py-0.5 rounded text-xs font-medium bg-red-100 dark:bg-red-900 text-red-800 dark:text-red-200">
              ✗ Error
            </span>
          ) : resultsCount > 0 ? (
            <span className="inline-flex items-center px-2.5 py-0.5 rounded text-xs font-medium bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200">
              ✓ Complete
            </span>
          ) : (
            <span className="inline-flex items-center px-2.5 py-0.5 rounded text-xs font-medium bg-gray-100 dark:bg-gray-800 text-gray-800 dark:text-gray-200">
              ○ Ready
            </span>
          )}
        </div>

        {/* Computation Time */}
        {computationTime !== null && (
          <div>
            <div className="text-[10px] font-medium text-muted-foreground uppercase mb-1">
              Computation Time
            </div>
            <div className="text-sm font-medium text-foreground">
              {computationTime.toFixed(0)} ms
            </div>
          </div>
        )}

        {/* Results Count */}
        {resultsCount > 0 && (
          <div>
            <div className="text-[10px] font-medium text-muted-foreground uppercase mb-1">
              Data Points
            </div>
            <div className="text-sm font-medium text-foreground">{resultsCount}</div>
          </div>
        )}

        {/* Error Message */}
        {error && (
          <div>
            <div className="text-[10px] font-medium text-muted-foreground uppercase mb-1">
              Error
            </div>
            <div className="text-xs text-red-600 dark:text-red-400 break-words">{error}</div>
          </div>
        )}

        {/* Validation */}
        {resultsCount > 0 && !error && (
          <div>
            <div className="text-[10px] font-medium text-muted-foreground uppercase mb-1">
              Validation
            </div>
            <div className="space-y-1">
              <div className="flex items-center space-x-1.5 text-xs">
                <svg
                  className="w-3 h-3 text-green-500"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M5 13l4 4L19 7"
                  />
                </svg>
                <span className="text-foreground">Geometry valid</span>
              </div>
              <div className="flex items-center space-x-1.5 text-xs">
                <svg
                  className="w-3 h-3 text-green-500"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M5 13l4 4L19 7"
                  />
                </svg>
                <span className="text-foreground">Calculations successful</span>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

