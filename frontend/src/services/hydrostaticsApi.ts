import axios from "axios";
import type {
  Vessel,
  VesselDetails,
  CreateVesselDto,
  Station,
  Waterline,
  Offset,
  OffsetsGrid,
  Loadcase,
  CreateLoadcaseDto,
  ComputeTableRequest,
  ComputeTableResponse,
  HydroResult,
  GenerateCurvesRequest,
  CurveData,
  BonjeanCurve,
} from "../types/hydrostatics";

const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5003";
const HYDRO_BASE = `${API_BASE_URL}/api/v1/hydrostatics`;

// Create axios instance with defaults
const api = axios.create({
  baseURL: HYDRO_BASE,
  headers: {
    "Content-Type": "application/json",
  },
});

// Vessels API
export const vesselsApi = {
  async list(): Promise<{ vessels: Vessel[]; total: number }> {
    const response = await api.get("/vessels");
    return response.data;
  },

  async get(id: string): Promise<VesselDetails> {
    const response = await api.get(`/vessels/${id}`);
    return response.data;
  },

  async create(vessel: CreateVesselDto): Promise<VesselDetails> {
    const response = await api.post("/vessels", vessel);
    return response.data;
  },

  async update(id: string, vessel: CreateVesselDto): Promise<VesselDetails> {
    const response = await api.put(`/vessels/${id}`, vessel);
    return response.data;
  },

  async delete(id: string): Promise<void> {
    await api.delete(`/vessels/${id}`);
  },
};

// Geometry API
export const geometryApi = {
  async importStations(vesselId: string, stations: Station[]): Promise<{ imported: number }> {
    const response = await api.post(`/vessels/${vesselId}/stations`, { stations });
    return response.data;
  },

  async importWaterlines(vesselId: string, waterlines: Waterline[]): Promise<{ imported: number }> {
    const response = await api.post(`/vessels/${vesselId}/waterlines`, { waterlines });
    return response.data;
  },

  async bulkImportOffsets(vesselId: string, offsets: Offset[]): Promise<{ imported: number }> {
    const response = await api.post(`/vessels/${vesselId}/offsets:bulk`, { offsets });
    return response.data;
  },

  async importCombinedGeometry(
    vesselId: string,
    stations: Station[],
    waterlines: Waterline[],
    offsets: Offset[]
  ): Promise<{ stations_imported: number; waterlines_imported: number; offsets_imported: number }> {
    const response = await api.post(`/vessels/${vesselId}/geometry:import`, {
      stations,
      waterlines,
      offsets,
    });
    return response.data;
  },

  async uploadCsv(
    vesselId: string,
    file: File,
    format: "combined" | "offsets_only"
  ): Promise<{ stations_imported: number; waterlines_imported: number; offsets_imported: number }> {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("format", format);

    const response = await api.post(`/vessels/${vesselId}/offsets:upload`, formData, {
      headers: {
        "Content-Type": "multipart/form-data",
      },
    });
    return response.data;
  },

  async getOffsetsGrid(vesselId: string): Promise<OffsetsGrid> {
    const response = await api.get(`/vessels/${vesselId}/offsets`);
    return response.data;
  },
};

// Loadcases API
export const loadcasesApi = {
  async list(vesselId: string): Promise<{ loadcases: Loadcase[] }> {
    const response = await api.get(`/vessels/${vesselId}/loadcases`);
    return response.data;
  },

  async get(vesselId: string, loadcaseId: string): Promise<Loadcase> {
    const response = await api.get(`/vessels/${vesselId}/loadcases/${loadcaseId}`);
    return response.data;
  },

  async create(vesselId: string, loadcase: CreateLoadcaseDto): Promise<Loadcase> {
    const response = await api.post(`/vessels/${vesselId}/loadcases`, loadcase);
    return response.data;
  },

  async update(
    vesselId: string,
    loadcaseId: string,
    loadcase: CreateLoadcaseDto
  ): Promise<Loadcase> {
    const response = await api.put(`/vessels/${vesselId}/loadcases/${loadcaseId}`, loadcase);
    return response.data;
  },

  async delete(vesselId: string, loadcaseId: string): Promise<void> {
    await api.delete(`/vessels/${vesselId}/loadcases/${loadcaseId}`);
  },
};

// Hydrostatics computations API
export const hydrostaticsApi = {
  async computeTable(
    vesselId: string,
    request: ComputeTableRequest
  ): Promise<ComputeTableResponse> {
    const response = await api.post(`/vessels/${vesselId}/compute/table`, request);
    return response.data;
  },

  async computeSingle(
    vesselId: string,
    loadcaseId: string | undefined,
    draft: number
  ): Promise<HydroResult> {
    const response = await api.post(`/vessels/${vesselId}/compute/single`, {
      loadcaseId,
      draft,
    });
    return response.data;
  },
};

// Curves API
export const curvesApi = {
  async getTypes(vesselId: string): Promise<{ curve_types: string[] }> {
    const response = await api.get(`/vessels/${vesselId}/curves/types`);
    return response.data;
  },

  async generate(
    vesselId: string,
    request: GenerateCurvesRequest
  ): Promise<{ curves: Record<string, CurveData> }> {
    const response = await api.post(`/vessels/${vesselId}/curves`, request);
    return response.data;
  },

  async getBonjean(vesselId: string): Promise<{ curves: BonjeanCurve[] }> {
    const response = await api.get(`/vessels/${vesselId}/curves/bonjean`);
    return response.data;
  },
};

// Export combined API object
export const hydrostaticsApiClient = {
  vessels: vesselsApi,
  geometry: geometryApi,
  loadcases: loadcasesApi,
  hydrostatics: hydrostaticsApi,
  curves: curvesApi,
};

export default hydrostaticsApiClient;
