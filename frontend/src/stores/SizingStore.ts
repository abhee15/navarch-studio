import { makeAutoObservable, runInAction } from "mobx";
import type {
  MissionCase,
  CreateMissionCaseDto,
  CreateSizingRunDto,
  SizingRun,
  CandidateDesign,
  CandidateWithFlags,
  SizingLocksDto,
  ExportFormat,
} from "../types/sizing";
import * as sizingApi from "../services/sizingApi";

export class SizingStore {
  // Mission Cases
  missionCases: MissionCase[] = [];
  selectedMission: MissionCase | null = null;

  // Sizing Runs
  currentRun: SizingRun | null = null;
  runHistory: SizingRun[] = [];

  // Candidates
  candidates: CandidateDesign[] = [];
  selectedCandidate: CandidateDesign | null = null;
  compareCandidates: CandidateDesign[] = [];

  // UI State
  isLoading = false;
  error: string | null = null;
  wizardStep = 1;
  locks: SizingLocksDto = {};

  // Workspace view
  viewMode: "3d" | "2d" | "table" = "3d";
  compareMode = false;

  constructor() {
    makeAutoObservable(this);
  }

  // Mission Cases
  async loadMissionCases() {
    this.isLoading = true;
    this.error = null;
    try {
      const cases = await sizingApi.getMissionCases();
      runInAction(() => {
        // Clear and repopulate to maintain observable array
        this.missionCases.length = 0;
        // Defensive: Ensure cases is an array before spreading
        if (Array.isArray(cases)) {
          cases.forEach((c) => this.missionCases.push(c));
        } else {
          console.warn("[SizingStore] getMissionCases returned non-array:", cases);
        }
        this.isLoading = false;
      });
    } catch (error) {
      runInAction(() => {
        this.error = error instanceof Error ? error.message : "Failed to load mission cases";
        this.isLoading = false;
      });
    }
  }

  async createMissionCase(dto: CreateMissionCaseDto) {
    this.isLoading = true;
    this.error = null;
    try {
      const mission = await sizingApi.createMissionCase(dto);
      runInAction(() => {
        this.missionCases.push(mission);
        this.selectedMission = mission;
        this.isLoading = false;
      });
      return mission;
    } catch (error) {
      runInAction(() => {
        this.error = error instanceof Error ? error.message : "Failed to create mission case";
        this.isLoading = false;
      });
      throw error;
    }
  }

  async selectMission(id: string) {
    this.isLoading = true;
    this.error = null;
    try {
      const mission = await sizingApi.getMissionCase(id);
      runInAction(() => {
        this.selectedMission = mission;
        this.isLoading = false;
      });
    } catch (error) {
      runInAction(() => {
        this.error = error instanceof Error ? error.message : "Failed to load mission case";
        this.isLoading = false;
      });
    }
  }

  async deleteMissionCase(id: string) {
    this.isLoading = true;
    this.error = null;
    try {
      await sizingApi.deleteMissionCase(id);
      runInAction(() => {
        this.missionCases = this.missionCases.filter((m) => m.id !== id);
        if (this.selectedMission?.id === id) {
          this.selectedMission = null;
        }
        this.isLoading = false;
      });
    } catch (error) {
      runInAction(() => {
        this.error = error instanceof Error ? error.message : "Failed to delete mission case";
        this.isLoading = false;
      });
    }
  }

  // Sizing Runs
  async runSolver(dto: CreateSizingRunDto) {
    this.isLoading = true;
    this.error = null;
    try {
      const run = await sizingApi.createSizingRun(dto);
      console.log("[SizingStore] Received run from API:", run);
      console.log("[SizingStore] Run ID:", run.id);
      console.log("[SizingStore] Run keys:", Object.keys(run));
      console.log("[SizingStore] Full run object JSON:", JSON.stringify(run, null, 2));

      // If it's an error object, log it clearly
      if ("error" in run || "message" in run) {
        console.error("[SizingStore] ERROR: Backend returned error instead of run:", run);
      }

      runInAction(() => {
        this.currentRun = run;
        this.isLoading = false;
      });

      // Load candidates - use defensive check for ID (handle both camelCase and PascalCase)
      const runId = run.id || (run as unknown as { Id?: string }).Id;
      if (!runId) {
        console.error("[SizingStore] Run object has no id field!", run);
        throw new Error("Run ID is missing from API response");
      }

      await this.loadCandidates(runId);

      return run;
    } catch (error) {
      runInAction(() => {
        this.error = error instanceof Error ? error.message : "Failed to run solver";
        this.isLoading = false;
      });
      throw error;
    }
  }

  async loadCandidates(runId: string) {
    this.isLoading = true;
    this.error = null;
    try {
      const candidates = await sizingApi.getRunCandidates(runId);
      runInAction(() => {
        // Clear and repopulate to maintain observable array
        this.candidates.length = 0;
        // Defensive: Use forEach instead of spread to avoid Symbol.iterator issues
        if (Array.isArray(candidates)) {
          // DEBUG: Log first candidate to see property names
          if (candidates.length > 0) {
            console.log("[SizingStore] Sample candidate properties:", Object.keys(candidates[0]));
            console.log("[SizingStore] Sample candidate data:", candidates[0]);
          }
          candidates.forEach((c) => this.candidates.push(c));
          // Auto-select first candidate
          if (candidates.length > 0 && !this.selectedCandidate) {
            this.selectedCandidate = candidates[0];
          }
        } else {
          console.warn("[SizingStore] getRunCandidates returned non-array:", candidates);
        }
        this.isLoading = false;
      });
    } catch (error) {
      runInAction(() => {
        this.error = error instanceof Error ? error.message : "Failed to load candidates";
        this.isLoading = false;
      });
    }
  }

  selectCandidate(id: string) {
    const candidate = this.candidates.find((c) => c.id === id);
    if (candidate) {
      this.selectedCandidate = candidate;
    }
  }

  updateCandidate(updatedCandidate: CandidateDesign) {
    runInAction(() => {
      // Update in candidates array
      const index = this.candidates.findIndex((c) => c.id === updatedCandidate.id);
      if (index >= 0) {
        this.candidates[index] = updatedCandidate;
      }

      // Update selected candidate if it's the same one
      if (this.selectedCandidate?.id === updatedCandidate.id) {
        this.selectedCandidate = updatedCandidate;
      }

      // Update in compare candidates if present
      const compareIndex = this.compareCandidates.findIndex((c) => c.id === updatedCandidate.id);
      if (compareIndex >= 0) {
        this.compareCandidates[compareIndex] = updatedCandidate;
      }
    });
  }

  toggleCompareCandidate(id: string) {
    const candidate = this.candidates.find((c) => c.id === id);
    if (!candidate) return;

    const index = this.compareCandidates.findIndex((c) => c.id === id);
    if (index >= 0) {
      this.compareCandidates.splice(index, 1);
    } else if (this.compareCandidates.length < 3) {
      this.compareCandidates.push(candidate);
    }

    this.compareMode = this.compareCandidates.length > 0;
  }

  clearComparison() {
    this.compareCandidates.length = 0;
    this.compareMode = false;
  }

  async exportCandidate(id: string, format: ExportFormat) {
    this.isLoading = true;
    this.error = null;
    try {
      const blob = await sizingApi.exportCandidate(id, format);

      // Download file
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `candidate-${id}.${format}`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);

      runInAction(() => {
        this.isLoading = false;
      });
    } catch (error) {
      runInAction(() => {
        this.error = error instanceof Error ? error.message : "Failed to export candidate";
        this.isLoading = false;
      });
    }
  }

  async pushToHydrostatics(candidateId: string) {
    this.isLoading = true;
    this.error = null;
    try {
      const result = await sizingApi.pushToHydrostatics(candidateId);
      runInAction(() => {
        this.isLoading = false;
      });
      return result.vesselId;
    } catch (error) {
      runInAction(() => {
        this.error = error instanceof Error ? error.message : "Failed to push to hydrostatics";
        this.isLoading = false;
      });
      throw error;
    }
  }

  // UI Helpers
  setWizardStep(step: number) {
    this.wizardStep = step;
  }

  setViewMode(mode: "3d" | "2d" | "table") {
    this.viewMode = mode;
  }

  updateLocks(locks: Partial<SizingLocksDto>) {
    this.locks = { ...this.locks, ...locks };
  }

  // Parse flags from JSON
  getCandidateWithFlags(candidate: CandidateDesign): CandidateWithFlags {
    let flags: Record<string, boolean> = {};
    try {
      const flagsArray = JSON.parse(candidate.flagsJson) as string[];
      flags = flagsArray.reduce(
        (acc, flag) => {
          acc[flag] = true;
          return acc;
        },
        {} as Record<string, boolean>
      );
    } catch {
      // If parsing fails, return empty flags
    }

    return { ...candidate, flags };
  }

  // Reset state
  reset() {
    this.selectedMission = null;
    this.currentRun = null;
    this.candidates.length = 0;
    this.selectedCandidate = null;
    this.compareCandidates.length = 0;
    this.compareMode = false;
    this.wizardStep = 1;
    this.locks = {};
    this.error = null;
  }
}

export const sizingStore = new SizingStore();
