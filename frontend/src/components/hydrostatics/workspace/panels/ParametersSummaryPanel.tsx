import { observer } from "mobx-react-lite";
import { settingsStore } from "../../../../stores/SettingsStore";
import { getUnitSymbol } from "../../../../utils/unitSymbols";

interface ParametersSummaryPanelProps {
  loadcaseName?: string;
  waterType: string;
  minDraft: number;
  maxDraft: number;
  draftStep: number;
  integrationRule: string;
  smoothing: string;
  onEditClick: () => void;
}

export const ParametersSummaryPanel = observer(
  ({
    loadcaseName,
    waterType,
    minDraft,
    maxDraft,
    draftStep,
    integrationRule,
    smoothing,
    onEditClick,
  }: ParametersSummaryPanelProps) => {
    const displayUnits = settingsStore.preferredUnits;
    const lengthUnit = getUnitSymbol(displayUnits, "Length");

    const draftCount = Math.ceil((maxDraft - minDraft) / draftStep) + 1;

    return (
      <div className="h-full flex flex-col space-y-3">
        <h4 className="text-sm font-semibold text-foreground">Current Parameters</h4>

        <div className="space-y-2 flex-1">
          {/* Loadcase */}
          <div>
            <div className="text-[10px] font-medium text-muted-foreground uppercase mb-1">
              Loadcase
            </div>
            <span className="inline-flex items-center px-2.5 py-0.5 rounded text-xs font-medium bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200">
              {loadcaseName || "Design Waterline"}
            </span>
          </div>

          {/* Water Type */}
          <div>
            <div className="text-[10px] font-medium text-muted-foreground uppercase mb-1">
              Water Type
            </div>
            <span className="inline-flex items-center px-2.5 py-0.5 rounded text-xs font-medium bg-cyan-100 dark:bg-cyan-900 text-cyan-800 dark:text-cyan-200">
              {waterType}
            </span>
          </div>

          {/* Draft Range */}
          <div>
            <div className="text-[10px] font-medium text-muted-foreground uppercase mb-1">
              Draft Range
            </div>
            <span className="inline-flex items-center px-2.5 py-0.5 rounded text-xs font-medium bg-indigo-100 dark:bg-indigo-900 text-indigo-800 dark:text-indigo-200">
              {minDraft} - {maxDraft} {lengthUnit} × {draftStep} ({draftCount} pts)
            </span>
          </div>

          {/* Integration */}
          <div>
            <div className="text-[10px] font-medium text-muted-foreground uppercase mb-1">
              Integration
            </div>
            <span className="inline-flex items-center px-2.5 py-0.5 rounded text-xs font-medium bg-purple-100 dark:bg-purple-900 text-purple-800 dark:text-purple-200">
              {integrationRule}
            </span>
          </div>

          {/* Smoothing */}
          <div>
            <div className="text-[10px] font-medium text-muted-foreground uppercase mb-1">
              Smoothing
            </div>
            <span className="inline-flex items-center px-2.5 py-0.5 rounded text-xs font-medium bg-pink-100 dark:bg-pink-900 text-pink-800 dark:text-pink-200">
              {smoothing}
            </span>
          </div>
        </div>

        {/* Edit Button */}
        <button
          onClick={onEditClick}
          className="w-full inline-flex items-center justify-center px-3 py-2 border border-border rounded-md shadow-sm text-sm font-medium text-foreground bg-card hover:bg-accent transition-colors focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
        >
          <svg className="w-4 h-4 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
            />
          </svg>
          Edit Parameters
        </button>
      </div>
    );
  }
);
