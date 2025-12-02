import React, { useEffect, useMemo, useState } from "react";
import { observer } from "mobx-react-lite";
import { useNavigate, useLocation } from "react-router-dom";
import { useStore } from "../../stores";
import { Footer } from "../../components/Footer";
import { Step1MissionCargo } from "../../components/sizing/wizard/Step1MissionCargo";
import { Step2HullFamilies } from "../../components/sizing/wizard/Step2HullFamilies";
import { Step2bHullGeometryDetails } from "../../components/sizing/wizard/Step2bHullGeometryDetails";
import { Step2SpeedEnvironment } from "../../components/sizing/wizard/Step2SpeedEnvironment";
import { Step3Constraints } from "../../components/sizing/wizard/Step3Constraints";
import { Step4Options } from "../../components/sizing/wizard/Step4Options";
import type { CreateMissionCaseDto, ShipDVesselTaxonomy } from "../../types/sizing";
import { UserProfileMenu } from "../../components/UserProfileMenu";
import { UserSettingsDialog } from "../../components/UserSettingsDialog";
import { AppHeader } from "../../components/AppHeader";
import { Button } from "../../components/ui/button";
import { Home, Sparkles, AlertTriangle } from "lucide-react";
import { getStepInferenceReason } from "../../utils/diagnosticHelpers";
import { toast } from "react-hot-toast";
import { getErrorMessage } from "../../types/errors";

const FALLBACK_TAXONOMY: Record<string, { value: string; label: string }[]> = {
  commercial: [
    { value: "general_cargo", label: "General Cargo" },
    { value: "bulk_carrier", label: "Bulk Carrier" },
    { value: "container", label: "Container Ship" },
    { value: "fishing", label: "Fishing Vessel" },
    { value: "tanker", label: "Tanker" },
    { value: "lng_carrier", label: "LNG Carrier" },
    { value: "cruise_vessel", label: "Cruise Vessel" },
    { value: "passenger_vessel", label: "Passenger Vessel" },
  ],
  government: [
    { value: "cutter", label: "Cutter" },
    { value: "medical_ship", label: "Medical Ship" },
    { value: "general_military", label: "General Military" },
  ],
  recreational: [
    { value: "yacht", label: "Yacht" },
    { value: "recreational_fishing", label: "Fishing" },
    { value: "high_speed_craft", label: "High Speed Craft" },
  ],
  research: [{ value: "research_vessel", label: "Research Vessel" }],
};

const FALLBACK_TAXONOMY_ENTRIES: ShipDVesselTaxonomy[] = Object.entries(FALLBACK_TAXONOMY).flatMap(
  ([category, types]) =>
    types.map((type) => ({
      id: `fallback-${category}-${type.value}`,
      category,
      type: type.value,
      displayName: type.label,
      description: null,
      bowFamilies: [],
      midshipFamilies: [],
      sternFamilies: [],
      maskVersion: 1,
      additionalParametersJson: null,
    }))
);

const formatCategoryLabel = (category: string) =>
  category.replace(/_/g, " ").replace(/\b\w/g, (char) => char.toUpperCase());

export const MissionWizard: React.FC = observer(() => {
  const navigate = useNavigate();
  const location = useLocation();
  const { sizingStore, authStore } = useStore();

  const aiGeneratedMission = location.state?.aiGeneratedMission;
  const editingMission = location.state?.editingMission;
  const isAdjustingAfterFailure = location.state?.isAdjustingAfterFailure;
  const diagnostics = location.state?.diagnostics;
  const existingMissionCaseId = location.state?.missionCaseId;

  const [currentStep, setCurrentStep] = useState(() => {
    if (isAdjustingAfterFailure && location.state?.initialStep) {
      return location.state.initialStep;
    }
    return 1;
  });
  const [isGenerating, setIsGenerating] = useState(false);
  const [showSettings, setShowSettings] = useState(false);

  const [formData, setFormData] = useState<Partial<CreateMissionCaseDto>>(() => {
    if (editingMission) {
      return editingMission;
    }
    if (aiGeneratedMission) {
      return {
        name: aiGeneratedMission.name,
        missionCategory: aiGeneratedMission.missionCategory || "commercial",
        missionType: aiGeneratedMission.missionType || "general_cargo",
        cargoBasis: aiGeneratedMission.cargoBasis?.toLowerCase() || "teu",
        cargoValue: aiGeneratedMission.cargoValue,
        cargoDensityTPerM3: aiGeneratedMission.cargoDensityTPerM3,
        serviceSpeedKn: aiGeneratedMission.serviceSpeedKn,
        seaMarginPct: aiGeneratedMission.seaMarginPct || 15,
        capBeamM: aiGeneratedMission.capBeamM,
        capDraftM: aiGeneratedMission.capDraftM,
        notes: aiGeneratedMission.notes,
        bowFamily: aiGeneratedMission.bowFamily,
        midshipFamily: aiGeneratedMission.midshipFamily,
        sternFamily: aiGeneratedMission.sternFamily,
        familyMaskVersion: aiGeneratedMission.familyMaskVersion,
        shipdInputVectorJson: aiGeneratedMission.shipdInputVectorJson,
      };
    }
    return {
      missionCategory: "commercial",
      missionType: "general_cargo",
      cargoBasis: "teu",
      serviceSpeedKn: 20,
      seaMarginPct: 15,
    };
  });

  const [solverMode, setSolverMode] = useState<
    "first_principles" | "data_driven_real" | "data_driven_ml"
  >(() => {
    if (isAdjustingAfterFailure && location.state?.solverMode) {
      return location.state.solverMode;
    }
    return "first_principles";
  });

  const [solverMaxCandidates, setSolverMaxCandidates] = useState<number>(5);

  useEffect(() => {
    sizingStore.ensureShipDMetadataLoaded();
  }, [sizingStore]);

  useEffect(() => {
    if (!sizingStore.missionCases.length && !sizingStore.isLoading) {
      sizingStore.loadMissionCases();
    }
  }, [sizingStore]);

  const taxonomyEntries = useMemo(() => {
    return sizingStore.shipdTaxonomy.length > 0
      ? sizingStore.shipdTaxonomy
      : FALLBACK_TAXONOMY_ENTRIES;
  }, [sizingStore.shipdTaxonomy]);

  const categoryOptions = useMemo(() => {
    const options: { value: string; label: string }[] = [];
    const seen = new Set<string>();

    taxonomyEntries.forEach((entry) => {
      const key = entry.category.toLowerCase();
      if (!seen.has(key)) {
        seen.add(key);
        options.push({ value: entry.category, label: formatCategoryLabel(entry.category) });
      }
    });

    if (options.length === 0) {
      return Object.keys(FALLBACK_TAXONOMY).map((category) => ({
        value: category,
        label: formatCategoryLabel(category),
      }));
    }

    return options;
  }, [taxonomyEntries]);

  const selectedCategory =
    formData.missionCategory &&
    categoryOptions.some((opt) => opt.value === formData.missionCategory)
      ? formData.missionCategory
      : categoryOptions[0]?.value;

  const existingMissionNameSet = useMemo(() => {
    const names = new Set<string>();
    sizingStore.missionCases.forEach((mission) => {
      if (!existingMissionCaseId || mission.id !== existingMissionCaseId) {
        const normalized = mission.name?.trim().toLowerCase();
        if (normalized) {
          names.add(normalized);
        }
      }
    });
    return names;
  }, [sizingStore.missionCases, existingMissionCaseId]);

  const missionNameConflict = useMemo(() => {
    const normalized = formData.name?.trim().toLowerCase();
    if (!normalized) {
      return false;
    }
    return existingMissionNameSet.has(normalized);
  }, [formData.name, existingMissionNameSet]);

  const missionNameConflictMessage = missionNameConflict
    ? "A brief with this name already exists in this workspace. Please choose a different name."
    : null;

  const taxonomyEntriesForCategory = useMemo(() => {
    if (!selectedCategory) return [];
    return taxonomyEntries.filter(
      (entry) => entry.category.toLowerCase() === selectedCategory.toLowerCase()
    );
  }, [selectedCategory, taxonomyEntries]);

  const vesselTypeOptions = useMemo(
    () =>
      taxonomyEntriesForCategory.map((entry) => ({
        value: entry.type,
        label: entry.displayName || formatCategoryLabel(entry.type),
        description: entry.description,
      })),
    [taxonomyEntriesForCategory]
  );

  useEffect(() => {
    if (!categoryOptions.length) return;

    setFormData((prev) => {
      const updates: Partial<CreateMissionCaseDto> = {};

      if (
        !prev.missionCategory ||
        !categoryOptions.some((option) => option.value === prev.missionCategory)
      ) {
        updates.missionCategory = categoryOptions[0].value;
      }

      const effectiveCategory =
        updates.missionCategory ?? prev.missionCategory ?? categoryOptions[0].value;
      const entries = taxonomyEntries.filter(
        (entry) => entry.category.toLowerCase() === effectiveCategory.toLowerCase()
      );

      if (
        entries.length > 0 &&
        (!prev.missionType ||
          !entries.some((entry) => entry.type.toLowerCase() === prev.missionType!.toLowerCase()))
      ) {
        updates.missionType = entries[0].type;
      }

      if (Object.keys(updates).length === 0) {
        return prev;
      }

      return { ...prev, ...updates };
    });
  }, [categoryOptions, taxonomyEntries]);

  const taxonomyEntry = useMemo(() => {
    if (!selectedCategory || !formData.missionType) {
      console.log("[MissionWizard] No taxonomy entry - missing category or type:", {
        selectedCategory,
        missionType: formData.missionType,
      });
      return undefined;
    }

    const found =
      taxonomyEntriesForCategory.find(
        (entry) => entry.type.toLowerCase() === formData.missionType!.toLowerCase()
      ) ||
      FALLBACK_TAXONOMY_ENTRIES.find(
        (entry) =>
          entry.category.toLowerCase() === selectedCategory.toLowerCase() &&
          entry.type.toLowerCase() === formData.missionType!.toLowerCase()
      );

    if (found) {
      console.log("[MissionWizard] Found taxonomy entry:", {
        type: found.type,
        category: found.category,
        bowCount: found.bowFamilies?.length || 0,
        midCount: found.midshipFamilies?.length || 0,
        sternCount: found.sternFamilies?.length || 0,
      });
    } else {
      console.warn("[MissionWizard] Taxonomy entry not found:", {
        selectedCategory,
        missionType: formData.missionType,
        availableTypes: taxonomyEntriesForCategory.map((e) => e.type),
      });
    }

    return found;
  }, [selectedCategory, formData.missionType, taxonomyEntriesForCategory]);

  const updateFormData = (
    data:
      | Partial<CreateMissionCaseDto>
      | ((prev: Partial<CreateMissionCaseDto>) => Partial<CreateMissionCaseDto>)
  ) => {
    setFormData((prev) => (typeof data === "function" ? data(prev) : { ...prev, ...data }));
  };

  // Determine if geometry details step should be shown
  const showGeometryDetails = useMemo(() => {
    return !!(formData.bowFamily && formData.midshipFamily && formData.sternFamily);
  }, [formData.bowFamily, formData.midshipFamily, formData.sternFamily]);

  const steps = useMemo(() => {
    const baseSteps = [
      { number: 1, title: "Vessel Requirements" },
      { number: 2, title: "Hull Families" },
    ];

    if (showGeometryDetails) {
      baseSteps.push({ number: 2.5, title: "Hull Geometry Details" });
    }

    baseSteps.push(
      { number: 3, title: "Speed & Environment" },
      { number: 4, title: "Constraints" },
      { number: 5, title: "Options & Review" }
    );

    return baseSteps;
  }, [showGeometryDetails]);

  const nextStep = () => {
    // Calculate actual step number accounting for conditional geometry step
    const actualStepCount = steps.length;
    if (currentStep < actualStepCount) {
      setCurrentStep(currentStep + 1);
    }
  };

  const previousStep = () => {
    if (currentStep > 1) {
      setCurrentStep(currentStep - 1);
    }
  };

  // Map currentStep to actual step component
  const getActualStepNumber = (): number | "geometry" => {
    if (currentStep === 1) return 1;
    if (currentStep === 2) return 2;
    if (currentStep === 3 && showGeometryDetails) return "geometry"; // Geometry details
    if (currentStep === 3 && !showGeometryDetails) return 3; // Speed & Environment
    if (currentStep === 4 && showGeometryDetails) return 3; // Speed & Environment
    if (currentStep === 4 && !showGeometryDetails) return 4; // Constraints
    if (currentStep === 5 && showGeometryDetails) return 4; // Constraints
    if (currentStep === 5 && !showGeometryDetails) return 5; // Options
    if (currentStep === 6 && showGeometryDetails) return 5; // Options
    return currentStep;
  };

  const handleHome = () => navigate("/dashboard");

  const handleLogout = () => {
    authStore.logout();
    navigate("/login");
  };

  const handleSubmit = async () => {
    setIsGenerating(true);
    try {
      let missionCaseId: string;

      if (existingMissionCaseId) {
        await sizingStore.updateMissionCase(
          existingMissionCaseId,
          formData as CreateMissionCaseDto
        );
        missionCaseId = existingMissionCaseId;
      } else {
        const mission = await sizingStore.createMissionCase(formData as CreateMissionCaseDto);
        missionCaseId = mission.id;
      }

      // Extract additionalParameters from shipdInputsJson
      let additionalParameters: Record<string, unknown> | undefined;
      if (formData.shipdInputsJson) {
        try {
          const parsed = JSON.parse(formData.shipdInputsJson);
          if (parsed.additionalParameters) {
            additionalParameters = parsed.additionalParameters;
          }
        } catch {
          // Ignore parse errors
        }
      }

      const run = await sizingStore.runSolver({
        missionCaseId,
        mode: solverMode,
        options: {
          maxCandidates: solverMaxCandidates,
          additionalParameters: additionalParameters,
        },
        vesselCategory: formData.missionCategory,
        vesselType: formData.missionType,
        bowFamily: formData.bowFamily,
        midshipFamily: formData.midshipFamily,
        sternFamily: formData.sternFamily,
        familyMaskVersion: formData.familyMaskVersion,
        shipdInputVectorJson: formData.shipdInputVectorJson,
      });

      navigate(`/sizing/runs/${run.id}`);
    } catch (error) {
      console.error("Failed to create brief and run solver:", error);
      toast.error(getErrorMessage(error));
      setIsGenerating(false);
    }
  };

  const renderStepContent = () => {
    const actualStep = getActualStepNumber();
    const isFirstStep = currentStep === 1;
    const isLastStep = currentStep === steps.length;

    switch (actualStep) {
      case 1:
        return (
          <Step1MissionCargo
            formData={formData}
            updateFormData={updateFormData}
            onNext={nextStep}
            onPrevious={previousStep}
            onSubmit={handleSubmit}
            isFirstStep={isFirstStep}
            isLastStep={isLastStep}
            metadataLoading={sizingStore.isShipdMetadataLoading}
            metadataError={sizingStore.shipdMetadataError}
            categoryOptions={categoryOptions}
            vesselTypeOptions={vesselTypeOptions}
            onReloadMetadata={() => sizingStore.ensureShipDMetadataLoaded()}
            nameConflict={missionNameConflict}
            nameConflictMessage={missionNameConflictMessage}
          />
        );
      case 2:
        return (
          <Step2HullFamilies
            formData={formData}
            updateFormData={updateFormData}
            onNext={nextStep}
            onPrevious={previousStep}
            onSubmit={handleSubmit}
            isFirstStep={isFirstStep}
            isLastStep={isLastStep}
            taxonomyEntry={taxonomyEntry}
            metadataLoading={sizingStore.isShipdMetadataLoading}
            metadataError={sizingStore.shipdMetadataError}
          />
        );
      case "geometry":
        return (
          <Step2bHullGeometryDetails
            formData={formData}
            updateFormData={updateFormData}
            onNext={nextStep}
            onPrevious={previousStep}
            onSubmit={handleSubmit}
            isFirstStep={false}
            isLastStep={false}
            bowFamily={formData.bowFamily}
            midshipFamily={formData.midshipFamily}
            sternFamily={formData.sternFamily}
            taxonomyEntry={taxonomyEntry}
          />
        );
      case 3:
        return (
          <Step2SpeedEnvironment
            formData={formData}
            updateFormData={updateFormData}
            onNext={nextStep}
            onPrevious={previousStep}
            onSubmit={handleSubmit}
            isFirstStep={isFirstStep}
            isLastStep={isLastStep}
          />
        );
      case 4:
        return (
          <Step3Constraints
            formData={formData}
            updateFormData={updateFormData}
            onNext={nextStep}
            onPrevious={previousStep}
            onSubmit={handleSubmit}
            isFirstStep={isFirstStep}
            isLastStep={isLastStep}
          />
        );
      case 5:
      default:
        return (
          <Step4Options
            formData={formData}
            updateFormData={updateFormData}
            solverMode={solverMode}
            setSolverMode={setSolverMode}
            solverMaxCandidates={solverMaxCandidates}
            setSolverMaxCandidates={setSolverMaxCandidates}
            onNext={nextStep}
            onPrevious={previousStep}
            onSubmit={handleSubmit}
            isFirstStep={isFirstStep}
            isLastStep={isLastStep}
            isGenerating={isGenerating}
          />
        );
    }
  };

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
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-foreground">Hull Sizing Wizard</h1>
            <p className="mt-2 text-muted-foreground">
              Define your brief requirements and choose your solver mode for optimal hull designs
            </p>

            {aiGeneratedMission && (
              <div className="mt-4 rounded-lg border border-blue-200 bg-blue-50 p-4 dark:border-blue-800 dark:bg-blue-900/20">
                <div className="flex items-center gap-2">
                  <Sparkles className="h-5 w-5 text-blue-600 dark:text-blue-400" />
                  <p className="text-sm text-blue-800 dark:text-blue-300">
                    Mission parameters pre-filled by AI Copilot. Review and adjust as needed before
                    generating candidates.
                  </p>
                </div>
              </div>
            )}

            {isAdjustingAfterFailure && (
              <div className="mt-4 rounded-lg border border-yellow-200 bg-yellow-50 p-4 dark:border-yellow-800 dark:bg-yellow-900/20">
                <div className="flex items-start gap-3">
                  <AlertTriangle className="mt-0.5 h-5 w-5 flex-shrink-0 text-yellow-600 dark:text-yellow-400" />
                  <div className="flex-1">
                    <p className="text-sm font-medium text-yellow-900 dark:text-yellow-300">
                      Adjusting Parameters After Solver Failure
                    </p>
                    <p className="mt-1 text-sm text-yellow-800 dark:text-yellow-400">
                      {getStepInferenceReason(diagnostics)}. Your previous values have been restored
                      for editing.
                    </p>
                  </div>
                </div>
              </div>
            )}
          </div>

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

          <div className="relative rounded-lg bg-card p-8 shadow">
            {isGenerating && (
              <div className="absolute inset-0 z-50 flex flex-col items-center justify-center rounded-lg bg-card/90 backdrop-blur-sm">
                <div className="flex flex-col items-center space-y-4">
                  <div className="h-16 w-16 animate-spin rounded-full border-4 border-blue-200 border-t-blue-600" />
                  <div className="text-center">
                    <p className="text-lg font-semibold text-foreground">
                      🧮 Generating Hull Designs...
                    </p>
                    <p className="mt-1 text-sm text-muted-foreground">
                      Running first-principles solver
                    </p>
                    <p className="mt-1 text-xs text-muted-foreground">
                      This usually takes 1-2 seconds
                    </p>
                  </div>
                </div>
              </div>
            )}

            {renderStepContent()}
          </div>

          {sizingStore.error && (
            <div className="mt-4 rounded-lg bg-red-50 p-4 text-red-800 dark:bg-red-900/20 dark:text-red-400">
              {sizingStore.error}
            </div>
          )}
        </div>
      </main>

      <Footer />
      <UserSettingsDialog isOpen={showSettings} onClose={() => setShowSettings(false)} />
    </div>
  );
});
