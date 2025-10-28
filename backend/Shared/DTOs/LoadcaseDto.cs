using Shared.Attributes;

namespace Shared.DTOs;

public class LoadcaseDto : UnitAwareDto
{
    public Guid? Id { get; set; }
    public Guid VesselId { get; set; }
    public string Name { get; set; } = string.Empty;

    [Convertible("Density")]
    public decimal Rho { get; set; } = 1025m;

    [Convertible("Length")]
    public decimal? KG { get; set; }

    public string? Notes { get; set; }
}

