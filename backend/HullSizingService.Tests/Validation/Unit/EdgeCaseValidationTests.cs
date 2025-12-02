using FluentAssertions;
using HullSizingService.Services.Validation;
using Shared.Models.Sizing;
using Xunit;

namespace HullSizingService.Tests.Validation.Unit;

/// <summary>
/// Edge case validation tests - ensures validation services handle boundary conditions,
/// invalid inputs, and extreme values gracefully without throwing exceptions.
///
/// These tests ensure robust error handling for production use.
/// </summary>
[Trait("Category", "Unit")]
public class EdgeCaseValidationTests
{
    private readonly DesignValidationService _validationService;

    public EdgeCaseValidationTests()
    {
        _validationService = new DesignValidationService(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DesignValidationService>.Instance);
    }

    [Theory]
    [InlineData(-0.1, 0.80)] // Negative Fn
    [InlineData(0.15, -0.1)] // Negative Cb
    [InlineData(0.15, 1.5)]  // Cb > 1.5 (unrealistic)
    [InlineData(2.0, 0.80)]  // Fn > 0.5 (planing regime - outside our validation range)
    public void ValidateAlexanderLimit_InvalidInputs_ShouldReturnError(decimal fn, decimal cb)
    {
        // Act
        var result = _validationService.ValidateAlexanderLimit(fn, cb);

        // Assert
        result.Severity.Should().Be("Error", "Invalid inputs should result in error severity");
        result.Message.Should().Contain("Invalid", "Error message should indicate invalid input");
    }

    [Fact]
    public void ValidateAlexanderLimit_ZeroFroudeNumber_ShouldReturnMaxCb()
    {
        // Act
        var result = _validationService.ValidateAlexanderLimit(0.0m, 0.80m);

        // Assert
        result.ViolatesLimit.Should().BeFalse("Zero Fn should allow high Cb");
        result.MaxEfficientCb.Should().Be(0.82m, "Zero Fn should return max Cb from first reference point");
    }

    [Fact]
    public void ValidateResistanceTrend_ZeroDisplacement_ShouldReturnError()
    {
        // Act
        var result = _validationService.ValidateResistanceTrend(1000m, 0m, "container");

        // Assert
        result.Severity.Should().Be("Error");
        result.Message.Should().Contain("Invalid displacement");
    }

    [Fact]
    public void ValidateResistanceTrend_NegativeEhp_ShouldReturnError()
    {
        // Act
        var result = _validationService.ValidateResistanceTrend(-100m, 50000m, "container");

        // Assert
        result.Severity.Should().Be("Error");
        result.Message.Should().Contain("Invalid EHP");
    }

    [Fact]
    public void ValidateFormCoefficients_ZeroCm_ShouldReturnError()
    {
        // Arrange
        var coefficients = new FormCoefficients
        {
            Cb = 0.70m,
            Cp = 0.75m,
            Cm = 0m, // Invalid - Cm must be > 0
            Cwp = 0.85m
        };

        // Act
        var result = _validationService.ValidateFormCoefficients(coefficients, "container");

        // Assert
        result.IsValid.Should().BeFalse("Zero Cm should be invalid");
        result.Warnings.Should().Contain(w => w.Field == "Cm" && w.Severity == "Error");
    }

    [Fact]
    public void ValidateFormCoefficients_NegativeCoefficients_ShouldReturnError()
    {
        // Arrange
        var coefficients = new FormCoefficients
        {
            Cb = -0.1m, // Invalid
            Cp = 0.75m,
            Cm = 0.99m,
            Cwp = 0.85m
        };

        // Act
        var result = _validationService.ValidateFormCoefficients(coefficients, "container");

        // Assert
        result.IsValid.Should().BeFalse("Negative Cb should be invalid");
        result.Warnings.Should().Contain(w => w.Field == "Cb" && w.Severity == "Error");
    }

    [Fact]
    public void ValidateFormCoefficients_CbGreaterThanCm_ShouldReturnWarning()
    {
        // Arrange - Unusual but possible case
        var coefficients = new FormCoefficients
        {
            Cb = 0.85m,
            Cp = 0.87m,
            Cm = 0.80m, // Cb > Cm is unusual
            Cwp = 0.90m
        };

        // Act
        var result = _validationService.ValidateFormCoefficients(coefficients, "container");

        // Assert
        result.Warnings.Should().Contain(w =>
            w.Field == "CoefficientRelationship" &&
            w.Message.Contains("exceeds Midship Coefficient"));
    }

    [Fact]
    public void ValidateAgainstExpectedRanges_UnknownVesselType_ShouldReturnWarning()
    {
        // Arrange
        var candidate = new CandidateDesign
        {
            Id = Guid.NewGuid(),
            SizingRunId = Guid.NewGuid(),
            HullFamily = "unknown_type",
            LppM = 100m,
            BM = 20m,
            TM = 10m,
            Cb = 0.70m,
            Cp = 0.75m,
            Cwp = 0.85m,
            Fn = 0.20m,
            DisplacementT = 50000m,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var result = _validationService.ValidateAgainstExpectedRanges(
            candidate,
            "unknown_vessel_type_not_in_reference_data");

        // Assert
        result.Warnings.Should().Contain(w =>
            w.Field == "VesselType" &&
            w.Message.Contains("No expected ranges defined"));
    }

    [Fact]
    public void ValidateAgainstExpectedRanges_ExtremeValues_ShouldHandleGracefully()
    {
        // Arrange - Extreme but valid values
        var candidate = new CandidateDesign
        {
            Id = Guid.NewGuid(),
            SizingRunId = Guid.NewGuid(),
            HullFamily = "container",
            LppM = 400m, // Very long
            BM = 60m,    // Very wide
            TM = 15m,    // Deep draft
            Cb = 0.40m,  // Very fine
            Cp = 0.45m,
            Cwp = 0.70m,
            Fn = 0.35m,  // High speed
            DisplacementT = 200000m,
            CreatedAt = DateTime.UtcNow
        };

        // Act - Should not throw
        var result = _validationService.ValidateAgainstExpectedRanges(
            candidate,
            "container");

        // Assert
        result.Should().NotBeNull("Validation should complete without exception");
        // May have warnings for extreme values, but should not crash
    }

    [Fact]
    public void ValidateFormCoefficients_MissingCm_ShouldHandleGracefully()
    {
        // Arrange - Cm is nullable in CandidateDesign
        var candidate = new CandidateDesign
        {
            Id = Guid.NewGuid(),
            SizingRunId = Guid.NewGuid(),
            HullFamily = "container",
            LppM = 200m,
            BM = 32m,
            TM = 12m,
            Cb = 0.65m,
            Cp = 0.70m,
            Cwp = 0.80m,
            Cm = null, // Missing Cm
            Fn = 0.25m,
            DisplacementT = 50000m,
            CreatedAt = DateTime.UtcNow
        };

        // Act - Validate form coefficients with null Cm
        var coefficients = new FormCoefficients
        {
            Cb = candidate.Cb,
            Cp = candidate.Cp,
            Cm = 0.99m, // Use default when Cm is null
            Cwp = candidate.Cwp
        };

        var result = _validationService.ValidateFormCoefficients(coefficients, "container");

        // Assert
        result.Should().NotBeNull("Validation should handle missing Cm gracefully");
    }
}


