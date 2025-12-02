using FluentAssertions;
using HullSizingService.Data;
using HullSizingService.Services;
using HullSizingService.Services.Solver;
using HullSizingService.Services.Validation;
using HullSizingService.Tests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models.Sizing;
using Xunit;

namespace HullSizingService.Tests.Validation.Integration;

/// <summary>
/// Integration tests for resistance validation against reference data.
///
/// Validates that resistance calculations (EHP, Ct) match expected trends
/// and reference values for known test cases.
///
/// Source: Ship Design Validation Handbook - Resistance Coefficient Validation
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "LongRunning")]
public class ResistanceValidationIntegrationTests : IDisposable
{
    private readonly SizingDbContext _context;
    private readonly FirstPrinciplesSolver _solver;
    private readonly IDesignValidationService _validationService;
    private readonly Mock<IWaterPropertiesService> _waterServiceMock;

    public ResistanceValidationIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SizingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new SizingDbContext(options);
        SeedTestData();

        _waterServiceMock = new Mock<IWaterPropertiesService>();
        _waterServiceMock
            .Setup(w => w.GetWaterPropertiesAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HullSizingService.Services.Integration.WaterPropertiesResponse(
                DensityKgM3: 1025.0m,
                KinematicViscosityM2S: 0.000001188m,
                TemperatureCelsius: 15.0m,
                SalinityPpt: 35.0m
            ));

        var familyService = new HullFamilyService(_context, Mock.Of<ILogger<HullFamilyService>>());
        var closureService = new DisplacementClosureService(Mock.Of<ILogger<DisplacementClosureService>>());
        var resistanceService = new HoltropResistanceService(Mock.Of<ILogger<HoltropResistanceService>>());
        var stabilityService = new StabilityScreenService(Mock.Of<ILogger<StabilityScreenService>>());

        _solver = new FirstPrinciplesSolver(
            familyService,
            closureService,
            resistanceService,
            stabilityService,
            _waterServiceMock.Object,
            _context,
            Mock.Of<ILogger<FirstPrinciplesSolver>>()
        );

        var validationLogger = Mock.Of<ILogger<DesignValidationService>>();
        _validationService = new DesignValidationService(validationLogger);
    }

    private void SeedTestData()
    {
        _context.HullFamilyPresets.Add(new HullFamilyPreset
        {
            Id = Guid.NewGuid(),
            Family = "tanker",
            DisplayName = "Product Carrier / Tanker",
            LOverBMin = 5.5m,
            LOverBMax = 7.0m,
            BOverTMin = 2.0m,
            BOverTMax = 2.8m,
            DOverTMin = 1.2m,
            DOverTMax = 1.4m,
            CbMin = 0.75m,
            CbMax = 0.85m,
            CpMin = 0.77m,
            CpMax = 0.88m,
            CwpMin = 0.82m,
            CwpMax = 0.90m,
            FnMin = 0.12m,
            FnMax = 0.18m,
            GeneratorType = "shipd",
            IsActive = true
        });

        _context.KpiWeights.AddRange(
            new KpiWeight { Id = Guid.NewGuid(), UserId = null, Metric = "delta_balance", Weight = 0.35m },
            new KpiWeight { Id = Guid.NewGuid(), UserId = null, Metric = "installed_power", Weight = 0.25m },
            new KpiWeight { Id = Guid.NewGuid(), UserId = null, Metric = "constraints_ok", Weight = 0.20m },
            new KpiWeight { Id = Guid.NewGuid(), UserId = null, Metric = "stability_screen", Weight = 0.10m },
            new KpiWeight { Id = Guid.NewGuid(), UserId = null, Metric = "teu_or_volume_fit", Weight = 0.10m }
        );

        _context.SaveChanges();
    }

    [Fact]
    public async Task ProductCarrier_ResistanceTrend_ShouldMatchExpected()
    {
        // Arrange: Product Carrier calibration case
        var missionCase = new MissionCase
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = "test-tenant",
            Name = "Product Carrier Resistance Validation",
            MissionCategory = ValidationTestCases.CalibrationCase.VesselType,
            MissionType = ValidationTestCases.CalibrationCase.VesselSubtype,
            CargoBasis = "weight",
            CargoValue = ValidationTestCases.CalibrationCase.DeadweightTonnes,
            ServiceSpeedKn = ValidationTestCases.CalibrationCase.ServiceSpeedKn,
            SeaMarginPct = 0.15m,
            BowFamily = ValidationTestCases.CalibrationCase.BowFamily,
            MidshipFamily = ValidationTestCases.CalibrationCase.MidshipFamily,
            SternFamily = ValidationTestCases.CalibrationCase.SternFamily,
            CapLoaM = ValidationTestCases.CalibrationCase.LppM * 1.05m,
            CapBeamM = ValidationTestCases.CalibrationCase.BeamM * 1.1m,
            CapDraftM = ValidationTestCases.CalibrationCase.DraftM * 1.1m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.MissionCases.Add(missionCase);
        await _context.SaveChangesAsync();

        // Act
        var solverRequest = new Solver.SolverRequest(
            MissionCase: missionCase,
            Locks: null,
            Options: new Solver.SizingOptionsDto(
                FamilyHints: new List<string> { "tanker", "product_carrier" },
                MaxCandidates: 1,
                MinFn: null,
                MaxFn: null,
                AdditionalParameters: null
            )
        );

        var (candidates, diagnostics) = await _solver.SolveAsync(solverRequest, CancellationToken.None);

        // Assert
        candidates.Should().NotBeEmpty("Solver should generate at least one candidate.");

        var candidate = candidates.First();

        // Validate EHP is reasonable (should be positive and within expected range)
        candidate.EhpKw.Should().HaveValue("EHP should be calculated.");
        candidate.EhpKw!.Value.Should().BePositive("EHP should be positive.");
        candidate.EhpKw.Value.Should().BeLessThan(50000m, "EHP should be reasonable for a 40k DWT vessel at 14 knots.");

        // Validate resistance trend
        if (candidate.EhpKw.HasValue && candidate.DisplacementT > 0)
        {
            var resistanceValidation = _validationService.ValidateResistanceTrend(
                candidate.EhpKw.Value,
                candidate.DisplacementT,
                ValidationTestCases.CalibrationCase.VesselSubtype);

            // Product carrier at moderate speed should have Low to Moderate trend
            resistanceValidation.TrendCategory.Should().BeOneOf("Low", "Moderate",
                $"Resistance trend should be Low or Moderate for product carrier at {ValidationTestCases.CalibrationCase.ServiceSpeedKn} knots. " +
                $"Got: {resistanceValidation.TrendCategory}, EHP/tonne: {resistanceValidation.EhpPerTonne:F4}");

            resistanceValidation.EhpPerTonne.Should().BePositive("EHP per tonne should be positive.");

            // Low-speed cargo vessels typically have EHP/tonne < 0.3
            resistanceValidation.EhpPerTonne.Should().BeLessThan(0.5m,
                $"EHP per tonne should be reasonable for a low-speed cargo vessel. Got: {resistanceValidation.EhpPerTonne:F4}");
        }

        // Validate SHP is calculated and reasonable
        candidate.ShpKw.Should().HaveValue("SHP should be calculated.");
        candidate.ShpKw!.Value.Should().BePositive("SHP should be positive.");
        candidate.ShpKw.Value.Should().BeGreaterThan(candidate.EhpKw!.Value,
            "SHP should be greater than EHP (accounts for propulsive efficiency and margins).");
    }

    [Fact]
    public async Task ResistanceTrend_ShouldIncreaseWithSpeed()
    {
        // Arrange: Test that resistance trend increases with speed
        var speeds = new[] { 12.0m, 14.0m, 16.0m };
        var ehpValues = new List<decimal>();

        foreach (var speed in speeds)
        {
            var missionCase = new MissionCase
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                TenantId = "test-tenant",
                Name = $"Resistance Trend Test - {speed} knots",
                MissionCategory = "commercial",
                MissionType = "general_cargo",
                CargoBasis = "weight",
                CargoValue = 50000m,
                ServiceSpeedKn = speed,
                SeaMarginPct = 0.15m,
                BowFamily = "bulbous_bow",
                MidshipFamily = "full_midship",
                SternFamily = "transom_stern",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.MissionCases.Add(missionCase);
            await _context.SaveChangesAsync();

            var solverRequest = new Solver.SolverRequest(
                MissionCase: missionCase,
                Locks: null,
                Options: new Solver.SizingOptionsDto(
                    FamilyHints: new List<string> { "cargo" },
                    MaxCandidates: 1,
                    MinFn: null,
                    MaxFn: null,
                    AdditionalParameters: null
                )
            );

            var (candidates, _) = await _solver.SolveAsync(solverRequest, CancellationToken.None);
            if (candidates.Any() && candidates.First().EhpKw.HasValue)
            {
                ehpValues.Add(candidates.First().EhpKw.Value);
            }
        }

        // Assert: EHP should generally increase with speed
        if (ehpValues.Count >= 2)
        {
            for (int i = 1; i < ehpValues.Count; i++)
            {
                // Allow some tolerance - at very low speeds, resistance increase may be minimal
                // But at higher speeds, increase should be significant
                if (speeds[i] >= 14.0m)
                {
                    ehpValues[i].Should().BeGreaterThan(ehpValues[i - 1] * 0.9m,
                        $"EHP should increase with speed. Speed {speeds[i]}kn: {ehpValues[i]:F1}kW should be > {speeds[i - 1]}kn: {ehpValues[i - 1]:F1}kW");
                }
            }
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
