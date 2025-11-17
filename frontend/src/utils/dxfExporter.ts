/**
 * DXF Exporter for Hull Lines Plan
 *
 * Generates AutoCAD-compatible DXF files from hull geometry
 * Format: ASCII DXF R14/2000 (widely supported)
 * Uses actual geometry or FormCoefficientHullGenerator (solver logic) - NOT Wigley
 */

import type { CandidateDesign } from "../types/sizing";
import { normalizeGeometry, type OffsetsGrid } from "./geometryFormatConverter";
import { generateFormCoefficientHull, type HullDimensions } from "./formCoefficientHullGenerator";

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

  // Validate input
  if (!lpp || lpp <= 0 || !beam || beam <= 0 || !draft || draft <= 0) {
    throw new Error("Invalid hull dimensions: LPP, beam, and draft must be positive numbers");
  }

  // DXF Header
  let dxf = "";

  // Section: HEADER
  dxf += "0\nSECTION\n";
  dxf += "2\nHEADER\n";
  dxf += "9\n$ACADVER\n1\nAC1015\n"; // AutoCAD 2000
  dxf += "9\n$INSUNITS\n70\n6\n"; // Meters
  dxf += "0\nENDSEC\n";

  // Section: TABLES
  dxf += "0\nSECTION\n";
  dxf += "2\nTABLES\n";

  // Layer Table
  // Note: Using CONTINUOUS for all linetypes for maximum AutoCAD compatibility
  // CONTINUOUS is the default and doesn't need to be defined in LTYPE table
  const layers: DXFLayer[] = [
    { name: "HULL_OUTLINE", color: 1, lineType: "CONTINUOUS" },
    { name: "WATERLINES", color: 5, lineType: "CONTINUOUS" },
    { name: "STATIONS", color: 3, lineType: "CONTINUOUS" },
    { name: "CENTERLINE", color: 7, lineType: "CONTINUOUS" },
    { name: "PERPENDICULARS", color: 1, lineType: "CONTINUOUS" },
    { name: "DIMENSIONS", color: 8, lineType: "CONTINUOUS" },
  ];

  dxf += "0\nTABLE\n";
  dxf += "2\nLAYER\n";
  dxf += `70\n${layers.length}\n`; // Layer count

  layers.forEach((layer) => {
    dxf += "0\nLAYER\n";
    dxf += `2\n${layer.name}\n`;
    dxf += "70\n0\n"; // Standard layer flag
    dxf += `62\n${layer.color}\n`; // Color
    dxf += `6\n${layer.lineType}\n`; // Linetype (must match LTYPE table)
  });

  dxf += "0\nENDTAB\n";
  dxf += "0\nENDSEC\n";

  // Section: ENTITIES
  dxf += "0\nSECTION\n";
  dxf += "2\nENTITIES\n";

  // Calculate extents for proper AutoCAD display
  const minX = -lpp / 2 - 5; // Add margin
  const maxX = lpp / 2 + 5;
  const minY = -beam - 5;
  const maxY = beam + 5;

  // Get geometry (actual or generated using FormCoefficientHullGenerator)
  let geometry: OffsetsGrid | null = null;

  if (candidate.geometryJson) {
    // Try to use actual geometry
    geometry = normalizeGeometry(candidate.geometryJson);
  }

  if (!geometry) {
    // Generate using FormCoefficientHullGenerator (solver logic)
    try {
      const dims: HullDimensions = {
        length: lpp,
        beam,
        draft,
        lcbPercent: candidate.lcbPctLpp ?? 0.5,
      };

      const generated = generateFormCoefficientHull(
        dims,
        candidate.cb ?? 0.68,
        candidate.cp ?? 0.73,
        candidate.cm ?? 0.93,
        candidate.cwp ?? 0.8,
        23, // BSRA standard
        13,
        candidate.bowFamily,
        candidate.midshipFamily,
        candidate.sternFamily,
        candidate.vesselType
      );

      geometry = {
        stations: generated.stations,
        waterlines: generated.waterlines,
        offsets: generated.offsets,
      };
    } catch (error) {
      console.error("[dxfExporter] Failed to generate geometry:", error);
      throw new Error("Failed to generate hull geometry for DXF export");
    }
  }

  // Generate waterlines from geometry (plan view - XY plane at different Z depths)
  const waterlineCount = Math.min(7, geometry.waterlines.length);
  for (let wlIdx = 0; wlIdx < waterlineCount; wlIdx++) {
    const z = geometry.waterlines[wlIdx];
    const waterlinePoints = generateWaterlinePointsFromGeometry(
      geometry,
      z,
      lpp,
      50 // resolution
    );

    // Only generate if we have valid points
    if (waterlinePoints.length > 1) {
      // Port side (negative Y)
      dxf += polyline(waterlinePoints, "WATERLINES");

      // Starboard side (mirror across centerline - positive Y)
      const starboardPoints = waterlinePoints.map((p) => ({
        x: p.x,
        y: -p.y,
      }));
      dxf += polyline(starboardPoints, "WATERLINES");
    }
  }

  // Generate stations as vertical lines in plan view (XY plane)
  // Stations are vertical slices through the hull
  const stationCount = Math.min(11, geometry.stations.length);
  for (let stIdx = 0; stIdx < stationCount; stIdx++) {
    const stationX = geometry.stations[stIdx];
    const x = stationX - lpp / 2; // Convert to centered coordinates

    // At each station, find the maximum half-breadth at any waterline
    let maxHalfBreadth = 0;
    if (geometry.offsets[stIdx]) {
      for (let wlIdx = 0; wlIdx < geometry.waterlines.length; wlIdx++) {
        const halfBreadth = geometry.offsets[stIdx][wlIdx] ?? 0;
        maxHalfBreadth = Math.max(maxHalfBreadth, halfBreadth);
      }
    }

    // Draw station line (vertical line in plan view)
    if (maxHalfBreadth > 0) {
      // Port side
      dxf += line({ x, y: -maxHalfBreadth }, { x, y: maxHalfBreadth }, "STATIONS");
    }
  }

  // Centerline (longitudinal centerline)
  dxf += line({ x: minX, y: 0 }, { x: maxX, y: 0 }, "CENTERLINE");

  // Perpendiculars (AP and FP)
  dxf += line({ x: -lpp / 2, y: minY }, { x: -lpp / 2, y: maxY }, "PERPENDICULARS"); // AP
  dxf += line({ x: lpp / 2, y: minY }, { x: lpp / 2, y: maxY }, "PERPENDICULARS"); // FP

  // Dimension annotations
  const textHeight = Math.max(lpp, beam) * 0.02; // Scale text to drawing size
  dxf += text({ x: 0, y: minY + textHeight }, `Lpp = ${lpp.toFixed(2)}m`, textHeight, "DIMENSIONS");
  dxf += text(
    { x: -lpp / 2 - textHeight * 2, y: 0 },
    `B = ${beam.toFixed(2)}m`,
    textHeight,
    "DIMENSIONS",
    90
  );

  // End ENTITIES section
  dxf += "0\nENDSEC\n";

  // EOF
  dxf += "0\nEOF\n";

  return dxf;
}

/**
 * Generate waterline points from geometry (OffsetsGrid format)
 * Uses actual geometry or FormCoefficientHullGenerator output - NOT Wigley
 */
function generateWaterlinePointsFromGeometry(
  geometry: OffsetsGrid,
  targetZ: number,
  lpp: number,
  resolution: number
): Point2D[] {
  const points: Point2D[] = [];

  // Find the waterline index closest to targetZ
  let targetWaterlineIdx = 0;
  let minDiff = Math.abs(geometry.waterlines[0] - targetZ);
  for (let i = 1; i < geometry.waterlines.length; i++) {
    const diff = Math.abs(geometry.waterlines[i] - targetZ);
    if (diff < minDiff) {
      minDiff = diff;
      targetWaterlineIdx = i;
    }
  }

  // Generate points along the waterline by interpolating between stations
  for (let i = 0; i <= resolution; i++) {
    const xNorm = i / resolution; // 0 to 1
    const x = -lpp / 2 + lpp * xNorm; // Convert to centered coordinates

    // Find station indices to interpolate between
    for (let j = 0; j < geometry.stations.length - 1; j++) {
      const stationX = geometry.stations[j];
      const nextStationX = geometry.stations[j + 1];
      const stationXNorm = stationX / lpp; // Normalize to 0-1

      if (xNorm >= stationXNorm && xNorm <= nextStationX / lpp) {
        // Interpolate between stations
        const nextStationXNorm = nextStationX / lpp;
        const t = (xNorm - stationXNorm) / (nextStationXNorm - stationXNorm);

        const halfBreadth1 = geometry.offsets[j]?.[targetWaterlineIdx] ?? 0;
        const halfBreadth2 = geometry.offsets[j + 1]?.[targetWaterlineIdx] ?? 0;
        const y = halfBreadth1 + t * (halfBreadth2 - halfBreadth1);

        points.push({ x, y });
        break;
      } else if (j === geometry.stations.length - 2) {
        // Use last station
        const y = geometry.offsets[geometry.stations.length - 1]?.[targetWaterlineIdx] ?? 0;
        points.push({ x, y });
        break;
      }
    }
  }

  return points;
}

// Note: generateStationPoints function removed - stations are now generated as simple lines in plan view
// This function can be restored if body plan view export is needed in the future

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
  try {
    // Validate candidate data before generating DXF
    if (!candidate) {
      throw new Error("Candidate design is required");
    }

    console.log("Generating DXF for candidate:", {
      id: candidate.id,
      lppM: candidate.lppM,
      beamM: candidate.beamM,
      draftM: candidate.draftM,
      hullFamily: candidate.hullFamily,
    });

    const dxfContent = generateDXF(candidate);

    if (!dxfContent || dxfContent.trim().length === 0) {
      throw new Error("Generated DXF content is empty");
    }

    console.log(`DXF generated successfully: ${dxfContent.length} characters`);

    // Ensure proper line endings (CRLF for Windows compatibility)
    const dxfWithLineEndings = dxfContent.replace(/\n/g, "\r\n");
    const blob = new Blob([dxfWithLineEndings], {
      type: "application/dxf",
    });

    const url = URL.createObjectURL(blob);

    const link = document.createElement("a");
    link.href = url;
    link.download =
      filename ||
      `hull_${candidate.hullFamily || "design"}_${candidate.lppM?.toFixed(0) || "unknown"}m_${Date.now()}.dxf`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);

    console.log("DXF file download initiated:", link.download);
  } catch (error) {
    console.error("Error generating DXF file:", error);
    const errorMessage = error instanceof Error ? error.message : "Unknown error occurred";
    alert(
      `Failed to export DXF file: ${errorMessage}\n\nPlease check the browser console for details.`
    );
  }
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

  // Generate triangular facets from Wigley hull
  // This would need to mesh the Wigley surface
  // Placeholder for now

  return "STL export placeholder - requires hull meshing implementation";
}
