import React from "react";
import type { PanelId, PanelState } from "../../../types/workspace";

interface PanelWrapperProps {
  panelId: PanelId;
  title: string;
  panelState: PanelState;
  onToggleCollapse?: () => void;
  onToggleFullscreen?: () => void;
  children: React.ReactNode;
  actions?: React.ReactNode; // Optional action buttons
}

/**
 * Wrapper component for resistance panels with collapse/expand controls
 */
export function PanelWrapper({
  panelId: _panelId, // Not currently used, but may be needed for future features
  title,
  panelState,
  onToggleCollapse,
  onToggleFullscreen,
  children,
  actions,
}: PanelWrapperProps) {
  if (panelState.hidden) return null;

  return (
    <div className="bg-card border border-border rounded-lg overflow-hidden h-full flex flex-col">
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-2 border-b border-border bg-muted/30">
        <h3 className="text-sm font-semibold text-foreground">{title}</h3>
        <div className="flex items-center gap-1">
          {actions}
          {onToggleCollapse && (
            <button
              onClick={onToggleCollapse}
              className="p-1 hover:bg-accent/10 rounded"
              title={panelState.collapsed ? "Expand" : "Collapse"}
            >
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d={panelState.collapsed ? "M19 9l-7 7-7-7" : "M5 15l7-7 7 7"}
                />
              </svg>
            </button>
          )}
          {onToggleFullscreen && (
            <button
              onClick={onToggleFullscreen}
              className="p-1 hover:bg-accent/10 rounded"
              title="Fullscreen"
            >
              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4"
                />
              </svg>
            </button>
          )}
        </div>
      </div>

      {/* Content */}
      {!panelState.collapsed && <div className="p-4 overflow-auto flex-1">{children}</div>}
    </div>
  );
}
