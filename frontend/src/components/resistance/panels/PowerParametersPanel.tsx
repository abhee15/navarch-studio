interface PowerParametersPanelProps {
  serviceMargin: number;
  etaD: number | undefined;
  etaH: number | undefined;
  etaR: number | undefined;
  etaO: number | undefined;
  useDecomposedEfficiency: boolean;
  onServiceMarginChange: (value: number) => void;
  onEtaDChange: (value: number | undefined) => void;
  onEtaHChange: (value: number | undefined) => void;
  onEtaRChange: (value: number | undefined) => void;
  onEtaOChange: (value: number | undefined) => void;
  onUseDecomposedEfficiencyChange: (value: boolean) => void;
  disabled?: boolean;
}

/**
 * Panel for power calculation parameters (service margin, efficiencies)
 */
export function PowerParametersPanel({
  serviceMargin,
  etaD,
  etaH,
  etaR,
  etaO,
  useDecomposedEfficiency,
  onServiceMarginChange,
  onEtaDChange,
  onEtaHChange,
  onEtaRChange,
  onEtaOChange,
  onUseDecomposedEfficiencyChange,
  disabled = false,
}: PowerParametersPanelProps) {
  return (
    <div className="space-y-3">
      {/* Service Margin Slider */}
      <div>
        <div className="flex items-center justify-between mb-2">
          <label className="block text-xs text-muted-foreground">Service Margin</label>
          <span className="text-xs font-medium">{serviceMargin}%</span>
        </div>
        <input
          type="range"
          min="0"
          max="30"
          step="0.5"
          value={serviceMargin}
          onChange={(e) => onServiceMarginChange(parseFloat(e.target.value))}
          disabled={disabled}
          className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer dark:bg-gray-700 disabled:opacity-50"
        />
        <div className="flex justify-between text-[10px] text-muted-foreground mt-1">
          <span>0%</span>
          <span>15%</span>
          <span>30%</span>
        </div>
      </div>

      {/* Efficiency Mode Toggle */}
      <div>
        <label className="flex items-center cursor-pointer">
          <input
            type="checkbox"
            checked={useDecomposedEfficiency}
            onChange={(e) => onUseDecomposedEfficiencyChange(e.target.checked)}
            disabled={disabled}
            className="mr-2"
          />
          <span className="text-xs">Use Decomposed Efficiencies</span>
        </label>
      </div>

      {/* Efficiency Inputs */}
      {useDecomposedEfficiency ? (
        <div className="space-y-2">
          <div>
            <label className="block text-xs text-muted-foreground mb-1">ηH (Hull Efficiency)</label>
            <input
              type="number"
              step="0.01"
              min="0.9"
              max="1.05"
              value={etaH ?? ""}
              onChange={(e) =>
                onEtaHChange(e.target.value ? parseFloat(e.target.value) : undefined)
              }
              placeholder="0.98"
              disabled={disabled}
              className="w-full rounded-md border-border bg-background text-sm px-2 py-1 disabled:opacity-50"
            />
          </div>
          <div>
            <label className="block text-xs text-muted-foreground mb-1">
              ηR (Relative Rotative)
            </label>
            <input
              type="number"
              step="0.01"
              min="0.95"
              max="1.05"
              value={etaR ?? ""}
              onChange={(e) =>
                onEtaRChange(e.target.value ? parseFloat(e.target.value) : undefined)
              }
              placeholder="1.02"
              disabled={disabled}
              className="w-full rounded-md border-border bg-background text-sm px-2 py-1 disabled:opacity-50"
            />
          </div>
          <div>
            <label className="block text-xs text-muted-foreground mb-1">ηO (Open Water)</label>
            <input
              type="number"
              step="0.01"
              min="0.5"
              max="0.8"
              value={etaO ?? ""}
              onChange={(e) =>
                onEtaOChange(e.target.value ? parseFloat(e.target.value) : undefined)
              }
              placeholder="0.65"
              disabled={disabled}
              className="w-full rounded-md border-border bg-background text-sm px-2 py-1 disabled:opacity-50"
            />
          </div>
        </div>
      ) : (
        <div>
          <label className="block text-xs text-muted-foreground mb-1">
            ηD (Overall Propulsive Efficiency)
          </label>
          <input
            type="number"
            step="0.01"
            min="0.5"
            max="0.8"
            value={etaD ?? ""}
            onChange={(e) => onEtaDChange(e.target.value ? parseFloat(e.target.value) : undefined)}
            disabled={disabled}
            className="w-full rounded-md border-border bg-background text-sm px-2 py-1 disabled:opacity-50"
          />
        </div>
      )}
    </div>
  );
}
