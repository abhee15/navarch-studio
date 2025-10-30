export function getEnv(key: string, fallback?: string): string | undefined {
  if (typeof process !== "undefined" && process.env) {
    return process.env[key] ?? fallback;
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
