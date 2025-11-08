import { api as sharedApi } from "./api";
import type {
  RaoCalculationRequestDto,
  RaoResultDto,
  SeaStateDto,
  MotionResponseDto,
  ExceedanceAnalysisDto,
} from "../types/seakeeping";

// Use shared API client (includes auth headers and interceptors)
const api = sharedApi;

export const seakeepingApi = {
  /**
   * Calculate RAOs for a vessel
   */
  async calculateRaos(vesselId: string, request: RaoCalculationRequestDto): Promise<RaoResultDto> {
    const response = await api.post(`/hydrostatics/vessels/${vesselId}/seakeeping/raos`, request);
    return response.data;
  },

  /**
   * Get RAO results by ID
   */
  async getRaos(vesselId: string, raoId: string): Promise<RaoResultDto> {
    const response = await api.get(`/hydrostatics/vessels/${vesselId}/seakeeping/raos/${raoId}`);
    return response.data;
  },

  /**
   * Analyze motion response in irregular seas
   */
  async analyzeMotion(
    vesselId: string,
    raoId: string,
    seaState: SeaStateDto
  ): Promise<MotionResponseDto> {
    const response = await api.post(
      `/hydrostatics/vessels/${vesselId}/seakeeping/raos/${raoId}/motion-response`,
      seaState
    );
    return response.data;
  },

  /**
   * Calculate exceedance probabilities
   */
  async calculateExceedance(
    vesselId: string,
    motionResponseId: string,
    thresholds: Record<string, number[]>
  ): Promise<ExceedanceAnalysisDto> {
    const response = await api.post(
      `/hydrostatics/vessels/${vesselId}/seakeeping/motion-responses/${motionResponseId}/exceedance`,
      { thresholds }
    );
    return response.data;
  },

  /**
   * Export RAO results (PDF, CSV)
   */
  async exportRaos(vesselId: string, raoId: string, format: "pdf" | "csv"): Promise<Blob> {
    const response = await api.post(
      `/hydrostatics/vessels/${vesselId}/seakeeping/raos/${raoId}/export`,
      { format },
      { responseType: "blob" }
    );
    return response.data;
  },
};
