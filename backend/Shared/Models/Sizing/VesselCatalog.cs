namespace Shared.Models.Sizing;

/// <summary>
/// Reference vessels for data-driven mode (KCS, KVLCC2, Series 60, etc.)
/// </summary>
public class VesselCatalog
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Vessel name
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Data source: SIMMAN/NMRI (KCS), KRISO (KVLCC2), ITTC (Series 60), etc.
    /// </summary>
    public string? Provenance { get; set; }

    /// <summary>
    /// Vessel type: container, tanker, bulk, fishing, etc.
    /// </summary>
    public string? VesselType { get; set; }

    // Principal Dimensions

    /// <summary>
    /// Length between perpendiculars (m)
    /// </summary>
    public decimal? LppM { get; set; }

    /// <summary>
    /// Waterline length (m)
    /// </summary>
    public decimal? LwlM { get; set; }

    /// <summary>
    /// Beam (m)
    /// </summary>
    public decimal? BM { get; set; }

    /// <summary>
    /// Draft (m)
    /// </summary>
    public decimal? TM { get; set; }

    /// <summary>
    /// Depth (m)
    /// </summary>
    public decimal? DM { get; set; }

    // Form Coefficients

    /// <summary>
    /// Block coefficient
    /// </summary>
    public decimal? Cb { get; set; }

    /// <summary>
    /// Prismatic coefficient
    /// </summary>
    public decimal? Cp { get; set; }

    /// <summary>
    /// Waterplane coefficient
    /// </summary>
    public decimal? Cwp { get; set; }

    /// <summary>
    /// Midship coefficient
    /// </summary>
    public decimal? Cm { get; set; }

    // Capacity & Speed

    /// <summary>
    /// Deadweight tonnage
    /// </summary>
    public decimal? DwtT { get; set; }

    /// <summary>
    /// Service speed (kn)
    /// </summary>
    public decimal? ServiceSpeedKn { get; set; }

    // Provenance Tracking

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Source URL for data
    /// </summary>
    public string? SourceUrl { get; set; }

    /// <summary>
    /// License information
    /// </summary>
    public string? LicenseInfo { get; set; }
}






