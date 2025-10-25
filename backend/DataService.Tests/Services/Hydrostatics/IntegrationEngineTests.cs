using DataService.Services.Hydrostatics;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataService.Tests.Services.Hydrostatics;

public class IntegrationEngineTests
{
    private readonly IIntegrationEngine _integrationEngine;

    public IntegrationEngineTests()
    {
        var logger = new Mock<ILogger<IntegrationEngine>>();
        _integrationEngine = new IntegrationEngine(logger.Object);
    }

    [Fact]
    public void TrapezoidalRule_SimpleRectangle_ReturnsCorrectArea()
    {
        // Arrange: Rectangle with height 2, width 3 → Area = 6
        var x = new List<decimal> { 0, 3 };
        var y = new List<decimal> { 2, 2 };

        // Act
        var result = _integrationEngine.TrapezoidalRule(x, y);

        // Assert
        Assert.Equal(6m, result, 3); // 3 decimal places
    }

    [Fact]
    public void TrapezoidalRule_Triangle_ReturnsCorrectArea()
    {
        // Arrange: Triangle with base 4, height 2 → Area = 4
        var x = new List<decimal> { 0, 4 };
        var y = new List<decimal> { 0, 2 };

        // Act
        var result = _integrationEngine.TrapezoidalRule(x, y);

        // Assert
        Assert.Equal(4m, result, 3);
    }

    [Fact]
    public void SimpsonsRule_Parabola_HighAccuracy()
    {
        // Arrange: Integrate x² from 0 to 2 → Exact: 8/3 ≈ 2.6667
        var x = new List<decimal> { 0, 1, 2 };
        var y = x.Select(xi => xi * xi).ToList();

        // Act
        var result = _integrationEngine.SimpsonsRule(x, y);

        // Assert
        var expected = 8m / 3m;
        Assert.True(Math.Abs(result - expected) < 0.01m,
            $"Expected {expected}, got {result}");
    }

    [Fact]
    public void SimpsonsRule_EvenNumberOfPoints_ThrowsException()
    {
        // Arrange: Even number of points (4 points = 3 intervals)
        var x = new List<decimal> { 0, 1, 2, 3 };
        var y = new List<decimal> { 1, 1, 1, 1 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _integrationEngine.SimpsonsRule(x, y));
    }

    [Fact]
    public void CompositeSimpson_EvenNumberOfPoints_HandlesCorrectly()
    {
        // Arrange: Even number of points
        var x = new List<decimal> { 0, 1, 2, 3 };
        var y = new List<decimal> { 1, 1, 1, 1 }; // Constant function → Area = 3

        // Act
        var result = _integrationEngine.CompositeSimpson(x, y);

        // Assert
        Assert.Equal(3m, result, 2);
    }

    [Fact]
    public void Integrate_AutoSelectsMethod_ProducesReasonableResult()
    {
        // Arrange: Simple linear function
        var x = new List<decimal> { 0, 1, 2, 3, 4 };
        var y = new List<decimal> { 0, 1, 2, 3, 4 }; // y = x

        // Act
        var result = _integrationEngine.Integrate(x, y);

        // Assert: ∫x dx from 0 to 4 = x²/2 = 16/2 = 8
        Assert.True(Math.Abs(result - 8m) < 0.1m);
    }

    [Fact]
    public void FirstMoment_SimpleCase_CalculatesCorrectly()
    {
        // Arrange: Compute ∫ x * 1 dx from 0 to 2 → x²/2 = 2
        var x = new List<decimal> { 0, 2 };
        var y = new List<decimal> { 1, 1 };

        // Act
        var result = _integrationEngine.FirstMoment(x, y);

        // Assert
        Assert.Equal(2m, result, 2);
    }

    [Fact]
    public void SecondMoment_SimpleCase_CalculatesCorrectly()
    {
        // Arrange: Compute ∫ x² * 1 dx from 0 to 2
        // Using trapezoidal with 2 points: (0²*1 + 2²*1)/2 * 2 = 4
        var x = new List<decimal> { 0, 2 };
        var y = new List<decimal> { 1, 1 };

        // Act
        var result = _integrationEngine.SecondMoment(x, y);

        // Assert: Trapezoidal approximation gives 4 (exact integral is 8/3 ≈ 2.667)
        Assert.True(Math.Abs(result - 4m) < 0.1m, $"Expected ~4, got {result}");
    }
}

