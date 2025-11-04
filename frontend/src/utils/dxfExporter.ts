/**
 * DXF Exporter for Hull Lines Plan
 *
 * Generates AutoCAD-compatible DXF files from hull geometry
 * Format: ASCII DXF R14/2000 (widely supported)
 */

import type { CandidateDesign } from "../types/sizing";

interface Point2D {
  x: number;
  y: number;
}

interface DXFLayer {
  name: string;
  color: number;
  lineType: string;
}

/**
 * Generate DXF file from hull candidate
 * Exports Plan View with waterlines and stations
 */
export function generateDXF(candidate: CandidateDesign): string {
  const lpp = candidate.lppM;
  const beam = candidate.beamM;
  const draft = candidate.draftM;

  // DXF Header
  let dxf = "";

  // Section: HEADER
  dxf += "0\nSECTION\n";
  dxf += "2\nHEADER\n";
  dxf += "9\n$ACADVER\n1\nAC1015\n"; // AutoCAD 2000
  dxf += "9\n$INSUNITS\n70\n6\n"; // Meters
  dxf += "0\nENDSEC\n";

  // Section: TABLES (Layers)
  dxf += "0\nSECTION\n";
  dxf += "2\nTABLES\n";

  // Layer Table
  dxf += "0\nTABLE\n";
  dxf += "2\nLAYER\n";

  const layers: DXFLayer[] = [
    { name: "HULL_OUTLINE", color: 1, lineType: "CONTINUOUS" },
    { name: "WATERLINES", color: 5, lineType: "CONTINUOUS" },
    { name: "STATIONS", color: 3, lineType: "DASHED" },
    { name: "CENTERLINE", color: 7, lineType: "CENTER" },
    { name: "PERPENDICULARS", color: 1, lineType: "CONTINUOUS" },
    { name: "DIMENSIONS", color: 8, lineType: "CONTINUOUS" },
  ];

  layers.forEach((layer) => {
    dxf += "0\nLAYER\n";
    dxf += `2\n${layer.name}\n`;
    dxf += `70\n0\n`;
    dxf += `62\n${layer.color}\n`;
    dxf += `6\n${layer.lineType}\n`;
  });

  dxf += "0\nENDTAB\n";
  dxf += "0\nENDSEC\n";

  // Section: ENTITIES
  dxf += "0\nSECTION\n";
  dxf += "2\nENTITIES\n";

  // Generate waterlines (simplified Wigley form)
  const waterlineCount = 7;
  for (let wlIdx = 0; wlIdx < waterlineCount; wlIdx++) {
    const z = -draft + (draft * wlIdx) / (waterlineCount - 1);
    const waterlinePoints = generateWaterlinePoints(lpp, beam, draft, z, 50);

    // Port side
    dxf += polyline(waterlinePoints, "WATERLINES");

    // Starboard side (mirror)
    const starboardPoints = waterlinePoints.map((p) => ({ x: p.x, y: -p.y }));
    dxf += polyline(starboardPoints, "WATERLINES");
  }

  // Generate stations (11 stations: AP, 1-9, FP)
  const stationCount = 11;
  for (let stIdx = 0; stIdx < stationCount; stIdx++) {
    const x = -lpp / 2 + (lpp * stIdx) / (stationCount - 1);
    const stationPoints = generateStationPoints(lpp, beam, draft, x, 20);

    dxf += polyline(stationPoints, "STATIONS");
  }

  // Centerline
  dxf += line({ x: -lpp / 2, y: 0 }, { x: lpp / 2, y: 0 }, "CENTERLINE");

  // Perpendiculars
  dxf += line({ x: -lpp / 2, y: -beam / 2 }, { x: -lpp / 2, y: beam / 2 }, "PERPENDICULARS"); // AP
  dxf += line({ x: lpp / 2, y: -beam / 2 }, { x: lpp / 2, y: beam / 2 }, "PERPENDICULARS"); // FP

  // Dimension annotations
  dxf += text({ x: 0, y: -beam / 2 - 2 }, `Lpp = ${lpp.toFixed(2)}m`, 0.5, "DIMENSIONS");
  dxf += text({ x: -lpp / 2 - 2, y: 0 }, `B = ${beam.toFixed(2)}m`, 0.5, "DIMENSIONS", 90);

  // End ENTITIES section
  dxf += "0\nENDSEC\n";

  // EOF
  dxf += "0\nEOF\n";

  return dxf;
}

/**
 * Generate waterline points using Wigley parabolic form
 */
function generateWaterlinePoints(
  lpp: number,
  beam: number,
  draft: number,
  z: number,
  resolution: number
): Point2D[] {
  const points: Point2D[] = [];

  for (let i = 0; i <= resolution; i++) {
    const x = -lpp / 2 + (lpp * i) / resolution;

    // Wigley form: y = (B/2) * (1 - (2x/L)²) * (1 - (z/T)²)
    const xNorm = (2 * x) / lpp;
    const zNorm = z / draft;
    const y = (beam / 2) * (1 - xNorm * xNorm) * (1 - zNorm * zNorm);

    points.push({ x, y });
  }

  return points;
}

/**
 * Generate station section points (vertical slices)
 */
function generateStationPoints(
  lpp: number,
  beam: number,
  draft: number,
  x: number,
  resolution: number
): Point2D[] {
  const points: Point2D[] = [];

  for (let i = 0; i <= resolution; i++) {
    const z = -draft + (draft * i) / resolution;

    // Wigley form at this station
    const xNorm = (2 * x) / lpp;
    const zNorm = z / draft;
    const y = (beam / 2) * (1 - xNorm * xNorm) * (1 - zNorm * zNorm);

    // Body plan coordinates (z is vertical, y is horizontal half-breadth)
    points.push({ x: y, y: z });
  }

  return points;
}

/**
 * DXF POLYLINE entity
 */
function polyline(points: Point2D[], layer: string): string {
  let dxf = "0\nLWPOLYLINE\n";
  dxf += `8\n${layer}\n`;
  dxf += `90\n${points.length}\n`;
  dxf += `70\n0\n`; // Not closed

  points.forEach((p) => {
    dxf += `10\n${p.x.toFixed(6)}\n`;
    dxf += `20\n${p.y.toFixed(6)}\n`;
  });

  return dxf;
}

/**
 * DXF LINE entity
 */
function line(start: Point2D, end: Point2D, layer: string): string {
  let dxf = "0\nLINE\n";
  dxf += `8\n${layer}\n`;
  dxf += `10\n${start.x.toFixed(6)}\n`;
  dxf += `20\n${start.y.toFixed(6)}\n`;
  dxf += `11\n${end.x.toFixed(6)}\n`;
  dxf += `21\n${end.y.toFixed(6)}\n`;
  return dxf;
}

/**
 * DXF TEXT entity
 */
function text(
  position: Point2D,
  content: string,
  height: number,
  layer: string,
  rotation: number = 0
): string {
  let dxf = "0\nTEXT\n";
  dxf += `8\n${layer}\n`;
  dxf += `10\n${position.x.toFixed(6)}\n`;
  dxf += `20\n${position.y.toFixed(6)}\n`;
  dxf += `40\n${height.toFixed(6)}\n`;
  dxf += `1\n${content}\n`;
  if (rotation !== 0) {
    dxf += `50\n${rotation.toFixed(6)}\n`;
  }
  return dxf;
}

/**
 * Trigger browser download of DXF file
 */
export function downloadDXF(candidate: CandidateDesign, filename?: string): void {
  const dxfContent = generateDXF(candidate);
  const blob = new Blob([dxfContent], { type: "application/dxf" });
  const url = URL.createObjectURL(blob);

  const link = document.createElement("a");
  link.href = url;
  link.download =
    filename || `hull_${candidate.hullFamily}_${candidate.lppM.toFixed(0)}m_${Date.now()}.dxf`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  URL.revokeObjectURL(url);
}

/**
 * Generate IGES file (simplified - basic B-spline surfaces)
 * IGES is more complex - this is a placeholder
 */
export function generateIGES(_candidate: CandidateDesign): string {
  // IGES format is significantly more complex
  // For MVP, we'll focus on DXF
  // Full IGES implementation would require:
  // - NURBS surface generation
  // - Directory entry section
  // - Parameter data section
  // - Terminate section

  return `IGES export not yet implemented. Use DXF for 2D lines plan.
For 3D surface export, consider STL format instead.`;
}

/**
 * Generate STL file for 3D printing/CFD
 */
export function generateSTL(_candidate: CandidateDesign): string {
  // STL is simpler than IGES for 3D meshes
  // Binary STL format is more efficient
  // For now, ASCII STL

  let stl = "solid hull\n";

  // Generate triangular facets from Wigley hull
  // This would need to mesh the Wigley surface
  // Placeholder for now

  stl += "endsolid hull\n";

  return "STL export placeholder - requires hull meshing implementation";
}
