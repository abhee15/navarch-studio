using DataService.Data;
using DataService.Services.Hydrostatics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using Xunit;

namespace DataService.Tests.Services.Hydrostatics;

public class HydroCalculatorTests : IDisposable
{
    private readonly DataDbContext _context;
    private readonly IIntegrationEngine _integrationEngine;
    private readonly IHydroCalculator _hydroCalculator;

    public HydroCalculatorTests()
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
    }

    [Fact]
    public async Task RectangularBarge_Displacement_MatchesAnalytical()
    {
        // Arrange: Rectangular barge (L=100m, B=20m, T=5m)
        var barge = CreateRectangularBarge(length: 100, beam: 20, draft: 5);
        var rho = 1025m; // kg/m³

        // Act
        var result = await _hydroCalculator.ComputeAtDraftAsync(barge.Id, null, draft: 5.0m);

        // Assert
        var expectedVolume = 100m * 20m * 5m; // 10,000 m³
        var expectedDisplacement = expectedVolume * rho; // 10,250,000 kg

        Assert.True(Math.Abs((result.DispVolume ?? 0) - expectedVolume) / expectedVolume < 0.005m,
            $"Volume error: Expected {expectedVolume}, got {result.DispVolume}");

        Assert.True(Math.Abs((result.DispWeight ?? 0) - expectedDisplacement) / expectedDisplacement < 0.005m,
            $"Displacement error: Expected {expectedDisplacement}, got {result.DispWeight}");
    }

    [Fact]
    public async Task RectangularBarge_KB_MatchesAnalytical()
    {
        // Arrange: Rectangular barge (L=100m, B=20m, T=5m)
        var barge = CreateRectangularBarge(length: 100, beam: 20, draft: 5);

        // Act
        var result = await _hydroCalculator.ComputeAtDraftAsync(barge.Id, null, draft: 5.0m);

        // Assert: KB for rectangular barge = T/2 = 2.5m
        var expectedKB = 2.5m;

        Assert.True(Math.Abs((result.KBz ?? 0) - expectedKB) < 0.01m,
            $"KB error: Expected {expectedKB}, got {result.KBz}");
    }

    [Fact]
    public async Task RectangularBarge_BMt_MatchesAnalytical()
    {
        // Arrange: Rectangular barge (L=100m, B=20m, T=5m)
        var barge = CreateRectangularBarge(length: 100, beam: 20, draft: 5);

        // Act
        var result = await _hydroCalculator.ComputeAtDraftAsync(barge.Id, null, draft: 5.0m);

        // Assert: BM_t = (B²/12) / T = (20²/12) / 5 = 400/12/5 ≈ 6.667m
        var expectedBMt = (20m * 20m / 12m) / 5m; // 6.667m

        Assert.True(Math.Abs((result.BMt ?? 0) - expectedBMt) / expectedBMt < 0.05m,
            $"BMt error: Expected {expectedBMt}, got {result.BMt}. Error: {Math.Abs((result.BMt ?? 0) - expectedBMt) / expectedBMt * 100:F2}%");
    }

    [Fact]
    public async Task RectangularBarge_LCB_IsAtMidship()
    {
        // Arrange: Rectangular barge (L=100m, B=20m, T=5m)
        var barge = CreateRectangularBarge(length: 100, beam: 20, draft: 5);

        // Act
        var result = await _hydroCalculator.ComputeAtDraftAsync(barge.Id, null, draft: 5.0m);

        // Assert: LCB should be at midship (50m)
        var expectedLCB = 50m;

        Assert.True(Math.Abs((result.LCBx ?? 0) - expectedLCB) < 1m,
            $"LCB error: Expected {expectedLCB}, got {result.LCBx}");
    }

    [Fact]
    public async Task RectangularBarge_Cb_IsUnity()
    {
        // Arrange: Rectangular barge (L=100m, B=20m, T=5m)
        var barge = CreateRectangularBarge(length: 100, beam: 20, draft: 5);

        // Act
        var result = await _hydroCalculator.ComputeAtDraftAsync(barge.Id, null, draft: 5.0m);

        // Assert: Cb = 1.0 for rectangular barge
        Assert.True(Math.Abs((result.Cb ?? 0) - 1.0m) < 0.01m,
            $"Cb error: Expected 1.0, got {result.Cb}");
    }

    [Fact]
    public async Task RectangularBarge_WithLoadcase_CalculatesGM()
    {
        // Arrange
        var barge = CreateRectangularBarge(length: 100, beam: 20, draft: 5);
        var loadcase = new Loadcase
        {
            Id = Guid.NewGuid(),
            VesselId = barge.Id,
            Name = "Design",
            Rho = 1025m,
            KG = 4.0m // Center of gravity at 4m
        };
        _context.Loadcases.Add(loadcase);
        await _context.SaveChangesAsync();

        // Act
        var result = await _hydroCalculator.ComputeAtDraftAsync(barge.Id, loadcase.Id, draft: 5.0m);

        // Assert: GM_t = KB + BM_t - KG = 2.5 + 6.667 - 4.0 ≈ 5.167m
        Assert.NotNull(result.GMt);
        Assert.True(result.GMt > 4m && result.GMt < 6m,
            $"GMt out of expected range: got {result.GMt}");
    }

    private Vessel CreateRectangularBarge(decimal length, decimal beam, decimal draft)
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

        // Create stations: 5 stations equally spaced from 0 to length
        int numStations = 5;
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

        // Create waterlines: 3 waterlines from 0 to draft
        int numWaterlines = 3;
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

    public void Dispose()
    {
        _context?.Dispose();
    }
}

