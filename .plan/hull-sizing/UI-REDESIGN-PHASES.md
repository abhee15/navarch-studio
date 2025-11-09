# Hull Sizing UI Redesign - Phased Implementation Plan

**Goal:** Align Hull Sizing module with Hydrostatics baseline for consistency, improve UX, maximize viewport space

**Status:** Planning Complete - Ready for Implementation  
**Estimated Total Effort:** 8-12 hours (can be split across multiple sessions)  
**Priority:** HIGH - UX consistency critical for professional application

---

## Current State Assessment

### **Critical Issues:**
1. ❌ Inconsistent UI patterns (different from Hydrostatics/Resistance)
2. ❌ Wasted space (KPI panel takes 33% width, shows redundant info)
3. ❌ Fixed layout (can't customize, no draggable panels)
4. ❌ Light theme default (industry standard is dark for CAD/engineering)
5. ❌ Poor theme contrast (blue hull on light gray canvas in both themes)
6. ❌ Inline SVG icons everywhere (verbose, inconsistent with lucide-react)
7. ❌ Emojis in UI chrome (unprofessional: 🚀 📊 📐 ⚡)

### **Space Utilization Problems:**
- KPI panel: 1/3 width showing 20+ metrics (most redundant)
- Large padding: `p-6` everywhere
- Viewports constrained by surrounding chrome
- Can only see 1 or 4 viewports (no 2-up, 3-up, custom)

---

## PHASE 1: Foundation & Quick Wins (2-3 hours)

**Priority:** CRITICAL - These fix immediate user complaints and establish baseline

### 1.1 Dark Theme Default
**File:** `frontend/src/contexts/ThemeContext.tsx`
```tsx
// Line 37: Change default
return "dark";  // Was: "light"
```
**Why first:** Industry standard for CAD/engineering apps, better for long sessions

### 1.2 Theme-Aware Canvas Backgrounds
**Files:**
- `frontend/src/components/sizing/visualization/Hull3DScene.tsx`
- `frontend/src/components/sizing/visualization/Hull3DThumbnail.tsx`

```tsx
// Replace: bg-slate-50
// With:    bg-gray-100 dark:bg-gray-900
```
**Why first:** Fixes poor contrast issue immediately

### 1.3 Theme-Aware Hull Colors
**File:** `frontend/src/components/sizing/visualization/WigleyHull3D.tsx`
```tsx
// Add hook to detect theme
import { useTheme } from "../../../contexts/ThemeContext";

// In component:
const { theme } = useTheme();
const defaultColor = theme === "dark" ? "#60a5fa" : "#1e40af";  // blue-400 vs blue-800

// Update prop default (line 25)
color = defaultColor,
```
**Why first:** Better visual contrast in both themes

### 1.4 Replace Emojis with Lucide Icons
**Files:** All sizing pages/components
**Changes:**
- Import lucide-react icons
- Replace 🚀 → `<Rocket />`
- Replace 📊 → `<BarChart3 />`
- Replace 📐 → `<Ruler />`
- Replace ⚡ → `<Zap />`

**Example:**
```tsx
// Before
<Button>🚀 New Mission</Button>

// After
import { Rocket } from "lucide-react";
<Button><Rocket className="h-4 w-4 mr-2" />New Mission</Button>
```

**Why first:** Low effort, high impact on professionalism

### 1.5 Replace Custom Headers with AppHeader
**Files:**
- `pages/sizing/MissionCasesList.tsx`
- `pages/sizing/SizingRunResults.tsx`
- `pages/sizing/CandidateWorkspace.tsx`
- `pages/sizing/MissionWizard.tsx`

```tsx
// Before (40+ lines of duplicated header code)
<header className="border-b...">
  <div className="px-4 py-2">
    <div className="flex items-center justify-between">
      <h1>NavArch Studio</h1>
      <button>Home</button>
    </div>
  </div>
</header>

// After (3 lines)
import { AppHeader } from "../../components/AppHeader";
<AppHeader
  left={<h1 className="text-xl font-semibold">Hull Sizing</h1>}
  right={<UserProfileMenu ... />}
/>
```

**Why first:** Eliminates code duplication, ensures consistent header height (56px)

**Phase 1 Deliverables:**
- ✅ Dark theme by default
- ✅ Better canvas/hull contrast
- ✅ Professional icons (no emojis)
- ✅ Consistent headers
- ✅ ~200 lines of code removed (deduplication)

**Testing:** Visual QA - check light/dark themes, verify icons render

---

## PHASE 2: Semantic Color Tokens (1-2 hours)

**Priority:** HIGH - Enables proper theming and maintainability

### 2.1 Global Search-Replace in Sizing Module
**Tool:** PowerShell bulk replace across all `pages/sizing/**` and `components/sizing/**`

**Replacements:**
```tsx
// Backgrounds
bg-white           → bg-card
dark:bg-gray-800   → (remove, bg-card handles both)

// Text
text-gray-900      → text-foreground
dark:text-white    → (remove, text-foreground handles both)
text-gray-600      → text-muted-foreground
dark:text-gray-400 → (remove)

// Borders
border-gray-200    → border-border
dark:border-gray-700 → (remove)

// Primary button backgrounds (remove custom gradients)
bg-gradient-to-r from-blue-600 to-cyan-600 → bg-primary
text-primary-foreground (already handled by Button component)
```

### 2.2 Update Button Components
**Files:** All sizing pages

```tsx
// Before
<Button className="bg-gradient-to-r from-blue-600 to-cyan-600 hover:from-blue-700 hover:to-cyan-700">

// After
<Button>  {/* Uses default bg-primary from design system */}
```

### 2.3 Replace Raw Buttons with Button Component
**Files:** All sizing pages (header buttons, card actions)

```tsx
// Before
<button className="inline-flex items-center px-3 py-1.5 text-xs...">
  Home
</button>

// After
<Button variant="ghost" size="sm">
  <Home className="h-4 w-4 mr-2" />
  Home
</Button>
```

**Phase 2 Deliverables:**
- ✅ All colors use semantic tokens
- ✅ Theme switching works perfectly
- ✅ No hardcoded colors
- ✅ Consistent with Hydrostatics

**Testing:** Toggle theme, verify all colors adapt correctly

---

## PHASE 3: Draggable Panel Layout (3-4 hours)

**Priority:** HIGH - Core UX improvement, enables customization

### 3.1 Create Panel Components
**New files:** (8 panel components)
1. `components/sizing/workspace/panels/PlanViewPanel.tsx`
2. `components/sizing/workspace/panels/ProfileViewPanel.tsx`
3. `components/sizing/workspace/panels/SectionsViewPanel.tsx`
4. `components/sizing/workspace/panels/View3DPanel.tsx`
5. `components/sizing/workspace/panels/KPISummaryPanel.tsx`
6. `components/sizing/workspace/panels/ParameterControlsPanel.tsx`
7. `components/sizing/workspace/panels/ResistanceCurvePanel.tsx`
8. `components/sizing/workspace/panels/OffsetsTablePanel.tsx`

**Pattern:** Wrap existing components in PanelWrapper
```tsx
import { PanelWrapper } from "../../hydrostatics/workspace/panels/PanelWrapper";

export function PlanViewPanel({ candidate, onExport }) {
  return (
    <PanelWrapper
      title="Plan View"
      onCollapse={...}
      onFullscreen={...}
      onExport={onExport}
    >
      <Hull2DPlan candidate={candidate} />
    </PanelWrapper>
  );
}
```

### 3.2 Create Workspace Grid Layout Hook
**File:** `frontend/src/hooks/useSizingWorkspaceLayout.ts`

**Reuse pattern from:** `useWorkspaceLayout.ts` (Hydrostatics)

**Customizations for Hull Sizing:**
- Default layout: Quad viewports + parameters + resistance
- Presets:
  - "Classic Quad" - 4 viewports equal size
  - "3D Focus" - Large 3D, small plan/profile/sections
  - "Designer" - Plan + Profile dominant
  - "Analyst" - Charts/KPIs dominant
  - "Compact" - All panels collapsed, just viewports

### 3.3 Implement Grid Layout in CandidateWorkspace
**File:** `pages/sizing/CandidateWorkspace.tsx`

```tsx
import { Responsive, WidthProvider } from "react-grid-layout";
import { useSizingWorkspaceLayout } from "../../hooks/useSizingWorkspaceLayout";

// In component:
const {
  layout,
  gridLayouts,
  panelStates,
  updateGridLayout,
  togglePanelCollapsed,
  setPanelFullscreen,
  loadPreset,
  getPresets,
} = useSizingWorkspaceLayout(candidate.id);

// Render
<ResponsiveGridLayout
  layouts={gridLayouts}
  breakpoints={{ lg: 1200, md: 996, sm: 768 }}
  cols={{ lg: 12, md: 10, sm: 6 }}
  rowHeight={80}
  onLayoutChange={handleLayoutChange}
  isDraggable={!isMobile}
  isResizable={!isMobile}
  draggableHandle=".cursor-move"
>
  {visiblePanels.map(panelId => (
    <div key={panelId} data-grid={getGridPosition(panelId)}>
      {renderPanel(panelId)}
    </div>
  ))}
</ResponsiveGridLayout>
```

### 3.4 Add Toolbar with Presets
**Component:** Above grid layout

```tsx
<div className="flex items-center justify-between mb-4 p-3 bg-card border border-border rounded-lg">
  <div className="flex items-center gap-3">
    <label className="text-sm font-medium text-muted-foreground">Layout:</label>
    <Select value={currentPreset} onChange={loadPreset}>
      <option>Classic Quad</option>
      <option>3D Focus</option>
      <option>Designer</option>
      <option>Analyst</option>
    </Select>
  </div>
  <div className="flex items-center gap-2">
    <Button variant="outline" size="sm" onClick={resetLayout}>
      <RotateCcw className="h-4 w-4" />
    </Button>
    <Button variant="outline" size="sm" onClick={saveAsPreset}>
      <Save className="h-4 w-4" />
    </Button>
  </div>
</div>
```

**Phase 3 Deliverables:**
- ✅ 8 draggable, resizable panels
- ✅ Layout presets
- ✅ Persistent layouts (per candidate)
- ✅ Consistent with Hydrostatics UX

**Testing:** 
- Drag panels, verify position saves
- Resize panels, verify size saves
- Switch presets, verify layouts load
- Test on mobile (dragging disabled)

---

## PHASE 4: Compact HUD & Space Optimization (2 hours)

**Priority:** HIGH - Maximizes viewport space

### 4.1 Replace KPI Panel with Compact HUD
**File:** `pages/sizing/CandidateWorkspace.tsx`

**Remove:** Left column with tabbed KPI panel (current ~400px width)

**Add:** Compact HUD bar above grid:
```tsx
<div className="bg-card border border-border rounded-lg p-3 mb-4 flex items-center justify-between">
  {/* Left: Essential Info */}
  <div className="flex items-center gap-4">
    <span className="inline-flex items-center justify-center w-10 h-10 rounded-full bg-primary text-primary-foreground font-bold">
      #{candidate.rank}
    </span>
    <div>
      <h3 className="font-semibold text-foreground text-sm">
        {candidate.hullFamily.replace("_", " ")}
      </h3>
      <p className="text-xs text-muted-foreground">
        Score: {candidate.score.toFixed(1)} • 
        {candidate.lppM.toFixed(1)}m × {candidate.beamM.toFixed(1)}m × {candidate.draftM.toFixed(1)}m
      </p>
    </div>
  </div>
  
  {/* Right: Actions */}
  <div className="flex items-center gap-2">
    {flags.length > 0 && (
      <button 
        onClick={() => setShowWarnings(!showWarnings)}
        className="flex items-center gap-2 px-3 py-1.5 rounded-lg border border-yellow-500/30 bg-yellow-50 dark:bg-yellow-900/20 text-yellow-800 dark:text-yellow-300 hover:bg-yellow-100 dark:hover:bg-yellow-900/30 transition-colors"
      >
        <AlertTriangle className="h-4 w-4" />
        {flags.length} warnings
      </button>
    )}
    <Button variant="outline" size="sm" onClick={() => setShowAllMetrics(true)}>
      <Info className="h-4 w-4 mr-2" />
      All Metrics
    </Button>
  </div>
</div>
```

**Height:** 60px (vs current ~1200px KPI panel)

### 4.2 Create KPI Detail Modal
**New file:** `components/sizing/workspace/KPIDetailModal.tsx`

Shows all metrics in compact modal when user clicks "All Metrics":
```tsx
<dialog className="max-w-2xl w-full">
  <div className="grid grid-cols-2 gap-4">
    {/* All 20+ metrics in dense 2-column grid */}
    {/* Height: ~400px vs current ~1200px */}
  </div>
</dialog>
```

### 4.3 Reduce Padding & Increase Density
**Files:** All sizing cards and panels

**Changes:**
- Card padding: `p-6` → `p-5`
- Grid gaps: `gap-6` → `gap-4`
- Metric grids: `gap-4` → `gap-2`
- Item padding: `py-3` → `py-2`
- Heading margins: `mt-4` → `mt-3`

### 4.4 Optimize Metric Display
**File:** `components/sizing/CandidateCard.tsx`

```tsx
// Before: 2-column grid (shows less info)
<div className="mt-4 grid grid-cols-2 gap-4 text-sm">

// After: 3-column grid (denser, shows more)
<div className="mt-3 grid grid-cols-3 gap-2 text-xs">
```

**Phase 4 Deliverables:**
- ✅ Compact HUD (60px vs 1200px)
- ✅ KPI details available in modal (on-demand)
- ✅ 20-30% denser layout overall
- ✅ ~50% more viewport space

**Testing:**
- Verify all metrics accessible via modal
- Check warnings display correctly
- Measure viewport size increase

---

## PHASE 5: Viewport Enhancements (2 hours)

**Priority:** MEDIUM - Improves visualization UX

### 5.1 Add Rotation Hint to 3D Views
**Files:**
- `components/sizing/visualization/Hull3DScene.tsx`
- `components/sizing/visualization/Hull3DThumbnail.tsx`

```tsx
const [showHint, setShowHint] = useState(true);

// Add overlay that fades after 3s or first interaction
{showHint && (
  <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
    <div className="bg-black/60 text-white px-4 py-2 rounded-lg text-sm animate-pulse">
      <Mouse className="h-4 w-4 inline mr-2" />
      Drag to rotate
    </div>
  </div>
)}
```

### 5.2 Better Camera Angles
**Files:** Hull3DScene, Hull3DThumbnail

```tsx
// Current: Oblique view
position: [cameraDistance * 0.8, cameraDistance * 0.5, cameraDistance * 0.7]

// Proposed: Classic isometric (better shows hull form)
position: [cameraDistance, cameraDistance * 0.7, cameraDistance]
```

### 5.3 Add View Angle Presets
**New component:** `components/sizing/visualization/ViewAngleButtons.tsx`

```tsx
<div className="absolute top-16 right-4 flex flex-col gap-1">
  <button onClick={() => setCameraView("iso")} title="Isometric">⬚</button>
  <button onClick={() => setCameraView("front")} title="Bow">↑</button>
  <button onClick={() => setCameraView("side")} title="Profile">→</button>
  <button onClick={() => setCameraView("top")} title="Plan">⊤</button>
</div>
```

**Phase 5 Deliverables:**
- ✅ Rotation hint for new users
- ✅ Better default camera angle
- ✅ Quick view angle switching

**Testing:** 
- Verify hint shows and fades
- Test camera angles for clarity
- Check mobile responsiveness

---

## PHASE 6: Advanced Features (2-3 hours)

**Priority:** MEDIUM-LOW - Nice to have, can be done incrementally

### 6.1 Auto-Generate TypeScript Types from Backend DTOs
**Tool:** NSwag or openapi-typescript

**Setup:**
1. Add NSwag NuGet package to backend
2. Configure OpenAPI generation
3. Add npm script: `npm run generate:types`
4. Auto-generate `src/types/generated/sizing.ts`

**Benefits:**
- Prevents property name mismatches (like beamM vs bM)
- Single source of truth
- Auto-updates when backend changes

**Implementation:**
```json
// package.json
"scripts": {
  "generate:types": "nswag openapi2tsclient /input:http://localhost:5000/swagger/v1/swagger.json /output:src/types/generated/sizing.ts"
}
```

### 6.2 Parameter Sensitivity Indicators
**File:** `components/sizing/workspace/ParameterSliders.tsx`

Show how sensitive score/displacement is to each parameter:
```tsx
<div className="flex items-center justify-between">
  <label>Length (Lpp)</label>
  <span className="text-xs text-muted-foreground">
    Sensitivity: 
    <span className="font-semibold text-orange-600">HIGH</span>
    {/* Shows: LOW | MEDIUM | HIGH based on ∂score/∂param */}
  </span>
</div>
<Slider ... />
```

### 6.3 DXF Export for 2D Lines Plan
**New service:** `frontend/src/services/dxfExporter.ts`

Generates DXF file from 2D plan view for AutoCAD import:
```tsx
export function exportDXF(candidate: CandidateDesign): string {
  const dxf = new DxfWriter();
  
  // Add waterlines as polylines
  waterlines.forEach(wl => {
    dxf.addPolyline(wl.points, { layer: "WATERLINES", color: 5 });
  });
  
  // Add stations
  stations.forEach(st => {
    dxf.addPolyline(st.points, { layer: "STATIONS", color: 3 });
  });
  
  return dxf.stringify();
}
```

**Phase 6 Deliverables:**
- ✅ Type safety from backend DTOs
- ✅ Sensitivity analysis
- ✅ DXF export capability

**Testing:**
- Generate types, verify match backend
- Check sensitivity indicators accuracy
- Import DXF into AutoCAD/FreeCAD

---

## PHASE 7: Polish & Refinements (1 hour)

**Priority:** LOW - Final touches

### 7.1 Add Keyboard Shortcuts
**File:** `pages/sizing/CandidateWorkspace.tsx`

```tsx
useEffect(() => {
  const handleKeyPress = (e: KeyboardEvent) => {
    if (e.target instanceof HTMLInputElement) return;
    
    switch(e.key) {
      case '1': focusPanel('plan-view'); break;
      case '2': focusPanel('profile-view'); break;
      case '3': focusPanel('sections-view'); break;
      case '4': focusPanel('3d-view'); break;
      case 'm': toggleShowAllMetrics(); break;
      case 'p': loadPreset('classic-quad'); break;
    }
  };
  
  window.addEventListener('keydown', handleKeyPress);
  return () => window.removeEventListener('keydown', handleKeyPress);
}, []);
```

### 7.2 Add Loading States & Skeleton Screens
**Pattern:** Show skeleton while candidates load

```tsx
{loading && (
  <div className="animate-pulse">
    <div className="h-10 bg-gray-200 dark:bg-gray-700 rounded"></div>
    <div className="mt-4 grid grid-cols-3 gap-4">
      {[1,2,3].map(i => (
        <div key={i} className="h-64 bg-gray-200 dark:bg-gray-700 rounded"></div>
      ))}
    </div>
  </div>
)}
```

### 7.3 Add Tooltips for Complex Metrics
**Library:** Consider adding `@radix-ui/react-tooltip`

```tsx
<Tooltip content="Block coefficient: Volume / (Lpp × B × T)">
  <span className="cursor-help underline decoration-dotted">Cb</span>
</Tooltip>
```

**Phase 7 Deliverables:**
- ✅ Keyboard shortcuts
- ✅ Smooth loading states
- ✅ Helpful tooltips

**Testing:** 
- Verify shortcuts work
- Check skeleton screens
- Test tooltip positioning

---

## Additional Suggestions & Considerations

### **Suggestion 1: Add Comparison Mode in Workspace**
Currently: Compare in separate page
**Better:** Compare 2-3 candidates side-by-side in same workspace

**Implementation:**
- Add "Split View" button in toolbar
- Divide grid into 2-3 vertical sections
- Each section shows different candidate
- Synchronized scrolling/zooming

**Benefit:** Direct visual comparison without context switching

---

### **Suggestion 2: Real-Time Collaboration Indicators**
**Future feature:** Show if other users viewing same candidate

```tsx
<div className="flex items-center gap-2">
  <Avatar size="sm" src={user.avatar} />
  <span className="text-xs">John viewing</span>
</div>
```

**Use case:** Team design reviews

---

### **Suggestion 3: Hull Color Customization**
**Add to settings:**
- User picks hull color (blue, gray, red oxide, white)
- Saved per user
- Applied to all 3D views

**Why:** Personal preference, visual accessibility

---

### **Suggestion 4: Export All Views as PDF Report**
**Feature:** Generate PDF with all 4 views + KPIs

```tsx
<Button onClick={generateReport}>
  <FileText className="h-4 w-4 mr-2" />
  Generate Report (PDF)
</Button>
```

**Uses:** jsPDF + html2canvas (already installed)

---

### **Suggestion 5: Parameter Change History**
**Feature:** Track parameter adjustments

```tsx
<div className="text-xs text-muted-foreground">
  Last change: Lpp 52.3m → 54.1m (+3.4%) • 2 min ago
  <button className="text-primary">Undo</button>
</div>
```

**Benefit:** Experiment tracking, easy rollback

---

## Implementation Priority Matrix

| Phase | Effort | Impact | Priority | Dependencies |
|-------|--------|--------|----------|--------------|
| **Phase 1** | 2-3h | HIGH | **CRITICAL** | None |
| **Phase 2** | 1-2h | MEDIUM | **HIGH** | Phase 1 |
| **Phase 3** | 3-4h | HIGH | **HIGH** | Phase 1, 2 |
| **Phase 4** | 2h | HIGH | **HIGH** | Phase 3 |
| **Phase 5** | 2h | MEDIUM | MEDIUM | Phase 3 |
| **Phase 6** | 2-3h | LOW | LOW | None (independent) |
| **Phase 7** | 1h | LOW | LOW | All others |

**Recommended order:** 1 → 2 → 3 → 4 → 5 → 6 → 7

**Can pause after Phase 4** for user feedback before continuing

---

## Risks & Mitigation

### **Risk 1: Breaking Existing User Workflows**
**Mitigation:**
- Implement "Classic Quad" preset that matches current layout
- Add migration guide
- Keep for 1-2 releases with deprecation notice

### **Risk 2: Performance with Many Panels**
**Mitigation:**
- Lazy load panel content
- Virtualize long lists (offsets table)
- Debounce layout changes

### **Risk 3: Mobile Experience**
**Mitigation:**
- Disable drag/resize on mobile
- Provide mobile-optimized preset (stacked layout)
- Test on tablets (768px breakpoint)

### **Risk 4: Layout State Complexity**
**Mitigation:**
- Reuse proven `useWorkspaceLayout` hook
- Add reset button (escape hatch)
- Provide good defaults

---

## Success Criteria

### **Must Have (MVP):**
- [ ] Dark theme default
- [ ] Consistent icons (lucide-react, no emojis)
- [ ] AppHeader on all pages
- [ ] Semantic color tokens throughout
- [ ] Draggable panel layout working
- [ ] Compact HUD replacing KPI panel
- [ ] 40-50% more viewport space

### **Should Have (Full):**
- [ ] Theme-aware canvas/hull colors
- [ ] Layout presets (4-5 options)
- [ ] Persistent layouts per candidate
- [ ] Rotation hints on 3D views
- [ ] Better camera angles
- [ ] Dense metric display

### **Nice to Have (Polish):**
- [ ] Auto-generated TS types
- [ ] Sensitivity indicators
- [ ] DXF export
- [ ] Keyboard shortcuts
- [ ] Comparison mode in workspace

---

## Testing Strategy

### **Visual Regression:**
- Screenshot before/after each phase
- Compare with Hydrostatics module
- Verify theme consistency

### **Functional:**
- Create mission → run solver → open workspace
- Drag panels, resize, collapse, fullscreen
- Switch layouts, verify persistence
- Test mobile responsiveness
- Verify all metrics still accessible

### **Performance:**
- Measure panel render time
- Check 3D canvas FPS
- Verify no layout thrashing

### **Cross-Module:**
- Verify push to Hydrostatics still works
- Check push to Resistance works
- Ensure consistent UX across modules

---

## Rollout Plan

### **Week 1: Foundation (Phases 1-2)**
- Dark theme + icons + headers + colors
- Low risk, high impact
- Get user feedback

### **Week 2: Layout Redesign (Phases 3-4)**
- Draggable panels + compact HUD
- Higher complexity
- Beta test with power users

### **Week 3: Enhancements (Phases 5-7)**
- Viewport improvements
- Advanced features
- Polish based on feedback

---

## Maintenance Considerations

### **Long-term:**
1. **Design system evolution:** If Hydrostatics UI changes, Hull Sizing follows
2. **Panel extensibility:** Easy to add new panels (propeller sizing, stability analysis)
3. **User preferences:** Layouts saved per user, not global
4. **Mobile-first:** Test all changes on mobile breakpoints

### **Documentation Needed:**
1. User guide: How to customize workspace layout
2. Developer guide: How to add new panels
3. Design system: Color tokens, spacing scale, component usage

---

## Estimated Total Effort

| Phase | Hours | Can Pause After? |
|-------|-------|------------------|
| Phase 1 | 2-3 | ✅ Yes - Core fixes done |
| Phase 2 | 1-2 | ✅ Yes - Colors consistent |
| Phase 3 | 3-4 | ✅ Yes - Draggable working |
| Phase 4 | 2 | ✅ Yes - Space optimized |
| Phase 5 | 2 | ✅ Yes - Enhanced visuals |
| Phase 6 | 2-3 | Optional |
| Phase 7 | 1 | Optional |
| **TOTAL** | **13-17 hours** | **Can split into 4-5 sessions** |

**Minimum viable improvement:** Phases 1-4 (8-11 hours)  
**Full redesign:** All phases (13-17 hours)

---

## Expected Outcomes

### **After Phase 1-2 (Foundation):**
- Professional appearance (no emojis, proper icons)
- Consistent with Hydrostatics
- Better theme experience

### **After Phase 3-4 (Layout Redesign):**
- Customizable workspace
- 50% more viewport space
- Faster workflow (less scrolling)

### **After Phase 5-7 (Full Polish):**
- Best-in-class hull sizing UX
- On par with commercial CAD software
- Extensible for future features

---

## Questions Before Starting

1. **Rollout strategy:**
   - a) Implement all phases in one go (13-17 hours)
   - b) Implement Phases 1-4, pause for feedback (8-11 hours)
   - c) Implement Phase 1 only, iterate based on feedback (2-3 hours)

2. **Current deployment:**
   - Should we wait for property name fix to deploy first?
   - Or start UI redesign in parallel?

3. **User testing:**
   - Want to review mockups/wireframes first?
   - Or trust the plan and proceed directly?

**My recommendation:** **1b (Phases 1-4) + 2a (wait for deployment) + 3c (trust & proceed)**

This gets the critical improvements done (~8-11 hours), lets you test with real data after deployment, and we can iterate Phases 5-7 based on your feedback.

**Ready to proceed when you are!**






