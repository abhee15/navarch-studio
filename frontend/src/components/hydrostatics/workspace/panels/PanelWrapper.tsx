import type { ReactNode } from "react";
import type { PanelId } from "../../../../types/workspace";

interface PanelWrapperProps {
  panelId: PanelId;
  title: string;
  children: ReactNode;
  collapsed: boolean;
  fullscreen: boolean;
  onToggleCollapse: () => void;
  onToggleFullscreen: (fullscreen: boolean) => void;
  isDragging?: boolean;
  showDragHandle?: boolean;
}

export function PanelWrapper({
  panelId,
  title,
  children,
  collapsed,
  fullscreen,
  onToggleCollapse,
  onToggleFullscreen,
  isDragging = false,
  showDragHandle = true,
}: PanelWrapperProps) {
  // Fullscreen modal
  if (fullscreen) {
    return (
      <div className="fixed inset-0 z-50 bg-background/95 backdrop-blur-sm flex flex-col">
        {/* Fullscreen Header */}
        <div className="flex-shrink-0 border-b border-border bg-card/80 backdrop-blur-sm">
          <div className="px-4 py-3 flex items-center justify-between">
            <h2 className="text-lg font-semibold text-foreground">{title}</h2>
            <button
              onClick={() => onToggleFullscreen(false)}
              className="inline-flex items-center px-3 py-1.5 text-sm font-medium text-foreground hover:text-foreground/80 border border-border rounded hover:bg-accent/10 transition-colors"
              title="Exit Fullscreen (Esc)"
            >
              <svg className="w-4 h-4 mr-1.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M6 18L18 6M6 6l12 12"
                />
              </svg>
              Close
            </button>
          </div>
        </div>

        {/* Fullscreen Content */}
        <div className="flex-1 overflow-auto p-4">{children}</div>
      </div>
    );
  }

  // Normal panel
  return (
    <div
      className={`
        h-full flex flex-col bg-card border border-border rounded-lg shadow-sm
        ${isDragging ? "opacity-50" : "opacity-100"}
        transition-opacity
      `}
    >
      {/* Panel Header */}
      <div className="flex-shrink-0 border-b border-border bg-card/80 backdrop-blur-sm">
        <div className="px-3 py-2 flex items-center justify-between">
          <div className="flex items-center space-x-2 flex-1 min-w-0">
            {/* Drag Handle */}
            {showDragHandle && (
              <div
                className="cursor-move text-muted-foreground hover:text-foreground transition-colors flex-shrink-0"
                title="Drag to reposition"
              >
                <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M4 6h16M4 12h16M4 18h16"
                  />
                </svg>
              </div>
            )}

            {/* Title */}
            <h3 className="text-sm font-medium text-card-foreground truncate">{title}</h3>
          </div>

          {/* Actions */}
          <div className="flex items-center space-x-1 flex-shrink-0">
            {/* Fullscreen Button */}
            {panelId === "geometry" && (
              <button
                onClick={() => onToggleFullscreen(true)}
                className="p-1 text-muted-foreground hover:text-foreground hover:bg-accent rounded transition-colors"
                title="Expand to Fullscreen"
              >
                <svg className="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4"
                  />
                </svg>
              </button>
            )}

            {/* Collapse/Expand Button */}
            <button
              onClick={onToggleCollapse}
              className="p-1 text-muted-foreground hover:text-foreground hover:bg-accent rounded transition-colors"
              title={collapsed ? "Expand" : "Collapse"}
            >
              <svg
                className={`w-4 h-4 transform transition-transform ${collapsed ? "rotate-180" : ""}`}
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M19 9l-7 7-7-7"
                />
              </svg>
            </button>
          </div>
        </div>
      </div>

      {/* Panel Content */}
      {!collapsed && <div className="flex-1 overflow-auto p-3">{children}</div>}

      {/* Collapsed State */}
      {collapsed && (
        <div className="flex-1 flex items-center justify-center p-4">
          <p className="text-xs text-muted-foreground">Click to expand</p>
        </div>
      )}
    </div>
  );
}
