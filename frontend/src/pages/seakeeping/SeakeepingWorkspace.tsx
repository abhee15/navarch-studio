import { observer } from "mobx-react-lite";
import { useEffect, useState } from "react";
import { useLocation, useParams, useNavigate } from "react-router-dom";
import { seakeepingStore } from "../../stores/SeakeepingStore";
import { Button } from "../../components/ui/button";
import { ArrowLeft } from "lucide-react";
import { RaoSetupPanel } from "../../components/seakeeping/panels/RaoSetupPanel";
import { RaoChartsPanel } from "../../components/seakeeping/panels/RaoChartsPanel";
import { SeaStatePanel } from "../../components/seakeeping/panels/SeaStatePanel";
import { MotionResponsePanel } from "../../components/seakeeping/panels/MotionResponsePanel";
import { ExceedancePanel } from "../../components/seakeeping/panels/ExceedancePanel";
import { Enhanced3DPanel } from "../../components/seakeeping/panels/Enhanced3DPanel";
import { VesselSelectionDialog } from "../../components/seakeeping/VesselSelectionDialog";

type TabType = "raos" | "motion" | "exceedance" | "3d";

export const SeakeepingWorkspace = observer(() => {
  const location = useLocation();
  const { vesselId } = useParams();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<TabType>("raos");
  const [showVesselPicker, setShowVesselPicker] = useState(false);

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
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M14.828 14.828a4 4 0 01-5.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
            <h2 className="text-2xl font-semibold">Seakeeping Analysis</h2>
            <p className="text-muted-foreground">
              Select a vessel from Hydrostatics to analyze its seakeeping performance
            </p>
            <div className="flex flex-col sm:flex-row gap-3 justify-center mt-6">
              <Button onClick={() => setShowVesselPicker(true)} size="lg">
                <svg className="h-5 w-5 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"
                  />
                </svg>
                Select Vessel
              </Button>
              <Button onClick={() => navigate("/hydrostatics")} variant="outline" size="lg">
                Go to Hydrostatics
              </Button>
            </div>
          </div>
        </div>
        {showVesselPicker && <VesselSelectionDialog onClose={() => setShowVesselPicker(false)} />}
      </>
    );
  }

  return (
    <div className="flex h-screen bg-background">
      {/* Sidebar */}
      <div className="w-80 border-r border-border bg-card flex flex-col">
        {/* Header */}
        <div className="p-4 border-b border-border">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => navigate("/hydrostatics")}
            className="mb-2"
          >
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back
          </Button>
          <h1 className="text-xl font-bold">Seakeeping Analysis</h1>
          <p className="text-sm text-muted-foreground mt-1">{vessel.name}</p>
          <div className="mt-2 text-xs space-y-1 text-muted-foreground">
            <div>Lpp: {vessel.lpp.toFixed(2)} m</div>
            <div>Beam: {vessel.beam.toFixed(2)} m</div>
            <div>Draft: {vessel.draft.toFixed(2)} m</div>
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
