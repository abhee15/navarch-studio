import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { vesselsApi } from "../../services/hydrostaticsApi";
import type { VesselDetails } from "../../types/hydrostatics";
import { OverviewTab } from "../../components/hydrostatics/tabs/OverviewTab";
import { GeometryTab } from "../../components/hydrostatics/tabs/GeometryTab";
import { LoadcasesTab } from "../../components/hydrostatics/tabs/LoadcasesTab";
import { ComputationsTab } from "../../components/hydrostatics/tabs/ComputationsTab";
import { CurvesTab } from "../../components/hydrostatics/tabs/CurvesTab";
import { TrimSolverTab } from "../../components/hydrostatics/tabs/TrimSolverTab";

type TabName = "overview" | "geometry" | "loadcases" | "computations" | "curves" | "trim";

export function VesselDetail() {
  const { vesselId } = useParams<{ vesselId: string }>();
  const navigate = useNavigate();
  const [vessel, setVessel] = useState<VesselDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<TabName>("overview");

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

  const tabs = [
    { id: "overview", label: "Overview", count: null },
    { id: "geometry", label: "Geometry", count: vessel?.offsetsCount || 0 },
    { id: "loadcases", label: "Loadcases", count: null },
    { id: "computations", label: "Computations", count: null },
    { id: "curves", label: "Curves", count: null },
    { id: "trim", label: "Trim Solver", count: null },
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
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <div className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <div className="flex items-center space-x-4">
            <button
              onClick={handleBack}
              className="inline-flex items-center text-sm text-gray-500 hover:text-gray-700"
            >
              <svg className="h-5 w-5 mr-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M15 19l-7-7 7-7"
                />
              </svg>
              Back
            </button>
            <div className="flex-1">
              <h1 className="text-2xl font-bold text-gray-900">{vessel.name}</h1>
              {vessel.description && (
                <p className="mt-1 text-sm text-gray-500">{vessel.description}</p>
              )}
            </div>
            <div className="flex items-center space-x-2">
              <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                {vessel.unitsSystem}
              </span>
            </div>
          </div>

          {/* Principal Particulars Summary */}
          <div className="mt-4 grid grid-cols-3 gap-4 text-sm">
            <div className="bg-gray-50 px-3 py-2 rounded">
              <span className="text-gray-500">Lpp:</span>
              <span className="ml-2 font-semibold text-gray-900">{vessel.lpp} m</span>
            </div>
            <div className="bg-gray-50 px-3 py-2 rounded">
              <span className="text-gray-500">Beam:</span>
              <span className="ml-2 font-semibold text-gray-900">{vessel.beam} m</span>
            </div>
            <div className="bg-gray-50 px-3 py-2 rounded">
              <span className="text-gray-500">Design Draft:</span>
              <span className="ml-2 font-semibold text-gray-900">{vessel.designDraft} m</span>
            </div>
          </div>

          {/* Tabs */}
          <div className="mt-6 border-b border-gray-200">
            <nav className="-mb-px flex space-x-8" aria-label="Tabs">
              {tabs.map((tab) => (
                <button
                  key={tab.id}
                  onClick={() => setActiveTab(tab.id as TabName)}
                  className={`
                    ${
                      activeTab === tab.id
                        ? "border-blue-500 text-blue-600"
                        : "border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300"
                    }
                    whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm transition-colors
                  `}
                >
                  {tab.label}
                  {tab.count !== null && (
                    <span
                      className={`
                        ml-2 py-0.5 px-2.5 rounded-full text-xs font-medium
                        ${
                          activeTab === tab.id
                            ? "bg-blue-100 text-blue-600"
                            : "bg-gray-100 text-gray-900"
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

      {/* Tab Content */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {activeTab === "overview" && <OverviewTab vessel={vessel} onUpdate={loadVessel} />}
        {activeTab === "geometry" && <GeometryTab vesselId={vessel.id} vessel={vessel} />}
        {activeTab === "loadcases" && <LoadcasesTab vesselId={vessel.id} />}
        {activeTab === "computations" && <ComputationsTab vesselId={vessel.id} />}
        {activeTab === "curves" && <CurvesTab vesselId={vessel.id} />}
        {activeTab === "trim" && <TrimSolverTab vesselId={vessel.id} />}
      </div>
    </div>
  );
}

export default VesselDetail;
