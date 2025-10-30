import { useEffect, useState } from "react";
import { vesselsApi } from "../../../services/hydrostaticsApi";
import type { VesselTemplate } from "../../../types/hydrostatics";

interface TemplateGalleryProps {
  onSelectTemplate: (template: VesselTemplate) => void;
}

export function TemplateGallery({ onSelectTemplate }: TemplateGalleryProps) {
  const [templates, setTemplates] = useState<VesselTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [expanded, setExpanded] = useState(true);

  useEffect(() => {
    const loadTemplates = async () => {
      try {
        const data = await vesselsApi.getTemplates();
        setTemplates(data);
      } catch (error) {
        console.error("Failed to load templates:", error);
      } finally {
        setLoading(false);
      }
    };

    loadTemplates();
  }, []);

  const handleSurpriseMe = () => {
    if (templates.length > 0) {
      const randomTemplate = templates[Math.floor(Math.random() * templates.length)];
      onSelectTemplate(randomTemplate);
    }
  };

  return (
    <div className="rounded-xl border border-border p-3">
      <div className="flex items-center justify-between mb-2">
        <button
          onClick={() => setExpanded(!expanded)}
          className="flex items-center gap-2 text-sm font-semibold hover:text-primary"
        >
          <svg
            className={`h-4 w-4 transition-transform ${expanded ? "rotate-90" : ""}`}
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
          </svg>
          Start from Template
        </button>
        {expanded && (
          <button
            onClick={handleSurpriseMe}
            disabled={loading || templates.length === 0}
            className="text-[11px] px-2 py-1 rounded-lg border border-border hover:bg-accent/10 disabled:opacity-50"
          >
            Surprise me
          </button>
        )}
      </div>

      {expanded && (
        <div className="mt-2">
          {loading ? (
            <div className="text-xs text-muted-foreground py-4 text-center">
              Loading templates...
            </div>
          ) : templates.length === 0 ? (
            <div className="text-xs text-muted-foreground py-4 text-center">
              No templates available
            </div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
              {templates.map((template) => (
                <button
                  key={template.id}
                  onClick={() => onSelectTemplate(template)}
                  className="text-left rounded-lg border border-border p-2 hover:bg-accent/10 transition-colors"
                >
                  <div className="text-xs text-muted-foreground">Template</div>
                  <div className="text-sm font-medium">{template.name}</div>
                  {template.description && (
                    <div className="text-[11px] text-muted-foreground mt-1 line-clamp-2">
                      {template.description}
                    </div>
                  )}
                </button>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
