import type { Ittc57CalculationResult } from "../../../types/resistance";

interface Ittc57ResultsPanelProps {
  result: Ittc57CalculationResult;
}

/**
 * Panel displaying ITTC-57 friction results table
 */
export function Ittc57ResultsPanel({ result }: Ittc57ResultsPanelProps) {
  return (
    <div className="overflow-x-auto">
      <table className="min-w-full text-xs">
        <thead className="bg-muted">
          <tr>
            <th className="px-2 py-1 text-left">Speed (m/s)</th>
            <th className="px-2 py-1 text-right">Re</th>
            <th className="px-2 py-1 text-right">Fn</th>
            <th className="px-2 py-1 text-right">CF</th>
            <th className="px-2 py-1 text-right">CF_eff</th>
          </tr>
        </thead>
        <tbody>
          {result.speedGrid.map((speed, idx) => (
            <tr key={idx} className="border-t border-border hover:bg-muted/50">
              <td className="px-2 py-1">{speed.toFixed(3)}</td>
              <td className="px-2 py-1 text-right">
                {result.reynoldsNumbers[idx]?.toExponential(2)}
              </td>
              <td className="px-2 py-1 text-right">{result.froudeNumbers[idx]?.toFixed(4)}</td>
              <td className="px-2 py-1 text-right">
                {result.frictionCoefficients[idx]?.toFixed(6)}
              </td>
              <td className="px-2 py-1 text-right">
                {result.effectiveFrictionCoefficients[idx]?.toFixed(6)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
