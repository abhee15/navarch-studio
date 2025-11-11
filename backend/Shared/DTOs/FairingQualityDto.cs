namespace Shared.DTOs;

/// <summary>
/// Contains fairing quality analysis for all stations
/// </summary>
public record FairingQualityDto
{
    /// <summary>
    /// Quality analysis for each station
    /// </summary>
    public List<StationQualityDto> StationQualities { get; init; } = new();

    /// <summary>
    /// Overall quality score (0-100, where 100 is perfectly smooth)
    /// </summary>
    public decimal OverallScore { get; init; }
}

/// <summary>
/// Fairing quality analysis for a single station
/// </summary>
public record StationQualityDto
{
    /// <summary>
    /// Station index
    /// </summary>
    public int StationIndex { get; init; }

    /// <summary>
    /// Quality score for this station (0-100)
    /// </summary>
    public decimal Score { get; init; }

    /// <summary>
    /// Quality level classification
    /// </summary>
    public string QualityLevel { get; init; } = "Good"; // "Good", "Caution", "Issue"

    /// <summary>
    /// Regions with fairing issues
    /// </summary>
    public List<FlaggedRegionDto> FlaggedRegions { get; init; } = new();
}

/// <summary>
/// Represents a region with potential fairing issues
/// </summary>
public record FlaggedRegionDto
{
    /// <summary>
    /// Start Z coordinate of flagged region
    /// </summary>
    public decimal StartZ { get; init; }

    /// <summary>
    /// End Z coordinate of flagged region
    /// </summary>
    public decimal EndZ { get; init; }

    /// <summary>
    /// Maximum curvature change in this region
    /// </summary>
    public decimal MaxCurvatureChange { get; init; }

    /// <summary>
    /// Severity level
    /// </summary>
    public string Severity { get; init; } = "Low"; // "Low", "Medium", "High"
}
