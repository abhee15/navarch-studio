import { api } from "./api";
import type {
  ComparisonSnapshot,
  CreateComparisonSnapshotDto,
  UpdateComparisonSnapshotDto,
  ComparisonReport,
  CompareRunsRequest,
} from "../types/comparison";

export const comparisonApi = {
  /**
   * Create a new comparison snapshot for a vessel
   */
  async createSnapshot(
    vesselId: string,
    dto: CreateComparisonSnapshotDto
  ): Promise<ComparisonSnapshot> {
    const response = await api.post(`/comparison/vessels/${vesselId}/snapshots`, dto);
    return response.data;
  },

  /**
   * Get all comparison snapshots for a vessel
   */
  async getSnapshots(vesselId: string): Promise<ComparisonSnapshot[]> {
    const response = await api.get(`/comparison/vessels/${vesselId}/snapshots`);
    return response.data;
  },

  /**
   * Get a single comparison snapshot by ID
   */
  async getSnapshot(snapshotId: string): Promise<ComparisonSnapshot> {
    const response = await api.get(`/comparison/snapshots/${snapshotId}`);
    return response.data;
  },

  /**
   * Update a comparison snapshot (e.g., set as baseline)
   */
  async updateSnapshot(
    snapshotId: string,
    dto: UpdateComparisonSnapshotDto
  ): Promise<ComparisonSnapshot> {
    const response = await api.patch(`/comparison/snapshots/${snapshotId}`, dto);
    return response.data;
  },

  /**
   * Delete a comparison snapshot
   */
  async deleteSnapshot(snapshotId: string): Promise<void> {
    await api.delete(`/comparison/snapshots/${snapshotId}`);
  },

  /**
   * Compare two snapshots (baseline vs candidate)
   */
  async compareSnapshots(request: CompareRunsRequest): Promise<ComparisonReport> {
    const response = await api.post("/comparison/compare", request);
    return response.data;
  },
};
