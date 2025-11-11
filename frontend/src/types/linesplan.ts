// Types for Lines Plan feature

/**
 * A single 3D point
 */
export interface Point3D {
  x: number;
  y: number;
  z: number;
}

/**
 * A single diagonal curve (45° line from baseline)
 */
export interface DiagonalCurve {
  diagonalIndex: number;
  angle: number; // 45° typical
  points: Point3D[];
}

/**
 * Collection of diagonal curves
 */
export interface DiagonalsData {
  diagonals: DiagonalCurve[];
}

/**
 * Section area curve showing volume distribution along hull length
 */
export interface SectionAreaCurve {
  stationPositions: number[]; // X coordinates
  sectionalAreas: number[]; // Areas at each station
  units: string; // "SI" or "Imperial"
}

/**
 * A flagged region with potential fairing issues
 */
export interface FlaggedRegion {
  startZ: number;
  endZ: number;
  maxCurvatureChange: number;
  severity: "Low" | "Medium" | "High";
}

/**
 * Fairing quality analysis for a single station
 */
export interface StationQuality {
  stationIndex: number;
  score: number; // 0-100
  qualityLevel: "Good" | "Caution" | "Issue";
  flaggedRegions: FlaggedRegion[];
}

/**
 * Overall fairing quality analysis
 */
export interface FairingQuality {
  stationQualities: StationQuality[];
  overallScore: number; // 0-100
}

/**
 * Lines plan export options for PDF/SVG
 */
export interface LinesPlanExportOptions {
  paperSize: "A0" | "A1" | "A2" | "A3" | "Letter" | "Tabloid";
  scale: "1:50" | "1:100" | "1:200" | "1:500" | "Custom";
  customScale?: number;
  orientation: "Landscape" | "Portrait";
  includeTitleBlock: boolean;
  includeGrid: boolean;
  includeOffsetsTable: boolean;
  includeSectionAreaCurve: boolean;
  includeDiagonals: boolean;
  quality: "Draft" | "Final";
  colorMode: boolean;
  watermark?: string;
}

/**
 * IGES export options
 */
export interface IgesExportOptions {
  includeStations: boolean;
  includeWaterlines: boolean;
  includeButtocks: boolean;
  includeDiagonals: boolean;
  version: "5.3";
}

/**
 * Lines plan layout dimensions
 */
export interface LinesPlanLayout {
  bodyPlanWidth: number;
  profileHeight: number;
  halfBreadthHeight: number;
  offsetsTableHeight: number;
  titleBlockHeight: number;
}

/**
 * View mode for components
 */
export type LinesPlanView = "body-plan" | "profile" | "half-breadth" | "plan";

/**
 * Visibility state for lines plan elements
 */
export interface LinesPlanVisibility {
  bodyPlan: boolean;
  waterlines: boolean;
  buttocks: boolean;
  diagonals: boolean;
  sectionAreaCurve: boolean;
  grid: boolean;
  titleBlock: boolean;
  offsetsTable: boolean;
}
