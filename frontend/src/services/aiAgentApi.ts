import axios from "axios";
import {
  NLMissionRequest,
  MissionSuggestionResponse,
  RefineMissionRequest,
  UsageStats,
} from "../types/ai";
import { MissionCase } from "../types/sizing";

const API_BASE_URL = import.meta.env.VITE_API_URL || "http://localhost:5002";

const aiAgentClient = axios.create({
  baseURL: `${API_BASE_URL}/api/v1/ai-agent`,
  headers: {
    "Content-Type": "application/json",
  },
});

// Add auth token to requests
aiAgentClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export const aiAgentApi = {
  /**
   * Convert natural language to mission parameters
   */
  async suggestMission(
    naturalLanguage: string,
    preferredUnits: string = "SI"
  ): Promise<MissionSuggestionResponse> {
    const request: NLMissionRequest = {
      naturalLanguage,
      preferredUnits,
    };

    const response = await aiAgentClient.post<MissionSuggestionResponse>(
      "/suggest/mission",
      request
    );
    return response.data;
  },

  /**
   * Refine existing mission based on user feedback
   */
  async refineMission(
    currentMission: MissionCase,
    userFeedback: string
  ): Promise<MissionSuggestionResponse> {
    const request: RefineMissionRequest = {
      currentMission,
      userFeedback,
    };

    const response = await aiAgentClient.post<MissionSuggestionResponse>(
      "/refine/mission",
      request
    );
    return response.data;
  },

  /**
   * Get usage statistics
   */
  async getUsageStats(): Promise<UsageStats> {
    // Placeholder for future implementation
    return {
      totalRequests: 0,
      totalTokens: 0,
      totalCostUsd: 0,
      requestsToday: 0,
      dailyLimit: 100,
    };
  },
};
