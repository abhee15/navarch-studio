import type { HydroResult, VesselDetails } from "../../../../types/hydrostatics";
import { HullDiagram } from "../../HullDiagram";

interface HullDiagramPanelProps {
  vessel: VesselDetails | null;
  currentResult: HydroResult | null;
  results: HydroResult[];
  highlightedDraft: number | null;
  kg?: number;
  lcg?: number;
  onDraftHover?: (draft: number | null) => void;
}

export function HullDiagramPanel({
  vessel,
  currentResult,
  results,
  highlightedDraft,
  kg = 0,
  lcg = 0,
  onDraftHover,
}: HullDiagramPanelProps) {
  if (!vessel) {
    return (
      <div className="flex items-center justify-center h-full">
        <p className="text-sm text-muted-foreground">No vessel data</p>
      </div>
    );
  }

  return (
    <HullDiagram
      lpp={vessel.lpp}
      beam={vessel.beam}
      draft={currentResult?.draft || vessel.designDraft}
      designDraft={vessel.designDraft}
      kb={currentResult?.kBz}
      lcb={currentResult?.lCBx}
      kg={kg}
      lcg={lcg}
      results={results}
      highlightedDraft={highlightedDraft}
      onDraftHover={onDraftHover}
    />
  );
}
