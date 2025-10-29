/**
 * Runtime Configuration Loader
 *
 * Loads application configuration at runtime from /config.json.
 * This allows backend URLs to change without rebuilding the frontend.
 *
 * Fallback Strategy:
 * 1. Try to load /config.json (production)
 * 2. Fall back to environment variables (build-time)
 * 3. Fall back to localhost (development)
 */

export interface AppConfig {
  apiUrl: string;
  authMode: "cognito" | "local";
  cognitoUserPoolId: string;
  cognitoClientId: string;
  awsRegion: string;
}

let cachedConfig: AppConfig | null = null;
let configPromise: Promise<AppConfig> | null = null;

/**
 * Load application configuration
 * Uses caching to avoid multiple fetches
 */
export async function loadConfig(): Promise<AppConfig> {
  // Return cached config if available
  if (cachedConfig) {
    return cachedConfig;
  }

  // Return in-flight promise if already loading
  if (configPromise) {
    return configPromise;
  }

  // Start loading config
  configPromise = loadConfigInternal();
  cachedConfig = await configPromise;

  return cachedConfig;
}

async function loadConfigInternal(): Promise<AppConfig> {
  try {
    // Try to fetch runtime config from CDN
    console.log("[Config] Loading runtime configuration from /config.json...");

    const response = await fetch("/config.json", {
      cache: "no-cache", // Always fetch latest version
      headers: {
        Accept: "application/json",
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to load config: ${response.status} ${response.statusText}`);
    }

    const config = await response.json();

    console.log("[Config] ✅ Runtime configuration loaded successfully");
    console.log("[Config] API URL:", config.apiUrl);
    console.log("[Config] Auth Mode:", config.authMode);

    return config;
  } catch (error) {
    console.warn(
      "[Config] ⚠️ Failed to load runtime config, falling back to build-time config:",
      error
    );

    // Fall back to build-time environment variables
    const fallbackConfig: AppConfig = {
      apiUrl: import.meta.env.VITE_API_URL || "http://localhost:5002",
      authMode: (import.meta.env.VITE_AUTH_MODE || "local") as "cognito" | "local",
      cognitoUserPoolId: import.meta.env.VITE_COGNITO_USER_POOL_ID || "",
      cognitoClientId: import.meta.env.VITE_COGNITO_CLIENT_ID || "",
      awsRegion: import.meta.env.VITE_AWS_REGION || "us-east-1",
    };

    console.log("[Config] Using fallback configuration");
    console.log("[Config] API URL:", fallbackConfig.apiUrl);
    console.log("[Config] Auth Mode:", fallbackConfig.authMode);

    return fallbackConfig;
  }
}

/**
 * Check if config has been loaded
 */
export function isConfigLoaded(): boolean {
  return cachedConfig !== null;
}

/**
 * Get cached config synchronously (must call loadConfig() first)
 * Throws if config not loaded yet
 */
export function getConfig(): AppConfig {
  if (!cachedConfig) {
    throw new Error("Config not loaded! Call loadConfig() first during app initialization.");
  }
  return cachedConfig;
}

/**
 * Clear cached config (useful for testing)
 */
export function clearConfigCache(): void {
  cachedConfig = null;
  configPromise = null;
}
