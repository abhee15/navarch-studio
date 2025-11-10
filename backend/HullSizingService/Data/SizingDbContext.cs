using Microsoft.EntityFrameworkCore;
using Shared.Models.Sizing;

namespace HullSizingService.Data;

/// <summary>
/// Database context for Hull Sizing Service (schema: sizing)
/// </summary>
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
    public DbSet<IsoContainer> IsoContainers => Set<IsoContainer>();

    // Idempotency tracking
    public DbSet<PushOperation> PushOperations => Set<PushOperation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Use 'sizing' schema to separate from other services (identity, data)
        modelBuilder.HasDefaultSchema("sizing");

        // Configure all entities
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

            // Unique constraint on name per tenant (excluding soft-deleted records)
            entity.HasIndex(e => new { e.Name, e.TenantId })
                .IsUnique()
                .HasFilter("deleted_at IS NULL");

            // Query filter for soft delete
            entity.HasQueryFilter(e => e.DeletedAt == null);

            // Relationships
            entity.HasMany(e => e.SizingRuns)
                .WithOne(r => r.MissionCase)
                .HasForeignKey(r => r.MissionCaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureSizingRun(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SizingRun>(entity =>
        {
            entity.ToTable("sizing_runs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Mode).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.LocksJson).HasColumnType("jsonb");
            entity.Property(e => e.OptionsJson).HasColumnType("jsonb");

            // Indexes
            entity.HasIndex(e => e.MissionCaseId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            // Relationships
            entity.HasMany(e => e.Candidates)
                .WithOne(c => c.SizingRun)
                .HasForeignKey(c => c.SizingRunId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureCandidateDesign(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CandidateDesign>(entity =>
        {
            entity.ToTable("candidate_designs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.HullFamily).IsRequired().HasMaxLength(50);

            // Principal dimensions - numeric(12,4)
            entity.Property(e => e.LppM).HasColumnType("numeric(12,4)");
            entity.Property(e => e.LwlM).HasColumnType("numeric(12,4)");
            entity.Property(e => e.LoaM).HasColumnType("numeric(12,4)");
            entity.Property(e => e.BM).HasColumnType("numeric(12,4)");
            entity.Property(e => e.TM).HasColumnType("numeric(12,4)");
            entity.Property(e => e.DM).HasColumnType("numeric(12,4)");

            // Form coefficients - numeric(6,4)
            entity.Property(e => e.Cb).HasColumnType("numeric(6,4)");
            entity.Property(e => e.Cp).HasColumnType("numeric(6,4)");
            entity.Property(e => e.Cwp).HasColumnType("numeric(6,4)");
            entity.Property(e => e.Cm).HasColumnType("numeric(6,4)");

            // Displacement - numeric(12,3)
            entity.Property(e => e.DisplacementT).HasColumnType("numeric(12,3)");

            // Speed/powering
            entity.Property(e => e.Fn).HasColumnType("numeric(6,4)");
            entity.Property(e => e.LwlOverLambda).HasColumnType("numeric(6,3)");
            entity.Property(e => e.EhpKw).HasColumnType("numeric(10,2)");
            entity.Property(e => e.ShpKw).HasColumnType("numeric(10,2)");

            // Stability
            entity.Property(e => e.GmEstM).HasColumnType("numeric(8,3)");
            entity.Property(e => e.KbM).HasColumnType("numeric(8,3)");
            entity.Property(e => e.LcbPctLpp).HasColumnType("numeric(6,3)");

            // Scoring
            entity.Property(e => e.Score).HasColumnType("numeric(8,4)");
            entity.Property(e => e.ScoresJson).HasColumnType("jsonb");
            entity.Property(e => e.FlagsJson).HasColumnType("jsonb");
            entity.Property(e => e.GeometryJson).HasColumnType("jsonb");

            // Indexes
            entity.HasIndex(e => e.SizingRunId);
            entity.HasIndex(e => e.Score).IsDescending();
            entity.HasIndex(e => e.Rank);
            entity.HasIndex(e => e.HullFamily);
        });
    }

    private void ConfigureHullFamilyPreset(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HullFamilyPreset>(entity =>
        {
            entity.ToTable("hull_family_presets");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Family).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Family).IsUnique();

            // Ratios
            entity.Property(e => e.LOverBMin).HasColumnType("numeric(5,2)");
            entity.Property(e => e.LOverBMax).HasColumnType("numeric(5,2)");
            entity.Property(e => e.BOverTMin).HasColumnType("numeric(5,2)");
            entity.Property(e => e.BOverTMax).HasColumnType("numeric(5,2)");
            entity.Property(e => e.DOverTMin).HasColumnType("numeric(5,2)");
            entity.Property(e => e.DOverTMax).HasColumnType("numeric(5,2)");

            // Coefficients
            entity.Property(e => e.CbMin).HasColumnType("numeric(5,3)");
            entity.Property(e => e.CbMax).HasColumnType("numeric(5,3)");
            entity.Property(e => e.CpMin).HasColumnType("numeric(5,3)");
            entity.Property(e => e.CpMax).HasColumnType("numeric(5,3)");
            entity.Property(e => e.CwpMin).HasColumnType("numeric(5,3)");
            entity.Property(e => e.CwpMax).HasColumnType("numeric(5,3)");

            // Froude
            entity.Property(e => e.FnMin).HasColumnType("numeric(5,3)");
            entity.Property(e => e.FnMax).HasColumnType("numeric(5,3)");

            entity.HasIndex(e => e.IsActive).HasFilter("is_active = true");
        });
    }

    private void ConfigureVesselCatalog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VesselCatalog>(entity =>
        {
            entity.ToTable("vessel_catalog");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);

            // Dimensions
            entity.Property(e => e.LppM).HasColumnType("numeric(12,4)");
            entity.Property(e => e.LwlM).HasColumnType("numeric(12,4)");
            entity.Property(e => e.BM).HasColumnType("numeric(12,4)");
            entity.Property(e => e.TM).HasColumnType("numeric(12,4)");
            entity.Property(e => e.DM).HasColumnType("numeric(12,4)");

            // Coefficients
            entity.Property(e => e.Cb).HasColumnType("numeric(6,4)");
            entity.Property(e => e.Cp).HasColumnType("numeric(6,4)");
            entity.Property(e => e.Cwp).HasColumnType("numeric(6,4)");
            entity.Property(e => e.Cm).HasColumnType("numeric(6,4)");

            // Capacity
            entity.Property(e => e.DwtT).HasColumnType("numeric(12,2)");
            entity.Property(e => e.ServiceSpeedKn).HasColumnType("numeric(6,2)");

            entity.HasIndex(e => e.VesselType);
            entity.HasIndex(e => e.Provenance);
        });
    }

    private void ConfigureKpiWeight(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KpiWeight>(entity =>
        {
            entity.ToTable("kpi_weights");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Metric).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Weight).HasColumnType("numeric(5,3)");

            entity.HasIndex(e => new { e.UserId, e.Metric }).IsUnique();
        });
    }

    private void ConfigureIsoContainer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IsoContainer>(entity =>
        {
            entity.ToTable("iso_containers");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ContainerType).IsRequired().HasMaxLength(10);
        });
    }

    private void ConfigurePushOperation(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PushOperation>(entity =>
        {
            entity.ToTable("push_operations");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.IdempotencyKey).IsRequired().HasMaxLength(255);

            entity.HasIndex(e => e.IdempotencyKey).IsUnique();
            entity.HasIndex(e => e.CandidateId);
        });
    }
}
