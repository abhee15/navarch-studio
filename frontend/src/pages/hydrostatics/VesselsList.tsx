import { useState, useEffect } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { observer } from "mobx-react-lite";
import toast from "react-hot-toast";
import { getErrorMessage } from "../../types/errors";
import { vesselsApi } from "../../services/hydrostaticsApi";
import type { Vessel } from "../../types/hydrostatics";
import { useStore } from "../../stores";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { Footer } from "../../components/Footer";
import { UserSettingsDialog } from "../../components/UserSettingsDialog";
import { AppHeader } from "../../components/AppHeader";
import { settingsStore } from "../../stores/SettingsStore";
import { getUnitSymbol } from "../../utils/unitSymbols";

export const VesselsList = observer(function VesselsList() {
  const navigate = useNavigate();
  const location = useLocation();
  const { authStore } = useStore();
  const [vessels, setVessels] = useState<Vessel[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);
  const [showSettings, setShowSettings] = useState(false);

  // Detect if we're in resistance context
  const isResistanceContext = location.pathname.startsWith("/resistance");

  const loadVessels = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await vesselsApi.list();
      setVessels(data.vessels);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadVessels();
  }, []);

  // Refresh list when units change so values arrive converted from backend
  useEffect(() => {
    const handler = () => loadVessels();
    window.addEventListener("units:changed", handler as EventListener);
    return () => window.removeEventListener("units:changed", handler as EventListener);
  }, []);

  const handleCreateVessel = () => {
    // Vessels are always created in hydrostatics context
    navigate("/hydrostatics/vessels/create");
  };

  const handleVesselClick = (vesselId: string) => {
    if (isResistanceContext) {
      navigate(`/resistance/vessels/${vesselId}`);
    } else {
      navigate(`/hydrostatics/vessels/${vesselId}/workspace`);
    }
  };

  const handleDeleteVessel = async (vesselId: string, vesselName: string, e: React.MouseEvent) => {
    e.stopPropagation();

    if (!confirm(`Are you sure you want to delete "${vesselName}"?`)) {
      return;
    }

    try {
      setDeletingId(vesselId);
      await vesselsApi.delete(vesselId);
      toast.success("Vessel deleted successfully");
      loadVessels();
    } catch (error) {
      console.error("Failed to delete vessel:", error);
      toast.error("Failed to delete vessel");
    } finally {
      setDeletingId(null);
    }
  };

  const handleHome = () => {
    navigate("/dashboard");
  };

  const handleLogout = () => {
    authStore.logout();
    navigate("/login");
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffHours < 1) return "Just now";
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? "s" : ""} ago`;
    if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? "s" : ""} ago`;
    return date.toLocaleDateString();
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-background">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-background flex flex-col">
      <AppHeader
        left={<h1 className="text-lg font-bold text-foreground">NavArch Studio</h1>}
        right={
          <>
            <button
              onClick={handleHome}
              className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-foreground hover:text-foreground/80 border border-border rounded hover:bg-accent/10"
            >
              <svg className="h-4 w-4 mr-1.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"
                />
              </svg>
              Home
            </button>
            <UserProfileMenu onOpenSettings={() => setShowSettings(true)} onLogout={handleLogout} />
          </>
        }
      />

      {/* Page Header */}
      <div className="bg-card shadow border-b border-border">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-card-foreground">
                {isResistanceContext ? "Resistance & Powering" : "Hydrostatics"}
              </h1>
              <p className="mt-1 text-sm text-muted-foreground">
                {isResistanceContext
                  ? "Select a vessel to perform resistance and powering calculations"
                  : "Manage vessels and compute hydrostatic properties"}
              </p>
            </div>
            {!isResistanceContext && (
              <button
                onClick={handleCreateVessel}
                className="inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-ring"
              >
                <svg
                  className="-ml-1 mr-2 h-5 w-5"
                  xmlns="http://www.w3.org/2000/svg"
                  viewBox="0 0 20 20"
                  fill="currentColor"
                >
                  <path
                    fillRule="evenodd"
                    d="M10 5a1 1 0 011 1v3h3a1 1 0 110 2h-3v3a1 1 0 11-2 0v-3H6a1 1 0 110-2h3V6a1 1 0 011-1z"
                    clipRule="evenodd"
                  />
                </svg>
                New Vessel
              </button>
            )}
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 flex-1">
        {error && (
          <div
            className="mb-4 bg-destructive/10 border border-destructive/30 text-destructive px-4 py-3 rounded relative"
            role="alert"
          >
            <strong className="font-bold">Error: </strong>
            <span className="block sm:inline">{error}</span>
          </div>
        )}

        {vessels.length === 0 ? (
          <div className="text-center py-12">
            <svg
              className="mx-auto h-12 w-12 text-muted-foreground"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 13h6m-3-3v6m-9 1V7a2 2 0 012-2h6l2 2h6a2 2 0 012 2v8a2 2 0 01-2 2H5a2 2 0 01-2-2z"
              />
            </svg>
            <h3 className="mt-2 text-sm font-medium text-foreground">No vessels</h3>
            <p className="mt-1 text-sm text-muted-foreground">
              {isResistanceContext
                ? "Create a vessel in Hydrostatics first, then select it here for resistance calculations."
                : "Get started by creating a new vessel."}
            </p>
            <div className="mt-6">
              {isResistanceContext ? (
                <button
                  onClick={() => navigate("/hydrostatics/vessels/create")}
                  className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-primary-foreground bg-primary hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-ring"
                >
                  <svg
                    className="-ml-1 mr-2 h-5 w-5"
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                  >
                    <path
                      fillRule="evenodd"
                      d="M10 5a1 1 0 011 1v3h3a1 1 0 110 2h-3v3a1 1 0 11-2 0v-3H6a1 1 0 110-2h3V6a1 1 0 011-1z"
                      clipRule="evenodd"
                    />
                  </svg>
                  Create Vessel in Hydrostatics
                </button>
              ) : (
                <button
                  onClick={handleCreateVessel}
                  className="inline-flex items-center px-4 py-2 border border-transparent shadow-sm text-sm font-medium rounded-md text-primary-foreground bg-primary hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-ring"
                >
                  <svg
                    className="-ml-1 mr-2 h-5 w-5"
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                  >
                    <path
                      fillRule="evenodd"
                      d="M10 5a1 1 0 011 1v3h3a1 1 0 110 2h-3v3a1 1 0 11-2 0v-3H6a1 1 0 110-2h3V6a1 1 0 011-1z"
                      clipRule="evenodd"
                    />
                  </svg>
                  New Vessel
                </button>
              )}
            </div>
          </div>
        ) : (
          <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {vessels.map((vessel) => (
              <div
                key={vessel.id}
                onClick={() => handleVesselClick(vessel.id)}
                className={`bg-card overflow-hidden shadow rounded-lg hover:shadow-md transition-shadow duration-200 cursor-pointer border ${
                  vessel.isTemplate
                    ? "border-blue-200 dark:border-blue-800 bg-blue-50/50 dark:bg-blue-950/20"
                    : "border-border"
                }`}
              >
                <div className="p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <svg
                        className="h-10 w-10 text-primary"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"
                        />
                      </svg>
                    </div>
                    <div className="ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-muted-foreground truncate">
                          Vessel
                        </dt>
                        <dd className="flex items-center gap-2">
                          <span className="text-lg font-semibold text-card-foreground">
                            {vessel.name}
                          </span>
                          {vessel.isTemplate && (
                            <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-medium bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300 border border-blue-200 dark:border-blue-800">
                              <svg
                                className="h-3 w-3 mr-1"
                                fill="none"
                                viewBox="0 0 24 24"
                                stroke="currentColor"
                              >
                                <path
                                  strokeLinecap="round"
                                  strokeLinejoin="round"
                                  strokeWidth={2}
                                  d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
                                />
                              </svg>
                              Template
                            </span>
                          )}
                        </dd>
                      </dl>
                    </div>
                  </div>
                  {vessel.description && (
                    <p className="mt-3 text-sm text-muted-foreground line-clamp-2">
                      {vessel.description}
                    </p>
                  )}
                  <div className="mt-4 grid grid-cols-3 gap-2 text-xs">
                    <div>
                      <span className="text-muted-foreground">Lpp:</span>
                      <span className="ml-1 font-medium text-card-foreground">
                        {vessel.lpp}
                        {getUnitSymbol(settingsStore.preferredUnits, "Length")}
                      </span>
                    </div>
                    <div>
                      <span className="text-muted-foreground">B:</span>
                      <span className="ml-1 font-medium text-card-foreground">
                        {vessel.beam}
                        {getUnitSymbol(settingsStore.preferredUnits, "Length")}
                      </span>
                    </div>
                    <div>
                      <span className="text-muted-foreground">T:</span>
                      <span className="ml-1 font-medium text-card-foreground">
                        {vessel.designDraft}
                        {getUnitSymbol(settingsStore.preferredUnits, "Length")}
                      </span>
                    </div>
                  </div>
                  <div className="mt-3 flex items-center justify-between text-xs">
                    <span className="text-muted-foreground">
                      Updated {formatDate(vessel.updatedAt)}
                    </span>
                    {!vessel.isTemplate && (
                      <button
                        onClick={(e) => handleDeleteVessel(vessel.id, vessel.name, e)}
                        disabled={deletingId === vessel.id}
                        className="text-destructive hover:text-destructive/80 disabled:opacity-50 p-1 rounded hover:bg-destructive/10"
                        title="Delete vessel"
                      >
                        {deletingId === vessel.id ? (
                          <svg className="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24">
                            <circle
                              className="opacity-25"
                              cx="12"
                              cy="12"
                              r="10"
                              stroke="currentColor"
                              strokeWidth="4"
                            />
                            <path
                              className="opacity-75"
                              fill="currentColor"
                              d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                            />
                          </svg>
                        ) : (
                          <svg
                            className="h-4 w-4"
                            fill="none"
                            viewBox="0 0 24 24"
                            stroke="currentColor"
                          >
                            <path
                              strokeLinecap="round"
                              strokeLinejoin="round"
                              strokeWidth={2}
                              d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                            />
                          </svg>
                        )}
                      </button>
                    )}
                    {vessel.isTemplate && (
                      <span className="text-muted-foreground italic text-[10px]">
                        Read-only template
                      </span>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Footer */}
      <Footer />

      {/* Settings Dialog */}
      <UserSettingsDialog isOpen={showSettings} onClose={() => setShowSettings(false)} />
    </div>
  );
});

export default VesselsList;
