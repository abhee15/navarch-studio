import React from "react";
import { Settings, Bot } from "lucide-react";

interface WorkspaceUtilitiesBarProps {
  onSettingsClick: () => void;
  onCopilotClick: () => void;
  activePanel: "settings" | "copilot" | null;
}

/**
 * Vertical icon bar for workspace utilities
 * Always visible on xl+ screens, provides quick access to Settings and AI Copilot
 */
export const WorkspaceUtilitiesBar: React.FC<WorkspaceUtilitiesBarProps> = ({
  onSettingsClick,
  onCopilotClick,
  activePanel,
}) => {
  return (
    <aside className="hidden xl:flex xl:flex-col w-12 border-l border-border bg-card/30 backdrop-blur-sm">
      {/* Settings Icon Button */}
      <button
        onClick={onSettingsClick}
        className={`
          flex flex-col items-center justify-center gap-1 p-3
          border-b border-border transition-colors
          hover:bg-accent/50 active:bg-accent
          ${activePanel === "settings" ? "bg-accent text-accent-foreground" : "text-muted-foreground"}
        `}
        title="Visualization Settings"
        aria-label="Open visualization settings"
      >
        <Settings className="h-5 w-5" />
        <span
          className="text-[10px] font-medium"
          style={{ writingMode: "vertical-rl", textOrientation: "mixed" }}
        >
          Settings
        </span>
      </button>

      {/* AI Copilot Icon Button */}
      <button
        onClick={onCopilotClick}
        className={`
          flex flex-col items-center justify-center gap-1 p-3
          border-b border-border transition-colors
          hover:bg-accent/50 active:bg-accent
          ${activePanel === "copilot" ? "bg-accent text-accent-foreground" : "text-muted-foreground"}
        `}
        title="AI Copilot (⌘K)"
        aria-label="Open AI Copilot"
      >
        <Bot className="h-5 w-5" />
        <span
          className="text-[10px] font-medium"
          style={{ writingMode: "vertical-rl", textOrientation: "mixed" }}
        >
          Copilot
        </span>
      </button>
    </aside>
  );
};
