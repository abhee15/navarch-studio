import { makeAutoObservable, runInAction } from "mobx";
import { api } from "../services/api";
import type { UnitSystemId } from "../utils/unitSymbols";
import axios from "axios";

// Type alias for compatibility
export type UnitSystem = UnitSystemId;

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
   * Fails gracefully - uses default settings if API call fails
   */
  async loadSettings(): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      console.log("[SettingsStore] Loading settings from API...");
      const response = await api.get<UserSettings>("/users/settings");
      console.log("[SettingsStore] Settings loaded successfully:", response.data);
      runInAction(() => {
        this.settings = response.data;
        this.loading = false;
      });
    } catch (err: unknown) {
      let errorMessage = "Failed to load settings";

      // Log the full error for debugging
      console.error("[SettingsStore] Failed to load settings - Full error:", err);
      console.error("[SettingsStore] Error type:", typeof err);
      console.error("[SettingsStore] Error constructor:", (err as Error)?.constructor?.name);

      // Handle axios errors
      if (axios.isAxiosError(err)) {
        console.error("[SettingsStore] Axios error details:", {
          message: err.message,
          status: err.response?.status,
          statusText: err.response?.statusText,
          data: err.response?.data,
          headers: err.response?.headers,
          config: {
            url: err.config?.url,
            method: err.config?.method,
            baseURL: err.config?.baseURL,
          },
        });

        if (err.response) {
          // Server responded with error
          errorMessage = `Failed to load settings: Server error (${err.response.status})`;
          if (err.response.data?.message) {
            errorMessage = err.response.data.message;
          } else if (typeof err.response.data === "string") {
            errorMessage = `Failed to load settings: ${err.response.data}`;
          }
        } else if (err.request) {
          // Request made but no response
          errorMessage = "Failed to load settings: No response from server";
        } else {
          // Error setting up request
          errorMessage = `Failed to load settings: ${err.message}`;
        }
      } else if (err instanceof Error) {
        errorMessage = `Failed to load settings: ${err.message}`;
      } else if (typeof err === "string") {
        errorMessage = `Failed to load settings: ${err}`;
      }

      // Use default settings and continue - don't break the app
      console.warn("[SettingsStore] Using default settings due to error:", errorMessage);
      runInAction(() => {
        this.settings = { preferredUnits: "SI" };
        this.error = errorMessage;
        this.loading = false;
      });
    }
  }

  /**
   * Update user settings
   */
  async updateSettings(settings: Partial<UserSettings>): Promise<void> {
    this.loading = true;
    this.error = null;

    try {
      console.log("[SettingsStore] Updating settings:", settings);
      const response = await api.put<UserSettings>("/users/settings", settings);
      console.log("[SettingsStore] Settings updated successfully:", response.data);
      runInAction(() => {
        this.settings = response.data;
        this.loading = false;
      });
    } catch (err: unknown) {
      let errorMessage = "Failed to update settings";

      console.error("[SettingsStore] Failed to update settings - Full error:", err);

      if (axios.isAxiosError(err)) {
        console.error("[SettingsStore] Axios error details:", {
          message: err.message,
          status: err.response?.status,
          data: err.response?.data,
        });

        if (err.response) {
          errorMessage = `Failed to update settings: Server error (${err.response.status})`;
          if (err.response.data?.message) {
            errorMessage = err.response.data.message;
          }
        } else if (err.request) {
          errorMessage = "Failed to update settings: No response from server";
        } else {
          errorMessage = `Failed to update settings: ${err.message}`;
        }
      } else if (err instanceof Error) {
        errorMessage = `Failed to update settings: ${err.message}`;
      } else if (typeof err === "string") {
        errorMessage = `Failed to update settings: ${err}`;
      }

      runInAction(() => {
        this.error = errorMessage;
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
