export function getEnv(key: string, fallback?: string): string | undefined {
  // Prefer Vite's import.meta.env in the browser/build
  try {
    const viteEnv = (import.meta as unknown as { env?: Record<string, string | undefined> })?.env;
    if (viteEnv && typeof viteEnv[key] !== "undefined") {
      return viteEnv[key];
    }
  } catch {
    // ignore
  }

  // Fallback to Node-style env (useful for tests)
  if (
    typeof process !== "undefined" &&
    (process as unknown as { env?: Record<string, string | undefined> }).env
  ) {
    const env = (process as unknown as { env?: Record<string, string | undefined> }).env;
    return env?.[key] ?? fallback;
  }

  return fallback;
}

export function getAuthMode(): "cognito" | "local" {
  const mode = getEnv("VITE_AUTH_MODE", "local");
  return (mode === "cognito" ? "cognito" : "local") as "cognito" | "local";
}

export function getApiUrl(): string {
  return getEnv("VITE_API_URL", "http://localhost:5002") as string;
}
