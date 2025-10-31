using Shared.Attributes;

namespace Shared.DTOs;

/// <summary>
/// DTO for catalog water property anchor points
/// </summary>
public class CatalogWaterPropertyDto : UnitAwareDto
{
    public Guid Id { get; set; }
    public string Medium { get; set; } = string.Empty;  // "Fresh", "Sea"

    public decimal Temperature_C { get; set; }  // Temperature in Celsius (no conversion)

    public decimal Salinity_PSU { get; set; }  // Salinity in PSU (no conversion)

    [Convertible("Density")]
    public decimal Density { get; set; }  // Density (kg/m³)

    public decimal KinematicViscosity_m2s { get; set; }  // Kinematic viscosity (m²/s, no conversion)

    public string SourceRef { get; set; } = string.Empty;  // "ITTC 7.5-02-01-03"
    public DateTime RetrievedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for interpolated water properties at a specific temperature
/// </summary>
public class WaterPropertiesDto : UnitAwareDto
{
    public string Medium { get; set; } = string.Empty;
    public decimal Temperature_C { get; set; }
    public decimal Salinity_PSU { get; set; }

    [Convertible("Density")]
    public decimal Density { get; set; }  // kg/m³

    public decimal KinematicViscosity_m2s { get; set; }  // m²/s

    public bool IsInterpolated { get; set; }  // Whether value was interpolated or exact

    public string? SourceRef { get; set; }  // Reference for exact values
}
