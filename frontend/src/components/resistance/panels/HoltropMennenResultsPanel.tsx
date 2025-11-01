import type { HoltropMennenCalculationResult } from "../../../types/resistance";

interface HoltropMennenResultsPanelProps {
  result: HoltropMennenCalculationResult;
}

/**
 * Panel displaying Holtrop-Mennen resistance results table
 */
export function HoltropMennenResultsPanel({ result }: HoltropMennenResultsPanelProps) {
  return (
    <div className="overflow-x-auto">
      <table className="min-w-full text-xs">
        <thead className="bg-muted">
          <tr>
            <th className="px-2 py-1 text-left">Speed (m/s)</th>
            <th className="px-2 py-1 text-right">RT (N)</th>
            <th className="px-2 py-1 text-right">RF (N)</th>
            <th className="px-2 py-1 text-right">RR (N)</th>
            <th className="px-2 py-1 text-right">EHP (kW)</th>
          </tr>
        </thead>
        <tbody>
          {result.speedGrid.map((speed, idx) => (
            <tr key={idx} className="border-t border-border hover:bg-muted/50">
              <td className="px-2 py-1">{speed.toFixed(3)}</td>
              <td className="px-2 py-1 text-right">{result.totalResistance[idx]?.toFixed(0)}</td>
              <td className="px-2 py-1 text-right">{result.frictionResistance[idx]?.toFixed(0)}</td>
              <td className="px-2 py-1 text-right">
                {result.residuaryResistance[idx]?.toFixed(0)}
              </td>
              <td className="px-2 py-1 text-right">{result.effectivePower[idx]?.toFixed(2)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
