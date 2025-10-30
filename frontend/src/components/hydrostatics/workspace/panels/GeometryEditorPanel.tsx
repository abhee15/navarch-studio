import type { VesselDetails } from "../../../../types/hydrostatics";

interface GeometryEditorPanelProps {
  vessel: VesselDetails | null;
  onEditClick: () => void;
}

export function GeometryEditorPanel({ vessel, onEditClick }: GeometryEditorPanelProps) {
  if (!vessel) {
    return (
      <div className="flex items-center justify-center h-full">
        <p className="text-sm text-muted-foreground">No vessel data</p>
      </div>
    );
  }

  const isComplete =
    vessel.stationsCount > 0 && vessel.waterlinesCount > 0 && vessel.offsetsCount > 0;

  return (
    <div className="h-full flex flex-col items-center justify-center space-y-4">
      {/* Summary */}
      <div className="text-center">
        <div className="inline-flex items-center space-x-2 mb-3">
          {isComplete ? (
            <svg
              className="w-8 h-8 text-green-500"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
          ) : (
            <svg
              className="w-8 h-8 text-yellow-500"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
              />
            </svg>
          )}
        </div>

        <h3 className="text-sm font-semibold text-foreground mb-2">Geometry Configuration</h3>

        <div className="space-y-1 text-xs text-muted-foreground">
          <div className="flex items-center justify-center space-x-2">
            <span>Stations:</span>
            <span className="font-medium text-foreground">{vessel.stationsCount}</span>
          </div>
          <div className="flex items-center justify-center space-x-2">
            <span>Waterlines:</span>
            <span className="font-medium text-foreground">{vessel.waterlinesCount}</span>
          </div>
          <div className="flex items-center justify-center space-x-2">
            <span>Offsets:</span>
            <span className="font-medium text-foreground">{vessel.offsetsCount}</span>
          </div>
        </div>

        <div className="mt-3">
          {isComplete ? (
            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200">
              ✓ Complete
            </span>
          ) : (
            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-yellow-100 dark:bg-yellow-900 text-yellow-800 dark:text-yellow-200">
              ⚠ Incomplete
            </span>
          )}
        </div>
      </div>

      {/* Edit Button */}
      <button
        onClick={onEditClick}
        className="inline-flex items-center px-4 py-2 border border-border rounded-md shadow-sm text-sm font-medium text-foreground bg-card hover:bg-accent transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
      >
        <svg
          className="w-4 h-4 mr-2"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
          />
        </svg>
        Edit Geometry
      </button>
    </div>
  );
}

