import { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { vesselsApi } from "../../services/hydrostaticsApi";
import { getErrorMessage } from "../../types/errors";
import type { Vessel } from "../../types/hydrostatics";
import { Button } from "../ui/button";
import { settingsStore } from "../../stores/SettingsStore";
import { getUnitSymbol } from "../../utils/unitSymbols";

interface VesselSelectionDialogProps {
  onClose: () => void;
}

export function VesselSelectionDialog({ onClose }: VesselSelectionDialogProps) {
  const navigate = useNavigate();
  const [vessels, setVessels] = useState<Vessel[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadVessels();
  }, []);

  const loadVessels = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await vesselsApi.list();
      setVessels(data.vessels || []);
    } catch (err) {
      setError(getErrorMessage(err));
      setVessels([]);
    } finally {
      setLoading(false);
    }
  };

  const handleVesselSelect = (vessel: Vessel) => {
    // Create vessel snapshot for seakeeping
    const vesselSnapshot = {
      id: vessel.id,
      name: vessel.name,
      lpp: vessel.lpp,
      beam: vessel.beam,
      draft: vessel.designDraft,
      displacement: 0, // Will be calculated in seakeeping if needed
      units: vessel.units || "SI",
    };
    navigate(`/seakeeping/${vessel.id}`, { state: { vesselSnapshot } });
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-card border border-border rounded-lg shadow-xl max-w-2xl w-full mx-4 max-h-[80vh] flex flex-col">
        {/* Header */}
        <div className="px-6 py-4 border-b border-border flex items-center justify-between">
          <div>
            <h2 className="text-xl font-bold text-foreground">Select a Vessel</h2>
            <p className="text-sm text-muted-foreground mt-1">
              Choose a vessel from Hydrostatics to analyze
            </p>
          </div>
          <button
            onClick={onClose}
            className="text-muted-foreground hover:text-foreground"
            title="Close"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M6 18L18 6M6 6l12 12"
              />
            </svg>
          </button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-6">
          {loading && (
            <div className="flex items-center justify-center py-12">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
            </div>
          )}

          {error && (
            <div className="bg-destructive/10 border border-destructive/20 rounded-lg p-4 text-center">
              <p className="text-sm text-destructive">{error}</p>
              <Button onClick={loadVessels} variant="outline" className="mt-3">
                Try Again
              </Button>
            </div>
          )}

          {!loading && !error && vessels.length === 0 && (
            <div className="text-center py-12">
              <svg
                className="mx-auto h-12 w-12 text-muted-foreground"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"
                />
              </svg>
              <h3 className="mt-2 text-sm font-medium text-foreground">No Vessels Found</h3>
              <p className="mt-1 text-sm text-muted-foreground">
                Create a vessel in Hydrostatics first.
              </p>
              <Button onClick={() => navigate("/hydrostatics/vessels/create")} className="mt-4">
                Create Vessel
              </Button>
            </div>
          )}

          {!loading && !error && vessels.length > 0 && (
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {vessels.map((vessel) => (
                <button
                  key={vessel.id}
                  onClick={() => handleVesselSelect(vessel)}
                  className="bg-background border border-border rounded-lg p-4 hover:border-primary hover:bg-primary/5 transition-all text-left group"
                >
                  <div className="flex items-start">
                    <div className="flex-shrink-0">
                      <svg
                        className="h-8 w-8 text-primary group-hover:text-primary"
                        fill="none"
                        viewBox="0 0 24 24"
                        stroke="currentColor"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          strokeWidth={2}
                          d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"
                        />
                      </svg>
                    </div>
                    <div className="ml-3 flex-1 min-w-0">
                      <h3 className="text-base font-semibold text-foreground group-hover:text-primary truncate">
                        {vessel.name}
                      </h3>
                      {vessel.description && (
                        <p className="mt-1 text-xs text-muted-foreground line-clamp-2">
                          {vessel.description}
                        </p>
                      )}
                      <div className="mt-2 grid grid-cols-3 gap-2 text-xs">
                        <div>
                          <span className="text-muted-foreground">Lpp:</span>
                          <span className="ml-1 font-medium text-foreground">
                            {vessel.lpp}
                            {getUnitSymbol(settingsStore.preferredUnits, "Length")}
                          </span>
                        </div>
                        <div>
                          <span className="text-muted-foreground">B:</span>
                          <span className="ml-1 font-medium text-foreground">
                            {vessel.beam}
                            {getUnitSymbol(settingsStore.preferredUnits, "Length")}
                          </span>
                        </div>
                        <div>
                          <span className="text-muted-foreground">T:</span>
                          <span className="ml-1 font-medium text-foreground">
                            {vessel.designDraft}
                            {getUnitSymbol(settingsStore.preferredUnits, "Length")}
                          </span>
                        </div>
                      </div>
                      {vessel.isTemplate && (
                        <span className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-medium bg-blue-100 dark:bg-blue-900/30 text-blue-800 dark:text-blue-300 border border-blue-200 dark:border-blue-800 mt-2">
                          Template
                        </span>
                      )}
                    </div>
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-6 py-4 border-t border-border flex justify-between items-center">
          <Button variant="outline" onClick={() => navigate("/hydrostatics")}>
            Go to Hydrostatics
          </Button>
          <Button variant="ghost" onClick={onClose}>
            Cancel
          </Button>
        </div>
      </div>
    </div>
  );
}
