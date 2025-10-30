import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { observer } from "mobx-react-lite";
import { vesselsApi } from "../../services/hydrostaticsApi";
import { getErrorMessage } from "../../types/errors";
import type { VesselDetails } from "../../types/hydrostatics";
import { WorkspaceLayout } from "../../components/hydrostatics/workspace/WorkspaceLayout";
import { useStore } from "../../stores";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { UserSettingsDialog } from "../../components/UserSettingsDialog";

export const VesselWorkspace = observer(function VesselWorkspace() {
  const { vesselId } = useParams<{ vesselId: string }>();
  const navigate = useNavigate();
  const { authStore } = useStore();
  const [vessel, setVessel] = useState<VesselDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showSettings, setShowSettings] = useState(false);

  const loadVessel = async () => {
    if (!vesselId) return;

    try {
      setLoading(true);
      setError(null);
      const data = await vesselsApi.get(vesselId);
      setVessel(data);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadVessel();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [vesselId]);

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
              onClick={() => navigate("/hydrostatics/vessels")}
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

      {/* Workspace Content */}
      <WorkspaceLayout vessel={vessel} onBack={handleBack} onVesselUpdated={loadVessel} />

      {/* Settings Dialog */}
      <UserSettingsDialog isOpen={showSettings} onClose={() => setShowSettings(false)} />
    </div>
  );
});

export default VesselWorkspace;
