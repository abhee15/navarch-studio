import type { OffsetsGrid } from "./hydrostatics";
import type { Loadcase } from "./hydrostatics";
import type { HydroResult } from "./hydrostatics";

/**
 * Vessel snapshot for seakeeping analysis (immutable)
 */
export interface VesselSnapshot {
  id: string;
  name: string;
  lpp: number;
  beam: number;
  draft: number;
  offsetsGrid: OffsetsGrid;
  loadcase: Loadcase;
  hydrostatics?: HydroResult;
}

/**
 * Frequency range for RAO calculation
 */
export interface FrequencyRangeDto {
  min: number; // rad/s
  max: number; // rad/s
  step: number; // rad/s
}

/**
 * RAO calculation request
 */
export interface RaoCalculationRequestDto {
  loadcaseId: string;
  frequencyRange: FrequencyRangeDto;
  motionModes: string[]; // ["heave", "pitch", "roll"]
}

/**
 * RAO calculation results
 */
export interface RaoResultDto {
  raoId: string;
  vesselId: string;
  loadcaseId: string;
  frequency: number[]; // rad/s
  heaveRao: number[]; // m/m
  pitchRao: number[]; // rad/m
  rollRao: number[]; // rad/m
  createdAt: string;
}

/**
 * Sea state parameters
 */
export interface SeaStateDto {
  significantHeight: number; // Hs (m)
  peakPeriod: number; // Tp (s)
  heading: number; // degrees (0=following, 180=head seas)
  spectrum: string; // "JONSWAP" or "PM"
  gamma?: number; // Peak enhancement factor (default 3.3)
}

/**
 * Motion response in irregular seas
 */
export interface MotionResponseDto {
  motionResponseId: string;
  raoResultId: string;
  seaState: SeaStateDto;
  significantHeave: number; // m
  significantPitch: number; // degrees
  significantRoll: number; // degrees
  meanPeriods: Record<string, number>; // { "heave": 8.2, "pitch": 8.5, "roll": 9.1 }
  exceedanceProbabilities: Record<string, number>; // { "heave2m": 0.15, ... }
  createdAt: string;
}

/**
 * Exceedance analysis results
 */
export interface ExceedanceAnalysisDto {
  motionResponseId: string;
  exceedances: Record<string, Record<number, number>>;
  // e.g., { "heave": { 1.0: 0.42, 2.0: 0.15 }, "pitch": { 3.0: 0.38 } }
}
