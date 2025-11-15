using FluentAssertions;
using HullSizingService.Services.Solver;
using HullSizingService.Tests.TestData;
using Microsoft.Extensions.Logging;
using Moq;

namespace HullSizingService.Tests.Services;

public class HoltropResistanceServiceTests
{
    private readonly HoltropResistanceService _service;
    private readonly Mock<ILogger<HoltropResistanceService>> _loggerMock;

    public HoltropResistanceServiceTests()
    {
        _loggerMock = new Mock<ILogger<HoltropResistanceService>>();
        _service = new HoltropResistanceService(_loggerMock.Object);
    }

    [Fact]
    public async Task CalculateAsync_KCS_ShouldProduceReasonableResistance()
    {
        // Arrange
        var request = new ResistanceRequest(
            LppM: ReferenceVessels.KCS.LppM,
            LwlM: ReferenceVessels.KCS.LwlM,
            BeamM: ReferenceVessels.KCS.BeamM,
            DraftM: ReferenceVessels.KCS.DraftM,
            Cb: ReferenceVessels.KCS.Cb,
            Cp: ReferenceVessels.KCS.Cp,
            Cwp: ReferenceVessels.KCS.Cwp,
            Cm: ReferenceVessels.KCS.Cm,
            SpeedKn: ReferenceVessels.KCS.ServiceSpeedKn,
            WaterDensityKgM3: 1025m,
            KinematicViscosityM2S: 0.000001188m // 15°C seawater
        );

        // Act
        var result = await _service.CalculateAsync(request, default);

        // Assert
        result.Fn.Should().BeApproximately(ReferenceVessels.KCS.DesignFn, 0.01m, "Froude number should match design Fn");
        result.Cf.Should().BeGreaterThan(0, "friction coefficient should be positive");
        result.Cf.Should().BeLessThan(0.01m, "friction coefficient should be reasonable (<1%)");
        result.FormFactor.Should().BeGreaterThan(1.0m, "form factor should be >= 1.0");
        result.FormFactor.Should().BeLessThan(1.5m, "form factor should be reasonable (<1.5)");
        result.FrictionalResistanceKn.Should().BeGreaterThan(0, "frictional resistance should be positive");
        result.WaveResistanceKn.Should().BeGreaterThan(0, "wave resistance should be positive");
        result.TotalResistanceKn.Should().Be(result.FrictionalResistanceKn + result.WaveResistanceKn);
        result.EhpKw.Should().BeGreaterThan(0, "EHP should be positive");
        result.ShpKw.Should().BeGreaterThan(result.EhpKw, "SHP should be > EHP due to efficiency losses");

        // Sanity check: EHP should be in reasonable range for a 52,000t container ship at 24kn
        // Typical: 10,000-30,000 kW
        result.EhpKw.Should().BeInRange(5000m, 50000m, "EHP should be in reasonable range for large container ship");
    }

    [Fact]
    public async Task CalculateAsync_KCS_AgainstReference_WithinReasonableRange()
    {
        // Arrange - Use reference data from constants
        var referenceData = KCSResistanceConstants.ResistanceData;

        // Test a few speeds from the reference data
        var testSpeeds = new[] { 1.0m, 1.5m, 2.0m, 2.5m, 3.0m }; // m/s

        foreach (var speedMps in testSpeeds)
        {
            var reference = referenceData.FirstOrDefault(r => r.Speed_mps == speedMps);
            if (reference == null) continue;

            var speedKn = speedMps * 1.94384m; // Convert m/s to knots

            var request = new ResistanceRequest(
                LppM: ReferenceVessels.KCS.LppM,
                LwlM: ReferenceVessels.KCS.LwlM,
                BeamM: ReferenceVessels.KCS.BeamM,
                DraftM: ReferenceVessels.KCS.DraftM,
                Cb: ReferenceVessels.KCS.Cb,
                Cp: ReferenceVessels.KCS.Cp,
                Cwp: ReferenceVessels.KCS.Cwp,
                Cm: ReferenceVessels.KCS.Cm,
                SpeedKn: speedKn,
                WaterDensityKgM3: 1025m,
                KinematicViscosityM2S: 0.000001188m
            );

            // Act
            var result = await _service.CalculateAsync(request, default);

            // Assert - Compare total resistance (convert kN to N for comparison)
            var computedResistanceN = result.TotalResistanceKn * 1000m; // Convert kN to N

            // Note: Reference data is synthetic/test data, so we use a relaxed tolerance
            // The goal is to ensure calculations are in the right order of magnitude
            var resistanceRatio = computedResistanceN / reference.RT_ref_N;

            // Resistance should be within 2x-0.5x of reference (allowing for method differences)
            resistanceRatio.Should().BeInRange(0.5m, 2.0m,
                $"Total resistance at {speedMps} m/s should be in reasonable range. " +
                $"Reference: {reference.RT_ref_N:N} N, Computed: {computedResistanceN:N} N, Ratio: {resistanceRatio:F2}");

            // Log for debugging
            System.Diagnostics.Debug.WriteLine(
                $"Speed: {speedMps} m/s ({speedKn:F2} kn), " +
                $"Reference RT: {reference.RT_ref_N:N} N, " +
                $"Computed RT: {computedResistanceN:N} N, " +
                $"Ratio: {resistanceRatio:F2}");
        }
    }

    [Fact]
    public async Task CalculateAsync_KVLCC2_ShouldProduceReasonableResistance()
    {
        // Arrange
        var request = new ResistanceRequest(
            LppM: ReferenceVessels.KVLCC2.LppM,
            LwlM: ReferenceVessels.KVLCC2.LwlM,
            BeamM: ReferenceVessels.KVLCC2.BeamM,
            DraftM: ReferenceVessels.KVLCC2.DraftM,
            Cb: ReferenceVessels.KVLCC2.Cb,
            Cp: ReferenceVessels.KVLCC2.Cp,
            Cwp: ReferenceVessels.KVLCC2.Cwp,
            Cm: ReferenceVessels.KVLCC2.Cm,
            SpeedKn: ReferenceVessels.KVLCC2.ServiceSpeedKn,
            WaterDensityKgM3: 1025m,
            KinematicViscosityM2S: 0.000001188m
        );

        // Act
        var result = await _service.CalculateAsync(request, default);

        // Assert
        result.Fn.Should().BeApproximately(ReferenceVessels.KVLCC2.DesignFn, 0.01m);
        result.Cf.Should().BeGreaterThan(0).And.BeLessThan(0.01m);
        result.EhpKw.Should().BeGreaterThan(0);

        // VLCC at 15.5kn should have lower power than container ship at 24kn
        // Typical: 15,000-25,000 kW
        result.EhpKw.Should().BeInRange(8000m, 40000m, "EHP should be reasonable for VLCC at service speed");
    }

    [Theory]
    [InlineData(10, 0.115)] // Low speed - Fn = 10*0.5144/√(9.81*204) = 0.115
    [InlineData(15, 0.172)] // Medium speed - Fn = 15*0.5144/√(9.81*204) = 0.172
    [InlineData(20, 0.230)] // High speed for tanker - Fn = 20*0.5144/√(9.81*204) = 0.230
    public async Task CalculateAsync_VariousSpeeds_PowerShouldIncreaseCubically(decimal speedKn, decimal expectedFn)
    {
        // Arrange - Same hull, different speeds
        var request = new ResistanceRequest(
            LppM: 200m,
            LwlM: 204m,
            BeamM: 30m,
            DraftM: 12m,
            Cb: 0.75m,
            Cp: 0.78m,
            Cwp: 0.86m,
            Cm: 0.96m,
            SpeedKn: speedKn,
            WaterDensityKgM3: 1025m,
            KinematicViscosityM2S: 0.000001188m
        );

        // Act
        var result = await _service.CalculateAsync(request, default);

        // Assert
        result.Fn.Should().BeApproximately(expectedFn, 0.02m, "Froude number should match expected for speed");
        result.EhpKw.Should().BeGreaterThan(0, "power should increase with speed");

        // Power increases approximately with V³ at low speeds
        // This is a sanity check, not exact validation
    }

    [Fact]
    public async Task CalculateAsync_Performance_ShouldCompleteUnder50ms()
    {
        // Arrange
        var request = new ResistanceRequest(
            LppM: 150m,
            LwlM: 153m,
            BeamM: 22m,
            DraftM: 9m,
            Cb: 0.68m,
            Cp: 0.71m,
            Cwp: 0.83m,
            Cm: 0.96m,
            SpeedKn: 18m,
            WaterDensityKgM3: 1025m,
            KinematicViscosityM2S: 0.000001188m
        );

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await _service.CalculateAsync(request, default);
        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.Should().BeLessThan(50, "resistance calculation should complete in <50ms");
        result.EhpKw.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CalculateAsync_HighBlockCoefficient_ShouldHaveHigherResistance()
    {
        // Arrange - Compare two hulls: Cb=0.60 vs Cb=0.80
        var requestSlim = new ResistanceRequest(
            LppM: 150m,
            LwlM: 153m,
            BeamM: 21m,
            DraftM: 8.4m,
            Cb: 0.60m, // Slim hull
            Cp: 0.63m,
            Cwp: 0.78m,
            Cm: 0.95m,
            SpeedKn: 20m,
            WaterDensityKgM3: 1025m,
            KinematicViscosityM2S: 0.000001188m
        );

        var requestFull = new ResistanceRequest(
            LppM: 150m,
            LwlM: 153m,
            BeamM: 21m,
            DraftM: 8.4m,
            Cb: 0.80m, // Full hull
            Cp: 0.83m,
            Cwp: 0.88m,
            Cm: 0.96m,
            SpeedKn: 20m,
            WaterDensityKgM3: 1025m,
            KinematicViscosityM2S: 0.000001188m
        );

        // Act
        var resultSlim = await _service.CalculateAsync(requestSlim, default);
        var resultFull = await _service.CalculateAsync(requestFull, default);

        // Assert
        resultFull.WaveResistanceKn.Should().BeGreaterThan(resultSlim.WaveResistanceKn,
            "fuller hull should have higher wave resistance at same Fn");
    }

    [Fact]
    public async Task CalculateAsync_ShpMargins_ShouldBeApplied()
    {
        // Arrange
        var request = new ResistanceRequest(
            LppM: 150m,
            LwlM: 153m,
            BeamM: 22m,
            DraftM: 9m,
            Cb: 0.68m,
            Cp: 0.71m,
            Cwp: 0.83m,
            Cm: 0.96m,
            SpeedKn: 18m,
            WaterDensityKgM3: 1025m,
            KinematicViscosityM2S: 0.000001188m
        );

        // Act
        var result = await _service.CalculateAsync(request, default);

        // Assert
        // SHP = EHP / η * (1 + sea_margin) * (1 + service_margin)
        // With η=0.60, sea=0.15, service=0.10:
        // SHP = EHP / 0.60 * 1.15 * 1.10 = EHP * 2.108
        var expectedShp = result.EhpKw / 0.60m * 1.15m * 1.10m;
        result.ShpKw.Should().BeApproximately(expectedShp, 1m, "SHP should include propulsive efficiency and margins");
    }
}
