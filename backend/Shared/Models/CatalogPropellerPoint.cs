using System;

namespace Shared.Models;

/// <summary>
/// Open-water test point for a propeller series
/// Defines thrust, torque, and efficiency at a given advance coefficient
/// </summary>
public class CatalogPropellerPoint
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SeriesId { get; set; }

    /// <summary>
    /// Advance coefficient: J = V / (n * D)
    /// </summary>
    public decimal J { get; set; }

    /// <summary>
    /// Thrust coefficient: KT = T / (ρ * n² * D⁴)
    /// </summary>
    public decimal Kt { get; set; }

    /// <summary>
    /// Torque coefficient: KQ = Q / (ρ * n² * D⁵)
    /// </summary>
    public decimal Kq { get; set; }

    /// <summary>
    /// Open-water efficiency: η₀ = (J / 2π) * (KT / KQ)
    /// </summary>
    public decimal Eta0 { get; set; }

    /// <summary>
    /// Reynolds number (optional, for scale effects)
    /// </summary>
    public decimal? ReynoldsNumber { get; set; }

    // Navigation properties
    public CatalogPropellerSeries? Series { get; set; }
}
