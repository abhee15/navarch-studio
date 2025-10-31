# NavArch Studio — Data Submission Guide

You can provide the following artifacts by uploading files here (drag & drop), or by sharing a link. Use these templates for predictable ingestion.

## 1) Real KCS Reference Data
- File: `KCS_reference_template.csv` (rename to your dataset name, e.g., `KCS_reference_published_YYYY.csv`)
- Required columns: `speed_mps`, `RT_ref_N`
- Recommended: `speed_kts`, `Fn`, `LWL_m`, `rho_kg_per_m3`, `nu_m2_per_s`, `source_citation`
- Units: SI for numeric fields (m/s, N, m, kg/m^3, m^2/s).
- Tip: If your source is in knots or model-scale, include `scale = model|full` and supply `LWL_m` to compute Fn consistently.

## 2) Water Properties Lookup Table
- File: `WaterProperties_lookup_template.csv`
- Columns: `temp_C`, `salinity_ppt`, `rho_kg_per_m3`, `nu_m2_per_s`, `source_citation`, `notes`
- Provide rows for the temperatures & salinities you care about (e.g., 0–35°C, 25–35 ppt).

## 3) Specific HM Formulas/Documentation
- File: `HM_method_config.yaml` plus **PDFs** or citations to the exact variant you want (e.g., H-M 1982).
- In `HM_method_config.yaml`, select strategy for form factor `(k)`, the correlation allowance, transom correction, and air-resistance defaults.
- Attach the source PDF(s) or links so we lock implementation to your exact reference.

## 4) Appendage Calculation Details
- File: `Appendages_details_template.csv`
- For **generic** treatment: set `mode: generic_factor` in `HM_method_config.yaml` and give `generic_factor` (e.g., 1.05).
- For **detailed** treatment: set `mode: detailed_list` and fill one row per appendage with geometry and either `drag_coeff_Cd` or `friction_mult_factor`. Mark `include=true` for items to apply.

## Delivery Checklist
- ✅ Keep units SI in CSVs.
- ✅ One header row; commas as separators; UTF‑8 encoding.
- ✅ Include a `source_citation` for traceability (title/DOI/report link).
- ✅ If multiple datasets: include `dataset_id` and `version` columns.
- ✅ Zip everything and upload if you have many files.

## Suggested Repo Structure (optional)
```
/data
  /benchmarks
    KCS_reference_published_2025.csv
  /fluids
    WaterProperties_lookup.csv
/config
  HM_method_config.yaml
/appendages
  Appendages_details.csv
/docs
  HM_reference_paper.pdf
```