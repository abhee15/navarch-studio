import React, { useState } from "react";
import { observer } from "mobx-react-lite";
import { useNavigate } from "react-router-dom";
import { useStore } from "../../stores";
import { Footer } from "../../components/Footer";
import { Step1MissionCargo } from "../../components/sizing/wizard/Step1MissionCargo";
import { Step2SpeedEnvironment } from "../../components/sizing/wizard/Step2SpeedEnvironment";
import { Step3Constraints } from "../../components/sizing/wizard/Step3Constraints";
import { Step4Options } from "../../components/sizing/wizard/Step4Options";
import type { CreateMissionCaseDto } from "../../types/sizing";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { UserSettingsDialog } from "../../components/UserSettingsDialog";

export const MissionWizard: React.FC = observer(() => {
  const navigate = useNavigate();
  const { sizingStore, authStore } = useStore();
  const [currentStep, setCurrentStep] = useState(1);
  const [isGenerating, setIsGenerating] = useState(false);
  const [showSettings, setShowSettings] = useState(false);
  const [formData, setFormData] = useState<Partial<CreateMissionCaseDto>>({
    missionType: "commercial",
    cargoBasis: "teu",
    serviceSpeedKn: 20,
    seaMarginPct: 15,
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
      const mission = await sizingStore.createMissionCase(formData as CreateMissionCaseDto);

      // Automatically run solver with default options
      const run = await sizingStore.runSolver({
        missionCaseId: mission.id,
        mode: "first_principles",
        options: { maxCandidates: 5 },
      });

      // Navigate to results
      navigate(`/sizing/runs/${run.id}`);
    } catch (error) {
      console.error("Failed to create mission and run solver:", error);
      setIsGenerating(false);
    }
  };

  const steps = [
    { number: 1, title: "Mission & Cargo", component: Step1MissionCargo },
    { number: 2, title: "Speed & Environment", component: Step2SpeedEnvironment },
    { number: 3, title: "Constraints", component: Step3Constraints },
    { number: 4, title: "Options & Review", component: Step4Options },
  ];

  const CurrentStepComponent = steps[currentStep - 1].component;

  return (
    <div className="flex min-h-screen flex-col bg-gray-50 dark:bg-gray-900">
      {/* Main Navigation Header */}
      <header className="border-b border-border bg-card/80 backdrop-blur-sm flex-shrink-0 relative z-50">
        <div className="px-4 py-2">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <h1 className="text-lg font-bold text-foreground">NavArch Studio</h1>
            </div>
            <div className="flex items-center space-x-2">
              <button
                onClick={handleHome}
                className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-foreground hover:text-foreground/80 border border-border rounded hover:bg-accent/10"
              >
                <svg
                  className="h-4 w-4 mr-1.5"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"
                  />
                </svg>
                Home
              </button>
              <UserProfileMenu
                onOpenSettings={() => setShowSettings(true)}
                onLogout={handleLogout}
              />
            </div>
          </div>
        </div>
      </header>

      <main className="flex-1 py-8">
        <div className="mx-auto max-w-4xl px-4 sm:px-6 lg:px-8">
          {/* Header */}
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-gray-900 dark:text-white">Hull Sizing Wizard</h1>
            <p className="mt-2 text-gray-600 dark:text-gray-400">
              Define your mission requirements and let our first-principles solver generate optimal
              hull designs
            </p>
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
                          ? "border-blue-600 bg-blue-600 text-white dark:border-blue-500 dark:bg-blue-500"
                          : "border-gray-300 bg-white text-gray-500 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-400"
                      }`}
                    >
                      {step.number}
                    </div>
                    <span
                      className={`mt-2 text-sm ${
                        currentStep >= step.number
                          ? "font-medium text-blue-600 dark:text-blue-400"
                          : "text-gray-500 dark:text-gray-400"
                      }`}
                    >
                      {step.title}
                    </span>
                  </div>
                  {idx < steps.length - 1 && (
                    <div
                      className={`h-0.5 flex-1 ${
                        currentStep > step.number
                          ? "bg-blue-600 dark:bg-blue-500"
                          : "bg-gray-300 dark:bg-gray-600"
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
