interface ResistanceParametersPanelProps {
  formFactor: number | undefined;
  applyFormFactor: boolean;
  tempC: number;
  salinityPpt: number;
  onFormFactorChange: (value: number | undefined) => void;
  onApplyFormFactorChange: (value: boolean) => void;
  onTempChange: (value: number) => void;
  onSalinityChange: (value: number) => void;
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
  disabled = false,
}: ResistanceParametersPanelProps) {
  return (
    <div className="space-y-3">
      <div>
        <label className="block text-xs text-muted-foreground mb-1">Form Factor (1+k)</label>
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
