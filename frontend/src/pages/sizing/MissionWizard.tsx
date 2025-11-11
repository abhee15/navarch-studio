import React, { useState } from "react";
import { observer } from "mobx-react-lite";
import { useNavigate, useLocation } from "react-router-dom";
import { useStore } from "../../stores";
import { Footer } from "../../components/Footer";
import { Step1MissionCargo } from "../../components/sizing/wizard/Step1MissionCargo";
import { Step2SpeedEnvironment } from "../../components/sizing/wizard/Step2SpeedEnvironment";
import { Step3Constraints } from "../../components/sizing/wizard/Step3Constraints";
import { Step4Options } from "../../components/sizing/wizard/Step4Options";
import type { CreateMissionCaseDto } from "../../types/sizing";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { UserSettingsDialog } from "../../components/UserSettingsDialog";
import { AppHeader } from "../../components/AppHeader";
import { Button } from "../../components/ui/button";
import { Home, Sparkles, AlertTriangle } from "lucide-react";
import { getStepInferenceReason } from "../../utils/diagnosticHelpers";

export const MissionWizard: React.FC = observer(() => {
  const navigate = useNavigate();
  const location = useLocation();
  const { sizingStore, authStore } = useStore();

  // Check if coming from AI Copilot with pre-filled data
  const aiGeneratedMission = location.state?.aiGeneratedMission;
  const isAIGenerated = !!aiGeneratedMission;

  // Check if adjusting parameters after a failed solver run
  const editingMission = location.state?.editingMission;
  const isAdjustingAfterFailure = location.state?.isAdjustingAfterFailure;
  const diagnostics = location.state?.diagnostics;
  const existingMissionCaseId = location.state?.missionCaseId; // Store the ID of the mission being edited

  const [currentStep, setCurrentStep] = useState(() => {
    // If adjusting after failure, start at the inferred problematic step
    if (isAdjustingAfterFailure && location.state?.initialStep) {
      return location.state.initialStep;
    }
    return 1;
  });

  const [isGenerating, setIsGenerating] = useState(false);
  const [showSettings, setShowSettings] = useState(false);

  const [formData, setFormData] = useState<Partial<CreateMissionCaseDto>>(() => {
    // Priority 1: Editing an existing mission after failure
    if (editingMission) {
      return editingMission;
    }

    // Priority 2: AI-generated mission
    if (aiGeneratedMission) {
      return {
        name: aiGeneratedMission.name,
        missionType: aiGeneratedMission.missionType || "commercial",
        cargoBasis: aiGeneratedMission.cargoBasis?.toLowerCase() || "teu",
        cargoValue: aiGeneratedMission.cargoValue,
        cargoDensityTPerM3: aiGeneratedMission.cargoDensityTPerM3,
        serviceSpeedKn: aiGeneratedMission.serviceSpeedKn,
        seaMarginPct: aiGeneratedMission.seaMarginPct || 15,
        capBeamM: aiGeneratedMission.capBeamM,
        capDraftM: aiGeneratedMission.capDraftM,
        notes: aiGeneratedMission.notes,
      };
    }

    // Default: Empty form with sensible defaults
    return {
      missionType: "commercial",
      cargoBasis: "teu",
      serviceSpeedKn: 20,
      seaMarginPct: 15,
    };
  });

  const [solverMode, setSolverMode] = useState<
    "first_principles" | "data_driven_real" | "data_driven_ml"
  >(() => {
    // Preserve solver mode if adjusting after failure
    if (isAdjustingAfterFailure && location.state?.solverMode) {
      return location.state.solverMode;
    }
    return "first_principles";
  });

  const updateFormData = (data: Partial<CreateMissionCaseDto>) => {
    setFormData((prev) => ({ ...prev, ...data }));
  };

  const nextStep = () => {
    if (currentStep < 4) {
      setCurrentStep(currentStep + 1);
    }
  };

  const previousStep = () => {
    if (currentStep > 1) {
      setCurrentStep(currentStep - 1);
    }
  };

  const handleHome = () => {
    navigate("/dashboard");
  };

  const handleLogout = () => {
    authStore.logout();
    navigate("/login");
  };

  const handleSubmit = async () => {
    setIsGenerating(true);
    try {
      let missionCaseId: string;

      // If editing an existing mission (adjusting after failure), update it with new values
      if (existingMissionCaseId) {
        await sizingStore.updateMissionCase(
          existingMissionCaseId,
          formData as CreateMissionCaseDto
        );
        missionCaseId = existingMissionCaseId;
      } else {
        // Create new mission case
        const mission = await sizingStore.createMissionCase(formData as CreateMissionCaseDto);
        missionCaseId = mission.id;
      }

      // Run solver with selected mode
      const run = await sizingStore.runSolver({
        missionCaseId,
        mode: solverMode,
        options: { maxCandidates: 5 },
      });

      // Navigate to results
      navigate(`/sizing/runs/${run.id}`);
    } catch (error) {
      console.error("Failed to create brief and run solver:", error);
      setIsGenerating(false);
    }
  };

  const steps = [
    { number: 1, title: "Vessel Requirements", component: Step1MissionCargo },
    { number: 2, title: "Speed & Environment", component: Step2SpeedEnvironment },
    { number: 3, title: "Constraints", component: Step3Constraints },
    { number: 4, title: "Options & Review", component: Step4Options },
  ];

  const CurrentStepComponent = steps[currentStep - 1].component;

  return (
    <div className="flex min-h-screen flex-col bg-gray-50 dark:bg-gray-900">
      <AppHeader
        left={<h1 className="text-xl font-semibold text-foreground">Hull Sizing - New Brief</h1>}
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
        <div className="mx-auto max-w-4xl px-4 sm:px-6 lg:px-8">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Hull Sizing Wizard</h1>
            <p className="mt-2 text-gray-600 dark:text-gray-400">
              Define your brief requirements and choose your solver mode for optimal hull designs
            </p>

            {/* AI Generated Banner */}
            {isAIGenerated && (
              <div className="mt-4 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
                <div className="flex items-center gap-2">
                  <Sparkles className="w-5 h-5 text-blue-600 dark:text-blue-400" />
                  <p className="text-sm text-blue-800 dark:text-blue-300">
                    Mission parameters pre-filled by AI Copilot. Review and adjust as needed before
                    generating candidates.
                  </p>
                </div>
              </div>
            )}

            {/* Adjusting After Failure Banner */}
            {isAdjustingAfterFailure && (
              <div className="mt-4 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg p-4">
                <div className="flex items-start gap-3">
                  <AlertTriangle className="w-5 h-5 text-yellow-600 dark:text-yellow-400 flex-shrink-0 mt-0.5" />
                  <div className="flex-1">
                    <p className="text-sm font-medium text-yellow-900 dark:text-yellow-300">
                      Adjusting Parameters After Solver Failure
                    </p>
                    <p className="text-sm text-yellow-800 dark:text-yellow-400 mt-1">
                      {getStepInferenceReason(diagnostics)}. Your previous values have been restored
                      for editing.
                    </p>
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Progress Steps */}
          <div className="mb-8">
            <div className="flex items-center justify-between">
              {steps.map((step, idx) => (
                <React.Fragment key={step.number}>
                  <div className="flex flex-col items-center">
                    <div
                      className={`flex h-10 w-10 items-center justify-center rounded-full border-2 ${
                        currentStep >= step.number
                          ? "border-primary bg-primary text-primary-foreground"
                          : "border-muted bg-card text-muted-foreground"
                      }`}
                    >
                      {step.number}
                    </div>
                    <span
                      className={`mt-2 text-sm ${
                        currentStep >= step.number
                          ? "font-medium text-primary"
                          : "text-muted-foreground"
                      }`}
                    >
                      {step.title}
                    </span>
                  </div>
                  {idx < steps.length - 1 && (
                    <div
                      className={`h-0.5 flex-1 ${
                        currentStep > step.number ? "bg-primary" : "bg-border"
                      }`}
                      style={{ marginTop: "-2rem" }}
                    />
                  )}
                </React.Fragment>
              ))}
            </div>
          </div>

          {/* Step Content */}
          <div className="rounded-lg bg-white p-8 shadow dark:bg-gray-800 relative">
            {isGenerating && (
              <div className="absolute inset-0 z-50 flex flex-col items-center justify-center bg-white/90 backdrop-blur-sm dark:bg-gray-800/90 rounded-lg">
                <div className="flex flex-col items-center space-y-4">
                  <div className="h-16 w-16 animate-spin rounded-full border-4 border-blue-200 border-t-blue-600"></div>
                  <div className="text-center">
                    <p className="text-lg font-semibold text-gray-900 dark:text-white">
                      🧮 Generating Hull Designs...
                    </p>
                    <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">
                      Running first-principles solver
                    </p>
                    <p className="mt-1 text-xs text-gray-500 dark:text-gray-500">
                      This usually takes 1-2 seconds
                    </p>
                  </div>
                </div>
              </div>
            )}
            <CurrentStepComponent
              formData={formData}
              updateFormData={updateFormData}
              solverMode={solverMode}
              setSolverMode={setSolverMode}
              onNext={nextStep}
              onPrevious={previousStep}
              onSubmit={handleSubmit}
              isFirstStep={currentStep === 1}
              isLastStep={currentStep === 4}
              isGenerating={isGenerating}
            />
          </div>

          {/* Error Display */}
          {sizingStore.error && (
            <div className="mt-4 rounded-lg bg-red-50 p-4 text-red-800 dark:bg-red-900/20 dark:text-red-400">
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
