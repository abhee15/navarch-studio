import axios from "axios";
import { API_BASE_URL } from "../config/api";

export interface StabilityPoint {
  heelAngle: number;
  gz: number;
  kn: number;
  gmAtAngle?: number;
}

export interface StabilityCurve {
  method: string;
  displacement: number;
  kg: number;
  initialGMT: number;
  draft: number;
  points: StabilityPoint[];
  maxGZ: number;
  angleAtMaxGZ: number;
  computationTimeMs: number;
  units?: string;
}

export interface StabilityCriterion {
  name: string;
  requiredValue: number;
  actualValue: number;
  unit: string;
  passed: boolean;
  notes?: string;
}

export interface StabilityCriteriaResult {
  allCriteriaPassed: boolean;
  criteria: StabilityCriterion[];
  standard: string;
  summary: string;
  units?: string;
}

export interface StabilityRequest {
  loadcaseId: string;
  minAngle: number;
  maxAngle: number;
  angleIncrement: number;
  method: "WallSided" | "FullImmersion";
  draft?: number;
}

export interface StabilityMethod {
  id: string;
  name: string;
  description: string;
  maxRecommendedAngle?: number;
  computationSpeed: string;
}

export interface StabilityCriteriaCheckResponse {
  curve: StabilityCurve;
  criteria: StabilityCriteriaResult;
}

export const stabilityApi = {
  /**
   * Generate GZ curve for a vessel
   */
  async generateGZCurve(vesselId: string, request: StabilityRequest): Promise<StabilityCurve> {
    const response = await axios.post<StabilityCurve>(
      `${API_BASE_URL}/api/v1/stability/vessels/${vesselId}/gz-curve`,
      request
    );
    return response.data;
  },

  /**
   * Check stability criteria (includes GZ curve)
   */
  async checkStabilityCriteria(
    vesselId: string,
    request: StabilityRequest
  ): Promise<StabilityCriteriaCheckResponse> {
    const response = await axios.post<StabilityCriteriaCheckResponse>(
      `${API_BASE_URL}/api/v1/stability/vessels/${vesselId}/check-criteria`,
      request
    );
    return response.data;
  },

  /**
   * Get available stability calculation methods
   */
  async getAvailableMethods(): Promise<StabilityMethod[]> {
    const response = await axios.get<StabilityMethod[]>(
      `${API_BASE_URL}/api/v1/stability/vessels/methods`
    );
    return response.data;
  },
};

