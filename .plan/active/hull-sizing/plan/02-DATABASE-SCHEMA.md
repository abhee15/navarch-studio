# Database Schema - `sizing` Schema

## Overview
All tables reside in the `sizing` schema (isolated from `identity` and `data` schemas).

**Naming Convention:** snake_case (PostgreSQL standard)
**Numeric Precision:** 
- Lengths: `numeric(12,4)` (e.g., 9999.9999 m)
- Coefficients: `numeric(6,4)` (e.g., 0.9999)
- Mass/Displacement: `numeric(12,3)` (e.g., 999999.999 tonnes)

---

## Core Tables

### mission_cases

User-defined mission requirements (cargo, speed, environment, constraints).

```sql
CREATE TABLE sizing.mission_cases (
    -- Identity
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    tenant_id VARCHAR(50) NOT NULL,
    
    -- Basic info
    name VARCHAR(255) NOT NULL,
    mission_category VARCHAR(50), -- Commercial, Government, Pleasure
    mission_type VARCHAR(50) NOT NULL, -- container, tanker, bulk, fishing, yacht_disp, hsc_planing, etc.
    
    -- Cargo inputs (separated fields for clarity)
    cargo_basis VARCHAR(20) NOT NULL CHECK (cargo_basis IN ('volume', 'weight', 'teu')),
    cargo_value NUMERIC(12,2) CHECK (cargo_value >= 0),
    cargo_volume_m3 NUMERIC(12,2) CHECK (cargo_volume_m3 >= 0),
    cargo_density_t_per_m3 NUMERIC(6,3) CHECK (cargo_density_t_per_m3 > 0),
    teu_count INT CHECK (teu_count > 0),
    
    -- Speed & performance margins
    service_speed_kn NUMERIC(6,2) NOT NULL CHECK (service_speed_kn > 0),
    sea_margin_pct NUMERIC(5,2) DEFAULT 0.15 CHECK (sea_margin_pct >= 0 AND sea_margin_pct <= 1),
    service_margin_pct NUMERIC(5,2) DEFAULT 0.15 CHECK (service_margin_pct >= 0 AND service_margin_pct <= 1),
    
    -- Environment
    env_hs_m NUMERIC(6,2) CHECK (env_hs_m >= 0), -- Significant wave height
    env_tz_s NUMERIC(6,2) CHECK (env_tz_s > 0),  -- Wave period
    
    -- Design constraints (optional)
    cap_loa_m NUMERIC(8,2) CHECK (cap_loa_m > 0),
    cap_beam_m NUMERIC(8,2) CHECK (cap_beam_m > 0),
    cap_draft_m NUMERIC(6,2) CHECK (cap_draft_m > 0),
    cap_airdraft_m NUMERIC(6,2) CHECK (cap_airdraft_m > 0),
    
    -- Range requirement
    endurance_nm NUMERIC(8,2) CHECK (endurance_nm >= 0),
    
    -- Metadata
    notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at TIMESTAMPTZ -- Soft delete
);

-- Indexes
CREATE INDEX idx_mission_cases_user_id ON sizing.mission_cases(user_id) 
    WHERE deleted_at IS NULL;

CREATE INDEX idx_mission_cases_tenant_id ON sizing.mission_cases(tenant_id) 
    WHERE deleted_at IS NULL;

CREATE INDEX idx_mission_cases_mission_type ON sizing.mission_cases(mission_type) 
    WHERE deleted_at IS NULL;

-- Comments
COMMENT ON TABLE sizing.mission_cases IS 'User-defined mission requirements for hull sizing';
COMMENT ON COLUMN sizing.mission_cases.cargo_basis IS 'Primary input type: volume (m³), weight (tonnes), or teu (container count)';
COMMENT ON COLUMN sizing.mission_cases.sea_margin_pct IS 'Sea margin for resistance/power (typically 0.15 = 15%)';
COMMENT ON COLUMN sizing.mission_cases.service_margin_pct IS 'Service margin for power (typically 0.15 = 15%)';
```

---

### sizing_runs

Sizing computation runs (one mission case can have multiple runs with different options/locks).

```sql
CREATE TABLE sizing.sizing_runs (
    -- Identity
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    mission_case_id UUID NOT NULL REFERENCES sizing.mission_cases(id) ON DELETE CASCADE,
    
    -- Configuration
    mode VARCHAR(20) NOT NULL DEFAULT 'first_principles', -- first_principles | data_driven (Phase 2)
    locks_json JSONB, -- {keep_fn: true, keep_l_over_b: false, keep_b_over_t: false, ...}
    options_json JSONB, -- {family_hint: "container", ...}
    
    -- Execution tracking
    status VARCHAR(20) NOT NULL DEFAULT 'pending', -- pending | computing | completed | failed
    compute_time_ms INT,
    error_message TEXT,
    
    -- Metadata
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_sizing_runs_mission_case_id ON sizing.sizing_runs(mission_case_id);
CREATE INDEX idx_sizing_runs_status ON sizing.sizing_runs(status);
CREATE INDEX idx_sizing_runs_created_at ON sizing.sizing_runs(created_at DESC);

-- Comments
COMMENT ON TABLE sizing.sizing_runs IS 'Sizing computation runs with configuration and status tracking';
COMMENT ON COLUMN sizing.sizing_runs.locks_json IS 'Parameters locked during solver iterations (e.g., keep Froude number constant)';
COMMENT ON COLUMN sizing.sizing_runs.status IS 'Execution status: pending (queued), computing (in progress), completed (success), failed (error)';
```

---

### candidate_designs

Generated hull candidates (output of sizing solver).

```sql
CREATE TABLE sizing.candidate_designs (
    -- Identity
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sizing_run_id UUID NOT NULL REFERENCES sizing.sizing_runs(id) ON DELETE CASCADE,
    
    -- Hull type
    hull_family VARCHAR(50) NOT NULL, -- container, tanker, bulk, fishing, yacht_disp, hsc_planing, etc.
    rank INT NOT NULL, -- 1 = best, 2 = second, etc. (by score)
    is_selected BOOLEAN DEFAULT FALSE, -- User's chosen candidate
    
    -- Principal dimensions (numeric(12,4) for high precision)
    lpp_m NUMERIC(12,4) NOT NULL CHECK (lpp_m > 0), -- Length between perpendiculars
    lwl_m NUMERIC(12,4) NOT NULL CHECK (lwl_m > 0), -- Waterline length
    loa_m NUMERIC(12,4) NOT NULL CHECK (loa_m > 0), -- Length overall
    b_m NUMERIC(12,4) NOT NULL CHECK (b_m > 0),     -- Beam
    t_m NUMERIC(12,4) NOT NULL CHECK (t_m > 0),     -- Draft
    d_m NUMERIC(12,4) NOT NULL CHECK (d_m > 0),     -- Depth
    
    -- Form coefficients (numeric(6,4))
    cb NUMERIC(6,4) NOT NULL CHECK (cb > 0 AND cb <= 1),   -- Block coefficient
    cp NUMERIC(6,4) NOT NULL CHECK (cp > 0 AND cp <= 1),   -- Prismatic coefficient
    cwp NUMERIC(6,4) NOT NULL CHECK (cwp > 0 AND cwp <= 1), -- Waterplane coefficient
    cm NUMERIC(6,4) CHECK (cm > 0 AND cm <= 1),            -- Midship coefficient
    
    -- Mass & displacement (numeric(12,3))
    displacement_t NUMERIC(12,3) NOT NULL CHECK (displacement_t > 0),
    
    -- Speed characteristics (numeric(6,4))
    fn NUMERIC(6,4) NOT NULL CHECK (fn > 0),     -- Froude number
    lwl_over_lambda NUMERIC(6,3),                -- LWL/λ ratio (seakeeping screen)
    
    -- Resistance & power (numeric(10,2))
    ehp_kw NUMERIC(10,2) CHECK (ehp_kw >= 0),    -- Effective horsepower
    shp_kw NUMERIC(10,2) CHECK (shp_kw >= 0),    -- Shaft horsepower
    
    -- Stability estimates (numeric(8,3))
    gm_est_m NUMERIC(8,3),      -- Estimated transverse metacentric height
    kb_m NUMERIC(8,3),          -- Vertical center of buoyancy
    lcb_pct_lpp NUMERIC(6,3),   -- Longitudinal center of buoyancy (% of Lpp from AP)
    
    -- Scoring & validation
    scores_json JSONB, -- {delta_balance: 0.98, installed_power: 0.85, constraints_ok: 1.0, ...}
    flags_json JSONB,  -- {draft_exceeded: false, beam_exceeded: false, low_freeboard: true, ...}
    score NUMERIC(8,4) NOT NULL CHECK (score >= 0 AND score <= 1), -- Weighted composite score
    
    -- Geometry representation
    geometry_json JSONB, -- Offsets grid for 3D/2D rendering: {stations: [{x, waterlines: [{z, y}]}]}
    
    -- Metadata
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE INDEX idx_candidate_designs_sizing_run_id ON sizing.candidate_designs(sizing_run_id);
CREATE INDEX idx_candidate_designs_score_desc ON sizing.candidate_designs(score DESC);
CREATE INDEX idx_candidate_designs_rank ON sizing.candidate_designs(rank);
CREATE INDEX idx_candidate_designs_hull_family ON sizing.candidate_designs(hull_family);

-- Comments
COMMENT ON TABLE sizing.candidate_designs IS 'Generated hull candidates from sizing solver';
COMMENT ON COLUMN sizing.candidate_designs.rank IS 'Ranking within sizing run (1 = best by score)';
COMMENT ON COLUMN sizing.candidate_designs.is_selected IS 'User-selected candidate for further analysis';
COMMENT ON COLUMN sizing.candidate_designs.cb IS 'Block coefficient: Δ/(L×B×T×ρ)';
COMMENT ON COLUMN sizing.candidate_designs.cp IS 'Prismatic coefficient: Δ/(Am×L×ρ) where Am = midship area';
COMMENT ON COLUMN sizing.candidate_designs.cwp IS 'Waterplane area coefficient: Awp/(L×B)';
COMMENT ON COLUMN sizing.candidate_designs.cm IS 'Midship section coefficient: Am/(B×T)';
COMMENT ON COLUMN sizing.candidate_designs.fn IS 'Froude number: V/√(g×L)';
COMMENT ON COLUMN sizing.candidate_designs.geometry_json IS 'Parametric hull offsets grid for visualization';
```

---

## Reference Data Tables

### hull_family_presets

Hull type presets with geometric ratio ranges and coefficient bands (seeded from CSV).

```sql
CREATE TABLE sizing.hull_family_presets (
    -- Identity
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    family VARCHAR(50) UNIQUE NOT NULL, -- container, tanker, bulk, fishing, etc.
    display_name VARCHAR(100),
    
    -- Geometric ratio ranges
    l_over_b_min NUMERIC(5,2) NOT NULL,
    l_over_b_max NUMERIC(5,2) NOT NULL,
    b_over_t_min NUMERIC(5,2) NOT NULL,
    b_over_t_max NUMERIC(5,2) NOT NULL,
    d_over_t_min NUMERIC(5,2) NOT NULL,
    d_over_t_max NUMERIC(5,2) NOT NULL,
    
    -- Form coefficient ranges
    cb_min NUMERIC(5,3) NOT NULL,
    cb_max NUMERIC(5,3) NOT NULL,
    cp_min NUMERIC(5,3),
    cp_max NUMERIC(5,3),
    cwp_min NUMERIC(5,3),
    cwp_max NUMERIC(5,3),
    
    -- Froude number band (optional)
    fn_min NUMERIC(5,3),
    fn_max NUMERIC(5,3),
    
    -- Geometry generator type
    generator_type VARCHAR(50), -- wigley, series60, kcs_like, kvlcc2_like, planing
    
    -- Metadata
    is_active BOOLEAN DEFAULT TRUE,
    notes TEXT
);

-- Indexes
CREATE INDEX idx_hull_family_presets_family ON sizing.hull_family_presets(family);
CREATE INDEX idx_hull_family_presets_active ON sizing.hull_family_presets(is_active) 
    WHERE is_active = TRUE;

-- Comments
COMMENT ON TABLE sizing.hull_family_presets IS 'Hull type presets with geometric ranges (seeded from hull_family_presets_extended.csv)';
COMMENT ON COLUMN sizing.hull_family_presets.generator_type IS 'Parametric hull generator: wigley (fine), series60 (medium), kcs_like (container), kvlcc2_like (tanker), planing (HSC)';
```

**Seed Data (from CSV):**
- container, tanker, bulk, fishing, yacht_disp, hsc_planing, dredger, lpg_lng, roro_ropax, naval_patrol, catamaran_hsc, pontoon_barge

---

### vessel_catalog

Reference vessel data for data-driven sizing mode (Phase 2).

```sql
CREATE TABLE sizing.vessel_catalog (
    -- Identity
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    provenance VARCHAR(255), -- 'SIMMAN/NMRI', 'KRISO', 'Series-60', etc.
    vessel_type VARCHAR(50),
    
    -- Principal dimensions
    lpp_m NUMERIC(12,4),
    lwl_m NUMERIC(12,4),
    b_m NUMERIC(12,4),
    t_m NUMERIC(12,4),
    d_m NUMERIC(12,4),
    
    -- Form coefficients
    cb NUMERIC(6,4),
    cp NUMERIC(6,4),
    cwp NUMERIC(6,4),
    cm NUMERIC(6,4),
    
    -- Capacity & speed
    dwt_t NUMERIC(12,2),
    service_speed_kn NUMERIC(6,2),
    
    -- Provenance tracking
    notes TEXT,
    source_url VARCHAR(500),
    license_info TEXT
);

-- Indexes
CREATE INDEX idx_vessel_catalog_vessel_type ON sizing.vessel_catalog(vessel_type);
CREATE INDEX idx_vessel_catalog_provenance ON sizing.vessel_catalog(provenance);

-- Comments
COMMENT ON TABLE sizing.vessel_catalog IS 'Reference vessels for data-driven mode (KCS, KVLCC2, Series 60, etc.)';
COMMENT ON COLUMN sizing.vessel_catalog.provenance IS 'Data source: SIMMAN/NMRI (KCS), KRISO (KVLCC2), ITTC (Series 60), etc.';
```

**Initial Seed Data:**
- KCS (container)
- KVLCC2 (tanker)
- Series 60 variants (CB=0.60, 0.65, 0.70)

---

### kpi_weights

Scoring weights for multi-objective candidate ranking (user-specific or system default).

```sql
CREATE TABLE sizing.kpi_weights (
    -- Identity
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID, -- NULL = system default
    
    -- Weight configuration
    metric VARCHAR(50) NOT NULL, -- delta_balance, installed_power, constraints_ok, stability_screen, teu_or_volume_fit
    weight NUMERIC(5,3) NOT NULL CHECK (weight >= 0 AND weight <= 1),
    
    -- Constraint
    UNIQUE(user_id, metric)
);

-- Indexes
CREATE INDEX idx_kpi_weights_user_id ON sizing.kpi_weights(user_id);

-- Comments
COMMENT ON TABLE sizing.kpi_weights IS 'Scoring weights for multi-objective candidate ranking';
COMMENT ON COLUMN sizing.kpi_weights.user_id IS 'NULL = system default; user-specific weights override defaults';
```

**System Defaults (from kpi_weights.csv):**
```sql
INSERT INTO sizing.kpi_weights (user_id, metric, weight) VALUES
(NULL, 'delta_balance', 0.35),
(NULL, 'installed_power', 0.25),
(NULL, 'constraints_ok', 0.20),
(NULL, 'stability_screen', 0.10),
(NULL, 'teu_or_volume_fit', 0.10);
```

---

### push_operations

Idempotency tracking for "Push to Hydrostatics" operations.

```sql
CREATE TABLE sizing.push_operations (
    -- Identity
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    
    -- Idempotency
    idempotency_key VARCHAR(255) NOT NULL,
    
    -- Operation details
    candidate_id UUID NOT NULL REFERENCES sizing.candidate_designs(id),
    vessel_id UUID NOT NULL, -- Created vessel ID in data.vessels
    
    -- Metadata
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Indexes
CREATE UNIQUE INDEX idx_push_operations_idempotency_key ON sizing.push_operations(idempotency_key);
CREATE INDEX idx_push_operations_candidate_id ON sizing.push_operations(candidate_id);
CREATE INDEX idx_push_operations_vessel_id ON sizing.push_operations(vessel_id);

-- Comments
COMMENT ON TABLE sizing.push_operations IS 'Tracks "Push to Hydrostatics" operations for idempotency';
COMMENT ON COLUMN sizing.push_operations.idempotency_key IS 'Client-generated key to prevent duplicate vessel creation on retries';
```

---

## Seed Data Tables (Optional - can be config files)

These can be stored as static reference tables or config files. For MVP, we'll use tables.

### iso_containers

ISO standard container types for TEU-based sizing.

```sql
CREATE TABLE sizing.iso_containers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    container_type VARCHAR(10) NOT NULL, -- 20GP, 40GP, 40HC, 45HC
    length_mm INT NOT NULL,
    width_mm INT NOT NULL,
    height_mm INT NOT NULL,
    max_gross_kg INT NOT NULL
);

INSERT INTO sizing.iso_containers (container_type, length_mm, width_mm, height_mm, max_gross_kg) VALUES
('20GP', 6058, 2438, 2591, 30480),
('40GP', 12192, 2438, 2591, 30480),
('40HC', 12192, 2438, 2896, 30480),
('45HC', 13716, 2438, 2896, 32500);
```

### water_properties_cache

**NOTE:** Water properties are stored in `data.catalog_water_properties` (DataService owns this).
HullSizingService calls DataService via HTTP and caches results locally (in-memory, not DB).

**No table needed** - using IMemoryCache with 12-hour TTL.

---

## EF Core Configuration

### SizingDbContext.cs

```csharp
using Microsoft.EntityFrameworkCore;
using Shared.Models.Sizing;

namespace HullSizingService.Data;

public class SizingDbContext : DbContext
{
    public SizingDbContext(DbContextOptions<SizingDbContext> options)
        : base(options)
    {
    }

    // Core entities
    public DbSet<MissionCase> MissionCases => Set<MissionCase>();
    public DbSet<SizingRun> SizingRuns => Set<SizingRun>();
    public DbSet<CandidateDesign> CandidateDesigns => Set<CandidateDesign>();
    
    // Reference data
    public DbSet<HullFamilyPreset> HullFamilyPresets => Set<HullFamilyPreset>();
    public DbSet<VesselCatalog> VesselCatalog => Set<VesselCatalog>();
    public DbSet<KpiWeight> KpiWeights => Set<KpiWeight>();
    
    // Supporting
    public DbSet<IsoContainer> IsoContainers => Set<IsoContainer>();
    
    // Idempotency tracking
    public DbSet<PushOperation> PushOperations => Set<PushOperation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Use 'sizing' schema
        modelBuilder.HasDefaultSchema("sizing");

        // Apply all entity configurations
        ConfigureMissionCase(modelBuilder);
        ConfigureSizingRun(modelBuilder);
        ConfigureCandidateDesign(modelBuilder);
        ConfigureHullFamilyPreset(modelBuilder);
        ConfigureVesselCatalog(modelBuilder);
        ConfigureKpiWeight(modelBuilder);
        ConfigureIsoContainer(modelBuilder);
        ConfigurePushOperation(modelBuilder);
    }

    private void ConfigureMissionCase(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MissionCase>(entity =>
        {
            entity.ToTable("mission_cases");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.TenantId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.MissionType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CargoBasis).IsRequired().HasMaxLength(20);

            // Numeric precision
            entity.Property(e => e.CargoValue).HasColumnType("numeric(12,2)");
            entity.Property(e => e.CargoVolumeM3).HasColumnType("numeric(12,2)");
            entity.Property(e => e.CargoDensityTPerM3).HasColumnType("numeric(6,3)");
            entity.Property(e => e.ServiceSpeedKn).HasColumnType("numeric(6,2)");
            entity.Property(e => e.SeaMarginPct).HasColumnType("numeric(5,2)");
            entity.Property(e => e.ServiceMarginPct).HasColumnType("numeric(5,2)");
            entity.Property(e => e.EnvHsM).HasColumnType("numeric(6,2)");
            entity.Property(e => e.EnvTzS).HasColumnType("numeric(6,2)");
            entity.Property(e => e.CapLoaM).HasColumnType("numeric(8,2)");
            entity.Property(e => e.CapBeamM).HasColumnType("numeric(8,2)");
            entity.Property(e => e.CapDraftM).HasColumnType("numeric(6,2)");
            entity.Property(e => e.CapAirdraftM).HasColumnType("numeric(6,2)");
            entity.Property(e => e.EnduranceNm).HasColumnType("numeric(8,2)");

            // Indexes
            entity.HasIndex(e => e.UserId).HasFilter("deleted_at IS NULL");
            entity.HasIndex(e => e.TenantId).HasFilter("deleted_at IS NULL");
            entity.HasIndex(e => e.MissionType).HasFilter("deleted_at IS NULL");
            
            // Query filter for soft delete
            entity.HasQueryFilter(e => e.DeletedAt == null);

            // Relationships
            entity.HasMany(e => e.SizingRuns)
                .WithOne(r => r.MissionCase)
                .HasForeignKey(r => r.MissionCaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    // ... other configurations
}
```

---

## Migration Strategy

### Initial Migration

```bash
# Create migration
dotnet ef migrations add InitialSizingSchema --project backend/HullSizingService --context SizingDbContext

# Apply to database
dotnet ef database update --project backend/HullSizingService --context SizingDbContext
```

### Migration Files
```
backend/HullSizingService/Migrations/
├── 20241102_InitialSizingSchema.cs
└── SizingDbContextModelSnapshot.cs
```

### Auto-Migration on Startup
Program.cs applies pending migrations automatically in non-Development environments:

```csharp
if (app.Environment.EnvironmentName != "Development")
{
    await dbContext.Database.MigrateAsync();
}
```

---

## Data Integrity Constraints

### CHECK Constraints (Enforced at DB Level)

| Table | Constraint | Purpose |
|-------|-----------|---------|
| mission_cases | `cargo_basis IN ('volume', 'weight', 'teu')` | Validate cargo input type |
| mission_cases | `cargo_value >= 0` | Non-negative cargo |
| mission_cases | `service_speed_kn > 0` | Positive speed |
| mission_cases | `sea_margin_pct >= 0 AND sea_margin_pct <= 1` | Valid percentage |
| candidate_designs | `lpp_m > 0` | Positive dimensions |
| candidate_designs | `cb > 0 AND cb <= 1` | Valid coefficient range |
| candidate_designs | `score >= 0 AND score <= 1` | Valid score range |

### Foreign Key Constraints

- `sizing_runs.mission_case_id` → `mission_cases.id` (CASCADE DELETE)
- `candidate_designs.sizing_run_id` → `sizing_runs.id` (CASCADE DELETE)
- `push_operations.candidate_id` → `candidate_designs.id` (NO ACTION - keep history)

### Unique Constraints

- `hull_family_presets.family` - Unique family names
- `kpi_weights(user_id, metric)` - One weight per metric per user
- `push_operations.idempotency_key` - Prevent duplicate pushes

---

## Indexes Strategy

### Query Patterns

1. **List user's mission cases:**
   ```sql
   SELECT * FROM mission_cases 
   WHERE user_id = $1 AND deleted_at IS NULL 
   ORDER BY created_at DESC;
   ```
   Index: `idx_mission_cases_user_id` (partial, WHERE deleted_at IS NULL)

2. **Get candidates by run:**
   ```sql
   SELECT * FROM candidate_designs 
   WHERE sizing_run_id = $1 
   ORDER BY rank ASC;
   ```
   Index: `idx_candidate_designs_sizing_run_id`, `idx_candidate_designs_rank`

3. **Get top-scored candidates:**
   ```sql
   SELECT * FROM candidate_designs 
   WHERE sizing_run_id = $1 
   ORDER BY score DESC 
   LIMIT 10;
   ```
   Index: `idx_candidate_designs_score_desc` (descending)

4. **Check idempotency:**
   ```sql
   SELECT * FROM push_operations 
   WHERE idempotency_key = $1;
   ```
   Index: `idx_push_operations_idempotency_key` (unique)

---

## Soft Delete Pattern

### MissionCase Soft Delete

```csharp
// Service layer
public async Task DeleteAsync(Guid id, CancellationToken ct)
{
    var missionCase = await _context.MissionCases.FindAsync(id);
    if (missionCase == null) return;
    
    missionCase.DeletedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync(ct);
}

// EF Core query filter (auto-applied)
entity.HasQueryFilter(e => e.DeletedAt == null);

// Indexes ignore deleted rows
entity.HasIndex(e => e.UserId).HasFilter("deleted_at IS NULL");
```

**Cascading:** When mission case is soft-deleted, sizing_runs and candidate_designs remain (for audit). Frontend filters out deleted mission cases.

---

## Next: Read `03-BACKEND-PHASES.md` for implementation plan
