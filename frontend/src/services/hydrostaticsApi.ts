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

  async updateOffsetsGrid(vesselId: string, offsetsGrid: OffsetsGrid): Promise<OffsetsGrid> {
    const response = await api.put(`/vessels/${vesselId}/offsets`, offsetsGrid);
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

// Export API
export const exportApi = {
  async exportCsv(_vesselId: string, results: HydroResult[]): Promise<Blob> {
    // Convert results to CSV on client side for now
    const csvContent = convertToCSV(results);
    return new Blob([csvContent], { type: "text/csv" });
  },

  async exportJson(_vesselId: string, results: HydroResult[]): Promise<Blob> {
    const jsonContent = JSON.stringify(results, null, 2);
    return new Blob([jsonContent], { type: "application/json" });
  },

  async exportPdf(
    vesselId: string,
    loadcaseId: string | undefined,
    includeCurves: boolean
  ): Promise<Blob> {
    const response = await api.post(
      `/vessels/${vesselId}/export/pdf`,
      {
        loadcaseId,
        includeCurves,
      },
      {
        responseType: "blob",
      }
    );
    return response.data;
  },

  async exportExcel(
    vesselId: string,
    loadcaseId: string | undefined,
    includeCurves: boolean
  ): Promise<Blob> {
    const response = await api.post(
      `/vessels/${vesselId}/export/excel`,
      {
        loadcaseId,
        includeCurves,
      },
      {
        responseType: "blob",
      }
    );
    return response.data;
  },
};

// Helper function to convert results to CSV
function convertToCSV(results: HydroResult[]): string {
  if (results.length === 0) return "";

  const headers = [
    "Draft (m)",
    "Displacement (kg)",
    "Volume (m³)",
    "KB (m)",
    "LCB (m)",
    "TCB (m)",
    "BMt (m)",
    "BMl (m)",
    "GMt (m)",
    "GMl (m)",
    "Awp (m²)",
    "Iwp (m⁴)",
    "Cb",
    "Cp",
    "Cm",
    "Cwp",
  ];

  const csvRows = [headers.join(",")];

  for (const result of results) {
    const row = [
      result.draft?.toFixed(3) || "",
      result.dispWeight?.toFixed(0) || "",
      result.dispVolume?.toFixed(3) || "",
      result.kBz?.toFixed(3) || "",
      result.lCBx?.toFixed(3) || "",
      result.tCBy?.toFixed(3) || "",
      result.bMt?.toFixed(3) || "",
      result.bMl?.toFixed(3) || "",
      result.gMt?.toFixed(3) || "",
      result.gMl?.toFixed(3) || "",
      result.awp?.toFixed(3) || "",
      result.iwp?.toFixed(3) || "",
      result.cb?.toFixed(4) || "",
      result.cp?.toFixed(4) || "",
      result.cm?.toFixed(4) || "",
      result.cwp?.toFixed(4) || "",
    ];
    csvRows.push(row.join(","));
  }

  return csvRows.join("\n");
}

// Export combined API object
export const hydrostaticsApiClient = {
  vessels: vesselsApi,
  geometry: geometryApi,
  loadcases: loadcasesApi,
  hydrostatics: hydrostaticsApi,
  curves: curvesApi,
  export: exportApi,
};

export default hydrostaticsApiClient;
