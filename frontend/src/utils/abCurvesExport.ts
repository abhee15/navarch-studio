import type { CandidateDesign } from "../types/sizing";
import * as XLSX from "xlsx";

export interface OffsetsGrid {
  stations: number[];
  waterlines: number[];
  offsets: number[][]; // [stationIndex][waterlineIndex]
}

function parseCandidateOffsets(candidate: CandidateDesign): OffsetsGrid {
  if (!candidate.geometryJson) {
    throw new Error("Candidate has no geometryJson");
  }
  const parsed = JSON.parse(candidate.geometryJson);
  if (
    !parsed ||
    !Array.isArray(parsed.stations) ||
    !Array.isArray(parsed.waterlines) ||
    !Array.isArray(parsed.offsets)
  ) {
    throw new Error("geometryJson missing stations/waterlines/offsets");
  }
  return {
    stations: parsed.stations as number[],
    waterlines: parsed.waterlines as number[],
    offsets: parsed.offsets as number[][],
  };
}

function trapezoidalIntegrate(x: number[], y: number[]): number {
  let area = 0;
  for (let i = 1; i < x.length; i++) {
    const dx = x[i] - x[i - 1];
    area += 0.5 * (y[i] + y[i - 1]) * dx;
  }
  return area;
}

function offsetsSheet(grid: OffsetsGrid): any {
  // Build a table: first row headers "WL (m)", then station labels (0..N-1)
  const header = ["WL (m)", ...grid.stations.map((_, i) => i.toString())];
  const rows: (string | number)[][] = [header];
  // Transpose offsets to WL-major layout
  for (let wl = 0; wl < grid.waterlines.length; wl++) {
    const row: (string | number)[] = [Number(grid.waterlines[wl].toFixed(3))];
    for (let st = 0; st < grid.stations.length; st++) {
      const hb = grid.offsets[st]?.[wl] ?? 0;
      row.push(Number(Number(hb).toFixed(4)));
    }
    rows.push(row);
  }
  return XLSX.utils.aoa_to_sheet(rows);
}

function sacSheet(grid: OffsetsGrid): any {
  // Sectional Area Curve along stations at full breadth (2 * integral of half-breadths over Z)
  const header = ["Station Index", "X (m)", "Section Area (m^2)"];
  const rows: (string | number)[][] = [header];
  for (let st = 0; st < grid.stations.length; st++) {
    const z = grid.waterlines;
    const halfB = grid.offsets[st] ?? [];
    if (z.length >= 2 && halfB.length === z.length) {
      const halfArea = trapezoidalIntegrate(z, halfB);
      const area = 2 * halfArea;
      rows.push([st, Number(grid.stations[st].toFixed(3)), Number(area.toFixed(4))]);
    } else {
      rows.push([st, Number(grid.stations[st].toFixed(3)), 0]);
    }
  }
  return XLSX.utils.aoa_to_sheet(rows);
}

function bonjeanSheet(grid: OffsetsGrid): any {
  // Bonjean: For each station, area vs draft (waterline)
  // Columns: WL (m), then one column per station with sectional area up to that WL
  const header: (string | number)[] = ["WL (m)"];
  for (let st = 0; st < grid.stations.length; st++) {
    header.push(`Area @ Station ${st} (m^2)`);
  }
  const rows: (string | number)[][] = [header];
  for (let wlIdx = 0; wlIdx < grid.waterlines.length; wlIdx++) {
    const wl = grid.waterlines[wlIdx];
    const row: (string | number)[] = [Number(wl.toFixed(3))];
    for (let st = 0; st < grid.stations.length; st++) {
      // Integrate from 0..current WL index
      const zSlice = grid.waterlines.slice(0, wlIdx + 1);
      const hbSlice = (grid.offsets[st] ?? []).slice(0, wlIdx + 1);
      let area = 0;
      if (zSlice.length >= 2 && hbSlice.length === zSlice.length) {
        area = 2 * trapezoidalIntegrate(zSlice, hbSlice);
      }
      row.push(Number(area.toFixed(4)));
    }
    rows.push(row);
  }
  return XLSX.utils.aoa_to_sheet(rows);
}

export function exportAbCurvesXlsx(candidate: CandidateDesign): void {
  const grid = parseCandidateOffsets(candidate);
  const wb: any = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, offsetsSheet(grid), "Offsets");
  XLSX.utils.book_append_sheet(wb, sacSheet(grid), "SectionAreaCurve");
  XLSX.utils.book_append_sheet(wb, bonjeanSheet(grid), "Bonjean");
  const filename = `AbCurves_${candidate.id}.xlsx`;
  XLSX.writeFile(wb, filename, { bookType: "xlsx" });
}
