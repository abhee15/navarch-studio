namespace Shared.Models;

/// <summary>
/// Represents a snapshot of a hydrostatic computation run for comparison purposes
/// Stores both inputs and results to enable side-by-side comparison
/// </summary>
public class ComparisonSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User who created this snapshot
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Reference to the vessel
    /// </summary>
    public Guid VesselId { get; set; }
    public Vessel Vessel { get; set; } = null!;

    /// <summary>
    /// Reference to the loadcase used
    /// </summary>
    public Guid? LoadcaseId { get; set; }
    public Loadcase? Loadcase { get; set; }

    /// <summary>
    /// User-defined name for this run (e.g., "Baseline v1.0", "Optimized Hull")
    /// </summary>
    public string RunName { get; set; } = string.Empty;

    /// <summary>
    /// Optional description/notes about this run
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this snapshot is marked as a baseline for comparisons
    /// </summary>
    public bool IsBaseline { get; set; } = false;

    /// <summary>
    /// Snapshot of vessel principal particulars at time of computation
    /// </summary>
    public decimal VesselLpp { get; set; }
    public decimal VesselBeam { get; set; }
    public decimal VesselDesignDraft { get; set; }

    /// <summary>
    /// Snapshot of loadcase parameters at time of computation
    /// </summary>
    public decimal? LoadcaseRho { get; set; }
    public decimal? LoadcaseKG { get; set; }

    /// <summary>
    /// Computation parameters used
    /// </summary>
    public decimal MinDraft { get; set; }
    public decimal MaxDraft { get; set; }
    public decimal DraftStep { get; set; }

    /// <summary>
    /// JSON-serialized array of HydroResult data
    /// Stored as JSON to avoid creating additional tables while preserving full fidelity
    /// </summary>
    public string ResultsJson { get; set; } = "[]";

    /// <summary>
    /// Computation time in milliseconds
    /// </summary>
    public int ComputationTimeMs { get; set; }

    /// <summary>
    /// Metadata for tracking
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Tags for categorization (e.g., "iteration-1", "design-freeze")
    /// </summary>
    public string? Tags { get; set; }
}
