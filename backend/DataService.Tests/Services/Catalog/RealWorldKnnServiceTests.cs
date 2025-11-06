using DataService.Data;
using DataService.Services.Catalog;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using Xunit;

namespace DataService.Tests.Services.Catalog;

public class RealWorldKnnServiceTests : IDisposable
{
    private readonly DataDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly RealWorldKnnService _service;
    private readonly Mock<ILogger<RealWorldKnnService>> _loggerMock;

    public RealWorldKnnServiceTests()
    {
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataDbContext(options);
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<RealWorldKnnService>>();
        _service = new RealWorldKnnService(_context, _cache, _loggerMock.Object);

        // Seed test data
        SeedTestCatalog();
    }

    private void SeedTestCatalog()
    {
        var vessels = new List<CatalogVesselReal>
        {
            // Container ships
            new CatalogVesselReal
            {
                Id = Guid.NewGuid(),
                VesselId = "KCS",
                VesselType = "Container",
                LppM = 230.0m,
                BeamM = 32.2m,
                DraftM = 10.8m,
                DepthM = 19.0m,
                DisplacementT = 52030.0m,
                Cb = 0.6505m,
                ServiceSpeedMs = 12.34m,
                IsSystemData = true
            },
            new CatalogVesselReal
            {
                Id = Guid.NewGuid(),
                VesselId = "CONTAINER_MEDIUM",
                VesselType = "Container",
                LppM = 180.0m,
                BeamM = 28.0m,
                DraftM = 9.5m,
                DepthM = 16.0m,
                DisplacementT = 35000.0m,
                Cb = 0.63m,
                ServiceSpeedMs = 11.5m,
                IsSystemData = true
            },
            // Tankers
            new CatalogVesselReal
            {
                Id = Guid.NewGuid(),
                VesselId = "KVLCC2",
                VesselType = "Tanker",
                LppM = 320.0m,
                BeamM = 58.0m,
                DraftM = 20.8m,
                DepthM = 30.0m,
                DisplacementT = 312622.0m,
                Cb = 0.8098m,
                ServiceSpeedMs = 15.5m,
                IsSystemData = true
            },
            new CatalogVesselReal
            {
                Id = Guid.NewGuid(),
                VesselId = "TANKER_SMALL",
                VesselType = "Tanker",
                LppM = 150.0m,
                BeamM = 25.0m,
                DraftM = 10.0m,
                DepthM = 15.0m,
                DisplacementT = 25000.0m,
                Cb = 0.78m,
                ServiceSpeedMs = 8.0m,
                IsSystemData = true
            },
            // Naval combatant
            new CatalogVesselReal
            {
                Id = Guid.NewGuid(),
                VesselId = "DTMB_5415",
                VesselType = "Naval combatant",
                LppM = 142.0m,
                BeamM = 17.983m,
                DraftM = 6.179m,
                DepthM = 9.3m,
                DisplacementT = 12901.6m,
                Cb = 0.506m,
                ServiceSpeedMs = 10.0m,
                IsSystemData = true
            }
        };

        _context.CatalogVesselsReal.AddRange(vessels);
        _context.SaveChanges();
    }

    [Fact]
    public async Task FindSimilarVesselsAsync_ContainerMission_ReturnsContainers()
    {
        // Arrange
        var criteria = new MissionSearchCriteria
        {
            VesselType = "Container",
            TargetDisplacement = 50000.0m,
            ServiceSpeed = 12.0m,
            MaxBeam = 35.0m,
            MaxDraft = 12.0m
        };

        // Act
        var results = await _service.FindSimilarVesselsAsync(criteria, K: 5);

        // Assert
        results.Should().NotBeEmpty();
        results.Count.Should().BeLessThanOrEqualTo(5);
        results.First().VesselType.Should().Be("Container");  // Same type first
        results.First().SimilarityScore.Should().BeGreaterThan(0.5);  // Reasonable match
    }

    [Fact]
    public async Task FindSimilarVesselsAsync_OrdersByProximity()
    {
        // Arrange - target close to KCS
        var criteria = new MissionSearchCriteria
        {
            VesselType = "Container",
            TargetDisplacement = 52000.0m,  // Very close to KCS (52,030)
            ServiceSpeed = 12.3m,
            MaxBeam = 35.0m,
            MaxDraft = 12.0m
        };

        // Act
        var results = await _service.FindSimilarVesselsAsync(criteria, K: 5);

        // Assert
        results.Should().NotBeEmpty();
        results.First().VesselName.Should().Be("KCS");  // Closest match
        results.First().SimilarityScore.Should().BeGreaterThan(0.9);  // Very similar
    }

    [Fact]
    public async Task FindSimilarVesselsAsync_FewMatches_FallbackToAllTypes()
    {
        // Arrange - only 1 naval combatant in test data
        var criteria = new MissionSearchCriteria
        {
            VesselType = "Naval combatant",
            TargetDisplacement = 15000.0m,
            ServiceSpeed = 15.0m,
            MaxBeam = 20.0m,
            MaxDraft = 8.0m
        };

        // Act
        var results = await _service.FindSimilarVesselsAsync(criteria, K: 5);

        // Assert
        results.Should().NotBeEmpty();
        // Should have expanded to other types since only 1 naval in catalog
        results.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task FindSimilarVesselsAsync_CachesResults()
    {
        // Arrange
        var criteria = new MissionSearchCriteria
        {
            VesselType = "Container",
            TargetDisplacement = 50000.0m,
            ServiceSpeed = 12.0m
        };

        // Act - First call loads from DB
        var results1 = await _service.FindSimilarVesselsAsync(criteria, K: 5);
        
        // Act - Second call should use cache
        var results2 = await _service.FindSimilarVesselsAsync(criteria, K: 5);

        // Assert
        results1.Should().BeEquivalentTo(results2);
        // Both calls should return same vessels (cache working)
    }

    [Fact]
    public async Task FindSimilarVesselsAsync_EmptyCatalog_ReturnsEmpty()
    {
        // Arrange - clear catalog
        _context.CatalogVesselsReal.RemoveRange(_context.CatalogVesselsReal);
        await _context.SaveChangesAsync();
        _service.ClearCache();

        var criteria = new MissionSearchCriteria
        {
            VesselType = "Container",
            TargetDisplacement = 50000.0m,
            ServiceSpeed = 12.0m
        };

        // Act
        var results = await _service.FindSimilarVesselsAsync(criteria, K: 5);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task FindSimilarVesselsAsync_ReturnsTopK()
    {
        // Arrange
        var criteria = new MissionSearchCriteria
        {
            VesselType = "Container",
            TargetDisplacement = 50000.0m,
            ServiceSpeed = 12.0m
        };

        // Act
        var results = await _service.FindSimilarVesselsAsync(criteria, K: 3);

        // Assert
        results.Count.Should().BeLessThanOrEqualTo(3);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _cache.Dispose();
    }
}

