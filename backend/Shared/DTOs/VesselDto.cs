using Shared.Attributes;

namespace Shared.DTOs;

/// <summary>
/// DTO for creating/updating a vessel
/// </summary>
public class VesselDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    [Convertible("Length")]
    public decimal Lpp { get; set; }

    [Convertible("Length")]
    public decimal Beam { get; set; }

    [Convertible("Length")]
    public decimal DesignDraft { get; set; }

    public string UnitsSystem { get; set; } = "SI";
}

/// <summary>
/// DTO for vessel details with geometry counts
/// </summary>
public class VesselDetailsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    [Convertible("Length")]
    public decimal Lpp { get; set; }

    [Convertible("Length")]
    public decimal Beam { get; set; }

    [Convertible("Length")]
    public decimal DesignDraft { get; set; }

    public string UnitsSystem { get; set; } = "SI";
    public int StationsCount { get; set; }
    public int WaterlinesCount { get; set; }
    public int OffsetsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

