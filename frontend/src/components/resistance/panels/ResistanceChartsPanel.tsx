import type {
  Ittc57CalculationResult,
  HoltropMennenCalculationResult,
  PowerCurveResult,
  KcsBenchmarkResult,
} from "../../../types/resistance";
import { ResistanceCharts } from "../ResistanceCharts";

interface ResistanceChartsPanelProps {
  ittc57Result: Ittc57CalculationResult | null;
  hmResult: HoltropMennenCalculationResult | null;
  powerResult: PowerCurveResult | null;
  kcsBenchmarkResult: KcsBenchmarkResult | null | undefined;
}

/**
 * Panel wrapper for resistance and power charts
 */
export function ResistanceChartsPanel({
  ittc57Result,
  hmResult,
  powerResult,
  kcsBenchmarkResult,
}: ResistanceChartsPanelProps) {
  return (
    <div className="h-full">
      <ResistanceCharts
        ittc57Result={ittc57Result}
        hmResult={hmResult}
        powerResult={powerResult}
        kcsBenchmarkResult={kcsBenchmarkResult ?? null}
      />
    </div>
  );
}
