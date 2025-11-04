import { observer } from "mobx-react-lite";
import type { HydroResult } from "../../../../types/hydrostatics";
import { settingsStore } from "../../../../stores/SettingsStore";
import { getUnitSymbol } from "../../../../utils/unitSymbols";

interface HydrostaticsTablePanelProps {
  results: HydroResult[];
  highlightedDraft: number | null;
  onDraftHover?: (draft: number | null) => void;
}

export const HydrostaticsTablePanel = observer(
  ({ results, highlightedDraft, onDraftHover }: HydrostaticsTablePanelProps) => {
    const displayUnits = settingsStore.preferredUnits;
    const lengthUnit = getUnitSymbol(displayUnits, "Length");
    const massUnit = getUnitSymbol(displayUnits, "Mass");
    const areaUnit = getUnitSymbol(displayUnits, "Area");

    const formatNumber = (value: number | undefined | null, decimals: number = 2): string => {
      if (value === null || value === undefined) return "—";
      return value.toFixed(decimals);
    };

    if (results.length === 0) {
      return (
        <div className="flex items-center justify-center h-full">
          <div className="text-center">
            <svg
              className="mx-auto h-12 w-12 text-muted-foreground"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M3 10h18M3 14h18m-9-4v8m-7 0h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v8a2 2 0 002 2z"
              />
            </svg>
            <h3 className="mt-2 text-sm font-medium text-foreground">No Data</h3>
            <p className="mt-1 text-xs text-muted-foreground">
              Compute hydrostatics to see results table
            </p>
          </div>
        </div>
      );
    }

    return (
      <div className="h-full overflow-auto">
        <table className="min-w-full divide-y divide-border text-xs">
          <thead className="bg-muted sticky top-0 z-10">
            <tr>
              <th className="px-2 py-2 text-left text-[10px] font-medium text-muted-foreground uppercase tracking-wider">
                Draft ({lengthUnit})
              </th>
              <th className="px-2 py-2 text-left text-[10px] font-medium text-muted-foreground uppercase tracking-wider">
                Displ. ({massUnit})
              </th>
              <th className="px-2 py-2 text-left text-[10px] font-medium text-muted-foreground uppercase tracking-wider">
                KB ({lengthUnit})
              </th>
              <th className="px-2 py-2 text-left text-[10px] font-medium text-muted-foreground uppercase tracking-wider">
                BMt ({lengthUnit})
              </th>
              <th className="px-2 py-2 text-left text-[10px] font-medium text-muted-foreground uppercase tracking-wider">
                GMt ({lengthUnit})
              </th>
              <th className="px-2 py-2 text-left text-[10px] font-medium text-muted-foreground uppercase tracking-wider">
                LCB ({lengthUnit})
              </th>
              <th className="px-2 py-2 text-left text-[10px] font-medium text-muted-foreground uppercase tracking-wider">
                WPA ({areaUnit})
              </th>
              <th className="px-2 py-2 text-left text-[10px] font-medium text-muted-foreground uppercase tracking-wider">
                Cb
              </th>
              <th className="px-2 py-2 text-left text-[10px] font-medium text-muted-foreground uppercase tracking-wider">
                Cp
              </th>
              <th className="px-2 py-2 text-left text-[10px] font-medium text-muted-foreground uppercase tracking-wider">
                Cwp
              </th>
            </tr>
          </thead>
          <tbody className="bg-card divide-y divide-border">
            {results.map((result, idx) => (
              <tr
                key={idx}
                className={`cursor-pointer transition-colors ${
                  highlightedDraft !== null && Math.abs(highlightedDraft - result.draft) < 0.5
                    ? "bg-primary/20 dark:bg-primary/30"
                    : "hover:bg-muted/50"
                }`}
                onMouseEnter={() => onDraftHover && onDraftHover(result.draft)}
                onMouseLeave={() => onDraftHover && onDraftHover(null)}
              >
                <td className="px-2 py-2 whitespace-nowrap font-medium text-foreground">
                  {formatNumber(result.draft)}
                </td>
                <td className="px-2 py-2 whitespace-nowrap text-foreground">
                  {formatNumber(result.dispWeight, 0)}
                </td>
                <td className="px-2 py-2 whitespace-nowrap text-foreground">
                  {formatNumber(result.kBz)}
                </td>
                <td className="px-2 py-2 whitespace-nowrap text-foreground">
                  {formatNumber(result.bMt)}
                </td>
                <td className="px-2 py-2 whitespace-nowrap text-foreground">
                  {formatNumber(result.gMt)}
                </td>
                <td className="px-2 py-2 whitespace-nowrap text-foreground">
                  {formatNumber(result.lCBx)}
                </td>
                <td className="px-2 py-2 whitespace-nowrap text-foreground">
                  {formatNumber(result.awp, 1)}
                </td>
                <td className="px-2 py-2 whitespace-nowrap text-foreground">
                  {formatNumber(result.cb, 3)}
                </td>
                <td className="px-2 py-2 whitespace-nowrap text-foreground">
                  {formatNumber(result.cp, 3)}
                </td>
                <td className="px-2 py-2 whitespace-nowrap text-foreground">
                  {formatNumber(result.cwp, 3)}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    );
  }
);
