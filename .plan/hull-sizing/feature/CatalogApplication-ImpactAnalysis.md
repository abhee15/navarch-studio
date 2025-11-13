# Catalog Application Impact Analysis - ShipD Taxonomy Integration

## Overview

This document analyzes the impact of ShipD taxonomy changes on the Catalog application, which provides reference vessel data for the Data-Driven solver and user browsing.

## Current Catalog Architecture

### Entities

1. **`CatalogVesselReal`** (Real-World Vessels)
   - **Location**: `backend/Shared/Models/CatalogVesselReal.cs`
   - **Schema**: `catalog_user.vessels_real`
   - **Purpose**: Stores 600+ real-world vessels with actual dimensions and form coefficients
   - **Key Field**: `VesselType` (string) - Free-form values like:
     - "Container"
     - "Tanker"
     - "Bulk carrier"
     - "Cruise ship"
     - "Naval combatant"
     - etc.

2. **`ParametricHull`** (ShipD Parametric Hulls)
   - **Location**: `backend/Shared/Models/ParametricHull.cs`
   - **Schema**: `catalog_ml.parametric_hulls`
   - **Purpose**: Stores 82K synthetic hulls from MIT ShipD dataset
   - **Key Field**: `ParametricVector` (JSONB) - 45-parameter vector
   - **Note**: Does NOT have `VesselType` field - only parametric data

### Services

1. **`RealWorldKnnService`** (KNN Search)
   - **Location**: `backend/DataService/Services/Catalog/RealWorldKnnService.cs`
   - **Purpose**: Finds similar vessels using K-Nearest Neighbors algorithm
   - **Key Logic**: Filters by `VesselType` using case-insensitive string match:
     ```csharp
     .Where(v => v.VesselType.Equals(criteria.VesselType, StringComparison.OrdinalIgnoreCase))
     ```
   - **Fallback**: If < 3 matches, expands search to all vessel types

2. **`DataDrivenRealWorldSolver`** (Hull Sizing Solver)
   - **Location**: `backend/HullSizingService/Services/DataDriven/DataDrivenRealWorldSolver.cs`
   - **Purpose**: Uses catalog vessels as reference for generating candidates
   - **Key Logic**: Passes `MissionCase.MissionType` as `VesselType` to KNN search:
     ```csharp
     VesselType = mission.MissionType,  // Line 136
     ```

### Controllers

1. **`CatalogVesselsController`**
   - **Endpoint**: `POST /api/v1/catalog/vessels/search-similar`
   - **Uses**: `RealWorldKnnService` for KNN search
   - **Filters**: By `VesselType` from request

2. **`CatalogHullsController`**
   - **Endpoint**: `GET /api/v1/catalog/hulls?vesselType={type}`
   - **Filters**: By `VesselType` query parameter
   - **Returns**: List of catalog vessels matching type

## Impact Analysis

### Problem 1: Vessel Type Mismatch

**Current State**:
- `CatalogVesselReal.VesselType`: Free-form strings ("Container", "Tanker", "Bulk carrier")
- `MissionCase.MissionType`: Legacy field, may contain old values
- **New ShipD Taxonomy**: Structured values ("container", "bulk_carrier", "tanker")

**Impact**:
- ✅ **Case-insensitive match works**: "container" matches "Container"
- ❌ **Underscore mismatch**: "bulk_carrier" does NOT match "Bulk carrier"
- ❌ **Space mismatch**: "general_cargo" does NOT match "General cargo"
- ❌ **Naming inconsistency**: Catalog may use different terminology

**Example Failure**:
```csharp
// MissionCase.MissionType = "bulk_carrier" (from ShipD taxonomy)
// CatalogVesselReal.VesselType = "Bulk carrier" (from catalog)
// Result: No matches found, falls back to all types (less accurate)
```

### Problem 2: Missing Taxonomy Fields

**Current State**:
- `CatalogVesselReal` has NO fields for:
  - `VesselCategory` (Commercial, Government, Recreational, Research)
  - `BowFamily`, `MidshipFamily`, `SternFamily`
  - `FamilyMaskVersion`
  - `ShipdParametersJson`

**Impact**:
- Cannot filter catalog by ShipD taxonomy categories
- Cannot search catalog by hull families (bow/mid/stern)
- Cannot use catalog vessels as ShipD parameter reference
- Cannot display ShipD taxonomy info in catalog UI

### Problem 3: ParametricHull Integration Gap

**Current State**:
- `ParametricHull` stores 45-parameter vectors but has NO taxonomy fields
- Cannot map parametric hulls to ShipD taxonomy categories/types
- Cannot filter parametric hulls by bow/mid/stern families

**Impact**:
- Parametric hull catalog cannot leverage ShipD taxonomy for filtering
- Users cannot browse parametric hulls by ShipD categories
- Data-Driven parametric solver cannot use taxonomy for better matching

### Problem 4: UI/UX Inconsistency

**Current State**:
- Catalog browser shows `VesselType` as free-form string
- No category grouping (Commercial, Government, etc.)
- No hull family information displayed

**Impact**:
- Users see inconsistent terminology between Hull Sizing wizard and Catalog
- Cannot filter catalog by ShipD taxonomy
- Missing context about hull families for catalog vessels

## Required Changes

### Phase 1: Vessel Type Mapping (High Priority)

#### 1.1 Create Vessel Type Mapping Service
**File**: `backend/DataService/Services/Catalog/IVesselTypeMapper.cs` (NEW)

**Purpose**: Map between ShipD taxonomy vessel types and catalog vessel types

**Interface**:
```csharp
public interface IVesselTypeMapper
{
    /// <summary>
    /// Maps ShipD taxonomy vessel type to catalog vessel type(s)
    /// </summary>
    List<string> MapToCatalogTypes(string shipdVesselType);
    
    /// <summary>
    /// Maps catalog vessel type to ShipD taxonomy vessel type
    /// </summary>
    string? MapToShipDType(string catalogVesselType);
    
    /// <summary>
    /// Normalizes vessel type for comparison (handles spaces, underscores, case)
    /// </summary>
    string NormalizeVesselType(string vesselType);
}
```

**Implementation Strategy**:
- Create mapping dictionary for known mappings:
  ```csharp
  private static readonly Dictionary<string, List<string>> ShipDToCatalogMap = new()
  {
      { "container", new() { "Container", "Container ship" } },
      { "bulk_carrier", new() { "Bulk carrier", "Bulk", "Bulkcarrier" } },
      { "tanker", new() { "Tanker", "Crude oil tanker", "Product tanker" } },
      { "general_cargo", new() { "General cargo", "Cargo", "Cargo ship" } },
      { "fishing", new() { "Fishing", "Fishing vessel", "Trawler" } },
      { "yacht", new() { "Yacht", "Sailing yacht", "Motor yacht" } },
      { "cruise_vessel", new() { "Cruise ship", "Cruise", "Passenger ship" } },
      { "passenger_vessel", new() { "Passenger ship", "Ferry", "Passenger" } },
      { "cutters", new() { "Naval combatant", "Cutter", "Coast guard" } },
      { "general_military", new() { "Naval combatant", "Warship", "Military" } },
      { "research_vessel", new() { "Research vessel", "Research", "Survey ship" } },
      // ... more mappings
  };
  ```
- Use fuzzy matching for unknown types (normalize spaces/underscores/case)
- Log unmapped types for manual review

#### 1.2 Update RealWorldKnnService
**File**: `backend/DataService/Services/Catalog/RealWorldKnnService.cs`

**Changes**:
```csharp
public async Task<List<SimilarVessel>> FindSimilarVesselsAsync(
    MissionSearchCriteria criteria,
    int K = 5,
    CancellationToken cancellationToken = default)
{
    // ... existing code ...
    
    // NEW: Map ShipD vessel type to catalog vessel types
    var catalogTypes = _vesselTypeMapper.MapToCatalogTypes(criteria.VesselType);
    
    // Filter by mapped catalog types (OR logic)
    var sameType = catalog
        .Where(v => catalogTypes.Contains(
            _vesselTypeMapper.NormalizeVesselType(v.VesselType),
            StringComparer.OrdinalIgnoreCase))
        .ToList();
    
    // ... rest of existing code ...
}
```

#### 1.3 Update DataDrivenRealWorldSolver
**File**: `backend/HullSizingService/Services/DataDriven/DataDrivenRealWorldSolver.cs`

**Changes**:
- No changes needed - already passes `MissionType` correctly
- Mapping happens in `RealWorldKnnService`

### Phase 2: Extend Catalog Schema (Medium Priority)

#### 2.1 Add Taxonomy Fields to CatalogVesselReal
**Migration**: `backend/DataService/Migrations/YYYYMMDDHHMMSS_AddShipDTaxonomyToCatalog.cs` (NEW)

**Changes**:
```csharp
// Add nullable columns to vessels_real table
ALTER TABLE catalog_user.vessels_real
    ADD COLUMN vessel_category VARCHAR(50) NULL,  -- Commercial, Government, etc.
    ADD COLUMN shipd_vessel_type VARCHAR(50) NULL,  -- Normalized ShipD type
    ADD COLUMN bow_family VARCHAR(50) NULL,
    ADD COLUMN midship_family VARCHAR(50) NULL,
    ADD COLUMN stern_family VARCHAR(50) NULL,
    ADD COLUMN family_mask_version INT NULL DEFAULT 1,
    ADD COLUMN shipd_parameters_json JSONB NULL;  -- If we can derive from geometry

// Create index for faster filtering
CREATE INDEX ix_vessels_real_shipd_taxonomy 
    ON catalog_user.vessels_real(vessel_category, shipd_vessel_type);
```

**Model Update**: `backend/Shared/Models/CatalogVesselReal.cs`
```csharp
// Add new properties
public string? VesselCategory { get; set; }
public string? ShipdVesselType { get; set; }
public string? BowFamily { get; set; }
public string? MidshipFamily { get; set; }
public string? SternFamily { get; set; }
public int? FamilyMaskVersion { get; set; }
public string? ShipdParametersJson { get; set; }
```

#### 2.2 Create Catalog Taxonomy Seeder
**File**: `backend/DataService/Services/Catalog/CatalogTaxonomySeeder.cs` (NEW)

**Purpose**: Backfill taxonomy fields for existing catalog vessels

**Strategy**:
1. Use `IVesselTypeMapper` to map `VesselType` → `ShipdVesselType`
2. Infer `VesselCategory` from `ShipdVesselType` using ShipD taxonomy
3. For hull families: Use heuristics or manual labeling:
   - Analyze form coefficients (Cb, Cp) to infer families
   - Use vessel type → family mapping from ShipD taxonomy
   - Mark as "unknown" if cannot determine
4. Store results in database

**Implementation**:
```csharp
public async Task SeedTaxonomyAsync(CancellationToken cancellationToken)
{
    var vessels = await _context.CatalogVesselsReal
        .Where(v => v.ShipdVesselType == null)
        .ToListAsync(cancellationToken);
    
    foreach (var vessel in vessels)
    {
        // Map vessel type
        vessel.ShipdVesselType = _vesselTypeMapper.MapToShipDType(vessel.VesselType);
        
        // Get taxonomy entry
        var taxonomy = await _context.ShipDVesselTaxonomies
            .FirstOrDefaultAsync(t => t.VesselType == vessel.ShipdVesselType, cancellationToken);
        
        if (taxonomy != null)
        {
            vessel.VesselCategory = taxonomy.Category;
            // Infer families from form coefficients or use defaults
            vessel.BowFamily = InferBowFamily(vessel, taxonomy);
            vessel.MidshipFamily = InferMidshipFamily(vessel, taxonomy);
            vessel.SternFamily = InferSternFamily(vessel, taxonomy);
            vessel.FamilyMaskVersion = taxonomy.FamilyMaskVersion;
        }
    }
    
    await _context.SaveChangesAsync(cancellationToken);
}
```

#### 2.3 Update Catalog Controllers
**File**: `backend/DataService/Controllers/CatalogVesselsController.cs`

**Changes**:
- Add query parameters for ShipD taxonomy filtering:
  ```csharp
  [HttpGet]
  public async Task<ActionResult> ListVessels(
      [FromQuery] string? vesselCategory = null,
      [FromQuery] string? shipdVesselType = null,
      [FromQuery] string? bowFamily = null,
      [FromQuery] string? midshipFamily = null,
      [FromQuery] string? sternFamily = null)
  {
      var query = _context.CatalogVesselsReal.AsQueryable();
      
      if (!string.IsNullOrEmpty(vesselCategory))
          query = query.Where(v => v.VesselCategory == vesselCategory);
      
      if (!string.IsNullOrEmpty(shipdVesselType))
          query = query.Where(v => v.ShipdVesselType == shipdVesselType);
      
      // ... filter by families ...
      
      return Ok(await query.ToListAsync());
  }
  ```

### Phase 3: ParametricHull Taxonomy (Low Priority)

#### 3.1 Add Taxonomy Fields to ParametricHull
**Migration**: `backend/DataService/Migrations/YYYYMMDDHHMMSS_AddShipDTaxonomyToParametricHulls.cs` (NEW)

**Changes**:
```csharp
ALTER TABLE catalog_ml.parametric_hulls
    ADD COLUMN vessel_category VARCHAR(50) NULL,
    ADD COLUMN shipd_vessel_type VARCHAR(50) NULL,
    ADD COLUMN bow_family VARCHAR(50) NULL,
    ADD COLUMN midship_family VARCHAR(50) NULL,
    ADD COLUMN stern_family VARCHAR(50) NULL;

CREATE INDEX ix_parametric_hulls_shipd_taxonomy 
    ON catalog_ml.parametric_hulls(vessel_category, shipd_vessel_type);
```

**Note**: Parametric hulls may need classification algorithm to infer taxonomy from parameter vectors.

#### 3.2 Create Parametric Hull Classifier
**File**: `backend/DataService/Services/Catalog/ParametricHullClassifier.cs` (NEW)

**Purpose**: Classify parametric hulls into ShipD taxonomy based on parameter vectors

**Strategy**:
- Use parameter ranges to infer vessel type
- Use geometric measures (Cb, Cp, LbRatio, LsRatio) to infer families
- May require ML model or rule-based heuristics

### Phase 4: Frontend Updates (Medium Priority)

#### 4.1 Update Catalog Browser UI
**File**: `frontend/src/pages/catalog/CatalogBrowserV2.tsx`

**Changes**:
- Add ShipD taxonomy filters (Category, Vessel Type, Families)
- Show taxonomy badges on catalog cards
- Group catalog by category
- Show hull families when available

#### 4.2 Update Catalog API Client
**File**: `frontend/src/services/catalogApi.ts`

**Changes**:
- Add methods for taxonomy-filtered searches
- Include taxonomy fields in response types

## Migration Strategy

### Step 1: Non-Breaking Changes (Immediate)
1. ✅ Create `IVesselTypeMapper` service
2. ✅ Update `RealWorldKnnService` to use mapper
3. ✅ Test KNN search with ShipD taxonomy types
4. ✅ Verify backward compatibility (old `MissionType` values still work)

### Step 2: Schema Extension (Next Sprint)
1. ✅ Create migration for taxonomy fields (nullable)
2. ✅ Update `CatalogVesselReal` model
3. ✅ Create `CatalogTaxonomySeeder`
4. ✅ Run seeder to backfill existing vessels
5. ✅ Update DTOs and controllers

### Step 3: UI Enhancement (Future)
1. ✅ Update catalog browser with taxonomy filters
2. ✅ Add taxonomy display to catalog cards
3. ✅ Test end-to-end filtering

## Testing Strategy

### Unit Tests
1. **VesselTypeMapperTests**
   - Test all ShipD → Catalog mappings
   - Test normalization (spaces, underscores, case)
   - Test unknown types (fuzzy matching)

2. **RealWorldKnnServiceTests**
   - Test filtering with ShipD taxonomy types
   - Test fallback when no matches
   - Test with mixed catalog types

3. **CatalogTaxonomySeederTests**
   - Test taxonomy inference logic
   - Test family inference from form coefficients
   - Test backfill accuracy

### Integration Tests
1. **CatalogSearchIntegrationTests**
   - Test KNN search with ShipD taxonomy
   - Test catalog filtering by taxonomy
   - Test backward compatibility

2. **DataDrivenSolverIntegrationTests**
   - Test solver with ShipD taxonomy mission cases
   - Verify catalog matching works correctly
   - Test fallback scenarios

### E2E Tests
1. **CatalogBrowserE2ETests**
   - Test taxonomy filtering in UI
   - Test catalog browsing with taxonomy
   - Test catalog → vessel cloning with taxonomy

## Risk Assessment

### Low Risk
- ✅ Vessel type mapping (non-breaking, additive)
- ✅ Schema extension (nullable columns, backward compatible)

### Medium Risk
- ⚠️ Taxonomy inference accuracy (may need manual review)
- ⚠️ Performance impact of additional filters

### High Risk
- ❌ None identified - all changes are backward compatible

## Success Criteria

1. ✅ KNN search works with ShipD taxonomy vessel types
2. ✅ Catalog can be filtered by ShipD taxonomy
3. ✅ Catalog vessels display taxonomy information
4. ✅ Backward compatibility maintained (old `MissionType` values work)
5. ✅ No performance degradation in catalog queries
6. ✅ Taxonomy fields populated for >90% of catalog vessels

## Future Enhancements

1. **ML-Based Classification**: Train model to classify catalog vessels by parameter vectors
2. **Taxonomy Validation**: UI for users to correct/validate taxonomy assignments
3. **Cross-Reference**: Link catalog vessels to ShipD taxonomy entries
4. **Parameter Extraction**: Extract ShipD parameters from catalog vessel geometry
5. **Family Inference**: Use geometry analysis to infer bow/mid/stern families

## Dependencies

- ✅ ShipD taxonomy must be seeded first (already done)
- ✅ `IVesselTypeMapper` must be created before updating services
- ✅ Migration must run before seeder
- ✅ Seeder must run before UI updates

## Timeline Estimate

- **Phase 1 (Vessel Type Mapping)**: 2-3 days
- **Phase 2 (Schema Extension)**: 3-4 days
- **Phase 3 (ParametricHull)**: 5-7 days (optional, can defer)
- **Phase 4 (Frontend)**: 2-3 days

**Total**: ~2 weeks for Phases 1, 2, 4 (essential changes)
