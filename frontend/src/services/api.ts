import axios, { AxiosInstance } from "axios";
import { CognitoUserSession } from "amazon-cognito-identity-js";
import { userPool } from "../config/cognito";

// Function to get current Cognito JWT token
const getCognitoToken = (): Promise<string | null> => {
  return new Promise((resolve) => {
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

  // Request interceptor - Add Cognito JWT token, version, and unit preference
  client.interceptors.request.use(async (config) => {
    try {
      const token = await getCognitoToken();
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      // Add client version header for tracking
      config.headers["X-Client-Version"] = "1.0.0";
      
      // Add preferred units header for backend conversion
      // Import settingsStore dynamically to avoid circular dependency
      const { settingsStore } = await import("../stores/SettingsStore");
      config.headers["X-Preferred-Units"] = settingsStore.preferredUnits;
    } catch {
      console.log("No auth token available");
    }
    return config;
  });

  // Response interceptor - Handle 401 errors
  client.interceptors.response.use(
    (response) => response,
    (error) => {
      if (error.response?.status === 401) {
        // Clear Cognito session and redirect to login
        const cognitoUser = userPool.getCurrentUser();
        if (cognitoUser) {
          cognitoUser.signOut();
        }
        window.location.href = "/login";
      }
      return Promise.reject(error);
    }
  );

  return client;
};

export const api = createApiClient();
