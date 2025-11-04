import type { HydroResult } from "./hydrostatics";

export interface ComparisonSnapshot {
  id: string;
  vesselId: string;
  vesselName: string;
  loadcaseId?: string;
  loadcaseName?: string;
  runName: string;
  description?: string;
  isBaseline: boolean;
  tags?: string;
  vesselLpp: number;
  vesselBeam: number;
  vesselDesignDraft: number;
  loadcaseRho?: number;
  loadcaseKG?: number;
  minDraft: number;
  maxDraft: number;
  draftStep: number;
  results: HydroResult[];
  computationTimeMs: number;
  createdAt: string;
}

export interface CreateComparisonSnapshotDto {
  runName: string;
  description?: string;
  isBaseline?: boolean;
  tags?: string;
  loadcaseId?: string;
  minDraft: number;
  maxDraft: number;
  draftStep: number;
  results: HydroResult[];
  computationTimeMs: number;
}

export interface UpdateComparisonSnapshotDto {
  runName?: string;
  description?: string;
  isBaseline?: boolean;
  tags?: string;
}

export interface KpiComparison {
  kpiName: string;
  unit: string;
  baselineValue?: number;
  candidateValue?: number;
  absoluteDelta?: number;
  percentDelta?: number;
  interpretation?: "Better" | "Worse" | "Neutral" | "Unchanged";
}

export interface DraftComparison {
  draft: number;
  kpiComparisons: KpiComparison[];
}

export interface ComparisonReport {
  baseline: ComparisonSnapshot;
  candidate: ComparisonSnapshot;
  summaryComparisons: KpiComparison[];
  draftComparisons: DraftComparison[];
}

export interface CompareRunsRequest {
  baselineSnapshotId: string;
  candidateSnapshotId: string;
}
