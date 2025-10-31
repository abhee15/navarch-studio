using DataService.Data;
using DataService.Services.Hydrostatics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Constants;
using Xunit;

namespace DataService.Tests.Services.Hydrostatics;

/// <summary>
/// Tests for TemplateVesselSeeder service
/// </summary>
public class TemplateVesselSeederTests : IDisposable
{
    private readonly DataDbContext _context;
    private readonly ITemplateVesselSeeder _seeder;

    public TemplateVesselSeederTests()
    {
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: $"TemplateSeederTest_{Guid.NewGuid()}")
            .Options;

        _context = new DataDbContext(options);
        _seeder = new TemplateVesselSeeder(_context, Mock.Of<ILogger<TemplateVesselSeeder>>());
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task SeedHydrostaticsTemplateAsync_CreatesVessel_WhenNotExists()
    {
        // Act
        await _seeder.SeedHydrostaticsTemplateAsync();

        // Assert
        var vessel = await _context.Vessels
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == TemplateVessels.HydrostaticsVesselId);

        Assert.NotNull(vessel);
        Assert.Equal("Hydrostatic Vessel", vessel.Name);
        Assert.Equal(TemplateVessels.SystemUserId, vessel.UserId);
        Assert.Equal(100m, vessel.Lpp);
        Assert.Equal(10m, vessel.Beam);
        Assert.Equal(6.25m, vessel.DesignDraft);

        // Verify geometry was created
        var stationsCount = await _context.Stations.CountAsync(s => s.VesselId == vessel.Id);
        var waterlinesCount = await _context.Waterlines.CountAsync(w => w.VesselId == vessel.Id);
        var offsetsCount = await _context.Offsets.CountAsync(o => o.VesselId == vessel.Id);
        var loadcasesCount = await _context.Loadcases.CountAsync(l => l.VesselId == vessel.Id);

        Assert.True(stationsCount > 0, "Should have stations");
        Assert.True(waterlinesCount > 0, "Should have waterlines");
        Assert.True(offsetsCount > 0, "Should have offsets");
        Assert.Equal(1, loadcasesCount); // Should have one default loadcase
    }

    [Fact]
    public async Task SeedHydrostaticsTemplateAsync_DoesNotRecreate_WhenExists()
    {
        // Arrange - Seed once
        await _seeder.SeedHydrostaticsTemplateAsync();
        var firstVessel = await _context.Vessels
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == TemplateVessels.HydrostaticsVesselId);
        Assert.NotNull(firstVessel);
        var originalUpdatedAt = firstVessel.UpdatedAt;

        // Small delay to ensure timestamp would change if updated
        await Task.Delay(10);

        // Act - Seed again
        await _seeder.SeedHydrostaticsTemplateAsync();

        // Assert - Should be the same vessel (not recreated)
        var secondVessel = await _context.Vessels
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == TemplateVessels.HydrostaticsVesselId);

        Assert.NotNull(secondVessel);
        Assert.Equal(firstVessel.Id, secondVessel.Id);
        Assert.Equal(firstVessel.Name, secondVessel.Name);
        // UpdatedAt should be unchanged (idempotent)
        Assert.Equal(originalUpdatedAt, secondVessel.UpdatedAt);
    }

    [Fact]
    public async Task SeedHydrostaticsTemplateAsync_RestoresVessel_WhenSoftDeleted()
    {
        // Arrange - Create and soft delete
        await _seeder.SeedHydrostaticsTemplateAsync();
        var vessel = await _context.Vessels
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Id == TemplateVessels.HydrostaticsVesselId);
        Assert.NotNull(vessel);

        vessel.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Act - Seed again
        await _seeder.SeedHydrostaticsTemplateAsync();

        // Assert - Should restore (DeletedAt should be null)
        var restoredVessel = await _context.Vessels
            .FirstOrDefaultAsync(v => v.Id == TemplateVessels.HydrostaticsVesselId);

        Assert.NotNull(restoredVessel);
        Assert.Null(restoredVessel.DeletedAt);
    }

    [Fact]
    public async Task SeedHydrostaticsTemplateAsync_CreatesCompleteGeometry()
    {
        // Act
        await _seeder.SeedHydrostaticsTemplateAsync();

        // Assert
        var vessel = await _context.Vessels
            .FirstOrDefaultAsync(v => v.Id == TemplateVessels.HydrostaticsVesselId);
        Assert.NotNull(vessel);

        var stations = await _context.Stations
            .Where(s => s.VesselId == vessel.Id)
            .OrderBy(s => s.StationIndex)
            .ToListAsync();

        var waterlines = await _context.Waterlines
            .Where(w => w.VesselId == vessel.Id)
            .OrderBy(w => w.WaterlineIndex)
            .ToListAsync();

        var offsets = await _context.Offsets
            .Where(o => o.VesselId == vessel.Id)
            .ToListAsync();

        // Verify geometry completeness
        Assert.Equal(21, stations.Count); // Should have 21 stations
        Assert.Equal(13, waterlines.Count); // Should have 13 waterlines
        Assert.Equal(21 * 13, offsets.Count); // Should have all offsets (21 stations × 13 waterlines)

        // Verify stations are properly ordered and positioned
        for (int i = 0; i < stations.Count - 1; i++)
        {
            Assert.True(stations[i].X <= stations[i + 1].X, "Stations should be in ascending order");
        }

        // Verify waterlines are properly ordered
        for (int i = 0; i < waterlines.Count - 1; i++)
        {
            Assert.True(waterlines[i].Z <= waterlines[i + 1].Z, "Waterlines should be in ascending order");
        }
    }

    [Fact]
    public async Task SeedHydrostaticsTemplateAsync_CreatesDefaultLoadcase()
    {
        // Act
        await _seeder.SeedHydrostaticsTemplateAsync();

        // Assert
        var vessel = await _context.Vessels
            .FirstOrDefaultAsync(v => v.Id == TemplateVessels.HydrostaticsVesselId);
        Assert.NotNull(vessel);

        var loadcase = await _context.Loadcases
            .FirstOrDefaultAsync(l => l.VesselId == vessel.Id);

        Assert.NotNull(loadcase);
        Assert.Equal("Design Condition", loadcase.Name);
        Assert.Equal(1025m, loadcase.Rho); // Saltwater density
        Assert.Equal(6.25m * 0.5m, loadcase.KG); // KG at 50% of draft
    }
}
