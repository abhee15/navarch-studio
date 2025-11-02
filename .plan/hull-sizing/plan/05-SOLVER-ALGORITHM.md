# Solver Algorithm & Mathematical Formulation

## Overview
First-principles sizing using displacement balance, Froude number targeting, and Holtrop-Mennen resistance.

**Custom Algorithm Development Strategy:**
- Start with proven methods (Holtrop 1984, ITTC-57)
- Mark improvement areas with `// TODO: CUSTOM_ALGO`
- Reference SPS SName paper for modern refinements
- Develop proprietary algorithms in Phase 3

---

## Algorithm Flow

### High-Level Workflow

```
INPUT: Mission requirements (cargo, speed, environment, constraints)

1. Payload Conversion
   └─> Convert volume/weight/TEU to total mass (tonnes)

2. Displacement Estimation
   └─> Use DWT/Δ ratios by vessel type to estimate total displacement

3. Hull Family Selection
   └─> Filter applicable families by mission type, Fn range, constraints

4. For Each Family:
   ├─> a. Froude Number Targeting
   ├─> b. Length from Waterline (LWL) Calculation
   ├─> c. Displacement Closure Loop (Newton-Raphson)
   ├─> d. Stability Screen (quick GM estimate)
   ├─> e. Resistance Calculation (Holtrop-Mennen)
   ├─> f. Power Estimation (EHP, SHP)
   ├─> g. Geometry Generation (offsets grid)
   └─> h. Scoring (multi-objective)

5. Ranking
   └─> Sort candidates by composite score

OUTPUT: Ranked list of candidate designs
```

---

## 1. Payload Conversion

### Volume Basis
```
Given: V_cargo (m³), ρ_cargo (t/m³)
W_payload = V_cargo · ρ_cargo
```

**Cargo density presets:**
- Crude oil: 0.85 t/m³
- Products: 0.90 t/m³
- LNG: 0.45 t/m³
- LPG: 0.52 t/m³
- Grain: 0.78 t/m³
- Containers: 0.60 t/m³ (average loaded density)

### Weight Basis
```
Given: W_payload (tonnes)
(Direct input, no conversion)
```

### TEU Basis
```
Given: TEU count
W_payload = TEU · average_t_per_TEU

Average t/TEU by container type:
- 20GP: 15 t/TEU (typical loaded)
- 40GP: 22 t/TEU
- 40HC: 24 t/TEU
```

---

## 2. Total Displacement Estimation

### DWT/Δ Ratios by Vessel Type

```
Δ_total = W_payload / (DWT/Δ)_typical + margin

Typical DWT/Δ ratios:
- Container:  0.65 - 0.75
- Tanker:     0.80 - 0.90
- Bulk:       0.75 - 0.85
- Fishing:    0.35 - 0.50
- Yacht:      0.25 - 0.40
```

**Lightship estimate:**
```
W_lightship ≈ Δ_total · (1 - DWT/Δ)
```

**Fuel & stores:**
```
W_fuel = Range_nm · SFOC · SHP / (fuel_density · 1000)
       ≈ 0.05 · Δ_total (simplified for MVP)
```

**Margin:**
```
W_margin = 0.05 · Δ_total (5% design margin)
```

**Closure equation:**
```
Δ_total = W_payload + W_lightship + W_fuel + W_margin

Iterative solution:
1. Initial guess: Δ_total = W_payload / 0.70 (assume DWT/Δ ≈ 0.70)
2. Refine: Solve above equation
3. Converge when error < 1%
```

---

## 3. Froude Number Targeting

### Froude Number Definition
```
Fn = V / √(g · L)

where:
  V = speed (m/s)
  g = 9.81 m/s²
  L = waterline length (m)
```

### Target Fn by Vessel Type

| Type | Fn Range | Typical |
|------|----------|---------|
| Container | 0.23 - 0.30 | 0.26 |
| Tanker | 0.12 - 0.18 | 0.15 |
| Bulk | 0.12 - 0.16 | 0.14 |
| Fishing | 0.18 - 0.28 | 0.23 |
| Yacht (disp) | 0.20 - 0.27 | 0.24 |
| HSC/Planing | 0.40 - 0.60 | 0.50 (switch to Savitsky) |

**Selection Logic:**
```csharp
public decimal PickFroudeNumber(HullFamilyPreset family, decimal speedKn)
{
    var speedMs = speedKn * 0.5144m; // kn to m/s
    
    // Pick from family band based on speed
    // Higher speed within type → use upper Fn range
    var fnMid = (family.FnMin + family.FnMax) / 2;
    var fnRange = family.FnMax - family.FnMin;
    
    // Simple heuristic: if speed > typical, bump Fn up
    var speedFactor = (speedKn - 15m) / 15m; // Normalized around 15 kn
    var fn = fnMid + speedFactor * (fnRange / 4);
    
    // Clamp to family range
    return Math.Clamp(fn, family.FnMin, family.FnMax);
}
```

### LWL from Fn and V

```
Rearrange: Fn = V / √(g·LWL)
           Fn² = V² / (g·LWL)
           LWL = V² / (g·Fn²)

Example:
  V = 24 kn = 12.35 m/s
  Fn = 0.26
  g = 9.81 m/s²
  LWL = 12.35² / (9.81 · 0.26²) = 230.5 m
```

---

## 4. Displacement Closure Loop (Core Algorithm)

### Newton-Raphson Iteration

**Goal:** Find dimensions (L, B, T) and coefficients (Cb) such that:
```
Δ_calculated = ρ_seawater · ∇
where ∇ = LWL · B · T · Cb

Constraint: |Δ_calculated - Δ_target| / Δ_target < 0.01 (±1%)
```

### Pseudo-code

```
INPUT:
  Δ_target (tonnes)
  LWL_initial (from Fn targeting)
  L/B, B/T, D/T (from family preset)
  Cb, Cp, Cwp (from family preset)
  Locks: {keep_fn, keep_l_over_b, keep_b_over_t, keep_d_over_t, keep_cb}
  Constraints: {max_loa, max_beam, max_draft, max_airdraft}

CONSTANTS:
  ρ_seawater = 1.025 t/m³
  g = 9.81 m/s²
  tolerance = 0.01 (±1%)
  max_iterations = 50

ALGORITHM:
1. Initialize:
   LWL = LWL_initial
   B = LWL / (L/B)
   T = B / (B/T)
   D = T · (D/T)

2. Loop (max 50 iterations):
   
   a. Apply constraints:
      if max_beam and B > max_beam:
         B = max_beam
         flags.add("beam_exceeded")
      
      if max_draft and T > max_draft:
         T = max_draft
         flags.add("draft_exceeded")
      
      if max_loa and LWL·1.03 > max_loa:
         LWL = max_loa / 1.03
         flags.add("loa_exceeded")
   
   b. Compute displacement:
      ∇ = LWL · B · T · Cb
      Δ_calc = ρ_seawater · ∇
   
   c. Error:
      e = (Δ_calc - Δ_target) / Δ_target
   
   d. Check convergence:
      if |e| < tolerance:
         CONVERGED → break
   
   e. Adjust parameters (priority order):
      
      Priority 1: Adjust B (if not locked by L/B or max_beam)
      if !locks.keep_l_over_b and B < max_beam:
         B = B · (1 - e · 0.5)  // Proportional adjustment
      
      Priority 2: Adjust T (if not locked by B/T or max_draft)
      if !locks.keep_b_over_t and T < max_draft:
         T = T · (1 - e · 0.3)
      
      Priority 3: Adjust LWL (if Fn not locked)
      if !locks.keep_fn:
         LWL = LWL · (1 - e · 0.2)
         Fn_new = V / √(g · LWL)  // Recompute Fn
      
      Priority 4: Adjust Cb (if not locked)
      if !locks.keep_cb:
         Cb = Cb · (1 - e · 0.1)
         Cb = clamp(Cb, Cb_min, Cb_max)  // Stay in family range

3. Final dimensions:
   Lpp = LWL · 0.97  // Typical Lpp/LWL ratio
   LOA = LWL · 1.03  // Overhang allowance
   D = T · (D/T)

4. Check freeboard:
   FB = D - T
   if FB < minimum_freeboard(family):
      D = T + minimum_freeboard
      flags.add("low_freeboard")

OUTPUT:
  ClosureResult{
    Lpp, LWL, LOA, B, T, D,
    Cb, Cp, Cwp, Cm = Cb/Cp,
    Δ, iterations, flags
  }
```

### TODO: Custom Algorithm Enhancements

**Mark in code:**
```csharp
// TODO: CUSTOM_ALGO - Develop adaptive step sizing
// Current: Fixed proportional adjustments (0.5, 0.3, 0.2, 0.1)
// Proposed: Adaptive step based on error magnitude and iteration count
//   - Large error (|e| > 0.10): aggressive steps (0.8 factor)
//   - Medium error (0.05 < |e| < 0.10): moderate steps (0.5 factor)
//   - Small error (|e| < 0.05): fine steps (0.2 factor)
//   - Oscillation detection: reduce step if error sign changes
```

**Gradient-based optimization (Phase 3):**
```csharp
// TODO: CUSTOM_ALGO - Implement Jacobian-based multi-variate Newton
// Current: Sequential adjustment (B → T → LWL → Cb)
// Proposed: Simultaneous adjustment using partial derivatives
//   ∂Δ/∂B = ρ · LWL · T · Cb
//   ∂Δ/∂T = ρ · LWL · B · Cb
//   ∂Δ/∂LWL = ρ · B · T · Cb
//   ∂Δ/∂Cb = ρ · LWL · B · T
// Solve: J · δp = -f  where J = Jacobian, δp = parameter adjustments, f = error
```

---

## 5. Stability Screen (Quick GM)

### Metacentric Height Calculation

```
1. Waterplane area:
   Awp = Cwp · LWL · B

2. Second moment of waterplane area (assume ellipse):
   Iwp ≈ (π/64) · Cwp · LWL · B³
   (Simplified: Iwp ≈ Cwp · LWL · B³ / 12)

3. Transverse metacentric radius:
   BMt = Iwp / ∇
   where ∇ = displacement volume (m³)

4. Vertical center of buoyancy:
   KB ≈ k_B · T
   where k_B depends on hull form:
     - Full forms (tanker, bulk): k_B ≈ 0.53
     - Medium forms (fishing): k_B ≈ 0.52
     - Fine forms (container): k_B ≈ 0.50

5. Vertical center of gravity (estimate):
   KG ≈ k_G · D
   where k_G depends on vessel type:
     - Container (high deck loads): k_G ≈ 0.55
     - Tanker (low CG): k_G ≈ 0.50
     - Fishing (mixed): k_G ≈ 0.52

6. Transverse metacentric height:
   GMt = KB + BMt - KG

7. Roll period estimate:
   T_roll ≈ 2π · k_φ · (B / √(g · GMt))
   where k_φ ≈ 0.44 for typical ships
```

### Acceptance Criteria

```
GMt > 1.0 m (typical minimum for ocean-going vessels)
GMt > 0.15 m (absolute minimum for small craft)
T_roll between 8-15 seconds (comfortable range)
```

**Flags:**
- `low_gm` if GMt < 1.0 m
- `excessive_roll_period` if T_roll > 20 s
- `insufficient_roll_period` if T_roll < 5 s

---

## 6. Holtrop-Mennen Resistance

### Full Formulation (Holtrop & Mennen, 1982/1984)

**Inputs:**
- Lpp, LWL, B, T (m)
- Cb, Cp, Cwp (-)
- V (m/s), ρ (kg/m³), ν (m²/s)
- Optional: Bulb, transom, appendages

**Step 1: Wetted Surface Area**

```
S ≈ LWL · (2T + B) · √(Cm · (0.453 + 0.4425·Cb - 0.2862·Cm - 0.003467·B/T + 0.3696·Cwp))

Simplified for MVP:
S ≈ LWL · (2T + B) · √((Cb + 0.5·(1-Cb)) / 2)
```

**Step 2: Reynolds Number**

```
Rn = V · LWL / ν

Example:
  V = 12.35 m/s
  LWL = 230 m
  ν = 1.19×10⁻⁶ m²/s (15°C seawater)
  Rn = 12.35 · 230 / (1.19×10⁻⁶) = 2.39×10⁹
```

**Step 3: Froude Number**

```
Fn = V / √(g · LWL)
```

**Step 4: Frictional Resistance (ITTC-57)**

```
Cf = 0.075 / (log₁₀(Rn) - 2)²

Example:
  Rn = 2.39×10⁹
  log₁₀(Rn) = 9.378
  Cf = 0.075 / (9.378 - 2)² = 0.001378
```

**Step 5: Form Factor (1+k₁)**

Full Holtrop formula (complex):
```
1+k₁ = c13 · (0.93 + c12·(B/LWL)^0.92497·(0.95-Cp)^(-0.521448)·(1-Cp+0.0225·lcb)^0.6906)
where c12, c13 are additional factors
```

**Simplified for MVP:**
```
1+k₁ ≈ 1 + 0.5 · Cb

// TODO: CUSTOM_ALGO - Use full Holtrop form factor or develop custom correction
// Reference: SPS SName paper Section 4.2 for modern container ship corrections
```

**Frictional resistance:**
```
Rf = 0.5 · ρ · V² · S · Cf · (1+k₁)
```

**Step 6: Wave-Making Resistance**

Full Holtrop (very complex polynomial):
```
Rw = c1 · c2 · c5 · ∇ · ρ · g · exp(m1·Fn^d + m2·cos(λ·Fn^(-2)))

where c1, c2, c5, m1, m2, d, λ are functions of Cb, Cp, LWL/B, B/T, LCB, etc.
```

**Simplified for MVP:**
```
c1 ≈ 2223105 · (0.95 - Cb)^3.78613
c2 ≈ exp(-1.89·√Cp)
Rw ≈ c1 · c2 · Fn² · ρ · g · S

// TODO: CUSTOM_ALGO - Implement full Holtrop wave resistance
// OR develop custom polynomial regression from CFD data
// Reference: SPS SName paper Section 4.3 for Fn > 0.25 corrections
```

**Step 7: Total Resistance**

```
R_total = Rf + Rw + R_appendages + R_air

For MVP (no appendages, no air):
R_total = Rf + Rw
```

**Step 8: Power**

```
EHP = R_total · V  (kW)

SHP = EHP · (1 + sea_margin) · (1 + service_margin) / η_overall

where:
  sea_margin = 0.15 (15% for rough seas)
  service_margin = 0.15 (15% for fouling, aging)
  η_overall ≈ 0.70 (propeller efficiency · shaft efficiency · gearbox)
```

---

## 7. Geometry Generation

### Wigley Hull (Fine Forms)

**Equation:**
```
y(x, z) = (B/2) · (1 - (2x/L - 1)²) · (1 - (z/T)²)

where:
  x ∈ [0, L]  (longitudinal, 0 = aft perpendicular)
  z ∈ [0, T]  (vertical, 0 = baseline)
  y = half-breadth (port side positive)
```

**Properties:**
- Parabolic waterlines
- Parabolic sections
- Cb ≈ 0.444 (for pure Wigley)
- To achieve target Cb: scale B

**Implementation:**
```csharp
public HullGeometry GenerateWigleyHull(decimal lpp, decimal b, decimal t, decimal targetCb)
{
    var stations = new List<StationOffsets>();
    int numStations = 21; // 0, 0.5, 1, ..., 20
    int numWaterlines = 11; // 0, 0.2T, 0.4T, ..., T
    
    for (int i = 0; i < numStations; i++)
    {
        var x = lpp * i / (numStations - 1);
        var xNorm = (2m * x / lpp - 1m); // -1 to +1
        
        var waterlines = new List<WaterlineOffset>();
        
        for (int j = 0; j < numWaterlines; j++)
        {
            var z = t * j / (numWaterlines - 1);
            var zNorm = z / t; // 0 to 1
            
            var y = (b / 2m) * (1m - xNorm * xNorm) * (1m - zNorm * zNorm);
            
            // Scale to achieve target Cb
            y = y * (targetCb / 0.444m);
            
            waterlines.Add(new WaterlineOffset { Z = z, Y = y });
        }
        
        stations.Add(new StationOffsets { X = x, Waterlines = waterlines });
    }
    
    return new HullGeometry
    {
        Stations = stations,
        Volume = ComputeVolume(stations), // Simpson's rule integration
        WettedSurfaceArea = ComputeWettedSurface(stations)
    };
}
```

### Series 60 Hull (Medium to Full Forms)

**Parametric representation** (based on ITTC Series 60):
- Shape functions interpolated from parent forms (CB = 0.60, 0.65, 0.70, 0.75, 0.80)
- Section area curve (SAC) defines prismatic distribution
- Waterline shape varies with Cwp

**Implementation (Phase 2):**
```csharp
// TODO: Implement Series 60 parametric generator
// Reference: Todd Series 60 parent hulls, ITTC database
// Interpolate section shapes based on target Cb, Cp
```

---

## 8. Multi-Objective Scoring

### KPI Components (from kpi_weights.csv)

```
score = w1·S_delta + w2·S_power + w3·S_constraints + w4·S_stability + w5·S_capacity

where (system defaults):
  w1 = 0.35 (delta_balance)
  w2 = 0.25 (installed_power)
  w3 = 0.20 (constraints_ok)
  w4 = 0.10 (stability_screen)
  w5 = 0.10 (teu_or_volume_fit)
```

### Component Scoring Functions

**1. Delta Balance (w1 = 0.35):**
```
error = |(Δ_calc - Δ_target) / Δ_target|
S_delta = max(0, 1 - error / 0.01)

Perfect match (error = 0): S_delta = 1.0
At tolerance (error = 1%): S_delta = 0.0
```

**2. Installed Power (w2 = 0.25):**
```
Normalize SHP to typical range for vessel type
SHP_typical = f(Δ_target, vessel_type)

S_power = max(0, 1 - (SHP - SHP_typical) / SHP_typical)

Lower power is better (up to a point)
```

**3. Constraints OK (w3 = 0.20):**
```
violations = count of flags (draft_exceeded, beam_exceeded, loa_exceeded)

S_constraints = max(0, 1 - violations / 3)

No violations: S_constraints = 1.0
All 3 violated: S_constraints = 0.0
```

**4. Stability Screen (w4 = 0.10):**
```
GM_ideal = 1.5 m (target for ocean-going)

S_stability = max(0, min(1, GMt / GM_ideal))

GMt = 1.5 m or higher: S_stability = 1.0
GMt = 0.75 m: S_stability = 0.5
GMt < 0: S_stability = 0.0
```

**5. Capacity Fit (w5 = 0.10):**
```
For containers:
  TEU_fit = EstimateTEUCapacity(B, D, LOA)
  S_capacity = min(1, TEU_fit / TEU_target)

For bulk/tanker:
  V_cargo_fit = (LWL · B · D) · α_hold (α_hold ≈ 0.85 for cargo hold fraction)
  S_capacity = min(1, V_cargo_fit / V_cargo_target)
```

**Composite Score:**
```
score = Σ(wi · Si) / Σ(wi)

Normalize to [0, 1] range
```

---

## 9. Constraint Handling

### Hard Constraints (Must Not Violate)

1. **Max LOA:**
   ```
   if LOA > max_loa:
      Adjust LWL = max_loa / 1.03
      Recompute Fn (will change if not locked)
      Flag: "loa_exceeded"
   ```

2. **Max Beam:**
   ```
   if B > max_beam:
      Clamp B = max_beam
      Adjust L/B ratio (will change)
      Flag: "beam_exceeded"
   ```

3. **Max Draft:**
   ```
   if T > max_draft:
      Clamp T = max_draft
      Adjust B/T ratio (will change)
      Flag: "draft_exceeded"
   ```

4. **Max Air Draft:**
   ```
   if D + superstructure_height > max_airdraft:
      Reduce D
      Flag: "airdraft_exceeded"
   ```

### Soft Constraints (Warnings)

1. **Low Freeboard:**
   ```
   if D - T < 0.3 m:
      Flag: "low_freeboard"
      (Still return candidate, but warn user)
   ```

2. **Low GM:**
   ```
   if GMt < 1.0 m:
      Flag: "low_gm"
   ```

3. **Propeller Diameter:**
   ```
   D_prop_max = 0.7 · T (rule of thumb)
   if required_D_prop > D_prop_max:
      Flag: "prop_diameter_issue"
   ```

---

## 10. SPS SName Paper Reference

### Paper Details
- **Title:** "Ship Performance Prediction by Modern Statistical Methods and Physical Models"
- **Authors:** SPS (Ship Performance Society)
- **Focus:** Regression-based improvements to classical methods (Holtrop, Series 60)

### Key Takeaways for Custom Algorithm

**1. Form Factor (1+k₁) Improvements:**
- Classical Holtrop uses complex polynomial (20+ terms)
- SPS SName proposes regression from CFD data for modern hull forms
- **Action:** In Phase 3, develop custom (1+k₁) formula trained on container ships at Fn 0.25-0.30

**2. Wave Resistance at High Fn:**
- Holtrop 1984 less accurate for Fn > 0.28 (modern container ships)
- SPS SName provides corrections for bulbous bows and fine hulls
- **Action:** Add correction factor for container ships operating at Fn > 0.26

**3. Prismatic Coefficient Effects:**
- Cp distribution (LCB position) significantly affects wave resistance
- SPS SName proposes Cp-weighted corrections
- **Action:** Include LCB in resistance calculation (currently simplified)

**4. Appendage Resistance:**
- Classical methods use crude percentages (2-5% of total)
- Modern methods account for rudder, shaft brackets, bow thruster tunnels individually
- **Action:** Add appendage database by vessel type

### Implementation Plan (Phase 3)

```csharp
// Phase 1 (MVP): Standard Holtrop with simplifications
public decimal ComputeHoltropSimplified(...)
{
    // Simplified form factor, simplified wave resistance
    // Mark all areas with // TODO: CUSTOM_ALGO
}

// Phase 3: Custom Algorithm with SPS SName Refinements
public decimal ComputeCustomResistance(...)
{
    // 1. CFD-trained form factor for container ships
    var formFactor = ComputeCustomFormFactor(Cb, Cp, LWL_B, Fn);
    
    // 2. Regression-based wave resistance
    var waveRes = ComputeRegressionWaveResistance(Fn, Cb, Cp, LCB, hullForm);
    
    // 3. Type-specific appendage resistance
    var appendageRes = GetAppendageResistance(vesselType, LWL, V);
    
    return frictionalRes + waveRes + appendageRes;
}
```

---

## 11. Performance Targets

### Solver Performance

**Displacement Closure:**
- Target: <50ms per candidate
- Strategy: Fast math, early exit, cached presets

**Holtrop Calculation:**
- Target: <20ms per candidate
- Strategy: Simplified formulas, pre-compute constants

**Full Solve (5 candidates):**
- Target: <2s total (backend)
- Strategy: Parallel candidate generation

**Slider Interaction:**
- Target: <300ms round-trip (API call + re-render)
- Strategy: Debouncing, optimistic UI update, Web Worker

### 3D Rendering Performance

**Mesh Complexity:**
- Near LOD: 80,000 triangles (camera within 2× Lpp)
- Mid LOD: 40,000 triangles (camera 2-5× Lpp)
- Far LOD: 20,000 triangles (camera > 5× Lpp)

**Frame Rate:**
- Target: ≥45 FPS (p50)
- Minimum: 30 FPS (never below)
- Strategy: Dynamic LOD, frustum culling, throttle re-renders

---

## Next: Read `06-API-SPECIFICATION.md` for endpoint details
