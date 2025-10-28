using DataService.Data;
using DataService.Services.Hydrostatics;
using DataService.Tests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.DTOs;
using Shared.Models;
using Shared.TestData;
using Xunit;

namespace DataService.Tests.Services.Hydrostatics;

/// <summary>
/// Stability tests using rectangular barge with analytical solutions
/// Target: < 0.5% error compared to analytical formulas
/// </summary>
public class BargeStabilityTests : IDisposable
{
    private readonly DataDbContext _context;
    private readonly IIntegrationEngine _integrationEngine;
    private readonly IHydroCalculator _hydroCalculator;
    private readonly IStabilityCalculator _stabilityCalculator;
    private readonly IStabilityCriteriaChecker _criteriaChecker;
    private readonly Guid _vesselId;
    private readonly Guid _loadcaseId;

    public BargeStabilityTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: $"BargeStabilityTest_{Guid.NewGuid()}")
            .Options;

        _context = new DataDbContext(options);
        _integrationEngine = new IntegrationEngine(Mock.Of<ILogger<IntegrationEngine>>());
        _hydroCalculator = new HydroCalculator(_context, _integrationEngine, Mock.Of<ILogger<HydroCalculator>>());
        _stabilityCalculator = new StabilityCalculator(_context, _hydroCalculator, _integrationEngine, Mock.Of<ILogger<StabilityCalculator>>());
        _criteriaChecker = new StabilityCriteriaChecker(Mock.Of<ILogger<StabilityCriteriaChecker>>());

        // Create test barge with standard dimensions
        var (beam, draft, kg) = BargeGZReference.GetStandardBarge();
        (_vesselId, _loadcaseId) = SetupRectangularBarge(100m, beam, draft, kg);
    }

    private (Guid vesselId, Guid loadcaseId) SetupRectangularBarge(
        decimal length,
        decimal beam,
        decimal draft,
        decimal kg)
    {
        // Create vessel
        var vessel = new Vessel
        {
            Id = Guid.NewGuid(),
            Name = "Test Barge",
            Lpp = length,
            Beam = beam,
            DesignDraft = draft,
            CreatedAt = DateTime.UtcNow
        };

        _context.Vessels.Add(vessel);

        // Create rectangular barge geometry (20 stations, 10 waterlines)
        var (stations, waterlines, offsets) = HullTestData.GenerateRectangularBarge(
            length, beam, draft, numStations: 21, numWaterlines: 11);

        foreach (var station in stations)
        {
            _context.Stations.Add(new Station
            {
                VesselId = vessel.Id,
                StationIndex = station.Index,
                X = station.X
            });
        }

        foreach (var waterline in waterlines)
        {
            _context.Waterlines.Add(new Waterline
            {
                VesselId = vessel.Id,
                WaterlineIndex = waterline.Index,
                Z = waterline.Z
            });
        }

        foreach (var offset in offsets)
        {
            _context.Offsets.Add(new Offset
            {
                VesselId = vessel.Id,
                StationIndex = offset.StationIndex,
                WaterlineIndex = offset.WaterlineIndex,
                HalfBreadthY = offset.HalfBreadthY
            });
        }

        // Create loadcase
        var loadcase = new Loadcase
        {
            Id = Guid.NewGuid(),
            VesselId = vessel.Id,
            Name = "Design Condition",
            Rho = 1025m,
            KG = kg
        };

        _context.Loadcases.Add(loadcase);
        _context.SaveChanges();

        return (vessel.Id, loadcase.Id);
    }

    [Fact]
    public async Task Test1_BargeGZ_WallSidedMethod_MatchesAnalytical()
    {
        // Arrange
        var (beam, draft, kg) = BargeGZReference.GetStandardBarge();

        var request = new StabilityRequestDto
        {
            LoadcaseId = _loadcaseId,
            MinAngle = 0m,
            MaxAngle = 30m,
            AngleIncrement = 5m,
            Method = "WallSided",
            Draft = draft
        };

        // Act
        var result = await _stabilityCalculator.ComputeGZCurveAsync(_vesselId, request);

        // Assert
        Assert.NotEmpty(result.Points);

        foreach (var point in result.Points)
        {
            var analyticalGZ = BargeGZReference.ComputeAnalyticalGZ(beam, draft, point.HeelAngle);
            var error = Math.Abs((point.GZ - analyticalGZ) / (analyticalGZ != 0 ? analyticalGZ : 1m));

            Assert.True(error < 0.005m,
                $"GZ at {point.HeelAngle}°: Expected {analyticalGZ:F4}m, Got {point.GZ:F4}m (error {error:P2})");
        }
    }

    [Fact]
    public async Task Test2_BargeGZ_AreaUnderCurve_MatchesAnalytical()
    {
        // Arrange
        var (beam, draft, kg) = BargeGZReference.GetStandardBarge();

        var request = new StabilityRequestDto
        {
            LoadcaseId = _loadcaseId,
            MinAngle = 0m,
            MaxAngle = 40m,
            AngleIncrement = 1m,
            Method = "WallSided",
            Draft = draft
        };

        // Act
        var result = await _stabilityCalculator.ComputeGZCurveAsync(_vesselId, request);

        // Compute area using criteria checker
        var area_0_30 = _criteriaChecker.CalculateAreaUnderCurve(result.Points, 0m, 30m);
        var area_0_40 = _criteriaChecker.CalculateAreaUnderCurve(result.Points, 0m, 40m);

        // Compare with analytical
        var analyticalArea_0_30 = BargeGZReference.ComputeAreaUnderCurve(beam, draft, 0m, 30m);
        var analyticalArea_0_40 = BargeGZReference.ComputeAreaUnderCurve(beam, draft, 0m, 40m);

        // Assert
        var error_0_30 = Math.Abs((area_0_30 - analyticalArea_0_30) / analyticalArea_0_30);
        var error_0_40 = Math.Abs((area_0_40 - analyticalArea_0_40) / analyticalArea_0_40);

        Assert.True(error_0_30 < 0.005m,
            $"Area 0-30°: Expected {analyticalArea_0_30:F6} m·rad, Got {area_0_30:F6} m·rad (error {error_0_30:P2})");

        Assert.True(error_0_40 < 0.005m,
            $"Area 0-40°: Expected {analyticalArea_0_40:F6} m·rad, Got {area_0_40:F6} m·rad (error {error_0_40:P2})");
    }

    [Fact]
    public async Task Test3_BargeGZ_MaxGZAngle_Near45Degrees()
    {
        // Arrange
        var (beam, draft, kg) = BargeGZReference.GetStandardBarge();

        var request = new StabilityRequestDto
        {
            LoadcaseId = _loadcaseId,
            MinAngle = 0m,
            MaxAngle = 90m,
            AngleIncrement = 1m,
            Method = "WallSided",
            Draft = draft
        };

        // Act
        var result = await _stabilityCalculator.ComputeGZCurveAsync(_vesselId, request);

        // Assert
        var expectedMaxAngle = BargeGZReference.GetExpectedMaxGZAngle(); // 45°

        // Allow ±5° tolerance
        Assert.True(Math.Abs(result.AngleAtMaxGZ - expectedMaxAngle) <= 5m,
            $"Max GZ angle: Expected ~{expectedMaxAngle}°, Got {result.AngleAtMaxGZ}°");

        // Max GZ should be positive
        Assert.True(result.MaxGZ > 0,
            $"Max GZ should be positive, got {result.MaxGZ}m");
    }

    [Fact]
    public async Task Test4_BargeGMT_MatchesDirectCalculation()
    {
        // Arrange
        var (beam, draft, kg) = BargeGZReference.GetStandardBarge();

        var request = new StabilityRequestDto
        {
            LoadcaseId = _loadcaseId,
            MinAngle = 0m,
            MaxAngle = 10m,
            AngleIncrement = 1m,
            Method = "WallSided",
            Draft = draft
        };

        // Act
        var result = await _stabilityCalculator.ComputeGZCurveAsync(_vesselId, request);

        // Compute analytical GMT
        var analyticalGMT = BargeGZReference.ComputeGMT(beam, draft, kg);

        // Assert
        var error = Math.Abs((result.InitialGMT - analyticalGMT) / analyticalGMT);

        Assert.True(error < 0.005m,
            $"GMT: Expected {analyticalGMT:F4}m, Got {result.InitialGMT:F4}m (error {error:P2})");
    }

    [Fact]
    public async Task Test5_StableBarge_PassesIMOCriteria()
    {
        // Arrange - Create stable barge with low KG
        var (beam, draft, kg) = BargeGZReference.GetStableBarge();
        var (stableVesselId, stableLoadcaseId) = SetupRectangularBarge(100m, beam, draft, kg);

        var request = new StabilityRequestDto
        {
            LoadcaseId = stableLoadcaseId,
            MinAngle = 0m,
            MaxAngle = 60m,
            AngleIncrement = 1m,
            Method = "WallSided",
            Draft = draft
        };

        // Act
        var curve = await _stabilityCalculator.ComputeGZCurveAsync(stableVesselId, request);
        var criteriaResult = _criteriaChecker.CheckIntactStabilityCriteria(curve);

        // Assert
        Assert.True(criteriaResult.AllCriteriaPassed,
            $"Stable barge should pass all criteria. Summary: {criteriaResult.Summary}");

        Assert.Equal(6, criteriaResult.Criteria.Count); // Should have 6 IMO criteria
        Assert.All(criteriaResult.Criteria, c => Assert.True(c.Passed, $"Criterion '{c.Name}' failed: {c.ActualValue} vs {c.RequiredValue}"));
    }

    [Fact]
    public async Task Test6_UnstableBarge_FailsGMCriterion()
    {
        // Arrange - Create unstable barge with high KG (negative GMT)
        var (beam, draft, kg) = BargeGZReference.GetUnstableBarge();
        var (unstableVesselId, unstableLoadcaseId) = SetupRectangularBarge(100m, beam, draft, kg);

        var request = new StabilityRequestDto
        {
            LoadcaseId = unstableLoadcaseId,
            MinAngle = 0m,
            MaxAngle = 10m,
            AngleIncrement = 1m,
            Method = "WallSided",
            Draft = draft
        };

        // Act
        var curve = await _stabilityCalculator.ComputeGZCurveAsync(unstableVesselId, request);
        var criteriaResult = _criteriaChecker.CheckIntactStabilityCriteria(curve);

        // Assert
        Assert.False(criteriaResult.AllCriteriaPassed,
            "Unstable barge should fail at least one criterion");

        // Should specifically fail GMT criterion
        var gmtCriterion = criteriaResult.Criteria.FirstOrDefault(c => c.Name.Contains("GMT"));
        Assert.NotNull(gmtCriterion);
        Assert.False(gmtCriterion.Passed, "GMT criterion should fail for unstable barge");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}

