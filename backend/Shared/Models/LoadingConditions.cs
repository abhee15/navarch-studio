namespace Shared.Models;

/// <summary>
/// Stores loading condition estimates for a vessel
/// </summary>
public class LoadingConditions
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the parent vessel
    /// </summary>
    public Guid VesselId { get; set; }

    /// <summary>
    /// Lightship weight in tonnes
    /// </summary>
    public decimal? LightshipTonnes { get; set; }

    /// <summary>
    /// Deadweight in tonnes
    /// </summary>
    public decimal? DeadweightTonnes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public Vessel Vessel { get; set; } = null!;
}



