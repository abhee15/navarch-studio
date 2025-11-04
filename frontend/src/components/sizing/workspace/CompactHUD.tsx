import { useState } from "react";
import type { CandidateDesign } from "../../../types/sizing";
import { Check, AlertTriangle, Info, ChevronDown, ChevronUp } from "lucide-react";
import { Button } from "../../ui/button";

interface CompactHUDProps {
  candidate: CandidateDesign;
}

/**
 * Compact HUD - Critical metrics at a glance
 *
 * Shows only essential KPIs with option to expand for full details
 * Inspired by Hydrostatics HUD design
 */
export const CompactHUD: React.FC<CompactHUDProps> = ({ candidate }) => {
  const [isExpanded, setIsExpanded] = useState(false);

  // Parse flags
  let flags: string[] = [];
  try {
    flags = candidate.flagsJson ? JSON.parse(candidate.flagsJson) : [];
  } catch {
    flags = [];
  }

  const hasWarnings = flags.length > 0;
  const hasCritical = flags.some(
    (f) =>
      f.includes("loa_exceeded") ||
      f.includes("beam_constrained") ||
      f.includes("draft_constrained")
  );

  // Critical metrics only for compact view
  const criticalMetrics = [
    { label: "Lpp", value: candidate.lppM?.toFixed(1), unit: "m" },
    { label: "B", value: candidate.beamM?.toFixed(1), unit: "m" },
    { label: "T", value: candidate.draftM?.toFixed(1), unit: "m" },
    { label: "Δ", value: candidate.dispT?.toFixed(0), unit: "t" },
    { label: "Fn", value: candidate.fn?.toFixed(3), unit: "" },
    { label: "Score", value: (candidate.score * 100).toFixed(0), unit: "%" },
  ];

  // Calculate ratios for expanded view
  const lOverB = candidate.lppM && candidate.beamM ? candidate.lppM / candidate.beamM : 0;
  const bOverT = candidate.beamM && candidate.draftM ? candidate.beamM / candidate.draftM : 0;

  return (
    <div className="bg-card border border-border rounded-lg shadow-sm">
      {/* Compact HUD - Always Visible */}
      <div className="p-3">
        <div className="flex items-center justify-between mb-2">
          <div className="flex items-center gap-2">
            <h3 className="text-sm font-semibold text-foreground">
              {candidate.hullFamily.replace("_", " ").toUpperCase()}
            </h3>
            {candidate.isSelected && (
              <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-accent/20 text-accent-foreground text-xs font-medium">
                <Check className="h-3 w-3" />
                Selected
              </span>
            )}
            {hasWarnings && (
              <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-destructive/20 text-destructive text-xs font-medium">
                {hasCritical ? <AlertTriangle className="h-3 w-3" /> : <Info className="h-3 w-3" />}
                {flags.length}
              </span>
            )}
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setIsExpanded(!isExpanded)}
            className="h-7 text-xs"
          >
            {isExpanded ? (
              <>
                <ChevronUp className="h-3 w-3 mr-1" />
                Less
              </>
            ) : (
              <>
                <ChevronDown className="h-3 w-3 mr-1" />
                More
              </>
            )}
          </Button>
        </div>

        {/* Critical Metrics Grid */}
        <div className="grid grid-cols-3 sm:grid-cols-6 gap-3">
          {criticalMetrics.map((metric) => (
            <div key={metric.label} className="text-center">
              <div className="text-xs text-muted-foreground mb-0.5">{metric.label}</div>
              <div className="text-sm font-semibold text-foreground">
                {metric.value || "—"}
                {metric.value && metric.unit && (
                  <span className="text-xs text-muted-foreground ml-0.5">{metric.unit}</span>
                )}
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Expanded Details */}
      {isExpanded && (
        <div className="border-t border-border p-4 space-y-4 bg-muted/30">
          {/* Form Coefficients */}
          <div>
            <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-2">
              Form Coefficients
            </h4>
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
              <div>
                <div className="text-xs text-muted-foreground">CB</div>
                <div className="text-sm font-semibold text-foreground">
                  {candidate.cb?.toFixed(3) || "—"}
                </div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground">CP</div>
                <div className="text-sm font-semibold text-foreground">
                  {candidate.cp?.toFixed(3) || "—"}
                </div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground">CWP</div>
                <div className="text-sm font-semibold text-foreground">
                  {candidate.cwp?.toFixed(3) || "—"}
                </div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground">CM</div>
                <div className="text-sm font-semibold text-foreground">
                  {candidate.cm?.toFixed(3) || "—"}
                </div>
              </div>
            </div>
          </div>

          {/* Dimensional Ratios */}
          <div>
            <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-2">
              Dimensional Ratios
            </h4>
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
              <div>
                <div className="text-xs text-muted-foreground">L/B</div>
                <div className="text-sm font-semibold text-foreground">
                  {lOverB > 0 ? lOverB.toFixed(2) : "—"}
                </div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground">B/T</div>
                <div className="text-sm font-semibold text-foreground">
                  {bOverT > 0 ? bOverT.toFixed(2) : "—"}
                </div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground">Lwl/λ</div>
                <div className="text-sm font-semibold text-foreground">
                  {candidate.lwlOverLambda?.toFixed(2) || "—"}
                </div>
              </div>
            </div>
          </div>

          {/* Performance */}
          <div>
            <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-2">
              Performance
            </h4>
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
              <div>
                <div className="text-xs text-muted-foreground">EHP</div>
                <div className="text-sm font-semibold text-foreground">
                  {candidate.ehpKw?.toFixed(0) || "—"}
                  {candidate.ehpKw && (
                    <span className="text-xs text-muted-foreground ml-0.5">kW</span>
                  )}
                </div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground">SHP</div>
                <div className="text-sm font-semibold text-foreground">
                  {candidate.shpKw?.toFixed(0) || "—"}
                  {candidate.shpKw && (
                    <span className="text-xs text-muted-foreground ml-0.5">kW</span>
                  )}
                </div>
              </div>
              <div>
                <div className="text-xs text-muted-foreground">Rank</div>
                <div className="text-sm font-semibold text-foreground">#{candidate.rank}</div>
              </div>
            </div>
          </div>

          {/* Flags/Warnings */}
          {hasWarnings && (
            <div
              className={`rounded-lg p-3 border ${
                hasCritical
                  ? "bg-destructive/5 border-destructive/20"
                  : "bg-accent/10 border-accent/20"
              }`}
            >
              <h4
                className={`text-xs font-semibold uppercase tracking-wide mb-2 ${
                  hasCritical ? "text-destructive" : "text-accent-foreground"
                }`}
              >
                {hasCritical ? "⚠️ Warnings" : "ℹ️ Flags"}
              </h4>
              <ul className="space-y-1 text-xs">
                {flags.map((flag) => (
                  <li
                    key={flag}
                    className={`flex items-start ${
                      hasCritical ? "text-destructive" : "text-accent-foreground"
                    }`}
                  >
                    <span className="mr-2">•</span>
                    <span>{flag}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
