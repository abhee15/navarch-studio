namespace Shared.Models.Sizing;

/// <summary>
/// Sizing computation run with configuration and status tracking
/// </summary>
public class SizingRun
{
    public Guid Id { get; set; }
    public Guid MissionCaseId { get; set; }
    public string Mode { get; set; } = "first_principles";
    public string? LocksJson { get; set; }
    public string? OptionsJson { get; set; }
    public string Status { get; set; } = "pending";
    public int? ComputeTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public MissionCase MissionCase { get; set; } = null!;
    public ICollection<CandidateDesign> Candidates { get; set; } = new List<CandidateDesign>();
}

