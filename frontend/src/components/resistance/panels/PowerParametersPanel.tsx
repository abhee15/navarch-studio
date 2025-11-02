import { DefaultTag } from "../../common/DefaultTag";

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
  onUseTypicalValues?: () => void; // Callback to fetch and apply typical values
  etaDProvenance?: string; // Provenance for ηD
  etaHProvenance?: string; // Provenance for ηH
  etaRProvenance?: string; // Provenance for ηR
  etaOProvenance?: string; // Provenance for ηO
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
  onUseTypicalValues,
  etaDProvenance,
  etaHProvenance,
  etaRProvenance,
  etaOProvenance,
  disabled = false,
}: PowerParametersPanelProps) {
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
            <div className="flex items-center justify-between mb-1">
              <label className="block text-xs text-muted-foreground">ηH (Hull Efficiency)</label>
              {etaHProvenance && (
                <DefaultTag provenance={etaHProvenance} onClear={() => onEtaHChange(undefined)} />
              )}
            </div>
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
            <div className="flex items-center justify-between mb-1">
              <label className="block text-xs text-muted-foreground">ηR (Relative Rotative)</label>
              {etaRProvenance && (
                <DefaultTag provenance={etaRProvenance} onClear={() => onEtaRChange(undefined)} />
              )}
            </div>
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
            <div className="flex items-center justify-between mb-1">
              <label className="block text-xs text-muted-foreground">ηO (Open Water)</label>
              {etaOProvenance && (
                <DefaultTag provenance={etaOProvenance} onClear={() => onEtaOChange(undefined)} />
              )}
            </div>
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
          <div className="flex items-center justify-between mb-1">
            <label className="block text-xs text-muted-foreground">
              ηD (Overall Propulsive Efficiency)
            </label>
            {etaDProvenance && (
              <DefaultTag provenance={etaDProvenance} onClear={() => onEtaDChange(undefined)} />
            )}
          </div>
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
