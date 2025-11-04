/**
 * Diagnostic Utilities
 *
 * Provides helpful error messages and diagnostic information
 * to make troubleshooting easier.
 */

import { getConfig, isConfigLoaded } from "../config/runtime";
import type { AxiosError } from "axios";

export interface DiagnosticInfo {
  timestamp: string;
  configLoaded: boolean;
  apiUrl: string | null;
  expectedUrl: string | null;
  urlMismatch: boolean;
  corsIssue: boolean;
  serviceReachable: boolean | null;
  diagnosticMessage: string;
  recommendedActions: string[];
}

/**
 * Test if the API endpoint is reachable
 */
async function testApiReachability(apiUrl: string): Promise<boolean> {
  try {
    const response = await fetch(`${apiUrl}/health`, {
      method: "GET",
      mode: "cors",
      cache: "no-cache",
      signal: AbortSignal.timeout(5000), // 5 second timeout
    });
    return response.ok;
  } catch {
    return false;
  }
}

/**
 * Analyze API connectivity issues and provide helpful diagnostics
 */
export async function diagnoseApiIssue(error: AxiosError): Promise<DiagnosticInfo> {
  const timestamp = new Date().toISOString();
  const configLoaded = isConfigLoaded();
  const apiUrl = configLoaded ? getConfig().apiUrl : null;

  // Get the URL that was actually called from the error
  const attemptedUrl = error?.config?.baseURL || error?.config?.url || null;

  const diagnostic: DiagnosticInfo = {
    timestamp,
    configLoaded,
    apiUrl,
    expectedUrl: attemptedUrl,
    urlMismatch: false,
    corsIssue: false,
    serviceReachable: null,
    diagnosticMessage: "",
    recommendedActions: [],
  };

  // Check for CORS-specific errors
  if (
    error?.message?.includes("CORS") ||
    error?.message?.includes("Access-Control-Allow-Origin") ||
    error?.code === "ERR_NETWORK"
  ) {
    diagnostic.corsIssue = true;
    diagnostic.diagnosticMessage = "🚫 CORS Error Detected";
    diagnostic.recommendedActions.push(
      "The API Gateway is not allowing requests from your domain.",
      "This usually means:",
      "  1. API Gateway service is stuck or not fully deployed",
      "  2. CORS configuration has not been applied",
      "  3. CloudFront domain not in allowed origins list",
      "",
      "💡 Check service status:",
      "  aws apprunner list-services --region us-east-1",
      "",
      "💡 Look for Status: OPERATION_IN_PROGRESS (stuck deployment)"
    );
  }

  // Check for URL mismatch
  if (apiUrl && attemptedUrl && !attemptedUrl.includes(apiUrl)) {
    diagnostic.urlMismatch = true;
    diagnostic.diagnosticMessage = "⚠️ URL Mismatch Detected";
    diagnostic.recommendedActions.push(
      `Expected API URL: ${apiUrl}`,
      `Actually calling: ${attemptedUrl}`,
      "",
      "This means the frontend has stale configuration.",
      "",
      "💡 Solutions:",
      "  1. Hard refresh browser (Ctrl+Shift+R)",
      "  2. Clear browser cache",
      "  3. Open in Incognito/Private mode",
      "  4. Wait for config.json cache to expire (5 minutes)"
    );
  }

  // Test if service is reachable
  if (apiUrl) {
    diagnostic.serviceReachable = await testApiReachability(apiUrl);

    if (!diagnostic.serviceReachable) {
      diagnostic.diagnosticMessage = "🔴 API Service Not Reachable";
      diagnostic.recommendedActions.push(
        `Cannot connect to: ${apiUrl}`,
        "",
        "Possible causes:",
        "  1. Service is down or restarting",
        "  2. Service stuck in deployment (check AWS console)",
        "  3. Network connectivity issues",
        "  4. DNS not resolved yet (new service)",
        "",
        "💡 Check service health:",
        `  curl ${apiUrl}/health`,
        "",
        "💡 Check service status in AWS:",
        "  aws apprunner describe-service --service-arn <ARN>"
      );
    }
  }

  // Timeout errors
  if (error?.message?.includes("timeout") || error?.code === "ECONNABORTED") {
    diagnostic.diagnosticMessage = "⏱️ Request Timeout";
    diagnostic.recommendedActions.push(
      "Request took too long to complete.",
      "",
      "Possible causes:",
      "  1. API service is processing but slow",
      "  2. Service is under heavy load",
      "  3. Database queries taking too long",
      "  4. Service starting up (migrations running)",
      "",
      "💡 Check CloudWatch logs:",
      "  aws logs tail /aws/apprunner/navarch-studio-dev-data-service/service --follow"
    );
  }

  return diagnostic;
}

/**
 * Log diagnostic information in a user-friendly format
 */
export function logDiagnostics(diagnostic: DiagnosticInfo): void {
  console.group("🔍 API Diagnostic Report");
  console.log("Timestamp:", diagnostic.timestamp);
  console.log("Config Loaded:", diagnostic.configLoaded ? "✅" : "❌");
  console.log("Expected API URL:", diagnostic.apiUrl || "Not loaded");
  console.log("Attempted URL:", diagnostic.expectedUrl || "Unknown");

  if (diagnostic.urlMismatch) {
    console.warn("⚠️ URL MISMATCH DETECTED!");
  }

  if (diagnostic.corsIssue) {
    console.error("🚫 CORS ERROR DETECTED");
  }

  if (diagnostic.serviceReachable === false) {
    console.error("🔴 SERVICE NOT REACHABLE");
  } else if (diagnostic.serviceReachable === true) {
    console.log("Service Reachable: ✅");
  }

  if (diagnostic.diagnosticMessage) {
    console.log("\n" + diagnostic.diagnosticMessage);
  }

  if (diagnostic.recommendedActions.length > 0) {
    console.log("\n📋 Recommended Actions:");
    diagnostic.recommendedActions.forEach((action) => {
      if (action === "") {
        console.log("");
      } else {
        console.log(action);
      }
    });
  }

  console.groupEnd();
}

/**
 * Enhanced error message for display in UI
 */
export function getUserFriendlyError(error: AxiosError): string {
  // Prefer backend-provided message when available
  const data = (error?.response?.data ?? null) as unknown;
  if (data && typeof data === "object") {
    const anyData = data as Record<string, unknown>;
    if (typeof anyData.message === "string" && anyData.message.trim().length > 0) {
      return anyData.message as string;
    }
    if (typeof anyData.error === "string" && (anyData.error as string).trim().length > 0) {
      return anyData.error as string;
    }
  }

  // CORS errors
  if (error?.message?.includes("CORS") || error?.message?.includes("Access-Control-Allow-Origin")) {
    return "🚫 Connection Blocked: The API service is not accepting requests from this domain. This usually means the service is being deployed or has a configuration issue. Please try again in a few minutes.";
  }

  // Network errors
  if (error?.code === "ERR_NETWORK" || error?.message?.includes("Network Error")) {
    return "🔴 Cannot Reach API: Unable to connect to the backend service. The service may be restarting or experiencing issues. Please try again in a moment.";
  }

  // Timeout errors
  if (error?.message?.includes("timeout") || error?.code === "ECONNABORTED") {
    return "⏱️ Request Timeout: The request took too long to complete. The service may be starting up or under heavy load. Please try again.";
  }

  // 500 errors
  if (error?.response?.status === 500) {
    return "⚠️ Server Error: The backend encountered an error processing your request. This has been logged and will be investigated.";
  }

  // 401/403 errors
  if (error?.response?.status === 401 || error?.response?.status === 403) {
    return "🔒 Authentication Required: Your session may have expired. Please log in again.";
  }

  // 404 errors
  if (error?.response?.status === 404) {
    return "❓ Not Found: The requested resource could not be found. This may indicate a configuration issue.";
  }

  // Default
  return `❌ Request Failed: ${error?.message || "Unknown error occurred"}. Please try again or contact support if the issue persists.`;
}

/**
 * Check system health and log diagnostics
 */
export async function checkSystemHealth(): Promise<void> {
  console.group("🏥 System Health Check");

  // Check if runtime config is loaded
  const configLoaded = isConfigLoaded();
  console.log("Runtime Config:", configLoaded ? "✅ Loaded" : "❌ Not loaded");

  if (configLoaded) {
    const config = getConfig();
    console.log("API URL:", config.apiUrl);
    console.log("Auth Mode:", config.authMode);
    console.log("AWS Region:", config.awsRegion);

    // Test API reachability
    console.log("\nTesting API connectivity...");
    const reachable = await testApiReachability(config.apiUrl);

    if (reachable) {
      console.log("✅ API is reachable and responding");
    } else {
      console.error("❌ API is not reachable");
      console.log("💡 Possible issues:");
      console.log("  - Service is down or restarting");
      console.log("  - Service stuck in deployment");
      console.log("  - DNS not resolved yet");
      console.log("  - Network connectivity issues");
    }
  } else {
    console.warn("⚠️ Runtime config not loaded - using fallback");
  }

  console.groupEnd();
}
