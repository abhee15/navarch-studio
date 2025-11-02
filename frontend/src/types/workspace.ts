// Workspace layout types for the widget dashboard

import type { Layout as GridLayout } from "react-grid-layout";

/**
 * Workspace mode - determines what the user sees and can do
 */
export type WorkspaceMode = "view" | "edit";

/**
 * Panel identifier - unique key for each panel type
 */
export type PanelId =
  // Hydrostatics panels
  | "kpis"
  | "curves"
  | "hull"
  | "bodyplan"
  | "table"
  | "geometry"
  | "parameters"
  | "status"
  | "vessel3d"
  // Resistance panels
  | "resistance-calc-type"
  | "resistance-speed-grid"
  | "resistance-parameters"
  | "resistance-power-params"
  | "resistance-kcs-benchmark"
  | "resistance-ittc-results"
  | "resistance-hm-results"
  | "resistance-power-results"
  | "resistance-charts"
  | "resistance-unified-summary";

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
  panelStates: Partial<Record<PanelId, PanelState>>;
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
    { i: "vessel3d", x: 0, y: 8, w: 6, h: 8, minW: 4, minH: 6 },
    { i: "bodyplan", x: 6, y: 8, w: 6, h: 8, minW: 4, minH: 6 },
    { i: "table", x: 0, y: 16, w: 12, h: 8, minW: 6, minH: 4 },
    { i: "geometry", x: 0, y: 24, w: 6, h: 4, minW: 4, minH: 3 },
    { i: "parameters", x: 6, y: 24, w: 3, h: 4, minW: 3, minH: 3 },
    { i: "status", x: 9, y: 24, w: 3, h: 4, minW: 3, minH: 3 },
  ],
  md: [
    { i: "kpis", x: 0, y: 0, w: 10, h: 2, minW: 5, minH: 2 },
    { i: "curves", x: 0, y: 2, w: 6, h: 6, minW: 4, minH: 4 },
    { i: "hull", x: 6, y: 2, w: 4, h: 6, minW: 3, minH: 4 },
    { i: "vessel3d", x: 0, y: 8, w: 5, h: 8, minW: 4, minH: 6 },
    { i: "bodyplan", x: 5, y: 8, w: 5, h: 8, minW: 4, minH: 6 },
    { i: "table", x: 0, y: 16, w: 10, h: 8, minW: 5, minH: 4 },
    { i: "geometry", x: 0, y: 24, w: 5, h: 4, minW: 4, minH: 3 },
    { i: "parameters", x: 5, y: 24, w: 3, h: 4, minW: 3, minH: 3 },
    { i: "status", x: 8, y: 24, w: 2, h: 4, minW: 2, minH: 3 },
  ],
  sm: [
    { i: "kpis", x: 0, y: 0, w: 6, h: 2, minW: 6, minH: 2 },
    { i: "curves", x: 0, y: 2, w: 6, h: 6, minW: 6, minH: 4 },
    { i: "hull", x: 0, y: 8, w: 6, h: 6, minW: 6, minH: 4 },
    { i: "bodyplan", x: 0, y: 14, w: 6, h: 8, minW: 6, minH: 6 },
    { i: "table", x: 0, y: 22, w: 6, h: 8, minW: 6, minH: 4 },
    { i: "geometry", x: 0, y: 30, w: 6, h: 4, minW: 6, minH: 3 },
    { i: "parameters", x: 0, y: 34, w: 6, h: 4, minW: 6, minH: 3 },
    { i: "status", x: 0, y: 38, w: 6, h: 4, minW: 6, minH: 3 },
  ],
};

/**
 * Panel definitions with metadata (Hydrostatics panels)
 */
export const PANEL_DEFINITIONS: Partial<Record<PanelId, PanelDefinition>> = {
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
  bodyplan: {
    id: "bodyplan",
    title: "Body Plan",
    description: "2D sectional view of hull stations",
    icon: "layers",
    defaultSize: { w: 6, h: 8, minW: 4, minH: 6 },
    availableInMobile: false,
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
  vessel3d: {
    id: "vessel3d",
    title: "3D Vessel Visualization",
    description: "Interactive 3D hull shape with parameter controls",
    icon: "cube",
    defaultSize: { w: 6, h: 8, minW: 4, minH: 6 },
    availableInMobile: false,
  },
};

/**
 * Default panel states - all visible and expanded initially (Hydrostatics panels)
 */
export const DEFAULT_PANEL_STATES: Partial<Record<PanelId, PanelState>> = {
  kpis: { id: "kpis", collapsed: false, hidden: false, fullscreen: false },
  curves: { id: "curves", collapsed: false, hidden: false, fullscreen: false },
  hull: { id: "hull", collapsed: false, hidden: false, fullscreen: false },
  bodyplan: { id: "bodyplan", collapsed: false, hidden: false, fullscreen: false },
  table: { id: "table", collapsed: false, hidden: false, fullscreen: false },
  geometry: { id: "geometry", collapsed: false, hidden: true, fullscreen: false }, // Hidden by default
  parameters: { id: "parameters", collapsed: false, hidden: true, fullscreen: false }, // Hidden by default
  status: { id: "status", collapsed: false, hidden: true, fullscreen: false }, // Hidden by default
  vessel3d: { id: "vessel3d", collapsed: false, hidden: false, fullscreen: false },
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

// ============================================================================
// RESISTANCE WORKSPACE CONFIGURATION
// ============================================================================

/**
 * Resistance panel definitions with metadata
 */
export const RESISTANCE_PANEL_DEFINITIONS: Partial<Record<PanelId, PanelDefinition>> = {
  "resistance-calc-type": {
    id: "resistance-calc-type",
    title: "Calculation Type",
    description: "Select ITTC-57 or Holtrop-Mennen method",
    icon: "calculator",
    defaultSize: { w: 4, h: 2, minW: 3, minH: 2 },
    availableInMobile: true,
  },
  "resistance-speed-grid": {
    id: "resistance-speed-grid",
    title: "Speed Grid",
    description: "Select speed points for computation",
    icon: "speed",
    defaultSize: { w: 4, h: 3, minW: 3, minH: 2 },
    availableInMobile: true,
  },
  "resistance-parameters": {
    id: "resistance-parameters",
    title: "Resistance Parameters",
    description: "Form factor, temperature, salinity",
    icon: "settings",
    defaultSize: { w: 4, h: 4, minW: 3, minH: 3 },
    availableInMobile: true,
  },
  "resistance-power-params": {
    id: "resistance-power-params",
    title: "Power Parameters",
    description: "Efficiency and service margin settings",
    icon: "zap",
    defaultSize: { w: 4, h: 5, minW: 3, minH: 4 },
    availableInMobile: true,
  },
  "resistance-kcs-benchmark": {
    id: "resistance-kcs-benchmark",
    title: "KCS Benchmark",
    description: "Compare with KCS hull validation data",
    icon: "target",
    defaultSize: { w: 4, h: 4, minW: 3, minH: 3 },
    availableInMobile: false,
  },
  "resistance-ittc-results": {
    id: "resistance-ittc-results",
    title: "ITTC-57 Results",
    description: "Friction coefficient results",
    icon: "table",
    defaultSize: { w: 8, h: 6, minW: 6, minH: 4 },
    availableInMobile: true,
  },
  "resistance-hm-results": {
    id: "resistance-hm-results",
    title: "Holtrop-Mennen Results",
    description: "Total resistance breakdown",
    icon: "table",
    defaultSize: { w: 8, h: 6, minW: 6, minH: 4 },
    availableInMobile: true,
  },
  "resistance-power-results": {
    id: "resistance-power-results",
    title: "Power Curves",
    description: "EHP, DHP, and installed power",
    icon: "table",
    defaultSize: { w: 8, h: 6, minW: 6, minH: 4 },
    availableInMobile: true,
  },
  "resistance-charts": {
    id: "resistance-charts",
    title: "Charts",
    description: "Resistance and power visualization",
    icon: "chart",
    defaultSize: { w: 12, h: 8, minW: 8, minH: 6 },
    availableInMobile: true,
  },
  "resistance-unified-summary": {
    id: "resistance-unified-summary",
    title: "Unified Summary",
    description: "Comprehensive design summary with key metrics",
    icon: "dashboard",
    defaultSize: { w: 12, h: 8, minW: 8, minH: 6 },
    availableInMobile: true,
  },
};

/**
 * Default grid layout positions for resistance panels
 */
export const RESISTANCE_DEFAULT_GRID_LAYOUTS: {
  lg: GridLayout[];
  md: GridLayout[];
  sm: GridLayout[];
} = {
  lg: [
    // Left column: Input panels (33% width = 4 columns)
    { i: "resistance-calc-type", x: 0, y: 0, w: 4, h: 2, minW: 3, minH: 2 },
    { i: "resistance-speed-grid", x: 0, y: 2, w: 4, h: 3, minW: 3, minH: 2 },
    { i: "resistance-parameters", x: 0, y: 5, w: 4, h: 4, minW: 3, minH: 3 },
    { i: "resistance-power-params", x: 0, y: 9, w: 4, h: 5, minW: 3, minH: 4 },
    { i: "resistance-kcs-benchmark", x: 0, y: 14, w: 4, h: 4, minW: 3, minH: 3 },
    // Right column: Results panels (67% width = 8 columns)
    { i: "resistance-ittc-results", x: 4, y: 0, w: 8, h: 6, minW: 6, minH: 4 },
    { i: "resistance-hm-results", x: 4, y: 6, w: 8, h: 6, minW: 6, minH: 4 },
    { i: "resistance-power-results", x: 4, y: 12, w: 8, h: 6, minW: 6, minH: 4 },
    { i: "resistance-charts", x: 4, y: 18, w: 8, h: 8, minW: 8, minH: 6 },
    { i: "resistance-unified-summary", x: 0, y: 26, w: 12, h: 8, minW: 8, minH: 6 },
  ],
  md: [
    // Tablet: 3 columns for params, 7 for results
    { i: "resistance-calc-type", x: 0, y: 0, w: 3, h: 2, minW: 3, minH: 2 },
    { i: "resistance-speed-grid", x: 0, y: 2, w: 3, h: 3, minW: 3, minH: 2 },
    { i: "resistance-parameters", x: 0, y: 5, w: 3, h: 4, minW: 3, minH: 3 },
    { i: "resistance-power-params", x: 0, y: 9, w: 3, h: 5, minW: 3, minH: 4 },
    { i: "resistance-kcs-benchmark", x: 0, y: 14, w: 3, h: 4, minW: 3, minH: 3 },
    { i: "resistance-ittc-results", x: 3, y: 0, w: 7, h: 6, minW: 5, minH: 4 },
    { i: "resistance-hm-results", x: 3, y: 6, w: 7, h: 6, minW: 5, minH: 4 },
    { i: "resistance-power-results", x: 3, y: 12, w: 7, h: 6, minW: 5, minH: 4 },
    { i: "resistance-charts", x: 0, y: 18, w: 10, h: 8, minW: 8, minH: 6 },
    { i: "resistance-unified-summary", x: 0, y: 26, w: 10, h: 8, minW: 8, minH: 6 },
  ],
  sm: [
    // Mobile: stack all panels vertically
    { i: "resistance-calc-type", x: 0, y: 0, w: 6, h: 2, minW: 6, minH: 2 },
    { i: "resistance-speed-grid", x: 0, y: 2, w: 6, h: 3, minW: 6, minH: 2 },
    { i: "resistance-parameters", x: 0, y: 5, w: 6, h: 4, minW: 6, minH: 3 },
    { i: "resistance-power-params", x: 0, y: 9, w: 6, h: 5, minW: 6, minH: 4 },
    { i: "resistance-kcs-benchmark", x: 0, y: 14, w: 6, h: 4, minW: 6, minH: 3 },
    { i: "resistance-ittc-results", x: 0, y: 18, w: 6, h: 6, minW: 6, minH: 4 },
    { i: "resistance-hm-results", x: 0, y: 24, w: 6, h: 6, minW: 6, minH: 4 },
    { i: "resistance-power-results", x: 0, y: 30, w: 6, h: 6, minW: 6, minH: 4 },
    { i: "resistance-charts", x: 0, y: 36, w: 6, h: 8, minW: 6, minH: 6 },
    { i: "resistance-unified-summary", x: 0, y: 44, w: 6, h: 8, minW: 6, minH: 6 },
  ],
};

/**
 * Default panel states for resistance workspace
 */
export const RESISTANCE_DEFAULT_PANEL_STATES: Partial<Record<PanelId, PanelState>> = {
  // Input panels - visible in edit mode
  "resistance-calc-type": {
    id: "resistance-calc-type",
    collapsed: false,
    hidden: false,
    fullscreen: false,
  },
  "resistance-speed-grid": {
    id: "resistance-speed-grid",
    collapsed: false,
    hidden: false,
    fullscreen: false,
  },
  "resistance-parameters": {
    id: "resistance-parameters",
    collapsed: false,
    hidden: false,
    fullscreen: false,
  },
  "resistance-power-params": {
    id: "resistance-power-params",
    collapsed: false,
    hidden: false,
    fullscreen: false,
  },
  "resistance-kcs-benchmark": {
    id: "resistance-kcs-benchmark",
    collapsed: false,
    hidden: false,
    fullscreen: false,
  },
  // Results panels - initially hidden (no results yet)
  "resistance-ittc-results": {
    id: "resistance-ittc-results",
    collapsed: false,
    hidden: true,
    fullscreen: false,
  },
  "resistance-hm-results": {
    id: "resistance-hm-results",
    collapsed: false,
    hidden: true,
    fullscreen: false,
  },
  "resistance-power-results": {
    id: "resistance-power-results",
    collapsed: false,
    hidden: true,
    fullscreen: false,
  },
  "resistance-charts": {
    id: "resistance-charts",
    collapsed: false,
    hidden: true,
    fullscreen: false,
  },
  "resistance-unified-summary": {
    id: "resistance-unified-summary",
    collapsed: false,
    hidden: true,
    fullscreen: false,
  },
};

/**
 * Preset layouts for resistance workspace
 */
export const RESISTANCE_LAYOUT_PRESETS: LayoutPreset[] = [
  {
    id: "default",
    name: "Default Split",
    description: "Parameters on left (33%), Results on right (67%)",
    layout: {
      mode: "edit",
      gridLayouts: RESISTANCE_DEFAULT_GRID_LAYOUTS,
      panelStates: RESISTANCE_DEFAULT_PANEL_STATES as Record<PanelId, PanelState>,
      lastModified: new Date().toISOString(),
    },
  },
  {
    id: "parameters-focus",
    name: "Parameters Focus",
    description: "Wider left panel for detailed parameter tuning",
    layout: {
      mode: "edit",
      gridLayouts: {
        lg: [
          { i: "resistance-calc-type", x: 0, y: 0, w: 6, h: 2, minW: 3, minH: 2 },
          { i: "resistance-speed-grid", x: 0, y: 2, w: 6, h: 3, minW: 3, minH: 2 },
          { i: "resistance-parameters", x: 0, y: 5, w: 6, h: 4, minW: 3, minH: 3 },
          { i: "resistance-power-params", x: 0, y: 9, w: 6, h: 6, minW: 3, minH: 4 },
          { i: "resistance-kcs-benchmark", x: 0, y: 15, w: 6, h: 4, minW: 3, minH: 3 },
          { i: "resistance-ittc-results", x: 6, y: 0, w: 6, h: 8, minW: 4, minH: 4 },
          { i: "resistance-hm-results", x: 6, y: 8, w: 6, h: 8, minW: 4, minH: 4 },
          { i: "resistance-power-results", x: 6, y: 16, w: 6, h: 6, minW: 4, minH: 4 },
          { i: "resistance-charts", x: 0, y: 22, w: 12, h: 8, minW: 8, minH: 6 },
        ],
        md: RESISTANCE_DEFAULT_GRID_LAYOUTS.md,
        sm: RESISTANCE_DEFAULT_GRID_LAYOUTS.sm,
      },
      panelStates: RESISTANCE_DEFAULT_PANEL_STATES as Record<PanelId, PanelState>,
      lastModified: new Date().toISOString(),
    },
  },
  {
    id: "results-focus",
    name: "Results Focus",
    description: "Maximize space for tables and charts",
    layout: {
      mode: "view",
      gridLayouts: {
        lg: [
          { i: "resistance-calc-type", x: 0, y: 0, w: 3, h: 2, minW: 3, minH: 2 },
          { i: "resistance-speed-grid", x: 0, y: 2, w: 3, h: 3, minW: 3, minH: 2 },
          { i: "resistance-parameters", x: 0, y: 5, w: 3, h: 4, minW: 3, minH: 3 },
          { i: "resistance-hm-results", x: 3, y: 0, w: 9, h: 6, minW: 6, minH: 4 },
          { i: "resistance-power-results", x: 3, y: 6, w: 9, h: 6, minW: 6, minH: 4 },
          { i: "resistance-charts", x: 0, y: 12, w: 12, h: 10, minW: 8, minH: 6 },
        ],
        md: RESISTANCE_DEFAULT_GRID_LAYOUTS.md,
        sm: RESISTANCE_DEFAULT_GRID_LAYOUTS.sm,
      },
      panelStates: {
        ...(RESISTANCE_DEFAULT_PANEL_STATES as Record<PanelId, PanelState>),
        "resistance-power-params": {
          ...RESISTANCE_DEFAULT_PANEL_STATES["resistance-power-params"],
          hidden: true,
        } as PanelState,
        "resistance-kcs-benchmark": {
          ...RESISTANCE_DEFAULT_PANEL_STATES["resistance-kcs-benchmark"],
          hidden: true,
        } as PanelState,
      },
      lastModified: new Date().toISOString(),
    },
  },
  {
    id: "charts-focus",
    name: "Charts Focus",
    description: "Large chart area with compact results tables",
    layout: {
      mode: "view",
      gridLayouts: {
        lg: [
          { i: "resistance-parameters", x: 0, y: 0, w: 3, h: 4, minW: 3, minH: 3 },
          { i: "resistance-charts", x: 3, y: 0, w: 9, h: 12, minW: 8, minH: 8 },
          { i: "resistance-hm-results", x: 0, y: 12, w: 6, h: 6, minW: 6, minH: 4 },
          { i: "resistance-power-results", x: 6, y: 12, w: 6, h: 6, minW: 6, minH: 4 },
        ],
        md: RESISTANCE_DEFAULT_GRID_LAYOUTS.md,
        sm: RESISTANCE_DEFAULT_GRID_LAYOUTS.sm,
      },
      panelStates: {
        ...(RESISTANCE_DEFAULT_PANEL_STATES as Record<PanelId, PanelState>),
        "resistance-calc-type": {
          ...RESISTANCE_DEFAULT_PANEL_STATES["resistance-calc-type"],
          hidden: true,
        } as PanelState,
        "resistance-speed-grid": {
          ...RESISTANCE_DEFAULT_PANEL_STATES["resistance-speed-grid"],
          hidden: true,
        } as PanelState,
        "resistance-power-params": {
          ...RESISTANCE_DEFAULT_PANEL_STATES["resistance-power-params"],
          hidden: true,
        } as PanelState,
        "resistance-kcs-benchmark": {
          ...RESISTANCE_DEFAULT_PANEL_STATES["resistance-kcs-benchmark"],
          hidden: true,
        } as PanelState,
      },
      lastModified: new Date().toISOString(),
    },
  },
];
