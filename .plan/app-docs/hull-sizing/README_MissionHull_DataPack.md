# Mission→Hull Sizing – Data Pack (Free-Friendly)

This folder contains CSV/YAML templates and seed files to quickly stand up the independent Mission→Hull Sizing app,
focused on **free** sources and CAD interoperability.

## Folder ingestion order
1. `hull_family_presets_extended.csv` – ranges for L/B, B/T, D/T, Cb, Cp, Cwp by family.
2. `type_inference_rules.csv` – quick inference of candidate families from Fn + payload + constraints.
3. `iso_containers.csv` – standard container sizes for TEU fit (UI defaults).
4. `water_properties.csv` and `water_properties_salinity_template.csv` – seawater ρ and ν (derive/interpolate).
5. `sea_state_template.csv` – add your Hs/Tz pairs and labels.
6. `vessel_catalog_seed.csv` – register KCS/KVLCC2/Series-60/FAO rows (fill real values from sources).
7. `dataset_registry_template.csv` / `license_registry.csv` – track provenance and license limits.
8. `holtrop_input_template.csv` / `savitsky_input_template.csv` – solver inputs if you decouple UI and solver.
9. `offsets_template.csv` – grid for manual offsets import; keep x/Lpp & z/T normalized.
10. `export_layers_dxf.csv` – DXF layer names/colors for clean drawings in AutoCAD.
11. `kpi_weights.csv` – objective weights for scoring candidates.
12. `rendering_presets.csv` – LOD & sampling for fast, smooth interactions.
13. `ui_defaults.csv`, `keyboard_shortcuts.csv`, `localization_strings.csv` – polish & UX.
14. `canal_rules_template.csv`, `freeboard_presets_template.csv` – constraints and depth/freeboard heuristics.

## Where to place free geometry
- Put KCS/KVLCC2 **IGES** files under `data/iges/` and register them in `vessel_catalog_seed.csv` (source fields filled).
- For Series-60 and FAO examples, register principal dimensions/ratios; keep any scans internal-only.

## Import steps
- Create schema `sizing` and tables (see EPIC in canvas).
- Bulk import CSVs in this order: families → rules → ISO/water → catalog → weights → presets → UI.
- Verify `dataset_registry_template.csv` entries and fill `url` + `checksum_sha256` after download.

## CAD round-trip
- For export: DXF (2D plan/sections), IGES (NURBS surfaces). STEP/SAT optional.
- For import: IGES/STEP/SAT → fit to parametric hull (least-squares) → update (L,B,T,Cb,Cp).

## Notes
- All numeric ranges are **starting bands** for early-phase design; refine as you curate data.
- Keep everything **at system units**; avoid per-field units in UI forms.
- When in doubt, lock **Fn** and **L/B** during interactive edits for stability.