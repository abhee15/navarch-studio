namespace Shared.DTOs;

/// <summary>
/// DTO for creating/updating a vessel
/// </summary>
public record VesselDto
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Lpp { get; init; }
    public decimal Beam { get; init; }
    public decimal DesignDraft { get; init; }
    public string UnitsSystem { get; init; } = "SI";
}

/// <summary>
/// DTO for vessel details with geometry counts
/// </summary>
public record VesselDetailsDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Lpp { get; init; }
    public decimal Beam { get; init; }
    public decimal DesignDraft { get; init; }
    public string UnitsSystem { get; init; } = "SI";
    public int StationsCount { get; init; }
    public int WaterlinesCount { get; init; }
    public int OffsetsCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

