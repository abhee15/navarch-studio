using Shared.Attributes;

namespace Shared.DTOs;

/// <summary>
/// DTO for creating a comparison snapshot
/// </summary>
public class CreateComparisonSnapshotDto
{
    public string RunName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsBaseline { get; set; } = false;
    public string? Tags { get; set; }

    public Guid? LoadcaseId { get; set; }

    [Convertible("Length")]
    public decimal MinDraft { get; set; }

    [Convertible("Length")]
    public decimal MaxDraft { get; set; }

    [Convertible("Length")]
    public decimal DraftStep { get; set; }

    /// <summary>
    /// Array of hydrostatic results to snapshot
    /// </summary>
    public List<HydroResultDto> Results { get; set; } = new();

    public int ComputationTimeMs { get; set; }
}

/// <summary>
/// DTO for comparison snapshot response
/// </summary>
public class ComparisonSnapshotDto : UnitAwareDto
{
    public Guid Id { get; set; }
    public Guid VesselId { get; set; }
    public string VesselName { get; set; } = string.Empty;
    public Guid? LoadcaseId { get; set; }
    public string? LoadcaseName { get; set; }

    public string RunName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsBaseline { get; set; }
    public string? Tags { get; set; }

    // Vessel snapshot
    [Convertible("Length")]
    public decimal VesselLpp { get; set; }

    [Convertible("Length")]
    public decimal VesselBeam { get; set; }

    [Convertible("Length")]
    public decimal VesselDesignDraft { get; set; }

    // Loadcase snapshot
    [Convertible("Density")]
    public decimal? LoadcaseRho { get; set; }

    [Convertible("Length")]
    public decimal? LoadcaseKG { get; set; }

    // Computation parameters
    [Convertible("Length")]
    public decimal MinDraft { get; set; }

    [Convertible("Length")]
    public decimal MaxDraft { get; set; }

    [Convertible("Length")]
    public decimal DraftStep { get; set; }

    // Results
    public List<HydroResultDto> Results { get; set; } = new();
    public int ComputationTimeMs { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for comparison request (baseline vs candidate)
/// </summary>
public class CompareRunsRequestDto
{
    public Guid BaselineSnapshotId { get; set; }
    public Guid CandidateSnapshotId { get; set; }
}

/// <summary>
/// DTO for a single KPI comparison with delta
/// </summary>
public class KpiComparisonDto
{
    public string KpiName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal? BaselineValue { get; set; }
    public decimal? CandidateValue { get; set; }
    public decimal? AbsoluteDelta { get; set; }
    public decimal? PercentDelta { get; set; }
    public string? Interpretation { get; set; } // "Better", "Worse", "Neutral"
}

/// <summary>
/// DTO for draft-by-draft comparison
/// </summary>
public class DraftComparisonDto
{
    public decimal Draft { get; set; }
    public List<KpiComparisonDto> KpiComparisons { get; set; } = new();
}

/// <summary>
/// DTO for full comparison response
/// </summary>
public class ComparisonReportDto : UnitAwareDto
{
    public ComparisonSnapshotDto Baseline { get; set; } = null!;
    public ComparisonSnapshotDto Candidate { get; set; } = null!;

    /// <summary>
    /// Summary-level KPI comparisons (e.g., at design draft)
    /// </summary>
    public List<KpiComparisonDto> SummaryComparisons { get; set; } = new();

    /// <summary>
    /// Draft-by-draft comparisons for detailed analysis
    /// </summary>
    public List<DraftComparisonDto> DraftComparisons { get; set; } = new();

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for updating snapshot (e.g., setting as baseline)
/// </summary>
public class UpdateComparisonSnapshotDto
{
    public string? RunName { get; set; }
    public string? Description { get; set; }
    public bool? IsBaseline { get; set; }
    public string? Tags { get; set; }
}
