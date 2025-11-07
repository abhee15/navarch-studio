import React from "react";
import { observer } from "mobx-react-lite";
import { Sparkles, MessageSquare, Lightbulb } from "lucide-react";
import { useStore } from "../../../stores";

export const MinimizedPanel: React.FC = observer(() => {
  const { copilotStore } = useStore();

  return (
    <div className="fixed right-0 top-0 h-screen w-12 bg-card border-l border-border shadow-xl z-40 flex flex-col items-center py-4 gap-4">
      {/* Expand Button */}
      <button
        onClick={() => copilotStore.setPosition("right")}
        className="p-2 text-primary hover:bg-accent rounded transition-colors"
        title="Expand Copilot"
      >
        <Sparkles className="w-5 h-5" />
      </button>

      {/* Vertical Text */}
      <div className="flex-1 flex items-center justify-center">
        <span className="text-muted-foreground text-xs font-medium transform -rotate-90 whitespace-nowrap">
          AI COPILOT
        </span>
      </div>

      {/* Quick Actions */}
      <button
        className="p-2 text-muted-foreground hover:text-foreground hover:bg-accent rounded transition-colors"
        title="New chat"
        onClick={() => copilotStore.setPosition("right")}
      >
        <MessageSquare className="w-4 h-4" />
      </button>

      <button
        className="p-2 text-muted-foreground hover:text-foreground hover:bg-accent rounded transition-colors"
        title="View suggestions"
        onClick={() => copilotStore.setPosition("right")}
      >
        <Lightbulb className="w-4 h-4" />
      </button>
    </div>
  );
});
