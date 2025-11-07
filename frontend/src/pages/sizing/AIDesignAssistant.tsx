import React from "react";
import { observer } from "mobx-react-lite";
import { useNavigate } from "react-router-dom";
import { useStore } from "../../stores";
import { AppHeader } from "../../components/AppHeader";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { Button } from "../../components/ui/button";
import { Home, Sparkles, ArrowRight } from "lucide-react";

export const AIDesignAssistant: React.FC = observer(() => {
  const navigate = useNavigate();
  const { copilotStore, authStore } = useStore();

  const handleCreateMission = () => {
    if (!copilotStore.generatedMission) return;

    // Navigate to mission wizard with AI-generated data
    navigate("/sizing/mission/new", {
      state: { aiGeneratedMission: copilotStore.generatedMission },
    });
  };

  const handleReviewAndEdit = () => {
    if (!copilotStore.generatedMission) return;

    navigate("/sizing/mission/new", {
      state: {
        aiGeneratedMission: copilotStore.generatedMission,
        allowEdit: true,
      },
    });
  };

  const handleLogout = () => {
    authStore.logout();
    navigate("/login");
  };

  const handleHome = () => {
    navigate("/dashboard");
  };

  return (
    <div className="flex min-h-screen flex-col bg-gray-50 dark:bg-gray-900">
      <AppHeader
        left={
          <div className="flex items-center gap-2">
            <Sparkles className="w-5 h-5 text-blue-600" />
            <h1 className="text-xl font-semibold text-foreground">AI Design Assistant</h1>
          </div>
        }
        right={
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="sm" onClick={handleHome}>
              <Home className="h-4 w-4 md:mr-2" />
              <span className="hidden md:inline">Home</span>
            </Button>
            <UserProfileMenu onOpenSettings={() => {}} onLogout={handleLogout} />
          </div>
        }
      />

      <main className="flex-1 p-6">
        <div className="max-w-7xl mx-auto h-full">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 h-[calc(100vh-200px)]">
            {/* Chat Interface (Left 2/3) */}
            <div className="lg:col-span-2">
              {/* Temporary wrapper - CopilotPanel will be used as overlay */}
              <div className="bg-white rounded-lg shadow-lg h-full flex items-center justify-center p-8">
                <div className="text-center">
                  <Sparkles className="w-16 h-16 text-blue-600 mx-auto mb-4" />
                  <h2 className="text-2xl font-bold mb-2">AI Copilot Panel</h2>
                  <p className="text-gray-600 mb-6">
                    Click the Copilot button in the bottom-right corner to open the AI assistant, or
                    use the panel on the right side of your screen.
                  </p>
                </div>
              </div>
            </div>

            {/* Mission Preview (Right 1/3) */}
            <div className="bg-white rounded-lg shadow-lg p-6">
              <h3 className="text-lg font-semibold mb-4">Generated Mission</h3>

              {copilotStore.generatedMission ? (
                <div className="space-y-4">
                  <div>
                    <label className="text-sm text-gray-600">Mission Name</label>
                    <p className="font-medium">{copilotStore.generatedMission.name}</p>
                  </div>

                  <div>
                    <label className="text-sm text-gray-600">Vessel Type</label>
                    <p className="font-medium">{copilotStore.generatedMission.missionType}</p>
                  </div>

                  <div>
                    <label className="text-sm text-gray-600">Cargo</label>
                    <p className="font-medium">
                      {copilotStore.generatedMission.cargoValue.toLocaleString()}{" "}
                      {copilotStore.generatedMission.cargoBasis}
                    </p>
                  </div>

                  <div>
                    <label className="text-sm text-gray-600">Service Speed</label>
                    <p className="font-medium">
                      {copilotStore.generatedMission.serviceSpeedKn} knots
                    </p>
                  </div>

                  {copilotStore.generatedMission.capBeamM && (
                    <div>
                      <label className="text-sm text-gray-600">Max Beam</label>
                      <p className="font-medium">{copilotStore.generatedMission.capBeamM}m</p>
                    </div>
                  )}

                  {copilotStore.generatedMission.capDraftM && (
                    <div>
                      <label className="text-sm text-gray-600">Max Draft</label>
                      <p className="font-medium">{copilotStore.generatedMission.capDraftM}m</p>
                    </div>
                  )}

                  <div className="pt-4 space-y-2">
                    <button
                      onClick={handleCreateMission}
                      className="w-full px-4 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 transition-colors flex items-center justify-center gap-2"
                    >
                      Create Mission & Run Solver
                      <ArrowRight className="w-4 h-4" />
                    </button>

                    <button
                      onClick={handleReviewAndEdit}
                      className="w-full px-4 py-2 border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
                    >
                      Review & Edit Parameters
                    </button>
                  </div>
                </div>
              ) : (
                <div className="text-center text-gray-500 py-8">
                  <Sparkles className="w-12 h-12 text-gray-300 mx-auto mb-2" />
                  <p className="text-sm">Chat with Copilot to generate mission parameters</p>
                </div>
              )}
            </div>
          </div>
        </div>
      </main>

      {/* Note: CopilotPanel is now global and available on all pages */}
    </div>
  );
});
