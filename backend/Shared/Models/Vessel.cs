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

    // Navigation properties
    public ICollection<Station> Stations { get; set; } = new List<Station>();
    public ICollection<Waterline> Waterlines { get; set; } = new List<Waterline>();
    public ICollection<Offset> Offsets { get; set; } = new List<Offset>();
    public ICollection<Loadcase> Loadcases { get; set; } = new List<Loadcase>();
    public ICollection<HydroResult> HydroResults { get; set; } = new List<HydroResult>();
}

