import React from "react";
import type { CandidateDesign } from "../../types/sizing";
import { Button } from "../ui/button";
import { Hull3DThumbnail } from "./visualization/Hull3DThumbnail";
import { AlertTriangle, Award, TrendingUp } from "lucide-react";

interface CandidateCardProps {
  candidate: CandidateDesign;
  rank: number;
  onSelect: () => void;
  onCompare: () => void;
  isComparing: boolean;
}

export const CandidateCard: React.FC<CandidateCardProps> = ({
  candidate,
  rank,
  onSelect,
  onCompare,
  isComparing,
}) => {
  // Parse flags
  let flags: string[] = [];
  try {
    flags = JSON.parse(candidate.flagsJson);
  } catch {
    // Ignore parse errors
  }

  const hasWarnings = flags.some((f) => f.includes("constrained") || f.includes("exceeded"));

  return (
    <div
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
              <h3 className="text-base font-semibold text-foreground capitalize">
                {candidate.hullFamily.replace("_", " ")}
              </h3>
              <div className="flex items-center gap-1.5 mt-0.5">
                <Award className="h-3 w-3 text-muted-foreground" />
                <span className="text-xs font-medium text-muted-foreground">
                  Score: {(candidate.score * 100).toFixed(1)}%
                </span>
              </div>
            </div>
          </div>

          {/* Warning Badge */}
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

      {/* Card Body */}
      <div className="p-4 space-y-4">
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
        <div className="flex gap-2 pt-2">
          <Button onClick={onSelect} className="flex-1" size="sm">
            Open Workspace
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={onCompare}
            className={isComparing ? "ring-2 ring-primary" : ""}
          >
            {isComparing ? "✓" : "Compare"}
          </Button>
        </div>
      </div>
    </div>
  );
};
