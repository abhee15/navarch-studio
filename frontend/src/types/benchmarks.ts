export interface BenchmarkCase {
  slug: string;
  title: string;
  description?: string;
}

export interface BenchmarkGeometry {
  id: string;
  type: string;
  sourceUrl?: string;
  s3Key?: string;
  scaleNote?: string;
}

export interface BenchmarkTestPoint {
  id: string;
  fr: number;
  vm?: number;
  notes?: string;
}

export interface BenchmarkMetricRef {
  id: string;
  fr?: number;
  metric: string;
  valueNum?: number;
  unit?: string;
  tolRel?: number;
  figureRef?: string;
  sourceUrl?: string;
}

export interface BenchmarkAsset {
  id: string;
  kind: string;
  s3Key: string;
  caption?: string;
  figureRef?: string;
  sourceUrl?: string;
}

export interface BenchmarkCaseDetails {
  slug: string;
  title: string;
  description?: string;
  geometries: BenchmarkGeometry[];
  testPoints: BenchmarkTestPoint[];
  metricRefs: BenchmarkMetricRef[];
  assets: BenchmarkAsset[];
}

export interface ValidateRequest {
  fr?: number;
  ct?: number;
  sinkage?: number;
  trim?: number;
  cw?: number;
}

export interface MetricResult {
  metric: string;
  passed: boolean;
  actual?: number;
  reference?: number;
  delta?: number;
  relError?: number;
}

export interface ValidationRun {
  id: string;
  fr?: number;
  metrics?: string;
  status: string;
  createdAt: string;
}
