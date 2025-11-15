using DataService.Data;
using DataService.Services.Hydrostatics;
using DataService.Tests.TestData;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models;
using Shared.TestData;
using Xunit;

namespace DataService.Tests.Services.Hydrostatics;

/// <summary>
/// Barge validation tests using reference data constants
/// Validates hydrostatic calculations against known reference values
/// </summary>
public class BargeValidationTests : IDisposable
{
    private readonly DataDbContext _context;
    private readonly IIntegrationEngine _integrationEngine;
    private readonly IHydroCalculator _hydroCalculator;
    private readonly Vessel _vessel;
    private readonly Loadcase _loadcase;

    public BargeValidationTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<DataDbContext>()
            .UseInMemoryDatabase(databaseName: $"BargeValidation_{Guid.NewGuid()}")
            .Options;

        _context = new DataDbContext(options);
        _integrationEngine = new IntegrationEngine(Mock.Of<ILogger<IntegrationEngine>>());
        _hydroCalculator = new HydroCalculator(_context, _integrationEngine, Mock.Of<ILogger<HydroCalculator>>());

        // Create rectangular barge: L=100m, B=20m (standard test dimensions)
        decimal length = 100m;
        decimal beam = 20m;
        decimal designDraft = 10m;

        var (stations, waterlines, offsets) = HullTestData.GenerateRectangularBarge(
            length, beam, designDraft, numStations: 21, numWaterlines: 21);

        _vessel = new Vessel
        {
            Id = Guid.NewGuid(),
            Name = "Rectangular Barge Validation",
            Lpp = length,
            Beam = beam,
            DesignDraft = designDraft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Vessels.Add(_vessel);

        foreach (var station in stations)
        {
            _context.Stations.Add(new Station
            {
                Id = Guid.NewGuid(),
                VesselId = _vessel.Id,
                StationIndex = station.Index,
                X = station.X
            });
        }

        foreach (var waterline in waterlines)
        {
            _context.Waterlines.Add(new Waterline
            {
                Id = Guid.NewGuid(),
                VesselId = _vessel.Id,
                WaterlineIndex = waterline.Index,
                Z = waterline.Z
            });
        }

        foreach (var offset in offsets)
        {
            _context.Offsets.Add(new Offset
            {
                Id = Guid.NewGuid(),
                VesselId = _vessel.Id,
                StationIndex = offset.StationIndex,
                WaterlineIndex = offset.WaterlineIndex,
                HalfBreadthY = offset.HalfBreadthY
            });
        }

        // Create loadcase: density 1025 kg/m³ (seawater)
        _loadcase = new Loadcase
        {
            Id = Guid.NewGuid(),
            VesselId = _vessel.Id,
            Name = "Design Condition",
            Rho = 1025m,
            KG = designDraft * 0.5m, // KG at 50% of draft
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Loadcases.Add(_loadcase);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task BargeValidation_VolumeAgainstReference_WithinOnePercent()
    {
        // Arrange - Use reference data from constants
        var referenceData = BargeValidationConstants.HydroTable;
        var testDrafts = new[] { 0.5m, 1.0m, 2.0m, 3.0m, 4.0m, 5.0m };

        foreach (var draft in testDrafts)
        {
            var reference = referenceData.FirstOrDefault(r => r.Draft_T_m == draft);
            if (reference == null) continue;

            // Act
            var computed = await _hydroCalculator.ComputeAtDraftAsync(_vessel.Id, _loadcase.Id, draft);

            // Assert - Volume within 1% (accounting for numerical integration error)
            var volumeError = Math.Abs((computed.DispVolume ?? 0) - reference.Volume_disp_m3) / reference.Volume_disp_m3;
            volumeError.Should().BeLessThan(0.01m,
                $"Volume error at draft {draft}m should be < 1%. Expected: {reference.Volume_disp_m3:F2}, Got: {computed.DispVolume:F2}, Error: {volumeError:P2}");
        }
    }

    [Fact]
    public async Task BargeValidation_KBAgainstReference_WithinOnePercent()
    {
        // Arrange
        var referenceData = BargeValidationConstants.HydroTable;
        var testDrafts = new[] { 0.5m, 1.0m, 2.0m, 3.0m, 4.0m, 5.0m };

        foreach (var draft in testDrafts)
        {
            var reference = referenceData.FirstOrDefault(r => r.Draft_T_m == draft);
            if (reference == null) continue;

            // Act
            var computed = await _hydroCalculator.ComputeAtDraftAsync(_vessel.Id, _loadcase.Id, draft);

            // Assert - KB within 1% (analytical: KB = T/2 for rectangular barge)
            var kbError = Math.Abs((computed.KBz ?? 0) - reference.KB_m) / reference.KB_m;
            kbError.Should().BeLessThan(0.01m,
                $"KB error at draft {draft}m should be < 1%. Expected: {reference.KB_m:F3}, Got: {computed.KBz:F3}, Error: {kbError:P2}");
        }
    }

    [Fact]
    public async Task BargeValidation_BMtAgainstReference_WithinTwoPercent()
    {
        // Arrange
        var referenceData = BargeValidationConstants.HydroTable;
        var testDrafts = new[] { 0.5m, 1.0m, 2.0m, 3.0m, 4.0m, 5.0m };

        foreach (var draft in testDrafts)
        {
            var reference = referenceData.FirstOrDefault(r => r.Draft_T_m == draft);
            if (reference == null) continue;

            // Act
            var computed = await _hydroCalculator.ComputeAtDraftAsync(_vessel.Id, _loadcase.Id, draft);

            // Assert - BMt within 2% (relaxed tolerance for second moment integration)
            var bmtError = Math.Abs((computed.BMt ?? 0) - reference.BM_T_m) / reference.BM_T_m;
            bmtError.Should().BeLessThan(0.02m,
                $"BMt error at draft {draft}m should be < 2%. Expected: {reference.BM_T_m:F3}, Got: {computed.BMt:F3}, Error: {bmtError:P2}");
        }
    }
}
