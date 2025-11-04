import { api } from "../services/api";

const API_URL = import.meta.env.VITE_API_URL || "http://localhost:5001";

export interface DefaultValuesRequest {
  vesselType?: string;
  cb?: number;
  lb_Ratio?: number;
  bt_Ratio?: number;
  lpp?: number;
  beam?: number;
  draft?: number;
}

export interface DefaultValueItem {
  value: number;
  provenance: string;
  range?: string;
}

export interface DefaultValuesResponse {
  formFactor?: DefaultValueItem;
  appendageAreaPercent?: DefaultValueItem;
  roughnessAllowance?: DefaultValueItem;
  etaD?: DefaultValueItem;
  etaH?: DefaultValueItem;
  etaR?: DefaultValueItem;
  etaO?: DefaultValueItem;
  wettedSurfaceArea?: DefaultValueItem;
  cm?: DefaultValueItem;
  provenance: string;
}

/**
 * API client for default values service
 */
export const defaultValuesApi = {
  /**
   * Gets typical/default values for resistance and powering parameters
   */
  getTypicalValues: async (request: DefaultValuesRequest): Promise<DefaultValuesResponse> => {
    const response = await api.post<DefaultValuesResponse>(
      `${API_URL}/api/v1/defaultvalues/typical`,
      request
    );
    return response.data;
  },

  /**
   * Gets available vessel type categories
   */
  getVesselTypes: async (): Promise<string[]> => {
    const response = await api.get<string[]>(`${API_URL}/api/v1/defaultvalues/vessel-types`);
    return response.data;
  },
};
