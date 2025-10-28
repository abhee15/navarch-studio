using Shared.Attributes;

namespace Shared.DTOs;

/// <summary>
/// Single point on a GZ or KN curve
/// </summary>
public record StabilityPointDto
{
    /// <summary>
    /// Heel angle in degrees
    /// </summary>
    public decimal HeelAngle { get; init; }

    /// <summary>
    /// Righting arm GZ in meters
    /// </summary>
    [Convertible("Length")]
    public decimal GZ { get; init; }

    /// <summary>
    /// Righting arm kernel KN in meters
    /// </summary>
    [Convertible("Length")]
    public decimal KN { get; init; }

    /// <summary>
    /// Metacentric height at this angle (optional)
    /// </summary>
    [Convertible("Length")]
    public decimal? GMAtAngle { get; init; }
}

/// <summary>
/// Complete stability curve with metadata
/// </summary>
public class StabilityCurveDto : UnitAwareDto
{
    /// <summary>
    /// Calculation method used
    /// </summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>
    /// Vessel displacement in kg
    /// </summary>
    [Convertible("Mass")]
    public decimal Displacement { get; set; }

    /// <summary>
    /// Center of gravity height KG in meters
    /// </summary>
    [Convertible("Length")]
    public decimal KG { get; set; }

    /// <summary>
    /// Initial GMT in meters
    /// </summary>
    [Convertible("Length")]
    public decimal InitialGMT { get; set; }

    /// <summary>
    /// Draft in meters
    /// </summary>
    [Convertible("Length")]
    public decimal Draft { get; set; }

    /// <summary>
    /// Curve points
    /// </summary>
    public List<StabilityPointDto> Points { get; set; } = new();

    /// <summary>
    /// Maximum GZ value
    /// </summary>
    [Convertible("Length")]
    public decimal MaxGZ { get; set; }

    /// <summary>
    /// Angle at which maximum GZ occurs (degrees)
    /// </summary>
    public decimal AngleAtMaxGZ { get; set; }

    /// <summary>
    /// Computation time in milliseconds
    /// </summary>
    public int ComputationTimeMs { get; set; }
}

/// <summary>
/// Single stability criterion check result
/// </summary>
public record StabilityCriterionDto
{
    /// <summary>
    /// Criterion name/description
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Required value
    /// </summary>
    public decimal RequiredValue { get; init; }

    /// <summary>
    /// Actual computed value
    /// </summary>
    public decimal ActualValue { get; init; }

    /// <summary>
    /// Unit of measurement
    /// </summary>
    public string Unit { get; init; } = string.Empty;

    /// <summary>
    /// Whether criterion is satisfied
    /// </summary>
    public bool Passed { get; init; }

    /// <summary>
    /// Additional notes or warnings
    /// </summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Complete stability criteria check results
/// </summary>
public class StabilityCriteriaResultDto : UnitAwareDto
{
    /// <summary>
    /// Overall pass/fail status
    /// </summary>
    public bool AllCriteriaPassed { get; set; }

    /// <summary>
    /// Individual criterion results
    /// </summary>
    public List<StabilityCriterionDto> Criteria { get; set; } = new();

    /// <summary>
    /// Standard/code used for criteria
    /// </summary>
    public string Standard { get; set; } = "IMO A.749(18)";

    /// <summary>
    /// Summary message
    /// </summary>
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Request to compute GZ/KN curve
/// </summary>
public record StabilityRequestDto
{
    /// <summary>
    /// Loadcase ID (required for KG)
    /// </summary>
    public Guid LoadcaseId { get; init; }

    /// <summary>
    /// Minimum heel angle (degrees)
    /// </summary>
    public decimal MinAngle { get; init; } = 0m;

    /// <summary>
    /// Maximum heel angle (degrees)
    /// </summary>
    public decimal MaxAngle { get; init; } = 90m;

    /// <summary>
    /// Angle increment (degrees)
    /// </summary>
    public decimal AngleIncrement { get; init; } = 1m;

    /// <summary>
    /// Calculation method: "WallSided" or "FullImmersion"
    /// </summary>
    public string Method { get; init; } = "WallSided";

    /// <summary>
    /// Draft at which to compute stability (meters)
    /// </summary>
    [Convertible("Length")]
    public decimal? Draft { get; init; }
}

/// <summary>
/// Available stability calculation methods
/// </summary>
public record StabilityMethodDto
{
    /// <summary>
    /// Method identifier
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Recommended maximum angle (degrees)
    /// </summary>
    public decimal? MaxRecommendedAngle { get; init; }

    /// <summary>
    /// Typical computation time relative to fastest method
    /// </summary>
    public string ComputationSpeed { get; init; } = string.Empty;
}

