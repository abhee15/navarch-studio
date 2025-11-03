import { api } from "./api";
import type {
  MissionCase,
  CreateMissionCaseDto,
  UpdateMissionCaseDto,
  CreateSizingRunDto,
  SizingRun,
  CandidateDesign,
  ExportFormat,
} from "../types/sizing";

const BASE_PATH = "/api/v1/hull-sizing";

// Mission Cases
export const createMissionCase = async (dto: CreateMissionCaseDto): Promise<MissionCase> => {
  const response = await api.post<MissionCase>(`${BASE_PATH}/mission-cases`, dto);
  return response.data;
};

export const getMissionCases = async (): Promise<MissionCase[]> => {
  const response = await api.get<MissionCase[]>(`${BASE_PATH}/mission-cases`);
  return response.data;
};

export const getMissionCase = async (id: string): Promise<MissionCase> => {
  const response = await api.get<MissionCase>(`${BASE_PATH}/mission-cases/${id}`);
  return response.data;
};

export const updateMissionCase = async (
  id: string,
  dto: UpdateMissionCaseDto
): Promise<MissionCase> => {
  const response = await api.put<MissionCase>(`${BASE_PATH}/mission-cases/${id}`, dto);
  return response.data;
};

export const deleteMissionCase = async (id: string): Promise<void> => {
  await api.delete(`${BASE_PATH}/mission-cases/${id}`);
};

// Sizing Runs
export const createSizingRun = async (dto: CreateSizingRunDto): Promise<SizingRun> => {
  const response = await api.post<SizingRun>(`${BASE_PATH}/runs`, dto);
  return response.data;
};

export const getSizingRun = async (id: string): Promise<SizingRun> => {
  const response = await api.get<SizingRun>(`${BASE_PATH}/runs/${id}`);
  return response.data;
};

export const getRunCandidates = async (runId: string): Promise<CandidateDesign[]> => {
  const response = await api.get<CandidateDesign[]>(`${BASE_PATH}/runs/${runId}/candidates`);
  return response.data;
};

// Candidate Designs
export const getCandidate = async (id: string): Promise<CandidateDesign> => {
  const response = await api.get<CandidateDesign>(`${BASE_PATH}/candidates/${id}`);
  return response.data;
};

export const updateCandidate = async (
  id: string,
  updates: Partial<CandidateDesign>
): Promise<CandidateDesign> => {
  const response = await api.put<CandidateDesign>(`${BASE_PATH}/candidates/${id}`, updates);
  return response.data;
};

export const deleteCandidate = async (id: string): Promise<void> => {
  await api.delete(`${BASE_PATH}/candidates/${id}`);
};

export const exportCandidate = async (id: string, format: ExportFormat): Promise<Blob> => {
  const response = await api.post(
    `${BASE_PATH}/candidates/${id}/export/${format}`,
    {},
    { responseType: "blob" }
  );
  return response.data;
};

export const pushToHydrostatics = async (candidateId: string): Promise<{ vesselId: string }> => {
  const response = await api.post<{ vesselId: string }>(
    `${BASE_PATH}/candidates/${candidateId}/push-to-hydrostatics`
  );
  return response.data;
};

export const pushToResistance = async (candidateId: string): Promise<{ calculationId: string }> => {
  const response = await api.post<{ calculationId: string }>(
    `${BASE_PATH}/candidates/${candidateId}/push-to-resistance`
  );
  return response.data;
};
