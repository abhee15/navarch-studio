// Types for Hydrostatics module

export interface Vessel {
  id: string;
  name: string;
  description?: string;
  lpp: number; // Always in user's preferred units
  beam: number;
  designDraft: number;
  unitsSystem: string;
  createdAt: string;
  updatedAt: string;
}

export interface VesselDetails extends Vessel {
  stationsCount: number;
  waterlinesCount: number;
  offsetsCount: number;
}

export interface CreateVesselDto {
  name: string;
  description?: string;
  lpp: number; // In user's preferred units
  beam: number;
  designDraft: number;
}

export interface Station {
  stationIndex: number;
  x: number;
}

export interface Waterline {
  waterlineIndex: number;
  z: number;
}

export interface Offset {
  stationIndex: number;
  waterlineIndex: number;
  halfBreadthY: number;
}

export interface OffsetsGrid {
  stations: number[]; // Station X positions
  waterlines: number[]; // Waterline Z positions
  offsets: number[][]; // 2D grid [stationIndex][waterlineIndex]
}

export interface Loadcase {
  id: string;
  vesselId: string;
  name: string;
  rho: number;
  kg?: number;
  notes?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateLoadcaseDto {
  name: string;
  rho: number;
  kg?: number;
  notes?: string;
}

export interface HydroResult {
  draft: number;
  dispVolume: number;
  dispWeight: number;
  kBz: number;
  lCBx: number;
  tCBy: number;
  bMt: number;
  bMl: number;
  gMt?: number;
  gMl?: number;
  awp: number;
  iwp: number;
  cb: number;
  cp: number;
  cm: number;
  cwp: number;
  trimAngle?: number;
}

export interface ComputeTableRequest {
  loadcaseId?: string;
  drafts: number[];
}

export interface ComputeTableResponse {
  results: HydroResult[];
  computation_time_ms: number;
}

export interface CurvePoint {
  x: number;
  y: number;
}

export interface CurveData {
  type: string;
  xLabel: string;
  yLabel: string;
  points: CurvePoint[];
}

export interface BonjeanCurve {
  stationIndex: number;
  stationX: number;
  points: CurvePoint[];
}

export interface GenerateCurvesRequest {
  loadcaseId?: string;
  types: string[];
  minDraft: number;
  maxDraft: number;
  points?: number;
}
