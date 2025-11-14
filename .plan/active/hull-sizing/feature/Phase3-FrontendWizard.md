# Phase 3 — Frontend Wizard Taxonomy Plan

## Goals
- Surface the new vessel taxonomy selection (Category → Vessel Type → Bow/Midship/Stern families) in the Hull Sizing wizard.
- Fetch ShipD metadata from DataService (`/api/v1/shipd/parameters`, `/api/v1/shipd/taxonomy`) and hydrate MobX store state so dropdowns stay synchronized with backend defaults.
- Send the expanded payload (`missionCategory`, `vesselType`, `bowFamily`, `midshipFamily`, `sternFamily`, `familyMaskVersion`, `shipdInputVectorJson`) when creating missions and sizing runs.
- Display backend validation errors/warnings from the new ShipD adapter/validator (e.g., missing families, fallback defaults) to guide the user.

## Implementation Outline
1. **API Layer & Types**
   - Extend `frontend/src/services/sizingApi.ts` to call new ShipD metadata endpoints and include taxonomy fields in mission/run DTOs.
   - Update `frontend/src/types/sizing.ts` to match backend DTO shape additions (missionCategory, families, maskVersion, ShipD payload).
2. **MobX Store Enhancements**
   - Add observable state for `vesselCategories`, `vesselTypesByCategory`, and `familiesByType`.
   - Load metadata on store initialization or when the wizard mounts, with graceful fallback if endpoints return empty arrays.
   - Track the selected taxonomy fields in the store; auto-clear/reset dependent choices when category/type changes.
3. **Wizard Flow Update**
   - Restructure Step 1 to collect Category + Vessel Type (dependent dropdowns).
   - Introduce a dedicated step (or sub-section) for Bow/Midship/Stern family selection with filtered options based on selected type.
   - Surface ShipD warnings (from run response) in the wizard summary/diagnostics area.
4. **Validation & UX**
   - Disable “Next” or show inline errors when families are missing.
   - Use helper text referencing ShipD descriptions/ranges (if metadata contains descriptions, show them as tooltips).
   - Ensure existing mission editing/cloning flows populate the new fields.
5. **Regression Hooks**
   - Update integration tests (manual/automated) to assert new fields are persisted.
   - Capture sample metadata JSON in `temp/` for local testing while DataService tables are empty.

## Dependencies
- DataService metadata endpoints already scaffolded; seeding pending.
- Backend HullSizingService expects taxonomy fields; plan to warn when defaults are used.

## Next Actions
- [ ] Update `types/sizing.ts` and API client.
- [ ] Implement metadata load + observable state in `SizingStore`.
- [ ] Refactor wizard components (Step1, new Step2).
- [ ] Wire payload submission & error display.
- [ ] Smoke test mission creation + solver run with taxonomy selections.

