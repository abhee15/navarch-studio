import React, { useMemo } from "react";
import type { CandidateDesign } from "../../types/sizing";
import { Button } from "../ui/button";
import { Hull3DThumbnail } from "./visualization/Hull3DThumbnail";
import { AlertTriangle, Award, TrendingUp, Check, Database, Sparkles, Ship } from "lucide-react";

interface CandidateCardProps {
  candidate: CandidateDesign;
  rank: number;
  onSelect: () => void;
  onCompare: () => void;
  isComparing: boolean;
}

export const CandidateCard: React.FC<CandidateCardProps> = React.memo(
  ({ candidate, rank, onSelect, onCompare, isComparing }) => {
    // Parse flags
    let flags: string[] = [];
    try {
      flags = JSON.parse(candidate.flagsJson);
    } catch {
      // Ignore parse errors
    }

    const hasWarnings = flags.some((f) => f.includes("constrained") || f.includes("exceeded"));

    // Check if geometry generation failed
    const geometryGenerationFailed =
      candidate.geometryGenerationStatus === "BothFailed" ||
      candidate.geometryGenerationStatus === "FormCoefficientFailed";

    // Check if ShipD parameters are available
    const hasShipD = useMemo(() => {
      return !!candidate.shipdParametersJson || !!candidate.geometryJson;
    }, [candidate.shipdParametersJson, candidate.geometryJson]);

    // Parse ShipD vector to extract key geometry info
    const shipdGeometryInfo = useMemo(() => {
      if (!candidate.shipdParametersJson) return null;
      try {
        const vector = JSON.parse(candidate.shipdParametersJson);
        if (!Array.isArray(vector) || vector.length !== 45) return null;

        return {
          lb: vector[1], // Bow length ratio
          ls: vector[2], // Stern length ratio
          lm: 1 - vector[1] - vector[2], // Mid-body length ratio
          hasBulb: vector[31] > 0.5, // bit_BB
          beta: vector[8], // Flare angle (normalized)
          cdrft: vector[19], // Deadrise angle (normalized)
        };
      } catch {
        return null;
      }
    }, [candidate.shipdParametersJson]);

    return (
      <div
        data-testid={`candidate-card-${rank}`}
        data-hull-family={candidate.hullFamily}
        data-bow-family={candidate.bowFamily ?? ""}
        data-midship-family={candidate.midshipFamily ?? ""}
        data-stern-family={candidate.sternFamily ?? ""}
        className={`rounded-lg bg-card border border-border shadow-sm transition-all hover:shadow-md hover:border-primary/50 ${
          isComparing ? "ring-2 ring-primary" : ""
        }`}
      >
        {/* Header with Rank & Score */}
        <div className="bg-muted/30 px-4 py-3 border-b border-border">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-full bg-primary text-primary-foreground font-bold shadow-sm">
                #{rank}
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <h3 className="text-base font-semibold text-foreground capitalize">
                    {candidate.hullFamily.replace("_", " ")}
                  </h3>
                  {hasShipD && (
                    <span className="inline-flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-semibold bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300">
                      <Ship className="h-2.5 w-2.5" />
                      ShipD
                    </span>
                  )}
                </div>
                <div className="flex items-center gap-1.5 mt-0.5">
                  <Award className="h-3 w-3 text-muted-foreground" />
                  <span className="text-xs font-medium text-muted-foreground">
                    Score: {(candidate.score * 100).toFixed(1)}%
                  </span>
                </div>
              </div>
            </div>

            {/* Warning Badges */}
            <div className="flex items-center gap-2">
              {/* Geometry Generation Failure Badge */}
              {geometryGenerationFailed && (
                <div className="flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-destructive/10 text-destructive border border-destructive/20">
                  <AlertTriangle className="h-3 w-3" />
                  No Geometry
                </div>
              )}
              {/* Flags Badge */}
              {flags.length > 0 && (
                <div
                  className={`flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium ${
                    hasWarnings
                      ? "bg-destructive/10 text-destructive"
                      : "bg-accent/20 text-accent-foreground"
                  }`}
                >
                  {hasWarnings && <AlertTriangle className="h-3 w-3" />}
                  {flags.length} {flags.length === 1 ? "flag" : "flags"}
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Card Body */}
        <div className="p-4 space-y-4">
          {/* Provenance Panel (Data-Driven Real-World Mode) */}
          {candidate.solverMode === "DataDrivenReal" && candidate.referenceVesselName && (
            <div className="bg-gradient-to-r from-green-50 to-emerald-50 dark:from-green-900/20 dark:to-emerald-900/20 rounded-lg p-3 border border-green-200 dark:border-green-800">
              <div className="flex items-start gap-2">
                <Database className="h-4 w-4 text-green-600 dark:text-green-400 mt-0.5 flex-shrink-0" />
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <h4 className="text-xs font-semibold text-green-900 dark:text-green-300">
                      Data-Driven Design
                    </h4>
                    <Sparkles className="h-3 w-3 text-green-600 dark:text-green-400" />
                  </div>
                  <div className="space-y-1">
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-green-700 dark:text-green-500">Reference:</span>
                      <span className="font-mono font-semibold text-green-900 dark:text-green-200 truncate ml-2">
                        {candidate.referenceVesselName}
                      </span>
                    </div>
                    {candidate.similarityScore !== undefined && (
                      <div className="flex items-center justify-between text-xs">
                        <span className="text-green-700 dark:text-green-500">Similarity:</span>
                        <div className="flex items-center gap-2">
                          <div className="h-1.5 w-24 bg-green-200 dark:bg-green-900/50 rounded-full overflow-hidden">
                            <div
                              className="h-full bg-green-500 dark:bg-green-400 rounded-full"
                              style={{ width: `${(candidate.similarityScore * 100).toFixed(0)}%` }}
                            />
                          </div>
                          <span className="font-bold text-green-900 dark:text-green-200 min-w-[3rem]">
                            {(candidate.similarityScore * 100).toFixed(0)}%
                          </span>
                        </div>
                      </div>
                    )}
                  </div>
                  <p className="mt-1.5 text-xs text-green-700/80 dark:text-green-500/80">
                    Scaled from proven vessel, refined with physics
                  </p>
                </div>
              </div>
            </div>
          )}

          {/* Provenance Panel (ML/Parametric Mode) */}
          {candidate.solverMode === "DataDrivenML" && candidate.referenceVesselName && (
            <div className="bg-gradient-to-r from-purple-50 to-violet-50 dark:from-purple-900/20 dark:to-violet-900/20 rounded-lg p-3 border border-purple-200 dark:border-purple-800">
              <div className="flex items-start gap-2">
                <Sparkles className="h-4 w-4 text-purple-600 dark:text-purple-400 mt-0.5 flex-shrink-0" />
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    <h4 className="text-xs font-semibold text-purple-900 dark:text-purple-300">
                      ML-Generated Design
                    </h4>
                    <span className="px-1.5 py-0.5 text-[10px] font-bold bg-purple-500/20 text-purple-700 dark:text-purple-300 rounded">
                      BETA
                    </span>
                  </div>
                  <div className="space-y-1">
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-purple-700 dark:text-purple-500">Parametric Hull:</span>
                      <span className="font-mono font-semibold text-purple-900 dark:text-purple-200 truncate ml-2">
                        {candidate.referenceVesselName}
                      </span>
                    </div>
                    {candidate.similarityScore !== undefined && (
                      <div className="flex items-center justify-between text-xs">
                        <span className="text-purple-700 dark:text-purple-500">Similarity:</span>
                        <div className="flex items-center gap-2">
                          <div className="h-1.5 w-24 bg-purple-200 dark:bg-purple-900/50 rounded-full overflow-hidden">
                            <div
                              className="h-full bg-purple-500 dark:bg-purple-400 rounded-full"
                              style={{ width: `${(candidate.similarityScore * 100).toFixed(0)}%` }}
                            />
                          </div>
                          <span className="font-bold text-purple-900 dark:text-purple-200 min-w-[3rem]">
                            {(candidate.similarityScore * 100).toFixed(0)}%
                          </span>
                        </div>
                      </div>
                    )}
                    <div className="flex items-center justify-between text-xs">
                      <span className="text-purple-700 dark:text-purple-500">Source:</span>
                      <span className="font-mono text-purple-800 dark:text-purple-300 text-[10px]">
                        MIT ShipD Dataset
                      </span>
                    </div>
                  </div>
                  <p className="mt-1.5 text-xs text-purple-700/80 dark:text-purple-500/80">
                    Parametric hull converted & scaled, refined with physics
                  </p>
                </div>
              </div>
            </div>
          )}

          {/* 3D Thumbnail */}
          <div className="bg-muted/20 rounded-lg overflow-hidden">
            <Hull3DThumbnail candidate={candidate} height={200} />
          </div>

          {/* Principal Dimensions - Compact Grid */}
          <div className="bg-accent/5 rounded-lg p-3 border border-accent/20">
            <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wide mb-2">
              Principal Dimensions
            </h4>
            <div className="grid grid-cols-4 gap-3">
              <div className="text-center">
                <div className="text-xs text-muted-foreground mb-0.5">Lpp</div>
                <div className="text-sm font-bold text-foreground">
                  {candidate.lppM?.toFixed(1) || "—"}
                  <span className="text-xs text-muted-foreground ml-0.5">m</span>
                </div>
              </div>
              <div className="text-center">
                <div className="text-xs text-muted-foreground mb-0.5">B</div>
                <div className="text-sm font-bold text-foreground">
                  {candidate.beamM?.toFixed(1) || "—"}
                  <span className="text-xs text-muted-foreground ml-0.5">m</span>
                </div>
              </div>
              <div className="text-center">
                <div className="text-xs text-muted-foreground mb-0.5">T</div>
                <div className="text-sm font-bold text-foreground">
                  {candidate.draftM?.toFixed(1) || "—"}
                  <span className="text-xs text-muted-foreground ml-0.5">m</span>
                </div>
              </div>
              <div className="text-center">
                <div className="text-xs text-muted-foreground mb-0.5">CB</div>
                <div className="text-sm font-bold text-foreground">
                  {candidate.cb?.toFixed(3) || "—"}
                </div>
              </div>
            </div>
          </div>

          {/* ShipD Geometry Info (if available) */}
          {hasShipD && shipdGeometryInfo && (
            <div className="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-3 border border-blue-200 dark:border-blue-800">
              <h4 className="text-xs font-semibold text-blue-900 dark:text-blue-300 uppercase tracking-wide mb-2 flex items-center gap-1.5">
                <Ship className="h-3 w-3" />
                ShipD Geometry
              </h4>
              <div className="space-y-1.5 text-xs">
                <div className="flex justify-between">
                  <span className="text-blue-700 dark:text-blue-400">Longitudinal:</span>
                  <span className="font-mono font-semibold text-blue-900 dark:text-blue-200">
                    Lb={(shipdGeometryInfo.lb * 100).toFixed(0)}% Lm=
                    {(shipdGeometryInfo.lm * 100).toFixed(0)}% Ls=
                    {(shipdGeometryInfo.ls * 100).toFixed(0)}%
                  </span>
                </div>
                {shipdGeometryInfo.hasBulb && (
                  <div className="flex justify-between">
                    <span className="text-blue-700 dark:text-blue-400">Bulb:</span>
                    <span className="font-semibold text-blue-900 dark:text-blue-200">Present</span>
                  </div>
                )}
              </div>
            </div>
          )}

          {/* Key Performance Indicators */}
          <div className="space-y-2">
            <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wide flex items-center gap-1.5">
              <TrendingUp className="h-3 w-3" />
              Performance
            </h4>
            <div className="grid grid-cols-2 gap-2 text-xs">
              <div className="flex items-center justify-between px-2 py-1.5 bg-muted/30 rounded">
                <span className="text-muted-foreground">Displacement</span>
                <span className="font-semibold text-foreground">
                  {candidate.dispT?.toFixed(0) || "—"} t
                </span>
              </div>
              <div className="flex items-center justify-between px-2 py-1.5 bg-muted/30 rounded">
                <span className="text-muted-foreground">Fn</span>
                <span className="font-semibold text-foreground">
                  {candidate.fn?.toFixed(3) || "—"}
                </span>
              </div>
              {candidate.ehpKw && (
                <div className="flex items-center justify-between px-2 py-1.5 bg-muted/30 rounded">
                  <span className="text-muted-foreground">EHP</span>
                  <span className="font-semibold text-foreground">
                    {candidate.ehpKw.toFixed(0)} kW
                  </span>
                </div>
              )}
              {candidate.gmEstM && (
                <div className="flex items-center justify-between px-2 py-1.5 bg-muted/30 rounded">
                  <span className="text-muted-foreground">GMt (est)</span>
                  <span className="font-semibold text-foreground">
                    {candidate.gmEstM.toFixed(2)} m
                  </span>
                </div>
              )}
            </div>
          </div>

          {/* Actions */}
          <div className="border-t border-border pt-3 mt-1">
            <div className="flex gap-2">
              <Button onClick={onSelect} className="flex-1" size="sm">
                Open Workspace
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={onCompare}
                className={isComparing ? "ring-2 ring-primary" : ""}
              >
                {isComparing ? (
                  <>
                    <Check className="h-3 w-3 mr-1" />
                    Selected
                  </>
                ) : (
                  "Compare"
                )}
              </Button>
            </div>
          </div>
        </div>
      </div>
    );
  },
  (prevProps, nextProps) => {
    // Custom comparison to prevent unnecessary re-renders
    return (
      prevProps.candidate.id === nextProps.candidate.id &&
      prevProps.candidate.score === nextProps.candidate.score &&
      prevProps.candidate.geometryGenerationStatus ===
        nextProps.candidate.geometryGenerationStatus &&
      prevProps.rank === nextProps.rank &&
      prevProps.isComparing === nextProps.isComparing
    );
  }
);
