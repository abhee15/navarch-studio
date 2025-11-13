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

  // Generate waterlines (plan view - XY plane at different Z depths)
  // Waterlines show the hull shape at different drafts (looking from above)
  const waterlineCount = 7;
  for (let wlIdx = 0; wlIdx < waterlineCount; wlIdx++) {
    // Z ranges from -draft (baseline) to 0 (waterline) for plan view
    const z = -draft + (draft * wlIdx) / (waterlineCount - 1);
    const waterlinePoints = generateWaterlinePoints(lpp, beam, draft, z, 50);

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
  const stationCount = 11;
  for (let stIdx = 0; stIdx < stationCount; stIdx++) {
    const x = -lpp / 2 + (lpp * stIdx) / (stationCount - 1);

    // At each station, find the maximum half-breadth at any waterline
    let maxHalfBreadth = 0;
    for (let wlIdx = 0; wlIdx < waterlineCount; wlIdx++) {
      const z = -draft + (draft * wlIdx) / (waterlineCount - 1);
      const xNorm = (2 * x) / lpp;
      const zNorm = z / draft;
      const halfBreadth = (beam / 2) * (1 - xNorm * xNorm) * (1 - zNorm * zNorm);
      maxHalfBreadth = Math.max(maxHalfBreadth, halfBreadth);
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
