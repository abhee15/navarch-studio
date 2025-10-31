NavArch Studio — Data Sourcing Templates
=================================================
This folder contains CSV templates to standardize ingestion of public datasets.

FILES
-----
1. principal_particulars_template.csv
   - One row per vessel
   - Required: L_pp_m, B_m, T_m, Cb (others optional)

2. offsets_grid_template.csv
   - Long-form grid: (station_index, waterline_index) -> y_halfbreadth_m
   - Coordinates recommended non-dimensionalized by Lpp and T from source

3. appendages_manifest_template.csv
   - List appendage CAD files (rudder, skeg, bilge keel, stabilizers, etc.)

4. wageningen_b_series_template.csv
   - Open-water curves: populate J, KT, KQ, eta0 for each (Z, AE/A0, P/D, Re)

5. water_properties_fresh_template.csv
   - Freshwater density/viscosity vs temperature (0–30 C) from ITTC Table 1

6. water_properties_seawater35psu_template.csv
   - Seawater (35 PSU) density/viscosity vs temperature (0–30 C) from ITTC Table 2

INGESTION NOTES
---------------
- Preserve original coordinate conventions; record any conversions in 'notes'.
- Keep original files in ./raw/<dataset> and put normalized CSVs in ./normalized/.
- Add citation to 'source' column with exact page/URL and retrieval date.
- For offsets IGES: if only IGES is available, export offset stations at your preferred spacing and save as offsets_grid_template.csv.

