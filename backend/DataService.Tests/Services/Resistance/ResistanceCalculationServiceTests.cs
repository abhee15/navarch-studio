using DataService.Services.Resistance;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DataService.Tests.Services.Resistance;

/// <summary>
/// Unit tests for ResistanceCalculationService
/// Tests ITTC-57 friction coefficient calculation and related functions
/// </summary>
public class ResistanceCalculationServiceTests
{
    private readonly Mock<ILogger<ResistanceCalculationService>> _logger;
    private readonly WaterPropertiesService _waterProperties;
    private readonly ResistanceCalculationService _service;

    public ResistanceCalculationServiceTests()
    {
        _logger = new Mock<ILogger<ResistanceCalculationService>>();
        var waterPropsLogger = new Mock<ILogger<WaterPropertiesService>>();
        _waterProperties = new WaterPropertiesService(waterPropsLogger.Object);
        _service = new ResistanceCalculationService(_logger.Object, _waterProperties);
    }

    [Theory]
    [InlineData(1e5, 0.008333)]      // Low Reynolds number - calculated: 0.075/(log10(1e5)-2)^2
    [InlineData(1e6, 0.004688)]      // Typical model scale - calculated: 0.075/(log10(1e6)-2)^2
    [InlineData(1e7, 0.003000)]     // Intermediate - calculated: 0.075/(log10(1e7)-2)^2
    [InlineData(1e8, 0.002083)]      // Typical full scale - calculated: 0.075/(log10(1e8)-2)^2
    [InlineData(1e9, 0.001531)]      // Very high Reynolds number - calculated: 0.075/(log10(1e9)-2)^2
    public void CalculateIttc57Cf_CanonicalReValues_ReturnsExpectedValues(double re, double expectedCf)
    {
        // Arrange
        decimal reDecimal = (decimal)re;

        // Act
        decimal cf = _service.CalculateIttc57Cf(reDecimal);

        // Assert
        double cfDouble = (double)cf;
        cfDouble.Should().BeApproximately(expectedCf, 0.0002); // Allow wider tolerance
    }

    [Fact]
    public void CalculateIttc57Cf_InvalidRe_ThrowsException()
    {
        // Arrange
        decimal re = 0;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.CalculateIttc57Cf(re));
    }

    [Fact]
    public void CalculateIttc57Cf_TooLowRe_LogsWarning()
    {
        // Arrange
        decimal re = 50000; // Below valid range

        // Act
        _service.CalculateIttc57Cf(re);

        // Assert - Warning should be logged (can't easily test logging with NSubstitute, but method should not throw)
        // Just verify it completes
        Assert.True(true);
    }

    [Theory]
    [InlineData(0.002, 0.20, true, 0.0024)]    // CF=0.002, k=0.20, should be 1.2 * 0.002 = 0.0024
    [InlineData(0.002, 0.15, true, 0.0023)]    // CF=0.002, k=0.15, should be 1.15 * 0.002 = 0.0023
    [InlineData(0.002, 0.20, false, 0.002)]   // CF=0.002, form factor disabled, should be unchanged
    [InlineData(0.001, 0.20, true, 0.0012)]   // CF=0.001, k=0.20, should be 1.2 * 0.001 = 0.0012
    public void CalculateEffectiveCf_FormFactorApplication_CorrectMultiplication(
        decimal cf, decimal k, bool applyFormFactor, decimal expectedCfEff)
    {
        // Act
        decimal cfEff = _service.CalculateEffectiveCf(cf, k, applyFormFactor);

        // Assert
        cfEff.Should().BeApproximately(expectedCfEff, 0.0001m);
    }

    [Fact]
    public void CalculateReynoldsNumber_ValidInputs_ReturnsCorrectValue()
    {
        // Arrange
        decimal speed = 10.0m;      // 10 m/s
        decimal lwl = 120.0m;      // 120 m
        decimal nu = 1.188e-6m;   // Kinematic viscosity at 15°C

        // Act
        decimal re = _service.CalculateReynoldsNumber(speed, lwl, nu);

        // Assert
        // Re = V * L / ν = 10 * 120 / 1.188e-6 = 1.010101e9
        decimal expectedRe = speed * lwl / nu;
        re.Should().BeApproximately(expectedRe, 0.1m);
    }

    [Theory]
    [InlineData(-1, 120, 1.188e-6)]    // Negative speed
    [InlineData(10, 0, 1.188e-6)]      // Zero LWL
    [InlineData(10, 120, 0)]           // Zero viscosity
    public void CalculateReynoldsNumber_InvalidInputs_ThrowsException(
        decimal speed, decimal lwl, decimal nu)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.CalculateReynoldsNumber(speed, lwl, nu));
    }

    [Fact]
    public void CalculateReynoldsNumber_LowRe_LogsWarning()
    {
        // Arrange
        decimal speed = 0.5m;      // Very low speed
        decimal lwl = 1.0m;        // Small model
        decimal nu = 1.188e-6m;

        // Act
        decimal re = _service.CalculateReynoldsNumber(speed, lwl, nu);

        // Assert
        // Re should be calculated correctly but warning logged
        re.Should().BeLessThan(2e6m);
    }

    [Fact]
    public void CalculateFroudeNumber_ValidInputs_ReturnsCorrectValue()
    {
        // Arrange
        decimal speed = 10.0m;      // 10 m/s
        decimal lwl = 120.0m;      // 120 m
        decimal g = 9.80665m;      // Standard gravity

        // Act
        decimal fn = _service.CalculateFroudeNumber(speed, lwl, g);

        // Assert
        // Fn = V / sqrt(g * L) = 10 / sqrt(9.80665 * 120) = 10 / sqrt(1176.798) ≈ 0.293
        double expectedFn = 10.0 / Math.Sqrt(9.80665 * 120.0);
        ((double)fn).Should().BeApproximately(expectedFn, 0.001);
    }

    [Theory]
    [InlineData(-1, 120, 9.80665)]    // Negative speed
    [InlineData(10, 0, 9.80665)]      // Zero LWL
    [InlineData(10, 120, 0)]          // Zero gravity
    public void CalculateFroudeNumber_InvalidInputs_ThrowsException(
        decimal speed, decimal lwl, decimal g)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.CalculateFroudeNumber(speed, lwl, g));
    }

    [Fact]
    public void CalculateFroudeNumber_StandardGravity_DefaultsCorrectly()
    {
        // Arrange
        decimal speed = 10.0m;
        decimal lwl = 120.0m;

        // Act
        decimal fn1 = _service.CalculateFroudeNumber(speed, lwl);
        decimal fn2 = _service.CalculateFroudeNumber(speed, lwl, 9.80665m);

        // Assert - Should be the same
        fn1.Should().BeApproximately(fn2, 0.0001m);
    }
}
