using DataService.Data;
using DataService.Services.Hydrostatics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using Xunit;
using FluentAssertions;

namespace DataService.Tests.Services.Hydrostatics;

public class CurvesGeneratorTests : IDisposable
{
    private readonly DataDbContext _context;
    private readonly IIntegrationEngine _integrationEngine;
    private readonly IHydroCalculator _hydroCalculator;
    private readonly ICurvesGenerator _curvesGenerator;

    public CurvesGeneratorTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new DataDbContext(options);

        // Setup services
        var integrationLogger = new Mock<ILogger<IntegrationEngine>>();
        _integrationEngine = new IntegrationEngine(integrationLogger.Object);

        var hydroLogger = new Mock<ILogger<HydroCalculator>>();
        _hydroCalculator = new HydroCalculator(_context, _integrationEngine, hydroLogger.Object);

        var curvesLogger = new Mock<ILogger<CurvesGenerator>>();
        _curvesGenerator = new CurvesGenerator(_hydroCalculator, _integrationEngine, _context, curvesLogger.Object);
    }

    #region Bonjean Curves Tests

    [Fact]
    public async Task GenerateBonjeanCurves_RectangularBarge_ReturnsCorrectNumberOfCurves()
    {
        // Arrange
        var vessel = CreateRectangularBarge(length: 100, beam: 20, draft: 5, numStations: 5, numWaterlines: 6);

        // Act
        var bonjeanCurves = await _curvesGenerator.GenerateBonjeanCurvesAsync(vessel.Id);

        // Assert
        bonjeanCurves.Should().NotBeNull();
        bonjeanCurves.Should().HaveCount(5, "should have one curve per station");
    }

    [Fact]
    public async Task GenerateBonjeanCurves_RectangularBarge_CurvesHaveCorrectStationPositions()
    {
        // Arrange
        var vessel = CreateRectangularBarge(length: 100, beam: 20, draft: 5, numStations: 5, numWaterlines: 6);

        // Act
        var bonjeanCurves = await _curvesGenerator.GenerateBonjeanCurvesAsync(vessel.Id);

        // Assert
        bonjeanCurves[0].StationX.Should().Be(0m, "first station at bow");
        bonjeanCurves[2].StationX.Should().Be(50m, "midship station at center");
        bonjeanCurves[4].StationX.Should().Be(100m, "last station at stern");
    }

    [Fact]
    public async Task GenerateBonjeanCurves_RectangularBarge_EachCurveHasPointsForAllWaterlines()
    {
        // Arrange
        var vessel = CreateRectangularBarge(length: 100, beam: 20, draft: 5, numStations: 5, numWaterlines: 6);

        // Act
        var bonjeanCurves = await _curvesGenerator.GenerateBonjeanCurvesAsync(vessel.Id);

        // Assert
        foreach (var curve in bonjeanCurves)
        {
            curve.Points.Should().HaveCount(6, "should have one point per waterline");
        }
    }

    [Fact]
    public async Task GenerateBonjeanCurves_RectangularBarge_SectionalAreasIncreaseWithDraft()
    {
        // Arrange
        var vessel = CreateRectangularBarge(length: 100, beam: 20, draft: 10, numStations: 5, numWaterlines: 11);

        // Act
        var bonjeanCurves = await _curvesGenerator.GenerateBonjeanCurvesAsync(vessel.Id);

        // Assert - sectional area should increase monotonically with draft
        foreach (var curve in bonjeanCurves)
        {
            for (int i = 1; i < curve.Points.Count; i++)
            {
                curve.Points[i].Y.Should().BeGreaterThanOrEqualTo(curve.Points[i - 1].Y,
                    $"sectional area should increase with draft at station {curve.StationIndex}");
            }
        }
    }

    [Fact]
    public async Task GenerateBonjeanCurves_RectangularBarge_SectionalAreaMatchesExpected()
    {
        // Arrange: Rectangular barge (B=20m)
        // Expected sectional area = Breadth × Draft = 20 × T
        var vessel = CreateRectangularBarge(length: 100, beam: 20, draft: 10, numStations: 5, numWaterlines: 11);

        // Act
        var bonjeanCurves = await _curvesGenerator.GenerateBonjeanCurvesAsync(vessel.Id);

        // Assert - check sectional area at T=5m (should be approximately 100 m²)
        var midshipCurve = bonjeanCurves.FirstOrDefault(c => c.StationIndex == 2); // Midship station
        midshipCurve.Should().NotBeNull();

        var pointAtDraft5 = midshipCurve!.Points.FirstOrDefault(p => Math.Abs(p.X - 5m) < 0.1m);
        pointAtDraft5.Should().NotBeNull();

        // For rectangular section: Area = B × T = 20 × 5 = 100 m²
        var expectedArea = 100m;
        var tolerance = expectedArea * 0.05m; // 5% tolerance

        pointAtDraft5!.Y.Should().BeInRange(expectedArea - tolerance, expectedArea + tolerance,
            $"sectional area at 5m draft should be approximately {expectedArea} m²");
    }

    [Fact]
    public async Task GenerateBonjeanCurves_TriangularSection_HasZeroAreaAtKeel()
    {
        // Arrange
        var vessel = CreateTriangularVessel(length: 100, beam: 20, height: 10, numStations: 5, numWaterlines: 11);

        // Act
        var bonjeanCurves = await _curvesGenerator.GenerateBonjeanCurvesAsync(vessel.Id);

        // Assert - area at keel (Z=0) should be zero
        foreach (var curve in bonjeanCurves)
        {
            var keelPoint = curve.Points.FirstOrDefault(p => p.X == 0m);
            keelPoint.Should().NotBeNull();
            keelPoint!.Y.Should().Be(0m, "sectional area at keel should be zero");
        }
    }

    [Fact]
    public async Task GenerateBonjeanCurves_NonexistentVessel_ThrowsArgumentException()
    {
        // Arrange
        var nonexistentVesselId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await _curvesGenerator.GenerateBonjeanCurvesAsync(nonexistentVesselId);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Vessel with ID {nonexistentVesselId} not found");
    }

    [Fact]
    public async Task GenerateBonjeanCurves_VesselWithoutGeometry_ThrowsInvalidOperationException()
    {
        // Arrange - vessel with no stations, waterlines, or offsets
        var vessel = new Vessel
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Empty Vessel",
            Lpp = 100,
            Beam = 20,
            DesignDraft = 5
        };
        _context.Vessels.Add(vessel);
        await _context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _curvesGenerator.GenerateBonjeanCurvesAsync(vessel.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Vessel geometry is incomplete");
    }

    #endregion

    #region Standard Curves Tests

    [Fact]
    public async Task GenerateDisplacementCurve_RectangularBarge_ReturnsCorrectData()
    {
        // Arrange
        var vessel = CreateRectangularBarge(length: 100, beam: 20, draft: 10, numStations: 5, numWaterlines: 11);

        // Act
        var curve = await _curvesGenerator.GenerateDisplacementCurveAsync(
            vessel.Id, null, minDraft: 1, maxDraft: 10, points: 10);

        // Assert
        curve.Should().NotBeNull();
        curve.Type.Should().Be("displacement");
        curve.XLabel.Should().Be("Draft (m)");
        curve.YLabel.Should().Be("Displacement (kg)");
        curve.Points.Should().HaveCount(10);
        curve.Points.Should().OnlyContain(p => p.Y > 0, "displacement should be positive");
    }

    [Fact]
    public async Task GenerateKBCurve_RectangularBarge_ReturnsCorrectData()
    {
        // Arrange
        var vessel = CreateRectangularBarge(length: 100, beam: 20, draft: 10, numStations: 5, numWaterlines: 11);

        // Act
        var curve = await _curvesGenerator.GenerateKBCurveAsync(
            vessel.Id, null, minDraft: 1, maxDraft: 10, points: 10);

        // Assert
        curve.Should().NotBeNull();
        curve.Type.Should().Be("kb");
        curve.XLabel.Should().Be("Draft (m)");
        curve.YLabel.Should().Be("KB (m)");
        curve.Points.Should().HaveCount(10);
    }

    [Fact]
    public async Task GenerateLCBCurve_RectangularBarge_ReturnsCorrectData()
    {
        // Arrange
        var vessel = CreateRectangularBarge(length: 100, beam: 20, draft: 10, numStations: 5, numWaterlines: 11);

        // Act
        var curve = await _curvesGenerator.GenerateLCBCurveAsync(
            vessel.Id, null, minDraft: 1, maxDraft: 10, points: 10);

        // Assert
        curve.Should().NotBeNull();
        curve.Type.Should().Be("lcb");
        curve.Points.Should().HaveCount(10);
        // For rectangular barge, LCB should be near midship (50m)
        curve.Points.Should().OnlyContain(p => Math.Abs(p.Y - 50m) < 5m,
            "LCB should be near midship for rectangular barge");
    }

    [Fact]
    public async Task GenerateAwpCurve_RectangularBarge_ReturnsCorrectData()
    {
        // Arrange
        var vessel = CreateRectangularBarge(length: 100, beam: 20, draft: 10, numStations: 5, numWaterlines: 11);

        // Act
        var curve = await _curvesGenerator.GenerateAwpCurveAsync(
            vessel.Id, null, minDraft: 1, maxDraft: 10, points: 10);

        // Assert
        curve.Should().NotBeNull();
        curve.Type.Should().Be("awp");
        curve.XLabel.Should().Be("Draft (m)");
        curve.YLabel.Should().Be("Waterplane Area (m²)");
        curve.Points.Should().HaveCount(10);
        // For rectangular barge, waterplane area should be approximately L × B = 2000 m²
        curve.Points.Should().OnlyContain(p => Math.Abs(p.Y - 2000m) < 200m,
            "waterplane area should be approximately L × B");
    }

    [Fact]
    public async Task GenerateGMtCurve_WithoutLoadcase_ThrowsArgumentException()
    {
        // Arrange
        var vessel = CreateRectangularBarge(length: 100, beam: 20, draft: 10, numStations: 5, numWaterlines: 11);

        // Act
        Func<Task> act = async () => await _curvesGenerator.GenerateGMtCurveAsync(
            vessel.Id, null, minDraft: 1, maxDraft: 10, points: 10);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Loadcase ID is required to compute GM");
    }

    [Fact]
    public async Task GenerateMultipleCurves_RequestsMultipleTypes_ReturnsAllCurves()
    {
        // Arrange
        var vessel = CreateRectangularBarge(length: 100, beam: 20, draft: 10, numStations: 5, numWaterlines: 11);
        var curveTypes = new List<string> { "displacement", "kb", "lcb", "awp" };

        // Act
        var curves = await _curvesGenerator.GenerateMultipleCurvesAsync(
            vessel.Id, null, curveTypes, minDraft: 1, maxDraft: 10, points: 10);

        // Assert
        curves.Should().NotBeNull();
        curves.Should().HaveCount(4);
        curves.Should().ContainKeys("displacement", "kb", "lcb", "awp");
    }

    #endregion

    #region Helper Methods

    private Vessel CreateRectangularBarge(decimal length, decimal beam, decimal draft, int numStations, int numWaterlines)
    {
        var vessel = new Vessel
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Rectangular Barge Test",
            Lpp = length,
            Beam = beam,
            DesignDraft = draft
        };

        _context.Vessels.Add(vessel);

        // Create stations equally spaced from 0 to length
        for (int i = 0; i < numStations; i++)
        {
            var station = new Station
            {
                Id = Guid.NewGuid(),
                VesselId = vessel.Id,
                StationIndex = i,
                X = i * length / (numStations - 1)
            };
            _context.Stations.Add(station);
        }

        // Create waterlines from 0 to draft
        for (int i = 0; i < numWaterlines; i++)
        {
            var waterline = new Waterline
            {
                Id = Guid.NewGuid(),
                VesselId = vessel.Id,
                WaterlineIndex = i,
                Z = i * draft / (numWaterlines - 1)
            };
            _context.Waterlines.Add(waterline);
        }

        // Create offsets: all half-breadths = beam/2 (rectangular)
        for (int s = 0; s < numStations; s++)
        {
            for (int w = 0; w < numWaterlines; w++)
            {
                var offset = new Offset
                {
                    Id = Guid.NewGuid(),
                    VesselId = vessel.Id,
                    StationIndex = s,
                    WaterlineIndex = w,
                    HalfBreadthY = beam / 2 // Rectangular: constant breadth
                };
                _context.Offsets.Add(offset);
            }
        }

        _context.SaveChanges();
        return vessel;
    }

    private Vessel CreateTriangularVessel(decimal length, decimal beam, decimal height, int numStations, int numWaterlines)
    {
        var vessel = new Vessel
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Triangular Section Vessel",
            Lpp = length,
            Beam = beam,
            DesignDraft = height
        };

        _context.Vessels.Add(vessel);

        // Create stations equally spaced
        for (int i = 0; i < numStations; i++)
        {
            var station = new Station
            {
                Id = Guid.NewGuid(),
                VesselId = vessel.Id,
                StationIndex = i,
                X = i * length / (numStations - 1)
            };
            _context.Stations.Add(station);
        }

        // Create waterlines from 0 to height
        for (int i = 0; i < numWaterlines; i++)
        {
            var waterline = new Waterline
            {
                Id = Guid.NewGuid(),
                VesselId = vessel.Id,
                WaterlineIndex = i,
                Z = i * height / (numWaterlines - 1)
            };
            _context.Waterlines.Add(waterline);
        }

        // Create offsets: triangular section (half-breadth proportional to height)
        // At Z=0 (keel): y = 0
        // At Z=height: y = beam/2
        for (int s = 0; s < numStations; s++)
        {
            for (int w = 0; w < numWaterlines; w++)
            {
                var z = w * height / (numWaterlines - 1);
                var halfBreadth = (beam / 2) * (z / height); // Linear relationship

                var offset = new Offset
                {
                    Id = Guid.NewGuid(),
                    VesselId = vessel.Id,
                    StationIndex = s,
                    WaterlineIndex = w,
                    HalfBreadthY = halfBreadth
                };
                _context.Offsets.Add(offset);
            }
        }

        _context.SaveChanges();
        return vessel;
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}

