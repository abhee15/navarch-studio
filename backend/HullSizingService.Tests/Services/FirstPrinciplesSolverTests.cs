using FluentAssertions;
using HullSizingService.Data;
using HullSizingService.Services;
using HullSizingService.Services.Solver;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Models.Sizing;

namespace HullSizingService.Tests.Services;

public class FirstPrinciplesSolverTests : IDisposable
{
    private readonly SizingDbContext _context;
    private readonly FirstPrinciplesSolver _solver;
    private readonly Mock<IWaterPropertiesService> _waterServiceMock;

    public FirstPrinciplesSolverTests()
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
                DensityKgM3: 1025.87m,
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
    }

    private void SeedTestData()
    {
        // Add hull families
        _context.HullFamilyPresets.AddRange(
            new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = "container",
                DisplayName = "Container Ship",
                LOverBMin = 6.0m,
                LOverBMax = 8.0m,
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
                GeneratorType = "wigley",
                IsActive = true
            },
            new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = "tanker",
                DisplayName = "Oil Tanker",
                LOverBMin = 5.0m,
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
            },
            new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = "bulker",
                DisplayName = "Bulk Carrier",
                LOverBMin = 5.5m,
                LOverBMax = 7.5m,
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
                GeneratorType = "wigley",
                IsActive = true
            }
        );

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
    public async Task SolveAsync_ContainerMission_ShouldGenerateMultipleCandidates()
    {
        // Arrange
        var mission = new MissionCase
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = "test-tenant",
            Name = "Test Container Mission",
            MissionType = "commercial",
            CargoBasis = "teu",
            CargoValue = 5000m,
            TeuCount = 5000,
            ServiceSpeedKn = 24.0m,
            SeaMarginPct = 15.0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new SolverRequest(
            MissionCase: mission,
            Locks: null,
            Options: new SizingOptionsDto(null, 3, null, null)
        );

        // Act
        var (candidates, diagnostics) = await _solver.SolveAsync(request, default);

        // Assert
        candidates.Should().NotBeEmpty("solver should generate candidates");
        candidates.Count.Should().BeLessThanOrEqualTo(3, "should respect max candidates option");
        candidates.Should().AllSatisfy(c =>
        {
            c.LppM.Should().BeGreaterThan(0, "Lpp should be positive");
            c.BeamM.Should().BeGreaterThan(0, "Beam should be positive");
            c.DraftM.Should().BeGreaterThan(0, "Draft should be positive");
            c.Cb.Should().BeInRange(0.4m, 0.9m, "Cb should be in realistic range");
            c.DisplacementT.Should().BeGreaterThan(0, "Displacement should be positive");
            c.Fn.Should().BeGreaterThan(0, "Froude number should be positive");
        });
        diagnostics.Should().NotBeNull("diagnostics should always be returned");
    }

    [Fact]
    public async Task SolveAsync_WeightBasisCargo_ShouldEstimateDisplacementCorrectly()
    {
        // Arrange
        var mission = new MissionCase
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = "test-tenant",
            Name = "Test Weight Cargo",
            MissionType = "commercial",
            CargoBasis = "weight",
            CargoValue = 10000m, // 10,000 tonnes payload
            ServiceSpeedKn = 18.0m,
            SeaMarginPct = 15.0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new SolverRequest(
            MissionCase: mission,
            Locks: null,
            Options: new SizingOptionsDto(null, 2, null, null)
        );

        // Act
        var (candidates, diagnostics) = await _solver.SolveAsync(request, default);

        // Assert
        candidates.Should().NotBeEmpty();
        candidates.First().DisplacementT.Should().BeGreaterThan(10000m,
            "total displacement should be > payload (includes lightship)");
        candidates.First().DisplacementT.Should().BeLessThan(20000m,
            "total displacement should be reasonable (DWT/Δ ratio ~0.65-0.75)");
        diagnostics.Should().NotBeNull();
    }

    [Fact]
    public async Task SolveAsync_VolumeBasisCargo_ShouldConvertCorrectly()
    {
        // Arrange
        var mission = new MissionCase
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = "test-tenant",
            Name = "Test Volume Cargo",
            MissionType = "commercial",
            CargoBasis = "volume",
            CargoValue = 5000m, // 5,000 m³
            CargoVolumeM3 = 5000m,
            CargoDensityTPerM3 = 0.8m, // Light cargo (grain, timber)
            ServiceSpeedKn = 16.0m,
            SeaMarginPct = 15.0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new SolverRequest(
            MissionCase: mission,
            Locks: null,
            Options: new SizingOptionsDto(null, 2, null, null)
        );

        // Act
        var (candidates, diagnostics) = await _solver.SolveAsync(request, default);

        // Assert
        candidates.Should().NotBeEmpty();
        // Payload = 5000 m³ * 0.8 t/m³ = 4,000 tonnes
        // Total displacement should be higher
        candidates.First().DisplacementT.Should().BeGreaterThan(4000m, "displacement should include cargo + lightship");
        diagnostics.Should().NotBeNull();
    }

    [Fact]
    public async Task SolveAsync_ParallelGeneration_ShouldCompleteUnder2Seconds()
    {
        // Arrange
        var mission = new MissionCase
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = "test-tenant",
            Name = "Performance Test",
            MissionType = "commercial",
            CargoBasis = "weight",
            CargoValue = 50000m,
            ServiceSpeedKn = 20.0m,
            SeaMarginPct = 15.0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new SolverRequest(
            MissionCase: mission,
            Locks: null,
            Options: new SizingOptionsDto(null, 3, null, null) // 3 families in parallel
        );

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var (candidates, diagnostics) = await _solver.SolveAsync(request, default);
        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.Should().BeLessThan(2000, "solver should complete <2s for 3 candidates");
        candidates.Count.Should().BeLessThanOrEqualTo(3);
        diagnostics.Should().NotBeNull();
    }

    [Fact]
    public async Task SolveAsync_ScoringAndRanking_ShouldOrderByScore()
    {
        // Arrange
        var mission = new MissionCase
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = "test-tenant",
            Name = "Test Scoring",
            MissionType = "commercial",
            CargoBasis = "weight",
            CargoValue = 20000m,
            ServiceSpeedKn = 18.0m,
            SeaMarginPct = 15.0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new SolverRequest(
            MissionCase: mission,
            Locks: null,
            Options: new SizingOptionsDto(null, 3, null, null)
        );

        // Act
        var (candidates, diagnostics) = await _solver.SolveAsync(request, default);

        // Assert
        candidates.Should().NotBeEmpty();

        // Verify descending order by score
        for (int i = 0; i < candidates.Count - 1; i++)
        {
            candidates[i].Score.Should().BeGreaterThanOrEqualTo(candidates[i + 1].Score,
                $"candidate {i} should have higher or equal score than candidate {i + 1}");
        }

        // All candidates should have scores between 0 and 1
        candidates.Should().AllSatisfy(c =>
        {
            c.Score.Should().BeInRange(0m, 1m, "scores should be normalized 0-1");
        });
        diagnostics.Should().NotBeNull();
    }

    [Fact]
    public async Task SolveAsync_WithFamilyHints_ShouldOnlyGenerateSpecifiedFamilies()
    {
        // Arrange
        var mission = new MissionCase
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = "test-tenant",
            Name = "Test Family Hints",
            MissionType = "commercial",
            CargoBasis = "weight",
            CargoValue = 30000m,
            ServiceSpeedKn = 18.0m,
            SeaMarginPct = 15.0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new SolverRequest(
            MissionCase: mission,
            Locks: null,
            Options: new SizingOptionsDto(
                FamilyHints: new List<string> { "tanker" }, // Only tanker
                MaxCandidates: 5,
                MinFn: null,
                MaxFn: null
            )
        );

        // Act
        var (candidates, diagnostics) = await _solver.SolveAsync(request, default);

        // Assert
        candidates.Should().NotBeEmpty();
        candidates.Should().AllSatisfy(c =>
        {
            c.HullFamily.Should().Be("tanker", "only tanker family was hinted");
        });
        diagnostics.Should().NotBeNull();
    }

    [Fact]
    public async Task SolveAsync_TEUCargo_ShouldEstimatePayloadCorrectly()
    {
        // Arrange - 3000 TEU container ship
        var mission = new MissionCase
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = "test-tenant",
            Name = "3000 TEU Container Ship",
            MissionType = "commercial",
            CargoBasis = "teu",
            CargoValue = 3000m,
            TeuCount = 3000,
            ServiceSpeedKn = 22.0m,
            SeaMarginPct = 15.0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new SolverRequest(
            MissionCase: mission,
            Locks: null,
            Options: new SizingOptionsDto(new List<string> { "container" }, 1, null, null)
        );

        // Act
        var (candidates, diagnostics) = await _solver.SolveAsync(request, default);

        // Assert
        candidates.Should().NotBeEmpty();

        // 3000 TEU * 14t average = 42,000t payload
        // DWT = payload * 1.15 = 48,300t
        // Δ = DWT / 0.70 ≈ 69,000t for container ship
        candidates.First().DisplacementT.Should().BeInRange(50000m, 90000m,
            "displacement should be reasonable for 3000 TEU vessel");
        diagnostics.Should().NotBeNull();
    }

    [Fact]
    public async Task SolveAsync_AllCandidates_ShouldHaveValidStability()
    {
        // Arrange
        var mission = new MissionCase
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = "test-tenant",
            Name = "Stability Test",
            MissionType = "commercial",
            CargoBasis = "weight",
            CargoValue = 25000m,
            ServiceSpeedKn = 18.0m,
            SeaMarginPct = 15.0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new SolverRequest(
            MissionCase: mission,
            Locks: null,
            Options: new SizingOptionsDto(null, 3, null, null)
        );

        // Act
        var (candidates, diagnostics) = await _solver.SolveAsync(request, default);

        // Assert
        candidates.Should().AllSatisfy(c =>
        {
            c.GmEstM.Should().HaveValue("all candidates should have GM estimate");
            c.GmEstM.Should().BeGreaterThan(0m, "GM should be positive for stability");
            c.KbM.Should().HaveValue("all candidates should have KB");
            c.KbM.Should().BeGreaterThan(0m, "KB should be positive");
        });
        diagnostics.Should().NotBeNull();
    }

    [Fact]
    public async Task SolveAsync_AllCandidates_ShouldHaveResistanceCalculated()
    {
        // Arrange
        var mission = new MissionCase
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            TenantId = "test-tenant",
            Name = "Resistance Test",
            MissionType = "commercial",
            CargoBasis = "weight",
            CargoValue = 30000m,
            ServiceSpeedKn = 20.0m,
            SeaMarginPct = 15.0m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var request = new SolverRequest(
            MissionCase: mission,
            Locks: null,
            Options: new SizingOptionsDto(null, 3, null, null)
        );

        // Act
        var (candidates, diagnostics) = await _solver.SolveAsync(request, default);

        // Assert
        candidates.Should().AllSatisfy(c =>
        {
            c.EhpKw.Should().HaveValue("all candidates should have EHP");
            c.EhpKw.Should().BeGreaterThan(0m, "EHP should be positive");
            c.ShpKw.Should().HaveValue("all candidates should have SHP");
            c.ShpKw.Should().BeGreaterThan(c.EhpKw.GetValueOrDefault(0), "SHP should be > EHP");
        });
        diagnostics.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
