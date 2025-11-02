import { useState, useEffect } from "react";
import { useParams, useNavigate, useSearchParams } from "react-router-dom";
import { observer } from "mobx-react-lite";
import { comparisonApi } from "../../services/comparisonApi";
import { vesselsApi } from "../../services/hydrostaticsApi";
import { getErrorMessage } from "../../types/errors";
import type { ComparisonSnapshot, ComparisonReport } from "../../types/comparison";
import type { VesselDetails } from "../../types/hydrostatics";
import { ComparisonSideBySidePanel } from "../../components/hydrostatics/comparison/ComparisonSideBySidePanel";
import { ComparisonOverlayCharts } from "../../components/hydrostatics/comparison/ComparisonOverlayCharts";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { UserSettingsDialog } from "../../components/UserSettingsDialog";
import { useStore } from "../../stores";

export const ComparisonWorkspace = observer(function ComparisonWorkspace() {
  const { vesselId } = useParams<{ vesselId: string }>();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { authStore } = useStore();

  const [vessel, setVessel] = useState<VesselDetails | null>(null);
  const [snapshots, setSnapshots] = useState<ComparisonSnapshot[]>([]);
  const [baselineId, setBaselineId] = useState<string | null>(searchParams.get("baseline"));
  const [candidateId, setCandidateId] = useState<string | null>(searchParams.get("candidate"));
  const [comparisonReport, setComparisonReport] = useState<ComparisonReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [comparing, setComparing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showSettings, setShowSettings] = useState(false);
  const [activeView, setActiveView] = useState<"side-by-side" | "charts">("side-by-side");

  useEffect(() => {
    loadData();
  }, [vesselId]);

  useEffect(() => {
    if (baselineId && candidateId) {
      compareRuns();
    }
  }, [baselineId, candidateId]);

  const loadData = async () => {
    if (!vesselId) return;

    try {
      setLoading(true);
      setError(null);

      const [vesselData, snapshotsData] = await Promise.all([
        vesselsApi.get(vesselId),
        comparisonApi.getSnapshots(vesselId),
      ]);

      setVessel(vesselData);
      setSnapshots(snapshotsData);

      // Auto-select baseline if one exists and none selected
      if (!baselineId && snapshotsData.length > 0) {
        const baseline = snapshotsData.find((s) => s.isBaseline) || snapshotsData[0];
        setBaselineId(baseline.id);
      }
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  const compareRuns = async () => {
    if (!baselineId || !candidateId) return;

    try {
      setComparing(true);
      const report = await comparisonApi.compareSnapshots({
        baselineSnapshotId: baselineId,
        candidateSnapshotId: candidateId,
      });
      setComparisonReport(report);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setComparing(false);
    }
  };

  const handleSetAsBaseline = async (snapshotId: string) => {
    try {
      await comparisonApi.updateSnapshot(snapshotId, { isBaseline: true });
      await loadData();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const handleBack = () => {
    navigate(`/hydrostatics/vessels/${vesselId}`);
  };

  const handleHome = () => {
    navigate("/dashboard");
  };

  const handleLogout = () => {
    authStore.logout();
    navigate("/login");
  };

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
            <h3 className="mt-2 text-lg font-medium text-foreground">Error</h3>
            <p className="mt-1 text-sm text-muted-foreground">{error || "Vessel not found"}</p>
            <button
              onClick={handleBack}
              className="mt-4 inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-primary-foreground bg-primary hover:bg-primary/90"
            >
              ← Back to vessel
            </button>
          </div>
        </div>
      </div>
    );
  }

  const baseline = snapshots.find((s) => s.id === baselineId);
  const candidate = snapshots.find((s) => s.id === candidateId);

  return (
    <div className="h-screen flex flex-col bg-background overflow-hidden">
      {/* Header */}
      <header className="border-b border-border bg-card/80 backdrop-blur-sm flex-shrink-0">
        <div className="px-4 py-2">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <button
                onClick={handleBack}
                className="text-muted-foreground hover:text-foreground"
                title="Back to vessel"
              >
                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M10 19l-7-7m0 0l7-7m-7 7h18"
                  />
                </svg>
              </button>
              <div>
                <h1 className="text-lg font-bold text-foreground">Comparison Mode</h1>
                <p className="text-xs text-muted-foreground">{vessel.name}</p>
              </div>
            </div>
            <div className="flex items-center space-x-2">
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
              <UserProfileMenu
                onOpenSettings={() => setShowSettings(true)}
                onLogout={handleLogout}
              />
            </div>
          </div>
        </div>
      </header>

      {/* Main content */}
      <div className="flex-1 overflow-hidden flex flex-col">
        {/* Selection Bar */}
        <div className="bg-card border-b border-border p-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Baseline Selection */}
            <div>
              <label className="block text-sm font-medium text-foreground mb-2">Baseline Run</label>
              <select
                value={baselineId || ""}
                onChange={(e) => setBaselineId(e.target.value)}
                className="w-full px-3 py-2 border border-border rounded-md bg-background text-foreground text-sm"
              >
                <option value="">Select baseline...</option>
                {snapshots.map((snapshot) => (
                  <option key={snapshot.id} value={snapshot.id}>
                    {snapshot.runName}
                    {snapshot.isBaseline && " (Baseline)"}
                    {" - "}
                    {new Date(snapshot.createdAt).toLocaleDateString()}
                  </option>
                ))}
              </select>
            </div>

            {/* Candidate Selection */}
            <div>
              <label className="block text-sm font-medium text-foreground mb-2">
                Candidate Run
              </label>
              <select
                value={candidateId || ""}
                onChange={(e) => setCandidateId(e.target.value)}
                className="w-full px-3 py-2 border border-border rounded-md bg-background text-foreground text-sm"
              >
                <option value="">Select candidate...</option>
                {snapshots.map((snapshot) => (
                  <option key={snapshot.id} value={snapshot.id}>
                    {snapshot.runName}
                    {snapshot.isBaseline && " (Baseline)"}
                    {" - "}
                    {new Date(snapshot.createdAt).toLocaleDateString()}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* Action Bar */}
          {baseline && candidate && (
            <div className="mt-4 flex items-center justify-between">
              <div className="flex space-x-2">
                <button
                  onClick={() => setActiveView("side-by-side")}
                  className={`px-3 py-1.5 text-xs font-medium rounded ${
                    activeView === "side-by-side"
                      ? "bg-primary text-primary-foreground"
                      : "bg-muted text-muted-foreground hover:bg-muted/80"
                  }`}
                >
                  Side-by-Side
                </button>
                <button
                  onClick={() => setActiveView("charts")}
                  className={`px-3 py-1.5 text-xs font-medium rounded ${
                    activeView === "charts"
                      ? "bg-primary text-primary-foreground"
                      : "bg-muted text-muted-foreground hover:bg-muted/80"
                  }`}
                >
                  Overlay Charts
                </button>
              </div>

              <div className="flex space-x-2">
                {!candidate.isBaseline && (
                  <button
                    onClick={() => handleSetAsBaseline(candidate.id)}
                    className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-foreground border border-border rounded hover:bg-accent/10"
                  >
                    Set Candidate as Baseline
                  </button>
                )}
                <button
                  onClick={() => {
                    /* Export PDF - TODO */
                  }}
                  className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-primary-foreground bg-primary rounded hover:bg-primary/90"
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
                      d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                    />
                  </svg>
                  Export PDF
                </button>
              </div>
            </div>
          )}
        </div>

        {/* Comparison Display */}
        <div className="flex-1 overflow-auto p-4">
          {comparing && (
            <div className="flex items-center justify-center h-full">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
            </div>
          )}

          {!comparing && !comparisonReport && baseline && candidate && (
            <div className="flex items-center justify-center h-full">
              <p className="text-muted-foreground">Loading comparison...</p>
            </div>
          )}

          {!comparing && comparisonReport && activeView === "side-by-side" && (
            <ComparisonSideBySidePanel report={comparisonReport} />
          )}

          {!comparing && comparisonReport && activeView === "charts" && (
            <ComparisonOverlayCharts report={comparisonReport} />
          )}

          {!baseline || !candidate ? (
            <div className="flex items-center justify-center h-full">
              <div className="text-center">
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
                    d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
                  />
                </svg>
                <h3 className="mt-2 text-sm font-medium text-foreground">No Comparison</h3>
                <p className="mt-1 text-xs text-muted-foreground">
                  Select both a baseline and candidate run to compare
                </p>
              </div>
            </div>
          ) : null}
        </div>
      </div>

      {/* Settings Dialog */}
      <UserSettingsDialog isOpen={showSettings} onClose={() => setShowSettings(false)} />
    </div>
  );
});

export default ComparisonWorkspace;
