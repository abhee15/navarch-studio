using Microsoft.EntityFrameworkCore;
using Shared.Models;

namespace DataService.Data;

public class DataDbContext : DbContext
{
    public DataDbContext(DbContextOptions<DataDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    // Hydrostatics entities
    public DbSet<Vessel> Vessels => Set<Vessel>();
    public DbSet<Loadcase> Loadcases => Set<Loadcase>();
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<Waterline> Waterlines => Set<Waterline>();
    public DbSet<Offset> Offsets => Set<Offset>();
    public DbSet<HydroResult> HydroResults => Set<HydroResult>();
    public DbSet<Curve> Curves => Set<Curve>();
    public DbSet<CurvePoint> CurvePoints => Set<CurvePoint>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Use 'data' schema to separate from other services
        modelBuilder.HasDefaultSchema("data");

        // Product configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");

            // Soft delete support
            entity.HasQueryFilter(e => e.DeletedAt == null);
        });

        // Vessel configuration
        modelBuilder.Entity<Vessel>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Lpp).HasColumnType("decimal(10,3)");
            entity.Property(e => e.Beam).HasColumnType("decimal(10,3)");
            entity.Property(e => e.DesignDraft).HasColumnType("decimal(10,3)");
            entity.Property(e => e.UnitsSystem).HasMaxLength(10).HasDefaultValue("SI");

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
        });

        // Loadcase configuration
        modelBuilder.Entity<Loadcase>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Rho).HasColumnType("decimal(10,3)");
            entity.Property(e => e.KG).HasColumnType("decimal(10,3)");

            entity.HasIndex(e => e.VesselId);
        });

        // Station configuration
        modelBuilder.Entity<Station>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.X).HasColumnType("decimal(10,4)");

            entity.HasIndex(e => new { e.VesselId, e.StationIndex }).IsUnique();
        });

        // Waterline configuration
        modelBuilder.Entity<Waterline>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Z).HasColumnType("decimal(10,4)");

            entity.HasIndex(e => new { e.VesselId, e.WaterlineIndex }).IsUnique();
        });

        // Offset configuration
        modelBuilder.Entity<Offset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.HalfBreadthY).HasColumnType("decimal(10,4)");

            entity.HasIndex(e => new { e.VesselId, e.StationIndex, e.WaterlineIndex }).IsUnique();
        });

        // HydroResult configuration
        modelBuilder.Entity<HydroResult>(entity =>
        {
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
            entity.HasKey(e => e.Id);
            entity.Property(e => e.X).HasColumnType("decimal(15,6)");
            entity.Property(e => e.Y).HasColumnType("decimal(15,6)");

            entity.HasIndex(e => new { e.CurveId, e.Sequence });
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
            .Where(e => (e.Entity is Product || e.Entity is Vessel)
                && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            if (entry.Entity is Product product)
            {
                if (entry.State == EntityState.Added)
                {
                    product.CreatedAt = DateTime.UtcNow;
                }
                product.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Vessel vessel)
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





