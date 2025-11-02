import { observer } from "mobx-react-lite";
import type { ComparisonReport } from "../../../types/comparison";
import { settingsStore } from "../../../stores/SettingsStore";
import { getUnitSymbol } from "../../../utils/unitSymbols";

interface ComparisonSideBySidePanelProps {
  report: ComparisonReport;
}

export const ComparisonSideBySidePanel = observer(({ report }: ComparisonSideBySidePanelProps) => {
  const displayUnits = settingsStore.preferredUnits;
  const lengthUnit = getUnitSymbol(displayUnits, "Length");
  const massUnit = getUnitSymbol(displayUnits, "Mass");
  const areaUnit = getUnitSymbol(displayUnits, "Area");

  const formatNumber = (value: number | undefined | null, decimals: number = 2): string => {
    if (value === null || value === undefined) return "—";
    return value.toFixed(decimals);
  };

  const formatPercent = (value: number | undefined | null): string => {
    if (value === null || value === undefined) return "—";
    const sign = value > 0 ? "+" : "";
    return `${sign}${value.toFixed(1)}%`;
  };

  const getDeltaColor = (interpretation?: string): string => {
    switch (interpretation) {
      case "Better":
        return "text-green-600 dark:text-green-400";
      case "Worse":
        return "text-red-600 dark:text-red-400";
      case "Unchanged":
        return "text-muted-foreground";
      default:
        return "text-foreground";
    }
  };

  const getDeltaBadge = (interpretation?: string): string => {
    switch (interpretation) {
      case "Better":
        return "bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300";
      case "Worse":
        return "bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-300";
      case "Unchanged":
        return "bg-muted text-muted-foreground";
      default:
        return "bg-muted text-muted-foreground";
    }
  };

  const getUnitForKpi = (kpiName: string): string => {
    const lowerName = kpiName.toLowerCase();
    if (lowerName.includes("displacement") || lowerName.includes("weight")) return massUnit;
    if (
      lowerName.includes("kb") ||
      lowerName.includes("lcb") ||
      lowerName.includes("gm") ||
      lowerName.includes("bm")
    )
      return lengthUnit;
    if (lowerName.includes("wpa") || lowerName.includes("awp")) return areaUnit;
    return "";
  };

  return (
    <div className="space-y-6">
      {/* Summary Section */}
      <div className="bg-card border border-border rounded-lg p-4">
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-foreground">Run Comparison Summary</h2>
          <div className="flex items-center space-x-2 text-xs text-muted-foreground">
            <span className="font-medium">{report.baseline.runName}</span>
            <span>vs</span>
            <span className="font-medium">{report.candidate.runName}</span>
          </div>
        </div>

        {/* Run Info Cards */}
        <div className="grid grid-cols-2 gap-4 mb-6">
          {/* Baseline Info */}
          <div className="bg-blue-50 dark:bg-blue-950/20 border border-blue-200 dark:border-blue-800 rounded-lg p-3">
            <div className="flex items-center justify-between mb-2">
              <h3 className="text-sm font-medium text-blue-900 dark:text-blue-300">Baseline</h3>
              {report.baseline.isBaseline && (
                <span className="px-2 py-0.5 text-xs font-medium bg-blue-200 dark:bg-blue-800 text-blue-900 dark:text-blue-100 rounded">
                  Baseline
                </span>
              )}
            </div>
            <div className="space-y-1 text-xs text-blue-800 dark:text-blue-200">
              <div className="font-semibold">{report.baseline.runName}</div>
              <div>Loadcase: {report.baseline.loadcaseName || "Default"}</div>
              <div>Created: {new Date(report.baseline.createdAt).toLocaleDateString()}</div>
              <div>
                Drafts: {formatNumber(report.baseline.minDraft)} -{" "}
                {formatNumber(report.baseline.maxDraft)} {lengthUnit}
              </div>
            </div>
          </div>

          {/* Candidate Info */}
          <div className="bg-purple-50 dark:bg-purple-950/20 border border-purple-200 dark:border-purple-800 rounded-lg p-3">
            <div className="flex items-center justify-between mb-2">
              <h3 className="text-sm font-medium text-purple-900 dark:text-purple-300">
                Candidate
              </h3>
              {report.candidate.isBaseline && (
                <span className="px-2 py-0.5 text-xs font-medium bg-purple-200 dark:bg-purple-800 text-purple-900 dark:text-purple-100 rounded">
                  Baseline
                </span>
              )}
            </div>
            <div className="space-y-1 text-xs text-purple-800 dark:text-purple-200">
              <div className="font-semibold">{report.candidate.runName}</div>
              <div>Loadcase: {report.candidate.loadcaseName || "Default"}</div>
              <div>Created: {new Date(report.candidate.createdAt).toLocaleDateString()}</div>
              <div>
                Drafts: {formatNumber(report.candidate.minDraft)} -{" "}
                {formatNumber(report.candidate.maxDraft)} {lengthUnit}
              </div>
            </div>
          </div>
        </div>

        {/* KPI Comparison Table */}
        <div className="overflow-x-auto">
          <table className="min-w-full divide-y divide-border">
            <thead className="bg-muted">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  KPI
                </th>
                <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Baseline
                </th>
                <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Candidate
                </th>
                <th className="px-4 py-3 text-right text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Delta
                </th>
                <th className="px-4 py-3 text-center text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  % Change
                </th>
                <th className="px-4 py-3 text-center text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  Status
                </th>
              </tr>
            </thead>
            <tbody className="bg-card divide-y divide-border">
              {report.summaryComparisons.map((comparison, idx) => {
                const unit = getUnitForKpi(comparison.kpiName);
                return (
                  <tr key={idx} className="hover:bg-muted/30">
                    <td className="px-4 py-3 whitespace-nowrap text-sm font-medium text-foreground">
                      {comparison.kpiName}
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap text-sm text-right text-foreground">
                      {formatNumber(comparison.baselineValue, unit ? 2 : 3)}
                      {unit && ` ${unit}`}
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap text-sm text-right text-foreground">
                      {formatNumber(comparison.candidateValue, unit ? 2 : 3)}
                      {unit && ` ${unit}`}
                    </td>
                    <td
                      className={`px-4 py-3 whitespace-nowrap text-sm text-right font-medium ${getDeltaColor(
                        comparison.interpretation
                      )}`}
                    >
                      {formatNumber(comparison.absoluteDelta, unit ? 2 : 3)}
                      {unit && ` ${unit}`}
                    </td>
                    <td
                      className={`px-4 py-3 whitespace-nowrap text-sm text-center font-medium ${getDeltaColor(
                        comparison.interpretation
                      )}`}
                    >
                      {formatPercent(comparison.percentDelta)}
                    </td>
                    <td className="px-4 py-3 whitespace-nowrap text-center">
                      <span
                        className={`px-2 py-1 text-xs font-medium rounded ${getDeltaBadge(
                          comparison.interpretation
                        )}`}
                      >
                        {comparison.interpretation}
                      </span>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      {/* Draft-by-Draft Comparison */}
      {report.draftComparisons && report.draftComparisons.length > 0 && (
        <div className="bg-card border border-border rounded-lg p-4">
          <h3 className="text-md font-semibold text-foreground mb-4">Draft-by-Draft Comparison</h3>
          <div className="space-y-4 max-h-96 overflow-y-auto">
            {report.draftComparisons.map((draftComp, idx) => (
              <div key={idx} className="bg-muted/30 rounded p-3">
                <div className="font-medium text-sm text-foreground mb-2">
                  Draft: {formatNumber(draftComp.draft)} {lengthUnit}
                </div>
                <div className="grid grid-cols-3 gap-2 text-xs">
                  {draftComp.kpiComparisons.slice(0, 6).map((kpi, kIdx) => {
                    return (
                      <div key={kIdx} className="flex items-center justify-between">
                        <span className="text-muted-foreground">{kpi.kpiName}:</span>
                        <span className={`font-medium ${getDeltaColor(kpi.interpretation)}`}>
                          {formatPercent(kpi.percentDelta)}
                        </span>
                      </div>
                    );
                  })}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
});
