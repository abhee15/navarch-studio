using System;

namespace Shared.Models;

/// <summary>
/// Water property anchor points for interpolation (ITTC 7.5-02-01-03)
/// Supports linear interpolation for temperatures between 0-30°C
/// </summary>
public class CatalogWaterProperty
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Medium type: "Fresh" or "Sea"
    /// </summary>
    public required string Medium { get; set; }

    /// <summary>
    /// Temperature in Celsius (anchor points: 0, 15, 30)
    /// </summary>
    public decimal Temperature_C { get; set; }

    /// <summary>
    /// Salinity in PSU (0 for fresh, ~35 for seawater)
    /// </summary>
    public decimal Salinity_PSU { get; set; }

    /// <summary>
    /// Density in kg/m³
    /// </summary>
    public decimal Density_kgm3 { get; set; }

    /// <summary>
    /// Kinematic viscosity in m²/s
    /// </summary>
    public decimal KinematicViscosity_m2s { get; set; }

    /// <summary>
    /// Source reference (e.g., "ITTC 7.5-02-01-03 Table 1")
    /// </summary>
    public required string SourceRef { get; set; }

    /// <summary>
    /// Date data was retrieved/verified
    /// </summary>
    public DateTime RetrievedAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
