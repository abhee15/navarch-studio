namespace Shared.Models.Sizing;

/// <summary>
/// Sizing computation run with configuration and status tracking
/// </summary>
public class SizingRun
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Mission case this run belongs to
    /// </summary>
    public Guid MissionCaseId { get; set; }

    /// <summary>
    /// Sizing mode: first_principles or data_driven
    /// </summary>
    public string Mode { get; set; } = "first_principles";

    /// <summary>
    /// Locked parameters during solver iterations (JSON)
    /// Example: {"keepFn": true, "keepLOverB": false, "keepBOverT": false}
    /// </summary>
    public string? LocksJson { get; set; }

    /// <summary>
    /// Solver options (JSON)
    /// Example: {"familyHint": "container", "maxCandidates": 5}
    /// </summary>
    public string? OptionsJson { get; set; }

    /// <summary>
    /// Execution status: pending, computing, completed, failed
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Computation time in milliseconds
    /// </summary>
    public int? ComputeTimeMs { get; set; }

    /// <summary>
    /// Error message if status is failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation Properties

    /// <summary>
    /// Parent mission case
    /// </summary>
    public MissionCase MissionCase { get; set; } = null!;

    /// <summary>
    /// Generated candidate designs
    /// </summary>
    public ICollection<CandidateDesign> Candidates { get; set; } = new List<CandidateDesign>();
}



