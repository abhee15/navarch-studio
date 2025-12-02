namespace HullSizingService.Services.Validation;

/// <summary>
/// Aggregated validation result combining all validation checks for a candidate design.
/// This is stored as JSON in CandidateDesign.ValidationResultsJson
/// </summary>
public class AggregateValidationResult
{
    /// <summary>
    /// Whether all validations passed (no errors)
    /// </summary>
    public bool AllValid { get; set; }

    /// <summary>
    /// Total count of validation errors
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Total count of validation warnings
    /// </summary>
    public int WarningCount { get; set; }

    /// <summary>
    /// Design validation result (ranges, dimensions, coefficients)
    /// </summary>
    public DesignValidationResult? DesignValidation { get; set; }

    /// <summary>
    /// Alexander Limit validation result
    /// </summary>
    public AlexanderLimitValidationResult? AlexanderLimitValidation { get; set; }

    /// <summary>
    /// Resistance trend validation result
    /// </summary>
    public ResistanceTrendValidationResult? ResistanceTrendValidation { get; set; }

    /// <summary>
    /// Form coefficient validation result
    /// </summary>
    public FormCoefficientValidationResult? FormCoefficientValidation { get; set; }

    /// <summary>
    /// ShipD constraint validation results
    /// </summary>
    public AggregateConstraintValidationResult? ConstraintValidation { get; set; }

    /// <summary>
    /// Timestamp when validation was performed
    /// </summary>
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}
