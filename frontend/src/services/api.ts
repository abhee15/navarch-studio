import axios, { AxiosInstance } from "axios";
import { CognitoUserSession } from "amazon-cognito-identity-js";
import { userPool } from "../config/cognito";
import { LocalAuthService } from "./localAuthService";

type AuthMode = "cognito" | "local";

// Get auth mode from environment
const getAuthMode = (): AuthMode => {
  return (import.meta.env.VITE_AUTH_MODE || "local") as AuthMode;
};

// Function to get current JWT token based on auth mode
const getAuthToken = async (): Promise<string | null> => {
  const authMode = getAuthMode();

  if (authMode === "local") {
    return LocalAuthService.getToken();
  }

  // Cognito token
  return getCognitoToken();
};

// Function to get current Cognito JWT token
const getCognitoToken = (): Promise<string | null> => {
  return new Promise((resolve) => {
    if (!userPool) {
      resolve(null);
      return;
    }
    const cognitoUser = userPool.getCurrentUser();
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
  const baseURL = `${import.meta.env.VITE_API_URL || "http://localhost:5002"}/api/${API_VERSION}`;

  const client = axios.create({
    baseURL,
    timeout: 10000,
  });

  // Request interceptor - Add JWT token, version, and unit preference
  client.interceptors.request.use(async (config) => {
    try {
      const token = await getAuthToken();
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
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
          console.debug("Request with units:", settingsStore.preferredUnits);
        } catch (error) {
          // If settingsStore is not available yet, use default
          config.headers["X-Preferred-Units"] = "SI";
        }
      } else {
        // For settings endpoint, always use default to avoid circular dependency
        config.headers["X-Preferred-Units"] = "SI";
      }
    } catch (error) {
      console.log("No auth token available", error);
    }
    return config;
  });

  // Response interceptor - Handle 401 errors
  client.interceptors.response.use(
    (response) => response,
    (error) => {
      if (error.response?.status === 401) {
        // Clear session and redirect to login based on auth mode
        const authMode = getAuthMode();
        if (authMode === "local") {
          LocalAuthService.logout();
        } else if (userPool) {
          const cognitoUser = userPool.getCurrentUser();
          if (cognitoUser) {
            cognitoUser.signOut();
          }
        }
        window.location.href = "/login";
      }
      return Promise.reject(error);
    }
  );

  return client;
};

export const api = createApiClient();
