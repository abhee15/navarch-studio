using DataService.Services.Resistance;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DataService.Tests.Services.Resistance;

/// <summary>
/// Unit tests for WaterPropertiesService
/// Tests water density and kinematic viscosity calculations
/// </summary>
public class WaterPropertiesServiceTests
{
    private readonly Mock<ILogger<WaterPropertiesService>> _logger;
    private readonly WaterPropertiesService _service;

    public WaterPropertiesServiceTests()
    {
        _logger = new Mock<ILogger<WaterPropertiesService>>();
        _service = new WaterPropertiesService(_logger.Object);
    }

    [Theory]
    [InlineData(0, 35, 1.7915e-6, 0.4e-6)]      // 0°C, standard salinity - calculated from formula
    [InlineData(5, 35, 1.5715e-6, 0.4e-6)]      // 5°C
    [InlineData(10, 35, 1.3795e-6, 0.4e-6)]     // 10°C
    [InlineData(15, 35, 1.3535e-6, 0.3e-6)]     // 15°C - calculated: 1.7915-0.0352*15+0.0004*225 = 1.3535
    [InlineData(20, 35, 1.2475e-6, 0.3e-6)]     // 20°C - calculated
    [InlineData(25, 35, 1.1615e-6, 0.3e-6)]     // 25°C - calculated
    [InlineData(30, 35, 1.0955e-6, 0.3e-6)]     // 30°C - calculated
    public void GetKinematicViscosity_StandardTemperatures_ReturnsReasonableValues(
        int tempC, decimal salinityPpt, double expectedNu, double tolerance)
    {
        // Act
        decimal nu = _service.GetKinematicViscosity(tempC, salinityPpt);

        // Assert
        // Use calculated values from formula: ν = (1.7915 - 0.0352*T + 0.0004*T²) × 10^-6
        double nuDouble = (double)nu;
        nuDouble.Should().BeApproximately(expectedNu, tolerance);
    }

    [Fact]
    public void GetKinematicViscosity_StandardConditions_ReturnsReasonableValue()
    {
        // Arrange - 15°C, 35 ppt is standard seawater
        // Formula: ν = (1.7915 - 0.0352*15 + 0.0004*225) × 10^-6 = 1.3535 × 10^-6
        int tempC = 15;
        decimal salinityPpt = 35.0m;
        decimal expectedNu = 1.3535e-6m; // Calculated from formula

        // Act
        decimal nu = _service.GetKinematicViscosity(tempC, salinityPpt);

        // Assert - Allow tolerance for salinity correction
        nu.Should().BeApproximately(expectedNu, 0.2e-6m);
    }

    [Fact]
    public void GetKinematicViscosity_ClampsToReasonableRange()
    {
        // Arrange - Extreme temperatures
        decimal nu1 = _service.GetKinematicViscosity(-10, 35);  // Very cold
        decimal nu2 = _service.GetKinematicViscosity(50, 35);   // Very hot

        // Assert - Should be clamped
        nu1.Should().BeGreaterThanOrEqualTo(0.8e-6m);
        nu2.Should().BeLessThanOrEqualTo(2.0e-6m);
    }

    [Theory]
    [InlineData(0, 35, 1028)]     // 0°C
    [InlineData(15, 35, 1025)]    // 15°C (reference)
    [InlineData(25, 35, 1023)]    // 25°C
    [InlineData(30, 35, 1022)]    // 30°C
    public void GetWaterDensity_StandardTemperatures_ReturnsReasonableValues(
        int tempC, decimal salinityPpt, double expectedRho)
    {
        // Act
        decimal rho = _service.GetWaterDensity(tempC, salinityPpt);

        // Assert
        // Allow ±5 kg/m³ tolerance
        double rhoDouble = (double)rho;
        rhoDouble.Should().BeApproximately(expectedRho, 5.0);
    }

    [Fact]
    public void GetWaterDensity_StandardSeawater_MatchesExpected()
    {
        // Arrange - 15°C, 35 ppt is standard seawater
        int tempC = 15;
        decimal salinityPpt = 35.0m;
        decimal expectedRho = 1025.0m; // Standard seawater density

        // Act
        decimal rho = _service.GetWaterDensity(tempC, salinityPpt);

        // Assert
        rho.Should().BeApproximately(expectedRho, 2.0m);
    }

    [Fact]
    public void GetWaterDensity_Freshwater_LowerThanSeawater()
    {
        // Arrange
        decimal rhoSeawater = _service.GetWaterDensity(15, 35);
        decimal rhoFreshwater = _service.GetWaterDensity(15, 0);

        // Assert - Freshwater should be less dense
        rhoFreshwater.Should().BeLessThan(rhoSeawater);
    }

    [Fact]
    public void GetWaterDensity_ClampsToReasonableRange()
    {
        // Arrange - Extreme temperatures
        decimal rho1 = _service.GetWaterDensity(-10, 35);  // Very cold
        decimal rho2 = _service.GetWaterDensity(50, 35);     // Very hot

        // Assert - Should be clamped
        rho1.Should().BeGreaterThanOrEqualTo(995.0m);
        rho2.Should().BeLessThanOrEqualTo(1030.0m);
    }

    [Fact]
    public void GetAirDensity_ReturnsStandardValue()
    {
        // Act
        decimal rhoAir = WaterPropertiesService.GetAirDensity();

        // Assert
        rhoAir.Should().BeApproximately(1.225m, 0.001m);
    }

    [Fact]
    public void GetGravity_ReturnsStandardValue()
    {
        // Act
        decimal g = WaterPropertiesService.GetGravity();

        // Assert
        g.Should().BeApproximately(9.80665m, 0.0001m);
    }
}
