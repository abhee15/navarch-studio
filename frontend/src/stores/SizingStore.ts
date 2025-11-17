import { makeAutoObservable, runInAction, computed } from "mobx";
import type {
  MissionCase,
  CreateMissionCaseDto,
  CreateSizingRunDto,
  SizingRun,
  CandidateDesign,
  CandidateWithFlags,
  SizingLocksDto,
  ExportFormat,
  ShipDParameterMetadata,
  ShipDVesselTaxonomy,
  PushToHydrostaticsResult,
  PushToHydrostaticsRequest,
  SourceDesignSummary,
} from "../types/sizing";
import * as sizingApi from "../services/sizingApi";
import { extractShipDParameters } from "../utils/shipdParameterExtractor";
import { isAxiosError } from "axios";

export interface PushToHydroForm {
  vesselName: string;
  description?: string;
  shipdCategory?: string;
  shipdType?: string;
  shipdTypeDisplayName?: string;
  shipdBowFamily?: string;
  shipdMidshipFamily?: string;
  shipdSternFamily?: string;
  shipdMaskVersion?: number;
}

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

  // ShipD metadata
  shipdParameters: ShipDParameterMetadata[] = [];
  shipdTaxonomy: ShipDVesselTaxonomy[] = [];
  isShipdMetadataLoading = false;
  shipdMetadataLoaded = false;
  shipdMetadataError: string | null = null;

  // UI State
  isLoading = false;
  error: string | null = null;
  wizardStep = 1;
  locks: SizingLocksDto = {};

  // Granular loading state tracking
  loadingStates = {
    candidates: false,
    run: false,
    metadata: false,
    mission: false,
    export: false,
  };

  // Workspace view
  viewMode: "3d" | "2d" | "table" = "3d";
  compareMode = false;

  constructor() {
    makeAutoObservable(this);
    // Validate state on initialization
    this.validateState();
  }

  // Computed values for derived state
  @computed
  get hasSelectedCandidate(): boolean {
    return this.selectedCandidate !== null;
  }

  @computed
  get selectedCandidateWithFlags(): CandidateWithFlags | null {
    if (!this.selectedCandidate) return null;
    return this.getCandidateWithFlags(this.selectedCandidate);
  }

  @computed
  get hasUsableGeometry(): boolean {
    return this.candidates.some((c) => {
      const status = c.geometryGenerationStatus;
      // Check if geometry generation succeeded (Success means at least one method succeeded)
      return (
        status === "Success" ||
        (status !== "BothFailed" && status !== "FormCoefficientFailed" && status !== undefined)
      );
    });
  }

  @computed
  get isLoadingAny(): boolean {
    return (
      this.isLoading ||
      Object.values(this.loadingStates).some(Boolean) ||
      this.isShipdMetadataLoading
    );
  }

  // State validation
  private validateState(): void {
    // Ensure selected candidate exists in candidates array
    if (this.selectedCandidate) {
      const exists = this.candidates.some((c) => c.id === this.selectedCandidate?.id);
      if (!exists) {
        console.warn(
          "[SizingStore] Selected candidate not in candidates array, clearing selection"
        );
        this.selectedCandidate = null;
      }
    }

    // Ensure compare candidates exist in candidates array
    this.compareCandidates = this.compareCandidates.filter((compareCandidate) =>
      this.candidates.some((c) => c.id === compareCandidate.id)
    );

    // Update compare mode based on remaining candidates
    if (this.compareCandidates.length === 0) {
      this.compareMode = false;
    }
  }

  // Mission Cases
  async loadMissionCases() {
    this.loadingStates.mission = true;
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
        this.loadingStates.mission = false;
      });
    } catch (error) {
      runInAction(() => {
        this.error = error instanceof Error ? error.message : "Failed to load mission cases";
        this.loadingStates.mission = false;
      });
    }
  }

  async ensureShipDMetadataLoaded() {
    if (this.shipdMetadataLoaded || this.isShipdMetadataLoading) {
      return;
    }
    this.isShipdMetadataLoading = true;
    this.shipdMetadataError = null;
    try {
      const [parameters, taxonomy] = await Promise.all([
        sizingApi.getShipDParameterMetadata(),
        sizingApi.getShipDVesselTaxonomy(),
      ]);
      runInAction(() => {
        this.shipdParameters = parameters;
        this.shipdTaxonomy = taxonomy;
        this.shipdMetadataLoaded = true;
        this.isShipdMetadataLoading = false;
      });
    } catch (error) {
      runInAction(() => {
        this.shipdMetadataError =
          error instanceof Error ? error.message : "Failed to load hull form parameters metadata";
        this.isShipdMetadataLoading = false;
      });
    }
  }

  getVesselTypesForCategory(category: string): ShipDVesselTaxonomy[] {
    if (!category) return [];
    return this.shipdTaxonomy.filter(
      (entry) => entry.category.toLowerCase() === category.toLowerCase()
    );
  }

  getTaxonomyEntry(category: string, vesselType: string): ShipDVesselTaxonomy | undefined {
    return this.shipdTaxonomy.find(
      (entry) =>
        entry.category.toLowerCase() === category.toLowerCase() &&
        entry.type.toLowerCase() === vesselType.toLowerCase()
    );
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
    this.loadingStates.mission = true;
    this.error = null;
    try {
      const mission = await sizingApi.getMissionCase(id);
      runInAction(() => {
        this.selectedMission = mission;
        // Clear related state when mission changes
        this.currentRun = null;
        this.candidates.length = 0;
        this.selectedCandidate = null;
        this.compareCandidates.length = 0;
        this.compareMode = false;
        this.loadingStates.mission = false;
        // Validate state after update
        this.validateState();
      });
    } catch (error) {
      runInAction(() => {
        this.error = error instanceof Error ? error.message : "Failed to load mission case";
        this.loadingStates.mission = false;
      });
    }
  }

  async updateMissionCase(id: string, dto: Partial<CreateMissionCaseDto>) {
    this.isLoading = true;
    this.error = null;
    try {
      const updatedMission = await sizingApi.updateMissionCase(id, dto);
      runInAction(() => {
        // Update in the list
        const index = this.missionCases.findIndex((m) => m.id === id);
        if (index >= 0) {
          this.missionCases[index] = updatedMission;
        }
        // Update selected if it's the same one
        if (this.selectedMission?.id === id) {
          this.selectedMission = updatedMission;
        }
        this.isLoading = false;
      });
      return updatedMission;
    } catch (error) {
      runInAction(() => {
        this.error = error instanceof Error ? error.message : "Failed to update mission case";
        this.isLoading = false;
      });
      throw error;
    }
  }

  async cloneMissionCase(id: string, newName: string) {
    this.isLoading = true;
    this.error = null;
    try {
      const clonedMission = await sizingApi.cloneMissionCase(id, newName);
      runInAction(() => {
        this.missionCases.push(clonedMission);
        this.selectedMission = clonedMission;
        this.isLoading = false;
      });
      return clonedMission;
    } catch (error) {
      runInAction(() => {
        this.error = error instanceof Error ? error.message : "Failed to clone mission case";
        this.isLoading = false;
      });
      throw error;
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

      // Load mission case if missionCaseId is provided and not already loaded
      if (dto.missionCaseId && this.selectedMission?.id !== dto.missionCaseId) {
        await this.selectMission(dto.missionCaseId);
      }

      return run;
    } catch (error) {
      runInAction(() => {
        this.error = error instanceof Error ? error.message : "Failed to run solver";
        this.isLoading = false;
      });
      throw error;
    }
  }

  async loadSizingRun(runId: string, skipLoadingState = false) {
    if (!skipLoadingState) {
      this.loadingStates.run = true;
    }
    this.error = null;
    try {
      const run = await sizingApi.getSizingRun(runId);
      runInAction(() => {
        this.currentRun = run;
        if (!skipLoadingState) {
          this.loadingStates.run = false;
        }
        // Validate state after loading
        this.validateState();
      });
    } catch (error) {
      runInAction(() => {
        this.error = error instanceof Error ? error.message : "Failed to load sizing run";
        if (!skipLoadingState) {
          this.loadingStates.run = false;
        }
      });
      throw error;
    }
  }

  async loadCandidates(runId: string, skipLoadingState = false) {
    if (!skipLoadingState) {
      this.loadingStates.candidates = true;
    }
    this.error = null;
    try {
      const candidates = await sizingApi.getRunCandidates(runId);

      // Ensure metadata is loaded before extracting parameters
      if (!this.shipdMetadataLoaded) {
        await this.ensureShipDMetadataLoaded();
      }

      // Extract ShipD parameters from vector for each candidate
      const extractedCandidates = candidates.map((c) => {
        // If candidate already has extracted parameters, use as-is
        if (c.bowLengthRatio != null && c.sternLengthRatio != null) {
          return c;
        }

        // Extract parameters from vector if available
        if (c.shipdParametersJson && this.shipdParameters.length > 0) {
          try {
            return extractShipDParameters(c, this.shipdParameters);
          } catch (error) {
            console.error(
              "[SizingStore] Failed to extract ShipD parameters for candidate:",
              c.id,
              error
            );
            return c; // Return original candidate if extraction fails
          }
        }

        return c;
      });

      runInAction(() => {
        // Clear and repopulate to maintain observable array
        this.candidates.length = 0;
        // Defensive: Use forEach instead of spread to avoid Symbol.iterator issues
        if (Array.isArray(extractedCandidates)) {
          // DEBUG: Log first candidate to see property names
          if (extractedCandidates.length > 0) {
            console.log(
              "[SizingStore] Sample candidate properties:",
              Object.keys(extractedCandidates[0])
            );
            console.log("[SizingStore] Sample candidate data:", extractedCandidates[0]);
          }
          extractedCandidates.forEach((c) => this.candidates.push(c));
          // Auto-select first candidate
          if (extractedCandidates.length > 0 && !this.selectedCandidate) {
            this.selectedCandidate = extractedCandidates[0];
          }
        } else {
          console.warn("[SizingStore] getRunCandidates returned non-array:", candidates);
        }
        if (!skipLoadingState) {
          this.loadingStates.candidates = false;
        }
        // Validate state after loading
        this.validateState();
      });

      // Background: Poll for geometry readiness if needed (solver may finish shortly after)
      // Do not block UI; only update when valid geometry is present to improve rendering stability
      void this.pollCandidatesUntilGeometry(runId);
    } catch (error) {
      runInAction(() => {
        this.error = error instanceof Error ? error.message : "Failed to load candidates";
        if (!skipLoadingState) {
          this.loadingStates.candidates = false;
        }
      });
      throw error;
    }
  }

  async loadRunAndCandidates(runId: string) {
    // Load both run and candidates in parallel, managing loading state centrally
    this.loadingStates.run = true;
    this.loadingStates.candidates = true;
    this.error = null;
    try {
      await Promise.all([
        this.loadSizingRun(runId, true), // Skip individual loading state
        this.loadCandidates(runId, true), // Skip individual loading state
      ]);

      // Load mission case if currentRun has missionCaseId and not already loaded
      if (
        this.currentRun?.missionCaseId &&
        this.selectedMission?.id !== this.currentRun.missionCaseId
      ) {
        await this.selectMission(this.currentRun.missionCaseId);
      }

      runInAction(() => {
        this.loadingStates.run = false;
        this.loadingStates.candidates = false;
        // Validate state after loading
        this.validateState();
      });
    } catch (error) {
      runInAction(() => {
        this.error = error instanceof Error ? error.message : "Failed to load run and candidates";
        this.loadingStates.run = false;
        this.loadingStates.candidates = false;
      });
    }
  }

  selectCandidate(id: string) {
    const candidate = this.candidates.find((c) => c.id === id);
    if (!candidate) {
      console.warn("[SizingStore] Candidate not found:", id);
      this.error = `Candidate ${id} not found in current candidates list`;
      return;
    }

    runInAction(() => {
      // Ensure parameters are extracted if missing
      let updatedCandidate = candidate;
      if (
        candidate.shipdParametersJson &&
        (candidate.bowLengthRatio == null || candidate.sternLengthRatio == null)
      ) {
        try {
          updatedCandidate = extractShipDParameters(candidate, this.shipdParameters);
          // Update in candidates array
          const index = this.candidates.findIndex((c) => c.id === id);
          if (index >= 0) {
            this.candidates[index] = updatedCandidate;
          }
        } catch (error) {
          console.error("[SizingStore] Failed to extract ShipD parameters:", error);
          // Continue with original candidate if extraction fails
        }
      }
      this.selectedCandidate = updatedCandidate;
      this.error = null; // Clear any previous errors
      // Validate state after selection
      this.validateState();
    });
  }

  updateCandidate(updatedCandidate: CandidateDesign) {
    // Validate candidate structure
    if (!updatedCandidate || !updatedCandidate.id) {
      console.error("[SizingStore] Invalid candidate update:", updatedCandidate);
      this.error = "Invalid candidate data";
      return;
    }

    runInAction(() => {
      // Update in candidates array
      const index = this.candidates.findIndex((c) => c.id === updatedCandidate.id);
      if (index >= 0) {
        this.candidates[index] = updatedCandidate;
      } else {
        console.warn("[SizingStore] Candidate not found in array, adding:", updatedCandidate.id);
        this.candidates.push(updatedCandidate);
      }

      // Update selected candidate if it's the same one (maintain reference if possible)
      if (this.selectedCandidate?.id === updatedCandidate.id) {
        // If it's the same reference, update properties to maintain reactivity
        if (this.selectedCandidate === this.candidates[index]) {
          // Already updated via array update, just ensure reference is maintained
          this.selectedCandidate = this.candidates[index];
        } else {
          this.selectedCandidate = updatedCandidate;
        }
      }

      // Update in compare candidates if present
      const compareIndex = this.compareCandidates.findIndex((c) => c.id === updatedCandidate.id);
      if (compareIndex >= 0) {
        this.compareCandidates[compareIndex] = updatedCandidate;
      }

      // Validate state after update
      this.validateState();
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
    this.loadingStates.export = true;
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
        this.loadingStates.export = false;
      });
    } catch (error) {
      runInAction(() => {
        this.error = error instanceof Error ? error.message : "Failed to export candidate";
        this.loadingStates.export = false;
      });
    }
  }

  async pushToHydrostatics(
    candidate: CandidateDesign,
    form: PushToHydroForm,
    user?: { id?: string; name?: string }
  ): Promise<PushToHydrostaticsResult> {
    this.error = null;
    const idempotencyKey = this.generateIdempotencyKey();

    const payload: PushToHydrostaticsRequest = {
      vesselName: form.vesselName?.trim() || this.buildDefaultVesselName(candidate),
      description: form.description,
      shipdCategory:
        form.shipdCategory ?? candidate.vesselCategory ?? this.selectedMission?.missionCategory,
      shipdType: form.shipdType ?? candidate.vesselType ?? undefined,
      shipdTypeDisplayName: form.shipdTypeDisplayName,
      shipdBowFamily: form.shipdBowFamily ?? candidate.bowFamily ?? undefined,
      shipdMidshipFamily: form.shipdMidshipFamily ?? candidate.midshipFamily ?? undefined,
      shipdSternFamily: form.shipdSternFamily ?? candidate.sternFamily ?? undefined,
      shipdMaskVersion: form.shipdMaskVersion ?? candidate.familyMaskVersion,
    };

    if (payload.shipdCategory && payload.shipdType) {
      const taxonomyEntry = this.getTaxonomyEntry(payload.shipdCategory, payload.shipdType);
      if (taxonomyEntry) {
        payload.shipdTypeDisplayName =
          payload.shipdTypeDisplayName || taxonomyEntry.displayName || payload.shipdType;
        payload.shipdMaskVersion = payload.shipdMaskVersion ?? taxonomyEntry.maskVersion;
      }
    }

    payload.sourceDesign = this.buildSourceDesign(candidate, {
      idempotencyKey,
      designName: payload.vesselName,
      missionCaseId: this.selectedMission?.id ?? this.currentRun?.missionCaseId,
      missionName: this.selectedMission?.name,
      userId: user?.id ?? candidate.userId,
      userDisplayName: user?.name,
    });

    try {
      return await sizingApi.pushToHydrostatics(candidate.id, payload, idempotencyKey);
    } catch (error: unknown) {
      // Try to extract specific error message from API response
      let message = "Failed to push to hydrostatics";

      if (isAxiosError(error)) {
        const errorData = error.response?.data;
        if (errorData && typeof errorData === "object" && errorData !== null) {
          // Check for error message in various formats
          if ("error" in errorData && typeof errorData.error === "string") {
            message = errorData.error;
          } else if ("message" in errorData && typeof errorData.message === "string") {
            message = errorData.message;
          } else if ("details" in errorData) {
            const errorStr =
              "error" in errorData && typeof errorData.error === "string"
                ? errorData.error
                : "Error";
            message = `${errorStr}: ${String(errorData.details)}`;
          }

          // Add error type if available for better context
          if ("type" in errorData && typeof errorData.type === "string") {
            message = `[${errorData.type}] ${message}`;
          }
        } else if (typeof errorData === "string") {
          message = errorData;
        }
      } else if (error instanceof Error) {
        message = error.message;
      }

      this.error = message;
      console.error("[SizingStore] Push to hydrostatics failed:", {
        error,
        message,
        candidateId: candidate.id,
        payload,
      });
      throw new Error(message);
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
    this.isLoading = false;
    // Clear all loading states
    Object.keys(this.loadingStates).forEach((key) => {
      this.loadingStates[key as keyof typeof this.loadingStates] = false;
    });
    this.isShipdMetadataLoading = false;
    this.shipdMetadataError = null;
  }

  private buildDefaultVesselName(candidate: CandidateDesign): string {
    const baseName = candidate.hullFamily.replace(/_/g, " ");
    if (this.selectedMission?.name) {
      return `${this.selectedMission.name} • ${baseName}`;
    }
    return `${baseName} Design`;
  }

  private buildSourceDesign(
    candidate: CandidateDesign,
    overrides: SourceDesignSummary & { idempotencyKey: string }
  ): SourceDesignSummary {
    return {
      candidateId: candidate.id,
      sizingRunId: candidate.sizingRunId,
      missionCaseId:
        overrides.missionCaseId ?? this.selectedMission?.id ?? this.currentRun?.missionCaseId,
      userId: overrides.userId ?? candidate.userId,
      userDisplayName: overrides.userDisplayName,
      missionName: overrides.missionName ?? this.selectedMission?.name,
      runName: overrides.runName ?? this.buildRunName(),
      designName: overrides.designName ?? this.buildDefaultVesselName(candidate),
      sourceSystem: overrides.sourceSystem ?? "HullSizingService",
      idempotencyKey: overrides.idempotencyKey,
      originCreatedAt: candidate.createdAt,
    };
  }

  private buildRunName(): string | undefined {
    if (!this.currentRun) {
      return undefined;
    }

    const modeLabel = this.currentRun.mode.replace(/_/g, " ");
    const timestamp = this.currentRun.createdAt
      ? new Date(this.currentRun.createdAt).toLocaleDateString()
      : undefined;

    return timestamp ? `${modeLabel} • ${timestamp}` : modeLabel;
  }

  private generateIdempotencyKey(): string {
    if (typeof window !== "undefined" && window.crypto?.randomUUID) {
      return window.crypto.randomUUID();
    }
    return `hydro-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
  }

  // Background helper: poll candidates until at least one has usable geometry
  private async pollCandidatesUntilGeometry(runId: string, timeoutMs = 8000, intervalMs = 600) {
    const deadline = Date.now() + timeoutMs;

    // Simple validator: usable if geometryJson parses with stations OR a ShipD vector is present
    const hasUsableGeometry = (c: CandidateDesign): boolean => {
      if (c.geometryJson) {
        try {
          const parsed = JSON.parse(c.geometryJson) as {
            stations?: Array<{ position: number; offsets: Record<number, number> }>;
          };
          if (parsed && Array.isArray(parsed.stations) && parsed.stations.length > 0) {
            return true;
          }
        } catch {
          // fall through
        }
      }
      // Fallback: ShipD vector present (renderer can generate from vector)
      if (c.shipdParametersJson) {
        try {
          const vec = JSON.parse(c.shipdParametersJson);
          if (Array.isArray(vec) && vec.length === 45) {
            return true;
          }
        } catch {
          // fall through
        }
      }
      return false;
    };

    // If we already have usable geometry, skip polling
    if (this.candidates.some(hasUsableGeometry)) {
      return;
    }

    while (Date.now() < deadline) {
      try {
        const refreshed = await sizingApi.getRunCandidates(runId);
        if (Array.isArray(refreshed) && refreshed.some(hasUsableGeometry)) {
          // Preserve selection if possible; update list
          runInAction(() => {
            this.candidates.length = 0;
            refreshed.forEach((c) => this.candidates.push(c));
            if (this.selectedCandidate) {
              const sel = refreshed.find((c) => c.id === this.selectedCandidate?.id);
              if (sel) this.selectedCandidate = sel;
            }
          });
          console.log("[SizingStore] Geometry became available after polling; updated candidates.");
          return;
        }
      } catch (err) {
        console.debug("[SizingStore] Poll candidates retry failed:", err);
      }
      await new Promise((r) => setTimeout(r, intervalMs));
    }
    console.debug("[SizingStore] Geometry polling timed out; continuing with current candidates.");
  }
}

export const sizingStore = new SizingStore();
