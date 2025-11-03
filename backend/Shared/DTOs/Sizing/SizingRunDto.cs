namespace Shared.DTOs.Sizing;

/// <summary>
/// DTO for sizing run response
/// </summary>
public record SizingRunDto
{
    public Guid Id { get; init; }
    public Guid MissionCaseId { get; init; }
    public string Mode { get; init; } = string.Empty;
    public string? LocksJson { get; init; }
    public string? OptionsJson { get; init; }
    public string RunStatus { get; init; } = string.Empty;
    public int? ComputeTimeMs { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; }

    // Include candidate count summary
    public int CandidateCount { get; init; }
}

/// <summary>
/// DTO for creating a sizing run
/// </summary>
public record CreateSizingRunDto
{
    public Guid MissionCaseId { get; init; }
    public string Mode { get; init; } = "first_principles";
    public SizingLocksDto? Locks { get; init; }
    public SizingOptionsDto? Options { get; init; }
}

/// <summary>
/// Locks for parameters during solving
/// </summary>
public record SizingLocksDto
{
    public bool KeepFn { get; init; }
    public bool KeepLOverB { get; init; }
    public bool KeepBOverT { get; init; }
    public bool KeepDOverT { get; init; }
    public bool KeepCbBand { get; init; }
}

/// <summary>
/// Options for solver
/// </summary>
public record SizingOptionsDto
{
    public List<string>? FamilyHints { get; init; }
    public int MaxCandidates { get; init; } = 5;
    public decimal? MinFn { get; init; }
    public decimal? MaxFn { get; init; }
    public bool IncludeGeometry { get; init; } = false;
}

