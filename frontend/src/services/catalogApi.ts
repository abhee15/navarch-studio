import { api } from "./api";
import {
  CatalogWaterProperty,
  WaterProperties,
  CatalogHullListItem,
  CatalogHull,
  CloneHullRequest,
  CloneHullResponse,
  CatalogHullGeometry,
  CatalogPropellerSeriesListItem,
  CatalogPropellerSeries,
} from "../types/catalog";
import type { ShipDParameterMetadata, ShipDVesselTaxonomy } from "../types/sizing";

// ============================================================================
// Water Properties API
// ============================================================================

/**
 * Get all water property anchor points
 */
export const getWaterProperties = async (): Promise<CatalogWaterProperty[]> => {
  const response = await api.get("/catalog/water");
  return response.data;
};

/**
 * Get anchor points for a specific medium (Fresh or Sea)
 */
export const getWaterPropertiesByMedium = async (
  medium: "Fresh" | "Sea"
): Promise<CatalogWaterProperty[]> => {
  const response = await api.get(`/catalog/water/${medium}`);
  return response.data;
};

/**
 * Look up water properties for specific temperature and salinity with interpolation
 */
export const lookupWaterProperties = async (
  temperatureC: number,
  salinityPSU: number = 35
): Promise<WaterProperties> => {
  const response = await api.get("/catalog/water/lookup", {
    params: {
      temp: temperatureC,
      salinity: salinityPSU,
    },
  });
  return response.data;
};

// ============================================================================
// Catalog Hulls API
// ============================================================================

/**
 * List all catalog hulls with optional filters
 */
export interface CatalogHullFilters {
  vesselType?: string; // Legacy filter
  vesselCategory?: string; // ShipD taxonomy: Commercial, Government, Recreational, Research
  shipdVesselType?: string; // ShipD taxonomy: e.g., "bulk_carrier", "container"
  bowFamily?: string; // ShipD taxonomy: e.g., "bulbous_bow"
  midshipFamily?: string; // ShipD taxonomy: e.g., "full_midship"
  sternFamily?: string; // ShipD taxonomy: e.g., "transom_stern"
}

export const getCatalogHulls = async (
  filters?: CatalogHullFilters
): Promise<CatalogHullListItem[]> => {
  const params: Record<string, string> = {};

  // Legacy support: map hullType to vesselType
  if (filters?.vesselType) {
    params.vesselType = filters.vesselType;
  }

  // ShipD taxonomy filters
  if (filters?.vesselCategory) {
    params.vesselCategory = filters.vesselCategory;
  }
  if (filters?.shipdVesselType) {
    params.shipdVesselType = filters.shipdVesselType;
  }
  if (filters?.bowFamily) {
    params.bowFamily = filters.bowFamily;
  }
  if (filters?.midshipFamily) {
    params.midshipFamily = filters.midshipFamily;
  }
  if (filters?.sternFamily) {
    params.sternFamily = filters.sternFamily;
  }

  const response = await api.get("/catalog/hulls", { params });
  return response.data;
};

/**
 * Get a specific catalog hull by ID with detailed information
 */
export const getCatalogHull = async (id: string): Promise<CatalogHull> => {
  const response = await api.get(`/catalog/hulls/${id}`);
  return response.data;
};

/**
 * Clone a catalog hull to create a new user vessel with the geometry
 */
export const cloneCatalogHull = async (
  id: string,
  request: CloneHullRequest = {}
): Promise<CloneHullResponse> => {
  const response = await api.post(`/catalog/hulls/${id}/clone`, request);
  return response.data;
};

/**
 * Get geometry data for a catalog hull
 */
export const getCatalogHullGeometry = async (id: string): Promise<CatalogHullGeometry> => {
  const response = await api.get(`/catalog/hulls/${id}/geometry`);
  return response.data;
};

// ============================================================================
// Propeller Series API
// ============================================================================

/**
 * List all propeller series, optionally filtered by blade count
 */
export const getPropellerSeries = async (
  bladeCount?: number
): Promise<CatalogPropellerSeriesListItem[]> => {
  const params = bladeCount ? { bladeCount } : {};
  const response = await api.get("/catalog/propellers", { params });
  return response.data;
};

/**
 * Get a specific propeller series with all open-water points
 */
export const getPropellerSeriesDetails = async (id: string): Promise<CatalogPropellerSeries> => {
  const response = await api.get(`/catalog/propellers/${id}`);
  return response.data;
};

/**
 * Get open-water points for a series (for charting/fitting)
 */
export const getPropellerSeriesPoints = async (
  id: string
): Promise<CatalogPropellerSeries["points"]> => {
  const response = await api.get(`/catalog/propellers/${id}/points`);
  return response.data;
};

// ============================================================================
// ShipD Taxonomy API (for catalog filtering)
// ============================================================================

/**
 * Get ShipD parameter metadata
 */
export const getShipDParameterMetadata = async (): Promise<ShipDParameterMetadata[]> => {
  const response = await api.get("/shipd/parameters");
  return response.data;
};

/**
 * Get ShipD vessel taxonomy
 */
export const getShipDVesselTaxonomy = async (): Promise<ShipDVesselTaxonomy[]> => {
  const response = await api.get("/shipd/taxonomy");
  return response.data;
};
