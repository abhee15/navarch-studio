using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace DataService.Data;

public class DataDbContext : DbContext
{
    public DataDbContext(DbContextOptions<DataDbContext> options)
        : base(options)
    {
    }

    // Hydrostatics entities
    public DbSet<Vessel> Vessels => Set<Vessel>();
    public DbSet<Loadcase> Loadcases => Set<Loadcase>();
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<Waterline> Waterlines => Set<Waterline>();
    public DbSet<Offset> Offsets => Set<Offset>();
    public DbSet<HydroResult> HydroResults => Set<HydroResult>();
    public DbSet<Curve> Curves => Set<Curve>();
    public DbSet<CurvePoint> CurvePoints => Set<CurvePoint>();

    // Vessel metadata entities
    public DbSet<VesselMetadata> VesselMetadata => Set<VesselMetadata>();
    public DbSet<MaterialsConfig> MaterialsConfigs => Set<MaterialsConfig>();
    public DbSet<LoadingConditions> LoadingConditions => Set<LoadingConditions>();

    // Benchmark entities
    public DbSet<BenchmarkCase> BenchmarkCases => Set<BenchmarkCase>();
    public DbSet<BenchmarkGeometry> BenchmarkGeometries => Set<BenchmarkGeometry>();
    public DbSet<BenchmarkTestPoint> BenchmarkTestPoints => Set<BenchmarkTestPoint>();
    public DbSet<BenchmarkMetricRef> BenchmarkMetricRefs => Set<BenchmarkMetricRef>();

    // Comparison entities
    public DbSet<ComparisonSnapshot> ComparisonSnapshots => Set<ComparisonSnapshot>();
    public DbSet<BenchmarkAsset> BenchmarkAssets => Set<BenchmarkAsset>();
    public DbSet<BenchmarkValidationRun> BenchmarkValidationRuns => Set<BenchmarkValidationRun>();

    // Catalog entities
    public DbSet<CatalogPropellerSeries> CatalogPropellerSeries => Set<CatalogPropellerSeries>();
    public DbSet<CatalogPropellerPoint> CatalogPropellerPoints => Set<CatalogPropellerPoint>();
    public DbSet<CatalogWaterProperty> CatalogWaterProperties => Set<CatalogWaterProperty>();

    // Project board entities
    public DbSet<ProjectBoard> ProjectBoards => Set<ProjectBoard>();
    public DbSet<BoardCard> BoardCards => Set<BoardCard>();

    // Additional dataset entities
    public DbSet<SpeedGrid> SpeedGrids => Set<SpeedGrid>();
    public DbSet<SpeedPoint> SpeedPoints => Set<SpeedPoint>();
    public DbSet<EngineCurve> EngineCurves => Set<EngineCurve>();
    public DbSet<EnginePoint> EnginePoints => Set<EnginePoint>();
    public DbSet<SeaState> SeaStates => Set<SeaState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Use 'data' schema to separate from other services
        modelBuilder.HasDefaultSchema("data");

        // Vessel configuration
        modelBuilder.Entity<Vessel>(entity =>
        {
            entity.ToTable("vessels");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Lpp).HasColumnType("decimal(10,3)");
            entity.Property(e => e.Beam).HasColumnType("decimal(10,3)");
            entity.Property(e => e.DesignDraft).HasColumnType("decimal(10,3)");

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.SourceCatalogHullId);
            entity.HasQueryFilter(e => e.DeletedAt == null);

            // Configure relationships
            entity.HasMany(e => e.Stations)
                .WithOne(s => s.Vessel)
                .HasForeignKey(s => s.VesselId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Waterlines)
                .WithOne(w => w.Vessel)
                .HasForeignKey(w => w.VesselId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Offsets)
                .WithOne(o => o.Vessel)
                .HasForeignKey(o => o.VesselId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Loadcases)
                .WithOne(l => l.Vessel)
                .HasForeignKey(l => l.VesselId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-one relationships for metadata
            entity.HasOne(e => e.Metadata)
                .WithOne(m => m.Vessel)
                .HasForeignKey<VesselMetadata>(m => m.VesselId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Materials)
                .WithOne(m => m.Vessel)
                .HasForeignKey<MaterialsConfig>(m => m.VesselId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Loading)
                .WithOne(l => l.Vessel)
                .HasForeignKey<LoadingConditions>(l => l.VesselId)
                .OnDelete(DeleteBehavior.Cascade);

            // New dataset relationships
            entity.HasMany(e => e.SpeedGrids)
                .WithOne(sg => sg.Vessel)
                .HasForeignKey(sg => sg.VesselId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.EngineCurves)
                .WithOne(ec => ec.Vessel)
                .HasForeignKey(ec => ec.VesselId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.SeaStates)
                .WithOne(ss => ss.Vessel)
                .HasForeignKey(ss => ss.VesselId)
                .OnDelete(DeleteBehavior.Cascade);

            // Version notes
            entity.Property(e => e.VersionNotes).HasColumnType("text");
        });

        // Loadcase configuration
        modelBuilder.Entity<Loadcase>(entity =>
        {
            entity.ToTable("loadcases");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Rho).HasColumnType("decimal(10,3)");
            entity.Property(e => e.KG).HasColumnType("decimal(10,3)");

            entity.HasIndex(e => e.VesselId);
        });

        // Station configuration
        modelBuilder.Entity<Station>(entity =>
        {
            entity.ToTable("stations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.X).HasColumnType("decimal(10,4)");

            entity.HasIndex(e => new { e.VesselId, e.StationIndex }).IsUnique();
        });

        // Waterline configuration
        modelBuilder.Entity<Waterline>(entity =>
        {
            entity.ToTable("waterlines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Z).HasColumnType("decimal(10,4)");

            entity.HasIndex(e => new { e.VesselId, e.WaterlineIndex }).IsUnique();
        });

        // Offset configuration
        modelBuilder.Entity<Offset>(entity =>
        {
            entity.ToTable("offsets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.HalfBreadthY).HasColumnType("decimal(10,4)");

            entity.HasIndex(e => new { e.VesselId, e.StationIndex, e.WaterlineIndex }).IsUnique();
        });

        // HydroResult configuration
        modelBuilder.Entity<HydroResult>(entity =>
        {
            entity.ToTable("hydro_results");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Draft).HasColumnType("decimal(10,4)");
            entity.Property(e => e.DispVolume).HasColumnType("decimal(15,4)");
            entity.Property(e => e.DispWeight).HasColumnType("decimal(15,4)");
            entity.Property(e => e.KBz).HasColumnType("decimal(10,4)");
            entity.Property(e => e.LCBx).HasColumnType("decimal(10,4)");
            entity.Property(e => e.TCBy).HasColumnType("decimal(10,4)");
            entity.Property(e => e.BMt).HasColumnType("decimal(10,4)");
            entity.Property(e => e.BMl).HasColumnType("decimal(10,4)");
            entity.Property(e => e.GMt).HasColumnType("decimal(10,4)");
            entity.Property(e => e.GMl).HasColumnType("decimal(10,4)");
            entity.Property(e => e.Awp).HasColumnType("decimal(12,4)");
            entity.Property(e => e.Iwp).HasColumnType("decimal(15,4)");
            entity.Property(e => e.Cb).HasColumnType("decimal(6,4)");
            entity.Property(e => e.Cp).HasColumnType("decimal(6,4)");
            entity.Property(e => e.Cm).HasColumnType("decimal(6,4)");
            entity.Property(e => e.Cwp).HasColumnType("decimal(6,4)");
            entity.Property(e => e.TrimAngle).HasColumnType("decimal(6,3)");

            entity.HasIndex(e => e.VesselId);
            entity.HasIndex(e => e.LoadcaseId);
        });

        // Curve configuration
        modelBuilder.Entity<Curve>(entity =>
        {
            entity.ToTable("curves");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
            entity.Property(e => e.XLabel).HasMaxLength(100);
            entity.Property(e => e.YLabel).HasMaxLength(100);

            entity.HasIndex(e => e.VesselId);

            entity.HasMany(e => e.Points)
                .WithOne(p => p.Curve)
                .HasForeignKey(p => p.CurveId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CurvePoint configuration
        modelBuilder.Entity<CurvePoint>(entity =>
        {
            entity.ToTable("curve_points");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.X).HasColumnType("decimal(15,6)");
            entity.Property(e => e.Y).HasColumnType("decimal(15,6)");

            entity.HasIndex(e => new { e.CurveId, e.Sequence });
        });

        // VesselMetadata configuration
        modelBuilder.Entity<VesselMetadata>(entity =>
        {
            entity.ToTable("vessel_metadata");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VesselType).HasMaxLength(50);
            entity.Property(e => e.Size).HasMaxLength(50);
            entity.Property(e => e.BlockCoefficient).HasColumnType("decimal(5,3)");
            entity.Property(e => e.HullFamily).HasMaxLength(50);

            entity.HasIndex(e => e.VesselId).IsUnique();
        });

        // MaterialsConfig configuration
        modelBuilder.Entity<MaterialsConfig>(entity =>
        {
            entity.ToTable("materials_config");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.HullMaterial).HasMaxLength(50);
            entity.Property(e => e.SuperstructureMaterial).HasMaxLength(50);

            entity.HasIndex(e => e.VesselId).IsUnique();
        });

        // LoadingConditions configuration
        modelBuilder.Entity<LoadingConditions>(entity =>
        {
            entity.ToTable("loading_conditions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LightshipTonnes).HasColumnType("decimal(10,2)");
            entity.Property(e => e.DeadweightTonnes).HasColumnType("decimal(10,2)");

            entity.HasIndex(e => e.VesselId).IsUnique();
        });

        // BenchmarkCase configuration
        modelBuilder.Entity<BenchmarkCase>(entity =>
        {
            entity.ToTable("benchmark_case");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CanonicalRefs).HasColumnType("text");
            entity.HasIndex(e => e.Slug).IsUnique();

            // Catalog/Hull-specific fields
            entity.Property(e => e.HullType).HasMaxLength(50);
            entity.Property(e => e.Lpp_m).HasColumnType("decimal(10,3)");
            entity.Property(e => e.B_m).HasColumnType("decimal(10,3)");
            entity.Property(e => e.T_m).HasColumnType("decimal(10,3)");
            entity.Property(e => e.Cb).HasColumnType("decimal(6,4)");
            entity.Property(e => e.Cp).HasColumnType("decimal(6,4)");
            entity.Property(e => e.LCB_pctLpp).HasColumnType("decimal(6,3)");
            entity.Property(e => e.LCF_pctLpp).HasColumnType("decimal(6,3)");

            // Self-referencing relationship for catalog hulls
            entity.HasOne(e => e.CatalogHull)
                .WithMany(c => c.ChildBenchmarks)
                .HasForeignKey(e => e.CatalogHullId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // BenchmarkGeometry configuration
        modelBuilder.Entity<BenchmarkGeometry>(entity =>
        {
            entity.ToTable("benchmark_geometry");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(20);
            entity.Property(e => e.SourceUrl).HasMaxLength(500);
            entity.Property(e => e.S3Key).HasMaxLength(500);
            entity.Property(e => e.Checksum).HasMaxLength(128);
            entity.Property(e => e.ScaleNote).HasMaxLength(200);

            // Normalized JSON storage
            entity.Property(e => e.StationsJson).HasColumnType("text");
            entity.Property(e => e.WaterlinesJson).HasColumnType("text");
            entity.Property(e => e.OffsetsJson).HasColumnType("text");

            entity.HasIndex(e => e.CaseId);
            entity.HasOne(e => e.Case)
                .WithMany(c => c.Geometries)
                .HasForeignKey(e => e.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BenchmarkTestPoint configuration
        modelBuilder.Entity<BenchmarkTestPoint>(entity =>
        {
            entity.ToTable("benchmark_testpoint");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Fr).HasColumnType("decimal(10,6)");
            entity.Property(e => e.Vm).HasColumnType("decimal(10,6)");

            // Water properties for test conditions
            entity.Property(e => e.Medium).HasMaxLength(20);
            entity.Property(e => e.Temperature_C).HasColumnType("decimal(6,2)");
            entity.Property(e => e.Salinity_PSU).HasColumnType("decimal(8,4)");
            entity.Property(e => e.Density_kgm3).HasColumnType("decimal(10,4)");
            entity.Property(e => e.KinematicViscosity_m2s).HasColumnType("decimal(12,6)");

            entity.HasIndex(e => new { e.CaseId, e.Fr }).IsUnique();
            entity.HasOne(e => e.Case)
                .WithMany(c => c.TestPoints)
                .HasForeignKey(e => e.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BenchmarkMetricRef configuration
        modelBuilder.Entity<BenchmarkMetricRef>(entity =>
        {
            entity.ToTable("benchmark_metric_ref");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Metric).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ValueNum).HasColumnType("decimal(18,8)");
            entity.Property(e => e.Unit).HasMaxLength(20);
            entity.Property(e => e.TolRel).HasColumnType("decimal(10,6)");
            entity.Property(e => e.FigureRef).HasMaxLength(100);
            entity.Property(e => e.SourceUrl).HasMaxLength(500);
            entity.HasIndex(e => e.CaseId);
            entity.HasOne(e => e.Case)
                .WithMany(c => c.MetricRefs)
                .HasForeignKey(e => e.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BenchmarkAsset configuration
        modelBuilder.Entity<BenchmarkAsset>(entity =>
        {
            entity.ToTable("benchmark_asset");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Kind).IsRequired().HasMaxLength(30);
            entity.Property(e => e.S3Key).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Caption).HasMaxLength(300);
            entity.Property(e => e.FigureRef).HasMaxLength(100);
            entity.Property(e => e.SourceUrl).HasMaxLength(500);
            entity.HasIndex(e => e.CaseId);
            entity.HasOne(e => e.Case)
                .WithMany(c => c.Assets)
                .HasForeignKey(e => e.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BenchmarkValidationRun configuration
        modelBuilder.Entity<BenchmarkValidationRun>(entity =>
        {
            entity.ToTable("benchmark_validation_run");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Fr).HasColumnType("decimal(10,6)");
            entity.Property(e => e.Metrics).HasColumnType("text");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(30);
            entity.HasIndex(e => e.CaseId);
            entity.HasOne(e => e.Case)
                .WithMany(c => c.ValidationRuns)
                .HasForeignKey(e => e.CaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ProjectBoard configuration
        modelBuilder.Entity<ProjectBoard>(entity =>
        {
            entity.ToTable("project_boards");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.UserId);
            entity.HasQueryFilter(e => e.DeletedAt == null);

            entity.HasMany(e => e.Cards)
                .WithOne(c => c.Board)
                .HasForeignKey(c => c.BoardId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BoardCard configuration
        modelBuilder.Entity<BoardCard>(entity =>
        {
            entity.ToTable("board_cards");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.BoardId);

            entity.HasOne(e => e.Vessel)
                .WithMany()
                .HasForeignKey(e => e.VesselId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // SpeedGrid configuration
        modelBuilder.Entity<SpeedGrid>(entity =>
        {
            entity.ToTable("speed_grids");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.VesselId);

            entity.HasMany(e => e.SpeedPoints)
                .WithOne(p => p.SpeedGrid)
                .HasForeignKey(p => p.SpeedGridId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SpeedPoint configuration
        modelBuilder.Entity<SpeedPoint>(entity =>
        {
            entity.ToTable("speed_points");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Speed).HasColumnType("decimal(10,3)");
            entity.Property(e => e.SpeedKnots).HasColumnType("decimal(10,3)");
            entity.Property(e => e.FroudeNumber).HasColumnType("decimal(8,4)");
        });

        // EngineCurve configuration
        modelBuilder.Entity<EngineCurve>(entity =>
        {
            entity.ToTable("engine_curves");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.VesselId);

            entity.HasMany(e => e.EnginePoints)
                .WithOne(p => p.EngineCurve)
                .HasForeignKey(p => p.EngineCurveId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // EnginePoint configuration
        modelBuilder.Entity<EnginePoint>(entity =>
        {
            entity.ToTable("engine_points");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Rpm).HasColumnType("decimal(10,2)");
            entity.Property(e => e.PowerKw).HasColumnType("decimal(12,2)");
            entity.Property(e => e.Torque).HasColumnType("decimal(12,2)");
            entity.Property(e => e.FuelConsumption).HasColumnType("decimal(10,2)");
        });

        // SeaState configuration
        modelBuilder.Entity<SeaState>(entity =>
        {
            entity.ToTable("sea_states");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.WaveHeight).HasColumnType("decimal(8,3)");
            entity.Property(e => e.WavePeriod).HasColumnType("decimal(8,3)");
            entity.Property(e => e.WaveDirection).HasColumnType("decimal(6,2)");
            entity.Property(e => e.WindSpeed).HasColumnType("decimal(8,3)");
            entity.Property(e => e.WindDirection).HasColumnType("decimal(6,2)");
            entity.Property(e => e.WaterDepth).HasColumnType("decimal(10,3)");
            entity.HasIndex(e => e.VesselId);
        });

        // CatalogPropellerSeries configuration
        modelBuilder.Entity<CatalogPropellerSeries>(entity =>
        {
            entity.ToTable("catalog_propeller_series");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.BladeCount).IsRequired();
            entity.Property(e => e.ExpandedAreaRatio).HasColumnType("decimal(6,4)");
            entity.Property(e => e.PitchDiameterRatio).HasColumnType("decimal(6,4)");
            entity.Property(e => e.SourceUrl).HasMaxLength(500);
            entity.Property(e => e.License).HasMaxLength(200);

            entity.HasMany(e => e.OpenWaterPoints)
                .WithOne(p => p.Series)
                .HasForeignKey(p => p.SeriesId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CatalogPropellerPoint configuration
        modelBuilder.Entity<CatalogPropellerPoint>(entity =>
        {
            entity.ToTable("catalog_propeller_points");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.J).HasColumnType("decimal(10,6)");
            entity.Property(e => e.Kt).HasColumnType("decimal(10,6)");
            entity.Property(e => e.Kq).HasColumnType("decimal(10,6)");
            entity.Property(e => e.Eta0).HasColumnType("decimal(10,6)");
            entity.Property(e => e.ReynoldsNumber).HasColumnType("decimal(18,6)");

            entity.HasIndex(e => e.SeriesId);
        });

        // CatalogWaterProperty configuration
        modelBuilder.Entity<CatalogWaterProperty>(entity =>
        {
            entity.ToTable("catalog_water_properties");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Medium).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Temperature_C).HasColumnType("decimal(6,2)");
            entity.Property(e => e.Salinity_PSU).HasColumnType("decimal(8,4)");
            entity.Property(e => e.Density_kgm3).HasColumnType("decimal(10,4)");
            entity.Property(e => e.KinematicViscosity_m2s).HasColumnType("decimal(12,8)");
            entity.Property(e => e.SourceRef).IsRequired().HasMaxLength(200);

            entity.HasIndex(e => new { e.Medium, e.Temperature_C });
        });

        // ComparisonSnapshot configuration
        modelBuilder.Entity<ComparisonSnapshot>(entity =>
        {
            entity.ToTable("comparison_snapshots");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RunName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Tags).HasMaxLength(500);
            entity.Property(e => e.VesselLpp).HasColumnType("decimal(10,3)");
            entity.Property(e => e.VesselBeam).HasColumnType("decimal(10,3)");
            entity.Property(e => e.VesselDesignDraft).HasColumnType("decimal(10,3)");
            entity.Property(e => e.LoadcaseRho).HasColumnType("decimal(10,3)");
            entity.Property(e => e.LoadcaseKG).HasColumnType("decimal(10,3)");
            entity.Property(e => e.MinDraft).HasColumnType("decimal(10,3)");
            entity.Property(e => e.MaxDraft).HasColumnType("decimal(10,3)");
            entity.Property(e => e.DraftStep).HasColumnType("decimal(10,3)");
            entity.Property(e => e.ResultsJson).HasColumnType("text").IsRequired();

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.VesselId);
            entity.HasIndex(e => new { e.VesselId, e.IsBaseline });
            entity.HasQueryFilter(e => e.DeletedAt == null);

            entity.HasOne(e => e.Vessel)
                .WithMany()
                .HasForeignKey(e => e.VesselId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Loadcase)
                .WithMany()
                .HasForeignKey(e => e.LoadcaseId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Vessel
                && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            if (entry.Entity is Vessel vessel)
            {
                if (entry.State == EntityState.Added)
                {
                    vessel.CreatedAt = DateTime.UtcNow;
                }
                vessel.UpdatedAt = DateTime.UtcNow;
            }
        }
    }
}
