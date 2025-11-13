// Hull Sizing Module Types

export interface MissionCase {
  id: string;
  userId: string;
  tenantId: string;
  name: string;
  missionCategory?: string;
  missionType: string;
  cargoBasis: "teu" | "weight" | "volume";
  cargoValue: number;
  teuCount?: number;
  cargoVolumeM3?: number;
  cargoDensityTPerM3?: number;
  serviceSpeedKn: number;
  seaMarginPct?: number;
  envHsM?: number;
  envTzS?: number;
  capLoaM?: number;
  capBeamM?: number;
  capDraftM?: number;
  capAirdraftM?: number;
  enduranceNm?: number;
  notes?: string;
  bowFamily?: string;
  midshipFamily?: string;
  sternFamily?: string;
  familyMaskVersion?: number;
  shipdInputsJson?: string;
  shipdInputVectorJson?: string;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string;
}

export interface CreateMissionCaseDto {
  name: string;
  missionCategory: string;
  missionType: string;
  cargoBasis: "teu" | "weight" | "volume";
  cargoValue: number;
  teuCount?: number;
  cargoVolumeM3?: number;
  cargoDensityTPerM3?: number;
  serviceSpeedKn: number;
  seaMarginPct?: number;
  envHsM?: number;
  envTzS?: number;
  capLoaM?: number;
  capBeamM?: number;
  capDraftM?: number;
  capAirdraftM?: number;
  enduranceNm?: number;
  notes?: string;
  bowFamily?: string;
  midshipFamily?: string;
  sternFamily?: string;
  familyMaskVersion?: number;
  shipdInputsJson?: string;
  shipdInputVectorJson?: string;
}

export type UpdateMissionCaseDto = Partial<CreateMissionCaseDto>;

export interface CloneMissionCaseDto {
  name: string;
}

export interface SizingLocksDto {
  keepFn?: boolean;
  keepLOverB?: boolean;
  keepBOverT?: boolean;
  keepDOverT?: boolean;
  keepCbBand?: boolean;
}

export interface ShipDAdditionalParameters {
  // Section Geometry (Image 1: Section Geometry Parameters)
  flareAngleDeg?: number; // Beta, index 8
  deadriseAngleDeg?: number; // Cdrft, index 19
  chineType?: "hard" | "soft"; // Affects Rc (index 9) and Rk (index 10)
  curvatureType?: "convex" | "concave"; // Affects Kappa_bow (index 14) and Kappa_stern (index 24)
  tumblehomeEnabled?: boolean; // bit_EP_T, index 21 - only for fine_midship

  // Longitudinal Segmentation (Image 2)
  bowLengthRatio?: number; // Lb, index 1
  midBodyLengthRatio?: number; // Lm, derived: 1 - Lb - Ls
  sternLengthRatio?: number; // Ls, index 2
  bowRakeAngleDeg?: number; // Beta, index 8 - separate from flare
  sternRakeAngleDeg?: number; // Beta_trans, index 27

  // Bulb Geometry (Image 3) - only when bulbous_bow selected
  bulbLengthRatio?: number; // Lbb, index 33
  bulbWidthRatio?: number; // Bbb, index 35
  bulbHeightRatio?: number; // Hbb, index 34
  bulbAsymmetryFactor?: number; // Lbbm, index 36
  bulbFilletRadius?: number; // Rbb, index 37
}

export interface SizingOptionsDto {
  familyHints?: string[];
  maxCandidates?: number;
  minFn?: number;
  maxFn?: number;
  additionalParameters?: ShipDAdditionalParameters | Record<string, unknown>;
}

export interface CreateSizingRunDto {
  missionCaseId: string;
  mode: "first_principles" | "data_driven_real" | "data_driven_ml";
  locks?: SizingLocksDto;
  options?: SizingOptionsDto;
  vesselCategory?: string;
  vesselType?: string;
  bowFamily?: string;
  midshipFamily?: string;
  sternFamily?: string;
  familyMaskVersion?: number;
  shipdInputVectorJson?: string;
}

export interface SizingRun {
  id: string;
  missionCaseId: string;
  userId: string;
  tenantId: string;
  mode: "first_principles" | "data_driven_real" | "data_driven_ml";
  status: "pending" | "running" | "completed" | "failed";
  computeTimeMs?: number;
  errorMessage?: string;
  vesselCategory?: string;
  vesselType?: string;
  bowFamily?: string;
  midshipFamily?: string;
  sternFamily?: string;
  familyMaskVersion?: number;
  shipdInputVectorJson?: string;
  candidatesCount: number;
  createdAt: string;
  diagnostics?: SolverDiagnostics;
}

export interface SolverDiagnostics {
  totalFamiliesConsidered: number;
  familiesAfterFnFiltering: number;
  familiesAfterHintsFiltering: number;
  familiesFailedClosure: number;
  failureReasons: string[];
  suggestions: string[];
  targetDisplacementT: number;
  estimatedFroudeNumber?: number;
  missionType?: string;
  failedFamilies: Record<string, string>;
  summary: string;
}

export interface CandidateDesign {
  id: string;
  sizingRunId: string;
  userId: string;
  tenantId: string;
  hullFamily: string;
  vesselCategory?: string;
  vesselType?: string;
  bowFamily?: string;
  midshipFamily?: string;
  sternFamily?: string;
  familyMaskVersion?: number;
  shipdParametersJson?: string;

  // Principal dimensions
  lppM: number;
  lwlM: number;
  loaM: number;
  beamM: number;
  draftM: number;
  depthM: number;

  // Form coefficients
  cb: number;
  cp: number;
  cwp: number;
  cm?: number;

  // Derived values
  dispT: number;
  fn: number;
  lwlOverLambda?: number;

  // Hydrostatics
  kbM?: number;
  lcbPctLpp?: number;
  gmEstM?: number;

  // Resistance & Powering
  ehpKw?: number;
  shpKw?: number;

  // Geometry
  geometryJson?: string;

  // Scoring
  flagsJson: string;
  score: number;
  rank: number;
  isSelected: boolean;

  createdAt: string;

  // Provenance (Data-Driven Mode)
  referenceVesselId?: string;
  referenceVesselName?: string;
  similarityScore?: number;
  solverMode?: string;
}

// Parsed flags from JSON
export interface CandidateFlags {
  beamConstrained?: boolean;
  draftConstrained?: boolean;
  loaExceeded?: boolean;
  lowGm?: boolean;
  highGm?: boolean;
  noConvergence?: boolean;
  [key: string]: boolean | undefined;
}

// For display/editing
export interface CandidateWithFlags extends CandidateDesign {
  flags: CandidateFlags;
}

// Export formats
export type ExportFormat = "json" | "csv" | "dxf" | "iges" | "step";

// UI State
export interface SizingWorkspaceState {
  selectedMissionId?: string;
  selectedRunId?: string;
  selectedCandidateId?: string;
  compareMode: boolean;
  compareCandidateIds: string[];
  viewMode: "3d" | "2d" | "table";
  locksEnabled: SizingLocksDto;
}

// Design Space Exploration Types
export interface ParameterRanges {
  lppMinM?: number;
  lppMaxM?: number;
  lppSteps: number;
  beamMinM?: number;
  beamMaxM?: number;
  beamSteps: number;
  draftMinM?: number;
  draftMaxM?: number;
  draftSteps: number;
  speedMinKn?: number;
  speedMaxKn?: number;
  speedSteps: number;
  cbMin?: number;
  cbMax?: number;
  cbSteps: number;
}

export interface DesignSpaceExplorationRequest {
  missionCaseId: string;
  ranges: ParameterRanges;
  mode?: string;
  hullFamily?: string;
  maxVariants?: number;
}

export interface DesignSpaceExplorationResponse {
  batchId: string;
  sizingRunIds: string[];
  totalVariants: number;
  status: string;
  computeTimeMs?: number;
}

export interface ObjectiveStats {
  min: number;
  max: number;
  mean: number;
  stdDev: number;
}

export interface ParetoAnalysis {
  paretoFrontIds: string[];
  objectiveStatistics: Record<string, ObjectiveStats>;
}

export interface ExplorationResultsSummary {
  batchId: string;
  totalVariants: number;
  completedVariants: number;
  failedVariants: number;
  status: string;
  candidates: CandidateDesign[];
  paretoAnalysis?: ParetoAnalysis;
}

// ShipD Metadata
export interface ShipDParameterMetadata {
  id: string;
  parameterIndex: number;
  label: string;
  group?: string | null;
  description?: string | null;
  unit?: string | null;
  min?: number | null;
  max?: number | null;
  mean?: number | null;
  stdDev?: number | null;
  metadataJson?: string | null;
}

export interface ShipDVesselTaxonomy {
  id: string;
  category: string;
  type: string;
  displayName: string;
  description?: string | null;
  bowFamilies: string[];
  midshipFamilies: string[];
  sternFamilies: string[];
  maskVersion: number;
  additionalParametersJson?: string | null;
}
