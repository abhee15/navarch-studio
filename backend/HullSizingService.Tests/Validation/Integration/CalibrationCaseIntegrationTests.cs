using FluentAssertions;
using HullSizingService.Data;
using HullSizingService.Services;
using HullSizingService.Services.Solver;
using HullSizingService.Services.Validation;
using HullSizingService.Tests.Helpers;
using HullSizingService.Tests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models.Sizing;
using Xunit;

namespace HullSizingService.Tests.Validation.Integration;

/// <summary>
/// Integration test for Calibration Case: 40,000 DWT Product Carrier
///
/// This is the "Gold Standard" calibration case that validates the solver's accuracy
/// before running general test cases. Full end-to-end pipeline test.
///
/// Source: Ship Design Validation Handbook - Calibration Case
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "LongRunning")]
public class CalibrationCaseIntegrationTests : IDisposable
{
    private readonly SizingDbContext _context;
    private readonly FirstPrinciplesSolver _solver;
    private readonly IDesignValidationService _validationService;
    private readonly Mock<IWaterPropertiesService> _waterServiceMock;

    public CalibrationCaseIntegrationTests()
    {
        // Create in-memory database
        var options = new DbContextOptionsBuilder<SizingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new SizingDbContext(options);

        // Seed test data
        SeedTestData();

        // Create mocks
        _waterServiceMock = new Mock<IWaterPropertiesService>();
        _waterServiceMock
            .Setup(w => w.GetWaterPropertiesAsync(It.IsAny<decimal>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HullSizingService.Services.Integration.WaterPropertiesResponse(
                DensityKgM3: 1025.0m,
                KinematicViscosityM2S: 0.000001188m,
                TemperatureCelsius: 15.0m,
                SalinityPpt: 35.0m
            ));

        // Create service instances
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
        // Add hull family preset for product carrier (full-form)
        _context.HullFamilyPresets.Add(new HullFamilyPreset
        {
            Id = Guid.NewGuid(),
            Family = "tanker", // Product carriers use tanker-like hull forms
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
            GeneratorType = "wigley",
            IsActive = true
        });

        // Add KPI weights
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
    [Trait("Category", "Integration")]
    [Trait("Category", "LongRunning")]
    public async Task CalibrationCase_ProductCarrier_ValidatesAgainstExpectedRanges()
    {
        // Arrange: Create mission case matching calibration case
        var mission = new MissionCase
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = "test-tenant",
            Name = "Calibration Case: 40,000 DWT Product Carrier",
            MissionType = "product_carrier",
            MissionCategory = "commercial",
            CargoBasis = "weight",
            CargoValue = 40000m, // 40,000 DWT from prefinal_1
            ServiceSpeedKn = 14.0m, // 14 knots service speed from prefinal_1
            SeaMarginPct = 15.0m,
            CapLoaM = 185.0m, // LBP constraint from prefinal_1
            CapBeamM = 28.0m, // Breadth from prefinal_1
            CapDraftM = 12.87m, // Draft from prefinal_1
            BowFamily = "bulbous_bow",
            MidshipFamily = "full_midship",
            SternFamily = "transom_stern",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new SolverRequest(
            MissionCase: mission,
            Locks: null,
            Options: new SizingOptionsDto(null, 1, null, null, null) // Single candidate for calibration
        );

        // Act: Run solver
        var (candidates, diagnostics) = await _solver.SolveAsync(request, default);

        // Assert: Should generate at least one candidate
        candidates.Should().NotBeEmpty("solver should generate at least one candidate for calibration case");

        var candidate = candidates.First();

        // Validate principal dimensions (within ±5% tolerance)
        // Validate principal dimensions match prefinal_1 document
        candidate.LppM.Should().BeApproximately(
            185.0m, // Finalized LBP from prefinal_1
            185.0m * 0.05m,
            "Lpp should be approximately 185m (±5%)");

        candidate.BeamM.Should().BeApproximately(
            28.0m, // Finalized Breadth from prefinal_1
            28.0m * 0.05m,
            "Beam should be approximately 28m (±5%)");

        candidate.DraftM.Should().BeApproximately(
            12.87m, // Moulded draft from prefinal_1 (Depth 16.40m - Freeboard 3.55m = 12.85m ≈ 12.87m)
            12.87m * 0.05m,
            "Draft should be approximately 12.87m (±5%)");

        // Validate Block Coefficient (prefinal_1 shows Cb range 0.76-0.80, calibration uses upper range 0.79-0.80)
        candidate.Cb.Should().BeInRange(
            0.792m - 0.005m,
            0.80m + 0.005m,
            "Cb should be in range [0.792, 0.80] ± 0.02");

        // Validate Midship Coefficient (from prefinal_1: finalized CM = 0.99)
        candidate.Cm.Should().BeApproximately(
            0.99m,
            0.01m, // ±0.01 tolerance
            "Cm should be approximately 0.99 (±0.01) for rectangular midship section");

        // Validate Waterplane Coefficient (from prefinal_1: finalized CW = 0.87)
        candidate.Cwp.Should().BeApproximately(
            0.87m,
            0.02m, // ±0.02 tolerance
            "Cwp should be approximately 0.87 (±0.02)");

        // Validate Depth (from prefinal_1: finalized DEPTH = 16.40 m)
        candidate.DepthM.Should().BeApproximately(
            16.40m,
            16.40m * 0.10m, // ±10% tolerance (depth estimation has more variance)
            "Depth should be approximately 16.40m (±10%)");

        // Validate Freeboard (Depth - Draft should ≈ 3.55m from prefinal_1)
        var freeboard = candidate.DepthM - candidate.DraftM;
        freeboard.Should().BeApproximately(
            3.55m,
            0.50m, // ±0.5m tolerance
            "Freeboard (D-T) should be approximately 3.55m");

        // Validate against Alexander Limit
        var alexanderResult = _validationService.ValidateAlexanderLimit(candidate.Fn, candidate.Cb);
        alexanderResult.ViolatesLimit.Should().BeFalse(
            $"Design should not violate Alexander Limit. Fn={candidate.Fn:F3}, Cb={candidate.Cb:F3}");

        // Convert SolverCandidate to CandidateDesign for validation
        // (ValidateAgainstExpectedRanges expects CandidateDesign entity, not SolverCandidate)
        var candidateDesign = TestHelpers.MapSolverCandidateToCandidateDesign(candidate, Guid.NewGuid());

        // Validate against expected ranges
        var validationResult = _validationService.ValidateAgainstExpectedRanges(
            candidateDesign,
            "product_carrier",
            new ValidationToleranceConfig
            {
                CbTolerance = 0.005m, // ±0.5% tolerance for calibration case
                DimensionTolerancePercent = 5.0m
            });

        validationResult.IsValid.Should().BeTrue(
            "Calibration case should pass validation against expected ranges");

        // Log validation warnings if any (non-blocking)
        if (validationResult.Warnings.Any())
        {
            // Warnings are acceptable, but logged for review
            var warningMessages = string.Join("; ", validationResult.Warnings.Select(w => $"{w.Field}: {w.Message}"));
            System.Diagnostics.Debug.WriteLine($"Validation warnings: {warningMessages}");
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
