using Shared.DTOs.ShipD;

namespace HullSizingService.Services.Validation;

/// <summary>
/// Service for validating ShipD parameter constraints.
///
/// Validates constraints from HullParameterization_ConstraintsOverview.pdf to ensure
/// watertight mesh generation and prevent geometry failures.
/// </summary>
public interface IShipDConstraintValidationService
{
    /// <summary>
    /// Validates Constraint 0: Lb + Ls < 1.0
    ///
    /// The sum of normalized bow and stern lengths must be less than 1 to ensure
    /// a positive midship length (Lm = 1 - Lb - Ls > 0).
    /// Violation causes hull self-intersection.
    /// </summary>
    /// <param name="bowLengthRatio">Normalized bow length ratio (Lb, typically 0.2-0.4)</param>
    /// <param name="sternLengthRatio">Normalized stern length ratio (Ls, typically 0.2-0.4)</param>
    /// <returns>Validation result with violation status and fix suggestions</returns>
    ConstraintValidationResult ValidateLongitudinalProportions(
        decimal bowLengthRatio,
        decimal sternLengthRatio);

    /// <summary>
    /// Validates Constraint 6: Chine intersection is inboard
    ///
    /// If the mesh tears at the bilge, the chine is too far outboard.
    /// Fix: Increase Deadrise Angle or Reduce Beam at Chine (Bc).
    /// </summary>
    /// <param name="deadriseAngleDeg">Deadrise angle in degrees</param>
    /// <param name="beamAtChine">Beam at chine as ratio of max beam</param>
    /// <returns>Validation result</returns>
    ConstraintValidationResult ValidateChineIntersection(
        decimal deadriseAngleDeg,
        decimal beamAtChine);

    /// <summary>
    /// Validates Constraint 7: Keel/Chine radii intersection
    ///
    /// Keel radius intersection must be inboard of chine intersection.
    /// Used for Full-Form hulls (TC-A, Calibration Case).
    /// If the hull looks smooth but displacement is wrong, check these radii.
    /// </summary>
    /// <param name="keelRadius">Keel radius</param>
    /// <param name="chineRadius">Chine radius</param>
    /// <param name="deadriseAngleDeg">Deadrise angle in degrees</param>
    /// <returns>Validation result</returns>
    ConstraintValidationResult ValidateKeelChineRadii(
        decimal keelRadius,
        decimal chineRadius,
        decimal deadriseAngleDeg);

    /// <summary>
    /// Validates Constraint 38: Bulb beam is less than hull beam at Z-height
    ///
    /// If the bulb is impossible for the bow, it means it's wider than the ship's
    /// nose section at that water level. Fix: Reduce Bulb Width.
    /// </summary>
    /// <param name="bulbWidthRatio">Bulb width as ratio of max beam (Bbb)</param>
    /// <param name="bulbHeightRatio">Bulb height as ratio of draft (Hbb)</param>
    /// <param name="bowSectionWidthAtBulb">Bow section width at bulb height as ratio of max beam</param>
    /// <returns>Validation result</returns>
    ConstraintValidationResult ValidateBulbDimensions(
        decimal bulbWidthRatio,
        decimal bulbHeightRatio,
        decimal bowSectionWidthAtBulb);

    /// <summary>
    /// Validates all constraints for a ShipD parameter vector.
    /// </summary>
    /// <param name="parameters">ShipD parameter vector (45 parameters)</param>
    /// <param name="metadata">Parameter metadata for lookup</param>
    /// <returns>Aggregated validation result with all constraint violations</returns>
    AggregateConstraintValidationResult ValidateAllConstraints(
        decimal[] parameters,
        IReadOnlyList<ShipDParameterMetadataDto> metadata);
}

/// <summary>
/// Result of constraint validation
/// </summary>
public class ConstraintValidationResult
{
    public bool IsValid { get; init; }
    public string ConstraintName { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public List<string> FixSuggestions { get; init; } = new();
    public string Severity { get; init; } = "Error"; // "Error" or "Warning"
}

/// <summary>
/// Aggregated result of all constraint validations
/// </summary>
public class AggregateConstraintValidationResult
{
    public bool AllConstraintsValid { get; init; }
    public List<ConstraintValidationResult> Results { get; init; } = new();
    public int ErrorCount => Results.Count(r => r.Severity == "Error");
    public int WarningCount => Results.Count(r => r.Severity == "Warning");
}
