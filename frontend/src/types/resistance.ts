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
