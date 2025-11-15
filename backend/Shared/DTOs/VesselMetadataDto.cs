namespace Shared.DTOs;

/// <summary>
/// DTO for vessel design-phase metadata
/// </summary>
public class VesselMetadataDto
{
    public string? VesselType { get; set; }
    public string? Size { get; set; }
    public decimal? BlockCoefficient { get; set; }
    public string? HullFamily { get; set; }
    public string? ShipdCategory { get; set; }
    public string? ShipdType { get; set; }
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
}
