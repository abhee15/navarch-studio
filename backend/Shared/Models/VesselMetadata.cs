using System.ComponentModel.DataAnnotations;

namespace Shared.Models;

/// <summary>
/// Stores design-phase metadata for a vessel
/// </summary>
public class VesselMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the parent vessel
    /// </summary>
    public Guid VesselId { get; set; }

    /// <summary>
    /// Vessel type classification
    /// </summary>
    [MaxLength(100)]
    public string? VesselType { get; set; }

    /// <summary>
    /// Size classification
    /// </summary>
    [MaxLength(50)]
    public string? Size { get; set; }

    /// <summary>
    /// Block coefficient (Cb)
    /// </summary>
    public decimal? BlockCoefficient { get; set; }

    /// <summary>
    /// Hull family/form (Wigley, Series 60, NPL, Prismatic)
    /// </summary>
    [MaxLength(100)]
    public string? HullFamily { get; set; }

    /// <summary>
    /// ShipD taxonomy category stored alongside metadata for quick analytics (duplicated from vessel for backwards compat)
    /// </summary>
    [MaxLength(100)]
    public string? ShipdCategory { get; set; }

    /// <summary>
    /// ShipD taxonomy type slug
    /// </summary>
    [MaxLength(100)]
    public string? ShipdType { get; set; }

    /// <summary>
    /// ShipD taxonomy family mask version
    /// </summary>
    public int? ShipdMaskVersion { get; set; }

    // Form coefficients (from hull sizing)
    public decimal? PrismaticCoefficient { get; set; }
    public decimal? MidshipCoefficient { get; set; }
    public decimal? WaterplaneCoefficient { get; set; }

    // Additional dimensions (from hull sizing)
    public decimal? Lwl { get; set; } // Length waterline (m)
    public decimal? Loa { get; set; } // Length overall (m)
    public decimal? Depth { get; set; } // Depth (m)

    // Stability parameters (from hull sizing)
    public decimal? KbInitial { get; set; } // Initial vertical center of buoyancy (m)
    public decimal? LcbPctLpp { get; set; } // Longitudinal center of buoyancy (% of Lpp)
    public decimal? GmInitial { get; set; } // Initial metacentric height (m)

    // Propulsion data (from hull sizing)
    public decimal? EhpKw { get; set; } // Effective horsepower (kW)
    public decimal? ShpKw { get; set; } // Shaft horsepower (kW)
    public decimal? FroudeNumber { get; set; } // Froude number

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Vessel Vessel { get; set; } = null!;
}
