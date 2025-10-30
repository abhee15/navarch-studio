import { observer } from "mobx-react-lite";
import type { VesselDetails, Loadcase } from "../../../types/hydrostatics";
import { CollapsibleSection } from "../CollapsibleSection";
import { settingsStore } from "../../../stores/SettingsStore";
import { getUnitSymbol } from "../../../utils/unitSymbols";

interface EditModeLayoutProps {
  vessel: VesselDetails | null;
  loadcases: Loadcase[];
  selectedLoadcaseId: string;
  waterType: string;
  minDraft: number;
  maxDraft: number;
  draftStep: number;
  integrationRule: string;
  smoothing: string;
  kg: number;
  lcg: number;
  tcg: number;
  onLoadcaseChange: (id: string) => void;
  onWaterTypeChange: (type: string) => void;
  onMinDraftChange: (value: number) => void;
  onMaxDraftChange: (value: number) => void;
  onDraftStepChange: (value: number) => void;
  onIntegrationRuleChange: (rule: string) => void;
  onSmoothingChange: (smoothing: string) => void;
  onKgChange: (value: number) => void;
  onLcgChange: (value: number) => void;
  onTcgChange: (value: number) => void;
  onEditGeometry: () => void;
  onManageLoadcases: () => void;
}

const waterTypes = [
  { label: "Fresh Water", density: 1000 },
  { label: "Salt Water", density: 1025 },
  { label: "Brackish Water", density: 1012 },
];

const integrationRules = ["Simpson 1/3", "Trapezoidal", "Simpson 3/8"];
const smoothingOptions = ["None", "Moving Average", "Spline"];

export const EditModeLayout = observer(
  ({
    vessel,
    loadcases,
    selectedLoadcaseId,
    waterType,
    minDraft,
    maxDraft,
    draftStep,
    integrationRule,
    smoothing,
    kg,
    lcg,
    tcg,
    onLoadcaseChange,
    onWaterTypeChange,
    onMinDraftChange,
    onMaxDraftChange,
    onDraftStepChange,
    onIntegrationRuleChange,
    onSmoothingChange,
    onKgChange,
    onLcgChange,
    onTcgChange,
    onEditGeometry,
    onManageLoadcases,
  }: EditModeLayoutProps) => {
    const displayUnits = settingsStore.preferredUnits;
    const lengthUnit = getUnitSymbol(displayUnits, "Length");

    const draftCount = minDraft < maxDraft ? Math.ceil((maxDraft - minDraft) / draftStep) + 1 : 0;

    return (
      <div className="h-full flex bg-background">
        {/* Left Panel - Input Controls */}
        <div className="w-80 bg-card border-r border-border overflow-y-auto overflow-x-hidden flex-shrink-0 px-3 py-4">
          {/* Geometry Summary */}
          <CollapsibleSection title="Geometry" defaultExpanded={true}>
            <div className="space-y-2 min-w-0">
              {vessel && (
                <div className="space-y-1.5 min-w-0">
                  <div className="text-[11px]">
                    <span className="text-muted-foreground">Lpp:</span>
                    <span className="ml-1 font-medium text-foreground">
                      {vessel.lpp} {lengthUnit}
                    </span>
                  </div>
                  <div className="text-[11px]">
                    <span className="text-muted-foreground">Beam:</span>
                    <span className="ml-1 font-medium text-foreground">
                      {vessel.beam} {lengthUnit}
                    </span>
                  </div>
                  <div className="text-[11px]">
                    <span className="text-muted-foreground">Design Draft:</span>
                    <span className="ml-1 font-medium text-foreground">
                      {vessel.designDraft} {lengthUnit}
                    </span>
                  </div>
                  <div className="text-[11px] mt-2">
                    <span className="text-muted-foreground">Stations:</span>
                    <span className="ml-1 font-medium text-foreground">{vessel.stationsCount}</span>
                    <span className="mx-1 text-muted-foreground">•</span>
                    <span className="text-muted-foreground">Waterlines:</span>
                    <span className="ml-1 font-medium text-foreground">
                      {vessel.waterlinesCount}
                    </span>
                  </div>
                </div>
              )}
              {vessel && (
                <div className="text-[11px] mt-2">
                  <span className="text-muted-foreground">Offsets:</span>
                  <span className="ml-1 font-medium text-foreground">{vessel.offsetsCount}</span>
                </div>
              )}
              <button
                onClick={onEditGeometry}
                className="w-full mt-2 inline-flex items-center justify-center px-3 py-1.5 border border-border text-xs font-medium rounded text-foreground bg-background hover:bg-accent focus:outline-none focus:ring-2 focus:ring-ring"
              >
                {vessel &&
                (vessel.stationsCount ?? 0) > 0 &&
                (vessel.waterlinesCount ?? 0) > 0 &&
                (vessel.offsetsCount ?? 0) > 0 ? (
                  <>
                    <svg
                      className="w-3.5 h-3.5 mr-1.5"
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
                    Manage Geometry
                  </>
                ) : (
                  <>
                    <svg
                      className="w-3.5 h-3.5 mr-1.5"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        strokeWidth={2}
                        d="M12 4v16m8-8H4"
                      />
                    </svg>
                    Add Geometry
                  </>
                )}
              </button>
            </div>
          </CollapsibleSection>

          {/* Loadcases */}
          <CollapsibleSection title="Loadcase" defaultExpanded={true}>
            <div className="space-y-2 min-w-0">
              <select
                value={selectedLoadcaseId}
                onChange={(e) => onLoadcaseChange(e.target.value)}
                className="w-full border border-border bg-background text-foreground rounded text-xs py-1.5 px-2 focus:outline-none focus:ring-2 focus:ring-ring"
              >
                <option value="">Design Waterline</option>
                {loadcases.map((lc) => (
                  <option key={lc.id} value={lc.id}>
                    {lc.name}
                  </option>
                ))}
              </select>
              <button
                onClick={onManageLoadcases}
                className="w-full inline-flex items-center justify-center px-3 py-1.5 border border-border text-xs font-medium rounded text-foreground bg-background hover:bg-accent focus:outline-none focus:ring-2 focus:ring-ring"
              >
                Manage Loadcases
              </button>
            </div>
          </CollapsibleSection>

          {/* Computation Controls */}
          <CollapsibleSection title="Computation Controls" defaultExpanded={true}>
            <div className="space-y-2 min-w-0">
              <div className="min-w-0">
                <label className="block text-[11px] font-medium text-muted-foreground mb-0.5 truncate">
                  Range ({lengthUnit}) - {draftCount} pts
                </label>
                <div className="flex gap-0.5 items-center text-xs min-w-0">
                  <input
                    type="number"
                    value={minDraft}
                    onChange={(e) => onMinDraftChange(parseFloat(e.target.value) || 0)}
                    step="0.1"
                    placeholder="Min"
                    className="flex-1 min-w-0 border border-border bg-background text-foreground placeholder:text-muted-foreground rounded py-0.5 px-1 text-[11px] focus:outline-none focus:ring-2 focus:ring-ring"
                  />
                  <span className="text-muted-foreground text-[10px]">to</span>
                  <input
                    type="number"
                    value={maxDraft}
                    onChange={(e) => onMaxDraftChange(parseFloat(e.target.value) || 0)}
                    step="0.1"
                    placeholder="Max"
                    className="flex-1 min-w-0 border border-border bg-background text-foreground placeholder:text-muted-foreground rounded py-0.5 px-1 text-[11px] focus:outline-none focus:ring-2 focus:ring-ring"
                  />
                  <span className="text-muted-foreground text-[10px]">by</span>
                  <input
                    type="number"
                    value={draftStep}
                    onChange={(e) => onDraftStepChange(parseFloat(e.target.value) || 0.1)}
                    step="0.1"
                    placeholder="Step"
                    className="flex-1 min-w-0 border border-border bg-background text-foreground placeholder:text-muted-foreground rounded py-0.5 px-1 text-[11px] focus:outline-none focus:ring-2 focus:ring-ring"
                  />
                </div>
              </div>

              <div className="min-w-0">
                <label className="block text-[11px] font-medium text-muted-foreground mb-0.5 truncate">
                  Integration
                </label>
                <select
                  value={integrationRule}
                  onChange={(e) => onIntegrationRuleChange(e.target.value)}
                  className="w-full border border-border bg-background text-foreground rounded text-xs py-0.5 px-1.5 focus:outline-none focus:ring-2 focus:ring-ring"
                >
                  {integrationRules.map((rule) => (
                    <option key={rule} value={rule}>
                      {rule}
                    </option>
                  ))}
                </select>
              </div>

              <div className="min-w-0">
                <label className="block text-[11px] font-medium text-muted-foreground mb-0.5 truncate">
                  Smoothing
                </label>
                <select
                  value={smoothing}
                  onChange={(e) => onSmoothingChange(e.target.value)}
                  className="w-full border border-border bg-background text-foreground rounded text-xs py-0.5 px-1.5 focus:outline-none focus:ring-2 focus:ring-ring"
                >
                  {smoothingOptions.map((opt) => (
                    <option key={opt} value={opt}>
                      {opt}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          </CollapsibleSection>

          {/* Mass Properties */}
          <CollapsibleSection title="Mass Properties" defaultExpanded={true}>
            <div className="space-y-2 min-w-0">
              <div className="min-w-0">
                <label className="block text-[11px] font-medium text-muted-foreground mb-0.5 truncate">
                  KG ({lengthUnit})
                </label>
                <input
                  type="number"
                  value={kg}
                  onChange={(e) => onKgChange(parseFloat(e.target.value) || 0)}
                  step="0.1"
                  className="w-full border border-border bg-background text-foreground rounded text-xs py-0.5 px-1.5 focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>

              <div className="min-w-0">
                <label className="block text-[11px] font-medium text-muted-foreground mb-0.5 truncate">
                  LCG ({lengthUnit})
                </label>
                <input
                  type="number"
                  value={lcg}
                  onChange={(e) => onLcgChange(parseFloat(e.target.value) || 0)}
                  step="0.1"
                  className="w-full border border-border bg-background text-foreground rounded text-xs py-0.5 px-1.5 focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>

              <div className="min-w-0">
                <label className="block text-[11px] font-medium text-muted-foreground mb-0.5 truncate">
                  TCG ({lengthUnit})
                </label>
                <input
                  type="number"
                  value={tcg}
                  onChange={(e) => onTcgChange(parseFloat(e.target.value) || 0)}
                  step="0.1"
                  className="w-full border border-border bg-background text-foreground rounded text-xs py-0.5 px-1.5 focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>
            </div>
          </CollapsibleSection>

          {/* Environment */}
          <CollapsibleSection title="Environment" defaultExpanded={false}>
            <div className="space-y-2 min-w-0">
              <div className="min-w-0">
                <label className="block text-[11px] font-medium text-muted-foreground mb-0.5 truncate">
                  Water Type
                </label>
                <select
                  value={waterType}
                  onChange={(e) => onWaterTypeChange(e.target.value)}
                  className="w-full border border-border bg-background text-foreground rounded text-xs py-0.5 px-1.5 focus:outline-none focus:ring-2 focus:ring-ring"
                >
                  {waterTypes.map((wt) => (
                    <option key={wt.label} value={wt.label}>
                      {wt.label}
                    </option>
                  ))}
                </select>
              </div>
              <div className="text-[11px]">
                <span className="text-muted-foreground">Density:</span>
                <span className="ml-1 font-medium text-foreground">
                  {waterTypes.find((wt) => wt.label === waterType)?.density || 1025} kg/m³
                </span>
              </div>
            </div>
          </CollapsibleSection>
        </div>

        {/* Right Panel - Results Preview (Minimized) */}
        <div className="flex-1 overflow-auto bg-background flex items-center justify-center p-8">
          <div className="text-center max-w-md">
            <svg
              className="mx-auto h-16 w-16 text-muted-foreground"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z"
              />
            </svg>
            <h3 className="mt-4 text-lg font-medium text-foreground">Edit Mode</h3>
            <p className="mt-2 text-sm text-muted-foreground">
              Configure your parameters in the left panel. Click "Compute" in the toolbar to
              calculate hydrostatics and view results.
            </p>
          </div>
        </div>
      </div>
    );
  }
);
