import React, { useEffect } from "react";
import { X } from "lucide-react";
import { VisualizationSettings } from "./VisualizationSettings";
import type { VisualizationOptions } from "./VisualizationSettings";

interface VisualizationSettingsSlideOverProps {
  isOpen: boolean;
  onClose: () => void;
  onSettingsChange: (newSettings: VisualizationOptions) => void;
}

/**
 * Slide-over panel for visualization settings
 * Covers workspace from right side when opened
 */
export const VisualizationSettingsSlideOver: React.FC<VisualizationSettingsSlideOverProps> = ({
  isOpen,
  onClose,
  onSettingsChange,
}) => {
  // Handle Escape key to close
  useEffect(() => {
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === "Escape" && isOpen) {
        onClose();
      }
    };

    document.addEventListener("keydown", handleEscape);
    return () => document.removeEventListener("keydown", handleEscape);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return (
    <>
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-black/20 backdrop-blur-sm z-40 transition-opacity"
        onClick={onClose}
        aria-hidden="true"
      />

      {/* Slide-over Panel */}
      <div className="fixed inset-y-0 right-0 w-80 lg:w-96 bg-card border-l border-border shadow-2xl z-50 flex flex-col overflow-hidden animate-in slide-in-from-right duration-300">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-border bg-card/50 backdrop-blur-sm">
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 rounded-lg bg-purple-500/10 flex items-center justify-center">
              <span className="text-purple-500 text-lg">⚙️</span>
            </div>
            <h2 className="text-lg font-semibold text-foreground">Visualization Settings</h2>
          </div>
          <button
            onClick={onClose}
            className="p-2 rounded-md hover:bg-accent transition-colors"
            aria-label="Close settings panel"
          >
            <X className="h-5 w-5 text-muted-foreground" />
          </button>
        </div>

        {/* Settings Content */}
        <div className="flex-1 overflow-y-auto p-4">
          <VisualizationSettings onSettingsChange={onSettingsChange} />
        </div>

        {/* Footer with keyboard shortcut hint */}
        <div className="p-3 border-t border-border bg-muted/30 text-center">
          <p className="text-xs text-muted-foreground">
            Press{" "}
            <kbd className="px-1.5 py-0.5 text-xs font-semibold text-foreground bg-background border border-border rounded">
              Esc
            </kbd>{" "}
            to close
          </p>
        </div>
      </div>
    </>
  );
};
