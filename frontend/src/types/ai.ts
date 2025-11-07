import { MissionCase } from "./sizing";

export interface NLMissionRequest {
  naturalLanguage: string;
  preferredUnits?: string;
}

export interface MissionSuggestionResponse {
  missionCase: MissionCase;
  reasoning: string;
  confidence: number;
  alternativeSuggestions: string[];
}

export interface RefineMissionRequest {
  currentMission: MissionCase;
  userFeedback: string;
}

export interface ChatMessage {
  id: string;
  role: "user" | "assistant" | "system";
  content: string;
  timestamp: Date;
  actions?: ChatAction[];
}

export interface ChatAction {
  label: string;
  onClick: () => void;
  variant?: "primary" | "secondary";
}

export type PanelPosition = "right" | "left" | "floating" | "minimized" | "hidden";
export type PanelTab = "chat" | "suggestions" | "history" | "settings";

export interface UsageStats {
  totalRequests: number;
  totalTokens: number;
  totalCostUsd: number;
  requestsToday: number;
  dailyLimit: number;
}
