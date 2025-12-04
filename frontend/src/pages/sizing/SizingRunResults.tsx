import React, { useEffect, useState } from "react";
import { observer } from "mobx-react-lite";
import { useParams, useNavigate } from "react-router-dom";
import { useStore } from "../../stores";
import { Footer } from "../../components/Footer";
import { CandidateCard } from "../../components/sizing/CandidateCard";
import { ComparisonView } from "../../components/sizing/ComparisonView";
import { EmptyResultsPanel } from "../../components/sizing/EmptyResultsPanel";
import { Button } from "../../components/ui/button";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { UserSettingsDialog } from "../../components/UserSettingsDialog";
import { AppHeader } from "../../components/AppHeader";
import { Home, Waves, Grid3x3, Droplet, Circle, Layers, Eye, EyeOff } from "lucide-react";
import { inferProblematicStep } from "../../utils/diagnosticHelpers";
import { missionCaseToDto } from "../../utils/missionHelpers";
import type { Hull3DVisualizationOptions } from "../../components/sizing/visualization/Hull3DThumbnail";

export const SizingRunResults: React.FC = observer(() => {
  const { runId } = useParams<{ runId: string }>();
  const navigate = useNavigate();
  const { sizingStore, authStore } = useStore();
  const [showComparison, setShowComparison] = useState(false);
  const [showSettings, setShowSettings] = useState(false);

  // Global visualization options (applied to all candidate cards)
  const [globalVizOptions, setGlobalVizOptions] = useState<Hull3DVisualizationOptions>({
    showWaterlines: true,
    showButtocks: false,
    showSections: false,
    showWireframe: false,
    showWaterplane: false,
    showCenters: false,
  });

  const [showGlobalControls, setShowGlobalControls] = useState(false);

  useEffect(() => {
    if (!runId) return;

    // Check if currentRun is set and matches the runId
    // If not, load both the run and candidates
    if (!sizingStore.currentRun || sizingStore.currentRun.id !== runId) {
      sizingStore.loadRunAndCandidates(runId);
    } else {
      // If run is already loaded, just ensure candidates are loaded
      if (sizingStore.candidates.length === 0) {
        sizingStore.loadCandidates(runId);
      }
      // Safety check: Ensure mission case is loaded if run has missionCaseId
      if (
        sizingStore.currentRun.missionCaseId &&
        sizingStore.selectedMission?.id !== sizingStore.currentRun.missionCaseId
      ) {
        sizingStore.selectMission(sizingStore.currentRun.missionCaseId);
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [runId]); // Only depend on runId to avoid re-renders on store updates

  const handleHome = () => {
    navigate("/dashboard");
  };

  const handleLogout = () => {
    authStore.logout();
    navigate("/login");
  };

  // Show comparison view if active
  if (showComparison && sizingStore.compareCandidates.length >= 2) {
    return (
      <div className="flex min-h-screen flex-col bg-background">
        <AppHeader
          left={<h1 className="text-xl font-semibold text-foreground">Hull Sizing - Comparison</h1>}
          right={
            <div className="flex items-center gap-2">
              <Button variant="ghost" size="sm" onClick={handleHome}>
                <Home className="h-4 w-4 md:mr-2" />
                <span className="hidden md:inline">Home</span>
              </Button>
              <UserProfileMenu
                onOpenSettings={() => setShowSettings(true)}
                onLogout={handleLogout}
              />
            </div>
          }
        />
        <main className="flex-1">
          <ComparisonView
            candidates={sizingStore.compareCandidates}
            onClose={() => setShowComparison(false)}
          />
        </main>
        <Footer />
        <UserSettingsDialog isOpen={showSettings} onClose={() => setShowSettings(false)} />
      </div>
    );
  }

  return (
    <div className="flex min-h-screen flex-col bg-gray-50 dark:bg-gray-900">
      <AppHeader
        left={<h1 className="text-xl font-semibold text-foreground">Hull Sizing - Results</h1>}
        right={
          <div className="flex items-center gap-2">
            <Button variant="ghost" size="sm" onClick={handleHome}>
              <Home className="h-4 w-4 md:mr-2" />
              <span className="hidden md:inline">Home</span>
            </Button>
            <UserProfileMenu onOpenSettings={() => setShowSettings(true)} onLogout={handleLogout} />
          </div>
        }
      />

      <main className="flex-1 py-8">
        <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
          {/* Header */}
          <div className="mb-8">
            <div className="flex items-center justify-between">
              <div>
                <h1 className="text-3xl font-bold text-foreground">Hull Sizing Results</h1>
                <p className="mt-2 text-muted-foreground">
                  {sizingStore.candidates.length} candidate designs generated
                </p>
              </div>
              <Button variant="outline" onClick={() => navigate("/sizing/missions")}>
                ← Back to Briefs
              </Button>
            </div>
          </div>

          {/* Loading State */}
          {sizingStore.isLoading && (
            <div className="text-center py-12">
              <div className="animate-spin h-8 w-8 border-4 border-blue-600 border-t-transparent rounded-full mx-auto"></div>
              <p className="mt-4 text-muted-foreground">Running first-principles solver...</p>
            </div>
          )}

          {/* Compute Time */}
          {sizingStore.currentRun && sizingStore.currentRun.computeTimeMs && (
            <div className="mb-6 rounded-lg bg-accent/10 p-4 text-sm text-accent-foreground border border-accent/20">
              Solver completed in {sizingStore.currentRun.computeTimeMs}ms
            </div>
          )}

          {/* Global Visualization Controls Toolbar */}
          {!sizingStore.isLoading && sizingStore.candidates.length > 0 && (
            <div className="mb-6 rounded-lg border border-border bg-card shadow-sm">
              <button
                onClick={() => setShowGlobalControls(!showGlobalControls)}
                className="w-full px-4 py-3 flex items-center justify-between hover:bg-accent/50 transition-colors"
              >
                <div className="flex items-center gap-2">
                  {showGlobalControls ? (
                    <EyeOff className="h-4 w-4" />
                  ) : (
                    <Eye className="h-4 w-4" />
                  )}
                  <span className="text-sm font-medium text-foreground">
                    3D Display Options {showGlobalControls ? "(Hide)" : "(Show)"}
                  </span>
                </div>
                <span className="text-xs text-muted-foreground">
                  Apply to all {sizingStore.candidates.length} candidates
                </span>
              </button>

              {showGlobalControls && (
                <div className="px-4 py-3 border-t border-border">
                  <div className="flex flex-wrap items-center gap-2">
                    <Button
                      size="sm"
                      variant={globalVizOptions.showWaterlines ? "default" : "outline"}
                      onClick={() =>
                        setGlobalVizOptions({
                          ...globalVizOptions,
                          showWaterlines: !globalVizOptions.showWaterlines,
                        })
                      }
                      className="text-xs"
                    >
                      <Waves className="h-3.5 w-3.5 mr-1.5" />
                      Waterlines
                    </Button>
                    <Button
                      size="sm"
                      variant={globalVizOptions.showWireframe ? "default" : "outline"}
                      onClick={() =>
                        setGlobalVizOptions({
                          ...globalVizOptions,
                          showWireframe: !globalVizOptions.showWireframe,
                        })
                      }
                      className="text-xs"
                    >
                      <Grid3x3 className="h-3.5 w-3.5 mr-1.5" />
                      Wireframe
                    </Button>
                    <Button
                      size="sm"
                      variant={globalVizOptions.showButtocks ? "default" : "outline"}
                      onClick={() =>
                        setGlobalVizOptions({
                          ...globalVizOptions,
                          showButtocks: !globalVizOptions.showButtocks,
                        })
                      }
                      className="text-xs"
                    >
                      <Layers className="h-3.5 w-3.5 mr-1.5" />
                      Buttocks
                    </Button>
                    <Button
                      size="sm"
                      variant={globalVizOptions.showWaterplane ? "default" : "outline"}
                      onClick={() =>
                        setGlobalVizOptions({
                          ...globalVizOptions,
                          showWaterplane: !globalVizOptions.showWaterplane,
                        })
                      }
                      className="text-xs"
                    >
                      <Droplet className="h-3.5 w-3.5 mr-1.5" />
                      Waterplane
                    </Button>
                    <Button
                      size="sm"
                      variant={globalVizOptions.showCenters ? "default" : "outline"}
                      onClick={() =>
                        setGlobalVizOptions({
                          ...globalVizOptions,
                          showCenters: !globalVizOptions.showCenters,
                        })
                      }
                      className="text-xs"
                    >
                      <Circle className="h-3.5 w-3.5 mr-1.5" />
                      Centers
                    </Button>
                    <div className="ml-auto text-xs text-muted-foreground">
                      Hover over each card for individual controls
                    </div>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Empty State - No Candidates */}
          {!sizingStore.isLoading && sizingStore.candidates.length === 0 && (
            <EmptyResultsPanel
              diagnostics={sizingStore.currentRun?.diagnostics}
              onAdjustParameters={async () => {
                // Navigate back to wizard with the mission case to edit
                if (sizingStore.currentRun) {
                  await sizingStore.selectMission(sizingStore.currentRun.missionCaseId);

                  if (sizingStore.selectedMission) {
                    // Infer which step is likely causing issues
                    const initialStep = inferProblematicStep(sizingStore.currentRun.diagnostics);

                    // Convert mission case to DTO for form editing
                    const missionData = missionCaseToDto(sizingStore.selectedMission);

                    // Serialize diagnostics to avoid DataCloneError with complex objects
                    const diagnosticsData = sizingStore.currentRun.diagnostics
                      ? JSON.parse(JSON.stringify(sizingStore.currentRun.diagnostics))
                      : null;

                    // Navigate with state to restore form and context
                    navigate(`/sizing/wizard`, {
                      state: {
                        editingMission: missionData,
                        missionCaseId: sizingStore.currentRun.missionCaseId, // Pass the ID to reuse the mission
                        solverMode: sizingStore.currentRun.mode,
                        initialStep,
                        diagnostics: diagnosticsData,
                        isAdjustingAfterFailure: true,
                      },
                    });
                  }
                }
              }}
              onCloneBrief={async () => {
                // Clone the brief and navigate to missions list
                if (sizingStore.currentRun) {
                  try {
                    // Get the original brief to find its name
                    const originalBrief = sizingStore.missionCases.find(
                      (mc) => mc.id === sizingStore.currentRun!.missionCaseId
                    );

                    if (!originalBrief) {
                      console.error("Original brief not found");
                      return;
                    }

                    // Generate a unique name with timestamp
                    const existingNames = sizingStore.missionCases.map((mc) => mc.name);
                    const baseName = originalBrief.name;
                    let newName = `${baseName} - Copy`;
                    let counter = 2;

                    // Find a unique name
                    while (
                      existingNames.some((name) => name.toLowerCase() === newName.toLowerCase())
                    ) {
                      newName = `${baseName} - Copy ${counter}`;
                      counter++;
                    }

                    await sizingStore.cloneMissionCase(
                      sizingStore.currentRun.missionCaseId,
                      newName
                    );
                    navigate(`/sizing/missions`);
                  } catch (error) {
                    console.error("Failed to clone brief:", error);
                  }
                }
              }}
            />
          )}

          {/* Candidates Grid */}
          {!sizingStore.isLoading && sizingStore.candidates.length > 0 && (
            <div className="grid grid-cols-1 gap-6 lg:grid-cols-2 xl:grid-cols-3">
              {sizingStore.candidates.map((candidate, idx) => {
                const isComparing = sizingStore.compareCandidates.some(
                  (c) => c.id === candidate.id
                );
                return (
                  <CandidateCard
                    key={candidate.id}
                    candidate={candidate}
                    rank={idx + 1}
                    onSelect={() => navigate(`/sizing/workspace/${candidate.id}`)}
                    onCompare={() => sizingStore.toggleCompareCandidate(candidate.id)}
                    isComparing={isComparing}
                    globalVisualizationOptions={showGlobalControls ? globalVizOptions : undefined}
                  />
                );
              })}
            </div>
          )}

          {/* Compare Mode Actions */}
          {sizingStore.compareMode && (
            <div className="fixed bottom-8 left-1/2 -translate-x-1/2 rounded-lg bg-primary p-4 shadow-lg">
              <div className="flex items-center space-x-4">
                <span className="text-primary-foreground font-medium">
                  {sizingStore.compareCandidates.length} selected for comparison
                </span>
                <Button
                  variant="outline"
                  className="bg-primary-foreground text-primary hover:bg-primary-foreground/90"
                  onClick={() => sizingStore.clearComparison()}
                >
                  Clear
                </Button>
                <Button
                  variant="secondary"
                  onClick={() => setShowComparison(true)}
                  disabled={sizingStore.compareCandidates.length < 2}
                >
                  Compare ({sizingStore.compareCandidates.length})
                </Button>
              </div>
            </div>
          )}

          {/* Error State */}
          {sizingStore.error && (
            <div className="rounded-lg bg-destructive/10 p-4 text-destructive border border-destructive/20">
              {sizingStore.error}
            </div>
          )}
        </div>
      </main>

      <Footer />

      {/* Settings Dialog */}
      <UserSettingsDialog isOpen={showSettings} onClose={() => setShowSettings(false)} />
    </div>
  );
});
