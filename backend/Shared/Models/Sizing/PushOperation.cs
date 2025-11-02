namespace Shared.Models.Sizing;

public class PushOperation
{
    public Guid Id { get; set; }
    public string IdempotencyKey { get; set; } = null!;
    public Guid CandidateId { get; set; }
    public Guid VesselId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public CandidateDesign Candidate { get; set; } = null!;
}

