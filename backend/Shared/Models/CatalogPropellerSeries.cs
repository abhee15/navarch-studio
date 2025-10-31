using System;
using System.Collections.Generic;

namespace Shared.Models;

/// <summary>
/// Propeller series (e.g., Wageningen B-series) with characteristics
/// </summary>
public class CatalogPropellerSeries
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Series name (e.g., "Wageningen B-Series")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Number of blades (Z: typically 3-7)
    /// </summary>
    public int BladeCount { get; set; }

    /// <summary>
    /// Expanded area ratio (AE/A0: typically 0.40-1.05)
    /// </summary>
    public decimal ExpandedAreaRatio { get; set; }

    /// <summary>
    /// Pitch-diameter ratio (P/D: optional, varies by design)
    /// </summary>
    public decimal? PitchDiameterRatio { get; set; }

    /// <summary>
    /// Source URL (e.g., Zenodo DOI)
    /// </summary>
    public string? SourceUrl { get; set; }

    /// <summary>
    /// License information
    /// </summary>
    public string? License { get; set; }

    /// <summary>
    /// Flag for demo/synthetic data (excluded from calculations)
    /// </summary>
    public bool IsDemo { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<CatalogPropellerPoint> OpenWaterPoints { get; set; } = new List<CatalogPropellerPoint>();
}
