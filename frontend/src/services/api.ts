import axios, { AxiosInstance, AxiosError } from "axios";
import { CognitoUserSession } from "amazon-cognito-identity-js";
import { getUserPool } from "../config/cognito";
import { LocalAuthService } from "./localAuthService";
import { getConfig, isConfigLoaded } from "../config/runtime";
import { diagnoseApiIssue, logDiagnostics, getUserFriendlyError } from "../utils/diagnostics";
import { ApiError, ApiErrorResponse } from "../types/errors";
import { getApiUrl, getAuthMode } from "../utils/env";

type AuthMode = "cognito" | "local";

// Get auth mode from runtime config (with fallback to environment for local dev)
const readAuthMode = (): AuthMode => {
  if (isConfigLoaded()) {
    return getConfig().authMode;
  }
  return getAuthMode();
};

// Function to get current JWT token based on auth mode
const getAuthToken = async (): Promise<string | null> => {
  const authMode = readAuthMode();

  if (authMode === "local") {
    return LocalAuthService.getToken();
  }

  // Cognito token
  return getCognitoToken();
};

// Function to get current Cognito JWT token
const getCognitoToken = (): Promise<string | null> => {
  return new Promise((resolve) => {
    const pool = getUserPool();
    if (!pool) {
      resolve(null);
      return;
    }
    const cognitoUser = pool.getCurrentUser();
    if (!cognitoUser) {
      resolve(null);
      return;
    }

    cognitoUser.getSession((err: Error | null, session: CognitoUserSession | null) => {
      if (err || !session) {
        resolve(null);
        return;
      }

      resolve(session.getIdToken().getJwtToken());
    });
  });
};

const createApiClient = (): AxiosInstance => {
  // Explicitly use API v1
  const API_VERSION = "v1";

  // Use runtime config if loaded, otherwise fall back to environment variable
  const apiUrl = isConfigLoaded() ? getConfig().apiUrl : getApiUrl();

  const baseURL = `${apiUrl}/api/${API_VERSION}`;

  const client = axios.create({
    baseURL,
    timeout: 30000, // 30 seconds - increased for operations like vessel creation that may take time
  });

  // Request interceptor - Add JWT token, version, and unit preference
  client.interceptors.request.use(async (config) => {
    console.log(
      `[API] Making ${config.method?.toUpperCase()} request to: ${config.baseURL}${config.url}`
    );

    try {
      const token = await getAuthToken();
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
        console.log("[API] Added auth token to request");
      } else {
        console.log("[API] No auth token available");
      }
      // Add client version header for tracking
      config.headers["X-Client-Version"] = "1.0.0";

      // Add preferred units header for backend conversion
      // Only add if not requesting settings (to avoid circular dependency)
      if (!config.url?.includes("/users/settings")) {
        try {
          // Import settingsStore dynamically to avoid circular dependency
          const { settingsStore } = await import("../stores/SettingsStore");
          config.headers["X-Preferred-Units"] = settingsStore.preferredUnits;
          console.debug("[API] Request with units:", settingsStore.preferredUnits);
        } catch {
          // If settingsStore is not available yet, use default
          config.headers["X-Preferred-Units"] = "SI";
          console.debug("[API] Using default units: SI");
        }
      } else {
        // For settings endpoint, always use default to avoid circular dependency
        config.headers["X-Preferred-Units"] = "SI";
        console.log("[API] Settings endpoint - using default units");
      }
    } catch (error) {
      console.log("[API] Error setting up request headers:", error);
    }

    console.log("[API] Request config:", {
      url: config.url,
      method: config.method,
      baseURL: config.baseURL,
      headers: {
        ...config.headers,
        Authorization: config.headers.Authorization ? "Bearer ***" : undefined,
      },
    });

    return config;
  });

  // Response interceptor - Handle errors and convert to ApiError
  client.interceptors.response.use(
    (response) => {
      console.log(`[API] Response received from ${response.config.url}:`, {
        status: response.status,
        statusText: response.statusText,
        data: response.data,
      });
      return response;
    },
    async (error: AxiosError) => {
      console.error("[API] Request failed:", {
        url: error.config?.url,
        method: error.config?.method,
        status: error.response?.status,
        statusText: error.response?.statusText,
        data: error.response?.data,
        message: error.message,
      });

      // Run diagnostics to help identify the issue
      const diagnostic = await diagnoseApiIssue(error);
      logDiagnostics(diagnostic);

      // Log specific guidance for common issues
      if (diagnostic.corsIssue) {
        console.warn(
          "💡 CORS Issue Detected: This usually means the API Gateway service is stuck in deployment or CORS is not configured for your domain."
        );
        console.warn("   Check service status: aws apprunner list-services --region us-east-1");
      }

      if (diagnostic.urlMismatch) {
        console.warn(
          "💡 URL Mismatch: Frontend is calling a different URL than configured. Hard refresh your browser (Ctrl+Shift+R)."
        );
      }

      if (diagnostic.serviceReachable === false) {
        console.error(
          "💡 Service Unreachable: The API service is not responding. It may be down, restarting, or stuck in deployment."
        );
        console.error("   Check CloudWatch logs or AWS Console for service status.");
      }

      // Convert error response to ApiError if it matches the expected format
      let apiError: Error = error;
      if (error.response?.data && typeof error.response.data === "object") {
        const errorData = error.response.data as Partial<ApiErrorResponse>;

        // Check if response matches our ApiErrorResponse structure
        if (errorData.error && errorData.message && errorData.statusCode) {
          const convertedError = new ApiError(errorData as ApiErrorResponse);
          apiError = convertedError;
          console.log("[API] Converted to ApiError:", {
            statusCode: convertedError.statusCode,
            error: convertedError.error,
            correlationId: convertedError.correlationId,
          });
        } else {
          // Fallback: add user-friendly error message to the error object and surface it
          const friendly = getUserFriendlyError(error);
          Object.assign(error, { userMessage: friendly });
          error.message = friendly;
        }
      } else {
        // Network or other errors - add user-friendly message and surface it
        const friendly = getUserFriendlyError(error);
        Object.assign(error, { userMessage: friendly });
        error.message = friendly;
      }

      // Handle authentication errors
      if (error.response?.status === 401) {
        console.log("[API] Unauthorized - redirecting to login");
        // Clear session and redirect to login based on auth mode
        const authMode = readAuthMode();
        if (authMode === "local") {
          LocalAuthService.logout();
        } else {
          const pool = getUserPool();
          if (pool) {
            const cognitoUser = pool.getCurrentUser();
            if (cognitoUser) {
              cognitoUser.signOut();
            }
          }
        }
        window.location.href = "/login";
      }

      return Promise.reject(apiError);
    }
  );

  return client;
};

export const api = createApiClient();
