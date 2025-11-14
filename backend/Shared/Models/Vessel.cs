using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models;

/// <summary>
/// Represents a vessel (ship or marine structure) for hydrostatic analysis
/// </summary>
public class Vessel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// User who owns this vessel
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Vessel name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description or notes about the vessel
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Length between perpendiculars (m)
    /// </summary>
    public decimal Lpp { get; set; }

    /// <summary>
    /// Maximum breadth/beam (m)
    /// </summary>
    public decimal Beam { get; set; }

    /// <summary>
    /// Design draft (m)
    /// NOTE: All vessel dimensions stored in SI units (meters, kg, etc.)
    /// User's preferred unit system is stored in User.PreferredUnits
    /// </summary>
    public decimal DesignDraft { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Source catalog hull ID if this vessel was cloned from catalog
    /// </summary>
    public Guid? SourceCatalogHullId { get; set; }

    /// <summary>
    /// ShipD taxonomy category (Commercial, Government, etc.)
    /// </summary>
    [MaxLength(100)]
    public string? ShipdCategory { get; set; }

    /// <summary>
    /// ShipD taxonomy vessel type slug
    /// </summary>
    [MaxLength(100)]
    public string? ShipdType { get; set; }

    /// <summary>
    /// Human-readable ShipD vessel display name
    /// </summary>
    [MaxLength(150)]
    public string? ShipdTypeDisplayName { get; set; }

    /// <summary>
    /// ShipD bow family identifier
    /// </summary>
    [MaxLength(100)]
    public string? ShipdBowFamily { get; set; }

    /// <summary>
    /// ShipD midship family identifier
    /// </summary>
    [MaxLength(100)]
    public string? ShipdMidshipFamily { get; set; }

    /// <summary>
    /// ShipD stern family identifier
    /// </summary>
    [MaxLength(100)]
    public string? ShipdSternFamily { get; set; }

    /// <summary>
    /// ShipD family mask version
    /// </summary>
    public int? ShipdMaskVersion { get; set; }

    /// <summary>
    /// Serialized ShipD parameter vector snapshot (jsonb)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? ShipdParametersJson { get; set; }

    /// <summary>
    /// Hull-sizing candidate that produced this vessel
    /// </summary>
    public Guid? OriginCandidateId { get; set; }

    /// <summary>
    /// Hull-sizing run that produced the candidate
    /// </summary>
    public Guid? OriginSizingRunId { get; set; }

    /// <summary>
    /// Hull-sizing mission case
    /// </summary>
    public Guid? OriginMissionCaseId { get; set; }

    /// <summary>
    /// Upstream system identifier ("HullSizingService", etc.)
    /// </summary>
    [MaxLength(50)]
    public string? OriginSystem { get; set; }

    [MaxLength(200)]
    public string? OriginDesignName { get; set; }

    [MaxLength(200)]
    public string? OriginMissionName { get; set; }

    [MaxLength(200)]
    public string? OriginRunName { get; set; }

    public DateTime? OriginCreatedAt { get; set; }

    public DateTime? PushedToHydrostaticsAt { get; set; }

    public Guid? OriginUserId { get; set; }

    [MaxLength(200)]
    public string? OriginUserDisplayName { get; set; }

    [MaxLength(200)]
    public string? OriginIdempotencyKey { get; set; }

    // Navigation properties
    public ICollection<Station> Stations { get; set; } = new List<Station>();
    public ICollection<Waterline> Waterlines { get; set; } = new List<Waterline>();
    public ICollection<Offset> Offsets { get; set; } = new List<Offset>();
    public ICollection<Loadcase> Loadcases { get; set; } = new List<Loadcase>();
    public ICollection<HydroResult> HydroResults { get; set; } = new List<HydroResult>();

    // Design-phase metadata (one-to-one relationships)
    public VesselMetadata? Metadata { get; set; }
    public MaterialsConfig? Materials { get; set; }
    public LoadingConditions? Loading { get; set; }

    // Additional datasets
    public ICollection<SpeedGrid> SpeedGrids { get; set; } = new List<SpeedGrid>();
    public ICollection<EngineCurve> EngineCurves { get; set; } = new List<EngineCurve>();
    public ICollection<SeaState> SeaStates { get; set; } = new List<SeaState>();

    /// <summary>
    /// Version notes/history for this vessel
    /// </summary>
    public string? VersionNotes { get; set; }
}
