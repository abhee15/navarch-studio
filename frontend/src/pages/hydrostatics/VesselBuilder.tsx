import { useState } from "react";
import { useNavigate } from "react-router-dom";
import toast from "react-hot-toast";
import { vesselsApi } from "../../services/hydrostaticsApi";
import type { CreateVesselDto } from "../../types/hydrostatics";
import { BuilderSections } from "../../components/hydrostatics/builder/BuilderSections";
import { PreviewPanel } from "../../components/hydrostatics/builder/PreviewPanel";
import { LiveResultsPanel } from "../../components/hydrostatics/builder/LiveResultsPanel";
import { MobileActionBar } from "../../components/hydrostatics/builder/MobileActionBar";

export function VesselBuilder() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [quickMode, setQuickMode] = useState(
    typeof window !== "undefined" && window.innerWidth < 768
  );

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

            {/* Desktop Actions */}
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
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="flex-1 p-4 md:p-6 pb-24 md:pb-6">
        <div className="max-w-[1500px] mx-auto">
          <div className="grid grid-cols-12 gap-4">
            {/* Left: Builder Sections */}
            <aside className="col-span-12 lg:col-span-3">
              <BuilderSections formData={formData} onChange={handleChange} quickMode={quickMode} />
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
        </div>
      </main>

      {/* Mobile Action Bar */}
      <MobileActionBar onCancel={handleCancel} onSave={handleSave} loading={loading} />
    </div>
  );
}

export default VesselBuilder;
