import { ResistanceBreakdownTable } from "../ResistanceBreakdownTable";
import type { HoltropMennenCalculationResult } from "../../../types/resistance";

interface ResistanceBreakdownPanelProps {
  result: HoltropMennenCalculationResult | null;
}

/**
 * Panel for Resistance Breakdown Sparklines Table
 */
export function ResistanceBreakdownPanel({ result }: ResistanceBreakdownPanelProps) {
  if (!result) {
    return (
      <div className="p-4 text-center text-sm text-muted-foreground">
        Run Holtrop-Mennen calculation to see resistance breakdown
      </div>
    );
  }

  return <ResistanceBreakdownTable result={result} />;
}
