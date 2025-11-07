namespace Shared.DTOs.Sizing;

/// <summary>
/// DTO for design space exploration request
/// Sweeps through parameter ranges to generate multiple design variants
/// </summary>
public record DesignSpaceExplorationRequest
{
    public Guid MissionCaseId { get; init; }

    /// <summary>
    /// Parameter ranges to explore
    /// </summary>
    public ParameterRanges Ranges { get; init; } = new();

    /// <summary>
    /// Solver mode to use for all variants
    /// </summary>
    public string Mode { get; init; } = "first_principles";

    /// <summary>
    /// Hull family to use (or null for auto-selection)
    /// </summary>
    public string? HullFamily { get; init; }

    /// <summary>
    /// Maximum number of variants to generate (safety limit)
    /// </summary>
    public int MaxVariants { get; init; } = 100;
}

/// <summary>
/// Parameter ranges for exploration
/// </summary>
public record ParameterRanges
{
    // Length range
    public decimal? LppMinM { get; init; }
    public decimal? LppMaxM { get; init; }
    public int LppSteps { get; init; } = 5;

    // Beam range
    public decimal? BeamMinM { get; init; }
    public decimal? BeamMaxM { get; init; }
    public int BeamSteps { get; init; } = 5;

    // Draft range (optional)
    public decimal? DraftMinM { get; init; }
    public decimal? DraftMaxM { get; init; }
    public int DraftSteps { get; init; } = 1;

    // Speed range (optional)
    public decimal? SpeedMinKn { get; init; }
    public decimal? SpeedMaxKn { get; init; }
    public int SpeedSteps { get; init; } = 1;

    // Block coefficient range (optional)
    public decimal? CbMin { get; init; }
    public decimal? CbMax { get; init; }
    public int CbSteps { get; init; } = 1;
}

/// <summary>
/// Response for design space exploration
/// Returns a batch ID that can be used to track progress
/// </summary>
public record DesignSpaceExplorationResponse
{
    /// <summary>
    /// Batch ID for this exploration run (used to query results)
    /// </summary>
    public Guid BatchId { get; init; }

    /// <summary>
    /// List of sizing run IDs generated (one per variant)
    /// </summary>
    public List<Guid> SizingRunIds { get; init; } = new();

    /// <summary>
    /// Total number of variants requested
    /// </summary>
    public int TotalVariants { get; init; }

    /// <summary>
    /// Status of the exploration
    /// </summary>
    public string Status { get; init; } = "pending";

    /// <summary>
    /// Time taken (if completed)
    /// </summary>
    public int? ComputeTimeMs { get; init; }
}

/// <summary>
/// Summary of exploration results for display
/// </summary>
public record ExplorationResultsSummary
{
    public Guid BatchId { get; init; }
    public int TotalVariants { get; init; }
    public int CompletedVariants { get; init; }
    public int FailedVariants { get; init; }
    public string Status { get; init; } = "running";
    public List<CandidateDesignDto> Candidates { get; init; } = new();

    /// <summary>
    /// Pareto front analysis (optional enhancement)
    /// </summary>
    public ParetoAnalysis? ParetoAnalysis { get; init; }
}

/// <summary>
/// Pareto front analysis for multi-objective optimization
/// </summary>
public record ParetoAnalysis
{
    /// <summary>
    /// IDs of designs on the Pareto front
    /// </summary>
    public List<Guid> ParetoFrontIds { get; init; } = new();

    /// <summary>
    /// Objective space statistics
    /// </summary>
    public Dictionary<string, ObjectiveStats> ObjectiveStatistics { get; init; } = new();
}

/// <summary>
/// Statistics for an objective (e.g., power, displacement)
/// </summary>
public record ObjectiveStats
{
    public decimal Min { get; init; }
    public decimal Max { get; init; }
    public decimal Mean { get; init; }
    public decimal StdDev { get; init; }
}
