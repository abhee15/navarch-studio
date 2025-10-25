import { makeAutoObservable, runInAction } from "mobx";
import { api } from "../services/api";
import { UnitSystem } from "../utils/unitConversion";

export interface UserSettings {
  preferredUnits: UnitSystem;
}

export class SettingsStore {
  settings: UserSettings = {
    preferredUnits: "SI",
  };
  loading = false;
  error: string | null = null;

  constructor() {
    makeAutoObservable(this);
  }

  /**
   * Load user settings from backend
   */
  async loadSettings(): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      const response = await api.get<UserSettings>("/users/settings");
      runInAction(() => {
        this.settings = response.data;
        this.loading = false;
      });
    } catch (err) {
      runInAction(() => {
        this.error = err instanceof Error ? err.message : "Failed to load settings";
        this.loading = false;
      });
      console.error("Failed to load settings:", err);
    }
  }

  /**
   * Update user settings
   */
  async updateSettings(settings: Partial<UserSettings>): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      const response = await api.put<UserSettings>("/users/settings", settings);
      runInAction(() => {
        this.settings = response.data;
        this.loading = false;
      });
    } catch (err) {
      runInAction(() => {
        this.error = err instanceof Error ? err.message : "Failed to update settings";
        this.loading = false;
      });
      throw err;
    }
  }

  /**
   * Update preferred units
   */
  async updatePreferredUnits(units: UnitSystem): Promise<void> {
    await this.updateSettings({ preferredUnits: units });
  }

  /**
   * Get current preferred units
   */
  get preferredUnits(): UnitSystem {
    return this.settings.preferredUnits;
  }
}

export const settingsStore = new SettingsStore();

