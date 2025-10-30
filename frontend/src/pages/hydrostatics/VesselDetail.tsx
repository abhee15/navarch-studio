import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { observer } from "mobx-react-lite";
import { vesselsApi } from "../../services/hydrostaticsApi";
import type { VesselDetails } from "../../types/hydrostatics";
import { ConsolidatedHydrostaticsTab } from "../../components/hydrostatics/tabs/ConsolidatedHydrostaticsTab";
import { GeometryTab } from "../../components/hydrostatics/tabs/GeometryTab";
import { LoadcasesTab } from "../../components/hydrostatics/tabs/LoadcasesTab";
import { BonjeanCurvesTab } from "../../components/hydrostatics/tabs/BonjeanCurvesTab";
import { useStore } from "../../stores";
import { ThemeToggle } from "../../components/ThemeToggle";

type TabName = "hydrostatics" | "geometry" | "loadcases" | "bonjean";

export const VesselDetail = observer(function VesselDetail() {
  const { vesselId } = useParams<{ vesselId: string }>();
  const navigate = useNavigate();
  const { authStore } = useStore();
  const [vessel, setVessel] = useState<VesselDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<TabName>("hydrostatics");

  const loadVessel = async () => {
    if (!vesselId) return;

    try {
      setLoading(true);
      setError(null);
      const data = await vesselsApi.get(vesselId);
      setVessel(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load vessel");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadVessel();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [vesselId]);

  const handleBack = () => {
    navigate("/hydrostatics/vessels");
  };

  const handleHome = () => {
    navigate("/dashboard");
  };

  const handleLogout = () => {
    authStore.logout();
    navigate("/login");
  };

  const tabs = [
    { id: "hydrostatics", label: "Hydrostatics", count: null },
    { id: "geometry", label: "Geometry", count: vessel?.offsetsCount || 0 },
    { id: "loadcases", label: "Loadcases", count: null },
    { id: "bonjean", label: "Bonjean Curves", count: null },
  ] as const;

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-background">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary"></div>
      </div>
    );
  }

  if (error || !vessel) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <div className="bg-card p-8 rounded-lg shadow-md max-w-md w-full border border-border">
          <div className="text-center">
            <svg
              className="mx-auto h-12 w-12 text-destructive"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
              />
            </svg>
            <h3 className="mt-2 text-lg font-medium text-foreground">Error loading vessel</h3>
            <p className="mt-1 text-sm text-muted-foreground">{error || "Vessel not found"}</p>
            <button
              onClick={handleBack}
              className="mt-4 inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/90"
            >
              ← Back to vessels
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="h-screen flex flex-col bg-background overflow-hidden">
      {/* Main Navigation Header */}
      <header className="border-b border-border bg-card/80 backdrop-blur-sm flex-shrink-0">
        <div className="px-4 py-2">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <h1 className="text-lg font-bold text-foreground">NavArch Studio</h1>
            </div>
            <div className="flex items-center space-x-2">
              <ThemeToggle />
              <button
                onClick={handleHome}
                className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-foreground hover:text-foreground/80 border border-border rounded hover:bg-accent/10"
              >
                <svg
                  className="h-4 w-4 mr-1.5"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"
                  />
                </svg>
                Home
              </button>
              <button
                onClick={handleLogout}
                className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-foreground hover:text-foreground/80 border border-border rounded hover:bg-accent/10"
              >
                <svg
                  className="h-4 w-4 mr-1.5"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"
                  />
                </svg>
                Sign out
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Vessel Header - Compact Command Rail */}
      <div className="bg-card shadow flex-shrink-0 border-b border-border">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-3">
          {/* Single Row: Back | Title | Description | Principal Dims | Units Badge */}
          <div className="flex items-center gap-3">
            <button
              onClick={handleBack}
              className="inline-flex items-center text-xs text-muted-foreground hover:text-foreground flex-shrink-0"
            >
              <svg className="h-4 w-4 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M15 19l-7-7 7-7"
                />
              </svg>
              Back
            </button>

            <div className="h-5 w-px bg-border"></div>

            <h1 className="text-lg font-bold text-card-foreground flex-shrink-0">{vessel.name}</h1>

            {vessel.description && (
              <>
                <span className="text-xs text-muted-foreground">•</span>
                <p className="text-xs text-muted-foreground truncate flex-shrink-0 max-w-xs">
                  {vessel.description}
                </p>
              </>
            )}

            <div className="flex items-center gap-2 text-xs text-card-foreground ml-auto">
              <span className="text-muted-foreground">Lpp:</span>
              <span className="font-medium">{vessel.lpp}m</span>
              <span className="text-border">|</span>
              <span className="text-muted-foreground">B:</span>
              <span className="font-medium">{vessel.beam}m</span>
              <span className="text-border">|</span>
              <span className="text-muted-foreground">T:</span>
              <span className="font-medium">{vessel.designDraft}m</span>
            </div>

            <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-medium bg-primary/10 text-primary flex-shrink-0">
              {vessel.unitsSystem}
            </span>
          </div>

          {/* Tabs */}
          <div className="mt-3 border-b border-border">
            <nav className="-mb-px flex space-x-6" aria-label="Tabs">
              {tabs.map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id as TabName)}
                  className={`
                    ${
                      activeTab === tab.id
                        ? "border-primary text-primary"
                        : "border-transparent text-muted-foreground hover:text-foreground hover:border-muted"
                    }
                    whitespace-nowrap py-2 px-1 border-b-2 font-medium text-xs transition-colors
                  `}
                >
                  {tab.label}
                  {tab.count !== null && (
                    <span
                      className={`
                        ml-1.5 py-0.5 px-1.5 rounded-full text-[10px] font-medium
                        ${
                          activeTab === tab.id
                            ? "bg-primary/10 text-primary"
                            : "bg-muted text-muted-foreground"
                        }
                      `}
                    >
                      {tab.count}
                    </span>
                  )}
                </button>
              ))}
            </nav>
          </div>
        </div>
      </div>

      {/* Tab Content - Flex-1 to fill remaining space */}
      <div
        className={activeTab === "hydrostatics" ? "flex-1 overflow-hidden" : "flex-1 overflow-auto"}
      >
        {activeTab === "hydrostatics" && (
          <ConsolidatedHydrostaticsTab vesselId={vessel.id} vessel={vessel} />
        )}
        {activeTab === "geometry" && (
          <div className="max-w-7xl mx-auto px-3 py-4 h-full">
            <GeometryTab vesselId={vessel.id} vessel={vessel} />
          </div>
        )}
        {activeTab === "loadcases" && (
          <div className="max-w-7xl mx-auto px-3 py-4 h-full">
            <LoadcasesTab vesselId={vessel.id} />
          </div>
        )}
        {activeTab === "bonjean" && (
          <div className="max-w-7xl mx-auto px-3 py-4 h-full">
            <BonjeanCurvesTab vesselId={vessel.id} />
          </div>
        )}
      </div>
    </div>
  );
});

export default VesselDetail;
