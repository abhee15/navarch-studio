import { useState } from "react";
import { useNavigate } from "react-router-dom";
import toast from "react-hot-toast";
import { vesselsApi } from "../../services/hydrostaticsApi";
import type { CreateVesselDto, VesselTemplate } from "../../types/hydrostatics";
import { BuilderSections } from "../../components/hydrostatics/builder/BuilderSections";
import { PreviewPanel } from "../../components/hydrostatics/builder/PreviewPanel";
import { LiveResultsPanel } from "../../components/hydrostatics/builder/LiveResultsPanel";
import { MobileActionBar } from "../../components/hydrostatics/builder/MobileActionBar";
import { TemplateGallery } from "../../components/hydrostatics/builder/TemplateGallery";

export function VesselBuilder() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [quickMode, setQuickMode] = useState(
    typeof window !== "undefined" && window.innerWidth < 768
  );
  const [creationMode, setCreationMode] = useState<"template" | "manual" | null>(null);

  const [formData, setFormData] = useState<CreateVesselDto>({
    name: "New Vessel",
    description: "",
    lpp: 100,
    beam: 20,
    designDraft: 10,
    metadata: {
      vesselType: "Boat",
      size: "Small",
      blockCoefficient: 0.5,
      hullFamily: "Wigley",
    },
  });

  const handleChange = (data: Partial<CreateVesselDto>) => {
    setFormData((prev) => ({
      ...prev,
      ...data,
    }));
  };

  const handleTemplateSelect = (template: VesselTemplate) => {
    // Initialize form with template data
    setFormData({
      name: template.preset.name,
      description: template.preset.description || "",
      lpp: template.preset.lpp,
      beam: template.preset.beam,
      designDraft: template.preset.designDraft,
      metadata: template.preset.metadata || {
        vesselType: "Boat",
        size: "Small",
        blockCoefficient: 0.5,
        hullFamily: "Wigley",
      },
      materials: template.preset.materials,
      loading: template.preset.loading,
    });
    // Move to form view (template gallery will be hidden)
    setCreationMode("manual");
  };

  const handleStartFromTemplate = () => {
    setCreationMode("template");
  };

  const handleCreateManually = () => {
    // Reset to default values for manual entry
    setFormData({
      name: "",
      description: "",
      lpp: 100,
      beam: 20,
      designDraft: 10,
      metadata: {
        vesselType: "Boat",
        size: "Small",
        blockCoefficient: 0.5,
        hullFamily: "Wigley",
      },
    });
    setCreationMode("manual");
  };

  const handleCancel = () => {
    navigate("/hydrostatics/vessels");
  };

  const handleSave = async () => {
    if (!formData.name.trim()) {
      toast.error("Vessel name is required");
      return;
    }

    if (formData.lpp <= 0 || formData.beam <= 0 || formData.designDraft <= 0) {
      toast.error("Principal dimensions must be greater than zero");
      return;
    }

    try {
      setLoading(true);
      const vessel = await vesselsApi.create(formData);
      toast.success("Vessel created successfully!");
      navigate(`/hydrostatics/vessels/${vessel.id}`);
    } catch (error) {
      console.error("Failed to create vessel:", error);
      toast.error(error instanceof Error ? error.message : "Failed to create vessel");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-background flex flex-col">
      {/* Header */}
      <header className="border-b border-border bg-card/80 backdrop-blur-sm flex-shrink-0">
        <div className="px-4 py-3">
          <div className="max-w-[1500px] mx-auto flex items-center justify-between">
            <div>
              <h1 className="text-2xl font-semibold tracking-tight">Vessel Builder</h1>
              <p className="text-muted-foreground text-sm">
                Create a vessel with optional configuration sections
              </p>
            </div>

            {/* Desktop Actions - Only show when form is visible */}
            {creationMode === "manual" && (
              <div className="hidden md:flex items-center gap-3">
                <label className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    className="h-4 w-4"
                    checked={quickMode}
                    onChange={(e) => setQuickMode(e.target.checked)}
                  />
                  Quick Create
                </label>
                <button
                  onClick={handleCancel}
                  disabled={loading}
                  className="px-3 py-2 rounded-xl bg-card shadow border border-border text-sm hover:bg-accent/10 disabled:opacity-50"
                >
                  Cancel
                </button>
                <button
                  onClick={handleSave}
                  disabled={loading}
                  className="px-3 py-2 rounded-xl bg-primary text-primary-foreground text-sm shadow hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  {loading ? "Saving..." : "Save Vessel"}
                </button>
              </div>
            )}
            {/* Show Cancel button in template selection mode */}
            {creationMode === "template" && (
              <div className="hidden md:flex items-center gap-3">
                <button
                  onClick={handleCancel}
                  className="px-3 py-2 rounded-xl bg-card shadow border border-border text-sm hover:bg-accent/10"
                >
                  Cancel
                </button>
              </div>
            )}
            {/* Show Cancel button in initial choice mode */}
            {creationMode === null && (
              <div className="hidden md:flex items-center gap-3">
                <button
                  onClick={handleCancel}
                  className="px-3 py-2 rounded-xl bg-card shadow border border-border text-sm hover:bg-accent/10"
                >
                  Cancel
                </button>
              </div>
            )}
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="flex-1 p-4 md:p-6 pb-24 md:pb-6">
        <div className="max-w-[1500px] mx-auto">
          {creationMode === null ? (
            /* Initial Choice Screen */
            <div className="flex items-center justify-center min-h-[60vh]">
              <div className="w-full max-w-4xl">
                <h2 className="text-2xl font-semibold text-center mb-8">
                  How would you like to create your vessel?
                </h2>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                  {/* Start from Template Card */}
                  <button
                    onClick={handleStartFromTemplate}
                    className="group relative bg-card rounded-2xl border-2 border-border p-8 hover:border-primary transition-all hover:shadow-lg text-left"
                  >
                    <div className="flex items-start gap-4">
                      <div className="flex-shrink-0 w-12 h-12 rounded-xl bg-primary/10 flex items-center justify-center group-hover:bg-primary/20 transition-colors">
                        <svg
                          className="w-6 h-6 text-primary"
                          fill="none"
                          viewBox="0 0 24 24"
                          stroke="currentColor"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM4 13a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zM16 13a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z"
                          />
                        </svg>
                      </div>
                      <div className="flex-1">
                        <h3 className="text-xl font-semibold mb-2">Start from Template</h3>
                        <p className="text-sm text-muted-foreground">
                          Choose from pre-configured vessel templates to get started quickly with
                          common vessel types and configurations.
                        </p>
                      </div>
                    </div>
                  </button>

                  {/* Create Manually Card */}
                  <button
                    onClick={handleCreateManually}
                    className="group relative bg-card rounded-2xl border-2 border-border p-8 hover:border-primary transition-all hover:shadow-lg text-left"
                  >
                    <div className="flex items-start gap-4">
                      <div className="flex-shrink-0 w-12 h-12 rounded-xl bg-primary/10 flex items-center justify-center group-hover:bg-primary/20 transition-colors">
                        <svg
                          className="w-6 h-6 text-primary"
                          fill="none"
                          viewBox="0 0 24 24"
                          stroke="currentColor"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            strokeWidth={2}
                            d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
                          />
                        </svg>
                      </div>
                      <div className="flex-1">
                        <h3 className="text-xl font-semibold mb-2">Create Manually</h3>
                        <p className="text-sm text-muted-foreground">
                          Enter vessel specifications from scratch. You'll have full control over
                          all parameters and dimensions.
                        </p>
                      </div>
                    </div>
                  </button>
                </div>
              </div>
            </div>
          ) : creationMode === "template" ? (
            /* Template Gallery View */
            <div className="max-w-4xl mx-auto">
              <div className="mb-6">
                <button
                  onClick={() => setCreationMode(null)}
                  className="flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors"
                >
                  <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M15 19l-7-7 7-7"
                    />
                  </svg>
                  Back to selection
                </button>
                <h2 className="text-2xl font-semibold mt-4">Select a Template</h2>
                <p className="text-muted-foreground text-sm mt-2">
                  Choose a template to pre-fill your vessel configuration
                </p>
              </div>
              <TemplateGallery onSelectTemplate={handleTemplateSelect} showHeader={false} />
            </div>
          ) : (
            /* Form View */
            <div className="grid grid-cols-12 gap-4">
              {/* Left: Builder Sections */}
              <aside className="col-span-12 lg:col-span-3">
                <BuilderSections
                  formData={formData}
                  onChange={handleChange}
                  quickMode={quickMode}
                />
              </aside>

              {/* Center: Preview */}
              <section className="col-span-12 lg:col-span-6">
                <PreviewPanel
                  loa={formData.lpp}
                  beam={formData.beam}
                  draft={formData.designDraft}
                  cb={formData.metadata?.blockCoefficient || 0.5}
                  hullFamily={formData.metadata?.hullFamily || "Wigley"}
                />
              </section>

              {/* Right: Live Results */}
              <aside className="col-span-12 lg:col-span-3">
                <LiveResultsPanel
                  loa={formData.lpp}
                  beam={formData.beam}
                  draft={formData.designDraft}
                  cb={formData.metadata?.blockCoefficient || 0.5}
                />
              </aside>
            </div>
          )}
        </div>
      </main>

      {/* Mobile Action Bar - Only show when form is visible */}
      {creationMode === "manual" && (
        <MobileActionBar onCancel={handleCancel} onSave={handleSave} loading={loading} />
      )}
    </div>
  );
}

export default VesselBuilder;
