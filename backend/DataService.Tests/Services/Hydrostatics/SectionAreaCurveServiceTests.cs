using DataService.Data;
using DataService.Services.Hydrostatics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using Xunit;

namespace DataService.Tests.Services.Hydrostatics;

public class SectionAreaCurveServiceTests
{
    private readonly DataDbContext _context;
    private readonly SectionAreaCurveService _service;
    private readonly Mock<ILogger<SectionAreaCurveService>> _mockLogger;
    private readonly IIntegrationEngine _integrationEngine;

    public SectionAreaCurveServiceTests()
    {
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new DataDbContext(options);
        _mockLogger = new Mock<ILogger<SectionAreaCurveService>>();
        _integrationEngine = new IntegrationEngine(Mock.Of<ILogger<IntegrationEngine>>());
        _service = new SectionAreaCurveService(_context, _integrationEngine, _mockLogger.Object);
    }

    [Fact]
    public async Task GetSectionAreaCurveAsync_WithRectangularBarge_ReturnsCorrectAreas()
    {
        // Arrange - Rectangular barge: 100m × 20m × 10m depth
        var vesselId = Guid.NewGuid();
        var vessel = new Vessel
        {
            Id = vesselId,
            Name = "Rectangular Barge",
            Lpp = 100m,
            Beam = 20m,
            DesignDraft = 5m
        };
        await _context.Vessels.AddAsync(vessel);

        // 11 stations from 0 to 100m
        var stations = new List<Station>();
        for (int i = 0; i <= 10; i++)
        {
            stations.Add(new Station { VesselId = vesselId, StationIndex = i, X = i * 10m });
        }
        await _context.Stations.AddRangeAsync(stations);

        // 11 waterlines from 0 to 10m
        var waterlines = new List<Waterline>();
        for (int i = 0; i <= 10; i++)
        {
            waterlines.Add(new Waterline { VesselId = vesselId, WaterlineIndex = i, Z = i * 1m });
        }
        await _context.Waterlines.AddRangeAsync(waterlines);

        // All offsets = 10m (half-breadth)
        var offsets = new List<Offset>();
        foreach (var station in stations)
        {
            foreach (var waterline in waterlines)
            {
                offsets.Add(new Offset
                {
                    VesselId = vesselId,
                    StationIndex = station.StationIndex,
                    WaterlineIndex = waterline.WaterlineIndex,
                    HalfBreadthY = 10m
                });
            }
        }
        await _context.Offsets.AddRangeAsync(offsets);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetSectionAreaCurveAsync(vesselId, default);

        // Assert
        result.Should().NotBeNull();
        result.StationPositions.Should().HaveCount(11);
        result.SectionalAreas.Should().HaveCount(11);

        // For rectangular section: Area = 2 × (Breadth/2) × Height = Breadth × Height = 20 × 10 = 200 m²
        foreach (var area in result.SectionalAreas)
        {
            area.Should().BeApproximately(200m, 1m, "rectangular barge should have constant sectional area");
        }
    }

    [Fact]
    public async Task GetSectionAreaCurveAsync_WithNoGeometry_ReturnsEmpty()
    {
        // Arrange
        var vesselId = Guid.NewGuid();

        // Act
        var result = await _service.GetSectionAreaCurveAsync(vesselId, default);

        // Assert
        result.Should().NotBeNull();
        result.StationPositions.Should().BeEmpty();
        result.SectionalAreas.Should().BeEmpty();
    }
}
