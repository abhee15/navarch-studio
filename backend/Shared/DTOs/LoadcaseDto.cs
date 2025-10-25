namespace Shared.DTOs;

public record LoadcaseDto
{
    public Guid? Id { get; init; }
    public Guid VesselId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Rho { get; init; } = 1025m;
    public decimal? KG { get; init; }
    public string? Notes { get; init; }
}

