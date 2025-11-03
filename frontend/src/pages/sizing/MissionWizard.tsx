import React, { useState } from "react";
import { observer } from "mobx-react-lite";
import { useNavigate } from "react-router-dom";
import { useStore } from "../../stores";
import { AppHeader } from "../../components/AppHeader";
import { Footer } from "../../components/Footer";
import { Step1MissionCargo } from "../../components/sizing/wizard/Step1MissionCargo";
import { Step2SpeedEnvironment } from "../../components/sizing/wizard/Step2SpeedEnvironment";
import { Step3Constraints } from "../../components/sizing/wizard/Step3Constraints";
import { Step4Options } from "../../components/sizing/wizard/Step4Options";
import type { CreateMissionCaseDto } from "../../types/sizing";

export const MissionWizard: React.FC = observer(() => {
  const navigate = useNavigate();
  const { sizingStore } = useStore();
  const [currentStep, setCurrentStep] = useState(1);
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

  const handleSubmit = async () => {
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
      <AppHeader />

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
          <div className="rounded-lg bg-white p-8 shadow dark:bg-gray-800">
            <CurrentStepComponent
              formData={formData}
              updateFormData={updateFormData}
              onNext={nextStep}
              onPrevious={previousStep}
              onSubmit={handleSubmit}
              isFirstStep={currentStep === 1}
              isLastStep={currentStep === 4}
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
    </div>
  );
});
