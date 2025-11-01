// Catalog Types - Reference data for hulls, propellers, and water properties

// ============================================================================
// Water Properties
// ============================================================================

export interface CatalogWaterProperty {
  id: string;
  medium: "Fresh" | "Sea";
  temperature_C: number;
  salinity_PSU: number;
  density: number; // Converted to user's preferred units
  kinematicViscosity_m2s: number; // Always SI
  sourceRef: string;
  retrievedAt: string;
  createdAt: string;
}

export interface WaterProperties {
  medium: "Fresh" | "Sea";
  temperature_C: number;
  salinity_PSU: number;
  density: number; // Converted to user's preferred units
  kinematicViscosity_m2s: number; // Always SI
  isInterpolated: boolean;
  sourceRef?: string;
  units: string; // Unit system (e.g., "SI", "Imperial")
}

// ============================================================================
// Catalog Hulls
// ============================================================================

export interface CatalogHullListItem {
  id: string;
  slug: string;
  title: string;
  description?: string;
  hullType?: "Container" | "Tanker" | "Naval" | "Template";
  lpp?: number; // Converted to user's preferred units
  beam?: number;
  draft?: number;
  cb?: number;
  geometryMissing: boolean;
  units: string; // Unit system
}

export interface CatalogHull extends CatalogHullListItem {
  canonicalRefs?: string;
  cp?: number;
  lcb?: number; // Converted to user's preferred units
  lcf?: number;
  stationsCount?: number;
  waterlinesCount?: number;
  offsetsCount?: number;
  createdAt: string;
}

export interface CloneHullRequest {
  vesselName?: string;
  userId?: string;
}

export interface CloneHullResponse {
  vesselId: string;
  vesselName: string;
  message: string;
}

export interface CatalogHullGeometry {
  stationsJson: string | null;
  waterlinesJson: string | null;
  offsetsJson: string | null;
  type: string | null;
  sourceUrl: string | null;
}

// ============================================================================
// Propeller Series
// ============================================================================

export interface CatalogPropellerSeriesListItem {
  id: string;
  name: string;
  bladeCount: number; // Z: 3-7
  expandedAreaRatio: number; // AE/A0
  pitchDiameterRatio?: number; // P/D
  sourceUrl?: string;
  license?: string;
  isDemo: boolean;
  pointsCount: number;
  createdAt: string;
}

export interface CatalogPropellerSeries extends CatalogPropellerSeriesListItem {
  points: CatalogPropellerPoint[];
}

export interface CatalogPropellerPoint {
  id: string;
  seriesId: string;
  j: number; // Advance coefficient (dimensionless)
  kt: number; // Thrust coefficient
  kq: number; // Torque coefficient
  eta0: number; // Open-water efficiency
  reynoldsNumber?: number;
}

// ============================================================================
// API Response Helpers
// ============================================================================

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ApiError {
  error: string;
  details?: string;
}
