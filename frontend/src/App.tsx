import React, { useEffect, useState } from "react";
import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { observer } from "mobx-react-lite";
import { useStore } from "./stores";
import { LoginPage } from "./pages/LoginPage";
import { SignupPage } from "./pages/SignupPage";
import { DashboardPage } from "./pages/DashboardPage";
import { VesselsList } from "./pages/hydrostatics/VesselsList";
import { VesselBuilder } from "./pages/hydrostatics/VesselBuilder";
import { VesselWorkspace } from "./pages/hydrostatics/VesselWorkspace";
import { ComparisonWorkspace } from "./pages/hydrostatics/ComparisonWorkspace";
import { VesselResistanceWorkspace } from "./pages/resistance/VesselResistanceWorkspace";
import { BenchmarksList } from "./pages/benchmarks/BenchmarksList";
import { BenchmarkDetail } from "./pages/benchmarks/BenchmarkDetail";
import { CatalogBrowser } from "./pages/catalog/CatalogBrowser";
import { HullDetailPage } from "./pages/catalog/HullDetailPage";
import { ThemeProvider } from "./contexts/ThemeContext";
import { ToastProvider } from "./components/common/Toast";
import { loadConfig } from "./config/runtime";
import { checkSystemHealth } from "./utils/diagnostics";
import { UnitsEffectProvider } from "./providers/UnitsEffectProvider";
import { settingsStore } from "./stores/SettingsStore";

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
      <div className="flex h-screen flex-col items-center justify-center font-sans">
        <div className="mb-4">Loading configuration...</div>
        {error && <div className="text-sm text-orange-500">Using fallback configuration</div>}
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
        <UnitsEffectProvider>
          {/* Soft remount routes when units change so data/effects re-run safely */}
          <div key={settingsStore.preferredUnits}>
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
                path="/hydrostatics/vessels/create"
                element={
                  <ProtectedRoute>
                    <VesselBuilder />
                  </ProtectedRoute>
                }
              />
              {/* Default to new Workspace */}
              <Route
                path="/hydrostatics/vessels/:vesselId"
                element={
                  <ProtectedRoute>
                    <VesselWorkspace />
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
              <Route
                path="/hydrostatics/vessels/:vesselId/compare"
                element={
                  <ProtectedRoute>
                    <ComparisonWorkspace />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/resistance/vessels"
                element={
                  <ProtectedRoute>
                    <VesselsList />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/resistance/vessels/:vesselId"
                element={
                  <ProtectedRoute>
                    <VesselResistanceWorkspace />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/benchmarks"
                element={
                  <ProtectedRoute>
                    <BenchmarksList />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/benchmarks/:slug"
                element={
                  <ProtectedRoute>
                    <BenchmarkDetail />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/catalog"
                element={
                  <ProtectedRoute>
                    <CatalogBrowser />
                  </ProtectedRoute>
                }
              />
              <Route
                path="/catalog/hulls/:id"
                element={
                  <ProtectedRoute>
                    <HullDetailPage />
                  </ProtectedRoute>
                }
              />
              <Route path="/" element={<Navigate to="/dashboard" />} />
            </Routes>
          </div>
        </UnitsEffectProvider>
      </BrowserRouter>
    </ThemeProvider>
  </ConfigLoader>
));
