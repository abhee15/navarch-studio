// Speed Grid Types
export interface SpeedGrid {
  id: string;
  vesselId: string;
  name: string;
  description?: string;
  createdAt: string;
  updatedAt: string;
  speedPoints: SpeedPoint[];
}

export interface SpeedPoint {
  id: string;
  speedGridId: string;
  speed: number; // m/s
  speedKnots?: number;
  froudeNumber?: number;
  notes?: string;
  displayOrder: number;
}

export interface CreateSpeedGridRequest {
  name: string;
  description?: string;
  speedPoints: CreateSpeedPointRequest[];
}

export interface CreateSpeedPointRequest {
  speed: number; // m/s
  speedKnots?: number; // Optional convenience field
  notes?: string;
  displayOrder: number;
}

export interface UpdateSpeedPointsRequest {
  speedPoints: CreateSpeedPointRequest[];
}

// ITTC-57 Calculation Types
export interface Ittc57CalculationRequest {
  vesselId: string;
  speedGridId: string;
  formFactor?: number; // (1+k) - optional
  applyFormFactor?: boolean;
  tempC?: number;
  salinityPpt?: number;
}

export interface Ittc57CalculationResult {
  speedGrid: number[]; // m/s
  reynoldsNumbers: number[];
  froudeNumbers: number[];
  frictionCoefficients: number[]; // CF (ITTC-57)
  effectiveFrictionCoefficients: number[]; // CF_eff = (1+k)*CF
  formFactor?: number;
}

// Holtrop-Mennen Calculation Types
export interface HoltropMennenCalculationRequest {
  vesselId: string;
  speedGridId: string;
  // Geometry (optional - uses vessel defaults)
  lWL?: number; // Length at waterline (m)
  b?: number; // Beam (m)
  t?: number; // Draft (m)
  cB?: number; // Block coefficient
  cP?: number; // Prismatic coefficient
  cM?: number; // Midship coefficient
  lCB_pct?: number; // LCB as % of LWL
  // Wetted surface
  s?: number; // Wetted surface area (m²)
  // Appendages
  appendageFactor?: number;
  a_transom?: number; // Transom area (m²)
  // Air resistance
  windageArea?: number; // Windage area (m²)
  // Form factor
  k?: number; // Form factor (1+k)
  applyFormFactor?: boolean;
  // Water properties
  tempC?: number;
  salinityPpt?: number;
}

export interface HoltropMennenCalculationResult {
  speedGrid: number[]; // m/s
  reynoldsNumbers: number[];
  froudeNumbers: number[];
  frictionCoefficients: number[];
  effectiveFrictionCoefficients: number[];
  frictionResistance: number[]; // RF (N)
  residuaryResistance: number[]; // RR (N)
  appendageResistance: number[]; // RA (N)
  correlationAllowance: number[]; // RCA (N)
  airResistance: number[]; // RAA (N)
  totalResistance: number[]; // RT (N)
  effectivePower: number[]; // EHP (kW)
}

// Power Calculation Types
export interface PowerCurveRequest {
  effectivePower: number[]; // EHP (kW)
  speedGrid: number[]; // m/s
  etaD?: number; // Overall propulsive efficiency (0.6-0.7 typical)
  etaH?: number; // Hull efficiency (~0.98-1.02)
  etaR?: number; // Relative rotative efficiency (~1.0-1.05)
  etaO?: number; // Open water efficiency (~0.5-0.7)
  serviceMargin: number; // Percentage (0-30%)
}

export interface PowerCurveResult {
  speedGrid: number[]; // m/s
  effectivePower: number[]; // EHP (kW)
  deliveredPower: number[]; // DHP (kW)
  installedPower: number[]; // P_inst (kW)
  serviceMargin: number; // Percentage used
  etaD?: number; // Overall efficiency used
}

// KCS Benchmark Types
export interface KcsReferencePoint {
  speed: number; // m/s
  rtReference: number; // N (reference total resistance)
  source?: string; // Optional source annotation
}

export interface KcsBenchmarkRequest {
  vesselId: string;
  speedGridId: string;
  // KCS vessel particulars
  lWL: number;
  b: number;
  t: number;
  cB?: number;
  cP?: number;
  cM?: number;
  lCB_pct?: number;
  s?: number;
  // Water properties
  tempC?: number;
  salinityPpt?: number;
  // Reference data
  referenceData: KcsReferencePoint[];
  // Tolerances
  maeTolerancePercent?: number; // Default 3%
  maxTolerancePercent?: number; // Default 5%
}

export interface KcsBenchmarkResult {
  speedGrid: number[]; // m/s
  calculatedResistance: number[]; // RT_calc (N)
  referenceResistance: number[]; // RT_ref (N)
  errorPercent: number[]; // Δ% at each speed
  meanAbsoluteError: number; // MAE (%)
  maxError: number; // Max error (%)
  pass: boolean; // True if MAE≤tolerance and Max≤tolerance
  maeTolerance: number;
  maxTolerance: number;
  // Calculation details
  calculationDetails?: HoltropMennenCalculationResult;
}

// Speed-Draft Matrix Types
export interface DesignPoint {
  name: string;
  speed: number; // m/s
  draft: number; // m
  expectedPower?: number; // kW (optional)
}

export interface TrialPoint {
  name: string;
  speed: number; // m/s
  draft: number; // m
  measuredPower: number; // kW
  trialDate?: string;
}

export interface SpeedDraftMatrixRequest {
  vesselId: string;
  // Speed range
  minSpeed: number; // m/s
  maxSpeed: number; // m/s
  speedSteps?: number; // default 20
  // Draft range
  minDraft: number; // m
  maxDraft: number; // m
  draftSteps?: number; // default 15
  // Calculation parameters (optional)
  lWL?: number;
  b?: number;
  cB?: number;
  cP?: number;
  cM?: number;
  lCB_pct?: number;
  s?: number;
  appendageFactor?: number;
  a_transom?: number;
  windageArea?: number;
  k?: number;
  applyFormFactor?: boolean;
  // Water properties
  tempC?: number;
  salinityPpt?: number;
  // Design and trial points (optional)
  designPoints?: DesignPoint[];
  trialPoints?: TrialPoint[];
}

export interface MatrixPointDetails {
  speedIndex: number;
  draftIndex: number;
  speed: number; // m/s
  draft: number; // m
  // Resistance components
  frictionResistance: number; // RF (N)
  residuaryResistance: number; // RR (N)
  appendageResistance: number; // RA (N)
  correlationAllowance: number; // RCA (N)
  airResistance: number; // RAA (N)
  totalResistance: number; // RT (N)
  // Power
  effectivePower: number; // EHP (kW)
  // Non-dimensional parameters
  reynoldsNumber: number;
  froudeNumber: number;
  frictionCoefficient: number;
  // Form coefficients at this draft
  cB: number;
  cP: number;
  cM: number;
}

export interface SpeedDraftMatrixResult {
  speedGrid: number[]; // m/s
  draftGrid: number[]; // m
  // Matrix data: [draftIndex][speedIndex] = value
  powerMatrix: number[][]; // Effective Power (kW)
  resistanceMatrix: number[][]; // Total Resistance (N)
  froudeNumberMatrix: number[][];
  // Detailed breakdown available for each point
  pointDetails: MatrixPointDetails[];
  // Design and trial points echoed back
  designPoints?: DesignPoint[];
  trialPoints?: TrialPoint[];
  // Calculation metadata
  totalPoints: number;
  calculationMethod: string;
}
