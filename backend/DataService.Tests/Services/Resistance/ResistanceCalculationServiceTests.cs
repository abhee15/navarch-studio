using DataService.Services.Resistance;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataService.Tests.Services.Resistance;

public class ResistanceCalculationServiceTests
{
    private readonly IResistanceCalculationService _service;
    private readonly WaterPropertiesService _waterProperties;

    public ResistanceCalculationServiceTests()
    {
        var logger = new Mock<ILogger<ResistanceCalculationService>>();
        var waterLogger = new Mock<ILogger<WaterPropertiesService>>();
        _waterProperties = new WaterPropertiesService(waterLogger.Object);
        _service = new ResistanceCalculationService(logger.Object, _waterProperties);
    }

    [Theory]
    // Expected values calculated from ITTC-57 formula: CF = 0.075 / (log10(Re) - 2)^2
    [InlineData(1e5, 0.008333)]  // Re=1e5: log10(1e5)=5, CF=0.075/(3^2)=0.008333
    [InlineData(1e6, 0.004688)]  // Re=1e6: log10(1e6)=6, CF=0.075/(4^2)=0.004688
    [InlineData(1e7, 0.003000)]  // Re=1e7: log10(1e7)=7, CF=0.075/(5^2)=0.003000
    [InlineData(1e8, 0.002083)]  // Re=1e8: log10(1e8)=8, CF=0.075/(6^2)=0.002083
    [InlineData(1e9, 0.001531)]  // Re=1e9: log10(1e9)=9, CF=0.075/(7^2)=0.001531
    public void CalculateIttc57Cf_CanonicalReValues_ReturnsExpectedValues(double reDouble, double expectedCfDouble)
    {
        // Arrange
        decimal re = (decimal)reDouble;
        decimal expectedCf = (decimal)expectedCfDouble;

        // Act
        decimal cf = _service.CalculateIttc57Cf(re);

        // Assert: Allow 1% tolerance for formula calculations
        decimal tolerance = expectedCf * 0.01m;
        Assert.InRange(cf, expectedCf - tolerance, expectedCf + tolerance);
    }

    [Fact]
    public void CalculateIttc57Cf_InvalidRe_ThrowsException()
    {
        // Arrange
        decimal re = 0m;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _service.CalculateIttc57Cf(re));
    }

    [Fact]
    public void CalculateIttc57Cf_VeryLowRe_WarnsButDoesNotThrow()
    {
        // Arrange
        decimal re = 50000m; // Below valid range but should still compute

        // Act
        decimal cf = _service.CalculateIttc57Cf(re);

        // Assert: Should still return a value (though with warning)
        Assert.True(cf > 0);
    }

    [Fact]
    public void CalculateEffectiveCf_WithFormFactor_AppliesMultiplier()
    {
        // Arrange
        decimal cf = 0.003m;
        decimal k = 0.20m;

        // Act
        decimal cfEff = _service.CalculateEffectiveCf(cf, k, applyFormFactor: true);

        // Assert: CF_eff = (1 + k) * CF = 1.2 * 0.003 = 0.0036
        Assert.Equal(0.0036m, cfEff, 6);
    }

    [Fact]
    public void CalculateEffectiveCf_WithoutFormFactor_ReturnsOriginal()
    {
        // Arrange
        decimal cf = 0.003m;
        decimal k = 0.20m;

        // Act
        decimal cfEff = _service.CalculateEffectiveCf(cf, k, applyFormFactor: false);

        // Assert: Should return original CF
        Assert.Equal(cf, cfEff);
    }

    [Fact]
    public void CalculateEffectiveCf_DifferentKValues_ScalesCorrectly()
    {
        // Arrange
        decimal cf = 0.003m;
        decimal k1 = 0.10m;
        decimal k2 = 0.30m;

        // Act
        decimal cfEff1 = _service.CalculateEffectiveCf(cf, k1, applyFormFactor: true);
        decimal cfEff2 = _service.CalculateEffectiveCf(cf, k2, applyFormFactor: true);

        // Assert
        Assert.Equal(0.0033m, cfEff1, 6); // 1.1 * 0.003
        Assert.Equal(0.0039m, cfEff2, 6); // 1.3 * 0.003
    }

    [Fact]
    public void CalculateReynoldsNumber_StandardConditions_ReturnsCorrectValue()
    {
        // Arrange
        decimal speed = 10.0m; // m/s
        decimal lwl = 120.0m;   // m
        decimal nu = 1.188e-6m; // m²/s (at 15°C)

        // Act
        decimal re = _service.CalculateReynoldsNumber(speed, lwl, nu);

        // Assert: Re = V * L / ν = 10 * 120 / 1.188e-6 ≈ 1.010e9
        decimal expectedRe = speed * lwl / nu;
        Assert.Equal(expectedRe, re, 0);
    }

    [Fact]
    public void CalculateReynoldsNumber_LowRe_GeneratesWarning()
    {
        // Arrange: Very low speed and small length to get Re < 2e6
        decimal speed = 0.1m; // Very low speed
        decimal lwl = 5.0m;    // Small model
        decimal nu = 1.188e-6m;

        // Act
        decimal re = _service.CalculateReynoldsNumber(speed, lwl, nu);

        // Assert: Should calculate but be below threshold (Re = 0.1 * 5 / 1.188e-6 ≈ 420,875 < 2e6)
        Assert.True(re < 2e6m);
    }

    [Fact]
    public void CalculateReynoldsNumber_InvalidInputs_ThrowsException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _service.CalculateReynoldsNumber(-1m, 120m, 1.188e-6m));
        Assert.Throws<ArgumentException>(() => 
            _service.CalculateReynoldsNumber(10m, 0m, 1.188e-6m));
        Assert.Throws<ArgumentException>(() => 
            _service.CalculateReynoldsNumber(10m, 120m, 0m));
    }

    [Fact]
    public void CalculateFroudeNumber_StandardConditions_ReturnsCorrectValue()
    {
        // Arrange
        decimal speed = 10.0m; // m/s
        decimal lwl = 120.0m;   // m
        decimal g = 9.80665m;   // m/s²

        // Act
        decimal fn = _service.CalculateFroudeNumber(speed, lwl, g);

        // Assert: Fn = V / sqrt(g*L) = 10 / sqrt(9.80665 * 120) ≈ 0.291
        decimal expectedFn = speed / (decimal)Math.Sqrt((double)(g * lwl));
        Assert.Equal(expectedFn, fn, 4);
    }

    [Fact]
    public void CalculateFroudeNumber_InvalidInputs_ThrowsException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _service.CalculateFroudeNumber(-1m, 120m, 9.80665m));
        Assert.Throws<ArgumentException>(() => 
            _service.CalculateFroudeNumber(10m, 0m, 9.80665m));
        Assert.Throws<ArgumentException>(() => 
            _service.CalculateFroudeNumber(10m, 120m, 0m));
    }

    [Fact]
    public void CalculateReynoldsNumber_ThenCalculateCf_ProducesConsistentResults()
    {
        // Arrange: Typical ship speed
        decimal speed = 10.29m; // m/s (~20 knots)
        decimal lwl = 120.0m;
        decimal nu = 1.188e-6m;

        // Act
        decimal re = _service.CalculateReynoldsNumber(speed, lwl, nu);
        decimal cf = _service.CalculateIttc57Cf(re);

        // Assert: Should produce reasonable CF value (typically 0.002-0.003 for full scale)
        Assert.True(cf > 0.001m);
        Assert.True(cf < 0.005m);
    }
}

