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
    /// Vessel category selected for this run (Commercial, Government, etc.).
    /// </summary>
    public string? VesselCategory { get; set; }

    /// <summary>
    /// Vessel type slug selected for this run (e.g., general_cargo, bulk_carrier).
    /// </summary>
    public string? VesselType { get; set; }

    /// <summary>
    /// Bow family identifier applied for ShipD parameterization.
    /// </summary>
    public string? BowFamily { get; set; }

    /// <summary>
    /// Midship family identifier.
    /// </summary>
    public string? MidshipFamily { get; set; }

    /// <summary>
    /// Stern family identifier.
    /// </summary>
    public string? SternFamily { get; set; }

    /// <summary>
    /// Version of the family mask used to derive ShipD parameters.
    /// </summary>
    public int? FamilyMaskVersion { get; set; }

    /// <summary>
    /// ShipD input vector captured for auditing (JSON array of 45 parameters).
    /// </summary>
    public string? ShipdInputVectorJson { get; set; }

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
    /// Solver diagnostics (JSON) - provides actionable feedback when 0 candidates generated
    /// Contains failure reasons, suggestions, and technical details
    /// </summary>
    public string? DiagnosticsJson { get; set; }

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
