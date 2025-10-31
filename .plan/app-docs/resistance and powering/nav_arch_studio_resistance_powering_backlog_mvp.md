# Epic: Resistance & Powering (Design Phase) — MVP Backlog

**Scope (MVP)**: ITTC‑57 friction (± form factor), Holtrop–Mennen (HM) total resistance & EHP vs speed, delivered/installed power with service margin, speed‑grid editor, KCS benchmark harness & CI gate, one‑click plot/data export. Works **standalone** and **linked** to Hydrostatics via a shared **Design Condition** bar.

---

## Product Goals
- Size propulsion preliminarily with traceable physics and repeatable outputs.
- Export publication‑ready plots/tables in one click.
- Enforce quality via KCS benchmark tolerances (≤3% MAE, ≤5% max by default).
- Sub‑150 ms recompute for 50 speed points on baseline hardware.

---

## Architecture (high level)
- **Core math library** (`@navarch/core-hydro`):
  - `ittc57Cf(Re)`
  - `hmTotalResistance(inputs, speedGrid, options)` → `{components, RT[], EHP[]}`
  - `powerCurves(EHP[], etaD, serviceMargin)` → `{DHP[], Pinst[]}`
  - Pure functions, SI internally; deterministic; unit‑tested.
- **Unit/phys props** (`@navarch/units`): canonical SI; conversion helpers; water properties from temp/salinity.
- **State**: `DesignConditionStore`
  - source: `linked | local`, object: `{LWL, B, T, CB, CP, CM, LCB_pct, S, rho, nu, tempC, salinity}`
  - Reset‑to‑source per field; change events for consumers.
- **UI** (`/resistance`): left rail (Speed Grid, Viscous Options, HM Advanced, Efficiencies & Margin), main pane (plots, tables, benchmark, exports).
- **Charts**: declarative JSON spec → charts (e.g., Recharts). Snapshot tests on the JSON spec.
- **Benchmark**: `benchmarks/kcs_reference.csv` + harness to compute % errors & badges.
- **Exports**: image (PNG/SVG) via headless chart render; CSV via rows aligned to speed grid.
- **CI**: test, bench, visual snapshot; merge blocked on tolerance.

---

## Definitions
**Definition of Ready (DoR)**
- Inputs/units specified; error/warning policy defined; test data listed; UI placement known.

**Definition of Done (DoD)**
- Unit tests ≥90% for core logic; story‑level acceptance Gherkin green; snapshots updated intentionally; KCS tolerances pass; accessibility labels on interactive controls; docs & examples updated.

**Validation/Warnings (global)**
- Missing/estimated fields flagged with pill.
- Re < 2×10^6 → non‑blocking warning (scale effects).
- Unit toggles cause <0.1% numeric drift; otherwise show red toast & log issue.

---

## Prioritized Stories → Tasks → Acceptance (BDD)

### R1 — ITTC‑57 Friction Line (+ optional form factor) — **Priority P1**
**Goal**: Compute `CF` across speed grid; optional `(1+k)`.

**Tasks**
1. Core: `ittc57Cf(Re)` with domain checks; `(1+k)` application.
2. Water properties: ν from temperature/salinity (library stub + table lookup).
3. UI Card: inputs (temp→ν view; k number; toggle apply form factor; units read‑only per system).
4. Table/Plot: V vs `CF`, and `CF_eff` when enabled.
5. Warnings: Re threshold; NaN/Inf guard.
6. Tests: analytical points (e.g., Re=1e6, 1e8), k=0, k=0.2.

**Gherkin**
```
Given LWL, S, ν and a speed grid
When I compute ITTC‑57
Then CF(V) is shown for each speed
And when k=0.2 and form‑factor is ON
Then CF_eff = 1.2 * CF at every speed
And when any Re < 2e6
Then a non‑blocking warning is displayed
```

### R4 — Speed Grid Editor — **Priority P1**
**Goal**: Flexible sampling.

**Tasks**
1. Modes: Range (start/end/step), Manual (editable list), Add design point.
2. CSV import/export (header: `V,Units`).
3. Internal normalization to SI.
4. Instant recompute on change; debounce 150 ms.
5. Tests: add/remove/duplicate handling; import malformed CSV → gentle error.

**Gherkin**
```
Given I add speeds via Range 0–30 kts step 2
Then 16 points are generated and results recompute
When I switch to Manual and remove 10 kts
Then dependent plots/tables update immediately
When I export the grid
Then the CSV matches the editor contents
```

### R7 — Design Condition Bar (Hydro link) — **Priority P1**
**Goal**: Shared vessel/condition + water props; **non‑blocking** overrides.

**Tasks**
1. Store: `DesignConditionStore` with `source` flag, per‑field reset.
2. Read‑only view when linked; editable when standalone.
3. Sync events from Hydrostatics; show banner: Linked/Standalone with switch.
4. Tests: switching modes preserves or replaces values as chosen.

**Gherkin**
```
Given Linked mode
When Hydrostatics condition changes
Then Resistance defaults update without erasing local overrides
And a “Reset to condition” pill appears for overridden fields
```

### R2 — Holtrop–Mennen Total Resistance & EHP — **Priority P2**
**Goal**: HM components → RT(V); EHP(V).

**Tasks**
1. Core HM: friction (from R1), residuary (Fn), appendage factor (generic v1), correlation allowance, air resistance (default CD*Awind*0.5*ρ_air*V^2), transom correction input.
2. Inputs panel: geometry/coeffs (LWL, B, T, CB, CP, CM, LCB%), S, A_transom, flags, windage area.
3. Plots: RT vs V; EHP vs V; component stacked bars at selected V.
4. Unit conversions & stability checks; NaN/Inf guards.
5. Tests: regression suite vs saved baselines; unit parity SI/Imp within 0.1%.

**Gherkin**
```
Given complete HM inputs and a speed grid
When I compute
Then RT(V) and EHP(V) are rendered without NaNs
And toggling units preserves values within 0.1%
```

### R3 — Delivered/Installed Power + Service Margin — **Priority P2**
**Goal**: DHP, P_inst with ηD and slider.

**Tasks**
1. Core: `powerCurves(EHP, etaD|{etaH,etaR,etaO}, serviceMargin)`.
2. UI: slider 0–30% (default 15%), readouts at design & trial speeds.
3. Defaults: η_H/η_R/η_O typicals; single η_D override.
4. Tests: EHP=1000 kW, η_D=0.6 → DHP=1666.7 kW; SM=15% → P_inst=1916.7 kW (±0.1 kW).

**Gherkin**
```
Given ηD=0.6 and SM=15%
When applied to EHP curve
Then DHP=EHP/0.6 and P_inst=DHP*1.15 at each speed
```

### R5 — KCS Benchmark Check — **Priority P2**
**Goal**: Automated overlay & tolerance badges; PASS gates CI.

**Tasks**
1. Preset loader: fill KCS particulars (read‑only) + reference `RT_ref(V)`.
2. Harness: run R1–R3 → compute % error per V; MAE & Max.
3. UI: overlay curve; table with Δ% and PASS/FAIL badges; tooltips w/ likely causes.
4. Report: button to export PDF/CSV of benchmark run.
5. CI Job: run harness on PR; block on tolerance fail.

**Gherkin**
```
Given KCS preset
When I Run Benchmark
Then the overlay and Δ% table show
And PASS if MAE≤3% and Max≤5%
```

### R6 — Plot & Data Export — **Priority P3**
**Goal**: Ready‑to‑drop assets.

**Tasks**
1. Export current plots as PNG & SVG (title/axes/units/legend/footnote/timestamp).
2. Export tables as CSV: V, Re, CF, CF_eff, RT, EHP, DHP, P_inst.
3. “Add to Report” queue → Report Builder (stub ok in MVP).
4. Visual snapshot tests to guard axes/legend regressions.

**Gherkin**
```
Given a computed run
When I click Export
Then files download with correct metadata and units
```

### Standalone Parity Stories (work without Hydro link)
- **R1a (P1)** ITTC‑57 friction form‑factor with local inputs.
- **R2a (P2)** HM with local geometry/coeffs.
- **R3a (P2)** Power curves with local η and margin.
- **R5a (P2)** KCS Benchmark in Standalone preset.
- **R7a (P1)** Mode switch Linked↔Standalone with keep/replace behavior.

---

## Sprint Plan (2 × 2 weeks)
**Sprint 1** (P1 focus)
- R1 ITTC‑57 (+k)
- R4 Speed Grid Editor
- R7 Design Condition bar (read from Hydrostatics; minimal sync)
- Plot scaffolding + CSV export (subset)

**Sprint 2** (P2/P3 focus)
- R2 Holtrop–Mennen + EHP
- R3 Delivered Power + service margin
- R5 KCS benchmark + CI gates
- R6 Full plot export (PNG/SVG) + Report queue

---

## Story Points (Fibonacci, estimate)
- R1: 5 (core math + UI + warnings)
- R4: 5 (editor + CSV io + normalization)
- R7: 3 (store + sync + UX)
- R2: 8 (HM breadth + charts)
- R3: 3 (simple math + slider + readouts)
- R5: 5 (harness + CI + UI)
- R6: 3 (export + snapshots)

Velocity target ~16–18 points/sprint → feasible in 2 sprints.

---

## Test Strategy (how to test each piece on its own)
**Unit Tests (core)**
- `ittc57Cf` numeric checks at canonical Re (1e5…1e9); derivative monotonicity.
- Form‑factor multiply correctness.
- HM components isolation tests (friction matches R1; air resistance scales ~V²).
- Power curve algebra invariants.
- Unit conversion round‑trip (<0.1% drift).

**Golden‑Dataset Tests**
- Save JSON for a fixed input set; compare RT/EHP arrays within tolerance.

**Visual Regression Tests**
- Snapshot chart spec JSON; assert axes labels, units, legends, series keys unchanged.

**Benchmark Tests (CI)**
- KCS linked pipeline → PASS threshold.
- KCS standalone pipeline → PASS threshold.

**Performance Micro‑bench**
- 50‑point grid under 150 ms (Node + browser), track in CI.

**Manual QA Scripts**
- Toggle units SI↔Imp and verify readouts at 10/20/25 knots vs EHP labels.
- Drag service margin slider; confirm live recompute & readout rounding (0.1 kW).
- Import malformed CSV → error toast, no crash; export → re‑import equals.

---

## Data & Fixtures
- `benchmarks/kcs_reference.csv`: speed (m/s), RT_ref (N). Source curated & documented in repo.
- `fixtures/demo_design_condition.json`: realistic vessel for demos.
- Water properties table: temperature (°C) → ν (m²/s), ρ (kg/m³) (curated table; interpolation OK).

---

## UI Details
**Design Condition Bar**
- Vessel ▼, Condition ▼, water props (ρ, ν) read‑only in Linked; editable in Standalone. Mode pill + banner.

**Left Rail (collapsible)**
- Speed Grid (Range/Manual/Design point; CSV io)
- Viscous Options (k toggle/input)
- HM Advanced (appendage factor, A_transom, windage area)
- Efficiencies & Margin (ηD or ηH/ηR/ηO; slider)

**Main Pane**
- Resistance & EHP plots (line plots; component breakdown toggle)
- Power curves (Delivered/Installed; slider above)
- Benchmark (overlay, Δ% table, PASS/FAIL)
- Tables (download)

Accessibility: labels, ARIA roles, keyboard ops for grid editor & slider.

---

## Error/Warning Catalog
- **Low Re**: “Re < 2×10^6 — check scale effects.”
- **Missing coeff**: show estimate derivation (e.g., CP from CB/CM) with ‘Use estimate’ chip.
- **Unit drift**: red toast if >0.1% after toggle; log for investigation.

---

## Risks & Mitigations
- **HM variant drift**: lock to documented regression set; version outputs; ship inline docs.
- **Data‑entry overload**: hide advanced inputs by default; presets; quick‑start with KCS.
- **Perf**: memoize derived coeffs; batch recompute; minimal re‑renders.

---

## Implementation Notes
- Internals SI; UI follows system default; converter at boundaries.
- Provide `estimateS(LWL,B,T,CM)` helper (clearly marked estimate) for Standalone.
- Keep `(1+k)` surface area scaling isolated; never mutate `CF` base array.
- Separate `rho_air` constant and `CD` default; expose overrides.

---

## API (internal shapes)
```ts
interface DesignCondition {
  vesselId?: string;
  name: string;
  LWL: number; B: number; T: number; CB?: number; CP?: number; CM?: number; LCB_pct?: number;
  S?: number; rho: number; nu: number; tempC: number; salinity?: number;
  source: 'linked' | 'local';
}

interface ResistanceRun {
  conditionId?: string; condition: DesignCondition; speedGrid: number[]; // m/s
  k?: number; etaD?: number; etas?: {etaH:number; etaR:number; etaO:number}; serviceMargin?: number;
}

interface Results {
  Re: number[]; CF: number[]; CF_eff?: number[]; RT: number[]; EHP: number[]; DHP?: number[]; Pinst?: number[];
  components?: { RF:number[]; RR:number[]; RA:number[]; RCA:number[]; RAA:number[] };
}
```

---

## What to Build First (Incremental, independently testable)
1. **Core math (R1 minimal)** with CLI/dev panel → verify CF & k behavior (no UI coupling).
2. **Speed Grid (R4)** → feed core math; prove instant recompute & CSV io.
3. **Design Condition Store (R7)** → manually toggle Linked/Standalone with dummy Hydro payload.
4. **HM shell (R2 partial)** using only friction + stub residuary → wire plots.
5. **Power curves (R3)** → slider correctness & readouts.
6. **Benchmark harness (R5)** with canned mini dataset → CI job.
7. **Full HM** + exports (R6) → finalize MVP.

---

## Done = Success Criteria (module level)
- KCS MAE ≤3%, Max ≤5% (configurable) in both Linked & Standalone pipelines.
- SI/Imp parity <0.1% drift after toggles.
- Exports (PNG/SVG/CSV) include vessel/condition footnote + timestamp.
- Perf: ≤150 ms for 50 points.
- Tests: ≥10 unit (math), ≥5 visual, CI bench gate live.

---

## Future (post‑MVP)
- Prohaska `(k)` estimation (CR vs Fn^4 regression).
- Detailed appendage resistance catalog (rudder, brackets, bilge keels).
- Sea margin profile vs speed; weather routing hooks.
- Propulsor sizing (B‑series / open‑water curves) & quasi‑propulsive coefficients.

