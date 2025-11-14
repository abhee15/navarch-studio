namespace Shared.Models.Sizing;

/// <summary>
/// Tracks "Push to Hydrostatics" operations for idempotency
/// </summary>
public class PushOperation
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Client-generated idempotency key to prevent duplicate vessel creation
    /// </summary>
    public string IdempotencyKey { get; set; } = null!;

    /// <summary>
    /// Candidate design that was pushed
    /// </summary>
    public Guid CandidateId { get; set; }

    /// <summary>
    /// Created vessel ID in data.vessels (DataService)
    /// </summary>
    public Guid VesselId { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    // Navigation Properties

    /// <summary>
    /// Source candidate design
    /// </summary>
    public CandidateDesign Candidate { get; set; } = null!;
}












