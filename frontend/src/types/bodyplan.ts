/**
 * Types for Body Plan visualization
 */

export interface BodyPlanData {
  stations: number[]; // X positions (longitudinal)
  waterlines: number[]; // Z positions (vertical)
  offsets: number[][]; // Half-breadths [stationIndex][waterlineIndex]
}

export interface StationCurve {
  stationIndex: number;
  stationX: number;
  points: { z: number; y: number }[]; // z = waterline height, y = half-breadth
  color: string;
  region: "forward" | "midship" | "aft";
}

export interface BodyPlanViewOptions {
  showMirrored: boolean;
  showGrid: boolean;
  showStationLabels: boolean;
  enableFairing: boolean;
  fairingResolution: number;
  colorScheme: "region" | "gradient" | "monochrome";
}

export interface ExportOptions {
  format: "svg" | "dxf";
  includeMetadata: boolean;
  layerPerStation: boolean;
  scaleFactor?: number;
}

export interface BodyPlanInteraction {
  hoveredStation: number | null;
  selectedStation: number | null;
  onStationHover?: (stationIndex: number | null) => void;
  onStationSelect?: (stationIndex: number | null) => void;
  onSync3DView?: (stationIndex: number) => void;
}
