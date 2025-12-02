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
/// Integration test for Test Case A: Bulk Carrier/VLCC (250,000 tonnes, 15 knots)
///
/// Validates the solver across low-speed, high-block-coefficient regime.
/// Tests full-form hull generation with maximum cargo volume optimization.
///
/// Source: Ship Design Validation Handbook - Standard Test Cases (TC-A)
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "LongRunning")]
public class StandardTestCaseAIntegrationTests : IDisposable
{
    private readonly SizingDbContext _context;
    private readonly FirstPrinciplesSolver _solver;
    private readonly IDesignValidationService _validationService;
    private readonly Mock<IWaterPropertiesService> _waterServiceMock;

    public StandardTestCaseAIntegrationTests()
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
        // Add hull family preset for bulk carrier (full-form, high Cb)
        _context.HullFamilyPresets.Add(new HullFamilyPreset
        {
            Id = Guid.NewGuid(),
            Family = "bulk",
            DisplayName = "Bulk Carrier",
            LOverBMin = 5.5m,
            LOverBMax = 6.5m,
            BOverTMin = 2.2m,
            BOverTMax = 2.9m,
            DOverTMin = 1.25m,
            DOverTMax = 1.45m,
            CbMin = 0.70m,
            CbMax = 0.80m,
            CpMin = 0.73m,
            CpMax = 0.83m,
            CwpMin = 0.80m,
            CwpMax = 0.88m,
            FnMin = 0.14m,
            FnMax = 0.20m,
            GeneratorType = "shipd",
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
    public async Task TestCaseA_BulkCarrier_ShouldMeetExpectedParameters()
    {
        // Arrange
        var missionCase = new MissionCase
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = "test-tenant",
            Name = "TC-A Bulk Carrier Test",
            MissionCategory = ValidationTestCases.TestCaseA.VesselType,
            MissionType = ValidationTestCases.TestCaseA.VesselSubtype,
            CargoBasis = "weight",
            CargoValue = ValidationTestCases.TestCaseA.CargoTonnes,
            ServiceSpeedKn = ValidationTestCases.TestCaseA.ServiceSpeedKn,
            SeaMarginPct = 0.20m, // 20% sea margin for large bulk carrier
            BowFamily = "blunt_bow", // Bluff bow for volume optimization
            MidshipFamily = "full_midship", // Maximum volume
            SternFamily = "transom_stern",
            CapLoaM = ValidationTestCases.TestCaseA.ExpectedLppMax * 1.05m,
            CapBeamM = ValidationTestCases.TestCaseA.ExpectedBeamMax * 1.1m,
            CapDraftM = null, // Let solver determine
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.MissionCases.Add(missionCase);
        await _context.SaveChangesAsync();

        // Act - Generate candidate using solver
        var solverRequest = new Solver.SolverRequest(
            MissionCase: missionCase,
            Locks: null,
            Options: new Solver.SizingOptionsDto(
                FamilyHints: new List<string> { "bulk" },
                MaxCandidates: 1,
                MinFn: null,
                MaxFn: null,
                AdditionalParameters: null
            )
        );

        var (candidates, diagnostics) = await _solver.SolveAsync(solverRequest, CancellationToken.None);

        // Assert
        candidates.Should().NotBeEmpty("A candidate design should be generated for TC-A.");

        var candidate = candidates.First();

        // Convert SolverCandidate to CandidateDesign for validation
        var candidateDesign = ConvertToCandidateDesign(candidate);

        var validationResult = _validationService.ValidateAgainstExpectedRanges(
            candidateDesign,
            ValidationTestCases.TestCaseA.VesselSubtype);

        // Validate Block Coefficient (high Cb expected for bulk carrier)
        candidate.Cb.Should().BeInRange(
            ValidationTestCases.TestCaseA.ExpectedCbMin,
            ValidationTestCases.TestCaseA.ExpectedCbMax,
            $"Block Coefficient should be in high range {ValidationTestCases.TestCaseA.ExpectedCbMin}-{ValidationTestCases.TestCaseA.ExpectedCbMax} for bulk carrier.");

        // Validate Froude Number (low speed regime)
        candidate.Fn.Should().BeInRange(
            ValidationTestCases.TestCaseA.ExpectedFnMin,
            ValidationTestCases.TestCaseA.ExpectedFnMax,
            $"Froude Number should be in range {ValidationTestCases.TestCaseA.ExpectedFnMin}-{ValidationTestCases.TestCaseA.ExpectedFnMax} for low-speed bulk carrier.");

        // Validate principal dimensions
        candidate.LppM.Should().BeInRange(
            ValidationTestCases.TestCaseA.ExpectedLppMin,
            ValidationTestCases.TestCaseA.ExpectedLppMax,
            $"Length should be in range {ValidationTestCases.TestCaseA.ExpectedLppMin}-{ValidationTestCases.TestCaseA.ExpectedLppMax}m for large bulk carrier.");

        candidate.BM.Should().BeInRange(
            ValidationTestCases.TestCaseA.ExpectedBeamMin,
            ValidationTestCases.TestCaseA.ExpectedBeamMax,
            $"Beam should be in range {ValidationTestCases.TestCaseA.ExpectedBeamMin}-{ValidationTestCases.TestCaseA.ExpectedBeamMax}m.");

        // Validate Alexander Limit (should be well below limit at low speed)
        var alexanderValidation = _validationService.ValidateAlexanderLimit(candidate.Fn, candidate.Cb);
        alexanderValidation.ViolatesLimit.Should().BeFalse(
            $"Design should not violate Alexander Limit at low speed (Fn={candidate.Fn:F3}, Cb={candidate.Cb:F3}).");

        // Validate resistance trend (should be Low - primarily frictional drag)
        if (candidate.EhpKw.HasValue && candidate.DisplacementT > 0)
        {
            var resistanceValidation = _validationService.ValidateResistanceTrend(
                candidate.EhpKw.Value,
                candidate.DisplacementT,
                ValidationTestCases.TestCaseA.VesselSubtype);

            resistanceValidation.TrendCategory.Should().BeEquivalentTo(ValidationTestCases.TestCaseA.ExpectedEhpTrend,
                $"Resistance trend should be '{ValidationTestCases.TestCaseA.ExpectedEhpTrend}' for low-speed bulk carrier.");
        }

        // Validate midship coefficient (should be ≈0.99 for full-form)
        if (candidate.Cm.HasValue)
        {
            candidate.Cm.Value.Should().BeApproximately(0.99m, 0.01m,
                "Midship coefficient should be approximately 0.99 for full-form bulk carrier.");
        }

        // Validate form coefficient relationships
        if (candidate.Cm.HasValue && candidate.Cm.Value > 0)
        {
            var expectedCp = candidate.Cb / candidate.Cm.Value;
            candidate.Cp.Should().BeApproximately(expectedCp, 0.01m,
                "Prismatic coefficient should equal Cb/Cm.");
        }
    }

    private static CandidateDesign ConvertToCandidateDesign(HullSizingService.Services.Solver.SolverCandidate candidate)
    {
        return new CandidateDesign
        {
            Id = Guid.NewGuid(),
            SizingRunId = Guid.Empty, // Not persisted
            HullFamily = candidate.HullFamily,
            LppM = candidate.LppM,
            LwlM = candidate.LwlM,
            LoaM = candidate.LoaM,
            BM = candidate.BeamM,
            TM = candidate.DraftM,
            DM = candidate.DepthM,
            Cb = candidate.Cb,
            Cp = candidate.Cp,
            Cwp = candidate.Cwp,
            Cm = candidate.Cm,
            DisplacementT = candidate.DisplacementT,
            Fn = candidate.Fn,
            LwlOverLambda = candidate.LwlOverLambda,
            KbM = candidate.KbM,
            LcbPctLpp = candidate.LcbPctLpp,
            GmEstM = candidate.GmEstM,
            EhpKw = candidate.EhpKw,
            ShpKw = candidate.ShpKw,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
