using System;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using HullSizingService.Data;
using HullSizingService.Services;
using HullSizingService.Services.Solver;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models.Sizing;
using Xunit;

namespace HullSizingService.Tests.Validation.Integration;

/// <summary>
/// Integration tests for geometry generation in candidate designs.
/// Validates that geometryJson is properly generated and formatted for frontend visualization.
///
/// These tests ensure the backend generates geometry data that can be correctly parsed
/// and rendered by frontend visualization components.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "LongRunning")]
public class GeometryGenerationIntegrationTests : IDisposable
{
    private readonly SizingDbContext _context;
    private readonly FirstPrinciplesSolver _solver;
    private readonly Mock<IWaterPropertiesService> _waterServiceMock;

    public GeometryGenerationIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SizingDbContext>()
            .UseInMemoryDatabase(databaseName: $"GeometryGenerationTests_{Guid.NewGuid()}")
            .Options;

        _context = new SizingDbContext(options);

        // Mock water properties service
        _waterServiceMock = new Mock<IWaterPropertiesService>();
        _waterServiceMock
            .Setup(x => x.GetWaterPropertiesAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync(new HullSizingService.Services.Integration.WaterPropertiesResponse(
                DensityKgM3: 1025.0m,
                KinematicViscosityM2S: 0.000001188m,
                TemperatureCelsius: 15.0m,
                SalinityPpt: 35.0m
            ));

        // Setup solver dependencies
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var familyService = new HullFamilyService(_context, loggerFactory.CreateLogger<HullFamilyService>());
        var closureService = new DisplacementClosureService(loggerFactory.CreateLogger<DisplacementClosureService>());
        var resistanceService = new HoltropResistanceService(loggerFactory.CreateLogger<HoltropResistanceService>());
        var stabilityService = new StabilityScreenService(loggerFactory.CreateLogger<StabilityScreenService>());

        _solver = new FirstPrinciplesSolver(
            familyService,
            closureService,
            resistanceService,
            stabilityService,
            _waterServiceMock.Object,
            _context,
            loggerFactory.CreateLogger<FirstPrinciplesSolver>());

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Seed hull families for testing
        if (!_context.HullFamilyPresets.Any())
        {
            _context.HullFamilyPresets.AddRange(
                new HullFamilyPreset
                {
                    Id = Guid.NewGuid(),
                    Family = "bulbous_bow",
                    MissionType = "commercial",
                    CbMin = 0.65m,
                    CbMax = 0.85m,
                    FnMin = 0.15m,
                    FnMax = 0.30m,
                    IsActive = true
                },
                new HullFamilyPreset
                {
                    Id = Guid.NewGuid(),
                    Family = "full_midship",
                    MissionType = "commercial",
                    CbMin = 0.75m,
                    CbMax = 0.90m,
                    FnMin = 0.12m,
                    FnMax = 0.20m,
                    IsActive = true
                },
                new HullFamilyPreset
                {
                    Id = Guid.NewGuid(),
                    Family = "transom_stern",
                    MissionType = "commercial",
                    CbMin = 0.60m,
                    CbMax = 0.85m,
                    FnMin = 0.15m,
                    FnMax = 0.30m,
                    IsActive = true
                }
            );
            _context.SaveChanges();
        }
    }

    [Fact]
    public async Task GeometryJson_ShouldBeValidJson()
    {
        // Arrange
        var mission = CreateTestMissionCase();
        var request = new SolverRequest(
            MissionCase: mission,
            Locks: null,
            Options: new SizingOptionsDto(null, 1, null, null, null)
        );

        // Act
        var (candidates, diagnostics) = await _solver.SolveAsync(request, default);

        // Assert
        candidates.Should().NotBeEmpty("solver should generate at least one candidate");
        var candidate = candidates.First();

        candidate.GeometryJson.Should().NotBeNullOrEmpty("candidate should have geometry JSON");

        // Validate JSON is parseable
        Action parseJson = () => JsonSerializer.Deserialize<object>(candidate.GeometryJson!);
        parseJson.Should().NotThrow("geometry JSON should be valid JSON");
    }

    [Fact]
    public async Task GeometryJson_ShouldHaveRequiredStructure()
    {
        // Arrange
        var mission = CreateTestMissionCase();
        var request = new SolverRequest(
            MissionCase: mission,
            Locks: null,
            Options: new SizingOptionsDto(null, 1, null, null, null)
        );

        // Act
        var (candidates, diagnostics) = await _solver.SolveAsync(request, default);

        // Assert
        candidates.Should().NotBeEmpty();
        var candidate = candidates.First();
        candidate.GeometryJson.Should().NotBeNullOrEmpty();

        var geometry = JsonSerializer.Deserialize<JsonElement>(candidate.GeometryJson!);

        // Check for OffsetsGrid format structure
        var hasOffsetsGridFormat =
            geometry.TryGetProperty("stations", out _) ||
            geometry.TryGetProperty("Stations", out _);

        var hasShipDFormat =
            geometry.TryGetProperty("stations", out var stationsProp) &&
            stationsProp.ValueKind == JsonValueKind.Array &&
            stationsProp.GetArrayLength() > 0 &&
            stationsProp[0].ValueKind == JsonValueKind.Object &&
            stationsProp[0].TryGetProperty("position", out _);

        (hasOffsetsGridFormat || hasShipDFormat).Should().BeTrue(
            "geometry JSON should have either OffsetsGrid or ShipD format structure");
    }

    [Fact]
    public async Task GeometryJson_ShouldHaveValidNumericValues()
    {
        // Arrange
        var mission = CreateTestMissionCase();
        var request = new SolverRequest(
            MissionCase: mission,
            Locks: null,
            Options: new SizingOptionsDto(null, 1, null, null, null)
        );

        // Act
        var (candidates, diagnostics) = await _solver.SolveAsync(request, default);

        // Assert
        candidates.Should().NotBeEmpty();
        var candidate = candidates.First();
        candidate.GeometryJson.Should().NotBeNullOrEmpty();

        var geometry = JsonSerializer.Deserialize<JsonElement>(candidate.GeometryJson!);

        // Check for OffsetsGrid format
        if (geometry.TryGetProperty("stations", out var stationsProp) ||
            geometry.TryGetProperty("Stations", out stationsProp))
        {
            if (stationsProp.ValueKind == JsonValueKind.Array)
            {
                // OffsetsGrid format
                var stations = stationsProp.GetArrayLength() > 0
                    ? stationsProp.EnumerateArray().Select(s => s.GetDecimal()).ToArray()
                    : Array.Empty<decimal>();

                stations.Should().NotBeEmpty("stations array should not be empty");
                stations.All(s => s >= 0).Should().BeTrue("all station positions should be non-negative");
                stations.All(s => decimal.IsNormal(s) || s == 0).Should().BeTrue(
                    "all station positions should be finite numbers");

                // Check waterlines
                if (geometry.TryGetProperty("waterlines", out var waterlinesProp) ||
                    geometry.TryGetProperty("Waterlines", out waterlinesProp))
                {
                    if (waterlinesProp.ValueKind == JsonValueKind.Array)
                    {
                        var waterlines = waterlinesProp.EnumerateArray().Select(w => w.GetDecimal()).ToArray();
                        waterlines.Should().NotBeEmpty("waterlines array should not be empty");
                        waterlines.All(w => decimal.IsNormal(w) || w == 0).Should().BeTrue(
                            "all waterline heights should be finite numbers");
                    }
                }
            }
        }
    }

    [Fact]
    public async Task GeometryGenerationStatus_ShouldReflectGenerationSuccess()
    {
        // Arrange
        var mission = CreateTestMissionCase();
        var request = new SolverRequest(
            MissionCase: mission,
            Locks: null,
            Options: new SizingOptionsDto(null, 1, null, null, null)
        );

        // Act
        var (candidates, diagnostics) = await _solver.SolveAsync(request, default);

        // Assert
        candidates.Should().NotBeEmpty();
        var candidate = candidates.First();

        if (candidate.GeometryGenerationStatus == GeometryGenerationStatus.Success)
        {
            candidate.GeometryJson.Should().NotBeNullOrEmpty(
                "successful geometry generation should produce geometry JSON");
        }
        else
        {
            // If generation failed, there should be an error message
            candidate.GeometryGenerationError.Should().NotBeNullOrEmpty(
                "failed geometry generation should have an error message");
        }
    }

    private MissionCase CreateTestMissionCase()
    {
        return new MissionCase
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = "test-tenant",
            Name = "Test Mission",
            MissionType = "product_carrier",
            MissionCategory = "commercial",
            CargoBasis = "weight",
            CargoValue = 40000m,
            ServiceSpeedKn = 14.0m,
            SeaMarginPct = 15.0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
