import { observer } from "mobx-react-lite";
import type { HydroResult } from "../../../../types/hydrostatics";
import { settingsStore } from "../../../../stores/SettingsStore";
import { getUnitSymbol } from "../../../../utils/unitSymbols";

interface KPISummaryPanelProps {
  result: HydroResult | null;
}

export const KPISummaryPanel = observer(({ result }: KPISummaryPanelProps) => {
  const displayUnits = settingsStore.preferredUnits;
  const lengthUnit = getUnitSymbol(displayUnits, "Length");
  const massUnit = getUnitSymbol(displayUnits, "Mass");
  const areaUnit = getUnitSymbol(displayUnits, "Area");

  const formatNumber = (value: number | undefined | null, decimals: number = 2): string => {
    if (value === null || value === undefined) return "—";
    return value.toFixed(decimals);
  };

  if (!result) {
    return (
      <div className="flex items-center justify-center h-full">
        <p className="text-sm text-muted-foreground">No computation results available</p>
      </div>
    );
  }

  const kpis = [
    {
      label: "Displacement",
      value: formatNumber(result.dispWeight, 0),
      unit: massUnit,
      color: "text-blue-600 dark:text-blue-400",
      bgColor: "bg-blue-50 dark:bg-blue-900/20",
    },
    {
      label: "Draft",
      value: formatNumber(result.draft, 2),
      unit: lengthUnit,
      color: "text-indigo-600 dark:text-indigo-400",
      bgColor: "bg-indigo-50 dark:bg-indigo-900/20",
    },
    {
      label: "BMt",
      value: formatNumber(result.bMt, 2),
      unit: lengthUnit,
      color: "text-purple-600 dark:text-purple-400",
      bgColor: "bg-purple-50 dark:bg-purple-900/20",
    },
    {
      label: "GMt",
      value: formatNumber(result.gMt, 2),
      unit: lengthUnit,
      color: "text-pink-600 dark:text-pink-400",
      bgColor: "bg-pink-50 dark:bg-pink-900/20",
    },
    {
      label: "LCB",
      value: formatNumber(result.lCBx, 2),
      unit: lengthUnit,
      color: "text-orange-600 dark:text-orange-400",
      bgColor: "bg-orange-50 dark:bg-orange-900/20",
    },
    {
      label: "WPA",
      value: formatNumber(result.awp, 1),
      unit: areaUnit,
      color: "text-green-600 dark:text-green-400",
      bgColor: "bg-green-50 dark:bg-green-900/20",
    },
  ];

  return (
    <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
      {kpis.map((kpi) => (
        <div key={kpi.label} className={`${kpi.bgColor} rounded-lg p-3 border border-border/50`}>
          <div className="text-xs font-medium text-muted-foreground mb-1">{kpi.label}</div>
          <div className={`text-xl font-bold ${kpi.color} truncate`}>{kpi.value}</div>
          <div className="text-[10px] text-muted-foreground mt-0.5">{kpi.unit}</div>
        </div>
      ))}
    </div>
  );
});
