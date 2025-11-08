using Shared.Models;

namespace NavArch.Shared.Models;

/// <summary>
/// Represents Response Amplitude Operator (RAO) calculation results for a vessel.
/// RAOs describe the motion response of a vessel to regular waves across different frequencies.
/// </summary>
public class RaoResult
{
    public Guid Id { get; set; }
    public Guid VesselId { get; set; }
    public Guid LoadcaseId { get; set; }

    /// <summary>
    /// Array of wave frequencies (rad/s) at which RAOs were calculated.
    /// </summary>
    public double[] Frequency { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Heave RAO values (m/m) - vertical motion response per unit wave amplitude.
    /// </summary>
    public double[] HeaveRao { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Pitch RAO values (rad/m) - pitch rotation response per unit wave amplitude.
    /// </summary>
    public double[] PitchRao { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Roll RAO values (rad/m) - roll rotation response per unit wave amplitude.
    /// </summary>
    public double[] RollRao { get; set; } = Array.Empty<double>();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; } // Soft delete

    // Navigation properties
    public Vessel Vessel { get; set; } = null!;
    public Loadcase Loadcase { get; set; } = null!;
}
