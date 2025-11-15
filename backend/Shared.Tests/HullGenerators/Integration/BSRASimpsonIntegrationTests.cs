using Shared.Constants;
using Shared.HullGenerators.Integration;

namespace Shared.Tests.HullGenerators.Integration;

/// <summary>
/// Tests for BSRA Simpson integration methods
/// </summary>
public class BSRASimpsonIntegrationTests
{
    [Fact]
    public void CalculateVolume_WithBSRAStations_ReturnsCorrectVolume()
    {
        // Arrange - Create BSRA standard 23 stations
        var stations = BSRAConstants.SimpsonMultipliers.Select(m => m.Station * 10m).ToList(); // Scale to actual length
        var length = 185m; // Product Carrier Lbp
        var sectionalAreas = new List<decimal>
        {
            13.94m, 38.69m, 79.39m, 123.78m, 162.73m, 233.88m, 290.80m, 332.62m, 345.97m, 352.02m,
            353.36m, 353.96m, 353.96m, 353.93m, 353.33m, 352.06m, 344.83m, 314.71m, 249.49m,
            200.06m, 141.47m, 84.11m, 26.46m
        };

        // Act
        var volume = BSRASimpsonIntegration.CalculateVolume(stations, sectionalAreas, length);

        // Assert
        volume.Should().BeGreaterThan(0);
        // Reference volume from Product Carrier: ~52,827 m³
        // Allow 5% tolerance for test
        volume.Should().BeApproximately(52827m, 2641m); // ±5%
    }

    [Fact]
    public void CalculateLCB_WithBSRAStations_ReturnsCorrectLCB()
    {
        // Arrange
        var stations = BSRAConstants.SimpsonMultipliers.Select(m => m.Station * 10m).ToList();
        var length = 185m;
        var sectionalAreas = new List<decimal>
        {
            13.94m, 38.69m, 79.39m, 123.78m, 162.73m, 233.88m, 290.80m, 332.62m, 345.97m, 352.02m,
            353.36m, 353.96m, 353.96m, 353.93m, 353.33m, 352.06m, 344.83m, 314.71m, 249.49m,
            200.06m, 141.47m, 84.11m, 26.46m
        };

        // Act
        var lcb = BSRASimpsonIntegration.CalculateLCB(stations, sectionalAreas, length);

        // Assert
        lcb.Should().BeGreaterThan(0);
        // LCB should be forward of midship for Product Carrier
        var midship = length / 2.0m;
        lcb.Should().BeGreaterThan(midship);
    }

    [Fact]
    public void CalculateVolume_WithNon23Stations_ThrowsException()
    {
        // Arrange
        var stations = new List<decimal> { 0, 50, 100, 150, 200 };
        var sectionalAreas = new List<decimal> { 100, 200, 300, 200, 100 };
        var length = 200m;

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            BSRASimpsonIntegration.CalculateVolume(stations, sectionalAreas, length));
    }

    [Fact]
    public void GetMultiplierByIndex_WithValidIndex_ReturnsCorrectMultiplier()
    {
        // Act
        var multiplier0 = BSRASimpsonIntegration.GetMultiplierByIndex(0); // AP: 0.25
        var multiplier11 = BSRASimpsonIntegration.GetMultiplierByIndex(11); // Midship: 4.0
        var multiplier22 = BSRASimpsonIntegration.GetMultiplierByIndex(22); // FP: 0.25

        // Assert
        multiplier0.Should().Be(0.25m);
        multiplier11.Should().Be(4.0m);
        multiplier22.Should().Be(0.25m);
    }
}
