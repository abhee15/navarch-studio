import { DefaultTag } from "../../common/DefaultTag";

interface ResistanceParametersPanelProps {
  formFactor: number | undefined;
  applyFormFactor: boolean;
  tempC: number;
  salinityPpt: number;
  onFormFactorChange: (value: number | undefined) => void;
  onApplyFormFactorChange: (value: boolean) => void;
  onTempChange: (value: number) => void;
  onSalinityChange: (value: number) => void;
  onUseTypicalValues?: () => void; // Callback to fetch and apply typical values
  formFactorProvenance?: string; // Provenance info for form factor
  disabled?: boolean; // For view mode
}

/**
 * Panel for resistance parameters (form factor, temperature, salinity)
 */
export function ResistanceParametersPanel({
  formFactor,
  applyFormFactor,
  tempC,
  salinityPpt,
  onFormFactorChange,
  onApplyFormFactorChange,
  onTempChange,
  onSalinityChange,
  onUseTypicalValues,
  formFactorProvenance,
  disabled = false,
}: ResistanceParametersPanelProps) {
  return (
    <div className="space-y-3">
      {/* Use Typical Values Button */}
      {onUseTypicalValues && !disabled && (
        <button
          onClick={onUseTypicalValues}
          className="w-full px-3 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm rounded-md transition-colors flex items-center justify-center gap-2"
        >
          <svg
            className="w-4 h-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
            xmlns="http://www.w3.org/2000/svg"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M13 10V3L4 14h7v7l9-11h-7z"
            />
          </svg>
          Use Typical Values
        </button>
      )}

      <div>
        <div className="flex items-center justify-between mb-1">
          <label className="block text-xs text-muted-foreground">Form Factor (1+k)</label>
          {formFactorProvenance && (
            <DefaultTag
              provenance={formFactorProvenance}
              onClear={() => onFormFactorChange(undefined)}
            />
          )}
        </div>
        <input
          type="number"
          step="0.01"
          value={formFactor ?? ""}
          onChange={(e) =>
            onFormFactorChange(e.target.value ? parseFloat(e.target.value) : undefined)
          }
          placeholder="Default: 0.20"
          disabled={disabled}
          className="w-full rounded-md border-border bg-background text-sm px-2 py-1 disabled:opacity-50"
        />
      </div>
      <label className="flex items-center cursor-pointer">
        <input
          type="checkbox"
          checked={applyFormFactor}
          onChange={(e) => onApplyFormFactorChange(e.target.checked)}
          disabled={disabled}
          className="mr-2"
        />
        <span className="text-xs">Apply Form Factor</span>
      </label>
      <div>
        <label className="block text-xs text-muted-foreground mb-1">Temperature (°C)</label>
        <input
          type="number"
          value={tempC}
          onChange={(e) => onTempChange(parseInt(e.target.value) || 15)}
          disabled={disabled}
          className="w-full rounded-md border-border bg-background text-sm px-2 py-1 disabled:opacity-50"
        />
      </div>
      <div>
        <label className="block text-xs text-muted-foreground mb-1">Salinity (ppt)</label>
        <input
          type="number"
          step="0.1"
          value={salinityPpt}
          onChange={(e) => onSalinityChange(parseFloat(e.target.value) || 35.0)}
          disabled={disabled}
          className="w-full rounded-md border-border bg-background text-sm px-2 py-1 disabled:opacity-50"
        />
      </div>
    </div>
  );
}
