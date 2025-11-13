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
import { Home } from "lucide-react";
import { inferProblematicStep } from "../../utils/diagnosticHelpers";
import { missionCaseToDto } from "../../utils/missionHelpers";

export const SizingRunResults: React.FC = observer(() => {
  const { runId } = useParams<{ runId: string }>();
  const navigate = useNavigate();
  const { sizingStore, authStore } = useStore();
  const [showComparison, setShowComparison] = useState(false);
  const [showSettings, setShowSettings] = useState(false);

  useEffect(() => {
    if (runId) {
      // Check if currentRun is set and matches the runId
      // If not, load both the run and candidates
      if (!sizingStore.currentRun || sizingStore.currentRun.id !== runId) {
        sizingStore.loadRunAndCandidates(runId);
      } else {
        // If run is already loaded, just ensure candidates are loaded
        if (sizingStore.candidates.length === 0) {
          sizingStore.loadCandidates(runId);
        }
      }
    }
  }, [runId, sizingStore]);

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
      <div className="flex min-h-screen flex-col bg-gray-50 dark:bg-gray-900">
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
                <h1 className="text-3xl font-bold text-gray-900 dark:text-white">
                  Hull Sizing Results
                </h1>
                <p className="mt-2 text-gray-600 dark:text-gray-400">
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
              <p className="mt-4 text-gray-600 dark:text-gray-400">
                Running first-principles solver...
              </p>
            </div>
          )}

          {/* Compute Time */}
          {sizingStore.currentRun && sizingStore.currentRun.computeTimeMs && (
            <div className="mb-6 rounded-lg bg-accent/10 p-4 text-sm text-accent-foreground border border-accent/20">
              Solver completed in {sizingStore.currentRun.computeTimeMs}ms
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
              {sizingStore.candidates.map((candidate, idx) => (
                <CandidateCard
                  key={candidate.id}
                  candidate={candidate}
                  rank={idx + 1}
                  onSelect={() => navigate(`/sizing/workspace/${candidate.id}`)}
                  onCompare={() => sizingStore.toggleCompareCandidate(candidate.id)}
                  isComparing={sizingStore.compareCandidates.some((c) => c.id === candidate.id)}
                />
              ))}
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
