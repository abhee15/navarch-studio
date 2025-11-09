import { useState } from "react";
import { ChevronUp, ChevronDown } from "lucide-react";
import { ParameterSliders } from "./ParameterSliders";
import type { CandidateDesign } from "../../../types/sizing";

interface ParametersDrawerProps {
  candidate: CandidateDesign;
  onUpdate: (updates: Partial<CandidateDesign>) => void;
  isUpdating?: boolean;
}

/**
 * Mobile Bottom Drawer for Interactive Parameters
 *
 * Provides a swipeable drawer interface for mobile devices that slides up
 * from the bottom to reveal parameter controls.
 */
export const ParametersDrawer: React.FC<ParametersDrawerProps> = ({
  candidate,
  onUpdate,
  isUpdating,
}) => {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <>
      {/* Backdrop */}
      {isOpen && (
        <div
          className="fixed inset-0 bg-black/50 z-40 md:hidden"
          onClick={() => setIsOpen(false)}
        />
      )}

      {/* Drawer */}
      <div
        className={`
        fixed bottom-0 left-0 right-0 z-50 md:hidden
        bg-card border-t-2 border-border rounded-t-2xl shadow-2xl
        transform transition-transform duration-300 ease-out
        ${isOpen ? "translate-y-0" : "translate-y-[calc(100%-72px)]"}
      `}
      >
        {/* Drag Handle */}
        <button
          onClick={() => setIsOpen(!isOpen)}
          className="w-full py-4 flex flex-col items-center touch-manipulation active:bg-accent/10"
        >
          <div className="w-12 h-1 bg-border rounded-full mb-2" />
          <div className="flex items-center gap-2 text-sm font-medium text-foreground">
            {isOpen ? <ChevronDown className="h-4 w-4" /> : <ChevronUp className="h-4 w-4" />}
            <span>Interactive Parameters</span>
          </div>
        </button>

        {/* Content - Scrollable */}
        <div className="max-h-[65vh] overflow-y-auto overscroll-contain p-4 pb-8">
          <ParameterSliders candidate={candidate} onUpdate={onUpdate} isUpdating={isUpdating} />
        </div>
      </div>
    </>
  );
};
