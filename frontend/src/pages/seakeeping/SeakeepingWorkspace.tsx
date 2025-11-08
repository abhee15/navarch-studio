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

type TabType = "raos" | "motion" | "exceedance" | "3d";

export const SeakeepingWorkspace = observer(() => {
  const location = useLocation();
  const { vesselId } = useParams();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<TabType>("raos");

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
      <div className="flex h-screen items-center justify-center bg-background">
        <div className="text-center space-y-4">
          <h2 className="text-2xl font-semibold">No Vessel Selected</h2>
          <p className="text-muted-foreground">
            Please select a vessel from Hydrostatics to analyze
          </p>
          <Button onClick={() => navigate("/hydrostatics")}>Go to Hydrostatics</Button>
        </div>
      </div>
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
