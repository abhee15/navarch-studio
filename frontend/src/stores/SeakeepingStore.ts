import { makeAutoObservable, runInAction } from "mobx";
import { seakeepingApi } from "../services/seakeepingApi";
import type {
  VesselSnapshot,
  RaoResultDto,
  RaoCalculationRequestDto,
  SeaStateDto,
  MotionResponseDto,
} from "../types/seakeeping";
import { getErrorMessage } from "../types/errors";

export class SeakeepingStore {
  vesselSnapshot: VesselSnapshot | null = null;
  raoResults: RaoResultDto | null = null;
  motionResponse: MotionResponseDto | null = null;
  isCalculating = false;
  isAnalyzing = false;
  error: string | null = null;

  constructor() {
    makeAutoObservable(this);
  }

  async calculateRaos(request: RaoCalculationRequestDto) {
    if (!this.vesselSnapshot) {
      this.error = "No vessel snapshot loaded";
      return;
    }

    this.isCalculating = true;
    this.error = null;

    try {
      const result = await seakeepingApi.calculateRaos(this.vesselSnapshot.id, request);
      runInAction(() => {
        this.raoResults = result;
        this.isCalculating = false;
      });
    } catch (err) {
      runInAction(() => {
        this.error = getErrorMessage(err);
        this.isCalculating = false;
      });
    }
  }

  async analyzeMotion(seaState: SeaStateDto) {
    if (!this.vesselSnapshot || !this.raoResults) {
      this.error = "No RAO results available";
      return;
    }

    this.isAnalyzing = true;
    this.error = null;

    try {
      const result = await seakeepingApi.analyzeMotion(
        this.vesselSnapshot.id,
        this.raoResults.raoId,
        seaState
      );
      runInAction(() => {
        this.motionResponse = result;
        this.isAnalyzing = false;
      });
    } catch (err) {
      runInAction(() => {
        this.error = getErrorMessage(err);
        this.isAnalyzing = false;
      });
    }
  }

  setVesselSnapshot(snapshot: VesselSnapshot) {
    this.vesselSnapshot = snapshot;
  }

  reset() {
    this.vesselSnapshot = null;
    this.raoResults = null;
    this.motionResponse = null;
    this.error = null;
  }
}

export const seakeepingStore = new SeakeepingStore();
