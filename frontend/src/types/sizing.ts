// Hull Sizing Module Types

export interface MissionCase {
  id: string;
  userId: string;
  tenantId: string;
  name: string;
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
  createdAt: string;
  updatedAt: string;
  deletedAt?: string;
}

export interface CreateMissionCaseDto {
  name: string;
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
}

export type UpdateMissionCaseDto = Partial<CreateMissionCaseDto>;

export interface SizingLocksDto {
  keepFn?: boolean;
  keepLOverB?: boolean;
  keepBOverT?: boolean;
  keepDOverT?: boolean;
  keepCbBand?: boolean;
}

export interface SizingOptionsDto {
  familyHints?: string[];
  maxCandidates?: number;
  minFn?: number;
  maxFn?: number;
}

export interface CreateSizingRunDto {
  missionCaseId: string;
  mode: "first_principles" | "data_driven_real" | "data_driven_ml";
  locks?: SizingLocksDto;
  options?: SizingOptionsDto;
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
  candidatesCount: number;
  createdAt: string;
}

export interface CandidateDesign {
  id: string;
  sizingRunId: string;
  userId: string;
  tenantId: string;
  hullFamily: string;

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
