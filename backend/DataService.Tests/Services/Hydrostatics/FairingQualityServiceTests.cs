using DataService.Data;
using DataService.Services.Hydrostatics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using Xunit;

namespace DataService.Tests.Services.Hydrostatics;

public class FairingQualityServiceTests
{
    private readonly DataDbContext _context;
    private readonly FairingQualityService _service;
    private readonly Mock<ILogger<FairingQualityService>> _mockLogger;

    public FairingQualityServiceTests()
    {
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new DataDbContext(options);
        _mockLogger = new Mock<ILogger<FairingQualityService>>();
        _service = new FairingQualityService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task AnalyzeFairingQualityAsync_WithSmoothCurve_ReturnsHighScore()
    {
        // Arrange
        var vesselId = Guid.NewGuid();
        var vessel = new Vessel
        {
            Id = vesselId,
            Name = "Smooth Hull",
            Lpp = 100m,
            Beam = 20m,
            DesignDraft = 8m
        };
        await _context.Vessels.AddAsync(vessel);

        // Create smooth parabolic offsets
        var stations = new List<Station>();
        for (int i = 0; i <= 10; i++)
        {
            stations.Add(new Station { VesselId = vesselId, StationIndex = i, X = i * 10m });
        }
        await _context.Stations.AddRangeAsync(stations);

        var waterlines = new List<Waterline>();
        for (int i = 0; i <= 10; i++)
        {
            waterlines.Add(new Waterline { VesselId = vesselId, WaterlineIndex = i, Z = i * 1m });
        }
        await _context.Waterlines.AddRangeAsync(waterlines);

        // Parabolic offsets (smooth curve)
        var offsets = new List<Offset>();
        foreach (var station in stations)
        {
            foreach (var waterline in waterlines)
            {
                // Parabolic: Y = 10 * sqrt(1 - (Z/10)²)
                decimal zNorm = waterline.Z / 10m;
                decimal halfBreadth = 10m * (decimal)Math.Sqrt(Math.Max(0, (double)(1m - zNorm * zNorm)));

                offsets.Add(new Offset
                {
                    VesselId = vesselId,
                    StationIndex = station.StationIndex,
                    WaterlineIndex = waterline.WaterlineIndex,
                    HalfBreadthY = halfBreadth
                });
            }
        }
        await _context.Offsets.AddRangeAsync(offsets);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.AnalyzeFairingQualityAsync(vesselId, default);

        // Assert
        result.Should().NotBeNull();
        result.StationQualities.Should().HaveCount(11);
        result.OverallScore.Should().BeGreaterThan(70m, "smooth curve should have high quality score");

        // Most stations should have "Good" quality
        var goodStations = result.StationQualities.Count(sq => sq.QualityLevel == "Good");
        goodStations.Should().BeGreaterThan(5, "majority of stations should be well-faired");
    }

    [Fact]
    public async Task AnalyzeFairingQualityAsync_WithNoGeometry_ReturnsZeroScore()
    {
        // Arrange
        var vesselId = Guid.NewGuid();

        // Act
        var result = await _service.AnalyzeFairingQualityAsync(vesselId, default);

        // Assert
        result.Should().NotBeNull();
        result.StationQualities.Should().BeEmpty();
        result.OverallScore.Should().Be(0m);
    }
}
