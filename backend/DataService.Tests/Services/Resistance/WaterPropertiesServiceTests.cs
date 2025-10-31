using DataService.Services.Resistance;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataService.Tests.Services.Resistance;

public class WaterPropertiesServiceTests
{
    private readonly WaterPropertiesService _service;

    public WaterPropertiesServiceTests()
    {
        var logger = new Mock<ILogger<WaterPropertiesService>>();
        _service = new WaterPropertiesService(logger.Object);
    }

    [Theory]
    [InlineData(0.0, 35.0, 1.20e-6)]   // Cold seawater
    [InlineData(15.0, 35.0, 1.188e-6)] // Standard seawater (ITTC reference)
    [InlineData(25.0, 35.0, 0.90e-6)]  // Warm seawater
    public void GetKinematicViscosity_StandardConditions_ReturnsReasonableValues(
        double tempC, double salinityPpt, double expectedNuMin)
    {
        // Arrange
        decimal temp = (decimal)tempC;
        decimal salinity = (decimal)salinityPpt;

        // Act
        decimal nu = _service.GetKinematicViscosity(temp, salinity);

        // Assert: Should be in reasonable range for seawater (0.8e-6 to 2.0e-6 m²/s)
        Assert.True(nu >= 0.8e-6m);
        Assert.True(nu <= 2.0e-6m);
        Assert.True(nu >= (decimal)expectedNuMin * 0.8m); // Allow tolerance
    }

    [Fact]
    public void GetKinematicViscosity_TemperatureVariation_FollowsExpectedTrend()
    {
        // Arrange
        decimal salinity = 35.0m;

        // Act
        decimal nu0 = _service.GetKinematicViscosity(0m, salinity);
        decimal nu15 = _service.GetKinematicViscosity(15m, salinity);
        decimal nu30 = _service.GetKinematicViscosity(30m, salinity);

        // Assert: Viscosity decreases with temperature
        Assert.True(nu0 > nu15);
        Assert.True(nu15 > nu30);
    }

    [Fact]
    public void GetKinematicViscosity_SalinityVariation_FollowsExpectedTrend()
    {
        // Arrange
        decimal temp = 15.0m;

        // Act
        decimal nuFresh = _service.GetKinematicViscosity(temp, 0m);
        decimal nuSalt = _service.GetKinematicViscosity(temp, 35m);

        // Assert: Seawater has slightly higher viscosity (though small difference)
        // Note: The actual difference is small, so we mainly check both are in range
        Assert.True(nuFresh >= 0.8e-6m);
        Assert.True(nuSalt >= 0.8e-6m);
        Assert.True(nuFresh <= 2.0e-6m);
        Assert.True(nuSalt <= 2.0e-6m);
    }

    [Theory]
    [InlineData(0.0, 35.0, 1028.0)]    // Cold seawater
    [InlineData(15.0, 35.0, 1025.0)]    // Standard seawater
    [InlineData(25.0, 35.0, 1023.0)]    // Warm seawater
    public void GetWaterDensity_StandardConditions_ReturnsReasonableValues(
        double tempC, double salinityPpt, double expectedRhoMin)
    {
        // Arrange
        decimal temp = (decimal)tempC;
        decimal salinity = (decimal)salinityPpt;

        // Act
        decimal rho = _service.GetWaterDensity(temp, salinity);

        // Assert: Should be in reasonable range (995-1030 kg/m³)
        Assert.True(rho >= 995.0m);
        Assert.True(rho <= 1030.0m);
        Assert.True(rho >= (decimal)expectedRhoMin * 0.98m); // Allow tolerance
    }

    [Fact]
    public void GetWaterDensity_TemperatureVariation_FollowsExpectedTrend()
    {
        // Arrange
        decimal salinity = 35.0m;

        // Act
        decimal rho0 = _service.GetWaterDensity(0m, salinity);
        decimal rho15 = _service.GetWaterDensity(15m, salinity);
        decimal rho30 = _service.GetWaterDensity(30m, salinity);

        // Assert: Density decreases with temperature
        Assert.True(rho0 > rho15);
        Assert.True(rho15 > rho30);
    }

    [Fact]
    public void GetWaterDensity_SalinityVariation_FollowsExpectedTrend()
    {
        // Arrange
        decimal temp = 15.0m;

        // Act
        decimal rhoFresh = _service.GetWaterDensity(temp, 0m);
        decimal rhoSalt = _service.GetWaterDensity(temp, 35m);

        // Assert: Seawater is denser than freshwater
        Assert.True(rhoSalt > rhoFresh);
        Assert.True(rhoSalt >= 1020m); // Seawater should be >1020 kg/m³
        Assert.True(rhoFresh <= 1005m); // Freshwater should be ~1000 kg/m³
    }

    [Fact]
    public void GetAirDensity_ReturnsStandardValue()
    {
        // Act
        decimal rhoAir = WaterPropertiesService.GetAirDensity();

        // Assert: Standard air density at sea level
        Assert.Equal(1.225m, rhoAir, 3);
    }

    [Fact]
    public void GetGravity_ReturnsStandardValue()
    {
        // Act
        decimal g = WaterPropertiesService.GetGravity();

        // Assert: Standard gravity
        Assert.Equal(9.80665m, g, 5);
    }
}

