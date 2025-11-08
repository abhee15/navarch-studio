using DataService.Services.Seakeeping;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DataService.Tests.Services.Seakeeping;

/// <summary>
/// Tests for Exceedance Calculator.
/// Validates Rayleigh distribution probability calculations.
/// </summary>
public class ExceedanceCalculatorTests
{
    private readonly ExceedanceCalculator _sut;

    public ExceedanceCalculatorTests()
    {
        var mockLogger = new Mock<ILogger<ExceedanceCalculator>>();
        _sut = new ExceedanceCalculator(mockLogger.Object);
    }

    [Fact]
    public void CalculateExceedanceProbabilities_ValidInput_ReturnsCorrectProbabilities()
    {
        // Arrange
        var significantResponse = 2.0; // m
        var thresholds = new[] { 1.0, 2.0, 3.0 };

        // Act
        var result = _sut.CalculateExceedanceProbabilities(significantResponse, thresholds);

        // Assert
        result.Should().HaveCount(3);
        result.Should().ContainKeys(1.0, 2.0, 3.0);

        // All probabilities should be between 0 and 1
        result.Values.Should().AllSatisfy(p => p.Should().BeInRange(0, 1));

        // Higher thresholds should have lower exceedance probability
        result[1.0].Should().BeGreaterThan(result[2.0]);
        result[2.0].Should().BeGreaterThan(result[3.0]);
    }

    [Fact]
    public void CalculateExceedanceProbabilities_RayleighDistribution_MatchesTheory()
    {
        // Arrange - Significant response = 4.0 → σ = 1.0
        var significantResponse = 4.0;
        var threshold = 1.0;

        // Act
        var result = _sut.CalculateExceedanceProbabilities(significantResponse, new[] { threshold });

        // Assert - Rayleigh: P(x > 1) = exp(-1²/(2*1²)) = exp(-0.5) ≈ 0.6065
        var expected = Math.Exp(-0.5);
        result[threshold].Should().BeApproximately(expected, 0.001);
    }

    [Fact]
    public void CalculateExceedanceProbabilities_ZeroResponse_ReturnsZeroProbabilities()
    {
        // Arrange
        var significantResponse = 0.0;
        var thresholds = new[] { 1.0, 2.0 };

        // Act
        var result = _sut.CalculateExceedanceProbabilities(significantResponse, thresholds);

        // Assert
        result.Values.Should().AllSatisfy(p => p.Should().Be(0));
    }

    [Fact]
    public void CalculateExceedanceProbabilities_NegativeThreshold_ReturnsOne()
    {
        // Arrange
        var significantResponse = 2.0;
        var thresholds = new[] { -1.0 };

        // Act
        var result = _sut.CalculateExceedanceProbabilities(significantResponse, thresholds);

        // Assert - Always exceeds negative threshold
        result[-1.0].Should().Be(1.0);
    }

    [Theory]
    [InlineData(1.0, 0.5, 0.882)] // P(x > 0.5σ) ≈ 0.882
    [InlineData(1.0, 1.0, 0.607)] // P(x > 1σ) ≈ 0.607
    [InlineData(1.0, 2.0, 0.135)] // P(x > 2σ) ≈ 0.135
    public void CalculateExceedanceProbabilities_StandardDeviationMultiples_MatchesRayleigh(
        double sigma,
        double thresholdMultiple,
        double expectedProb)
    {
        // Arrange
        var significantResponse = 4.0 * sigma; // x₁/₃ = 4σ
        var threshold = sigma * thresholdMultiple;

        // Act
        var result = _sut.CalculateExceedanceProbabilities(significantResponse, new[] { threshold });

        // Assert
        result[threshold].Should().BeApproximately(expectedProb, 0.01);
    }
}

