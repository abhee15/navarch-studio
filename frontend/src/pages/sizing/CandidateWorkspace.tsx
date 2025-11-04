import React, { useEffect, useState } from "react";
import { observer } from "mobx-react-lite";
import { useParams, useNavigate } from "react-router-dom";
import { useStore } from "../../stores";
import { Footer } from "../../components/Footer";
import { Button } from "../../components/ui/button";
import { ViewportQuadLayout } from "../../components/sizing/visualization/ViewportQuadLayout";
import { CompactHUD } from "../../components/sizing/workspace/CompactHUD";
import { OffsetsTable } from "../../components/sizing/workspace/OffsetsTable";
import { ParameterSliders } from "../../components/sizing/workspace/ParameterSliders";
import { ResistanceCurvePanel } from "../../components/sizing/workspace/ResistanceCurvePanel";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { UserSettingsDialog } from "../../components/UserSettingsDialog";
import { adjustParameter } from "../../services/sizingApi";
import type { CandidateDesign } from "../../types/sizing";
import { AppHeader } from "../../components/AppHeader";
import { BarChart3, Ruler, Ship, Zap, Home } from "lucide-react";

export const CandidateWorkspace: React.FC = observer(() => {
  const { candidateId } = useParams<{ candidateId: string }>();
  const navigate = useNavigate();
  const { sizingStore, authStore } = useStore();
  const [activeTab, setActiveTab] = useState<"kpi" | "offsets">("kpi");
  const [showSettings, setShowSettings] = useState(false);
  const [isAdjusting, setIsAdjusting] = useState(false);

  const candidate = sizingStore.selectedCandidate;

  useEffect(() => {
    if (candidateId && (!candidate || candidate.id !== candidateId)) {
      // Load candidate details
      // For now, candidate should already be in store from results page
      const found = sizingStore.candidates.find((c) => c.id === candidateId);
      if (found) {
        sizingStore.selectCandidate(candidateId);
      }
    }
  }, [candidateId, candidate, sizingStore]);

  const handleHome = () => {
    navigate("/dashboard");
  };

  const handleLogout = () => {
    authStore.logout();
    navigate("/login");
  };

  const handleParameterAdjust = async (updates: Partial<CandidateDesign>) => {
    if (!candidate) return;

    setIsAdjusting(true);
    try {
      // Determine which parameter was updated
      let parameter = "";
      let value = 0;

      if (updates.lppM !== undefined) {
        parameter = "lppM";
        value = updates.lppM;
      } else if (updates.beamM !== undefined) {
        parameter = "bM";
        value = updates.beamM;
      } else if (updates.draftM !== undefined) {
        parameter = "tM";
        value = updates.draftM;
      } else if (updates.cb !== undefined) {
        parameter = "cb";
        value = updates.cb;
      }

      if (!parameter) {
        console.warn("No recognized parameter in updates:", updates);
        return;
      }

      console.log(`[Adjusting] ${parameter} = ${value} for candidate ${candidate.id}`);

      const updatedCandidate = await adjustParameter(candidate.id, {
        parameter,
        value,
        recomputeMode: "fast",
      });

      // Update the candidate in the store
      sizingStore.updateCandidate(updatedCandidate);

      console.log("[Adjusted] New displacement:", updatedCandidate.dispT);
    } catch (error) {
      console.error("Failed to adjust parameter:", error);
    } finally {
      setIsAdjusting(false);
    }
  };

  if (!candidate) {
    return (
      <div className="flex min-h-screen flex-col bg-gray-50 dark:bg-gray-900">
        <AppHeader
          left={<h1 className="text-xl font-semibold text-foreground">Hull Sizing - Workspace</h1>}
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
        <main className="flex-1 flex items-center justify-center">
          <div className="text-center">
            <p className="text-gray-600 dark:text-gray-400">Loading candidate...</p>
          </div>
        </main>
        <Footer />
        <UserSettingsDialog isOpen={showSettings} onClose={() => setShowSettings(false)} />
      </div>
    );
  }

  // Parse flags
  let flags: string[] = [];
  try {
    flags = JSON.parse(candidate.flagsJson);
  } catch {
    // Ignore
  }

  const hasConstraintViolations = flags.some(
    (f) => f.includes("constrained") || f.includes("exceeded")
  );

  return (
    <div className="flex min-h-screen flex-col bg-gray-50 dark:bg-gray-900">
      <AppHeader
        left={<h1 className="text-xl font-semibold text-foreground">Hull Sizing - Workspace</h1>}
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

      <main className="flex-1">
        {/* Toolbar */}
        <div className="border-b border-gray-200 bg-white dark:border-gray-700 dark:bg-gray-800">
          <div className="mx-auto max-w-7xl px-4 py-4 sm:px-6 lg:px-8">
            <div className="flex items-center justify-between">
              <div className="flex items-center space-x-4">
                <Button variant="outline" size="sm" onClick={() => navigate(-1)}>
                  ← Back
                </Button>
                <div>
                  <h2 className="text-xl font-bold text-gray-900 dark:text-white capitalize">
                    {candidate.hullFamily.replace("_", " ")}
                  </h2>
                  <p className="text-sm text-gray-600 dark:text-gray-400">
                    Rank #{candidate.rank} • Score: {(candidate.score * 100).toFixed(1)}%
                  </p>
                </div>
              </div>

              <div className="flex space-x-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => sizingStore.exportCandidate(candidate.id, "json")}
                >
                  Export JSON
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => sizingStore.exportCandidate(candidate.id, "csv")}
                >
                  Export CSV
                </Button>
                <Button
                  size="sm"
                  onClick={async () => {
                    const vesselId = await sizingStore.pushToHydrostatics(candidate.id);
                    navigate(`/hydrostatics/vessels/${vesselId}/workspace`);
                  }}
                >
                  Push to Hydrostatics
                </Button>
              </div>
            </div>
          </div>
        </div>

        {/* Content */}
        <div className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
          {/* CAD-Style Quad Viewport */}
          <div className="mb-6 rounded-lg bg-white shadow dark:bg-gray-800 overflow-hidden">
            <div className="border-b border-gray-200 dark:border-gray-700 p-4">
              <h3 className="font-semibold text-gray-900 dark:text-white">
                Hull Visualization (CAD Layout)
              </h3>
              <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                Click any viewport header to maximize. Plan view shows waterlines, 3D shows
                parametric Wigley hull.
              </p>
            </div>
            <div className="h-[900px]">
              <ViewportQuadLayout candidate={candidate} />
            </div>
          </div>

          <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
            {/* Left: KPI Panel with Tabs */}
            <div className="space-y-4">
              {/* Tab Switcher */}
              <div className="flex gap-2 border-b border-gray-200 dark:border-gray-700">
                <button
                  onClick={() => setActiveTab("kpi")}
                  className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
                    activeTab === "kpi"
                      ? "border-blue-600 text-blue-600 dark:text-blue-400"
                      : "border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200"
                  }`}
                >
                  <BarChart3 className="h-4 w-4 mr-2" />
                  KPIs & Metrics
                </button>
                <button
                  onClick={() => setActiveTab("offsets")}
                  className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
                    activeTab === "offsets"
                      ? "border-blue-600 text-blue-600 dark:text-blue-400"
                      : "border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200"
                  }`}
                >
                  <Ruler className="h-4 w-4 mr-2" />
                  Table of Offsets
                </button>
              </div>

              {/* Tab Content */}
              {activeTab === "kpi" && <CompactHUD candidate={candidate} />}
              {activeTab === "offsets" && <OffsetsTable candidate={candidate} />}
            </div>

            {/* Center: Parameter Sliders */}
            <div className="space-y-6">
              <ParameterSliders
                candidate={candidate}
                onUpdate={handleParameterAdjust}
                isUpdating={isAdjusting}
              />
            </div>

            {/* Center (Old): Performance KPIs - Keeping for backward compatibility */}
            <div className="space-y-6 hidden">
              <div className="rounded-lg bg-white p-6 shadow dark:bg-gray-800">
                <h3 className="font-semibold text-gray-900 dark:text-white mb-4">Performance</h3>
                <dl className="space-y-3 text-sm">
                  <div className="flex justify-between">
                    <dt className="text-gray-600 dark:text-gray-400">Displacement:</dt>
                    <dd className="font-medium text-gray-900 dark:text-white">
                      {candidate.dispT.toFixed(0)} tonnes
                    </dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-gray-600 dark:text-gray-400">Froude Number:</dt>
                    <dd className="font-medium text-gray-900 dark:text-white">
                      {candidate.fn.toFixed(4)}
                    </dd>
                  </div>
                  {candidate.lwlOverLambda && (
                    <div className="flex justify-between">
                      <dt className="text-gray-600 dark:text-gray-400">Lwl/λ:</dt>
                      <dd className="font-medium text-gray-900 dark:text-white">
                        {candidate.lwlOverLambda.toFixed(2)}
                      </dd>
                    </div>
                  )}
                  {candidate.ehpKw && (
                    <>
                      <div className="flex justify-between">
                        <dt className="text-gray-600 dark:text-gray-400">EHP:</dt>
                        <dd className="font-medium text-gray-900 dark:text-white">
                          {candidate.ehpKw.toFixed(0)} kW
                        </dd>
                      </div>
                      <div className="flex justify-between">
                        <dt className="text-gray-600 dark:text-gray-400">SHP:</dt>
                        <dd className="font-medium text-gray-900 dark:text-white">
                          {candidate.shpKw?.toFixed(0)} kW
                        </dd>
                      </div>
                    </>
                  )}
                </dl>
              </div>

              <div className="rounded-lg bg-white p-6 shadow dark:bg-gray-800">
                <h3 className="font-semibold text-gray-900 dark:text-white mb-4">
                  Stability (Preliminary)
                </h3>
                <dl className="space-y-3 text-sm">
                  {candidate.kbM && (
                    <div className="flex justify-between">
                      <dt className="text-gray-600 dark:text-gray-400">KB:</dt>
                      <dd className="font-medium text-gray-900 dark:text-white">
                        {candidate.kbM.toFixed(2)} m
                      </dd>
                    </div>
                  )}
                  {candidate.lcbPctLpp && (
                    <div className="flex justify-between">
                      <dt className="text-gray-600 dark:text-gray-400">LCB (% Lpp):</dt>
                      <dd className="font-medium text-gray-900 dark:text-white">
                        {candidate.lcbPctLpp.toFixed(2)}%
                      </dd>
                    </div>
                  )}
                  {candidate.gmEstM && (
                    <div className="flex justify-between">
                      <dt className="text-gray-600 dark:text-gray-400">GMt (est):</dt>
                      <dd
                        className={`font-medium ${
                          candidate.gmEstM > 1.0
                            ? "text-green-600 dark:text-green-400"
                            : "text-yellow-600 dark:text-yellow-400"
                        }`}
                      >
                        {candidate.gmEstM.toFixed(2)} m
                      </dd>
                    </div>
                  )}
                </dl>
                <p className="mt-4 text-xs text-gray-500 dark:text-gray-400">
                  Preliminary estimates. Push to Hydrostatics for detailed stability analysis.
                </p>
              </div>
            </div>

            {/* Right: Resistance Curve & Flags */}
            <div className="space-y-6">
              {/* Resistance Curve */}
              <ResistanceCurvePanel candidate={candidate} />

              {/* Flags & Warnings */}
              {flags.length > 0 && (
                <div
                  className={`rounded-lg p-6 shadow border ${
                    hasConstraintViolations
                      ? "bg-destructive/5 border-destructive/20"
                      : "bg-accent/10 border-accent/20"
                  }`}
                >
                  <h3
                    className={`font-semibold mb-4 ${
                      hasConstraintViolations ? "text-destructive" : "text-accent-foreground"
                    }`}
                  >
                    {hasConstraintViolations ? "Warnings" : "Flags"}
                  </h3>
                  <ul className="space-y-2 text-sm">
                    {flags.map((flag) => (
                      <li
                        key={flag}
                        className={`flex items-start space-x-2 ${
                          hasConstraintViolations
                            ? "text-yellow-800 dark:text-yellow-400"
                            : "text-blue-800 dark:text-blue-400"
                        }`}
                      >
                        <span>•</span>
                        <span className="capitalize">{flag.replace(/_/g, " ")}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              <div className="rounded-lg bg-white p-6 shadow dark:bg-gray-800">
                <h3 className="font-semibold text-gray-900 dark:text-white mb-4">Quick Actions</h3>
                <div className="space-y-3">
                  <Button
                    variant="outline"
                    className="w-full"
                    onClick={() => sizingStore.exportCandidate(candidate.id, "json")}
                  >
                    Export JSON
                  </Button>
                  <Button
                    variant="outline"
                    className="w-full"
                    onClick={() => sizingStore.exportCandidate(candidate.id, "csv")}
                  >
                    <BarChart3 className="h-4 w-4 mr-2" />
                    Export CSV
                  </Button>
                  <Button
                    variant="default"
                    className="w-full"
                    onClick={async () => {
                      const vesselId = await sizingStore.pushToHydrostatics(candidate.id);
                      navigate(`/hydrostatics/vessels/${vesselId}/workspace`);
                    }}
                  >
                    <Ship className="h-4 w-4 mr-2" />
                    Push to Hydrostatics
                  </Button>
                  <Button
                    variant="outline"
                    className="w-full"
                    onClick={() => {
                      // TODO: Implement push to resistance
                      console.log("Push to resistance");
                    }}
                  >
                    <Zap className="h-4 w-4 mr-2" />
                    Analyze Resistance
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>

      <Footer />

      {/* Settings Dialog */}
      <UserSettingsDialog isOpen={showSettings} onClose={() => setShowSettings(false)} />
    </div>
  );
});
