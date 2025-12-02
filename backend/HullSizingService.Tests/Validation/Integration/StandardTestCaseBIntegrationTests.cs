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
/// Integration test for Test Case B: General Cargo (50,000 tonnes, 20 knots)
///
/// Validates the solver across medium-speed, medium-block-coefficient regime.
/// Tests balanced design between speed and cargo capacity.
///
/// Source: Ship Design Validation Handbook - Standard Test Cases (TC-B)
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "LongRunning")]
public class StandardTestCaseBIntegrationTests : IDisposable
{
    private readonly SizingDbContext _context;
    private readonly FirstPrinciplesSolver _solver;
    private readonly IDesignValidationService _validationService;
    private readonly Mock<IWaterPropertiesService> _waterServiceMock;

    public StandardTestCaseBIntegrationTests()
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
            Family = "cargo",
            DisplayName = "General Cargo",
            LOverBMin = 6.0m,
            LOverBMax = 7.5m,
            BOverTMin = 2.3m,
            BOverTMax = 3.0m,
            DOverTMin = 1.3m,
            DOverTMax = 1.5m,
            CbMin = 0.55m,
            CbMax = 0.70m,
            CpMin = 0.58m,
            CpMax = 0.75m,
            CwpMin = 0.75m,
            CwpMax = 0.85m,
            FnMin = 0.20m,
            FnMax = 0.28m,
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
    public async Task TestCaseB_GeneralCargo_ShouldMeetExpectedParameters()
    {
        // Arrange
        var missionCase = new MissionCase
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = "test-tenant",
            Name = "TC-B General Cargo Test",
            MissionCategory = ValidationTestCases.TestCaseB.VesselType,
            MissionType = ValidationTestCases.TestCaseB.VesselSubtype,
            CargoBasis = "weight",
            CargoValue = ValidationTestCases.TestCaseB.CargoTonnes,
            ServiceSpeedKn = ValidationTestCases.TestCaseB.ServiceSpeedKn,
            SeaMarginPct = 0.15m,
            BowFamily = ValidationTestCases.TestCaseB.BowFamily,
            MidshipFamily = ValidationTestCases.TestCaseB.MidshipFamily,
            SternFamily = ValidationTestCases.TestCaseB.SternFamily,
            CapLoaM = ValidationTestCases.TestCaseB.ExpectedLppMax * 1.05m,
            CapBeamM = ValidationTestCases.TestCaseB.ExpectedBeamMax * 1.1m,
            CapDraftM = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.MissionCases.Add(missionCase);
        await _context.SaveChangesAsync();

        // Act
        var solverRequest = new SolverRequest(
            MissionCase: missionCase,
            Locks: null,
            Options: new SizingOptionsDto(
                FamilyHints: new List<string> { "cargo", "container" },
                MaxCandidates: 1,
                MinFn: null,
                MaxFn: null,
                AdditionalParameters: null
            )
        );

        List<SolverCandidate> candidates;
        SolverDiagnostics diagnostics;
        (candidates, diagnostics) = await _solver.SolveAsync(solverRequest, CancellationToken.None);

        // Assert
        candidates.Should().NotBeEmpty("A candidate design should be generated for TC-B.");

        var candidate = candidates.First();
        var candidateDesign = ConvertToCandidateDesign(candidate);

        // Validate Block Coefficient (moderate Cb)
        candidate.Cb.Should().BeInRange(
            ValidationTestCases.TestCaseB.ExpectedCbMin,
            ValidationTestCases.TestCaseB.ExpectedCbMax,
            $"Block Coefficient should be in moderate range {ValidationTestCases.TestCaseB.ExpectedCbMin}-{ValidationTestCases.TestCaseB.ExpectedCbMax}.");

        // Validate Froude Number (medium speed)
        candidate.Fn.Should().BeInRange(
            ValidationTestCases.TestCaseB.ExpectedFnMin,
            ValidationTestCases.TestCaseB.ExpectedFnMax,
            $"Froude Number should be in range {ValidationTestCases.TestCaseB.ExpectedFnMin}-{ValidationTestCases.TestCaseB.ExpectedFnMax}.");

        // Validate dimensions
        candidate.LppM.Should().BeInRange(
            ValidationTestCases.TestCaseB.ExpectedLppMin,
            ValidationTestCases.TestCaseB.ExpectedLppMax,
            $"Length should be in range {ValidationTestCases.TestCaseB.ExpectedLppMin}-{ValidationTestCases.TestCaseB.ExpectedLppMax}m.");

        candidate.BeamM.Should().BeInRange(
            ValidationTestCases.TestCaseB.ExpectedBeamMin,
            ValidationTestCases.TestCaseB.ExpectedBeamMax,
            $"Beam should be in range {ValidationTestCases.TestCaseB.ExpectedBeamMin}-{ValidationTestCases.TestCaseB.ExpectedBeamMax}m.");

        // Validate Alexander Limit
        var alexanderValidation = _validationService.ValidateAlexanderLimit(candidate.Fn, candidate.Cb);
        alexanderValidation.ViolatesLimit.Should().BeFalse(
            "Design should not violate Alexander Limit.");

        // Validate resistance trend (Moderate)
        if (candidate.EhpKw.HasValue && candidate.DisplacementT > 0)
        {
            var resistanceValidation = _validationService.ValidateResistanceTrend(
                candidate.EhpKw.Value,
                candidate.DisplacementT,
                ValidationTestCases.TestCaseB.VesselSubtype);

            resistanceValidation.TrendCategory.Should().BeEquivalentTo(ValidationTestCases.TestCaseB.ExpectedEhpTrend,
                $"Resistance trend should be '{ValidationTestCases.TestCaseB.ExpectedEhpTrend}'.");
        }
    }

    private static CandidateDesign ConvertToCandidateDesign(HullSizingService.Services.Solver.SolverCandidate candidate)
    {
        return new CandidateDesign
        {
            Id = Guid.NewGuid(),
            SizingRunId = Guid.Empty,
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
