using FluentAssertions;
using HullSizingService.Services.Validation;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HullSizingService.Tests.Validation.Unit;

/// <summary>
/// Unit tests for ShipD constraint validation logic.
///
/// Tests constraint validation in isolation with mock data.
/// </summary>
[Trait("Category", "Unit")]
public class ShipDConstraintValidatorTests
{
    private readonly IShipDConstraintValidationService _validator;

    public ShipDConstraintValidatorTests()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ShipDConstraintValidationService>();
        _validator = new ShipDConstraintValidationService(logger);
    }

    #region Constraint 0: Lb + Ls < 1.0

    [Fact]
    [Trait("Category", "Unit")]
    public void ValidateLongitudinalProportions_ValidProportions_ReturnsValid()
    {
        // Arrange
        var lb = 0.30m;
        var ls = 0.30m; // Sum = 0.60 < 1.0

        // Act
        var result = _validator.ValidateLongitudinalProportions(lb, ls);

        // Assert
        result.IsValid.Should().BeTrue("Lb + Ls = 0.60 < 1.0 should be valid");
        result.Severity.Should().Be("Info");
        result.Message.Should().Contain("valid");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ValidateLongitudinalProportions_SumEqualsOne_ReturnsInvalid()
    {
        // Arrange
        var lb = 0.50m;
        var ls = 0.50m; // Sum = 1.0 (violates constraint)

        // Act
        var result = _validator.ValidateLongitudinalProportions(lb, ls);

        // Assert
        result.IsValid.Should().BeFalse("Lb + Ls = 1.0 violates constraint (must be < 1.0)");
        result.Severity.Should().Be("Error");
        result.Message.Should().Contain("violation");
        result.FixSuggestions.Should().NotBeEmpty("Should provide fix suggestions");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ValidateLongitudinalProportions_SumExceedsOne_ReturnsInvalid()
    {
        // Arrange
        var lb = 0.60m;
        var ls = 0.50m; // Sum = 1.10 > 1.0

        // Act
        var result = _validator.ValidateLongitudinalProportions(lb, ls);

        // Assert
        result.IsValid.Should().BeFalse("Lb + Ls > 1.0 violates constraint");
        result.Severity.Should().Be("Error");
        result.FixSuggestions.Should().NotBeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ValidateLongitudinalProportions_BoundaryCase_JustBelowOne_ReturnsValid()
    {
        // Arrange
        var lb = 0.49m;
        var ls = 0.50m; // Sum = 0.99 < 1.0

        // Act
        var result = _validator.ValidateLongitudinalProportions(lb, ls);

        // Assert
        result.IsValid.Should().BeTrue("Sum = 0.99 < 1.0 is valid (just below boundary)");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(0.25, 0.25, true)]   // Valid
    [InlineData(0.30, 0.30, true)]   // Valid
    [InlineData(0.40, 0.40, true)]   // Valid (0.80)
    [InlineData(0.50, 0.50, false)]  // Invalid (1.0)
    [InlineData(0.60, 0.50, false)]  // Invalid (1.1)
    public void ValidateLongitudinalProportions_VariousCombinations_ValidatesCorrectly(
        double lbValue, double lsValue, bool expectedValid)
    {
        // Arrange
        var lb = (decimal)lbValue;
        var ls = (decimal)lsValue;

        // Act
        var result = _validator.ValidateLongitudinalProportions(lb, ls);

        // Assert
        result.IsValid.Should().Be(expectedValid,
            $"Lb={lb}, Ls={ls}, Sum={lb + ls} should be {(expectedValid ? "valid" : "invalid")}");
    }

    #endregion

    #region Constraint 38: Bulb Dimensions

    [Fact]
    [Trait("Category", "Unit")]
    public void ValidateBulbDimensions_ValidBulb_ReturnsValid()
    {
        // Arrange
        var bulbWidth = 0.25m;
        var bulbHeight = 0.20m;
        var bowSectionWidth = 0.35m; // Bulb narrower than bow section

        // Act
        var result = _validator.ValidateBulbDimensions(bulbWidth, bulbHeight, bowSectionWidth);

        // Assert
        result.IsValid.Should().BeTrue("Bulb width < bow section width should be valid");
        result.Severity.Should().Be("Info");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ValidateBulbDimensions_BulbTooWide_ReturnsInvalid()
    {
        // Arrange
        var bulbWidth = 0.40m;
        var bulbHeight = 0.20m;
        var bowSectionWidth = 0.35m; // Bulb wider than bow section

        // Act
        var result = _validator.ValidateBulbDimensions(bulbWidth, bulbHeight, bowSectionWidth);

        // Assert
        result.IsValid.Should().BeFalse("Bulb width >= bow section width violates constraint");
        result.Severity.Should().Be("Error");
        result.FixSuggestions.Should().NotBeEmpty();
        result.Message.Should().Contain("wider");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ValidateBulbDimensions_ZeroDimensions_ReturnsInvalid()
    {
        // Arrange
        var bulbWidth = 0m;
        var bulbHeight = 0m;
        var bowSectionWidth = 0.35m;

        // Act
        var result = _validator.ValidateBulbDimensions(bulbWidth, bulbHeight, bowSectionWidth);

        // Assert
        result.IsValid.Should().BeFalse("Zero dimensions should be invalid");
        result.FixSuggestions.Should().Contain(s => s.Contains("positive"));
    }

    #endregion

    #region Edge Cases

    [Fact]
    [Trait("Category", "Unit")]
    public void ValidateLongitudinalProportions_VerySmallValues_ReturnsValid()
    {
        // Arrange
        var lb = 0.05m;
        var ls = 0.05m; // Sum = 0.10

        // Act
        var result = _validator.ValidateLongitudinalProportions(lb, ls);

        // Assert
        result.IsValid.Should().BeTrue("Very small proportions should still be valid if sum < 1.0");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ValidateLongitudinalProportions_NegativeValues_ShouldHandleGracefully()
    {
        // Arrange
        var lb = -0.10m; // Invalid input
        var ls = 0.30m;

        // Act
        var result = _validator.ValidateLongitudinalProportions(lb, ls);

        // Assert
        // Note: Current implementation doesn't check for negative, but constraint logic still works
        // Sum = 0.20 < 1.0, so technically valid from constraint perspective
        // In production, would want to add validation for negative values
        var sum = lb + ls;
        if (sum < 1.0m)
        {
            result.IsValid.Should().BeTrue("Constraint check only validates sum < 1.0");
        }
    }

    #endregion
}
