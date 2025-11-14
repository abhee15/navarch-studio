import React, { useEffect, useState } from "react";
import { observer } from "mobx-react-lite";
import { useParams, useNavigate } from "react-router-dom";
import toast from "react-hot-toast";
import { useStore } from "../../stores";
import { Footer } from "../../components/Footer";
import { Button } from "../../components/ui/button";
import { ViewportQuadLayout } from "../../components/sizing/visualization/ViewportQuadLayout";
import { CompactHUD } from "../../components/sizing/workspace/CompactHUD";
import { OffsetsTable } from "../../components/sizing/workspace/OffsetsTable";
import { ParameterSliders } from "../../components/sizing/workspace/ParameterSliders";
import { ParametersDrawer } from "../../components/sizing/workspace/ParametersDrawer";
import { ResistanceCurvePanel } from "../../components/sizing/workspace/ResistanceCurvePanel";
import { SensitivityPanel } from "../../components/sizing/workspace/SensitivityPanel";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { UserSettingsDialog } from "../../components/UserSettingsDialog";
import { adjustParameter } from "../../services/sizingApi";
import type { CandidateDesign } from "../../types/sizing";
import { AppHeader } from "../../components/AppHeader";
import {
  BarChart3,
  Ruler,
  Ship,
  Zap,
  Home,
  FileDown,
  Activity,
  FileJson,
  FileText,
} from "lucide-react";
import { downloadDXF } from "../../utils/dxfExporter";
import { ShipDParameterChart } from "../../components/sizing/visualization/ShipDParameterChart";
import { GeometryDetailsPanel } from "../../components/sizing/visualization/GeometryDetailsPanel";
import { PushToHydroModal } from "../../components/sizing/PushToHydroModal";
import type { PushToHydroForm } from "../../stores/SizingStore";

export const CandidateWorkspace: React.FC = observer(() => {
  const { candidateId } = useParams<{ candidateId: string }>();
  const navigate = useNavigate();
  const { sizingStore, authStore } = useStore();
  const [activeTab, setActiveTab] = useState<"kpi" | "offsets" | "sensitivity" | "shipd">("kpi");
  const [showSettings, setShowSettings] = useState(false);
  const [isAdjusting, setIsAdjusting] = useState(false);
  const [isPushModalOpen, setPushModalOpen] = useState(false);
  const [isPushingToHydro, setIsPushingToHydro] = useState(false);
  const [pushError, setPushError] = useState<string | null>(null);

  const candidate = sizingStore.selectedCandidate;
  const mission = sizingStore.selectedMission;

  useEffect(() => {
    if (candidateId && (!candidate || candidate.id !== candidateId)) {
      // Load candidate details
      // For now, candidate should already be in store from results page
      const found = sizingStore.candidates.find((c) => c.id === candidateId);
      if (found) {
        sizingStore.selectCandidate(candidateId);
      }
    }
    // Ensure ShipD metadata is loaded if candidate has ShipD parameters
    if (candidate?.shipdParametersJson) {
      sizingStore.ensureShipDMetadataLoaded();
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
      // Determine which parameter was updated and map to backend format
      let parameter = "";
      let value: number | boolean = 0;

      // Basic dimensions
      if (updates.lppM !== undefined) {
        parameter = "lppM";
        value = updates.lppM;
      } else if (updates.beamM !== undefined) {
        parameter = "bM";
        value = updates.beamM;
      } else if (updates.draftM !== undefined) {
        parameter = "tM";
        value = updates.draftM;
      } else if (updates.depthM !== undefined) {
        parameter = "dM";
        value = updates.depthM;
      }
      // Basic coefficients
      else if (updates.cb !== undefined) {
        parameter = "cb";
        value = updates.cb;
      } else if (updates.cp !== undefined) {
        parameter = "cp";
        value = updates.cp;
      } else if (updates.cwp !== undefined) {
        parameter = "cwp";
        value = updates.cwp;
      }
      // Advanced - Longitudinal
      else if (updates.bowLengthRatio !== undefined) {
        parameter = "bowLengthRatio";
        value = updates.bowLengthRatio;
      } else if (updates.sternLengthRatio !== undefined) {
        parameter = "sternLengthRatio";
        value = updates.sternLengthRatio;
      }
      // Advanced - Bow shape
      else if (updates.bowFlareAngle !== undefined) {
        parameter = "bowFlareAngle";
        value = updates.bowFlareAngle;
      } else if (updates.bowCurvature !== undefined) {
        parameter = "bowCurvature";
        value = updates.bowCurvature;
      } else if (updates.bowKnuckle !== undefined) {
        parameter = "bowKnuckle";
        value = updates.bowKnuckle;
      } else if (updates.deadriseAngle !== undefined) {
        parameter = "deadriseAngle";
        value = updates.deadriseAngle;
      }
      // Advanced - Stern shape
      else if (updates.sternRakeAngle !== undefined) {
        parameter = "sternRakeAngle";
        value = updates.sternRakeAngle;
      } else if (updates.sternCurvature !== undefined) {
        parameter = "sternCurvature";
        value = updates.sternCurvature;
      } else if (updates.sternKnuckle !== undefined) {
        parameter = "sternKnuckle";
        value = updates.sternKnuckle;
      } else if (updates.transomArea !== undefined) {
        parameter = "transomArea";
        value = updates.transomArea;
      } else if (updates.transomWidth !== undefined) {
        parameter = "transomWidth";
        value = updates.transomWidth;
      }
      // Advanced - Midship
      else if (updates.hasSheer !== undefined) {
        parameter = "hasSheer";
        value = updates.hasSheer ? 1 : 0; // Convert boolean to number for API
      } else if (updates.hasTumblehome !== undefined) {
        parameter = "hasTumblehome";
        value = updates.hasTumblehome ? 1 : 0;
      }
      // Advanced - Bulb
      else if (updates.hasBulb !== undefined) {
        parameter = "hasBulb";
        value = updates.hasBulb ? 1 : 0;
      } else if (updates.bulbLengthRatio !== undefined) {
        parameter = "bulbLengthRatio";
        value = updates.bulbLengthRatio;
      } else if (updates.bulbHeightRatio !== undefined) {
        parameter = "bulbHeightRatio";
        value = updates.bulbHeightRatio;
      } else if (updates.bulbWidthRatio !== undefined) {
        parameter = "bulbWidthRatio";
        value = updates.bulbWidthRatio;
      } else if (updates.bulbAsymmetry !== undefined) {
        parameter = "bulbAsymmetry";
        value = updates.bulbAsymmetry;
      } else if (updates.bulbFilletRadius !== undefined) {
        parameter = "bulbFilletRadius";
        value = updates.bulbFilletRadius;
      }

      if (!parameter) {
        console.warn("No recognized parameter in updates:", updates);
        return;
      }

      console.log(
        `[Adjusting] ${parameter} = ${value} for candidate ${candidate.id} (Hybrid mode: fast preview + background solver)`
      );

      // Call backend API with hybrid fast mode
      // Backend will apply intelligent ShipD vector scaling for fast preview
      // and queue a background solver re-run for accurate final results
      const updatedCandidate = await adjustParameter(candidate.id, {
        parameter,
        value: typeof value === "boolean" ? (value ? 1 : 0) : value,
        recomputeMode: "fast", // Hybrid mode: fast parametric scaling + background solver
      });

      // Update the candidate in the store with fast preview results
      sizingStore.updateCandidate(updatedCandidate);

      console.log(
        "[Adjusted] Fast preview updated. New displacement:",
        updatedCandidate.dispT,
        "t"
      );
      // Note: Background solver will re-run for accurate physics-based results
    } catch (error) {
      console.error("Failed to adjust parameter:", error);
      toast.error("Failed to update parameter. Please try again.");
    } finally {
      setIsAdjusting(false);
    }
  };

  const handleOpenPushModal = () => {
    if (!candidate) return;
    sizingStore.ensureShipDMetadataLoaded();
    setPushError(null);
    setPushModalOpen(true);
  };

  const handlePushToHydrostatics = async (form: PushToHydroForm) => {
    if (!candidate) return;
    setIsPushingToHydro(true);
    setPushError(null);
    try {
      const result = await sizingStore.pushToHydrostatics(candidate, form, {
        id: authStore.user?.id,
        name: authStore.user?.name,
      });
      toast.success("Vessel pushed to Hydrostatics");
      setPushModalOpen(false);
      navigate(`/hydrostatics/vessels/${result.vesselId}/workspace`);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Failed to push to Hydrostatics";
      setPushError(message);
      toast.error(message);
    } finally {
      setIsPushingToHydro(false);
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
            <p className="text-muted-foreground">Loading candidate...</p>
          </div>
        </main>
        <Footer />
        <UserSettingsDialog isOpen={showSettings} onClose={() => setShowSettings(false)} />
      </div>
    );
  }

  // Parse flags
  // let flags: string[] = [];
  // try {
  //   flags = JSON.parse(candidate.flagsJson);
  // } catch {
  //   // Ignore
  // }

  // const hasConstraintViolations = flags.some(
  //   (f) => f.includes("constrained") || f.includes("exceeded")
  // );

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
                  <h2 className="text-xl font-bold text-foreground capitalize">
                    {candidate.hullFamily.replace("_", " ")}
                  </h2>
                  <p className="text-sm text-muted-foreground">
                    Rank #{candidate.rank} • Score: {(candidate.score * 100).toFixed(1)}%
                  </p>
                </div>
              </div>

              <div className="flex space-x-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => sizingStore.exportCandidate(candidate.id, "json")}
                  title="Export JSON"
                >
                  <FileJson className="h-4 w-4" />
                  <span className="hidden md:inline ml-2">Export JSON</span>
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => sizingStore.exportCandidate(candidate.id, "csv")}
                  title="Export CSV"
                >
                  <FileText className="h-4 w-4" />
                  <span className="hidden md:inline ml-2">Export CSV</span>
                </Button>
                <Button size="sm" onClick={handleOpenPushModal} title="Push to Hydrostatics">
                  <Ship className="h-4 w-4" />
                  <span className="hidden md:inline ml-2">Push to Hydrostatics</span>
                </Button>
              </div>
            </div>
          </div>
        </div>

        {/* Content - New Flex Layout */}
        <div className="flex min-h-[calc(100vh-4rem)]">
          {/* Left Sidebar - Parameters (Desktop & Tablet) */}
          <aside className="hidden md:flex md:flex-col w-64 lg:w-80 xl:w-96 sticky top-16 h-[calc(100vh-4rem)] overflow-y-auto border-r border-border bg-card/50 backdrop-blur-sm">
            <div className="p-4 space-y-4">
              <ParameterSliders
                candidate={candidate}
                onUpdate={handleParameterAdjust}
                isUpdating={isAdjusting}
              />
            </div>
          </aside>

          {/* Main Content Area */}
          <main className="flex-1 min-w-0 overflow-y-auto">
            <div className="px-4 py-8 sm:px-6">
              {/* Viewports - Flexible Height */}
              <div className="mb-6 rounded-lg bg-card shadow overflow-hidden">
                <div className="border-b border-border p-4">
                  <h3 className="font-semibold text-foreground">Hull Visualization</h3>
                  <p className="text-sm text-muted-foreground mt-1">
                    Click any viewport header to maximize. Adjust parameters on the left to see live
                    updates.
                  </p>
                </div>
                <div className="min-h-[800px]">
                  <ViewportQuadLayout candidate={candidate} />
                </div>
              </div>

              {/* Compact Metrics Below - 2 Column Grid */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {/* KPI Panel with Tabs */}
                <div className="rounded-lg bg-card border border-border shadow-sm overflow-hidden">
                  {/* Tab Switcher */}
                  <div className="flex gap-2 border-b border-border bg-muted/30 px-4">
                    <button
                      onClick={() => setActiveTab("kpi")}
                      className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors flex items-center gap-2 ${
                        activeTab === "kpi"
                          ? "border-primary text-primary"
                          : "border-transparent text-muted-foreground hover:text-foreground"
                      }`}
                    >
                      <BarChart3 className="h-4 w-4" />
                      <span className="hidden sm:inline">KPIs</span>
                    </button>
                    <button
                      onClick={() => setActiveTab("offsets")}
                      className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors flex items-center gap-2 ${
                        activeTab === "offsets"
                          ? "border-primary text-primary"
                          : "border-transparent text-muted-foreground hover:text-foreground"
                      }`}
                    >
                      <Ruler className="h-4 w-4" />
                      <span className="hidden sm:inline">Offsets</span>
                    </button>
                    <button
                      onClick={() => setActiveTab("sensitivity")}
                      className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors flex items-center gap-2 ${
                        activeTab === "sensitivity"
                          ? "border-primary text-primary"
                          : "border-transparent text-muted-foreground hover:text-foreground"
                      }`}
                    >
                      <Activity className="h-4 w-4" />
                      <span className="hidden sm:inline">Sensitivity</span>
                    </button>
                    {candidate.shipdParametersJson && (
                      <button
                        onClick={() => setActiveTab("shipd")}
                        className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors flex items-center gap-2 ${
                          activeTab === "shipd"
                            ? "border-primary text-primary"
                            : "border-transparent text-muted-foreground hover:text-foreground"
                        }`}
                      >
                        <Ship className="h-4 w-4" />
                        <span className="hidden sm:inline">ShipD</span>
                      </button>
                    )}
                  </div>

                  {/* Tab Content */}
                  <div className="p-4">
                    {activeTab === "kpi" && <CompactHUD candidate={candidate} />}
                    {activeTab === "offsets" && <OffsetsTable candidate={candidate} />}
                    {activeTab === "sensitivity" && <SensitivityPanel candidate={candidate} />}
                    {activeTab === "shipd" && candidate.shipdParametersJson && (
                      <div className="space-y-4">
                        <GeometryDetailsPanel candidate={candidate} />
                        {sizingStore.shipdParameters && sizingStore.shipdParameters.length > 0 && (
                          <ShipDParameterChart
                            candidate={candidate}
                            metadata={sizingStore.shipdParameters}
                          />
                        )}
                      </div>
                    )}
                  </div>
                </div>

                {/* Resistance & Actions Combined */}
                <div className="space-y-6">
                  {/* Resistance Curve */}
                  <div className="rounded-lg bg-card border border-border shadow-sm overflow-hidden">
                    <div className="bg-muted/30 px-4 py-3 border-b border-border">
                      <h3 className="text-sm font-semibold text-foreground">Resistance Analysis</h3>
                    </div>
                    <div className="p-4">
                      <ResistanceCurvePanel candidate={candidate} />
                    </div>
                  </div>

                  {/* Actions Panel */}
                  <div className="rounded-lg bg-card border border-border shadow-sm overflow-hidden">
                    <div className="bg-muted/30 px-4 py-3 border-b border-border">
                      <h3 className="text-sm font-semibold text-foreground">Actions</h3>
                    </div>
                    <div className="p-4 space-y-2">
                      <Button
                        variant="default"
                        size="sm"
                        className="w-full"
                        onClick={handleOpenPushModal}
                      >
                        <Ship className="h-3 w-3 mr-2" />
                        Push to Hydrostatics
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        className="w-full"
                        onClick={() => {
                          console.log("Push to resistance");
                        }}
                      >
                        <Zap className="h-3 w-3 mr-2" />
                        Analyze Resistance
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        className="w-full"
                        onClick={() => downloadDXF(candidate)}
                      >
                        <FileDown className="h-3 w-3 mr-2" />
                        Export DXF (CAD)
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        className="w-full"
                        onClick={() => sizingStore.exportCandidate(candidate.id, "csv")}
                      >
                        <BarChart3 className="h-3 w-3 mr-2" />
                        Export CSV (Data)
                      </Button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </main>
        </div>
      </main>

      {/* Mobile Parameters Drawer */}
      <ParametersDrawer
        candidate={candidate}
        onUpdate={handleParameterAdjust}
        isUpdating={isAdjusting}
      />

      <Footer />

      {/* Settings Dialog */}
      <UserSettingsDialog isOpen={showSettings} onClose={() => setShowSettings(false)} />

      <PushToHydroModal
        isOpen={isPushModalOpen}
        onClose={() => {
          if (!isPushingToHydro) {
            setPushModalOpen(false);
          }
        }}
        candidate={candidate}
        missionName={mission?.name}
        missionCategory={mission?.missionCategory}
        taxonomy={sizingStore.shipdTaxonomy}
        isSubmitting={isPushingToHydro}
        error={pushError}
        onSubmit={handlePushToHydrostatics}
      />
    </div>
  );
});
