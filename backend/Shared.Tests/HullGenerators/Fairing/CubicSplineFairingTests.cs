using Shared.HullGenerators.Fairing;

namespace Shared.Tests.HullGenerators.Fairing;

/// <summary>
/// Tests for cubic spline fairing
/// </summary>
public class CubicSplineFairingTests
{
    [Fact]
    public void Fit_WithSimpleData_ReturnsValidCoefficients()
    {
        // Arrange
        var x = new List<decimal> { 0, 1, 2, 3, 4 };
        var y = new List<decimal> { 0, 1, 2, 1, 0 };
        decimal dx0 = 1m; // Start slope
        decimal dxn = -1m; // End slope

        // Act
        var coefficients = CubicSplineFairing.Fit(x, y, dx0, dxn);

        // Assert
        coefficients.Should().NotBeNull();
        coefficients.Count.Should().Be(x.Count - 1); // n-1 intervals
    }

    [Fact]
    public void Evaluate_WithFittedSpline_ReturnsCorrectValues()
    {
        // Arrange
        var x = new List<decimal> { 0, 1, 2, 3, 4 };
        var y = new List<decimal> { 0, 1, 2, 1, 0 };
        var coefficients = CubicSplineFairing.Fit(x, y, 1m, -1m);

        // Act
        var valueAt0 = CubicSplineFairing.Evaluate(coefficients, 0m);
        var valueAt2 = CubicSplineFairing.Evaluate(coefficients, 2m);
        var valueAt4 = CubicSplineFairing.Evaluate(coefficients, 4m);

        // Assert
        valueAt0.Should().BeApproximately(0m, 0.01m);
        valueAt2.Should().BeApproximately(2m, 0.01m);
        valueAt4.Should().BeApproximately(0m, 0.01m);
    }

    [Fact]
    public void Fit_WithInvalidInput_ThrowsException()
    {
        // Arrange
        var x = new List<decimal> { 0, 1, 2 };
        var y = new List<decimal> { 0, 1 }; // Different length

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            CubicSplineFairing.Fit(x, y, 0m, 0m));
    }

    [Fact]
    public void FairWaterline_WithBSRAStations_ReturnsFairedOffsets()
    {
        // Arrange
        var stations = new List<decimal> { 0m, 25m, 50m, 75m, 100m, 150m, 200m, 250m, 300m, 350m, 400m, 500m, 600m, 650m, 700m, 750m, 800m, 850m, 900m, 925m, 950m, 975m, 1000m };
        var halfBreadths = new List<decimal> { 0m, 0.2m, 0.8m, 1.8m, 2.6m, 5.25m, 8.15m, 10.6m, 12.25m, 13.15m, 13.45m, 13.6m, 13.6m, 13.55m, 13.45m, 13.25m, 12.4m, 10.5m, 7.45m, 5.55m, 3.6m, 2.25m, 1.4m };
        decimal waterlineHeight = 1.0m;

        // Act
        var faired = CubicSplineFairing.FairWaterline(stations, halfBreadths, waterlineHeight);

        // Assert
        faired.Should().NotBeNull();
        faired.Count.Should().Be(stations.Count);
        // Check that values are non-negative
        faired.All(hb => hb >= 0).Should().BeTrue();
    }
}
