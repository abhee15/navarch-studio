# Data-Driven Mode - User Guide

**Last Updated:** November 6, 2025  
**For:** Naval Architects, Ship Designers, Students  
**Difficulty:** Beginner-Friendly

---

## What is Data-Driven Mode?

Data-Driven mode uses **real-world vessel data** to accelerate your hull design process. Instead of starting from scratch, it finds similar vessels in a catalog of 600 real ships, scales them to your requirements, and refines the design with physics-based calculations.

### When to Use Data-Driven Mode

✅ **Use Data-Driven when:**
- You want faster results (~50% faster than First-Principles)
- You're designing a common vessel type (container, tanker, bulk, etc.)
- You trust proven hull forms from real ships
- You want to see which reference vessel your design is based on

❌ **Use First-Principles when:**
- You're designing novel/unconventional vessels
- You want to explore the full design space
- You don't care about reference vessels
- You prefer pure physics-based approach

---

## How It Works (Simple Explanation)

```
Your Mission → Find 5 Similar Vessels → Scale to Your Size → Physics Check → Final Designs
     ↓                    ↓                      ↓                  ↓              ↓
"500 TEU        Finds: KCS (Container)    Scales KCS from      Validates:    5 Optimal
 Container      Emma Maersk               52,000t → 50,000t    - Stability   Designs
 50kt ship"     Madrid Maersk             using cube-root      - Resistance  (with KCS
                MSC Oscar                 law, preserves       - Buoyancy    as reference)
                OOCL Belgium              Cb, Cp ratios        - Constraints"
```

---

## Step-by-Step Tutorial

### Step 1: Start Mission Wizard

1. Log in to NavArch Studio
2. Click **"Hull Sizing"** from dashboard
3. Click **"+ New Mission"**

---

### Step 2: Define Mission (Steps 1-3)

Complete the wizard as usual:
- **Step 1:** Vessel type, cargo (TEU, weight, or volume)
- **Step 2:** Service speed, sea conditions
- **Step 3:** Constraints (max beam, draft, LOA)

*No changes here - same as First-Principles mode*

---

### Step 3: Select Solver Mode (**NEW!**)

On **Step 4: Options & Review**, you'll see:

```
┌──────────────────────────────────────────────────────┐
│ Solver Mode                                          │
│                                                      │
│  ┌────────────────┐   ┌────────────────────────┐  │
│  │ 🧮             │   │ 📊  Data-Driven  NEW  │  │
│  │ First-         │   │                        │  │
│  │ Principles     │   │ KNN search on 600      │  │
│  └────────────────┘   │ real-world vessels     │  │
│                        └────────────────────────┘  │
│                                                      │
│ 💡 First-Principles: Pure physics from scratch      │
│    Data-Driven: Starts from similar real vessels    │
└──────────────────────────────────────────────────────┘
```

**Click on the Data-Driven card** to select it.

---

### Step 4: Review Benefits

When Data-Driven is selected, you'll see:

```
┌──────────────────────────────────────────────────────┐
│ 📊 Solver Mode: Data-Driven (Real-World Catalog)    │
│                                                      │
│ KNN search on 600 real-world vessels →              │
│ Scaling → Physics refinement                        │
│                                                      │
│ ✓ Faster convergence (~50% faster)                  │
│ ✓ Proven hull forms (KCS, KVLCC2, etc.)             │
│ ✓ Shows reference vessel & similarity score         │
│                                                      │
│ ⚡ Expected compute time: <1 second for 5 candidates │
└──────────────────────────────────────────────────────┘
```

---

### Step 5: Generate Hulls

Click **"🚀 Generate Hulls"**

You'll see:
```
Generating Hull Designs...
Running Data-Driven Real-World solver
This usually takes <1 second
```

---

### Step 6: View Results with Provenance

After generation, each candidate card will show a **green provenance panel**:

```
┌────────────────────────────────────────────┐
│ #1  Container  Score: 92.4%                │
├────────────────────────────────────────────┤
│ ┌────────────────────────────────────────┐ │
│ │ 📊 Data-Driven Design ✨               │ │
│ │ Reference: KCS                         │ │
│ │ Similarity: ████████░░ 87%             │ │
│ │ Scaled from proven vessel, refined     │ │
│ │ with physics                           │ │
│ └────────────────────────────────────────┘ │
│                                            │
│ [3D Hull View]                             │
│                                            │
│ Lpp: 235.6m  B: 33.1m  T: 11.2m  Cb: 0.651│
│                                            │
│ Displacement: 50,124 t                     │
│ Fn: 0.245                                  │
│ EHP: 3,456 kW                              │
│                                            │
│ [Open Workspace]  [Compare]                │
└────────────────────────────────────────────┘
```

**Provenance Panel Explained:**
- **Reference:** Which real vessel this design is based on
- **Similarity:** How similar your requirements were to that vessel (87% = very similar)
- **Green background:** Indicates data-driven design

---

## Understanding Results

### Similarity Score Interpretation

| Score | Meaning | What It Tells You |
|-------|---------|-------------------|
| 90-100% | Excellent match | Your mission is very similar to a known vessel. High confidence. |
| 75-89% | Good match | Similar vessel type and size. Reliable design. |
| 60-74% | Moderate match | Vessel scaled significantly, but still valid starting point. |
| <60% | Weak match | Consider First-Principles mode for better exploration. |

### When to Trust Data-Driven Results

✅ **High confidence when:**
- Similarity score >75%
- Vessel type matches exactly
- Minimal constraint violations
- Reference vessel is well-known (KCS, KVLCC2, DTMB 5415)

⚠️ **Medium confidence when:**
- Similarity score 60-75%
- Constraints applied (beam/draft limits)
- Reference vessel is less common

❌ **Low confidence when:**
- Similarity score <60%
- Mode shows "FirstPrinciples_Fallback" (data-driven failed)
- No provenance panel visible

**Tip:** If confidence is low, try First-Principles mode for comparison.

---

## Comparing Modes

| Aspect | First-Principles | Data-Driven Real-World |
|--------|------------------|------------------------|
| **Speed** | ~1.5 seconds | ~0.8 seconds (**47% faster**) |
| **Starting Point** | Random initialization | Similar real vessel |
| **Design Space** | Full exploration | Guided by catalog |
| **Novelty** | Can generate unique forms | Biased toward existing forms |
| **Provenance** | None | Shows reference vessel |
| **Confidence** | Physics validation only | Physics + real-world precedent |
| **Best For** | Novel vessels, exploration | Common vessels, speed |

---

## Catalog Vessels

The real-world catalog contains **600 vessels** from:

### Vessel Types

| Type | Count | Examples |
|------|-------|----------|
| Bulk carrier | 75 | Capesize, Panamax |
| Container | 63 | KCS, Emma Maersk |
| Offshore supply | 64 | PSV, AHTS |
| Fishing | 59 | Trawlers, seiners |
| Tanker | 55 | KVLCC2, Suezmax |
| Cruise ship | 56 | Symphony of the Seas |
| RO-RO | 56 | Car carriers |
| Ferry | 54 | Passenger ferries |
| Naval combatant | 50 | DTMB 5415 |
| Other | 68 | Research vessels, etc. |

### Data Sources

- **SIMMAN:** Model test data (KCS, KVLCC2, DTMB 5415)
- **MARIN:** Towing tank data
- **Public Registries:** Lloyd's, DNV
- **Academic:** MIT ShipD, Delft DSYHS

### Benchmark Vessels

Famous vessels in the catalog:
- **KCS** (KRISO Container Ship) - Model test standard
- **KVLCC2** - VLCC tanker benchmark
- **DTMB 5415** - Naval combatant standard
- **Emma Maersk** - Large container ship
- **Symphony of the Seas** - Largest cruise ship

---

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| `Ctrl+1` to `Ctrl+4` | Jump to wizard step 1-4 |
| `Ctrl+Enter` | Generate hulls (from Step 4) |
| `Esc` | Cancel generation |
| `M` | Toggle solver mode (Step 4) |

*(Keyboard shortcuts planned for future release)*

---

## Tips & Best Practices

### 1. When to Switch Modes

Start with **Data-Driven** for:
- Standard commercial vessels
- Common vessel types
- Quick feasibility studies

Switch to **First-Principles** if:
- Data-Driven returns low similarity (<60%)
- You see "FirstPrinciples_Fallback" in results
- You want to explore unconventional designs

### 2. Interpreting Provenance

If you get "Reference: KCS" with 87% similarity:
- ✅ Your design is based on a well-validated hull form
- ✅ Performance predictions are likely accurate
- ✅ You can look up KCS literature for more insights

If you get "Reference: UNKNOWN_VESSEL_123" with 55% similarity:
- ⚠️ Weaker match, more scaling applied
- ⚠️ Consider running First-Principles for comparison
- ⚠️ Validate results carefully

### 3. Combining with First-Principles

You can run **both modes** for the same mission:
1. Run Data-Driven first (faster)
2. Run First-Principles second (thorough)
3. Compare results to build confidence

### 4. Constraint Handling

If you have tight constraints (max beam, draft):
- Data-Driven will try to clamp and compensate
- If distortion >10%, candidate is marked invalid
- Fewer valid candidates may be returned
- Fallback to First-Principles is automatic

---

## FAQ

**Q: Will Data-Driven always be faster?**  
A: Usually yes (~50% faster), but if no good matches are found, it falls back to First-Principles.

**Q: Can I see the reference vessel's geometry?**  
A: Not yet - Phase 2 will include catalog browser where you can view reference vessels.

**Q: What if I design a type not in the catalog?**  
A: It will search across all types, or fall back to First-Principles if no reasonable match.

**Q: Can I add my own vessels to the catalog?**  
A: Not in Phase 1. Phase 2 will support user-added vessels.

**Q: Is data-driven less accurate?**  
A: No - it uses the same physics refinement as First-Principles, just with a better starting point.

**Q: What's the ML/Parametric mode?**  
A: Phase 2 feature (82,000+ synthetic hulls from MIT ShipD dataset) - coming soon!

---

## Troubleshooting

### Issue: Mode toggle doesn't appear

**Solution:** Update frontend to latest version

### Issue: No provenance panel on results

**Possible Causes:**
1. Mission was run before Nov 6, 2025 (before feature launch)
2. Mode was "first_principles" (no provenance for FP mode)
3. Solver fell back to First-Principles (check logs)

**Solution:** Run a new sizing with mode="data_driven_real"

### Issue: All candidates show "FirstPrinciples_Fallback"

**Possible Causes:**
1. No vessels in catalog for your type
2. Catalog not seeded

**Solution:** Contact support or check backend logs

---

## Support

**Documentation:** `.plan/app-docs/hull-sizing/data-driven/`  
**API Reference:** `04-API-REFERENCE.md`  
**Implementation:** `03-IMPLEMENTATION-GUIDE.md`  
**Architecture:** `01-ARCHITECTURE.md`

**Contact:** support@navarch.studio

---

**User Guide Version:** 1.0  
**Last Updated:** November 6, 2025  
**Status:** Complete

