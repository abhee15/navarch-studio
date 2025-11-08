# Hull Sizing UI Redesign - Quick Reference

**TL;DR:** Make Hull Sizing consistent with Hydrostatics, remove wasted space, add draggable panels

---

## What Changes

### ✅ **KEEP (Already Good)**
- Quad viewport concept (plan, profile, sections, 3D)
- Parameter sliders (interactive adjustment)
- Resistance curve visualization
- Overall page flow (mission → results → workspace)

### ⚠️ **FIX (Inconsistent)**
- Headers → Use AppHeader
- Icons → Use lucide-react
- Buttons → Use Button component
- Colors → Use semantic tokens
- Layout → Make draggable (like Hydrostatics)

### ❌ **REMOVE (Wasted Space)**
- KPI panel sidebar (1200px tall, 400px wide)
- Emojis in UI chrome
- Custom button gradients
- Excessive padding

### ➕ **ADD (Missing Features)**
- Compact HUD (60px, shows key info)
- Draggable/resizable panels
- Layout presets
- KPI detail modal (on-demand)
- Theme-aware colors

---

## Icon Mapping

| Old | New | Component |
|-----|-----|-----------|
| 🚀 | `<Rocket />` | New Mission button |
| 📊 | `<BarChart3 />` | KPIs tab |
| 📐 | `<Ruler />` | Offsets tab |
| ⚡ | `<Zap />` | Quick stats |
| ⚠️ | `<AlertTriangle />` | Warnings |
| ℹ️ | `<Info />` | Info button |
| Inline Home SVG | `<Home />` | Home button |
| Inline Plus SVG | `<Plus />` | Create actions |
| Inline Trash SVG | `<Trash2 />` | Delete actions |

---

## Color Token Replacements

| Old (Hardcoded) | New (Semantic) |
|-----------------|----------------|
| `bg-white` + `dark:bg-gray-800` | `bg-card` |
| `text-gray-900` + `dark:text-white` | `text-foreground` |
| `text-gray-600` + `dark:text-gray-400` | `text-muted-foreground` |
| `border-gray-200` + `dark:border-gray-700` | `border-border` |
| `bg-blue-600` (buttons) | `bg-primary` |
| `bg-slate-50` (canvas) | `bg-gray-100 dark:bg-gray-900` |

---

## Layout Changes

### **Before (Fixed 3-Column):**
```
| KPI Panel | Viewports | Params+Resist |
|  (400px)  |  (auto)   |    (400px)    |
|   33%     |    34%    |      33%      |
```

### **After (Draggable Grid):**
```
| HUD (60px fixed height)                    |
|--------------------------------------------|
| 12-column grid, user customizable          |
| - Panels can be anywhere                   |
| - Resize any panel                         |
| - Collapse to header                       |
| - Fullscreen any panel                     |
| - Saved per candidate                      |
```

---

## Workspace Presets

### **1. Classic Quad** (Default)
```
┌──────┬──────┬──────┐
│ Plan │Profile│ KPIs │
│ (4×4)│ (4×4) │(4×8) │
├──────┼──────┼──────┤
│Sectn │ 3D   │Params│
│(4×4) │(4×4) │(4×4) │
│      │      ├──────┤
│      │      │Resist│
└──────┴──────┴──────┘
```

### **2. 3D Focus**
```
┌──────┬────────────┐
│ Plan │    3D      │
│(3×4) │   (9×8)    │
├──────┤            │
│Profil│            │
│(3×4) │            │
└──────┴────────────┘
```

### **3. Designer** (CAD-like)
```
┌────────────┬──────┐
│    Plan    │Params│
│   (8×6)    │(4×6) │
├────────────┤      │
│  Profile   │      │
│   (8×6)    │      │
└────────────┴──────┘
```

### **4. Analyst** (Metrics-focused)
```
┌──────┬──────────────┐
│ KPIs │  Resistance  │
│(4×8) │   (8×4)      │
├──────┤              │
│Params│              │
│(4×4) ├──────────────┤
│      │  3D Preview  │
└──────┴──────────────┘
```

---

## KPI Panel Transformation

### **Before:**
- Left sidebar (400px × 1200px)
- Always visible
- 20+ metrics in 5 categories
- Redundant info (dimensions shown in sliders)
- Takes 33% of horizontal space

### **After:**
- Compact HUD (full width × 60px)
- Shows critical info: rank, family, score, warnings
- "Show All Metrics" button → modal
- Dimensions removed (shown in parameter sliders)
- **Saves 1140px vertical + 400px horizontal**

---

## Implementation Phases (Priority Order)

| Phase | Hours | Priority | Description |
|-------|-------|----------|-------------|
| **1** | 2-3 | ⭐⭐⭐ CRITICAL | Dark theme + icons + headers |
| **2** | 1-2 | ⭐⭐⭐ HIGH | Semantic color tokens |
| **3** | 3-4 | ⭐⭐⭐ HIGH | Draggable panel layout |
| **4** | 2 | ⭐⭐⭐ HIGH | Compact HUD + density |
| **5** | 2 | ⭐⭐ MEDIUM | Viewport enhancements |
| **6** | 2-3 | ⭐ LOW | Advanced features |
| **7** | 1 | ⭐ LOW | Polish & shortcuts |

**Recommended MVP:** Phases 1-4 (8-11 hours)  
**Complete redesign:** All phases (13-17 hours)

---

## Expected Results

### **Space Efficiency:**
- **Before:** ~40% of screen used for viewports
- **After:** ~65% of screen used for viewports
- **Gain:** +62% viewport area

### **Consistency:**
- **Before:** Unique UI (different from other modules)
- **After:** Matches Hydrostatics/Resistance exactly

### **Customization:**
- **Before:** Fixed layout, no options
- **After:** 4+ presets, fully customizable, persistent

### **Professional:**
- **Before:** Emojis, custom colors, inline SVG
- **After:** Clean icons, design system, theme-aware

---

## Files Modified Summary

**Pages:** 4 files
- MissionCasesList.tsx
- SizingRunResults.tsx
- CandidateWorkspace.tsx
- MissionWizard.tsx

**Components:** ~20 files
- All workspace panels
- CandidateCard
- ComparisonView
- Wizard steps
- Visualization components

**New Files:** ~10 files
- 8 panel components
- useSizingWorkspaceLayout hook
- KPIDetailModal

**Total LOC Changed:** ~800-1200 lines

---

## Risk Assessment

### **LOW RISK:**
- Phases 1-2 (icons, headers, colors) - visual only
- Phase 4 (HUD) - additive, can keep old for fallback
- Phase 5-7 - optional enhancements

### **MEDIUM RISK:**
- Phase 3 (draggable layout) - core UX change
  - **Mitigation:** Provide "Classic Quad" preset that matches current
  - **Mitigation:** Thoroughly test before deployment

### **OVERALL:** LOW-MEDIUM (proven pattern from Hydrostatics)

---

## Success Metrics

**Qualitative:**
- [ ] Looks identical to Hydrostatics (styling)
- [ ] Feels identical to Hydrostatics (interactions)
- [ ] No user confusion between modules

**Quantitative:**
- [ ] 50%+ more viewport space
- [ ] 0 emojis in UI chrome
- [ ] 100% color tokens (no hardcoded)
- [ ] 4+ layout presets available
- [ ] <100ms panel drag response time

**User Feedback:**
- [ ] "Feels professional now"
- [ ] "Much easier to see details"
- [ ] "Love the customizable layout"

---

## Recommendation

**Start with Phases 1-4** (8-11 hours):
1. Gets critical consistency fixes
2. Delivers major space savings
3. Provides draggable layout
4. Can pause for user feedback

**Then iterate Phases 5-7** based on feedback (4-6 hours)

**Total investment:** 12-17 hours over 2-3 sessions  
**ROI:** Professional, consistent UI that users love

---

## Decision Point

**Questions:**
1. Proceed with Phases 1-4 now? (8-11h commitment)
2. Wait for current deployment first? (property fix deploying)
3. Want to see wireframes/mockups first?

**I recommend:** 
- ✅ Wait for current deployment (~10 min)
- ✅ Test property fix with fresh data
- ✅ Then start Phase 1 (easy, low risk)
- ✅ Get your feedback before Phase 3 (draggable layout)

**Your call!** 🎯




