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
import { SeakeepingWorkspace } from "./pages/seakeeping/SeakeepingWorkspace";
import { BenchmarksList } from "./pages/benchmarks/BenchmarksList";
import { BenchmarkDetail } from "./pages/benchmarks/BenchmarkDetail";
import { CatalogBrowserV2 } from "./pages/catalog/CatalogBrowserV2";
import { HullDetailPage } from "./pages/catalog/HullDetailPage";
import { MissionCasesList } from "./pages/sizing/MissionCasesList";
import { MissionWizard } from "./pages/sizing/MissionWizard";
import { SizingRunResults } from "./pages/sizing/SizingRunResults";
import { CandidateWorkspace } from "./pages/sizing/CandidateWorkspace";
import { DesignSpaceExplorer } from "./pages/sizing/DesignSpaceExplorer";
import { GlobalCopilotWrapper } from "./components/ai/GlobalCopilotWrapper";
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

const ProtectedRoute: React.FC<{ children: React.ReactElement }> = observer(({ children }) => {
  const { authStore } = useStore();

  // Show loading while checking authentication status
  if (authStore.initializing) {
    return (
      <div className="flex h-screen items-center justify-center">
        <div className="text-center">
          <div className="mb-4 text-lg">Loading...</div>
        </div>
      </div>
    );
  }

  return authStore.isAuthenticated ? children : <Navigate to="/login" />;
});

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
        console.log("[TIP] Run checkHealth() in console anytime to diagnose issues");

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
            <GlobalCopilotWrapper>
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
                  path="/seakeeping"
                  element={
                    <ProtectedRoute>
                      <SeakeepingWorkspace />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/seakeeping/:vesselId"
                  element={
                    <ProtectedRoute>
                      <SeakeepingWorkspace />
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
                      <CatalogBrowserV2 />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/catalog/v2"
                  element={
                    <ProtectedRoute>
                      <CatalogBrowserV2 />
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
                <Route
                  path="/sizing/missions"
                  element={
                    <ProtectedRoute>
                      <MissionCasesList />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/sizing/missions/:missionId"
                  element={<Navigate to="/sizing/missions" replace />}
                />
                <Route
                  path="/sizing/explorer/:missionId"
                  element={
                    <ProtectedRoute>
                      <DesignSpaceExplorer />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/sizing/wizard"
                  element={
                    <ProtectedRoute>
                      <MissionWizard />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/sizing/mission/new"
                  element={
                    <ProtectedRoute>
                      <MissionWizard />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/sizing/runs/:runId"
                  element={
                    <ProtectedRoute>
                      <SizingRunResults />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/sizing/workspace/:candidateId"
                  element={
                    <ProtectedRoute>
                      <CandidateWorkspace />
                    </ProtectedRoute>
                  }
                />
                <Route path="/" element={<Navigate to="/dashboard" />} />
              </Routes>
            </GlobalCopilotWrapper>
          </div>
        </UnitsEffectProvider>
      </BrowserRouter>
    </ThemeProvider>
  </ConfigLoader>
));
