import { api } from "./api";
import {
  CatalogWaterProperty,
  WaterProperties,
  CatalogHullListItem,
  CatalogHull,
  CloneHullRequest,
  CloneHullResponse,
  CatalogPropellerSeriesListItem,
  CatalogPropellerSeries,
} from "../types/catalog";

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
 * List all catalog hulls, optionally filtered by hull type
 */
export const getCatalogHulls = async (
  hullType?: "Container" | "Tanker" | "Naval" | "Template"
): Promise<CatalogHullListItem[]> => {
  const params = hullType ? { hullType } : {};
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
