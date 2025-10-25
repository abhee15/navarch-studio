namespace Shared.DTOs;

public record TrimSolutionRequestDto
{
    public Guid LoadcaseId { get; init; }
    public decimal TargetDisplacement { get; init; }
}

public record TrimSolutionDto
{
    public decimal TargetDisplacement { get; init; }
    public decimal DraftAP { get; init; }
    public decimal DraftFP { get; init; }
    public decimal MeanDraft { get; init; }
    public decimal TrimAngle { get; init; }
    public decimal LCF { get; init; }
    public decimal MTC { get; init; }
    public bool Converged { get; init; }
    public int Iterations { get; init; }
}

