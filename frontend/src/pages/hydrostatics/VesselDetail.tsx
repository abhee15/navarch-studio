import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { observer } from "mobx-react-lite";
import { vesselsApi } from "../../services/hydrostaticsApi";
import type { VesselDetails } from "../../types/hydrostatics";
import { ConsolidatedHydrostaticsTab } from "../../components/hydrostatics/tabs/ConsolidatedHydrostaticsTab";
import { GeometryTab } from "../../components/hydrostatics/tabs/GeometryTab";
import { LoadcasesTab } from "../../components/hydrostatics/tabs/LoadcasesTab";
import { useStore } from "../../stores";
import { ThemeToggle } from "../../components/ThemeToggle";

type TabName = "hydrostatics" | "geometry" | "loadcases";

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
  ] as const;

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error || !vessel) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="bg-white p-8 rounded-lg shadow-md max-w-md w-full">
          <div className="text-center">
            <svg
              className="mx-auto h-12 w-12 text-red-500"
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
            <h3 className="mt-2 text-lg font-medium text-gray-900">Error loading vessel</h3>
            <p className="mt-1 text-sm text-gray-500">{error || "Vessel not found"}</p>
            <button
              onClick={handleBack}
              className="mt-4 inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700"
            >
              ← Back to vessels
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="h-screen flex flex-col bg-gray-50 dark:bg-gray-900 overflow-hidden">
      {/* Main Navigation Header */}
      <header className="border-b border-gray-200 dark:border-gray-700 bg-white/80 dark:bg-gray-800/80 backdrop-blur-sm flex-shrink-0">
        <div className="px-4 py-2">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <h1 className="text-lg font-bold text-gray-900 dark:text-white">NavArch Studio</h1>
            </div>
            <div className="flex items-center space-x-2">
              <ThemeToggle />
              <button
                onClick={handleHome}
                className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-gray-700 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white border border-gray-300 dark:border-gray-600 rounded hover:bg-gray-50 dark:hover:bg-gray-700"
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
                className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-gray-700 dark:text-gray-300 hover:text-gray-900 dark:hover:text-white border border-gray-300 dark:border-gray-600 rounded hover:bg-gray-50 dark:hover:bg-gray-700"
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
      <div className="bg-white dark:bg-gray-800 shadow flex-shrink-0">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-3">
          {/* Single Row: Back | Title | Description | Principal Dims | Units Badge */}
          <div className="flex items-center gap-3">
            <button
              onClick={handleBack}
              className="inline-flex items-center text-xs text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 flex-shrink-0"
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

            <div className="h-5 w-px bg-gray-300 dark:bg-gray-600"></div>

            <h1 className="text-lg font-bold text-gray-900 dark:text-white flex-shrink-0">
              {vessel.name}
            </h1>

            {vessel.description && (
              <>
                <span className="text-xs text-gray-400 dark:text-gray-500">•</span>
                <p className="text-xs text-gray-500 dark:text-gray-400 truncate flex-shrink-0 max-w-xs">
                  {vessel.description}
                </p>
              </>
            )}

            <div className="flex items-center gap-2 text-xs text-gray-600 dark:text-gray-300 ml-auto">
              <span className="text-gray-400 dark:text-gray-500">Lpp:</span>
              <span className="font-medium">{vessel.lpp}m</span>
              <span className="text-gray-300 dark:text-gray-600">|</span>
              <span className="text-gray-400 dark:text-gray-500">B:</span>
              <span className="font-medium">{vessel.beam}m</span>
              <span className="text-gray-300 dark:text-gray-600">|</span>
              <span className="text-gray-400 dark:text-gray-500">T:</span>
              <span className="font-medium">{vessel.designDraft}m</span>
            </div>

            <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-medium bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200 flex-shrink-0">
              {vessel.unitsSystem}
            </span>
          </div>

          {/* Tabs */}
          <div className="mt-3 border-b border-gray-200 dark:border-gray-700">
            <nav className="-mb-px flex space-x-6" aria-label="Tabs">
              {tabs.map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id as TabName)}
                  className={`
                    ${
                      activeTab === tab.id
                        ? "border-blue-500 dark:border-blue-400 text-blue-600 dark:text-blue-400"
                        : "border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600"
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
                            ? "bg-blue-100 dark:bg-blue-900 text-blue-600 dark:text-blue-200"
                            : "bg-gray-100 dark:bg-gray-700 text-gray-900 dark:text-gray-300"
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
      </div>
    </div>
  );
});

export default VesselDetail;
