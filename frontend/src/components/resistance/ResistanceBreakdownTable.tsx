import { useMemo, useState } from "react";
import { Sparkline } from "./Sparkline";
import type { HoltropMennenCalculationResult } from "../../types/resistance";
import { ArrowUpDown, ArrowUp, ArrowDown } from "lucide-react";

interface ResistanceBreakdownTableProps {
  result: HoltropMennenCalculationResult;
}

type SortColumn = "speed" | "RT" | "RF" | "RR" | "RA" | "RCA" | "RAA";
type SortDirection = "asc" | "desc" | null;

interface TableRow {
  speed: number;
  speedKnots: number;
  rt: number;
  rf: number;
  rr: number;
  ra: number;
  rca: number;
  raa: number;
  rtSparklineData: number[];
  rfSparklineData: number[];
  rrSparklineData: number[];
  raSparklineData: number[];
  rcaSparklineData: number[];
  raaSparklineData: number[];
}

/**
 * Resistance Breakdown Sparklines Table
 * Displays resistance components with sparklines for each speed
 * Features: Sortable columns, min/max highlighting
 */
export function ResistanceBreakdownTable({ result }: ResistanceBreakdownTableProps) {
  const [sortColumn, setSortColumn] = useState<SortColumn | null>(null);
  const [sortDirection, setSortDirection] = useState<SortDirection>(null);

  // Prepare table data with sparkline data for each row
  const tableData = useMemo(() => {
    const rows: TableRow[] = result.speedGrid.map((speed, idx) => ({
      speed,
      speedKnots: speed / 0.514444,
      rt: result.totalResistance[idx] / 1000, // Convert to kN
      rf: result.frictionResistance[idx] / 1000,
      rr: result.residuaryResistance[idx] / 1000,
      ra: result.appendageResistance[idx] / 1000,
      rca: result.correlationAllowance[idx] / 1000,
      raa: result.airResistance[idx] / 1000,
      // Sparkline data shows trend across all speeds
      rtSparklineData: result.totalResistance.map((v) => v / 1000),
      rfSparklineData: result.frictionResistance.map((v) => v / 1000),
      rrSparklineData: result.residuaryResistance.map((v) => v / 1000),
      raSparklineData: result.appendageResistance.map((v) => v / 1000),
      rcaSparklineData: result.correlationAllowance.map((v) => v / 1000),
      raaSparklineData: result.airResistance.map((v) => v / 1000),
    }));

    return rows;
  }, [result]);

  // Apply sorting
  const sortedData = useMemo(() => {
    if (!sortColumn || !sortDirection) return tableData;

    return [...tableData].sort((a, b) => {
      let aVal: number;
      let bVal: number;

      switch (sortColumn) {
        case "speed":
          aVal = a.speed;
          bVal = b.speed;
          break;
        case "RT":
          aVal = a.rt;
          bVal = b.rt;
          break;
        case "RF":
          aVal = a.rf;
          bVal = b.rf;
          break;
        case "RR":
          aVal = a.rr;
          bVal = b.rr;
          break;
        case "RA":
          aVal = a.ra;
          bVal = b.ra;
          break;
        case "RCA":
          aVal = a.rca;
          bVal = b.rca;
          break;
        case "RAA":
          aVal = a.raa;
          bVal = b.raa;
          break;
        default:
          return 0;
      }

      if (sortDirection === "asc") {
        return aVal - bVal;
      } else {
        return bVal - aVal;
      }
    });
  }, [tableData, sortColumn, sortDirection]);

  // Calculate min/max for each column
  const columnStats = useMemo(() => {
    if (tableData.length === 0)
      return {
        rt: { min: 0, max: 0 },
        rf: { min: 0, max: 0 },
        rr: { min: 0, max: 0 },
        ra: { min: 0, max: 0 },
        rca: { min: 0, max: 0 },
        raa: { min: 0, max: 0 },
      };

    return {
      rt: {
        min: Math.min(...tableData.map((r) => r.rt)),
        max: Math.max(...tableData.map((r) => r.rt)),
      },
      rf: {
        min: Math.min(...tableData.map((r) => r.rf)),
        max: Math.max(...tableData.map((r) => r.rf)),
      },
      rr: {
        min: Math.min(...tableData.map((r) => r.rr)),
        max: Math.max(...tableData.map((r) => r.rr)),
      },
      ra: {
        min: Math.min(...tableData.map((r) => r.ra)),
        max: Math.max(...tableData.map((r) => r.ra)),
      },
      rca: {
        min: Math.min(...tableData.map((r) => r.rca)),
        max: Math.max(...tableData.map((r) => r.rca)),
      },
      raa: {
        min: Math.min(...tableData.map((r) => r.raa)),
        max: Math.max(...tableData.map((r) => r.raa)),
      },
    };
  }, [tableData]);

  const handleSort = (column: SortColumn) => {
    if (sortColumn === column) {
      // Cycle through: asc -> desc -> null
      if (sortDirection === "asc") {
        setSortDirection("desc");
      } else if (sortDirection === "desc") {
        setSortColumn(null);
        setSortDirection(null);
      }
    } else {
      setSortColumn(column);
      setSortDirection("asc");
    }
  };

  const getSortIcon = (column: SortColumn) => {
    if (sortColumn !== column) {
      return <ArrowUpDown className="w-3 h-3 inline ml-1 opacity-50" />;
    }
    if (sortDirection === "asc") {
      return <ArrowUp className="w-3 h-3 inline ml-1 text-blue-500" />;
    }
    if (sortDirection === "desc") {
      return <ArrowDown className="w-3 h-3 inline ml-1 text-blue-500" />;
    }
    return null;
  };

  const isMinValue = (value: number, column: keyof typeof columnStats) => {
    return Math.abs(value - columnStats[column].min) < 0.001;
  };

  const isMaxValue = (value: number, column: keyof typeof columnStats) => {
    return Math.abs(value - columnStats[column].max) < 0.001;
  };

  const getValueClass = (value: number, column: keyof typeof columnStats) => {
    if (isMinValue(value, column)) {
      return "text-red-600 dark:text-red-400 font-semibold";
    }
    if (isMaxValue(value, column)) {
      return "text-green-600 dark:text-green-400 font-semibold";
    }
    return "";
  };

  return (
    <div className="overflow-x-auto">
      <table className="min-w-full text-xs border-collapse">
        <thead className="bg-muted sticky top-0 z-10">
          <tr>
            <th
              className="px-3 py-2 text-left border-b border-border cursor-pointer hover:bg-muted/80"
              onClick={() => handleSort("speed")}
            >
              Speed (kn) {getSortIcon("speed")}
            </th>
            <th
              className="px-3 py-2 text-center border-b border-border cursor-pointer hover:bg-muted/80"
              onClick={() => handleSort("RT")}
            >
              <div className="flex flex-col items-center">
                <span>RT (kN) {getSortIcon("RT")}</span>
                <span className="text-[10px] text-muted-foreground">Total</span>
              </div>
            </th>
            <th
              className="px-3 py-2 text-center border-b border-border cursor-pointer hover:bg-muted/80"
              onClick={() => handleSort("RF")}
            >
              <div className="flex flex-col items-center">
                <span>RF (kN) {getSortIcon("RF")}</span>
                <span className="text-[10px] text-muted-foreground">Friction</span>
              </div>
            </th>
            <th
              className="px-3 py-2 text-center border-b border-border cursor-pointer hover:bg-muted/80"
              onClick={() => handleSort("RR")}
            >
              <div className="flex flex-col items-center">
                <span>RR (kN) {getSortIcon("RR")}</span>
                <span className="text-[10px] text-muted-foreground">Residuary</span>
              </div>
            </th>
            <th
              className="px-3 py-2 text-center border-b border-border cursor-pointer hover:bg-muted/80"
              onClick={() => handleSort("RA")}
            >
              <div className="flex flex-col items-center">
                <span>RA (kN) {getSortIcon("RA")}</span>
                <span className="text-[10px] text-muted-foreground">Appendage</span>
              </div>
            </th>
            <th
              className="px-3 py-2 text-center border-b border-border cursor-pointer hover:bg-muted/80"
              onClick={() => handleSort("RCA")}
            >
              <div className="flex flex-col items-center">
                <span>RCA (kN) {getSortIcon("RCA")}</span>
                <span className="text-[10px] text-muted-foreground">Correlation</span>
              </div>
            </th>
            <th
              className="px-3 py-2 text-center border-b border-border cursor-pointer hover:bg-muted/80"
              onClick={() => handleSort("RAA")}
            >
              <div className="flex flex-col items-center">
                <span>RAA (kN) {getSortIcon("RAA")}</span>
                <span className="text-[10px] text-muted-foreground">Air</span>
              </div>
            </th>
          </tr>
        </thead>
        <tbody>
          {sortedData.map((row, idx) => (
            <tr key={idx} className="border-b border-border hover:bg-muted/30">
              <td className="px-3 py-2 font-medium">{row.speedKnots.toFixed(2)}</td>

              {/* RT Column */}
              <td className="px-3 py-2">
                <div className="flex flex-col items-center gap-1">
                  <span className={getValueClass(row.rt, "rt")}>{row.rt.toFixed(2)}</span>
                  <Sparkline
                    data={row.rtSparklineData}
                    color="#EF4444"
                    highlightMin={isMinValue(row.rt, "rt")}
                    highlightMax={isMaxValue(row.rt, "rt")}
                  />
                </div>
              </td>

              {/* RF Column */}
              <td className="px-3 py-2">
                <div className="flex flex-col items-center gap-1">
                  <span className={getValueClass(row.rf, "rf")}>{row.rf.toFixed(2)}</span>
                  <Sparkline
                    data={row.rfSparklineData}
                    color="#3B82F6"
                    highlightMin={isMinValue(row.rf, "rf")}
                    highlightMax={isMaxValue(row.rf, "rf")}
                  />
                </div>
              </td>

              {/* RR Column */}
              <td className="px-3 py-2">
                <div className="flex flex-col items-center gap-1">
                  <span className={getValueClass(row.rr, "rr")}>{row.rr.toFixed(2)}</span>
                  <Sparkline
                    data={row.rrSparklineData}
                    color="#10B981"
                    highlightMin={isMinValue(row.rr, "rr")}
                    highlightMax={isMaxValue(row.rr, "rr")}
                  />
                </div>
              </td>

              {/* RA Column */}
              <td className="px-3 py-2">
                <div className="flex flex-col items-center gap-1">
                  <span className={getValueClass(row.ra, "ra")}>{row.ra.toFixed(2)}</span>
                  <Sparkline
                    data={row.raSparklineData}
                    color="#F59E0B"
                    highlightMin={isMinValue(row.ra, "ra")}
                    highlightMax={isMaxValue(row.ra, "ra")}
                  />
                </div>
              </td>

              {/* RCA Column */}
              <td className="px-3 py-2">
                <div className="flex flex-col items-center gap-1">
                  <span className={getValueClass(row.rca, "rca")}>{row.rca.toFixed(2)}</span>
                  <Sparkline
                    data={row.rcaSparklineData}
                    color="#8B5CF6"
                    highlightMin={isMinValue(row.rca, "rca")}
                    highlightMax={isMaxValue(row.rca, "rca")}
                  />
                </div>
              </td>

              {/* RAA Column */}
              <td className="px-3 py-2">
                <div className="flex flex-col items-center gap-1">
                  <span className={getValueClass(row.raa, "raa")}>{row.raa.toFixed(2)}</span>
                  <Sparkline
                    data={row.raaSparklineData}
                    color="#EC4899"
                    highlightMin={isMinValue(row.raa, "raa")}
                    highlightMax={isMaxValue(row.raa, "raa")}
                  />
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      {/* Legend */}
      <div className="mt-4 px-3 py-2 bg-muted/30 rounded-lg">
        <div className="text-xs font-medium mb-2">Legend:</div>
        <div className="flex flex-wrap gap-4 text-xs">
          <div className="flex items-center gap-1">
            <span className="text-green-600 dark:text-green-400 font-semibold">●</span>
            <span>Maximum value</span>
          </div>
          <div className="flex items-center gap-1">
            <span className="text-red-600 dark:text-red-400 font-semibold">●</span>
            <span>Minimum value</span>
          </div>
          <div className="flex items-center gap-1">
            <span>📈</span>
            <span>Sparkline shows trend across all speeds</span>
          </div>
        </div>
      </div>
    </div>
  );
}
