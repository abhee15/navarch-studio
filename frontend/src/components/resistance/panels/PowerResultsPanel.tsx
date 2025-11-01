import type { PowerCurveResult } from "../../../types/resistance";

interface PowerResultsPanelProps {
  result: PowerCurveResult;
}

/**
 * Panel displaying power curves results table
 */
export function PowerResultsPanel({ result }: PowerResultsPanelProps) {
  return (
    <div className="space-y-3">
      {/* Power Settings Summary */}
      <div className="bg-muted/30 rounded p-2 text-xs">
        <p>
          <span className="font-medium">ηD:</span> {(result.etaD ?? 0.65).toFixed(3)},{" "}
          <span className="font-medium">Service Margin:</span> {result.serviceMargin.toFixed(1)}%
        </p>
        {result.deliveredPower.length > 0 && (
          <p className="mt-1">
            <span className="font-medium">Max DHP:</span>{" "}
            {Math.max(...result.deliveredPower).toFixed(0)} kW,{" "}
            <span className="font-medium">Max P_inst:</span>{" "}
            {Math.max(...result.installedPower).toFixed(0)} kW
          </p>
        )}
      </div>

      {/* Power Curves Table */}
      <div className="overflow-x-auto">
        <table className="min-w-full text-xs">
          <thead className="bg-muted">
            <tr>
              <th className="px-2 py-1 text-left">Speed (m/s)</th>
              <th className="px-2 py-1 text-right">EHP (kW)</th>
              <th className="px-2 py-1 text-right">DHP (kW)</th>
              <th className="px-2 py-1 text-right">P_inst (kW)</th>
            </tr>
          </thead>
          <tbody>
            {result.speedGrid.map((speed, idx) => (
              <tr key={idx} className="border-t border-border hover:bg-muted/50">
                <td className="px-2 py-1">{speed.toFixed(3)}</td>
                <td className="px-2 py-1 text-right">{result.effectivePower[idx]?.toFixed(2)}</td>
                <td className="px-2 py-1 text-right">{result.deliveredPower[idx]?.toFixed(2)}</td>
                <td className="px-2 py-1 text-right">{result.installedPower[idx]?.toFixed(2)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
