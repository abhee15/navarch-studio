import { api } from "./api";
import type {
  BenchmarkCase,
  BenchmarkCaseDetails,
  ValidateRequest,
  MetricResult,
} from "../types/benchmarks";

/**
 * List all benchmark cases
 */
const listCases = async (): Promise<BenchmarkCase[]> => {
  const response = await api.get("/benchmarks/cases");
  return response.data;
};

/**
 * Get detailed information for a specific benchmark case by slug
 */
const getCase = async (slug: string): Promise<BenchmarkCaseDetails> => {
  const response = await api.get(`/benchmarks/cases/${slug}`);
  return response.data;
};

/**
 * Validate benchmark data against test points
 */
const validate = async (slug: string, data: ValidateRequest): Promise<MetricResult[]> => {
  const response = await api.post(`/benchmarks/cases/${slug}/validate`, data);
  return response.data;
};

export const benchmarksApi = {
  listCases,
  getCase,
  validate,
};

// Also export individually for convenience
export { listCases as getBenchmarkCases, getCase as getBenchmarkCase };
