# Hull Sizing UI Redesign - Executive Summary

**Date:** November 4, 2025  
**Status:** Planning Complete - Awaiting Approval  
**Full Plan:** See `UI-REDESIGN-PHASES.md`

---

## Problem Statement

Hull Sizing module has **inconsistent UX** compared to Hydrostatics/Resistance:
- Different icons (inline SVG vs lucide-react)
- Different buttons (custom gradients vs design system)
- Different headers (custom vs AppHeader)
- Different colors (hardcoded vs semantic tokens)
- **Wasted space** (KPI panel takes 33% width)
- **Fixed layout** (can't customize like Hydrostatics)

**Result:** Feels like different application, less professional, inefficient use of space

---

## Solution: 7-Phase Redesign

### **PHASE 1: Foundation (2-3h)** ⭐ CRITICAL
- Dark theme default (industry standard)
- Theme-aware canvas/hull colors (better contrast)
- Lucide-react icons (no emojis)
- AppHeader component (consistent)

**Impact:** Professional appearance, matches Hydrostatics

---

### **PHASE 2: Color Tokens (1-2h)** ⭐ HIGH
- Replace ALL hardcoded colors
- Use semantic tokens (bg-card, text-foreground, etc.)
- Remove custom gradients

**Impact:** Proper theming, maintainable CSS

---

### **PHASE 3: Draggable Panels (3-4h)** ⭐ HIGH
- Implement react-grid-layout (same as Hydrostatics)
- 8 panels: 4 viewports + KPIs + parameters + resistance + offsets
- Layout presets: Classic Quad, 3D Focus, Designer, Analyst
- Persistent per candidate

**Impact:** Customizable workflow, matches Hydrostatics UX

---

### **PHASE 4: Compact HUD (2h)** ⭐ HIGH
- Replace massive KPI panel (1200px) with compact HUD (60px)
- All metrics available in modal (on-demand)
- Denser spacing throughout

**Impact:** 50% more viewport space, less scrolling

---

### **PHASE 5: Viewport Enhancements (2h)** ⭐ MEDIUM
- Rotation hints on 3D
- Better camera angles
- View angle presets (iso, front, side, top)

**Impact:** Better 3D visualization UX

---

### **PHASE 6: Advanced Features (2-3h)** LOW
- Auto-generate TS types from backend
- Parameter sensitivity indicators
- DXF export

**Impact:** Developer experience, advanced capabilities

---

### **PHASE 7: Polish (1h)** LOW
- Keyboard shortcuts
- Tooltips
- Loading states

**Impact:** Professional touches

---

## Space Savings Breakdown

| Change | Current | After | Gain |
|--------|---------|-------|------|
| **KPI Panel Width** | 400px (33%) | 0px (HUD instead) | +33% width |
| **KPI Panel Height** | ~1200px | 60px HUD | +1140px vertical |
| **Viewport Min Height** | 450px | 600px+ | +33% height |
| **Total Usable Space** | ~40% | ~65% | **+62% viewport area** |

---

## Before vs After Comparison

### **Current Hull Sizing Workspace:**
```
┌──────────────────────────────────────────────┐
│ Custom Header (32px) • Inline SVG • Emoji   │
├─────────────┬────────────────────┬──────────┤
│ KPI Panel   │  Viewports (4-up)  │ Params   │
│ (400px)     │  Fixed layout      │ Sliders  │
│             │  Click to maximize │          │
│ • 20 metrics│  (300px each)      │ Resist.  │
│ • Score     │                    │ Curve    │
│ • Flags     │  Can only do       │          │
│ • Export    │  1-up or 4-up      │ (400px)  │
│             │                    │          │
│ (scrolls    │                    │          │
│  ~1200px)   │                    │          │
└─────────────┴────────────────────┴──────────┘
     33%              34%              33%
```

### **After Redesign:**
```
┌──────────────────────────────────────────────────┐
│ AppHeader (56px) • Lucide Icons • No Emojis      │
├──────────────────────────────────────────────────┤
│ Compact HUD: #2•Fast Container•87.3 ⚠️2 [Metrics]│
├──────────────────────────────────────────────────┤
│  ┌──────────┬──────────┬──────────┬──────────┐  │
│  │ Plan     │ Profile  │ Sections │ 3D       │  │
│  │ (3×5)    │ (3×5)    │ (3×5)    │ (3×5)    │  │
│  │          │          │          │          │  │  Draggable
│  │ 450px+   │ Resize → │ ← Drag   │          │  │  Grid
│  │          │          │          │          │  │  Layout
│  ├──────────┴──────────┴──────────┼──────────┤  │
│  │ Parameters (6×3)                │Resistance│  │
│  │ • Lpp: [====|====] 52.3m       │ (6×3)    │  │
│  │ • Beam: [===|=====] 10.2m      │  Chart   │  │
│  └──────────────────────────────────┴──────────┘  │
│                                                    │
└────────────────────────────────────────────────────┘
        Full width • User customizable • Presets
```

**Key Improvements:**
- ✅ 62% more viewport area
- ✅ Customizable layout
- ✅ Consistent with Hydrostatics
- ✅ Professional appearance
- ✅ All info still accessible

---

## Effort Breakdown

**Minimum (Phases 1-4):** 8-11 hours
- Gets core consistency + space optimization
- Usable, professional result

**Recommended (Phases 1-5):** 10-13 hours
- Adds viewport enhancements
- Polished user experience

**Complete (All phases):** 13-17 hours
- All features, fully polished
- Production-ready

---

## Next Steps

1. **Review this plan** - Any concerns or adjustments?
2. **Approve approach** - Proceed with Phases 1-4? Or all phases?
3. **Wait for deployment** - Let property fix deploy first (currently running)
4. **Begin implementation** - Start with Phase 1 (2-3 hours)

---

## Additional Considerations

### **User Onboarding:**
After redesign, users will need guidance:
- Add "What's New" modal on first visit
- Show layout preset selector on first workspace open
- Quick tutorial: "Drag panels to customize"

### **Documentation:**
- Update user guide with new workspace screenshots
- Add keyboard shortcuts reference
- Document layout presets

### **Analytics:**
Track usage to validate improvements:
- Which layout presets are most popular?
- How often do users customize layouts?
- Does compact HUD improve task completion time?

---

## Summary

**In one sentence:** Make Hull Sizing look, feel, and work exactly like Hydrostatics while maximizing viewport space for the visualizations that matter most.

**Bottom line:** 8-11 hours of focused work delivers a professional, consistent, space-efficient Hull Sizing module that matches the quality of the rest of the application.

**Ready to start!** 🚀








