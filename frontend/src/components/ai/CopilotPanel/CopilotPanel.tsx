import React from "react";
import { observer } from "mobx-react-lite";
import { useStore } from "../../../stores";
import { DockedPanel } from "./DockedPanel";
import { MinimizedPanel } from "./MinimizedPanel";
import { Sparkles } from "lucide-react";

export const CopilotPanel: React.FC = observer(() => {
  const { copilotStore } = useStore();
  const { panelPosition } = copilotStore;

  // Panel is hidden - show floating button
  if (panelPosition === "hidden") {
    return (
      <button
        onClick={() => copilotStore.setPosition("right")}
        className="fixed bottom-6 right-6 p-3 bg-primary text-primary-foreground rounded-full shadow-lg hover:bg-primary/90 transition-all hover:scale-105 z-50"
        title="Open AI Copilot (⌘K)"
      >
        <Sparkles className="w-5 h-5" />
      </button>
    );
  }

  // Panel is minimized
  if (panelPosition === "minimized") {
    return <MinimizedPanel />;
  }

  // Panel is docked (left or right) - primary mode
  return <DockedPanel />;
});
