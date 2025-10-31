using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Constants;
using Shared.Models;
using Shared.TestData;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for seeding template vessels into the database
/// </summary>
public class TemplateVesselSeeder : ITemplateVesselSeeder
{
    private readonly DataDbContext _context;
    private readonly ILogger<TemplateVesselSeeder> _logger;

    public TemplateVesselSeeder(
        DataDbContext context,
        ILogger<TemplateVesselSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedHydrostaticsTemplateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if template vessel already exists (ignore soft-delete filter)
            var existingVessel = await _context.Vessels
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(v => v.Id == TemplateVessels.HydrostaticsVesselId, cancellationToken);

            if (existingVessel != null)
            {
                // If it was soft-deleted, restore it
                if (existingVessel.DeletedAt != null)
                {
                    existingVessel.DeletedAt = null;
                    existingVessel.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Restored existing template vessel {VesselId}", TemplateVessels.HydrostaticsVesselId);
                }
                else
                {
                    _logger.LogInformation("Template vessel {VesselId} already exists, skipping seed", TemplateVessels.HydrostaticsVesselId);
                }
                return;
            }

            _logger.LogInformation("Seeding hydrostatics template vessel {VesselId}...", TemplateVessels.HydrostaticsVesselId);

            // Use transaction to ensure atomicity (if supported by provider)
            var supportsTransactions = _context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;
            
            if (supportsTransactions)
            {
                transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            }

            try
            {
                // Generate Wigley hull geometry
                decimal length = 100m;
                decimal beam = 10m;
                decimal designDraft = 6.25m;
                int numStations = 21;
                int numWaterlines = 13;

                var (stations, waterlines, offsets) = HullTestData.GenerateWigleyHull(
                    length, beam, designDraft, numStations, numWaterlines);

                // Create vessel
                var vessel = new Vessel
                {
                    Id = TemplateVessels.HydrostaticsVesselId,
                    UserId = TemplateVessels.SystemUserId,
                    Name = "Hydrostatic Vessel",
                    Description = "Template vessel demonstrating complete hydrostatic analysis capabilities. This vessel contains full geometry and loadcase data for running all types of hydrostatic calculations.",
                    Lpp = length,
                    Beam = beam,
                    DesignDraft = designDraft,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Vessels.Add(vessel);

                // Add stations
                foreach (var station in stations)
                {
                    _context.Stations.Add(new Station
                    {
                        Id = Guid.NewGuid(),
                        VesselId = vessel.Id,
                        StationIndex = station.Index,
                        X = station.X
                    });
                }

                // Add waterlines
                foreach (var waterline in waterlines)
                {
                    _context.Waterlines.Add(new Waterline
                    {
                        Id = Guid.NewGuid(),
                        VesselId = vessel.Id,
                        WaterlineIndex = waterline.Index,
                        Z = waterline.Z
                    });
                }

                // Add offsets
                foreach (var offset in offsets)
                {
                    _context.Offsets.Add(new Offset
                    {
                        Id = Guid.NewGuid(),
                        VesselId = vessel.Id,
                        StationIndex = offset.StationIndex,
                        WaterlineIndex = offset.WaterlineIndex,
                        HalfBreadthY = offset.HalfBreadthY
                    });
                }

                // Create default loadcase (Design Condition)
                var loadcase = new Loadcase
                {
                    Id = Guid.NewGuid(),
                    VesselId = vessel.Id,
                    Name = "Design Condition",
                    Rho = 1025m, // Saltwater density (kg/m³)
                    KG = designDraft * 0.5m, // KG at 50% of draft
                    Notes = "Default load condition for template vessel",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Loadcases.Add(loadcase);

                // Add vessel metadata for completeness
                var metadata = new VesselMetadata
                {
                    VesselId = vessel.Id,
                    VesselType = "Research Vessel",
                    Size = "Small",
                    BlockCoefficient = 0.444m, // Approximate for Wigley hull
                    HullFamily = "Parabolic",
                    CreatedAt = DateTime.UtcNow
                };

                _context.VesselMetadata.Add(metadata);

                await _context.SaveChangesAsync(cancellationToken);
                
                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                _logger.LogInformation(
                    "Successfully seeded template vessel {VesselId} '{VesselName}' with {Stations} stations, {Waterlines} waterlines, {Offsets} offsets, and {Loadcases} loadcase(s)",
                    vessel.Id, vessel.Name, stations.Count, waterlines.Count, offsets.Count, 1);
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                _logger.LogError(ex, "Failed to seed template vessel {VesselId}", TemplateVessels.HydrostaticsVesselId);
                throw;
            }
            finally
            {
                transaction?.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding template vessel {VesselId}", TemplateVessels.HydrostaticsVesselId);
            throw;
        }
    }
}
