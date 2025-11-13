# Phase 2 — Backend Adapter & Validation Progress

## Overview
- Added EF Core migrations for both DataService and HullSizingService to store ShipD taxonomy metadata and ShipD-specific fields on missions, runs, and candidates.
  - DataService: `shipd_parameter_metadata` / `shipd_vessel_taxonomy` scaffolding with API surface (`/api/v1/shipd/parameters`, `/api/v1/shipd/taxonomy`).
  - HullSizingService: taxonomy columns plus JSON payload storage (`shipd_inputs_json`, `shipd_input_vector_json`, `shipd_parameters_json`).
- Introduced a provisional ShipD adapter/validator layer to keep the pipeline compiling while we work toward full metadata ingestion.
  - `ShipDParameterAdapter` builds a 45-length vector (placeholder) and preserves UI selections.
  - `ShipDConstraintValidator` enforces presence of bow/mid/stern families and vector length.
  - Both services are registered in DI and invoked from `SizingRunService` prior to solver execution.
- Serialized ShipD payloads now flow to mission cases, sizing runs, and candidate designs for provenance.
- Extended DTOs/entities to surface taxonomy selections (category, vessel type, families, mask version, ShipD JSON payload) so UI and downstream services can consume them later.
- HullSizingService now consults DataService metadata through the shared HTTP client (falling back to heuristics if tables are empty) to populate defaults and attach taxonomy-driven warnings.

## Current Limitations / TODO
- Adapter currently uses heuristics + user overrides; will transition to DataService-driven metadata once ShipD parameter labels & masks are exposed.
- Constraint validator only checks basic structure—needs algebraic ShipD constraints when metadata service lands.
- DataService still needs endpoints/seeders to publish canonical taxonomy + parameter stats.
- Solver still operates on mission case; future steps should integrate the generated ShipD vector into solver initialization.

## Next Steps
1. Seed DataService tables with canonical ShipD labels, ranges, and taxonomy defaults; add caching.
2. Expand validator with true ShipD constraint set once metadata is seeded.
3. Update solver adapters to consume ShipD vector directly (first-principles + data-driven).
4. Coordinate with frontend to send/receive new taxonomy fields and display validation feedback.
