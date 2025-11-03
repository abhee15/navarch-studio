import React, { useEffect, useState } from "react";
import { observer } from "mobx-react-lite";
import { useParams, useNavigate } from "react-router-dom";
import { useStore } from "../../stores";
import { AppHeader } from "../../components/AppHeader";
import { Footer } from "../../components/Footer";
import { CandidateCard } from "../../components/sizing/CandidateCard";
import { ComparisonView } from "../../components/sizing/ComparisonView";
import { Button } from "../../components/ui/button";

export const SizingRunResults: React.FC = observer(() => {
  const { runId } = useParams<{ runId: string }>();
  const navigate = useNavigate();
  const { sizingStore } = useStore();
  const [showComparison, setShowComparison] = useState(false);

  useEffect(() => {
    if (runId) {
      sizingStore.loadCandidates(runId);
    }
  }, [runId, sizingStore]);

  // Show comparison view if active
  if (showComparison && sizingStore.compareCandidates.length >= 2) {
    return (
      <div className="flex min-h-screen flex-col bg-gray-50 dark:bg-gray-900">
        <AppHeader />
        <main className="flex-1">
          <ComparisonView
            candidates={sizingStore.compareCandidates}
            onClose={() => setShowComparison(false)}
          />
        </main>
        <Footer />
      </div>
    );
  }

  return (
    <div className="flex min-h-screen flex-col bg-gray-50 dark:bg-gray-900">
      <AppHeader />

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
                ← Back to Missions
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
            <div className="mb-6 rounded-lg bg-green-50 p-4 text-sm text-green-800 dark:bg-green-900/20 dark:text-green-400">
              ✅ Solver completed in {sizingStore.currentRun.computeTimeMs}ms
            </div>
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
            <div className="fixed bottom-8 left-1/2 -translate-x-1/2 rounded-lg bg-blue-600 p-4 shadow-lg">
              <div className="flex items-center space-x-4">
                <span className="text-white font-medium">
                  {sizingStore.compareCandidates.length} selected for comparison
                </span>
                <Button
                  variant="outline"
                  className="bg-white text-blue-600 hover:bg-gray-100"
                  onClick={() => sizingStore.clearComparison()}
                >
                  Clear
                </Button>
                <Button
                  className="bg-blue-800 hover:bg-blue-900"
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
            <div className="rounded-lg bg-red-50 p-4 text-red-800 dark:bg-red-900/20 dark:text-red-400">
              {sizingStore.error}
            </div>
          )}
        </div>
      </main>

      <Footer />
    </div>
  );
});
