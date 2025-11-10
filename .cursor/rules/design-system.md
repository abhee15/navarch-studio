# NavArch Studio Design System

**Version:** 1.0  
**Last Updated:** 2025  
**Purpose:** Single source of truth for UI patterns, components, and styling guidelines

---

## Table of Contents

1. [Typography Scale](#typography-scale)
2. [Spacing System](#spacing-system)
3. [Color System](#color-system)
4. [Border & Shadows](#border--shadows)
5. [Component Patterns](#component-patterns)
6. [Layout Patterns](#layout-patterns)
7. [Interactive States](#interactive-states)
8. [Icons & Visual Elements](#icons--visual-elements)
9. [Best Practices](#best-practices)

---

## Typography Scale

### Font Sizes

Use Tailwind's text size utilities consistently across the application:

| Class | Size | Usage |
|-------|------|-------|
| `text-[10px]` | 10px | Badge text, metadata |
| `text-xs` | 12px | **Standard for inputs, labels, panel content** |
| `text-sm` | 14px | Body text, descriptions |
| `text-base` | 16px | Default body text |
| `text-lg` | 18px | Page subtitles, section headers |
| `text-xl` | 20px | Page titles |
| `text-2xl` | 24px | Main page headings |
| `text-3xl` | 30px | Large headings |

### Font Weights

| Class | Weight | Usage |
|-------|--------|-------|
| `font-normal` | 400 | Default text |
| `font-medium` | 500 | Emphasized content, values |
| `font-semibold` | 600 | Section titles in panels |
| `font-bold` | 700 | Page titles, major headings |

### Line Heights

Use Tailwind defaults:
- `leading-none` (1): Tight spacing for large headings
- `leading-tight` (1.25): Compact content
- `leading-normal` (1.5): Body text (default)
- `leading-relaxed` (1.625): Comfortable reading

### Typography Guidelines

**DO:**
- Use `text-xs` (12px) for all panel content, labels, and input fields
- Use `font-medium` to emphasize values/numbers
- Maintain consistent sizing within component groups

**DON'T:**
- Mix arbitrary sizes like `text-[11px]` with standard classes
- Use font sizes smaller than `text-[10px]` except for edge cases
- Change font sizes mid-component without semantic reason

---

## Spacing System

### Padding & Margin Scale

Use Tailwind's spacing scale (4px base unit):

| Class | Size | Usage |
|-------|------|-------|
| `p-0.5` / `m-0.5` | 2px | Minimal spacing |
| `p-1` / `m-1` | 4px | Tight spacing |
| `p-1.5` / `m-1.5` | 6px | Compact elements |
| `p-2` / `m-2` | 8px | Standard small spacing |
| `p-2.5` / `m-2.5` | 10px | Comfortable spacing |
| `p-3` / `m-3` | 12px | Medium spacing |
| `p-4` / `m-4` | 16px | **Standard card/panel padding** |
| `p-6` / `m-6` | 24px | Large spacing |
| `p-8` / `m-8` | 32px | Extra large spacing |

### Gap Utilities (Flex/Grid)

| Class | Size | Usage |
|-------|------|-------|
| `gap-0.5` | 2px | Inline elements |
| `gap-1` | 4px | Compact layouts |
| `gap-2` | 8px | Standard element spacing |
| `gap-3` | 12px | Comfortable spacing |
| `gap-4` | 16px | Section spacing |
| `gap-6` | 24px | Large section gaps |

### Section Spacing Standards

- **Between sections in forms:** `space-y-2` (8px)
- **Between form groups:** `space-y-3` (12px)
- **Between major sections:** `space-y-6` (24px)
- **Panel content:** `p-2.5` or `p-3` for collapsible sections, `p-4` for cards

---

## Color System

### Theme Colors

Defined in `frontend/src/index.css` using HSL values with CSS variables.

#### Light Mode (One Monokai Light)
```css
--background: 0 0% 98%        /* #FAFAFA - Main background */
--foreground: 220 13% 26%     /* #3B4252 - Primary text */
--card: 0 0% 100%             /* #FFFFFF - Card background */
--border: 220 13% 87%         /* #D8DEE9 - Borders */
--primary: 207 82% 66%        /* #5DADE2 - Primary blue */
--muted-foreground: 220 9% 46% /* #6C7A89 - Secondary text */
```

#### Dark Mode (One Monokai Dark)
```css
--background: 220 13% 18%     /* #282C34 - Main background */
--foreground: 218 17% 75%     /* #ABB2BF - Primary text */
--card: 219 14% 20%           /* #2C323C - Card background */
--border: 217 16% 27%         /* #3E4451 - Borders */
--primary: 207 82% 66%        /* #5DADE2 - Primary blue */
--muted-foreground: 218 11% 65% /* #8C94A3 - Secondary text */
```

### Semantic Colors

| Color | Usage |
|-------|-------|
| `text-foreground` | Primary text |
| `text-muted-foreground` | Secondary text, labels |
| `text-primary` | Links, action text |
| `text-destructive` | Error messages, warnings |
| `bg-background` | Main page background |
| `bg-card` | Panel/card backgrounds |
| `bg-accent` | Hover states, highlights |
| `bg-primary` | Primary buttons, badges |
| `border-border` | Standard borders |

### Opacity Levels

- Full opacity: Default
- `bg-card/80` or `bg-card/50`: Semi-transparent overlays
- `border-border/50`: Subtle borders
- `bg-accent/10` or `bg-accent/20`: Hover backgrounds
- `hover:opacity-80`: Icon/image hover

---

## Border & Shadows

### Border Widths

| Class | Width | Usage |
|-------|-------|-------|
| `border` | 1px | Standard borders |
| `border-2` | 2px | Emphasized borders |
| `border-4` | 4px | Accent bars (left/right) |

### Border Colors & Patterns

- **Standard border:** `border border-border`
- **Subtle border:** `border border-border/50`
- **Accent border:** `border-l-4 border-primary`
- **No border:** `border-0` or no class

### Border Radius

| Class | Radius | Usage |
|-------|--------|-------|
| `rounded` | 4px | Small elements |
| `rounded-md` | 6px | **Standard for inputs, buttons** |
| `rounded-lg` | 8px | **Standard for cards, panels** |
| `rounded-xl` | 12px | Large cards |
| `rounded-2xl` | 16px | Feature cards |
| `rounded-full` | 50% | Circular elements, avatars |

### Shadows

| Class | Elevation | Usage |
|-------|-----------|-------|
| `shadow-sm` | Light | Subtle depth |
| `shadow` | Standard | Cards, dropdowns |
| `shadow-md` | Medium | Elevated panels |
| `shadow-lg` | Large | Modals |
| `shadow-xl` | Extra large | **Modal dialogs** |

---

## Component Patterns

### Input Fields

#### Text Inputs
```tsx
<input
  type="text"
  className="w-full border border-border bg-background text-foreground rounded-md text-xs px-2 py-1 focus:outline-none focus:ring-2 focus:ring-ring"
/>
```

#### Number Inputs
```tsx
<input
  type="number"
  step="0.1"
  className="w-full border border-border bg-background text-foreground rounded-md text-xs px-2 py-1 focus:outline-none focus:ring-2 focus:ring-ring"
/>
```

#### Compact Number Inputs (in grids)
```tsx
<input
  type="number"
  className="flex-1 min-w-0 border border-border bg-background text-foreground rounded py-0.5 px-1 text-xs focus:outline-none focus:ring-2 focus:ring-ring"
/>
```

#### Labels
```tsx
<label className="block text-xs font-medium text-muted-foreground mb-0.5">
  Label Text
</label>
```

### Buttons

#### Primary Button
```tsx
<button className="inline-flex items-center px-4 py-1.5 border border-transparent text-xs font-semibold rounded shadow-sm text-primary-foreground bg-primary hover:bg-primary/90 focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed">
  Primary Action
</button>
```

#### Secondary Button
```tsx
<button className="inline-flex items-center px-3 py-1.5 border border-border text-xs font-medium rounded text-foreground bg-background hover:bg-accent/10 focus:outline-none focus:ring-2 focus:ring-ring">
  Secondary Action
</button>
```

#### Icon Button (Compact)
```tsx
<button className="p-1.5 rounded hover:bg-accent/10 text-muted-foreground hover:text-foreground">
  <svg className="h-4 w-4">...</svg>
</button>
```

### Panels & Sections

#### Card Container (Resistance Pattern)
```tsx
<div className="bg-card border border-border rounded-lg p-4">
  <h2 className="text-sm font-semibold mb-3">Section Title</h2>
  {/* Content */}
</div>
```

#### Collapsible Section (Hydrostatics Pattern)
```tsx
<CollapsibleSection title="Section Title" defaultExpanded={true}>
  {/* Content with px-2.5 py-2 applied internally */}
</CollapsibleSection>
```

**Note:** CollapsibleSection now includes subtle borders (`mx-2 my-1.5 border border-border/50 rounded-md`)

#### Modal Dialog
```tsx
<div className="inline-block align-bottom bg-card rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full sm:p-6">
  <h3 className="text-lg leading-6 font-medium text-foreground">
    Dialog Title
  </h3>
  {/* Content */}
</div>
```

### Select Dropdowns

Use the custom `Select` component:
```tsx
<Select
  value={selectedValue}
  onChange={setSelectedValue}
  options={[
    { value: "opt1", label: "Option 1" },
    { value: "opt2", label: "Option 2" },
  ]}
  className="w-full text-xs"
/>
```

### Value Display Patterns

#### Inline Label-Value (Legacy - Use Flexbox Instead)
```tsx
<div className="text-xs">
  <span className="text-muted-foreground">Label:</span>
  <span className="ml-1 font-medium text-foreground">Value</span>
</div>
```

#### Flexbox Label-Value (Recommended)
```tsx
<div className="flex justify-between items-center text-xs">
  <span className="text-muted-foreground">Label:</span>
  <span className="font-medium text-foreground">Value</span>
</div>
```

---

## Layout Patterns

### Sidebar Panels

**Hydrostatics Pattern:**
- Fixed width: `w-80` (320px)
- Border right: `border-r border-border`
- Scrollable: `overflow-y-auto`
- Padding: `px-3 py-4`

```tsx
<div className="w-80 bg-card border-r border-border overflow-y-auto flex-shrink-0 px-3 py-4">
  <CollapsibleSection title="Section 1">...</CollapsibleSection>
  <CollapsibleSection title="Section 2">...</CollapsibleSection>
</div>
```

### Content Areas (Centered)

**Resistance Pattern:**
- Centered container: `container mx-auto`
- Max width: `max-w-2xl` (672px)
- Padding: `px-4 py-6`
- Vertical spacing: `space-y-6`

```tsx
<div className="h-full overflow-auto bg-background">
  <div className="container mx-auto px-4 py-6 max-w-2xl">
    <div className="space-y-6">
      {/* Card sections */}
    </div>
  </div>
</div>
```

### Grid Layouts (React Grid Layout)

**Breakpoints:**
```tsx
const breakpoints = { lg: 1200, md: 996, sm: 768 };
const cols = { lg: 12, md: 10, sm: 6 };
```

**Row height:** `60px`  
**Compact type:** `vertical`

---

## Interactive States

### Hover

- **Buttons:** `hover:bg-primary/90` or `hover:bg-accent/10`
- **Text links:** `hover:text-primary/80`
- **Icons:** `hover:opacity-80`
- **Backgrounds:** `hover:bg-muted/50`

### Focus

Standard focus ring for keyboard navigation:
```css
focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2
```

For compact inputs (no offset):
```css
focus:outline-none focus:ring-2 focus:ring-ring
```

### Disabled

```css
disabled:opacity-50 disabled:cursor-not-allowed
```

### Active/Selected

- **Tabs/Sections:** `bg-accent/20 text-foreground border-l-4 border-primary`
- **Buttons:** `active:scale-95` (optional)

### Loading

Spinner with animation:
```tsx
<svg className="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z" />
</svg>
```

---

## Icons & Visual Elements

### Icon Sizes

| Class | Size | Usage |
|-------|------|-------|
| `h-3 w-3` | 12px | Tiny icons |
| `h-3.5 w-3.5` | 14px | Small icons, inline with text-xs |
| `h-4 w-4` | 16px | **Standard icon size** |
| `h-5 w-5` | 20px | Medium icons |
| `h-6 w-6` | 24px | Large icons |
| `h-12 w-12` | 48px | Empty state icons |
| `h-16 w-16` | 64px | Large empty states |

### SVG Stroke

- **Standard:** `strokeWidth={2}`
- **Thin:** `strokeWidth={1.5}`
- **Bold:** `strokeWidth={2.5}`

### Icon Spacing

When pairing icons with text:
- Before text: `mr-1.5` or `mr-2`
- After text: `ml-1.5` or `ml-2`

```tsx
<button>
  <svg className="h-4 w-4 mr-1.5">...</svg>
  Button Text
</button>
```

### Empty States

Center container with large icon and descriptive text:
```tsx
<div className="text-center py-12">
  <svg className="mx-auto h-12 w-12 text-muted-foreground">...</svg>
  <h3 className="mt-4 text-lg font-medium text-foreground">Title</h3>
  <p className="mt-2 text-sm text-muted-foreground">Description</p>
</div>
```

---

## Best Practices

### Consistency First

1. **Before creating new patterns**, check this document and existing components
2. **Reuse established patterns** rather than creating variations
3. **When in doubt**, match the closest similar component

### Component Creation Checklist

- [ ] Use `text-xs` for labels and content
- [ ] Apply standard focus states
- [ ] Include disabled states if interactive
- [ ] Support both light and dark modes
- [ ] Use semantic color tokens (e.g., `text-foreground`, not hardcoded colors)
- [ ] Match spacing from similar components
- [ ] Include proper accessibility attributes

### Pattern Selection Guide

**Choose Collapsible Sections when:**
- Building compact sidebars
- Need to conserve vertical space
- Want toggle functionality
- Multiple sections in tight space

**Choose Card Containers when:**
- Building centered content forms
- Need more breathing room
- Content is primary focus
- Fewer sections with more content

### Accessibility

- All interactive elements must be keyboard accessible
- Use semantic HTML (`button`, `label`, `input`) 
- Include proper ARIA attributes for custom components
- Maintain sufficient color contrast (WCAG AA minimum)
- Focus states must be clearly visible

### Performance

- Use `transition-all` sparingly (prefer specific properties)
- Add `will-change` for frequently animated elements
- Use `overflow-hidden` on containers with `rounded-*`

---

## Migration Guide

When refactoring existing components:

1. **Font sizes:** Change `text-[11px]` → `text-xs`
2. **Label-value pairs:** Change inline layout → flexbox with `justify-between`
3. **Borders:** Ensure consistent use of `border-border` token
4. **Spacing:** Align with scale (0.5, 1, 1.5, 2, 2.5, 3, 4, 6, 8)
5. **Test both themes:** Verify appearance in light and dark modes

---

## Version History

- **v1.0** (2025): Initial design system documentation
  - Established typography scale
  - Documented spacing system
  - Defined component patterns
  - Codified color system
  - Added layout patterns

---

## Questions or Additions?

This is a living document. As new patterns emerge or existing ones evolve:
1. Document the pattern here first
2. Update all existing instances to match
3. Reference this document in code reviews
4. Keep examples up-to-date with actual implementation

**When implementing new UI components, always consult this guide first.**
