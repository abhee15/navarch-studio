namespace Shared.Models.Sizing;

/// <summary>
/// User-defined mission requirements for hull sizing
/// </summary>
public class MissionCase
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User who created this mission case
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Tenant ID for multi-tenancy support
    /// </summary>
    public string TenantId { get; set; } = null!;

    /// <summary>
    /// User-friendly name for the mission
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Mission category: Commercial, Government, Pleasure
    /// </summary>
    public string? MissionCategory { get; set; }

    /// <summary>
    /// Mission type: container, tanker, bulk, fishing, yacht_disp, hsc_planing, etc.
    /// </summary>
    public string MissionType { get; set; } = null!;

    // Cargo Inputs

    /// <summary>
    /// Primary cargo input type: volume, weight, teu
    /// </summary>
    public string CargoBasis { get; set; } = null!;

    /// <summary>
    /// Cargo value (generic, for backward compatibility)
    /// </summary>
    public decimal? CargoValue { get; set; }

    /// <summary>
    /// Cargo volume in cubic meters
    /// </summary>
    public decimal? CargoVolumeM3 { get; set; }

    /// <summary>
    /// Cargo density in tonnes per cubic meter
    /// </summary>
    public decimal? CargoDensityTPerM3 { get; set; }

    /// <summary>
    /// TEU count for container vessels
    /// </summary>
    public int? TeuCount { get; set; }

    // Speed & Margins

    /// <summary>
    /// Service speed in knots
    /// </summary>
    public decimal ServiceSpeedKn { get; set; }

    /// <summary>
    /// Sea margin percentage (typically 0.15 = 15%)
    /// </summary>
    public decimal SeaMarginPct { get; set; } = 0.15m;

    /// <summary>
    /// Service margin percentage (typically 0.15 = 15%)
    /// </summary>
    public decimal ServiceMarginPct { get; set; } = 0.15m;

    // Environment

    /// <summary>
    /// Significant wave height in meters
    /// </summary>
    public decimal? EnvHsM { get; set; }

    /// <summary>
    /// Wave period in seconds (for wavelength overlay)
    /// </summary>
    public decimal? EnvTzS { get; set; }

    // Constraints

    /// <summary>
    /// Maximum length overall in meters
    /// </summary>
    public decimal? CapLoaM { get; set; }

    /// <summary>
    /// Maximum beam in meters
    /// </summary>
    public decimal? CapBeamM { get; set; }

    /// <summary>
    /// Maximum draft in meters
    /// </summary>
    public decimal? CapDraftM { get; set; }

    /// <summary>
    /// Maximum air draft in meters
    /// </summary>
    public decimal? CapAirdraftM { get; set; }

    /// <summary>
    /// Endurance/range in nautical miles
    /// </summary>
    public decimal? EnduranceNm { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }

    // Metadata

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Soft delete timestamp
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    // Navigation Properties

    /// <summary>
    /// Sizing runs associated with this mission case
    /// </summary>
    public ICollection<SizingRun> SizingRuns { get; set; } = new List<SizingRun>();
}








