# NavArch Studio – Mission→Hull Sizing Module (Spec v0.1)

## 1) Purpose & Scope
A first‑cut “Mission→Hull” wizard inside **NavArch Studio** that turns mission requirements (ship type, payload **volume/weight**, **speed**, and **environment**) into **preliminary principal dimensions** (Lpp/LWL, B, T, D), hull coefficients (Cb, Cp, Cwp), a **3D parametric hull**, and **early resistance/power** estimates. The module supports two design modes:

1) **First‑Principles Sizing** — rule‑of‑thumb ranges + physics (displacement balance, Froude scaling, coefficients by type, Holtrop resistance).
2) **Data‑Driven Sizing** — regressions/nearest‑neighbors over a curated vessel catalog (e.g., KCS, KVLCC2, Series 60 variants, HSC datasets) to suggest L/B/T and coefficients.

The tool returns multiple **candidate hulls** (container, tanker, bulk, fishing, yacht, HSC, etc.), each as a card with KPIs and a live 3D preview. Users can tweak constraints and lock ratios (e.g., Fn, L/B, B/T) and re‑solve instantly.

---

## 2) User Inputs (Wizard)
**Step A: Mission & Cargo**
- Mission category: Pleasure (Yacht, Pontoon, HSC), Government (Naval, Dredger, Medical), Commercial (Cargo, Tanker, Bulk, LNG/LPG, Container, Fishing)
- Cargo basis:
  - **Volume** (m³) + cargo density (t/m³) → converts to weight
  - **Weight** (t) → converts to displacement target after lightship/fuel
  - Container option: **TEU count** + stowage policy (on/off deck proportions)
- Endurance/Range (nm) [optional]

**Step B: Speed Profile**
- Service speed (kn or m/s)
- Sea/Service margin (% default 15–30)

**Step C: Environment & Constraints**
- Design sea state (Hs, Tz) or Beaufort; wave period **Tz** for wavelength overlay
- Theater constraints: Max LOA, Max Beam, Max Draft, Air Draft, Lock/Panamax/Canal rules
- Regulatory presets (freeboard class, notations — simplified)

**Step D: Options & Families**
- Hull family presets by mission: Container‑like (fine), Tanker/Bulk (full), Fishing (medium), Yacht (displacement/planing), HSC (planing/catamaran), Pontoon (cat)
- Geometry resolution and 3D rendering options

---

## 3) Outputs (per Candidate)
- **Principal Dimensions**: Lpp, LWL, LOA*, B, T, D, CB, CP, CWP, Displacement ∇
- **Speed/Power**: Fn, ITTC friction, Holtrop total R, EHP at speed, SHP with margins
- **Capacity Checks**: Cargo volume fit; DWT/Δ sanity; TEU arrangement estimate
- **Stability/Seakeeping Screens** (first‑pass): BMt, GMt (approx.), roll period check, L/λ overlay
- **3D**: Live parametric hull (Wigley, Series 60, KCS‑like, KVLCC2‑like, planing form) with waterplane, CB/CG/LCB markers and **wave‑length overlay**
- **Flags**: violated constraints (draft/beam/LOA), low freeboard, undersized propeller diameter vs T, excessive Fn for family, etc.

---

## 4) First‑Principles Sizing Loop (Core Algorithm)
**Given:** payload basis (V_cargo or W_cargo), service speed **V**, environment (Tz), and constraints.

1. **Convert payload** to mass:  
   - If volume input: `W_payload = ρ_cargo · V_cargo` (use density presets for crude/products/LNG/LPG/water; TEU → average t/TEU).

2. **Estimate Lightship + Ops weight**  
   - Use type‑based fractions or user override: `W_total ≈ W_payload + W_lightship + W_fuel + W_stores + W_margin`.  
   - If unknown, use **DWT/Δ** typical by type to close the loop initially.

3. **Pick target Froude number Fn by type** (typical ranges):
   - Container: **0.23–0.30**  
   - Tanker/Bulk: **0.12–0.18**  
   - Fishing/Patrol: **0.18–0.28**  
   - Yacht (disp.): **0.20–0.27**; Yacht (planing): use planing path  
   - HSC/Planing: switch to Savitsky (use Fn_T = V/√(g·T) screening)

4. **Solve preliminary LWL from speed & Fn**  
   `Fn = V / √(g·LWL)` ⇒ `LWL = V² / (g·Fn²)`  
   Lock LWL within LOA constraints and add **Lpp/LWL** conventions.

5. **Choose geometric ratios by type** (initial guesses):
   - **L/B**: Container **6.5–8.5**; Tanker **5.0–6.5**; Bulk **5.0–6.0**; Fishing **4.5–5.5**; Yacht‑disp **5–7**; Planing **3–4**  
   - **B/T**: Container **2.3–3.0**; Tanker **2.7–3.5**; Bulk **2.6–3.4**; Fishing **2.0–2.8**  
   - **D/T**: **1.25–1.60** (family‑dependent; ensures freeboard)

6. **Choose coefficients by type/speed** (starting ranges):
   - **Cb**: Container **0.60–0.70**, Tanker **0.80–0.85**, Bulk **0.75–0.85**, Fishing **0.55–0.65**, Yacht‑disp **0.45–0.55**  
   - **Cp**: Container **0.60–0.70**, Tanker **0.80–0.85**, Bulk **0.78–0.84**, Fishing **0.62–0.70**  
   - **Cwp**: Container **0.85–0.95**, Tanker **0.80–0.90**, Bulk **0.80–0.90**

7. **Close the displacement balance**  
   `∇ = LWL · B · T · Cb` (saltwater ρ≈1.025 t/m³) ⇒ `Δ = ρ · ∇`  
   Newton loop: adjust **B** (within L/B), then **T** (within B/T & DraftMax), else **LWL** and **Cb** until `Δ ≈ W_total` and `Fn` stays in the band.

8. **Depth & freeboard**  
   Use `D = T · (D/T)` plus a **freeboard check** (simplified class preset). If freeboard low → bump **D**; if stability low → bump **B** (and re‑close Δ).

9. **Stability screen (quick)**
   - `I_wp ≈ Cwp · LWL · B³ / 12`, `BMt = I_wp / ∇`  
   - `KB ≈ k_B · T` (k_B≈0.53, family‑dependent)  
   - `GMt ≈ KB + BMt − KG` (use KG from type‑based weight vertical CG)  
   - Check roll period target: `T_roll ≈ 2π · k_φ · (B / √(g·GMt))` with **k_φ ≈ 0.44–0.50**

10. **Resistance/Power (displacement hulls)**
    - ITTC‑57 friction + Holtrop & Mennen correlation for total **R(V)**  
    - **EHP = R·V**, add **sea margin** and **service margin** for **SHP**  
    - Prop diameter screen: `D_prop ≤ 0.7·T` (simple check)

11. **Environment overlay**
    - Deep‑water wavelength from period: `λ = g·Tz² / (2π)`  
    - Display `LWL/λ` and mark ranges where pitch/heave RAOs tend to amplify (screening only)

12. **Refined dimensions L′, B′, D′ (“factors”)**
    - **L′ (LOA or L1′)**: add rake/overhang allowance: `L′ = Lpp·(1 + α_L)`; α_L=0…0.03 by family  
    - **B′**: stability/cargo allowance if GM/Lashings insufficient: `B′ = B·(1 + α_B)`; α_B=0…0.05  
    - **D′**: `D′ = D + Δfreeboard(structural & class) + camber allowance`

13. **Constraint flags & scoring**  
    Multi‑objective score combining capacity fit, powering, seakeeping screen, constraints, and margins. Rank candidates.

---

## 5) Data‑Driven Sizing (Optional Mode)
- **Feature vector**: mission type, V (kn), payload (t), payload density, Hs, Tz, theater caps (LOA/Beam/Draft), material, propulsion type  
- **Targets**: Lpp, LWL, B, T, D, Cb, Cp, Cwp, DWT/Δ, installed power  
- **Models**:
  - **K‑Nearest Neighbors** over normalized features to suggest neighbor ratios (L/B, B/T, Cb)  
  - **Regularized regressions** to predict dimensions and coefficients with uncertainty bands  
- **Catalog sources** (internal table): KCS, KVLCC2, Series 60 variants, public planing craft series; user can import CSVs to grow corpus.

---

## 6) 3D Geometry & Visualization
- **Engine**: React + `react-three-fiber` (Three.js).
- **Families**:
  - **Wigley** (fine): `y = (B/2)·(1 − (x/L)²)·(1 − (z/T)²)`  
  - **Series 60‑like** parametric fuller hull (shape params to hit target **Cb**)  
  - **KCS‑like** and **KVLCC2‑like** templates (control points scaled to L,B,T,Cp)  
  - **Planing form** (Savitsky inputs: deadrise β, LCG, beam at chine B_c)
- **Overlays**: waterplane, CB/LCB/LCG markers, λ‑grid at surface, Fn badge, constraint lines (Max Beam/Draft), hull‑family silhouette compare.
- **Interaction**: sliders for L, B, T, Cb with **locks** (keep Fn, keep L/B, keep B/T). Instant re‑solve + redraw.

---

## 7) UI Flow (Wizard + Workspace)
1. **Mission → Cargo** (cards)  
2. **Speed & Environment**  
3. **Constraints** (LOA/Beam/Draft/Air draft)  
4. **Candidates** (cards grid): KPIs + 3D thumbnails; select to open **Workspace**  
5. **Workspace**: left pane = inputs & locks; right = 3D + key charts (R vs V, Δ balance, Fn vs L, GM screen).
6. **Export**: candidate JSON, CSV of dims/coeffs, PNG of views; “Send to Hydrostatics / Resistance modules”.

---

## 8) Minimal Data Model
- `MissionCase`: id, mission_type, cargo_basis (volume/weight/TEU), cargo_value, cargo_density, speed_kn, sea_margin_pct, Hs, Tz, caps (LOA/Beam/Draft/Air)
- `SizingRun`: id, mission_case_id, mode (first_principles/data), timestamp, note
- `CandidateDesign`: run_id, hull_family, Lpp, LWL, LOA, B, T, D, Cb, Cp, Cwp, disp, Fn, GM_est, EHP, SHP, scores, flags
- `HullFamily`: id, name, type, param_ranges (L/B, B/T, D/T, Cb, Cp), generator (Wigley/Series60/KCS/KVLCC2/Planing)
- `VesselCatalog` (for data‑mode): features + targets + provenance

---

## 9) API Sketch (Fast endpoints)
- `POST /mission-cases`  
- `POST /sizing-runs` (body: mission_case_id, mode, options) → returns `CandidateDesign[]`  
- `POST /candidates/{id}/hydrostatics` → compute curves/tables  
- `POST /candidates/{id}/resistance` → Holtrop sweep; return EHP curve  
- `GET /hull-families` → ranges/metadata

---

## 10) Pseudocode (First‑Principles Solver)
```
input: mission, payload_basis, V, env(Tz), caps

# payload & total weight
W_payload = basis_to_weight(payload_basis)
Δ_target  = close_with_type_fractions(W_payload, mission.type)

# choose Fn, ratios, coefficients from type presets
Fn   = pick_Fn_range(mission.type, V)
LWL  = V*V / (g * Fn*Fn)
[L_B, B_T, D_T, Cb, Cp, Cwp] = pick_presets(mission.type)

loop until converge and constraints satisfied:
  B = LWL / L_B
  T = B / B_T, clamp by caps.draft
  ∇ = LWL * B * T * Cb
  Δ = rho_sw * ∇
  error = Δ - Δ_target
  if |error| < tol and Fn in band: break
  adjust [B, T, LWL, Cb] in priority order to reduce error while keeping ratios and caps

D = T * D_T; D = ensure_freeboard(D, mission.type)

# quick stability screen
GM = quick_GM(LWL, B, T, Cwp, KG_est(mission.type))

# resistance & power
R = holtrop_total_resistance(LWL, B, T, Cb, Cp, V)
SHP = (R*V)*(1+sea_margin)*(1+service_margin) / eta_overall

# refined dimensions (allowances)
[L′, B′, D′] = apply_allowances(LWL, B, D, family)

return CandidateDesign(...)
```

---

## 11) Type Preset Table (seed values)
| Type | Fn | L/B | B/T | D/T | Cb | Cp | Cwp |
|---|---|---:|---:|---:|---:|---:|---:|
| Container | 0.23–0.30 | 6.5–8.5 | 2.3–3.0 | 1.30–1.55 | 0.60–0.70 | 0.60–0.70 | 0.85–0.95 |
| Tanker | 0.12–0.18 | 5.0–6.5 | 2.7–3.5 | 1.35–1.60 | 0.80–0.85 | 0.80–0.85 | 0.80–0.90 |
| Bulk | 0.12–0.16 | 5.0–6.0 | 2.6–3.4 | 1.35–1.60 | 0.75–0.85 | 0.78–0.84 | 0.80–0.90 |
| Fishing/Patrol | 0.18–0.28 | 4.5–5.5 | 2.0–2.8 | 1.25–1.45 | 0.55–0.65 | 0.62–0.70 | 0.85–0.92 |
| Yacht (disp.) | 0.20–0.27 | 5.0–7.0 | 2.2–3.0 | 1.25–1.45 | 0.45–0.55 | 0.60–0.68 | 0.86–0.94 |
| HSC/Planing* | — | 3.0–4.0 | 1.6–2.2 | 1.10–1.30 | 0.35–0.50 | — | — |
\* Switch to Savitsky dynamic‑lift path when planing (Fn ≳ 0.4 or Fn_T high).

---

## 12) TEU & Tank Checks (lightweight but useful)
- **Container**: compute bays × rows × tiers from B and D; limit by visibility & lashing rules (simplified); estimate TEU fit and weight.
- **Tanker/Bulk**: cargo hold block fraction α (type‑based) × (L·B·D) ≥ required cargo volume; ensure longitudinal CG ≈ LCB.

---

## 13) Integration Points (Existing Modules)
- **Hydrostatics**: pass CandidateDesign to compute curves; return KB/LCB/curves; update 3D markers.
- **Resistance/Powering**: run Holtrop sweep around service speed; show EHP vs V; propagate to **Propulsor Sizing** (diameter ≤ 0.7T).
- **Water Properties**: density/viscosity by temp/salinity for Δ & friction.

---

## 14) Acceptance Criteria (MVP)
1) Wizard converts mission+payload+speed into ≥3 ranked candidates per mission type within 1–2 seconds for default presets.
2) Each candidate closes Δ to within **±1%** and keeps Fn within target band.
3) Holtrop EHP curve is generated and exported (CSV/PNG) for each candidate.
4) Constraint flags fire correctly (draft/beam/LOA) and update score.
5) 3D hulls (Wigley, Series 60‑like, KCS‑like, KVLCC2‑like, planing) render with overlays at ≥30 FPS on default laptop.

---

## 15) Backlog (Nice‑to‑Haves)
- RAO import + strip‑theory seakeeping light solver for L/λ hot‑spots.
- Canal rule packs (Panamax, Suezmax, Seawaymax) as one‑click constraints.
- Class freeboard table hooks; probabilistic damage stability screens.
- Multi‑objective designer (Pareto) across Δ error, SHP, GM, TEU fit, L/λ.
- Dataset curation UI + confidence bands for data‑mode.
- Report generator block for candidate comparisons.

---

## 16) Implementation Notes
- Use **locks** aggressively (Fn, L/B, B/T, D/T) to make the solver feel stable.
- Start with **Wigley + Series 60‑like** families (minimal params) then layer KCS/KVLCC2 scalers.
- Keep the **Newton loop** simple, with soft penalties for constraint hits; expose tolerances in settings.
- All unit handling at system level (no per‑field units in UI).



# EPIC: Mission→Hull Sizing (Independent App)

## Goal
Ship a stand‑alone Mission→Hull Sizing app (schema `sizing`) with first‑principles + data‑driven sizing, rich interactive visualization (3D/2D), and CAD‑grade export (DXF/IGES/STEP/SAT). Provide a one‑click handoff to Hydrostatics/Resistance.

## Success metrics
- Δ closed within ±1% for accepted candidates; Fn within target band for family.
- 3D viewport >= 45 FPS on a mid‑range laptop (integrated GPU OK) with hull wireframe + overlays enabled.
- Compute < 300 ms for slider interactions; < 2 s full recompute (Holtrop @ service speed).
- Lossless import/export round‑trip with AutoCAD on representative test hulls (IGES/STEP/SAT/DXF).

---

## Workstreams & Stories

### A) Geometry & Visualization Engine
1. **Parametric hull kernel v1** (Wigley, Series‑60‑like): generate surface/mesh from (L,B,T,Cb,Cp,Cwp). *DoD:* JSON geom params → watertight mesh; section/plan views.
2. **KCS/KVLCC2 scalers**: scale control curves to match given L,B,T,Cp; preserve prismatic distribution.
3. **3D renderer**: react‑three‑fiber + camera controls + waterplane; CB/LCB/LCG markers; λ overlay from Tz.
4. **2D plan/sections**: orthographic slices, Bonjean‑style area view; SVG export.
5. **Interactive locks**: Keep Fn / L/B / B/T / D/T / Cb‑band; visual badges for active locks.
6. **Shape slider**: map slider → (V and/or LWL) → fast re‑closure; update hull + KPIs live.
7. **Inverse edit handles**: drag stations/waterline control points → least‑squares back‑solve to (B,T,Cb,Cp) within bands; update inputs panel.
8. **Constraint overlays**: Draft/Beam/LOA/air‑draft limits; canal presets bands.
9. **Performance budget**: dynamic LOD; freeze Holtrop beyond N events/sec; GPU profiling.

### B) First‑Principles Solver
10. **Δ closure loop**: priority B→T→LWL→Cb; Newton with soft penalties for constraints.
11. **Fn logic**: infer Fn band by family & V; solve LWL = V²/(g·Fn²); lock/unlock behavior.
12. **Coefficients presets**: per family (L/B, B/T, D/T, Cb, Cp, Cwp) with bands; JSON‑driven.
13. **Quick stability screen**: I_wp, BMt, KB, GMt with KG by family; roll period heuristic.
14. **Holtrop@V**: compute R, EHP, SHP including sea/service margins; surface area from kernel.
15. **Planing branch**: Savitsky screening for HSC; switch rendering to planing template.
16. **Scoring**: weighted objective across Δ error, SHP, constraints, GM; tunable weights.

### C) Data‑Driven Mode
17. **Catalog schema & seeder**: `catalog_vessel` with provenance; seed KCS/KVLCC2/Series‑60 (metadata only); ISO 668 containers.
18. **KNN baseline**: neighbors over (V, payload, constraints) → suggest (L/B, B/T, Cb, Cp) with uncertainty bands.
19. **Regularized regression**: predict (Lpp,B,T,D,Cb,Cp); k‑fold; confidence intervals.
20. **Provenance/ethics**: store fetch method/licence; block redistribution of AIS‑vendor data.

### D) UI/UX & Workflow
21. **Wizard**: Mission→Cargo → Speed/Env → Constraints → Candidates.
22. **Workspace**: two‑panel inputs↔view; 3D/2D tabs; KPIs, EHP vs V, Δ balance chart.
23. **Candidates grid**: ranked cards; compare mode (3‑up) with synchronized views.
24. **Push to Hydro/Resistance**: create job payload; return curves back‑annotation.
25. **State save/restore**: local draft + server snapshots; JSON diff for reviews.

### E) CAD I/O
26. **DXF exporter**: plan/sections (POLYLINE/3DFACE); metadata.
27. **IGES exporter**: trimmed NURBS surfaces for hull + deck.
28. **STEP exporter**: AP214/242 surfaces; units and layer mapping.
29. **SAT (ACIS) exporter**: solids/surfaces for AutoCAD/IntelliCAD.
30. **Importers**: IGES/STEP/SAT/DXF → fit param hull (ICP + LS) → recreate (L,B,T,Cb,Cp).
31. **AutoCAD RT tests**: verify open/visualize/round‑trip on sample hulls.

### F) Data, ETL & Governance
32. **Dataset registry**: YAML with source, licence, version, checksum.
33. **ISO containers table**: types, dims, MGW; TEU fit estimator.
34. **Water properties**: ρ, ν by temp/salinity; interpolation service.
35. **Legal guardrails**: ToS reminders for AIS vendors; Equasis usage notes.

### G) Quality & Validation
36. **Reference cases**: barge, Series‑60 CB=0.6, KCS, KVLCC2.
37. **Numerical tests**: Δ closure tolerance, lock behavior, inverse edits.
38. **Performance tests**: slider jitter < 30 ms; render > 45 FPS.
39. **Export/import tests**: IGES/STEP/SAT/DXF golden files; AutoCAD open‑checks.

---

## Deliverables (this EPIC)
- CSV templates (mission, run, families, catalog, ISO, water, test matrix).
- Wireframe + architecture diagram.
- API + DDL (schema `sizing`).
- Demo app with Compute + slider; live 3D/2D views; candidates and export.

## Notes on datasets & licensing
- Vessel particulars via **VesselFinder** and **MarineTraffic** are available via paid APIs (Master/Particulars endpoints). Redistribution is restricted—store only IDs and fields allowed by their ToS for internal use. Use **Equasis** for free, registered access; no bulk redistribution.
- Seed the catalog with **public standard hulls** (KCS/KVLCC2/Series‑60) and user‑uploaded CSVs. 

