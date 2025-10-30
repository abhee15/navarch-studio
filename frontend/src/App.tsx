import React, { useEffect, useState } from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { observer } from "mobx-react-lite";
import { useStore } from "./stores";
import { LoginPage } from "./pages/LoginPage";
import { SignupPage } from "./pages/SignupPage";
import { DashboardPage } from "./pages/DashboardPage";
import { VesselsList } from "./pages/hydrostatics/VesselsList";
import { VesselDetail } from "./pages/hydrostatics/VesselDetail";
import { VesselWorkspace } from "./pages/hydrostatics/VesselWorkspace";
import { ThemeProvider } from "./contexts/ThemeContext";
import { ToastProvider } from "./components/common/Toast";
import { loadConfig } from "./config/runtime";
import { checkSystemHealth } from "./utils/diagnostics";

// Extend Window interface to include our global debug function
declare global {
  interface Window {
    checkHealth: typeof checkSystemHealth;
  }
}

const ProtectedRoute: React.FC<{ children: React.ReactElement }> = ({ children }) => {
  const { authStore } = useStore();
  return authStore.isAuthenticated ? children : <Navigate to="/login" />;
};

/**
 * ConfigLoader component
 * Loads runtime configuration before rendering the app
 */
const ConfigLoader: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [configLoaded, setConfigLoaded] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadConfig()
      .then(() => {
        console.log("[App] Configuration loaded successfully");

        // Run system health check and make it available globally
        checkSystemHealth();

        // Expose health check function in browser console for debugging
        window.checkHealth = checkSystemHealth;
        console.log("💡 Run checkHealth() in console anytime to diagnose issues");

        setConfigLoaded(true);
      })
      .catch((err) => {
        console.error("[App] Failed to load configuration:", err);
        // Even if config fails to load, continue with fallback values
        setError(err.message);
        setConfigLoaded(true);
      });
  }, []);

  if (!configLoaded) {
    return (
      <div
        style={{
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          height: "100vh",
          flexDirection: "column",
          fontFamily: "system-ui, -apple-system, sans-serif",
        }}
      >
        <div style={{ marginBottom: "1rem" }}>Loading configuration...</div>
        {error && (
          <div style={{ color: "orange", fontSize: "0.875rem" }}>Using fallback configuration</div>
        )}
      </div>
    );
  }

  return <>{children}</>;
};

export const App: React.FC = observer(() => (
  <ConfigLoader>
    <ThemeProvider>
      <BrowserRouter>
        <ToastProvider />
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/signup" element={<SignupPage />} />
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <DashboardPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/hydrostatics/vessels"
            element={
              <ProtectedRoute>
                <VesselsList />
              </ProtectedRoute>
            }
          />
          <Route
            path="/hydrostatics/vessels/:vesselId"
            element={
              <ProtectedRoute>
                <VesselDetail />
              </ProtectedRoute>
            }
          />
          <Route
            path="/hydrostatics/vessels/:vesselId/workspace"
            element={
              <ProtectedRoute>
                <VesselWorkspace />
              </ProtectedRoute>
            }
          />
          <Route path="/" element={<Navigate to="/dashboard" />} />
        </Routes>
      </BrowserRouter>
    </ThemeProvider>
  </ConfigLoader>
));
