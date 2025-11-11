import { observer } from "mobx-react-lite";
import { useEffect, useState } from "react";
import { useLocation, useParams, useNavigate } from "react-router-dom";
import { seakeepingStore } from "../../stores/SeakeepingStore";
import { Button } from "../../components/ui/button";
import { ArrowLeft, Home } from "lucide-react";
import { RaoSetupPanel } from "../../components/seakeeping/panels/RaoSetupPanel";
import { RaoChartsPanel } from "../../components/seakeeping/panels/RaoChartsPanel";
import { SeaStatePanel } from "../../components/seakeeping/panels/SeaStatePanel";
import { MotionResponsePanel } from "../../components/seakeeping/panels/MotionResponsePanel";
import { ExceedancePanel } from "../../components/seakeeping/panels/ExceedancePanel";
import { Enhanced3DPanel } from "../../components/seakeeping/panels/Enhanced3DPanel";
import { VesselSelectionDialog } from "../../components/seakeeping/VesselSelectionDialog";
import { AppHeader } from "../../components/AppHeader";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { UserSettingsDialog } from "../../components/UserSettingsDialog";
import { useStore } from "../../stores";

type TabType = "raos" | "motion" | "exceedance" | "3d";

export const SeakeepingWorkspace = observer(() => {
  const location = useLocation();
  const { vesselId } = useParams();
  const navigate = useNavigate();
  const { authStore } = useStore();
  const [activeTab, setActiveTab] = useState<TabType>("raos");
  const [showVesselPicker, setShowVesselPicker] = useState(false);
  const [showSettings, setShowSettings] = useState(false);

  useEffect(() => {
    // Load vessel snapshot from routing state or fetch by ID
    if (location.state?.vesselSnapshot) {
      seakeepingStore.setVesselSnapshot(location.state.vesselSnapshot);
    } else if (vesselId) {
      // TODO: Fetch vessel data by ID
      console.log("Fetch vessel data for", vesselId);
    }

    // Cleanup on unmount
    return () => {
      seakeepingStore.reset();
    };
  }, [vesselId, location.state]);

  const vessel = seakeepingStore.vesselSnapshot;

  if (!vessel) {
    return (
      <>
        <div className="flex h-screen items-center justify-center bg-background">
          <div className="text-center space-y-4">
            <svg
              className="mx-auto h-16 w-16 text-cyan-600"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              strokeWidth={2}
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                d="M20 7c-2 0-4 2-4 4s2 4 4 4M4 7c2 0 4 2 4 4s-2 4-4 4M12 3v6M12 15v6M9 12h6"
              />
            </svg>
            <h2 className="text-2xl font-semibold">Seakeeping Analysis</h2>
            <p className="text-muted-foreground">
              Select a vessel from Hydrostatics to analyze its seakeeping performance
            </p>
            <div className="flex flex-col sm:flex-row gap-3 justify-center mt-6">
              <Button onClick={() => setShowVesselPicker(true)} size="lg">
                <svg
                  className="h-5 w-5 mr-2"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                  strokeWidth={2}
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    d="M20 7c-2 0-4 2-4 4s2 4 4 4M4 7c2 0 4 2 4 4s-2 4-4 4M12 3v6M12 15v6M9 12h6"
                  />
                </svg>
                Select Vessel
              </Button>
              <Button onClick={() => navigate("/hydrostatics/vessels")} variant="outline" size="lg">
                Go to Hydrostatics
              </Button>
              <Button onClick={() => navigate("/dashboard")} variant="outline" size="lg">
                <Home className="h-5 w-5 mr-2" />
                Dashboard
              </Button>
            </div>
          </div>
        </div>
        {showVesselPicker && <VesselSelectionDialog onClose={() => setShowVesselPicker(false)} />}
      </>
    );
  }

  const handleLogout = () => {
    authStore.logout();
    navigate("/login");
  };

  return (
    <>
      {/* System Rail */}
      <AppHeader
        left={
          <>
            <Button variant="ghost" size="sm" onClick={() => navigate("/dashboard")}>
              <Home className="h-4 w-4 mr-2" />
              Dashboard
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={() =>
                vesselId
                  ? navigate(`/hydrostatics/vessels/${vesselId}/workspace`)
                  : navigate("/hydrostatics/vessels")
              }
            >
              <ArrowLeft className="h-4 w-4 mr-2" />
              Hydrostatics
            </Button>
            <div className="flex items-center gap-2 pl-2 border-l border-border">
              <svg
                className="h-5 w-5 text-cyan-600"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                strokeWidth={2}
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M20 7c-2 0-4 2-4 4s2 4 4 4M4 7c2 0 4 2 4 4s-2 4-4 4M12 3v6M12 15v6M9 12h6"
                />
              </svg>
              <div>
                <h1 className="text-sm font-semibold">Seakeeping</h1>
                <p className="text-xs text-muted-foreground">{vessel.name}</p>
              </div>
            </div>
          </>
        }
        right={
          <UserProfileMenu onLogout={handleLogout} onOpenSettings={() => setShowSettings(true)} />
        }
      />

      <div className="flex h-[calc(100vh-3.5rem)] bg-background">
        {/* Sidebar */}
        <div className="w-80 border-r border-border bg-card flex flex-col">
          {/* Vessel Info */}
          <div className="p-4 border-b border-border">
            <h2 className="text-lg font-semibold">Vessel Properties</h2>
            <div className="mt-3 text-sm space-y-2 text-muted-foreground">
              <div className="flex justify-between">
                <span>Lpp:</span>
                <span className="font-medium text-foreground">{vessel.lpp.toFixed(2)} m</span>
              </div>
              <div className="flex justify-between">
                <span>Beam:</span>
                <span className="font-medium text-foreground">{vessel.beam.toFixed(2)} m</span>
              </div>
              <div className="flex justify-between">
                <span>Draft:</span>
                <span className="font-medium text-foreground">{vessel.draft.toFixed(2)} m</span>
              </div>
            </div>
          </div>

        {/* Setup Panels */}
        <div className="flex-1 overflow-y-auto">
          <RaoSetupPanel />
          <SeaStatePanel />
        </div>
      </div>

      {/* Main workspace */}
      <div className="flex-1 flex flex-col">
        {/* Tab Navigation */}
        <div className="border-b border-border bg-card">
          <div className="flex space-x-1 p-2">
            <TabButton
              label="RAOs"
              active={activeTab === "raos"}
              onClick={() => setActiveTab("raos")}
            />
            <TabButton
              label="Motion Response"
              active={activeTab === "motion"}
              onClick={() => setActiveTab("motion")}
              disabled={!seakeepingStore.raoResults}
            />
            <TabButton
              label="Exceedance"
              active={activeTab === "exceedance"}
              onClick={() => setActiveTab("exceedance")}
              disabled={!seakeepingStore.motionResponse}
            />
            <TabButton
              label="3D View"
              active={activeTab === "3d"}
              onClick={() => setActiveTab("3d")}
            />
          </div>
        </div>

        {/* Tab Content */}
        <div className="flex-1 overflow-auto p-6">
          {activeTab === "raos" && <RaoChartsPanel />}
          {activeTab === "motion" && <MotionResponsePanel />}
          {activeTab === "exceedance" && <ExceedancePanel />}
          {activeTab === "3d" && <Enhanced3DPanel />}
        </div>
      </div>
      </div>

      {/* Settings Dialog */}
      <UserSettingsDialog isOpen={showSettings} onClose={() => setShowSettings(false)} />
    </>
  );
});

function TabButton({
  label,
  active,
  onClick,
  disabled,
}: {
  label: string;
  active: boolean;
  onClick: () => void;
  disabled?: boolean;
}) {
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className={`
        px-4 py-2 rounded-md text-sm font-medium transition-colors
        ${
          active
            ? "bg-primary text-primary-foreground"
            : "text-muted-foreground hover:text-foreground hover:bg-muted"
        }
        ${disabled ? "opacity-50 cursor-not-allowed" : "cursor-pointer"}
      `}
    >
      {label}
    </button>
  );
}
