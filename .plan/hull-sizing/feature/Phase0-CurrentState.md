# Phase 0 — Current-State Assessment Notes

## 1. Repository Guardrails Reviewed
- `.cursor/rules/debugging-methodology.md` reinforces Infra → Config → App triage order; keep this in mind when altering solver/data flows or diagnosing future regressions.
- UI changes must respect the design system per `.cursor/rules/design-system.md`; leverage existing Tailwind tokens and component patterns.
- Frontend requirements: React 18 + MobX + axios, named exports, no inline styles, TypeScript strict.
- Backend requirements: .NET 8, async/await everywhere, controller → service → repository layering, DTOs as records, cancellation tokens mandatory.
- Data additions should route through DataService to maintain centralized provenance; avoid ad-hoc CSV ingestion in HullSizingService going forward.

## 2. HullSizingService Snapshot
- `SizingDbContext` targets schema `sizing`; key tables (`mission_cases`, `sizing_runs`, `candidate_designs`, `hull_family_presets`, `vessel_catalog`, `kpi_weights`, `iso_containers`, `push_operations`) already modeled with numeric precision + indexes.
- Current hull family selection is coarse (`HullFamilyPreset` seeded ranges, filtered mostly by Froude bounds). No taxonomy (category/type/bow-mid-stern) fields exist yet.
- Services:
  - `HullFamilyService` simply filters active presets; expands to incorporate taxonomy/ShipD metadata.
  - Solver pipeline split into FirstPrinciples/DataDriven/Ml modules under `Services/Solver` & `Services/DataDriven`.
- Seeds reference CSV heritage (`hull_family_presets_extended.csv` per model summary) but seeding now appears baked into EF migration / code; need to confirm and plan transition to DataService-hosted metadata.

## 3. DataService Snapshot
- `CatalogSeeder` seeds water properties, propeller series, template hulls, benchmark particulars via code (no runtime CSV parsing). `vessel_catalog_curated_600.csv` still exists—verify whether it is legacy.
- Opportunity: host ShipD parameter labels/metadata and vessel taxonomy tables here so HullSizingService consumes via API instead of local files.
- Need to evaluate existing endpoints for distributing catalog data; ensure new metadata fits established patterns (system data flagged via `IsSystemData`, etc.).

## 4. Frontend Wizard Baseline
- Mission wizard (`pages/sizing/MissionWizard.tsx`) currently has 4 steps:
  1. `Step1MissionCargo` (mission name, missionType, cargo fields).
  2. `Step2SpeedEnvironment` (service speed, margins, sea state).
  3. `Step3Constraints` (dimension caps).
  4. `Step4Options` (solver mode, advanced options).
- Step 1 `missionType` select options: commercial, government, pleasure, research. No dependent vessel-type/bow/mid/stern selectors yet.
- Payload uses `CreateMissionCaseDto`; new taxonomy inputs will require DTO/store/API updates plus potential wizard step restructuring (Phase 5).

## 5. Existing Planning Artifacts
- `.plan/hull-sizing/plan/*` provides legacy phased roadmap focused on initial service bring-up; useful references but predates ShipD/taxonomy concept.
- `PHASE1-BACKEND-COMPLETE.md` and other status docs indicate portions of baseline MVP already delivered; ensure new taxonomy changes integrate without regressing completed work.

## 6. Next Discovery Actions
1. Inspect `Shared/Models/Sizing/*` DTOs to catalog fields needing taxonomy extensions.
2. Review current API contracts (`Controllers/*.cs`, `Shared/DTOs/Sizing`) for mission creation and solver runs.
3. Inventory DataService endpoints delivering catalog/reference data; identify candidate endpoint for ShipD metadata or plan new one.
4. Gather solver expectations for geometry vectors (FirstPrinciples/DataDriven/ML) to scope adapter changes.

_Prepared: 2025-11-12_


