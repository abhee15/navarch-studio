# Phase 1 — ShipD Asset Ingestion Notes

## 1. Assets Collected (stored outside Git tracking)
- `temp/ShipD/X_LABELS.npy` — canonical list of 45 ShipD design parameters.
- `temp/ShipD/InputVectors_30k.npy` — dataset of 30,000 normalized parameter vectors.
- `temp/ShipD/ShipD_Dataset_Bagazinski_and_Ahmed_2023.pdf` — primary reference describing the parameterization and constraint set.
- `temp/ShipD/InputVectors_stats.csv` — locally generated summary (min/max/mean/std) for each parameter.
- `temp/ShipD/compute_stats.py` — temporary helper script used to derive the statistics (delete after use if desired).

## 2. Dataset Shape & Coverage
- Vectors: **30,000** samples.
- Parameters: **45** features, matching the label file ordering.
- Principal dimension parameters (`LOA`, `Lb`, `Ls`, `Bd`, `Dd`, `WL`, `Bc`) show expected normalized ranges (0–1 or narrow bands), confirming dataset already scaled to ShipD conventions.
- Bow/mid/stern shaping parameters span roughly `[-4, 4]`, aligning with ShipD's use of signed polynomial coefficients.
- Angle parameter `Beta` spans `0–45°` (normalized), consistent with bow flare/entry limits cited in the paper.
- Stability/appendage flags (e.g., `BK_z`, `Kappa_bow`) remain within `[0,1]`, indicating they are stored as normalized magnitudes rather than booleans.

## 3. Quick Parameter Index Map
The first ten parameters (for reference):

| Index | Label | Notes |
|-------|-------|-------|
| 00 | `LOA` | Baseline reference length (normalized constant = 10.0) |
| 01 | `Lb` | Bow length ratio |
| 02 | `Ls` | Stern length ratio |
| 03 | `Bd` | Design beam ratio |
| 04 | `Dd` | Design draft ratio |
| 05 | `Bs` | Bulb scaling parameter |
| 06 | `WL` | Waterline length ratio |
| 07 | `Bc` | Beam at chine |
| 08 | `Beta` | Bow flare angle (degrees) |
| 09 | `Rc` | Bow curvature coefficient |

Full listing with statistics is available in `temp/ShipD/InputVectors_stats.csv`.

## 4. Implications for Taxonomy Mapping
- **Principal Dimensions (idx 0–7):** Remain global inputs; taxonomy will gate their default ranges rather than toggle availability.
- **Bow Cluster (approx. idx 8–20):** Align with bow family dropdown; each family will select a subset/mask of these coefficients.
- **Midship Cluster (idx 21–30 approx.):** Maps to section fullness, tumblehome, camber parameters; tie to midship family selection.
- **Stern Cluster (idx 31–42):** Supports transom, rake, and appendage geometry — needed for stern family dropdown.
- **Appendage/Trait Flags (remaining indices):** Provide toggles for bulb, skeg, appendages; should be surfaced conditionally when families require them.

## 5. Next Steps
- Incorporate the parameter/constraint insights from the ShipD PDF into backend validator design.
- Use the statistics file to seed default ranges in DataService when we model taxonomy + parameter metadata.
- Plan UI copy/tooltips using the label names and observed ranges to help users understand each control.


