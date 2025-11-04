import { api as sharedApi } from "./api";
import type {
  BenchmarkCase,
  BenchmarkCaseDetails,
  ValidateRequest,
  MetricResult,
  ValidationRun,
} from "../types/benchmarks";

// Use shared API client which includes auth headers and interceptors
const api = sharedApi;

export const benchmarksApi = {
  async listCases(): Promise<BenchmarkCase[]> {
    const response = await api.get("/benchmarks/cases");
    return response.data;
  },

  async getCase(slug: string): Promise<BenchmarkCaseDetails> {
    const response = await api.get(`/benchmarks/cases/${slug}`);
    return response.data;
  },

  async ingest(slug: string): Promise<{ message: string }> {
    const response = await api.post(`/benchmarks/cases/${slug}/ingest`);
    return response.data;
  },

  async validate(slug: string, request: ValidateRequest): Promise<MetricResult[]> {
    const response = await api.post(`/benchmarks/cases/${slug}/validate`, request);
    return response.data;
  },

  async getValidations(slug: string): Promise<ValidationRun[]> {
    const response = await api.get(`/benchmarks/cases/${slug}/validations`);
    return response.data;
  },
};
