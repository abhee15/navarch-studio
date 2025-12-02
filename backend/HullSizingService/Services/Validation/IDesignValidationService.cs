using Shared.Models.Sizing;
using Shared.TestData;

namespace HullSizingService.Services.Validation;

/// <summary>
/// Service for validating candidate designs against expected ranges and reference data.
///
/// This service provides validation checks for:
/// - Block Coefficient (Cb) ranges per vessel type
/// - Froude Number validation
/// - Principal dimensions (Lpp, Beam, Draft)
/// - Alexander Limit compliance (Cb vs Fn relationship)
/// - Resistance/EHP trends
/// - Form coefficient relationships
/// </summary>
public interface IDesignValidationService
{
    /// <summary>
    /// Validates a candidate design against expected ranges for its vessel type.
    ///
    /// Checks:
    /// - Cb within expected range
    /// - Froude Number within expected range
    /// - Principal dimensions (Lpp, Beam) within expected ranges
    /// - Form coefficients relationships
    /// </summary>
    /// <param name="candidate">Candidate design to validate</param>
    /// <param name="vesselType">Vessel type/subtype for range lookup</param>
    /// <param name="toleranceConfig">Optional tolerance configuration (uses defaults if null)</param>
    /// <returns>Validation result with warnings and errors</returns>
    DesignValidationResult ValidateAgainstExpectedRanges(
        CandidateDesign candidate,
        string vesselType,
        ValidationToleranceConfig? toleranceConfig = null);

    /// <summary>
    /// Validates that Block Coefficient doesn't exceed maximum efficient value for given Froude Number (Alexander Limit).
    ///
    /// The Alexander Limit states: faster ships must be thinner (lower Cb).
    /// Designs that violate this are inefficient or physically impossible.
    /// </summary>
    /// <param name="froudeNumber">Froude Number of the design</param>
    /// <param name="blockCoefficient">Block Coefficient of the design</param>
    /// <returns>Validation result with margin to limit and severity level</returns>
    AlexanderLimitValidationResult ValidateAlexanderLimit(
        decimal froudeNumber,
        decimal blockCoefficient);

    /// <summary>
    /// Validates resistance/EHP trend against expected trend for vessel type.
    ///
    /// Categorizes EHP as:
    /// - Low: Primarily frictional drag (bulk carriers, tankers at low speed)
    /// - Moderate: Transitioning to wave drag (general cargo at moderate speed)
    /// - High: Primarily wave drag (fast container ships)
    /// </summary>
    /// <param name="ehpKw">Effective Horsepower in kilowatts</param>
    /// <param name="displacementTonnes">Displacement in tonnes</param>
    /// <param name="vesselType">Vessel type for trend comparison</param>
    /// <returns>Validation result with trend categorization and comparison</returns>
    ResistanceTrendValidationResult ValidateResistanceTrend(
        decimal ehpKw,
        decimal displacementTonnes,
        string vesselType);

    /// <summary>
    /// Validates form coefficients (Cb, Cp, Cm, Cwp) against expected ranges and relationships.
    ///
    /// Checks:
    /// - Physical relationships (e.g., Cp = Cb/Cm)
    /// - Vessel-type-specific ranges
    /// - Midship coefficient for specific hull forms (e.g., Cm ≈ 0.99 for full-form)
    /// </summary>
    /// <param name="coefficients">Form coefficients to validate</param>
    /// <param name="vesselType">Vessel type for range lookup</param>
    /// <returns>Validation result with coefficient checks</returns>
    FormCoefficientValidationResult ValidateFormCoefficients(
        FormCoefficients coefficients,
        string vesselType);
}

/// <summary>
/// Tolerance configuration for validation checks
/// </summary>
public class ValidationToleranceConfig
{
    /// <summary>Cb tolerance (default: ±0.05 for standard cases, ±0.02 for calibration)</summary>
    public decimal CbTolerance { get; set; } = 0.05m;

    /// <summary>Froude Number tolerance (default: ±0.01)</summary>
    public decimal FnTolerance { get; set; } = 0.01m;

    /// <summary>Dimension tolerance as percentage (default: ±5%)</summary>
    public decimal DimensionTolerancePercent { get; set; } = 5.0m;

    /// <summary>Resistance coefficient tolerance (default: ±0.05 to ±0.10 depending on speed)</summary>
    public decimal ResistanceTolerance { get; set; } = 0.05m;
}

/// <summary>
/// Form coefficients for validation
/// </summary>
public class FormCoefficients
{
    public decimal Cb { get; init; } // Block coefficient
    public decimal Cp { get; init; } // Prismatic coefficient
    public decimal Cm { get; init; } // Midship coefficient
    public decimal Cwp { get; init; } // Waterplane coefficient
}


/// <summary>
/// Design validation result
/// </summary>
public class DesignValidationResult
{
    public bool IsValid { get; set; }
    public List<ValidationWarning> Warnings { get; set; } = new();
    public List<ValidationError> Errors { get; set; } = new();
    public Dictionary<string, ComparisonData> Comparisons { get; set; } = new();
}

/// <summary>
/// Alexander Limit validation result
/// </summary>
public class AlexanderLimitValidationResult
{
    public bool ViolatesLimit { get; init; }
    public decimal FroudeNumber { get; init; }
    public decimal BlockCoefficient { get; init; }
    public decimal MaxEfficientCb { get; init; }
    public decimal MarginPercent { get; init; }
    public string Severity { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Resistance trend validation result
/// </summary>
public class ResistanceTrendValidationResult
{
    public string TrendCategory { get; init; } = string.Empty; // "Low", "Moderate", "High"
    public decimal EhpKw { get; init; }
    public decimal EhpPerTonne { get; init; }
    public string ExpectedTrend { get; init; } = string.Empty;
    public bool MatchesExpected { get; init; }
    public string Severity { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Form coefficient validation result
/// </summary>
public class FormCoefficientValidationResult
{
    public bool IsValid { get; set; }
    public List<CoefficientCheck> Checks { get; set; } = new();
    public List<ValidationWarning> Warnings { get; set; } = new();
}

/// <summary>
/// Validation warning
/// </summary>
public class ValidationWarning
{
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Severity { get; init; } = "Warning";
}

/// <summary>
/// Validation error
/// </summary>
public class ValidationError
{
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Comparison data (expected vs actual)
/// </summary>
public class ComparisonData
{
    public string Field { get; init; } = string.Empty;
    public decimal? ExpectedMin { get; init; }
    public decimal? ExpectedMax { get; init; }
    public decimal? ExpectedMean { get; init; }
    public decimal Actual { get; init; }
    public bool IsWithinRange { get; init; }
    public decimal? DeviationPercent { get; init; }
}

/// <summary>
/// Individual coefficient check result
/// </summary>
public class CoefficientCheck
{
    public string Coefficient { get; init; } = string.Empty; // "Cb", "Cp", "Cm", "Cwp"
    public decimal Value { get; init; }
    public decimal? ExpectedMin { get; init; }
    public decimal? ExpectedMax { get; init; }
    public bool IsWithinRange { get; init; }
    public bool RelationshipValid { get; init; } // e.g., Cp = Cb/Cm
    public string? RelationshipError { get; init; }
}
