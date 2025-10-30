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
    public DbSet<BenchmarkAsset> BenchmarkAssets => Set<BenchmarkAsset>();
    public DbSet<BenchmarkValidationRun> BenchmarkValidationRuns => Set<BenchmarkValidationRun>();

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
