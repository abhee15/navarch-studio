import { api as sharedApi } from "./api";
import type {
  SpeedGrid,
  SpeedPoint,
  CreateSpeedGridRequest,
  UpdateSpeedPointsRequest,
  Ittc57CalculationRequest,
  Ittc57CalculationResult,
  HoltropMennenCalculationRequest,
  HoltropMennenCalculationResult,
} from "../types/resistance";

// Use shared API client which includes auth headers and interceptors
// Routes to API Gateway which proxies to Data Service
const api = sharedApi;

// Speed Grid API
export const speedGridsApi = {
  async list(vesselId: string): Promise<{ speedGrids: SpeedGrid[] }> {
    const response = await api.get(`/resistance/vessels/${vesselId}/speed-grids`);
    return response.data;
  },

  async get(vesselId: string, gridId: string): Promise<SpeedGrid> {
    const response = await api.get(`/resistance/vessels/${vesselId}/speed-grids/${gridId}`);
    return response.data;
  },

  async create(vesselId: string, grid: CreateSpeedGridRequest): Promise<SpeedGrid> {
    const response = await api.post(`/resistance/vessels/${vesselId}/speed-grids`, grid);
    return response.data;
  },

  async update(vesselId: string, gridId: string, grid: CreateSpeedGridRequest): Promise<SpeedGrid> {
    const response = await api.put(`/resistance/vessels/${vesselId}/speed-grids/${gridId}`, grid);
    return response.data;
  },

  async updatePoints(
    vesselId: string,
    gridId: string,
    points: UpdateSpeedPointsRequest
  ): Promise<SpeedGrid> {
    const response = await api.post(
      `/resistance/vessels/${vesselId}/speed-grids/${gridId}/points`,
      points
    );
    return response.data;
  },

  async delete(vesselId: string, gridId: string): Promise<void> {
    await api.delete(`/resistance/vessels/${vesselId}/speed-grids/${gridId}`);
  },
};

// Resistance Calculation API
export const resistanceCalculationsApi = {
  async calculateIttc57(
    request: Ittc57CalculationRequest
  ): Promise<Ittc57CalculationResult> {
    const response = await api.post("/resistance/ittc57", request);
    return response.data;
  },

  async calculateHoltropMennen(
    request: HoltropMennenCalculationRequest
  ): Promise<HoltropMennenCalculationResult> {
    const response = await api.post("/resistance/holtrop-mennen", request);
    return response.data;
  },
};

// Export combined API object
export const resistanceApiClient = {
  speedGrids: speedGridsApi,
  calculations: resistanceCalculationsApi,
};

export default resistanceApiClient;

