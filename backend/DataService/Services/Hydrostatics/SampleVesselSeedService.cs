using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.DTOs;
using Shared.Models;
using Shared.TestData;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for seeding sample vessels (KCS, Wigley) into the database
/// </summary>
public class SampleVesselSeedService
{
    private readonly DataDbContext _context;
    private readonly ILogger<SampleVesselSeedService> _logger;

    public SampleVesselSeedService(DataDbContext context, ILogger<SampleVesselSeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the KCS (KRISO Container Ship) sample vessel
    /// </summary>
    public async Task SeedKcsVesselAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Check if KCS already exists
        var existingKcs = await _context.Vessels
            .FirstOrDefaultAsync(v => v.Name == "KCS (KRISO Container Ship)" && v.UserId == userId, cancellationToken);

        if (existingKcs != null)
        {
            _logger.LogInformation("KCS vessel already exists for user {UserId}", userId);
            return;
        }

        // Create KCS vessel with real dimensions
        // KCS dimensions from Tokyo 2015 Workshop
        var kcsVessel = new Vessel
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "KCS (KRISO Container Ship)",
            Description = "KRISO Container Ship benchmark case from Tokyo 2015 Workshop on CFD. Lpp=230m, B=32.2m, T=10.8m, Cb=0.6505",
            Lpp = 230m,
            Beam = 32.2m,
            DesignDraft = 10.8m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add metadata
        var kcsMetadata = new VesselMetadata
        {
            VesselId = kcsVessel.Id,
            VesselType = "Ship",
            Size = "Large",
            BlockCoefficient = 0.6505m,
            HullFamily = "KCS",
            CreatedAt = DateTime.UtcNow
        };

        _context.Vessels.Add(kcsVessel);
        _context.VesselMetadata.Add(kcsMetadata);

        // Generate geometry - for KCS, we'll use a representative hull form
        // In production, this would load actual KCS offset data
        await SeedKcsGeometryAsync(kcsVessel.Id, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded KCS vessel {VesselId} for user {UserId}", kcsVessel.Id, userId);
    }

    /// <summary>
    /// Seeds the Wigley parabolic hull sample vessel
    /// </summary>
    public async Task SeedWigleyVesselAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Check if Wigley already exists
        var existingWigley = await _context.Vessels
            .FirstOrDefaultAsync(v => v.Name == "Wigley Hull (Parabolic)" && v.UserId == userId, cancellationToken);

        if (existingWigley != null)
        {
            _logger.LogInformation("Wigley vessel already exists for user {UserId}", userId);
            return;
        }

        // Create Wigley vessel - standard dimensions from literature
        var wigleyVessel = new Vessel
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Wigley Hull (Parabolic)",
            Description = "Classical Wigley parabolic hull form. L=100m, B=10m, T=6.25m. Analytical benchmarks available for validation.",
            Lpp = 100m,
            Beam = 10m,
            DesignDraft = 6.25m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add metadata
        var wigleyMetadata = new VesselMetadata
        {
            VesselId = wigleyVessel.Id,
            VesselType = "Ship",
            Size = "Small",
            BlockCoefficient = 0.444m, // Cb ≈ 0.444 for Wigley hull
            HullFamily = "Wigley",
            CreatedAt = DateTime.UtcNow
        };

        _context.Vessels.Add(wigleyVessel);
        _context.VesselMetadata.Add(wigleyMetadata);

        // Generate geometry using HullTestData
        await SeedWigleyGeometryAsync(wigleyVessel.Id, wigleyVessel.Lpp, wigleyVessel.Beam, wigleyVessel.DesignDraft, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded Wigley vessel {VesselId} for user {UserId}", wigleyVessel.Id, userId);
    }

    /// <summary>
    /// Seeds the template vessel with fixed ID for all users
    /// This is the default vessel shown on first load
    /// </summary>
    public async Task SeedTemplateVesselAsync(CancellationToken cancellationToken = default)
    {
        var templateVesselId = Shared.Constants.TemplateVessels.HydrostaticsVesselId;

        // Check if template vessel already exists
        var existingTemplate = await _context.Vessels.FindAsync(new object[] { templateVesselId }, cancellationToken);

        if (existingTemplate != null)
        {
            // Check if it has geometry
            var hasGeometry = await _context.Stations.AnyAsync(s => s.VesselId == templateVesselId, cancellationToken);
            if (hasGeometry)
            {
                _logger.LogInformation("Template vessel already exists with geometry");
                return;
            }

            // Has vessel but no geometry - seed geometry
            _logger.LogInformation("Template vessel exists but has no geometry. Seeding geometry...");
            await SeedWigleyGeometryAsync(templateVesselId, 100m, 10m, 6.25m, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Template vessel geometry seeded");
            return;
        }

        // Create template vessel with Wigley hull form (well-known analytical hull)
        var templateVessel = new Vessel
        {
            Id = templateVesselId,
            UserId = Shared.Constants.TemplateVessels.SystemUserId, // System user (all zeros)
            Name = "Wigley Hull (Template)",
            Description = "Template vessel for hydrostatic analysis demonstration. Classical Wigley parabolic hull form with known analytical properties.",
            Lpp = 100m,
            Beam = 10m,
            DesignDraft = 6.25m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var templateMetadata = new VesselMetadata
        {
            VesselId = templateVessel.Id,
            VesselType = "Ship",
            Size = "Small",
            BlockCoefficient = 0.444m,
            HullFamily = "Wigley",
            CreatedAt = DateTime.UtcNow
        };

        _context.Vessels.Add(templateVessel);
        _context.VesselMetadata.Add(templateMetadata);

        // Generate Wigley geometry
        await SeedWigleyGeometryAsync(templateVessel.Id, templateVessel.Lpp, templateVessel.Beam, templateVessel.DesignDraft, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded template vessel {VesselId}", templateVessel.Id);
    }

    /// <summary>
    /// Seeds both sample vessels
    /// </summary>
    public async Task SeedAllSampleVesselsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await SeedKcsVesselAsync(userId, cancellationToken);
        await SeedWigleyVesselAsync(userId, cancellationToken);
        _logger.LogInformation("Completed seeding all sample vessels for user {UserId}", userId);
    }

    private Task SeedKcsGeometryAsync(Guid vesselId, CancellationToken cancellationToken)
    {
        // For KCS, we create a representative hull form
        // In production, load actual KCS offset data from a file or external source

        // Generate stations along length
        var stations = new List<Station>();
        for (int i = 0; i < 21; i++)
        {
            decimal x = 230m * i / 20; // From 0 to 230m
            stations.Add(new Station
            {
                Id = Guid.NewGuid(),
                VesselId = vesselId,
                StationIndex = i,
                X = x
            });
        }

        // Generate waterlines
        var waterlines = new List<Waterline>();
        for (int j = 0; j < 13; j++)
        {
            decimal z = 10.8m * j / 12; // From 0 to 10.8m
            waterlines.Add(new Waterline
            {
                Id = Guid.NewGuid(),
                VesselId = vesselId,
                WaterlineIndex = j,
                Z = z
            });
        }

        // Generate offsets - representative KCS-like hull form
        // Using a simplified parabolic shape with characteristic features
        var offsets = new List<Offset>();
        for (int i = 0; i < 21; i++)
        {
            decimal x = 230m * i / 20;
            decimal x_norm = (x - 115m) / 115m; // Normalize to -1 to +1

            for (int j = 0; j < 13; j++)
            {
                decimal z = 10.8m * j / 12;
                decimal z_norm = z / 10.8m; // Normalize to 0 to 1

                // Simplified KCS-like hull form
                // Note: This is a placeholder - actual KCS data should be loaded from source files
                decimal beamFraction = 0.5m;
                if (Math.Abs(x_norm) < 0.3m) // Fine ends
                {
                    beamFraction = 0.15m * Math.Abs(x_norm) / 0.3m;
                }
                else if (Math.Abs(x_norm) > 0.8m) // Full ends
                {
                    beamFraction = 0.5m - 0.1m * (Math.Abs(x_norm) - 0.8m) / 0.2m;
                }

                decimal halfBreadth = (32.2m / 2) * beamFraction * (1 - z_norm * z_norm);

                offsets.Add(new Offset
                {
                    Id = Guid.NewGuid(),
                    VesselId = vesselId,
                    StationIndex = i,
                    WaterlineIndex = j,
                    HalfBreadthY = halfBreadth
                });
            }
        }

        _context.Stations.AddRange(stations);
        _context.Waterlines.AddRange(waterlines);
        _context.Offsets.AddRange(offsets);

        return Task.CompletedTask;
    }

    private Task SeedWigleyGeometryAsync(Guid vesselId, decimal length, decimal beam, decimal designDraft, CancellationToken cancellationToken)
    {
        // Use HullTestData to generate Wigley geometry
        var (stationData, waterlineData, offsetData) = HullTestData.GenerateWigleyHull(
            length, beam, designDraft, 21, 13);

        // Convert to entities
        var stations = stationData.Select(s => new Station
        {
            Id = Guid.NewGuid(),
            VesselId = vesselId,
            StationIndex = s.Index,
            X = s.X
        }).ToList();

        var waterlines = waterlineData.Select(w => new Waterline
        {
            Id = Guid.NewGuid(),
            VesselId = vesselId,
            WaterlineIndex = w.Index,
            Z = w.Z
        }).ToList();

        var offsets = offsetData.Select(o => new Offset
        {
            Id = Guid.NewGuid(),
            VesselId = vesselId,
            StationIndex = o.StationIndex,
            WaterlineIndex = o.WaterlineIndex,
            HalfBreadthY = o.HalfBreadthY
        }).ToList();

        _context.Stations.AddRange(stations);
        _context.Waterlines.AddRange(waterlines);
        _context.Offsets.AddRange(offsets);

        return Task.CompletedTask;
    }
}
