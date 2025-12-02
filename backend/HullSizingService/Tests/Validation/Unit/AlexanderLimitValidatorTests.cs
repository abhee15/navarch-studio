using FluentAssertions;
using Shared.TestData;
using Xunit;

namespace HullSizingService.Tests.Validation.Unit;

/// <summary>
/// Unit tests for Alexander Limit validation logic.
///
/// Tests the limit curve interpolation and validation functions in isolation
/// with no external dependencies.
/// </summary>
[Trait("Category", "Unit")]
public class AlexanderLimitValidatorTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void InterpolateMaxEfficientCb_AtReferencePoint_ReturnsExactValue()
    {
        // Arrange
        var fn = 0.20m;

        // Act
        var maxCb = AlexanderLimitReference.InterpolateMaxEfficientCb(fn);

        // Assert
        maxCb.Should().BeApproximately(0.72m, 0.001m,
            "At reference point Fn=0.20, max efficient Cb should be 0.72");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void InterpolateMaxEfficientCb_BelowMinimum_ReturnsFirstPointValue()
    {
        // Arrange
        var fn = 0.10m; // Below minimum reference point (0.15)

        // Act
        var maxCb = AlexanderLimitReference.InterpolateMaxEfficientCb(fn);

        // Assert
        maxCb.Should().BeApproximately(0.82m, 0.001m,
            "Below minimum Fn, should return max Cb from first reference point");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void InterpolateMaxEfficientCb_AboveMaximum_ReturnsLastPointValue()
    {
        // Arrange
        var fn = 0.35m; // Above maximum reference point (0.30)

        // Act
        var maxCb = AlexanderLimitReference.InterpolateMaxEfficientCb(fn);

        // Assert
        maxCb.Should().BeApproximately(0.58m, 0.001m,
            "Above maximum Fn, should return min Cb from last reference point");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void InterpolateMaxEfficientCb_AtIntermediatePoint_InterpolatesCorrectly()
    {
        // Arrange
        var fn = 0.225m; // Between 0.20 (0.72) and 0.25 (0.65)

        // Act
        var maxCb = AlexanderLimitReference.InterpolateMaxEfficientCb(fn);

        // Assert
        // Should be between 0.65 and 0.72, closer to 0.72 (since we're closer to 0.20)
        maxCb.Should().BeInRange(0.65m, 0.72m,
            "Intermediate Fn should interpolate between adjacent reference points");
        maxCb.Should().BeGreaterThan(0.68m,
            "Closer to Fn=0.20 (Cb=0.72), so result should be > 0.68");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ViolatesLimit_BelowLimit_ReturnsFalse()
    {
        // Arrange
        var fn = 0.20m;
        var cb = 0.65m; // Below max efficient (0.72)

        // Act
        var violates = AlexanderLimitReference.ViolatesLimit(fn, cb);

        // Assert
        violates.Should().BeFalse("Cb below limit should not violate");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ViolatesLimit_AboveLimit_ReturnsTrue()
    {
        // Arrange
        var fn = 0.20m;
        var cb = 0.75m; // Above max efficient (0.72)

        // Act
        var violates = AlexanderLimitReference.ViolatesLimit(fn, cb);

        // Assert
        violates.Should().BeTrue("Cb above limit should violate");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ViolatesLimit_ExactlyAtLimit_ReturnsFalse()
    {
        // Arrange
        var fn = 0.20m;
        var maxCb = AlexanderLimitReference.InterpolateMaxEfficientCb(fn);
        var cb = maxCb; // Exactly at limit

        // Act
        var violates = AlexanderLimitReference.ViolatesLimit(fn, cb);

        // Assert
        violates.Should().BeFalse("Exactly at limit is considered valid (not violated)");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CalculateMarginPercent_BelowLimit_ReturnsPositive()
    {
        // Arrange
        var fn = 0.20m;
        var cb = 0.65m; // Below max efficient (0.72)

        // Act
        var margin = AlexanderLimitReference.CalculateMarginPercent(fn, cb);

        // Assert
        margin.Should().BePositive("Design below limit should have positive margin");
        margin.Should().BeGreaterThan(5m, "Safe margin should be > 5%");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CalculateMarginPercent_AboveLimit_ReturnsNegative()
    {
        // Arrange
        var fn = 0.20m;
        var cb = 0.75m; // Above max efficient (0.72)

        // Act
        var margin = AlexanderLimitReference.CalculateMarginPercent(fn, cb);

        // Assert
        margin.Should().BeNegative("Design above limit should have negative margin");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GetSeverityLevel_Within5Percent_ReturnsWarning()
    {
        // Arrange
        var fn = 0.20m;
        var maxCb = AlexanderLimitReference.InterpolateMaxEfficientCb(fn);
        var cb = maxCb * 0.97m; // 3% margin (< 5%)

        // Act
        var severity = AlexanderLimitReference.GetSeverityLevel(fn, cb);

        // Assert
        severity.Should().Be("Warning", "Within 5% margin should return Warning");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GetSeverityLevel_ViolatesLimit_ReturnsError()
    {
        // Arrange
        var fn = 0.20m;
        var cb = 0.75m; // Above limit

        // Act
        var severity = AlexanderLimitReference.GetSeverityLevel(fn, cb);

        // Assert
        severity.Should().Be("Error", "Violating limit should return Error");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void GetSeverityLevel_SafeMargin_ReturnsInfo()
    {
        // Arrange
        var fn = 0.20m;
        var cb = 0.60m; // Well below limit (safe margin)

        // Act
        var severity = AlexanderLimitReference.GetSeverityLevel(fn, cb);

        // Assert
        severity.Should().Be("Info", "Safe margin (> 5%) should return Info");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(0.15, 0.82)] // First reference point
    [InlineData(0.20, 0.72)]
    [InlineData(0.25, 0.65)]
    [InlineData(0.30, 0.58)] // Last reference point
    public void InterpolateMaxEfficientCb_AtAllReferencePoints_ReturnsExactValues(
        double fnValue, double expectedCbValue)
    {
        // Arrange
        var fn = (decimal)fnValue;
        var expectedCb = (decimal)expectedCbValue;

        // Act
        var maxCb = AlexanderLimitReference.InterpolateMaxEfficientCb(fn);

        // Assert
        maxCb.Should().BeApproximately(expectedCb, 0.001m,
            $"At reference point Fn={fn}, max efficient Cb should be {expectedCb}");
    }
}
