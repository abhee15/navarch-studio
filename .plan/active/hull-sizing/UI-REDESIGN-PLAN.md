# Hull Sizing UI Redesign - Complete Implementation Plan

**Version:** 1.0  
**Date:** November 4, 2025  
**Status:** Ready for Implementation  
**Baseline:** Hydrostatics module UI patterns

---

## Executive Summary

**Goal:** Transform Hull Sizing module to match Hydrostatics quality and consistency while optimizing for both desktop and mobile experiences.

**Key Changes:**
1. Professional styling (icons, colors, components)
2. Draggable panel layout (like Hydrostatics)
3. Compact HUD (maximize viewport space)
4. Dark theme default (industry standard)
5. Mobile-optimized (simplified UI, limited features)

**Effort:** 8-17 hours (phased approach)  
**Impact:** 50-60% more viewport space, professional consistency, better UX

---

## Responsive Design Strategy

### **Breakpoints** (Match Hydrostatics)
```tsx
{
  lg: 1200,  // Desktop (12-col grid, all features)
  md: 996,   // Tablet landscape (10-col grid, most features)
  sm: 768,   // Tablet portrait (6-col grid, simplified)
  mobile: <768  // Phone (stacked, limited features)
}
```

### **Mobile-Specific Constraints:**
- **Disable dragging/resizing** - `isDraggable={!isMobile}`, `isResizable={!isMobile}`
- **Single column layout** - Panels stack vertically
- **Hide secondary panels** - Focus on essential: 1 viewport + parameters
- **Simplified HUD** - Smaller, collapsible
- **Touch-optimized** - Larger tap targets (44px min)

### **Feature Matrix by Screen Size:**

| Feature | Desktop (≥1200) | Tablet (768-1199) | Mobile (<768) |
|---------|-----------------|-------------------|---------------|
| Quad viewports | ✅ Draggable | ✅ Draggable | ❌ 1-up only (tabs) |
| Panel resize | ✅ Yes | ✅ Yes | ❌ Fixed sizes |
| Layout presets | ✅ All 5 | ✅ All 5 | ❌ Mobile preset only |
| KPI panel | ✅ Draggable | ✅ Collapsed | ❌ Modal only |
| Parameter sliders | ✅ Full panel | ✅ Full panel | ✅ Accordion |
| Resistance curve | ✅ Panel | ✅ Panel | ⚠️ Simplified chart |
| Offsets table | ✅ Panel | ⚠️ Horizontal scroll | ❌ Hide (too dense) |
| Export actions | ✅ Toolbar | ✅ Toolbar | ✅ Bottom sheet |

---

## PHASE 1: Foundation & Quick Wins (2-3 hours)

**Priority:** ⭐⭐⭐ CRITICAL  
**Can pause after this phase:** ✅ Yes

### 1.1 Dark Theme Default
**File:** `frontend/src/contexts/ThemeContext.tsx` (line 37)

```tsx
// Current
if (window.matchMedia && window.matchMedia("(prefers-color-scheme: dark)").matches) {
  return "dark";
}
return "light";  // ❌ Light default

// Change to
return "dark";  // ✅ Dark default (industry standard for CAD)
// Still respects system preference if available
```

**Responsive:** No impact (works on all devices)

---

### 1.2 Theme-Aware Canvas Backgrounds
**Files:**
- `frontend/src/components/sizing/visualization/Hull3DScene.tsx` (line 35)
- `frontend/src/components/sizing/visualization/Hull3DThumbnail.tsx` (line 24)

```tsx
// Current
<div className="w-full h-full relative bg-slate-50">

// Change to
<div className="w-full h-full relative bg-gray-100 dark:bg-gray-900">
```

**Responsive:** Works on all devices, better contrast on OLED screens (mobile)

---

### 1.3 Theme-Aware Hull Colors
**File:** `frontend/src/components/sizing/visualization/WigleyHull3D.tsx`

```tsx
// Add at top
import { useTheme } from "../../../contexts/ThemeContext";

// In component (line 21)
export const WigleyHull3D: React.FC<WigleyHull3DProps> = ({
  candidate,
  showWaterplane = true,
  showCenters = true,
  color,  // Remove default here
  opacity = 0.8,
}) => {
  const { theme } = useTheme();
  
  // Dynamic color based on theme
  const hullColor = color || (theme === "dark" ? "#60a5fa" : "#1e40af");
  
  // Use hullColor in mesh material (line 136)
  <meshStandardMaterial
    color={hullColor}
    opacity={opacity}
    transparent={opacity < 1}
  />
```

**Color Scheme:**
- **Light mode:** `#1e40af` (blue-800) - darker for contrast on light gray
- **Dark mode:** `#60a5fa` (blue-400) - brighter for contrast on dark gray

**Responsive:** Better visibility on all screen types

---

### 1.4 Replace Emojis with Lucide Icons
**Files:** All 4 sizing pages + 6 wizard/workspace components

**Icon imports:**
```tsx
import { 
  Rocket,      // 🚀 New Mission
  BarChart3,   // 📊 KPIs
  Ruler,       // 📐 Offsets
  Zap,         // ⚡ Performance
  AlertTriangle, // ⚠️ Warnings
  Info,        // ℹ️ Info
  Home,        // Home button
  Plus,        // Create
  Trash2,      // Delete
  Settings,    // Settings
  Download,    // Export
} from "lucide-react";
```

**Replacement pattern:**
```tsx
// Before
<Button className="...">
  🚀 New Mission
</Button>

// After
<Button>
  <Rocket className="h-4 w-4 mr-2" />
  New Mission
</Button>
```

**Responsive consideration:**
- Desktop/Tablet: Icon + text
- Mobile: Icon only (save space)
```tsx
<Button>
  <Rocket className="h-4 w-4 md:mr-2" />
  <span className="hidden md:inline">New Mission</span>
</Button>
```

**Files to modify:**
1. `pages/sizing/MissionCasesList.tsx` - New Mission button
2. `pages/sizing/SizingRunResults.tsx` - Navigation buttons
3. `pages/sizing/CandidateWorkspace.tsx` - Tabs, actions
4. `pages/sizing/MissionWizard.tsx` - Step indicators
5. `components/sizing/CandidateCard.tsx` - Action buttons
6. `components/sizing/workspace/KPIPanel.tsx` - Export buttons

---

### 1.5 Replace Custom Headers with AppHeader
**Files:** All 4 sizing pages

```tsx
// Before (40 lines of duplicate code per page)
<header className="border-b border-border bg-card/80 backdrop-blur-sm flex-shrink-0 relative z-50">
  <div className="px-4 py-2">
    <div className="flex items-center justify-between">
      <h1 className="text-lg font-bold text-foreground">NavArch Studio</h1>
      <div className="flex items-center space-x-2">
        <button onClick={handleHome} className="inline-flex items-center px-3 py-1.5 text-xs...">
          <svg className="h-4 w-4 mr-1.5">...</svg>
          Home
        </button>
        <UserProfileMenu ... />
      </div>
    </div>
  </div>
</header>

// After (3 lines, responsive built-in)
import { AppHeader } from "../../components/AppHeader";

<AppHeader
  left={
    <>
      <h1 className="text-xl font-semibold">Hull Sizing</h1>
      <span className="hidden md:inline text-sm text-muted-foreground ml-3">
        {/* Page subtitle */}
      </span>
    </>
  }
  right={<UserProfileMenu ... />}
/>
```

**Responsive:** AppHeader handles mobile automatically (h-14 fixed, text truncates)

**Phase 1 Deliverables:**
- ✅ Dark theme default
- ✅ Better contrast (theme-aware colors)
- ✅ Professional icons (lucide-react)
- ✅ Consistent headers (AppHeader)
- ✅ Mobile-friendly (icon-only buttons, responsive AppHeader)

**Testing:**
- [ ] Visual QA at 1920px, 1024px, 768px, 375px
- [ ] Toggle light/dark theme on each breakpoint
- [ ] Verify icons render correctly
- [ ] Check mobile nav works

---

## PHASE 2: Semantic Color Tokens (1-2 hours)

**Priority:** ⭐⭐⭐ HIGH  
**Can pause after this phase:** ✅ Yes

### 2.1 Global Color Token Replacement

**PowerShell bulk replace across `pages/sizing/**` and `components/sizing/**`:**

```powershell
$files = Get-ChildItem -Recurse -Include *.tsx,*.ts

# Backgrounds
$files | ForEach-Object {
  (Get-Content $_.FullName -Raw) `
    -replace 'bg-white(\s+)dark:bg-gray-800', 'bg-card' `
    -replace 'bg-gray-50(\s+)dark:bg-gray-900', 'bg-background' `
    | Set-Content $_.FullName
}

# Text colors
$files | ForEach-Object {
  (Get-Content $_.FullName -Raw) `
    -replace 'text-gray-900(\s+)dark:text-white', 'text-foreground' `
    -replace 'text-gray-600(\s+)dark:text-gray-400', 'text-muted-foreground' `
    | Set-Content $_.FullName
}

# Borders
$files | ForEach-Object {
  (Get-Content $_.FullName -Raw) `
    -replace 'border-gray-200(\s+)dark:border-gray-700', 'border-border' `
    | Set-Content $_.FullName
}
```

### 2.2 Remove Custom Button Gradients

**Find all:**
```tsx
className="bg-gradient-to-r from-blue-600 to-cyan-600 hover:from-blue-700 hover:to-cyan-700"
```

**Replace with:**
```tsx
{/* Remove className, Button uses bg-primary by default */}
```

### 2.3 Standardize Button Components

**Replace raw buttons with Button component:**
```tsx
// Before
<button className="inline-flex items-center px-3 py-1.5 text-xs font-medium text-foreground hover:text-foreground/80 border border-border rounded hover:bg-accent/10">
  Home
</button>

// After
<Button variant="ghost" size="sm">
  <Home className="h-4 w-4 md:mr-2" />
  <span className="hidden md:inline">Home</span>
</Button>
```

**Responsive:** Text hidden on mobile, icon-only

**Phase 2 Deliverables:**
- ✅ All hardcoded colors replaced
- ✅ Theme switching works perfectly
- ✅ Buttons use design system
- ✅ Mobile-optimized button sizes

**Testing:**
- [ ] Toggle theme, verify no hardcoded colors remain
- [ ] Check all buttons on mobile (tap targets ≥44px)
- [ ] Verify focus states visible

---

## PHASE 3: Draggable Panel Layout (3-4 hours)

**Priority:** ⭐⭐⭐ HIGH  
**Can pause after this phase:** ✅ Yes

### 3.1 Create Panel Wrapper Components (8 panels)

**New files:** `components/sizing/workspace/panels/`

1. **PlanViewPanel.tsx**
```tsx
import { Hull2DPlan } from "../../visualization/Hull2DPlan";
import { PanelWrapper } from "../../../hydrostatics/workspace/panels/PanelWrapper";

export function PlanViewPanel({ candidate, collapsed, fullscreen, onToggle, onFullscreen }) {
  return (
    <PanelWrapper
      panelId="plan-view"
      title="Plan View (Top)"
      collapsed={collapsed}
      fullscreen={fullscreen}
      onToggleCollapsed={onToggle}
      onSetFullscreen={onFullscreen}
    >
      <div className="h-full min-h-[300px]">
        <Hull2DPlan candidate={candidate} />
      </div>
    </PanelWrapper>
  );
}
```

2. **ProfileViewPanel.tsx** - Same pattern for Hull2DProfile
3. **SectionsViewPanel.tsx** - Same pattern for Hull2DSections
4. **View3DPanel.tsx** - Same pattern for Hull3DScene
5. **ParameterControlsPanel.tsx** - Wraps ParameterSliders
6. **ResistanceChartPanel.tsx** - Wraps ResistanceCurvePanel
7. **KPISummaryPanel.tsx** - NEW compact metrics panel
8. **OffsetsDataPanel.tsx** - Wraps OffsetsTable

---

### 3.2 Create Workspace Layout Hook

**New file:** `hooks/useSizingWorkspaceLayout.ts`

**Reuse structure from:** `useWorkspaceLayout.ts` (Hydrostatics)

```tsx
export function useSizingWorkspaceLayout(candidateId: string) {
  const [mode, setMode] = useState<"view" | "edit">("view");
  const [panelStates, setPanelStates] = useState<PanelStates>({});
  const [gridLayouts, setGridLayouts] = useState<GridLayouts>({
    lg: defaultLgLayout,
    md: defaultMdLayout,
    sm: defaultSmLayout,
  });
  
  // Load saved layout from localStorage
  useEffect(() => {
    const saved = localStorage.getItem(`sizing-layout-${candidateId}`);
    if (saved) {
      const { layouts, states } = JSON.parse(saved);
      setGridLayouts(layouts);
      setPanelStates(states);
    }
  }, [candidateId]);
  
  // Save on change
  const updateGridLayout = (breakpoint, layout) => {
    const newLayouts = { ...gridLayouts, [breakpoint]: layout };
    setGridLayouts(newLayouts);
    localStorage.setItem(`sizing-layout-${candidateId}`, JSON.stringify({
      layouts: newLayouts,
      states: panelStates,
    }));
  };
  
  // Preset layouts
  const presets = {
    classicQuad: { /* 4 viewports equal */ },
    focus3D: { /* Large 3D, small others */ },
    designer: { /* Plan + Profile dominant */ },
    analyst: { /* Metrics + charts dominant */ },
    mobile: { /* Single column, 1 viewport */ },
  };
  
  return {
    mode,
    setMode,
    gridLayouts,
    panelStates,
    updateGridLayout,
    togglePanelCollapsed,
    setPanelFullscreen,
    loadPreset,
    resetLayout,
    getPresets: () => Object.keys(presets),
  };
}
```

---

### 3.3 Default Layouts per Breakpoint

**Desktop (lg: 12 columns):**
```tsx
const defaultLgLayout = [
  { i: "plan-view", x: 0, y: 0, w: 4, h: 5 },
  { i: "profile-view", x: 4, y: 0, w: 4, h: 5 },
  { i: "sections-view", x: 0, y: 5, w: 4, h: 5 },
  { i: "3d-view", x: 4, y: 5, w: 4, h: 5 },
  { i: "parameters", x: 8, y: 0, w: 4, h: 5 },
  { i: "resistance", x: 8, y: 5, w: 4, h: 5 },
];
```

**Tablet (md: 10 columns):**
```tsx
const defaultMdLayout = [
  { i: "plan-view", x: 0, y: 0, w: 5, h: 4 },
  { i: "profile-view", x: 5, y: 0, w: 5, h: 4 },
  { i: "3d-view", x: 0, y: 4, w: 7, h: 5 },
  { i: "parameters", x: 7, y: 4, w: 3, h: 5 },
  { i: "sections-view", x: 0, y: 9, w: 5, h: 4 },  // Below
  { i: "resistance", x: 5, y: 9, w: 5, h: 4 },
];
```

**Mobile (sm: 6 columns = single column):**
```tsx
const defaultSmLayout = [
  { i: "3d-view", x: 0, y: 0, w: 6, h: 4 },       // Only 1 viewport (3D)
  { i: "parameters", x: 0, y: 4, w: 6, h: 3 },    // Sliders below
  { i: "resistance", x: 0, y: 7, w: 6, h: 3 },    // Chart below
  // Other viewports hidden on mobile
];
```

---

### 3.4 Implement ResponsiveGridLayout

**File:** `pages/sizing/CandidateWorkspace.tsx`

```tsx
import { Responsive, WidthProvider } from "react-grid-layout";
import "react-grid-layout/css/styles.css";
import "react-resizable/css/styles.css";
import { useSizingWorkspaceLayout } from "../../hooks/useSizingWorkspaceLayout";

// Detect mobile
const [isMobile, setIsMobile] = useState(false);
useEffect(() => {
  const checkMobile = () => setIsMobile(window.innerWidth < 768);
  checkMobile();
  window.addEventListener("resize", checkMobile);
  return () => window.removeEventListener("resize", checkMobile);
}, []);

// Layout hook
const {
  gridLayouts,
  panelStates,
  updateGridLayout,
  togglePanelCollapsed,
  setPanelFullscreen,
  loadPreset,
} = useSizingWorkspaceLayout(candidate.id);

// Render
<ResponsiveGridLayout
  className="layout"
  layouts={gridLayouts}
  breakpoints={{ lg: 1200, md: 996, sm: 768 }}
  cols={{ lg: 12, md: 10, sm: 6 }}
  rowHeight={80}
  onLayoutChange={(layout, layouts) => {
    updateGridLayout(currentBreakpoint, layouts[currentBreakpoint]);
  }}
  isDraggable={!isMobile}  // Disable on mobile
  isResizable={!isMobile}   // Disable on mobile
  draggableHandle=".cursor-move"
  compactType={null}
  margin={[12, 12]}
  containerPadding={[0, 0]}
>
  {visiblePanels.map(panelId => (
    <div key={panelId}>
      {renderPanel(panelId)}
    </div>
  ))}
</ResponsiveGridLayout>
```

---

### 3.5 Mobile-Specific Viewport Selector

**For mobile (<768px), add viewport tabs:**

```tsx
{isMobile && (
  <div className="flex gap-1 p-2 bg-card border-b border-border overflow-x-auto">
    <button
      onClick={() => setMobileViewport("3d")}
      className={`px-3 py-2 rounded text-sm whitespace-nowrap ${
        mobileViewport === "3d" ? "bg-primary text-primary-foreground" : "bg-muted"
      }`}
    >
      3D
    </button>
    <button onClick={() => setMobileViewport("plan")} className="...">
      Plan
    </button>
    <button onClick={() => setMobileViewport("profile")} className="...">
      Profile
    </button>
    <button onClick={() => setMobileViewport("sections")} className="...">
      Sections
    </button>
  </div>
)}
```

**Shows:** One viewport at a time on mobile, swipe/tab to switch

---

**Phase 3 Deliverables:**
- ✅ 8 panel components created
- ✅ Draggable grid layout (desktop/tablet)
- ✅ Stacked layout (mobile)
- ✅ Persistent per candidate
- ✅ Touch-optimized for mobile

**Testing:**
- [ ] Desktop: Drag panels, resize, save layout
- [ ] Tablet: Verify 10-column grid works
- [ ] Mobile: Verify single-column stack, no drag
- [ ] Test on actual devices (iOS Safari, Chrome Android)

---

## PHASE 4: Compact HUD & Density (2 hours)

**Priority:** ⭐⭐⭐ HIGH  
**Can pause after this phase:** ✅ Yes

### 4.1 Create Compact HUD Component

**New file:** `components/sizing/workspace/CompactHUD.tsx`

```tsx
interface CompactHUDProps {
  candidate: CandidateDesign;
  flags: string[];
  onShowMetrics: () => void;
  onShowWarnings: () => void;
}

export function CompactHUD({ candidate, flags, onShowMetrics, onShowWarnings }: CompactHUDProps) {
  return (
    <div className="bg-card border border-border rounded-lg p-3 mb-4 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3">
      {/* Left: Essential Info */}
      <div className="flex items-center gap-3 sm:gap-4 min-w-0">
        {/* Rank Badge */}
        <span className="flex-shrink-0 inline-flex items-center justify-center w-10 h-10 rounded-full bg-primary text-primary-foreground font-bold text-base">
          #{candidate.rank}
        </span>
        
        {/* Hull Info */}
        <div className="min-w-0 flex-1">
          <h3 className="font-semibold text-foreground text-sm truncate">
            {candidate.hullFamily.replace("_", " ")}
          </h3>
          
          {/* Desktop: Full dimensions */}
          <p className="text-xs text-muted-foreground hidden md:block">
            Score: {candidate.score.toFixed(1)} • 
            {candidate.lppM.toFixed(1)}m × {candidate.beamM.toFixed(1)}m × {candidate.draftM.toFixed(1)}m
          </p>
          
          {/* Mobile: Score only */}
          <p className="text-xs text-muted-foreground md:hidden">
            Score: {candidate.score.toFixed(1)}
          </p>
        </div>
      </div>
      
      {/* Right: Actions */}
      <div className="flex items-center gap-2 flex-shrink-0">
        {/* Warnings (only if present) */}
        {flags.length > 0 && (
          <Button
            variant="outline"
            size="sm"
            onClick={onShowWarnings}
            className="border-yellow-500/30 bg-yellow-50 dark:bg-yellow-900/20 text-yellow-800 dark:text-yellow-300"
          >
            <AlertTriangle className="h-4 w-4 md:mr-2" />
            <span className="hidden md:inline">{flags.length} warnings</span>
            <span className="md:hidden">{flags.length}</span>
          </Button>
        )}
        
        {/* All Metrics */}
        <Button variant="outline" size="sm" onClick={onShowMetrics}>
          <Info className="h-4 w-4 md:mr-2" />
          <span className="hidden md:inline">All Metrics</span>
        </Button>
      </div>
    </div>
  );
}
```

**Responsive:**
- Mobile: Stacks vertically, shorter text, icon-only buttons
- Tablet/Desktop: Horizontal layout, full text

**Height:**
- Desktop: 60px
- Mobile: ~100px (stacked)
- vs current KPI panel: 1200px+

---

### 4.2 Create KPI Detail Modal

**New file:** `components/sizing/workspace/KPIDetailModal.tsx`

```tsx
interface KPIDetailModalProps {
  candidate: CandidateDesign;
  isOpen: boolean;
  onClose: () => void;
}

export function KPIDetailModal({ candidate, isOpen, onClose }: KPIDetailModalProps) {
  if (!isOpen) return null;
  
  return (
    <dialog
      open={isOpen}
      className="fixed inset-0 z-50 bg-black/50 flex items-center justify-center p-4"
      onClick={onClose}
    >
      <div
        className="bg-card rounded-lg shadow-xl max-w-3xl w-full max-h-[90vh] overflow-y-auto"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="sticky top-0 bg-card border-b border-border p-4 flex items-center justify-between">
          <h2 className="text-lg font-semibold text-foreground">All Metrics</h2>
          <Button variant="ghost" size="sm" onClick={onClose}>
            <X className="h-4 w-4" />
          </Button>
        </div>
        
        {/* Metrics Grid - RESPONSIVE */}
        <div className="p-4 sm:p-6">
          {/* Score Card */}
          <div className="mb-6 p-4 bg-primary/10 border border-primary/30 rounded-lg">
            <div className="grid grid-cols-2 sm:grid-cols-3 gap-4 text-center">
              <div>
                <p className="text-xs text-muted-foreground">Rank</p>
                <p className="text-2xl font-bold text-primary">#{candidate.rank}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Score</p>
                <p className="text-2xl font-bold text-foreground">{candidate.score.toFixed(1)}</p>
              </div>
              <div className="hidden sm:block">
                <p className="text-xs text-muted-foreground">Family</p>
                <p className="text-base font-semibold text-foreground">{candidate.hullFamily}</p>
              </div>
            </div>
          </div>
          
          {/* All Metrics - 2 columns on mobile, 3 on desktop */}
          <div className="grid grid-cols-2 lg:grid-cols-3 gap-4 text-sm">
            {/* Principal Dimensions */}
            <MetricCard label="Lpp" value={candidate.lppM} unit="m" />
            <MetricCard label="Beam" value={candidate.beamM} unit="m" />
            <MetricCard label="Draft" value={candidate.draftM} unit="m" />
            {/* ... all other metrics ... */}
          </div>
        </div>
      </div>
    </dialog>
  );
}
```

**Responsive:**
- Mobile: 2-column grid, smaller text
- Tablet: 2-column grid
- Desktop: 3-column grid

---

### 4.3 Update CandidateWorkspace Layout

**File:** `pages/sizing/CandidateWorkspace.tsx`

```tsx
// Remove old 3-column grid (lines 259-298)
// Replace with:

return (
  <div className="flex min-h-screen flex-col">
    <AppHeader ... />
    
    <main className="flex-1">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8 py-4 sm:py-8">
        {/* Compact HUD */}
        <CompactHUD
          candidate={candidate}
          flags={flags}
          onShowMetrics={() => setShowMetrics(true)}
          onShowWarnings={() => setShowWarnings(true)}
        />
        
        {/* Toolbar - Desktop only */}
        {!isMobile && (
          <div className="mb-4 flex items-center justify-between p-3 bg-card border border-border rounded-lg">
            <div className="flex items-center gap-3">
              <label className="text-sm font-medium text-muted-foreground">Layout:</label>
              <Select value={currentPreset} onChange={loadPreset}>
                <option value="classicQuad">Classic Quad</option>
                <option value="focus3D">3D Focus</option>
                <option value="designer">Designer</option>
                <option value="analyst">Analyst</option>
              </Select>
            </div>
            <div className="flex items-center gap-2">
              <Button variant="outline" size="sm" onClick={resetLayout} title="Reset Layout">
                <RotateCcw className="h-4 w-4" />
              </Button>
              <Button variant="outline" size="sm" onClick={exportAllViews} title="Export Views">
                <Download className="h-4 w-4" />
              </Button>
            </div>
          </div>
        )}
        
        {/* Mobile Viewport Selector */}
        {isMobile && (
          <div className="mb-4 flex gap-2 overflow-x-auto pb-2">
            <Button
              variant={mobileView === "3d" ? "default" : "outline"}
              size="sm"
              onClick={() => setMobileView("3d")}
            >
              3D
            </Button>
            <Button
              variant={mobileView === "plan" ? "default" : "outline"}
              size="sm"
              onClick={() => setMobileView("plan")}
            >
              Plan
            </Button>
            <Button
              variant={mobileView === "profile" ? "default" : "outline"}
              size="sm"
              onClick={() => setMobileView("profile")}
            >
              Profile
            </Button>
            <Button
              variant={mobileView === "sections" ? "default" : "outline"}
              size="sm"
              onClick={() => setMobileView("sections")}
            >
              Sections
            </Button>
          </div>
        )}
        
        {/* Draggable Grid Layout - Desktop/Tablet */}
        {!isMobile && (
          <ResponsiveGridLayout
            layouts={gridLayouts}
            breakpoints={{ lg: 1200, md: 996, sm: 768 }}
            cols={{ lg: 12, md: 10, sm: 6 }}
            rowHeight={80}
            onLayoutChange={handleLayoutChange}
            isDraggable={true}
            isResizable={true}
            draggableHandle=".cursor-move"
          >
            {/* Render panels */}
          </ResponsiveGridLayout>
        )}
        
        {/* Simple Stack - Mobile */}
        {isMobile && (
          <div className="space-y-4">
            {mobileView === "3d" && <View3DPanel candidate={candidate} ... />}
            {mobileView === "plan" && <PlanViewPanel candidate={candidate} ... />}
            {mobileView === "profile" && <ProfileViewPanel candidate={candidate} ... />}
            {mobileView === "sections" && <SectionsViewPanel candidate={candidate} ... />}
            
            {/* Parameters always shown on mobile */}
            <ParameterControlsPanel candidate={candidate} onUpdate={handleAdjust} />
            
            {/* Resistance chart (simplified on mobile) */}
            <ResistanceChartPanel candidate={candidate} simplified={true} />
          </div>
        )}
      </div>
    </main>
    
    {/* Modals */}
    <KPIDetailModal
      candidate={candidate}
      isOpen={showMetrics}
      onClose={() => setShowMetrics(false)}
    />
    <WarningsModal
      flags={flags}
      isOpen={showWarnings}
      onClose={() => setShowWarnings(false)}
    />
  </div>
);
```

---

### 4.4 Density Optimizations

**Changes across all cards/panels:**

```tsx
// Card padding
p-6 → p-4 sm:p-5  // Smaller on mobile

// Grid gaps
gap-6 → gap-3 sm:gap-4  // Tighter on mobile

// Metric grids
grid-cols-2 → grid-cols-2 sm:grid-cols-3  // Responsive columns

// Text sizes
text-lg → text-base sm:text-lg  // Smaller on mobile

// Spacing
mt-4 → mt-3  // Tighter overall
space-y-6 → space-y-4  // Less whitespace
```

**Phase 4 Deliverables:**
- ✅ Compact HUD (60-100px vs 1200px)
- ✅ KPI modal (on-demand)
- ✅ 30% denser layout
- ✅ Mobile viewport tabs
- ✅ Responsive spacing

**Testing:**
- [ ] Test at 375px (iPhone SE)
- [ ] Test at 768px (iPad portrait)
- [ ] Test at 1024px (iPad landscape)
- [ ] Verify touch targets ≥44px
- [ ] Check text legibility on small screens

---

## PHASE 5: Viewport Enhancements (2 hours)

**Priority:** ⭐⭐ MEDIUM  
**Can pause after this phase:** ✅ Yes

### 5.1 Add Rotation Hint (Desktop/Tablet Only)

```tsx
// Hull3DScene.tsx, Hull3DThumbnail.tsx
const [showHint, setShowHint] = useState(true);

useEffect(() => {
  // Auto-hide after 3 seconds
  const timer = setTimeout(() => setShowHint(false), 3000);
  return () => clearTimeout(timer);
}, []);

// Render (only on non-mobile)
{!isMobile && showHint && (
  <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
    <div className="bg-black/60 text-white px-4 py-2 rounded-lg text-sm animate-fade-out">
      <Mouse className="h-4 w-4 inline mr-2" />
      Drag to rotate
    </div>
  </div>
)}
```

**Responsive:** Only show on desktop/tablet (mobile uses touch gestures naturally)

---

### 5.2 Better Camera Angles

**Current issue:** Camera angles not optimized for clarity

```tsx
// Hull3DScene.tsx
// Current
position: [cameraDistance, cameraDistance * 0.6, cameraDistance * 0.8]

// Better: Classic isometric (shows hull form clearly)
position: [cameraDistance * 0.9, cameraDistance * 0.7, cameraDistance * 0.9]

// Hull3DThumbnail.tsx  
// Current
position: [cameraDistance * 0.8, cameraDistance * 0.5, cameraDistance * 0.7]

// Better: Bow-quarter view (emphasizes hull entry)
position: [cameraDistance * 0.6, cameraDistance * 0.6, cameraDistance]
```

**Responsive:** Same camera angles on all devices (THREE.js handles viewport automatically)

---

### 5.3 Quick View Angle Buttons (Desktop Only)

**New component:** `components/sizing/visualization/ViewAngleControls.tsx`

```tsx
{!isMobile && (
  <div className="absolute top-16 right-4 flex flex-col gap-1 bg-card/90 backdrop-blur rounded-lg p-1 shadow-lg border border-border">
    <button
      onClick={() => setCameraPreset("iso")}
      className="p-2 hover:bg-accent rounded"
      title="Isometric (I)"
    >
      <Box className="h-4 w-4" />
    </button>
    <button
      onClick={() => setCameraPreset("front")}
      className="p-2 hover:bg-accent rounded"
      title="Bow View (F)"
    >
      <ArrowUp className="h-4 w-4" />
    </button>
    <button
      onClick={() => setCameraPreset("side")}
      className="p-2 hover:bg-accent rounded"
      title="Profile (S)"
    >
      <ArrowRight className="h-4 w-4" />
    </button>
    <button
      onClick={() => setCameraPreset("top")}
      className="p-2 hover:bg-accent rounded"
      title="Plan (T)"
    >
      <Maximize2 className="h-4 w-4" />
    </button>
  </div>
)}
```

**Responsive:** Hidden on mobile (takes up precious space, less useful on small screens)

**Phase 5 Deliverables:**
- ✅ Rotation hint (desktop only)
- ✅ Better default camera angles
- ✅ Quick view angle switching (desktop only)
- ✅ Mobile-optimized (simpler controls)

---

## Mobile-Specific Optimizations Summary

### **Features LIMITED on Mobile (<768px):**

| Feature | Desktop | Mobile | Reason |
|---------|---------|--------|--------|
| Panel dragging | ✅ | ❌ | Touch conflicts, not needed |
| Panel resizing | ✅ | ❌ | Too fiddly on touch |
| Multiple viewports | ✅ 4-up | ❌ 1-up (tabs) | Screen too small |
| Layout presets | ✅ | ❌ | Single preset (mobile) |
| Rotation hint | ✅ | ❌ | Touch is intuitive |
| View angle buttons | ✅ | ❌ | Takes space |
| Offsets table | ✅ | ❌ Hidden | Too dense |
| Full KPI panel | ✅ Modal | ✅ Modal | Same (works well) |
| Export toolbar | ✅ Inline | ⚠️ Bottom sheet | Better UX |

### **Features OPTIMIZED for Mobile:**

1. **Viewport tabs** - Easy thumb navigation
2. **Larger tap targets** - Buttons ≥44px
3. **Stacked layout** - Natural scrolling
4. **Icon-only buttons** - Save space
5. **Simplified charts** - Fewer data points, larger text
6. **Bottom action sheet** - Export/actions at thumb level

---

## Testing Matrix

### **Desktop (≥1200px):**
- [ ] Chrome, Firefox, Safari, Edge
- [ ] Drag panels smoothly
- [ ] Resize panels to minimum/maximum
- [ ] Switch presets, verify layouts
- [ ] All features accessible
- [ ] Performance: 60fps during drag

### **Tablet (768-1199px):**
- [ ] iPad (1024×768), iPad Pro (1366×1024)
- [ ] 10-column grid renders correctly
- [ ] Panels draggable but not too small
- [ ] Touch gestures work (pinch zoom in 3D)

### **Mobile (<768px):**
- [ ] iPhone SE (375px), iPhone 14 (390px), iPhone 14 Pro Max (430px)
- [ ] Single column stacks correctly
- [ ] Viewport tabs work
- [ ] No horizontal scroll
- [ ] Buttons ≥44px tap target
- [ ] Text readable without zoom
- [ ] 3D rotation works with touch

---

## Performance Considerations

### **Mobile Performance:**
- Disable panel animations on slow devices
- Lazy load panels below fold
- Reduce 3D mesh quality on mobile
- Debounce parameter slider updates

```tsx
// Detect reduced motion preference
const prefersReducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

// Reduce 3D quality on mobile
const meshSegments = isMobile ? 30 : 60;  // Half resolution on mobile
```

---

## Accessibility Considerations

### **Keyboard Navigation:**
- Tab through all interactive elements
- Arrow keys to adjust sliders
- Number keys (1-4) to switch viewports
- Esc to close modals
- ? to show keyboard shortcuts

### **Screen Readers:**
- Proper ARIA labels on all panels
- Announce layout changes
- Describe 3D visualizations

### **Touch Accessibility:**
- All tap targets ≥44px
- Swipe gestures optional (buttons always available)
- Pinch zoom works on mobile

---

## Implementation Checklist

### **Phase 1 (Foundation):**
- [ ] Change theme default to dark
- [ ] Add theme-aware canvas backgrounds
- [ ] Add theme-aware hull colors
- [ ] Replace all emojis with lucide icons
  - [ ] Test icon sizes responsive (h-4 sm:h-5)
- [ ] Replace headers with AppHeader
  - [ ] Test mobile header truncation
- [ ] **Mobile QA:** Test on 375px, 768px, 1200px

### **Phase 2 (Color Tokens):**
- [ ] Run bulk color token replacement
- [ ] Remove custom button gradients
- [ ] Replace raw buttons with Button component
  - [ ] Add responsive text (hidden md:inline)
- [ ] **Theme QA:** Toggle light/dark on all breakpoints

### **Phase 3 (Draggable Layout):**
- [ ] Create 8 panel components
- [ ] Create useSizingWorkspaceLayout hook
- [ ] Implement ResponsiveGridLayout
  - [ ] Define lg/md/sm layouts
  - [ ] Add isDraggable={!isMobile}
- [ ] Add mobile viewport tabs
- [ ] Add mobile stacked layout
- [ ] **Responsive QA:** Test grid at all breakpoints
- [ ] **Touch QA:** Test on iPad, iPhone

### **Phase 4 (Compact HUD):**
- [ ] Create CompactHUD component
  - [ ] Test responsive stacking
- [ ] Create KPIDetailModal
  - [ ] Test 2-col vs 3-col grid
- [ ] Apply density optimizations
  - [ ] Test text legibility on mobile
- [ ] **Mobile QA:** Verify HUD readable at 375px

### **Phase 5 (Enhancements):**
- [ ] Add rotation hint (desktop only)
- [ ] Improve camera angles
- [ ] Add view angle controls (desktop only)
- [ ] **Cross-device QA:** Verify mobile not affected

---

## Mobile-First Design Decisions

### **Must Work Well on Mobile:**
1. ✅ Create mission (wizard)
2. ✅ View results (candidate cards)
3. ✅ Adjust parameters (sliders)
4. ✅ See 3D hull (touch to rotate)
5. ✅ View key metrics (HUD + modal)

### **OK to be Limited on Mobile:**
1. ⚠️ Customize layout (use default)
2. ⚠️ See all viewports simultaneously (1-up only)
3. ⚠️ Offsets table (hide, not critical)
4. ⚠️ Advanced export (simplified options)

### **Not Needed on Mobile:**
1. ❌ Drag/resize panels
2. ❌ Layout presets
3. ❌ Keyboard shortcuts
4. ❌ View angle buttons

---

## Progressive Enhancement Strategy

**Core functionality (works everywhere):**
- View mission list
- Create mission (wizard)
- Run solver
- View candidate results
- See 3D visualization
- Adjust basic parameters

**Enhanced (desktop/tablet):**
- Draggable panels
- Multi-viewport view
- Layout customization
- Advanced export
- Keyboard shortcuts

**Premium (desktop only):**
- Full workspace customization
- All panels simultaneously
- Maximum detail/density

---

## Final Recommendation

**Approach:** Mobile-first implementation, progressive enhancement

**Order:**
1. **Phase 1-2:** Foundation + colors (works well on ALL devices)
2. **Test on mobile** - Verify core experience good
3. **Phase 3:** Add draggable layout (desktop/tablet only)
4. **Test on tablet** - Verify drag/touch works
5. **Phase 4:** Optimize density (responsive spacing)
6. **Final QA:** Test on real devices across breakpoints

**Mobile Strategy:**
- Simple, focused experience (1 viewport, essential controls)
- No feature bloat on small screens
- Performance optimized (reduced 3D quality)
- Touch-first interactions

**Desktop Strategy:**
- Full power user features
- Customizable workspace
- Maximum information density
- Keyboard-optimized

---

## Success Criteria (Per Device)

### **Mobile (<768px):**
- [ ] Can complete full workflow (create → solve → explore)
- [ ] No horizontal scroll
- [ ] Text readable without zoom
- [ ] Buttons easy to tap (≥44px)
- [ ] Loads fast (< 3s on 4G)
- [ ] Battery efficient (3D not too heavy)

### **Tablet (768-1199px):**
- [ ] Draggable panels work with touch
- [ ] 2-up or 3-up viewports usable
- [ ] All core features accessible
- [ ] Split-screen friendly (iPad multitasking)

### **Desktop (≥1200px):**
- [ ] All features available
- [ ] Customizable to user preference
- [ ] Fast, smooth interactions
- [ ] Multi-monitor support

---

## Ready to Proceed!

**Plan is complete with:**
- ✅ 7 detailed phases
- ✅ Responsive design at every step
- ✅ Mobile-first approach
- ✅ Progressive enhancement
- ✅ Clear testing criteria
- ✅ Realistic effort estimates

**Next steps:**
1. Wait for current deployment (property fix)
2. Test with fresh data
3. Start Phase 1 implementation
4. Get feedback before Phase 3

**Questions before starting:**
1. Should I wait for current deployment first? (recommended)
2. Start with Phase 1 now? (can work in parallel)
3. Want to review mobile mockups first?

Your call! 🎯














