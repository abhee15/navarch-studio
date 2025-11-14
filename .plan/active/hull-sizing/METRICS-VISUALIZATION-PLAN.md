# Hull Sizing Metrics Visualization - Enhanced Representation

**Problem:** Current KPI panel shows 20+ metrics in boring vertical list, takes huge space  
**Solution:** Visual, color-coded metric cards like Hydrostatics + smart grouping + visualization

---

## Current vs Proposed

### **CURRENT (Bad):**
```
┌─────────────────┐
│ Principal Dims  │
├─────────────────┤
│ Lpp: 52.3 m     │
│ Beam: 10.2 m    │
│ Draft: 5.1 m    │
│ Depth: 7.2 m    │
│ LOA: 54.1 m     │
├─────────────────┤
│Form Coefficients│
├─────────────────┤
│ Cb: 0.623       │
│ Cp: 0.745       │
│ Cwp: 0.812      │
│ Cm: 0.915       │
└─────────────────┘
```
- Boring text list
- No visual hierarchy
- Hard to scan quickly
- Takes 1200px+ vertical space

### **PROPOSED (Good):**
```
┌──────────────────────────────────────────────┐
│ PRINCIPAL DIMENSIONS                         │
├──────┬──────┬──────┬──────┬──────┬──────────┤
│ Lpp  │ Lwl  │ LOA  │ Beam │Draft │  Depth   │
│ 52.3 │ 51.8 │ 54.1 │ 10.2 │ 5.1  │   7.2    │
│  m   │  m   │  m   │  m   │  m   │    m     │
└──────┴──────┴──────┴──────┴──────┴──────────┘

┌──────────────────────────────────────────────┐
│ FORM COEFFICIENTS (Visual Representation)    │
├──────┬──────┬──────┬──────┐                  │
│ Cb   │ Cp   │ Cwp  │ Cm   │  ┌───────────┐  │
│0.623 │0.745 │0.812 │0.915 │  │█████░░░░░ │  │
│ ███  │ ████ │█████ │██████│  │  0.623    │  │
└──────┴──────┴──────┴──────┘  └───────────┘  │
     ↑ Visual bars show relative values
     
┌──────────────────────────────────────────────┐
│ DIMENSIONAL RATIOS (With Guidelines)         │
├──────┬──────┬──────┬──────────────────┐     │
│ L/B  │ B/T  │ D/T  │  Lwl/λ           │     │
│ 5.12 │ 2.00 │ 1.41 │   1.23           │     │
│ ✓OK  │ ✓OK  │ ⚠️    │   ✓OK            │     │
└──────┴──────┴──────┴──────────────────┘     │
     ↑ Indicators show if ratios are typical
```

**Benefits:**
- Visual hierarchy (color-coded cards)
- Horizontal compact layout (less vertical space)
- Status indicators (✓/⚠️/❌)
- Visual bars for coefficients
- Fits in ~300px vs current 1200px

---

## Detailed Metric Representation Design

### **1. Principal Dimensions - Compact Card Grid**

**Component:** `PrincipalDimensionsCard.tsx`

```tsx
export function PrincipalDimensionsCard({ candidate }: { candidate: CandidateDesign }) {
  const dimensions = [
    { label: "Lpp", value: candidate.lppM, color: "blue", icon: <Ruler /> },
    { label: "Lwl", value: candidate.lwlM, color: "blue", icon: <Waves /> },
    { label: "LOA", value: candidate.loaM, color: "blue", icon: <ArrowLeftRight /> },
    { label: "Beam", value: candidate.beamM, color: "cyan", icon: <MoveHorizontal /> },
    { label: "Draft", value: candidate.draftM, color: "indigo", icon: <MoveVertical /> },
    { label: "Depth", value: candidate.depthM, color: "purple", icon: <MoveVertical /> },
  ];

  return (
    <div className="bg-card border border-border rounded-lg p-4">
      <h3 className="text-sm font-semibold text-foreground mb-3 flex items-center gap-2">
        <Ruler className="h-4 w-4 text-primary" />
        Principal Dimensions
      </h3>
      
      {/* Desktop: 6-column grid */}
      <div className="hidden lg:grid grid-cols-6 gap-3">
        {dimensions.map(dim => (
          <div key={dim.label} className={`bg-${dim.color}-50 dark:bg-${dim.color}-900/20 rounded-lg p-3 border border-${dim.color}-200 dark:border-${dim.color}-800`}>
            <div className="flex items-center justify-between mb-1">
              <span className="text-xs font-medium text-muted-foreground">{dim.label}</span>
              {dim.icon && <span className={`text-${dim.color}-600 dark:text-${dim.color}-400`}>{dim.icon}</span>}
            </div>
            <div className={`text-2xl font-bold text-${dim.color}-600 dark:text-${dim.color}-400`}>
              {dim.value?.toFixed(1)}
            </div>
            <div className="text-[10px] text-muted-foreground">m</div>
          </div>
        ))}
      </div>
      
      {/* Tablet: 3-column grid */}
      <div className="hidden md:grid lg:hidden grid-cols-3 gap-2">
        {/* Same cards, smaller */}
      </div>
      
      {/* Mobile: 2-column grid, smaller cards */}
      <div className="grid md:hidden grid-cols-2 gap-2">
        {/* Compact version */}
      </div>
    </div>
  );
}
```

**Visual Style:** Colored cards like Hydrostatics KPIs  
**Space:** ~120px height (vs current ~200px)

---

### **2. Form Coefficients - Visual Bars + Numbers**

**Component:** `FormCoefficientsCard.tsx`

```tsx
export function FormCoefficientsCard({ candidate }: { candidate: CandidateDesign }) {
  const coefficients = [
    { 
      label: "Cb", 
      name: "Block", 
      value: candidate.cb,
      range: [0.4, 0.85],  // Typical range for ships
      optimal: [0.55, 0.75],  // Sweet spot
      color: "blue"
    },
    { 
      label: "Cp", 
      name: "Prismatic", 
      value: candidate.cp,
      range: [0.55, 0.85],
      optimal: [0.65, 0.78],
      color: "purple"
    },
    { 
      label: "Cwp", 
      name: "Waterplane", 
      value: candidate.cwp,
      range: [0.65, 0.95],
      optimal: [0.75, 0.88],
      color: "cyan"
    },
    { 
      label: "Cm", 
      name: "Midship", 
      value: candidate.cm,
      range: [0.7, 1.0],
      optimal: [0.85, 0.98],
      color: "indigo"
    },
  ];

  return (
    <div className="bg-card border border-border rounded-lg p-4">
      <h3 className="text-sm font-semibold text-foreground mb-3 flex items-center gap-2">
        <Box className="h-4 w-4 text-primary" />
        Form Coefficients
      </h3>
      
      <div className="space-y-3">
        {coefficients.map(coef => {
          const percentage = ((coef.value - coef.range[0]) / (coef.range[1] - coef.range[0])) * 100;
          const isOptimal = coef.value >= coef.optimal[0] && coef.value <= coef.optimal[1];
          
          return (
            <div key={coef.label}>
              {/* Label + Value */}
              <div className="flex items-center justify-between mb-1">
                <span className="text-xs font-medium text-muted-foreground">
                  {coef.label} <span className="text-[10px]">({coef.name})</span>
                </span>
                <span className={`text-lg font-bold ${isOptimal ? 'text-green-600 dark:text-green-400' : `text-${coef.color}-600 dark:text-${coef.color}-400`}`}>
                  {coef.value.toFixed(3)}
                  {isOptimal && <span className="ml-1 text-xs">✓</span>}
                </span>
              </div>
              
              {/* Visual Bar */}
              <div className="relative h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
                {/* Optimal range background */}
                <div 
                  className="absolute h-full bg-green-200 dark:bg-green-900/30"
                  style={{
                    left: `${((coef.optimal[0] - coef.range[0]) / (coef.range[1] - coef.range[0])) * 100}%`,
                    width: `${((coef.optimal[1] - coef.optimal[0]) / (coef.range[1] - coef.range[0])) * 100}%`
                  }}
                />
                {/* Actual value */}
                <div 
                  className={`absolute h-full bg-${coef.color}-500 rounded-r-full transition-all`}
                  style={{ width: `${percentage}%` }}
                />
              </div>
              
              {/* Range labels */}
              <div className="flex justify-between mt-0.5 text-[9px] text-muted-foreground">
                <span>{coef.range[0]}</span>
                <span>{coef.range[1]}</span>
              </div>
            </div>
          );
        })}
      </div>
      
      {/* Legend */}
      <div className="mt-3 pt-3 border-t border-border text-[10px] text-muted-foreground flex items-center gap-2">
        <div className="flex items-center gap-1">
          <div className="w-3 h-2 bg-green-200 dark:bg-green-900/30 rounded"></div>
          <span>Optimal range</span>
        </div>
      </div>
    </div>
  );
}
```

**Visual Features:**
- Horizontal bars showing value relative to typical range
- Green background showing optimal range
- ✓ indicator if value is in optimal range
- Compact: ~180px height

---

### **3. Dimensional Ratios - Status Indicators**

**Component:** `DimensionalRatiosCard.tsx`

```tsx
export function DimensionalRatiosCard({ candidate }: { candidate: CandidateDesign }) {
  const lOverB = candidate.lppM / candidate.beamM;
  const bOverT = candidate.beamM / candidate.draftM;
  const dOverT = candidate.depthM / candidate.draftM;
  const lwlOverLambda = candidate.lwlOverLambda || 0;
  
  // Typical ranges by vessel type
  const typicalRanges = {
    container: { lOverB: [6, 8], bOverT: [2.5, 3.5], dOverT: [1.2, 1.5] },
    tanker: { lOverB: [5, 7], bOverT: [2.0, 2.8], dOverT: [1.15, 1.35] },
    bulker: { lOverB: [5.5, 7.5], bOverT: [2.2, 3.0], dOverT: [1.2, 1.4] },
  };
  
  // Determine vessel type from hull family
  const vesselType = candidate.hullFamily.includes("container") ? "container" 
    : candidate.hullFamily.includes("tanker") ? "tanker" : "bulker";
  const ranges = typicalRanges[vesselType];
  
  const checkRatio = (value: number, range: [number, number]) => {
    if (value >= range[0] && value <= range[1]) return "optimal";
    if (value > range[1] * 1.1 || value < range[0] * 0.9) return "warning";
    return "acceptable";
  };
  
  const ratios = [
    {
      label: "L/B",
      name: "Slenderness",
      value: lOverB,
      range: ranges.lOverB,
      status: checkRatio(lOverB, ranges.lOverB),
      description: "Length to beam ratio",
    },
    {
      label: "B/T",
      name: "Stiffness",
      value: bOverT,
      range: ranges.bOverT,
      status: checkRatio(bOverT, ranges.bOverT),
      description: "Beam to draft ratio (initial stability)",
    },
    {
      label: "D/T",
      name: "Freeboard",
      value: dOverT,
      range: ranges.dOverT,
      status: checkRatio(dOverT, ranges.dOverT),
      description: "Depth to draft ratio",
    },
    {
      label: "Lwl/λ",
      name: "Speed-Length",
      value: lwlOverLambda,
      range: [1.0, 2.0],
      status: lwlOverLambda >= 1.0 && lwlOverLambda <= 2.0 ? "optimal" : "acceptable",
      description: "Waterline length to wave length",
    },
  ];
  
  const statusColors = {
    optimal: { bg: "bg-green-50 dark:bg-green-900/20", text: "text-green-600 dark:text-green-400", border: "border-green-200 dark:border-green-800", icon: "✓" },
    acceptable: { bg: "bg-yellow-50 dark:bg-yellow-900/20", text: "text-yellow-600 dark:text-yellow-400", border: "border-yellow-200 dark:border-yellow-800", icon: "~" },
    warning: { bg: "bg-red-50 dark:bg-red-900/20", text: "text-red-600 dark:text-red-400", border: "border-red-200 dark:border-red-800", icon: "!" },
  };

  return (
    <div className="bg-card border border-border rounded-lg p-4">
      <h3 className="text-sm font-semibold text-foreground mb-3 flex items-center gap-2">
        <Sigma className="h-4 w-4 text-primary" />
        Dimensional Ratios
        <span className="ml-auto text-[10px] text-muted-foreground">
          For {vesselType} hull
        </span>
      </h3>
      
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
        {ratios.map(ratio => {
          const style = statusColors[ratio.status];
          return (
            <div 
              key={ratio.label}
              className={`${style.bg} border ${style.border} rounded-lg p-3 relative group`}
              title={`${ratio.description}\nTypical range: ${ratio.range[0]}-${ratio.range[1]}`}
            >
              {/* Status icon */}
              <div className={`absolute top-2 right-2 w-5 h-5 rounded-full ${style.bg} ${style.text} flex items-center justify-center text-xs font-bold border ${style.border}`}>
                {style.icon}
              </div>
              
              <div className="text-xs font-medium text-muted-foreground mb-1">
                {ratio.label}
              </div>
              <div className={`text-2xl font-bold ${style.text}`}>
                {ratio.value.toFixed(2)}
              </div>
              <div className="text-[10px] text-muted-foreground mt-1">
                {ratio.name}
              </div>
              
              {/* Tooltip on hover (desktop) */}
              <div className="hidden group-hover:block absolute bottom-full left-1/2 -translate-x-1/2 mb-2 px-2 py-1 bg-gray-900 dark:bg-gray-100 text-white dark:text-gray-900 text-[10px] rounded whitespace-nowrap pointer-events-none z-10">
                Typical: {ratio.range[0]}-{ratio.range[1]}
                <div className="absolute top-full left-1/2 -translate-x-1/2 border-4 border-transparent border-t-gray-900 dark:border-t-gray-100"></div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
```

**Features:**
- ✓/~/! status indicators
- Hover tooltips with typical ranges
- Color-coded by status (green=good, yellow=borderline, red=bad)
- Responsive grid (4-col desktop, 2-col mobile)
- Height: ~140px

---

### **4. Performance Metrics - Big Number Cards**

**Component:** `PerformanceMetricsCard.tsx`

```tsx
export function PerformanceMetricsCard({ candidate }: { candidate: CandidateDesign }) {
  const metrics = [
    {
      label: "Displacement",
      value: candidate.dispT,
      unit: "tonnes",
      color: "blue",
      icon: <Weight />,
      format: (v) => v.toFixed(0),
      change: null,  // Could show vs mission target
    },
    {
      label: "Froude Number",
      value: candidate.fn,
      unit: "",
      color: "purple",
      icon: <Gauge />,
      format: (v) => v.toFixed(3),
      description: "Speed-length ratio",
    },
    {
      label: "EHP",
      value: candidate.ehpKw,
      unit: "kW",
      color: "green",
      icon: <Zap />,
      format: (v) => v?.toFixed(0) || "N/A",
      description: "Effective power",
    },
    {
      label: "SHP (est.)",
      value: candidate.shpKw,
      unit: "kW",
      color: "orange",
      icon: <Flame />,
      format: (v) => v?.toFixed(0) || "N/A",
      description: "Shaft power (η≈0.65)",
    },
  ];

  return (
    <div className="bg-card border border-border rounded-lg p-4">
      <h3 className="text-sm font-semibold text-foreground mb-3 flex items-center gap-2">
        <TrendingUp className="h-4 w-4 text-primary" />
        Performance
      </h3>
      
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
        {metrics.map(metric => (
          <div 
            key={metric.label}
            className={`bg-${metric.color}-50 dark:bg-${metric.color}-900/20 border border-${metric.color}-200 dark:border-${metric.color}-800 rounded-lg p-3 group hover:shadow-md transition-shadow`}
          >
            <div className="flex items-start justify-between mb-2">
              <span className="text-xs font-medium text-muted-foreground">{metric.label}</span>
              <span className={`text-${metric.color}-600 dark:text-${metric.color}-400`}>
                {metric.icon}
              </span>
            </div>
            
            <div className={`text-2xl font-bold text-${metric.color}-600 dark:text-${metric.color}-400 tabular-nums`}>
              {metric.format(metric.value)}
            </div>
            
            <div className="text-[10px] text-muted-foreground mt-1">
              {metric.unit}
            </div>
            
            {/* Description tooltip */}
            {metric.description && (
              <div className="hidden group-hover:block absolute bottom-full left-0 mb-2 px-2 py-1 bg-gray-900 dark:bg-gray-100 text-white dark:text-gray-900 text-[10px] rounded whitespace-nowrap z-10">
                {metric.description}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
```

**Features:**
- Big, readable numbers
- Color-coded by metric type
- Icons for quick recognition
- Hover tooltips for explanations
- Height: ~140px

---

### **5. Visual Comparison Chart - Interactive Radar**

**Component:** `MetricsRadarChart.tsx` (NEW)

Shows all coefficients in radar/spider chart:

```tsx
import { RadarChart, Radar, PolarGrid, PolarAngleAxis } from "recharts";

export function MetricsRadarChart({ candidate }: { candidate: CandidateDesign }) {
  const data = [
    { metric: "Cb", value: candidate.cb, fullMark: 1.0 },
    { metric: "Cp", value: candidate.cp, fullMark: 1.0 },
    { metric: "Cwp", value: candidate.cwp, fullMark: 1.0 },
    { metric: "Cm", value: candidate.cm || 0, fullMark: 1.0 },
    { metric: "L/B÷10", value: (candidate.lppM / candidate.beamM) / 10, fullMark: 1.0 },
    { metric: "B/T÷3", value: (candidate.beamM / candidate.draftM) / 3, fullMark: 1.0 },
  ];

  return (
    <div className="bg-card border border-border rounded-lg p-4">
      <h3 className="text-sm font-semibold text-foreground mb-2">Form Profile</h3>
      
      <RadarChart width={280} height={280} data={data}>
        <PolarGrid stroke="hsl(var(--border))" />
        <PolarAngleAxis 
          dataKey="metric" 
          tick={{ fill: "hsl(var(--muted-foreground))", fontSize: 11 }}
        />
        <Radar
          dataKey="value"
          stroke="hsl(var(--primary))"
          fill="hsl(var(--primary))"
          fillOpacity={0.3}
        />
      </RadarChart>
      
      <p className="text-[10px] text-muted-foreground text-center mt-2">
        Visual "fingerprint" of hull form
      </p>
    </div>
  );
}
```

**Use case:** Quick visual comparison between candidates  
**Height:** ~320px  
**Benefit:** Instantly see hull "shape" - different families have different radar patterns

---

## Proposed New Metrics Layout

### **Compact HUD (Top Bar - 60px):**
```
┌─────────────────────────────────────────────────────────┐
│ #2 • Fast Container • Score: 87.3 • ⚠️ 2 | [All Metrics]│
└─────────────────────────────────────────────────────────┘
```

### **Metrics Panel (Draggable, Collapsible - ~400px):**

```
┌──────────────────────────────────────────────┐
│ 📏 PRINCIPAL DIMENSIONS              [−]     │
├──────┬──────┬──────┬──────┬──────┬──────────┤
│ Lpp  │ Lwl  │ LOA  │ Beam │Draft │  Depth   │
│ 52.3 │ 51.8 │ 54.1 │ 10.2 │ 5.1  │   7.2    │
│  m   │  m   │  m   │  m   │  m   │    m     │
└──────┴──────┴──────┴──────┴──────┴──────────┘

┌──────────────────────────────────────────────┐
│ 📦 FORM COEFFICIENTS                 [−]     │
├──────────────────────────────────────────────┤
│ Cb (Block)      0.623 ✓                      │
│ ████████████████░░░░  [0.4───0.85]           │
│                                              │
│ Cp (Prismatic)  0.745 ✓                      │
│ ██████████████████░░  [0.55──0.85]           │
│                                              │
│ Cwp (Waterplane) 0.812 ✓                     │
│ ███████████████████░  [0.65──0.95]           │
└──────────────────────────────────────────────┘

┌──────────────────────────────────────────────┐
│ Σ DIMENSIONAL RATIOS                 [−]     │
├───────┬───────┬───────┬──────────────────────┤
│  L/B  │  B/T  │  D/T  │     Lwl/λ            │
│ 5.12  │ 2.00  │ 1.41  │     1.23             │
│  ✓    │  ✓    │  ⚠️    │      ✓               │
│  OK   │  OK   │ Low   │     OK               │
└───────┴───────┴───────┴──────────────────────┘

┌──────────────────────────────────────────────┐
│ ⚡ PERFORMANCE                        [−]     │
├───────┬───────┬───────┬──────────────────────┤
│ Disp  │  Fn   │  EHP  │     SHP              │
│ 4,523 │ 0.235 │ 1,245 │    1,915             │
│   t   │   -   │  kW   │      kW              │
└───────┴───────┴───────┴──────────────────────┘

┌──────────────────────────────────────────────┐
│ 🎯 FORM PROFILE (Radar)              [−]     │
│        Cb                                    │
│         /\                                   │
│        /  \                                  │
│  L/B  |    | Cp                              │
│       |    |                                 │
│  B/T  \    / Cwp                             │
│        \  /                                  │
│         \/                                   │
│        Cm                                    │
└──────────────────────────────────────────────┘
```

**Total Height:** ~550px (vs current 1200px)  
**Visual Hierarchy:** Clear, scannable, informative  
**Responsive:** Grids collapse on mobile

---

## Alternative: Metrics Overlay on Viewports

**Concept:** Show key metrics AS OVERLAY on the viewports themselves

```
┌────────────────────────────────┐
│ Plan View                      │
│                                │
│  ┌──────────────────┐          │
│  │ Lpp: 52.3m       │  ← Overlay
│  │ Beam: 10.2m      │
│  │ Cb: 0.623        │
│  └──────────────────┘          │
│                                │
└────────────────────────────────┘
```

**Pros:**
- No separate panel needed
- Info where user is looking
- Maximum space efficiency

**Cons:**
- Can obscure viewport
- Less detailed than dedicated panel

---

## Updated Plan: Add New Phase for Metrics

### **PHASE 4B: Enhanced Metrics Visualization (add to existing plan)**

**Files to create:**
1. `components/sizing/workspace/panels/PrincipalDimensionsCard.tsx`
2. `components/sizing/workspace/panels/FormCoefficientsCard.tsx`
3. `components/sizing/workspace/panels/DimensionalRatiosCard.tsx`
4. `components/sizing/workspace/panels/PerformanceMetricsCard.tsx`
5. `components/sizing/workspace/panels/MetricsRadarChart.tsx` (optional)

**Integration:**
- These become draggable panels in grid layout
- User can show/hide each
- Default: Show dimensions + coefficients, hide others
- Mobile: Stack, show only critical (dimensions + performance)

---

## Metric Priority Classification

### **CRITICAL (Always Visible):**
1. ✅ Score & Rank (HUD)
2. ✅ Principal Dimensions (6 values)
3. ✅ Constraint Warnings (HUD if present)

### **IMPORTANT (Collapsible Panel):**
4. ⭐ Form Coefficients (4 values) - with visual bars
5. ⭐ Dimensional Ratios (4 values) - with status indicators
6. ⭐ Performance (4 values) - displacement, Fn, EHP, SHP

### **NICE TO HAVE (Modal/Optional Panel):**
7. 🔍 Stability estimates (KB, LCB, GM)
8. 🔍 Detailed ratios (L/D, T/D, etc.)
9. 🔍 Radar chart (visual comparison)

---

## Commit the Overflow Fix First

Let me commit the immediate fix, then update the comprehensive plan:

**Current deployment:** Overflow fix is pushing now (commit `119baeb`)

**After this deploys (~10 min):**
- ✅ Legends won't be clipped
- ✅ You can see collapsible legend buttons fully

**Then we proceed with full UI redesign** including better metric visualization!

---

## Summary of Plan Updates

I'll update `UI-REDESIGN-PLAN.md` to include:

**NEW: Phase 4B - Enhanced Metrics Visualization**
- Visual coefficient bars (show relative to optimal range)
- Status indicators on ratios (✓/⚠️/❌)
- Color-coded metric cards (like Hydrostatics)
- Optional radar chart for visual comparison
- Responsive grids (6-col → 3-col → 2-col)
- Hover tooltips with explanations

**Result:**
- Metrics are MORE informative (visual + status)
- Take LESS space (550px vs 1200px)
- Easier to scan (color + icons)
- Professional appearance

**Ready to update the plan with these metric visualization improvements!**













