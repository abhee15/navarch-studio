import { useState, useEffect, useCallback } from "react";
import type { Layout as GridLayout } from "react-grid-layout";
import type {
  WorkspaceLayout,
  WorkspaceMode,
  PanelId,
  PanelState,
  LayoutPreset,
} from "../types/workspace";
import {
  DEFAULT_GRID_LAYOUTS,
  DEFAULT_PANEL_STATES,
  LAYOUT_PRESETS,
  RESISTANCE_DEFAULT_GRID_LAYOUTS,
  RESISTANCE_DEFAULT_PANEL_STATES,
  RESISTANCE_LAYOUT_PRESETS,
} from "../types/workspace";

const STORAGE_KEY_PREFIX = "navarch-workspace-layout";

/**
 * Hook for managing workspace layout state and persistence
 * Handles localStorage operations and layout state management
 * @param vesselId - The ID of the vessel
 * @param workspaceType - The type of workspace ("hydrostatics" or "resistance")
 */
export function useWorkspaceLayout(
  vesselId: string,
  workspaceType: "hydrostatics" | "resistance" = "hydrostatics"
) {
  const storageKey = `${STORAGE_KEY_PREFIX}-${workspaceType}-${vesselId}`;

  // Load initial layout from localStorage or use default
  const loadLayout = useCallback((): WorkspaceLayout => {
    try {
      const stored = localStorage.getItem(storageKey);
      if (stored) {
        const parsed = JSON.parse(stored) as WorkspaceLayout;
        // Validate that all required panels exist
        const panelIds: PanelId[] = [
          "kpis",
          "curves",
          "hull",
          "table",
          "geometry",
          "parameters",
          "status",
        ];
        const hasAllPanels = panelIds.every((id) => id in parsed.panelStates);
        if (hasAllPanels) {
          return parsed;
        }
      }
    } catch (error) {
      console.warn("Failed to load workspace layout from localStorage:", error);
    }

    // Return default layout based on workspace type
    if (workspaceType === "resistance") {
      return {
        mode: "edit",
        gridLayouts: RESISTANCE_DEFAULT_GRID_LAYOUTS,
        panelStates: RESISTANCE_DEFAULT_PANEL_STATES as WorkspaceLayout["panelStates"],
        lastModified: new Date().toISOString(),
      };
    } else {
      return {
        mode: "edit",
        gridLayouts: DEFAULT_GRID_LAYOUTS,
        panelStates: DEFAULT_PANEL_STATES as WorkspaceLayout["panelStates"],
        lastModified: new Date().toISOString(),
      };
    }
  }, [storageKey, workspaceType]);

  const [layout, setLayout] = useState<WorkspaceLayout>(loadLayout);

  // Save layout to localStorage whenever it changes
  useEffect(() => {
    try {
      localStorage.setItem(storageKey, JSON.stringify(layout));
    } catch (error) {
      console.error("Failed to save workspace layout to localStorage:", error);
    }
  }, [layout, storageKey]);

  /**
   * Update workspace mode
   */
  const setMode = useCallback((mode: WorkspaceMode) => {
    setLayout((prev) => ({
      ...prev,
      mode,
      lastModified: new Date().toISOString(),
    }));
  }, []);

  /**
   * Update grid layout for a specific breakpoint
   */
  const updateGridLayout = useCallback(
    (breakpoint: "lg" | "md" | "sm", newLayout: GridLayout[]) => {
      setLayout((prev) => ({
        ...prev,
        gridLayouts: {
          ...prev.gridLayouts,
          [breakpoint]: newLayout,
        },
        lastModified: new Date().toISOString(),
      }));
    },
    []
  );

  /**
   * Update a single panel's state
   */
  const updatePanelState = useCallback((panelId: PanelId, updates: Partial<PanelState>) => {
    setLayout((prev) => ({
      ...prev,
      panelStates: {
        ...prev.panelStates,
        [panelId]: {
          ...prev.panelStates[panelId],
          ...updates,
        },
      },
      lastModified: new Date().toISOString(),
    }));
  }, []);

  /**
   * Toggle panel collapsed state
   */
  const togglePanelCollapsed = useCallback((panelId: PanelId) => {
    setLayout((prev) => {
      const currentState = prev.panelStates[panelId];
      if (!currentState) return prev; // Panel doesn't exist in current layout

      return {
        ...prev,
        panelStates: {
          ...prev.panelStates,
          [panelId]: {
            ...currentState,
            collapsed: !currentState.collapsed,
          },
        },
        lastModified: new Date().toISOString(),
      };
    });
  }, []);

  /**
   * Toggle panel hidden state
   */
  const togglePanelHidden = useCallback((panelId: PanelId) => {
    setLayout((prev) => {
      const currentState = prev.panelStates[panelId];
      if (!currentState) return prev; // Panel doesn't exist in current layout

      return {
        ...prev,
        panelStates: {
          ...prev.panelStates,
          [panelId]: {
            ...currentState,
            hidden: !currentState.hidden,
          },
        },
        lastModified: new Date().toISOString(),
      };
    });
  }, []);

  /**
   * Set panel fullscreen state
   */
  const setPanelFullscreen = useCallback((panelId: PanelId, fullscreen: boolean) => {
    setLayout((prev) => ({
      ...prev,
      panelStates: {
        ...prev.panelStates,
        [panelId]: {
          ...prev.panelStates[panelId],
          fullscreen,
        },
      },
      lastModified: new Date().toISOString(),
    }));
  }, []);

  /**
   * Reset layout to default
   */
  const resetLayout = useCallback(() => {
    const defaultLayout: WorkspaceLayout =
      workspaceType === "resistance"
        ? {
            mode: layout.mode, // Keep current mode
            gridLayouts: RESISTANCE_DEFAULT_GRID_LAYOUTS,
            panelStates: RESISTANCE_DEFAULT_PANEL_STATES as WorkspaceLayout["panelStates"],
            lastModified: new Date().toISOString(),
          }
        : {
            mode: layout.mode, // Keep current mode
            gridLayouts: DEFAULT_GRID_LAYOUTS,
            panelStates: DEFAULT_PANEL_STATES as WorkspaceLayout["panelStates"],
            lastModified: new Date().toISOString(),
          };
    setLayout(defaultLayout);
  }, [layout.mode, workspaceType]);

  /**
   * Load a preset layout
   */
  const loadPreset = useCallback(
    (presetId: string) => {
      const presets = workspaceType === "resistance" ? RESISTANCE_LAYOUT_PRESETS : LAYOUT_PRESETS;
      const preset = presets.find((p) => p.id === presetId);
      if (preset) {
        setLayout({
          ...preset.layout,
          mode: layout.mode, // Keep current mode
          lastModified: new Date().toISOString(),
        });
      }
    },
    [layout.mode, workspaceType]
  );

  /**
   * Get available presets
   */
  const getPresets = useCallback((): LayoutPreset[] => {
    return workspaceType === "resistance" ? RESISTANCE_LAYOUT_PRESETS : LAYOUT_PRESETS;
  }, [workspaceType]);

  /**
   * Export layout as JSON string
   */
  const exportLayout = useCallback((): string => {
    return JSON.stringify(layout, null, 2);
  }, [layout]);

  /**
   * Import layout from JSON string
   */
  const importLayout = useCallback((jsonString: string): boolean => {
    try {
      const imported = JSON.parse(jsonString) as WorkspaceLayout;
      // Basic validation
      if (imported.gridLayouts && imported.panelStates) {
        setLayout({
          ...imported,
          lastModified: new Date().toISOString(),
        });
        return true;
      }
      return false;
    } catch (error) {
      console.error("Failed to import layout:", error);
      return false;
    }
  }, []);

  /**
   * Get visible panels (not hidden)
   */
  const getVisiblePanels = useCallback((): PanelId[] => {
    return Object.entries(layout.panelStates)
      .filter(([, state]) => !state.hidden)
      .map(([id]) => id as PanelId);
  }, [layout.panelStates]);

  /**
   * Check if any panel is in fullscreen
   */
  const hasFullscreenPanel = useCallback((): boolean => {
    return Object.values(layout.panelStates).some((state) => state.fullscreen);
  }, [layout.panelStates]);

  /**
   * Get the panel that is currently in fullscreen (if any)
   */
  const getFullscreenPanel = useCallback((): PanelId | null => {
    const entry = Object.entries(layout.panelStates).find(([, state]) => state.fullscreen);
    return entry ? (entry[0] as PanelId) : null;
  }, [layout.panelStates]);

  return {
    layout,
    setMode,
    updateGridLayout,
    updatePanelState,
    togglePanelCollapsed,
    togglePanelHidden,
    setPanelFullscreen,
    resetLayout,
    loadPreset,
    getPresets,
    exportLayout,
    importLayout,
    getVisiblePanels,
    hasFullscreenPanel,
    getFullscreenPanel,
  };
}
