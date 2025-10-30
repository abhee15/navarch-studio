// Workspace layout types for the widget dashboard

import type { Layout as GridLayout } from "react-grid-layout";

/**
 * Workspace mode - determines what the user sees and can do
 */
export type WorkspaceMode = "view" | "edit";

/**
 * Panel identifier - unique key for each panel type
 */
export type PanelId = "kpis" | "curves" | "hull" | "table" | "geometry" | "parameters" | "status";

/**
 * Panel state - tracks individual panel visibility and behavior
 */
export interface PanelState {
  id: PanelId;
  collapsed: boolean;
  hidden: boolean;
  fullscreen: boolean;
}

/**
 * Panel definition - metadata about each available panel
 */
export interface PanelDefinition {
  id: PanelId;
  title: string;
  description: string;
  icon: string;
  defaultSize: {
    w: number;
    h: number;
    minW?: number;
    minH?: number;
    maxW?: number;
    maxH?: number;
  };
  availableInMobile: boolean;
}

/**
 * Workspace layout configuration
 * Combines grid layout with panel states
 */
export interface WorkspaceLayout {
  mode: WorkspaceMode;
  gridLayouts: {
    lg: GridLayout[];
    md: GridLayout[];
    sm: GridLayout[];
  };
  panelStates: Record<PanelId, PanelState>;
  lastModified: string;
}

/**
 * Preset layout template
 */
export interface LayoutPreset {
  id: string;
  name: string;
  description: string;
  layout: WorkspaceLayout;
}

/**
 * Default grid layout positions for each panel
 */
export const DEFAULT_GRID_LAYOUTS: { lg: GridLayout[]; md: GridLayout[]; sm: GridLayout[] } = {
  lg: [
    { i: "kpis", x: 0, y: 0, w: 12, h: 2, minW: 6, minH: 2 },
    { i: "curves", x: 0, y: 2, w: 8, h: 6, minW: 4, minH: 4 },
    { i: "hull", x: 8, y: 2, w: 4, h: 6, minW: 3, minH: 4 },
    { i: "table", x: 0, y: 8, w: 12, h: 8, minW: 6, minH: 4 },
    { i: "geometry", x: 0, y: 16, w: 6, h: 4, minW: 4, minH: 3 },
    { i: "parameters", x: 6, y: 16, w: 3, h: 4, minW: 3, minH: 3 },
    { i: "status", x: 9, y: 16, w: 3, h: 4, minW: 3, minH: 3 },
  ],
  md: [
    { i: "kpis", x: 0, y: 0, w: 10, h: 2, minW: 5, minH: 2 },
    { i: "curves", x: 0, y: 2, w: 6, h: 6, minW: 4, minH: 4 },
    { i: "hull", x: 6, y: 2, w: 4, h: 6, minW: 3, minH: 4 },
    { i: "table", x: 0, y: 8, w: 10, h: 8, minW: 5, minH: 4 },
    { i: "geometry", x: 0, y: 16, w: 5, h: 4, minW: 4, minH: 3 },
    { i: "parameters", x: 5, y: 16, w: 3, h: 4, minW: 3, minH: 3 },
    { i: "status", x: 8, y: 16, w: 2, h: 4, minW: 2, minH: 3 },
  ],
  sm: [
    { i: "kpis", x: 0, y: 0, w: 6, h: 2, minW: 6, minH: 2 },
    { i: "curves", x: 0, y: 2, w: 6, h: 6, minW: 6, minH: 4 },
    { i: "hull", x: 0, y: 8, w: 6, h: 6, minW: 6, minH: 4 },
    { i: "table", x: 0, y: 14, w: 6, h: 8, minW: 6, minH: 4 },
    { i: "geometry", x: 0, y: 22, w: 6, h: 4, minW: 6, minH: 3 },
    { i: "parameters", x: 0, y: 26, w: 6, h: 4, minW: 6, minH: 3 },
    { i: "status", x: 0, y: 30, w: 6, h: 4, minW: 6, minH: 3 },
  ],
};

/**
 * Panel definitions with metadata
 */
export const PANEL_DEFINITIONS: Record<PanelId, PanelDefinition> = {
  kpis: {
    id: "kpis",
    title: "Key Performance Indicators",
    description: "Summary of critical hydrostatic values",
    icon: "dashboard",
    defaultSize: { w: 12, h: 2, minW: 6, minH: 2 },
    availableInMobile: true,
  },
  curves: {
    id: "curves",
    title: "Hydrostatic Curves",
    description: "Interactive visualization of hydrostatic properties",
    icon: "chart",
    defaultSize: { w: 8, h: 6, minW: 4, minH: 4 },
    availableInMobile: true,
  },
  hull: {
    id: "hull",
    title: "Hull Diagram",
    description: "2D visualization of vessel hull and waterline",
    icon: "boat",
    defaultSize: { w: 4, h: 6, minW: 3, minH: 4 },
    availableInMobile: true,
  },
  table: {
    id: "table",
    title: "Hydrostatics Table",
    description: "Detailed computed values for all draft points",
    icon: "table",
    defaultSize: { w: 12, h: 8, minW: 6, minH: 4 },
    availableInMobile: true,
  },
  geometry: {
    id: "geometry",
    title: "Geometry Editor",
    description: "Vessel offsets and station configuration",
    icon: "grid",
    defaultSize: { w: 6, h: 4, minW: 4, minH: 3 },
    availableInMobile: false,
  },
  parameters: {
    id: "parameters",
    title: "Parameters Summary",
    description: "Current computation settings and conditions",
    icon: "settings",
    defaultSize: { w: 3, h: 4, minW: 3, minH: 3 },
    availableInMobile: true,
  },
  status: {
    id: "status",
    title: "Computation Status",
    description: "Last computation details and validation",
    icon: "info",
    defaultSize: { w: 3, h: 4, minW: 3, minH: 3 },
    availableInMobile: true,
  },
};

/**
 * Default panel states - all visible and expanded initially
 */
export const DEFAULT_PANEL_STATES: Record<PanelId, PanelState> = {
  kpis: { id: "kpis", collapsed: false, hidden: false, fullscreen: false },
  curves: { id: "curves", collapsed: false, hidden: false, fullscreen: false },
  hull: { id: "hull", collapsed: false, hidden: false, fullscreen: false },
  table: { id: "table", collapsed: false, hidden: false, fullscreen: false },
  geometry: { id: "geometry", collapsed: false, hidden: true, fullscreen: false }, // Hidden by default
  parameters: { id: "parameters", collapsed: false, hidden: true, fullscreen: false }, // Hidden by default
  status: { id: "status", collapsed: false, hidden: true, fullscreen: false }, // Hidden by default
};

/**
 * Preset layouts
 */
export const LAYOUT_PRESETS: LayoutPreset[] = [
  {
    id: "default",
    name: "Default",
    description: "Balanced layout with all core panels",
    layout: {
      mode: "view",
      gridLayouts: DEFAULT_GRID_LAYOUTS,
      panelStates: DEFAULT_PANEL_STATES,
      lastModified: new Date().toISOString(),
    },
  },
  {
    id: "curves-focus",
    name: "Curves Focus",
    description: "Large curves panel with minimal other information",
    layout: {
      mode: "view",
      gridLayouts: {
        lg: [
          { i: "kpis", x: 0, y: 0, w: 12, h: 2, minW: 6, minH: 2 },
          { i: "curves", x: 0, y: 2, w: 12, h: 10, minW: 6, minH: 6 },
          { i: "hull", x: 0, y: 12, w: 6, h: 4, minW: 3, minH: 4 },
          { i: "table", x: 6, y: 12, w: 6, h: 4, minW: 4, minH: 4 },
          { i: "geometry", x: 0, y: 16, w: 4, h: 3, minW: 4, minH: 3 },
          { i: "parameters", x: 4, y: 16, w: 4, h: 3, minW: 3, minH: 3 },
          { i: "status", x: 8, y: 16, w: 4, h: 3, minW: 3, minH: 3 },
        ],
        md: DEFAULT_GRID_LAYOUTS.md,
        sm: DEFAULT_GRID_LAYOUTS.sm,
      },
      panelStates: DEFAULT_PANEL_STATES,
      lastModified: new Date().toISOString(),
    },
  },
  {
    id: "table-focus",
    name: "Table Focus",
    description: "Large data table with compact visualizations",
    layout: {
      mode: "view",
      gridLayouts: {
        lg: [
          { i: "kpis", x: 0, y: 0, w: 12, h: 2, minW: 6, minH: 2 },
          { i: "table", x: 0, y: 2, w: 12, h: 10, minW: 6, minH: 6 },
          { i: "curves", x: 0, y: 12, w: 6, h: 5, minW: 4, minH: 4 },
          { i: "hull", x: 6, y: 12, w: 6, h: 5, minW: 3, minH: 4 },
          { i: "geometry", x: 0, y: 17, w: 4, h: 3, minW: 4, minH: 3 },
          { i: "parameters", x: 4, y: 17, w: 4, h: 3, minW: 3, minH: 3 },
          { i: "status", x: 8, y: 17, w: 4, h: 3, minW: 3, minH: 3 },
        ],
        md: DEFAULT_GRID_LAYOUTS.md,
        sm: DEFAULT_GRID_LAYOUTS.sm,
      },
      panelStates: DEFAULT_PANEL_STATES,
      lastModified: new Date().toISOString(),
    },
  },
  {
    id: "compact",
    name: "Compact",
    description: "All panels visible in small sizes",
    layout: {
      mode: "view",
      gridLayouts: {
        lg: [
          { i: "kpis", x: 0, y: 0, w: 12, h: 2, minW: 6, minH: 2 },
          { i: "curves", x: 0, y: 2, w: 6, h: 4, minW: 4, minH: 4 },
          { i: "hull", x: 6, y: 2, w: 3, h: 4, minW: 3, minH: 4 },
          { i: "status", x: 9, y: 2, w: 3, h: 4, minW: 3, minH: 3 },
          { i: "table", x: 0, y: 6, w: 9, h: 6, minW: 6, minH: 4 },
          { i: "parameters", x: 9, y: 6, w: 3, h: 3, minW: 3, minH: 3 },
          { i: "geometry", x: 9, y: 9, w: 3, h: 3, minW: 3, minH: 3 },
        ],
        md: DEFAULT_GRID_LAYOUTS.md,
        sm: DEFAULT_GRID_LAYOUTS.sm,
      },
      panelStates: {
        ...DEFAULT_PANEL_STATES,
        geometry: { id: "geometry", collapsed: false, hidden: false, fullscreen: false },
        parameters: { id: "parameters", collapsed: false, hidden: false, fullscreen: false },
        status: { id: "status", collapsed: false, hidden: false, fullscreen: false },
      },
      lastModified: new Date().toISOString(),
    },
  },
];
