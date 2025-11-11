using DataService.Data;
using DataService.Services.Hydrostatics;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using Xunit;

namespace DataService.Tests.Services.Hydrostatics;

public class DiagonalsServiceTests
{
    private readonly DataDbContext _context;
    private readonly DiagonalsService _service;
    private readonly Mock<ILogger<DiagonalsService>> _mockLogger;

    public DiagonalsServiceTests()
    {
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new DataDbContext(options);
        _mockLogger = new Mock<ILogger<DiagonalsService>>();
        _service = new DiagonalsService(_context, _mockLogger.Object);
    }

    [Fact]
    public async Task GetDiagonalsAsync_WithValidGeometry_ReturnsDiagonals()
    {
        // Arrange
        var vesselId = Guid.NewGuid();
        var vessel = new Vessel
        {
            Id = vesselId,
            Name = "Test Vessel",
            Lpp = 100m,
            Beam = 20m,
            DesignDraft = 8m
        };

        await _context.Vessels.AddAsync(vessel);

        // Create simple rectangular barge geometry
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

        var offsets = new List<Offset>();
        foreach (var station in stations)
        {
            foreach (var waterline in waterlines)
            {
                // Create elliptical section: Y = B/2 * sqrt(1 - (Z/D)^2)
                // Where B = 20m (full beam), D = 10m (design draft)
                decimal beam = 20m;
                decimal draft = 10m;
                decimal zNorm = waterline.Z / draft;
                decimal halfBreadth = (beam / 2m) * (decimal)Math.Sqrt((double)(1m - zNorm * zNorm));
                
                offsets.Add(new Offset
                {
                    VesselId = vesselId,
                    StationIndex = station.StationIndex,
                    WaterlineIndex = waterline.WaterlineIndex,
                    HalfBreadthY = halfBreadth // Elliptical section (curved)
                });
            }
        }
        await _context.Offsets.AddRangeAsync(offsets);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetDiagonalsAsync(vesselId, 3, default);

        // Assert
        result.Should().NotBeNull();
        result.Diagonals.Should().NotBeEmpty("elliptical hull should have diagonal intersections");
        result.Diagonals.Count.Should().BeLessThanOrEqualTo(3, "should not exceed requested count");

        // Each diagonal should have points
        foreach (var diagonal in result.Diagonals)
        {
            diagonal.Angle.Should().Be(45m);
            diagonal.Points.Should().NotBeEmpty("each diagonal should intersect the curved hull");
        }
    }

    [Fact]
    public async Task GetDiagonalsAsync_WithNoGeometry_ReturnsEmpty()
    {
        // Arrange
        var vesselId = Guid.NewGuid();

        // Act
        var result = await _service.GetDiagonalsAsync(vesselId, 3, default);

        // Assert
        result.Should().NotBeNull();
        result.Diagonals.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    public async Task GetDiagonalsAsync_WithInvalidCount_ThrowsArgumentException(int numDiagonals)
    {
        // Arrange
        var vesselId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetDiagonalsAsync(vesselId, numDiagonals, default));
    }
}
