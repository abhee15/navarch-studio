using FluentAssertions;
using HullSizingService.Services.Solver;
using HullSizingService.Tests.TestData;
using Microsoft.Extensions.Logging;
using Moq;

namespace HullSizingService.Tests.Services;

public class StabilityScreenServiceTests
{
    private readonly StabilityScreenService _service;
    private readonly Mock<ILogger<StabilityScreenService>> _loggerMock;

    public StabilityScreenServiceTests()
    {
        _loggerMock = new Mock<ILogger<StabilityScreenService>>();
        _service = new StabilityScreenService(_loggerMock.Object);
    }

    [Fact]
    public async Task ScreenAsync_Barge_ShouldMatchAnalyticalSolution()
    {
        // Arrange - Perfect box (analytical solution exists)
        var request = new StabilityRequest(
            LwlM: ReferenceVessels.Barge.LppM,
            BeamM: ReferenceVessels.Barge.BeamM,
            DraftM: ReferenceVessels.Barge.DraftM,
            DepthM: ReferenceVessels.Barge.DepthM,
            Cb: ReferenceVessels.Barge.Cb,
            Cwp: ReferenceVessels.Barge.Cwp,
            VesselType: "barge",
            DisplacementT: ReferenceVessels.Barge.DisplacementT
        );

        // Act
        var result = await _service.ScreenAsync(request, default);

        // Assert
        // For a box: KB = 0.5 * T
        result.KbM.Should().BeApproximately(ReferenceVessels.Barge.ExpectedKB, 0.1m, "KB should be ~0.5*T for box shape");

        // BMt = Iwp / ∇
        // Iwp = (1/12) * L * B³ * Cwp = (1/12) * 50 * 1000 * 1.0 = 4166.67
        // ∇ = 1500 m³
        // BMt = 4166.67 / 1500 = 2.78 m
        result.BmtM.Should().BeApproximately(ReferenceVessels.Barge.ExpectedBMt, 0.3m, "BMt should match analytical solution");

        // GMt should be positive
        result.GmEstM.Should().BeGreaterThan(0, "GM should be positive for stable barge");
        result.GmEstM.Should().BeApproximately(ReferenceVessels.Barge.ExpectedGMt, 0.5m, "GM should be close to analytical estimate");
    }

    [Fact]
    public async Task ScreenAsync_ContainerShip_ShouldHaveHigherKG()
    {
        // Arrange - Container ship (high CG due to deck containers)
        var requestContainer = new StabilityRequest(
            LwlM: 200m,
            BeamM: 30m,
            DraftM: 10m,
            DepthM: 18m,
            Cb: 0.65m,
            Cwp: 0.83m,
            VesselType: "container",
            DisplacementT: 40000m
        );

        var requestTanker = new StabilityRequest(
            LwlM: 200m,
            BeamM: 30m,
            DraftM: 10m,
            DepthM: 18m,
            Cb: 0.80m,
            Cwp: 0.88m,
            VesselType: "tanker",
            DisplacementT: 50000m
        );

        // Act
        var containerResult = await _service.ScreenAsync(requestContainer, default);
        var tankerResult = await _service.ScreenAsync(requestTanker, default);

        // Assert
        // Container ships have higher KG (0.65 * D) vs tankers (0.55 * D)
        containerResult.KgEstM.Should().BeGreaterThan(tankerResult.KgEstM,
            "container ship should have higher KG due to deck containers");

        // Container should have lower GMt (higher KG)
        containerResult.GmEstM.Should().BeLessThan(tankerResult.GmEstM,
            "container ship should have lower GM due to higher KG");
    }

    [Fact]
    public async Task ScreenAsync_LowGM_ShouldFlagStabilityIssue()
    {
        // Arrange - Narrow beam = low BMt = low GM
        var request = new StabilityRequest(
            LwlM: 100m,
            BeamM: 8m, // Very narrow for length
            DraftM: 4m,
            DepthM: 6m,
            Cb: 0.65m,
            Cwp: 0.80m,
            VesselType: "container", // High KG
            DisplacementT: 2000m
        );

        // Act
        var result = await _service.ScreenAsync(request, default);

        // Assert
        result.GmEstM.Should().BeLessThan(1.0m, "narrow beam should result in low GM");
        result.Flags.Should().Contain(f => f.Contains("low_gm"), "should flag low GM condition");
    }

    [Fact]
    public async Task ScreenAsync_WideBeam_ShouldHaveHighGM()
    {
        // Arrange - Wide beam = high BMt = high GM
        var request = new StabilityRequest(
            LwlM: 80m,
            BeamM: 20m, // Wide for length
            DraftM: 5m,
            DepthM: 7m,
            Cb: 0.70m,
            Cwp: 0.85m,
            VesselType: "tanker", // Low KG
            DisplacementT: 6000m
        );

        // Act
        var result = await _service.ScreenAsync(request, default);

        // Assert
        result.GmEstM.Should().BeGreaterThan(2.0m, "wide beam should result in high GM");
        result.Flags.Should().Contain(f => f.Contains("high_gm"), "should flag high GM (stiff ship)");
    }

    [Fact]
    public async Task ScreenAsync_Performance_ShouldCompleteUnder10ms()
    {
        // Arrange
        var request = new StabilityRequest(
            LwlM: 150m,
            BeamM: 22m,
            DraftM: 9m,
            DepthM: 14m,
            Cb: 0.68m,
            Cwp: 0.83m,
            VesselType: "bulker",
            DisplacementT: 25000m
        );

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await _service.ScreenAsync(request, default);
        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.Should().BeLessThan(10, "stability screening should be very fast");
        result.GmEstM.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("container", 0.65)]
    [InlineData("tanker", 0.55)]
    [InlineData("bulker", 0.60)]
    [InlineData("fishing", 0.62)]
    public async Task ScreenAsync_DifferentVesselTypes_ShouldHaveDifferentKG(string vesselType, decimal expectedKgFactor)
    {
        // Arrange
        var depth = 15m;
        var request = new StabilityRequest(
            LwlM: 120m,
            BeamM: 18m,
            DraftM: 7m,
            DepthM: depth,
            Cb: 0.70m,
            Cwp: 0.84m,
            VesselType: vesselType,
            DisplacementT: 12000m
        );

        // Act
        var result = await _service.ScreenAsync(request, default);

        // Assert
        var expectedKg = expectedKgFactor * depth;
        result.KgEstM.Should().BeApproximately(expectedKg, 0.5m,
            $"{vesselType} should have KG factor ~{expectedKgFactor}");
    }

    [Fact]
    public async Task ScreenAsync_RollPeriod_ShouldBeReasonable()
    {
        // Arrange
        var request = new StabilityRequest(
            LwlM: 150m,
            BeamM: 22m,
            DraftM: 9m,
            DepthM: 14m,
            Cb: 0.68m,
            Cwp: 0.83m,
            VesselType: "general_cargo",
            DisplacementT: 25000m
        );

        // Act
        var result = await _service.ScreenAsync(request, default);

        // Assert
        // Typical roll periods: 8-20 seconds
        result.TRollSeconds.Should().BeInRange(5m, 30m, "roll period should be in typical range");

        if (result.TRollSeconds < 8m)
        {
            result.Flags.Should().Contain("fast_roll_period", "should flag unusually fast roll");
        }

        if (result.TRollSeconds > 20m)
        {
            result.Flags.Should().Contain("slow_roll_period", "should flag unusually slow roll");
        }
    }
}




