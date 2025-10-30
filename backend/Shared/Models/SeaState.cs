namespace Shared.Models;

/// <summary>
/// Represents sea state parameters for seakeeping analysis
/// </summary>
public class SeaState
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VesselId { get; set; }
    public Vessel Vessel { get; set; } = null!;

    /// <summary>
    /// Sea state name (e.g., "Beaufort 5", "Significant Wave 2m")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Significant wave height (m)
    /// </summary>
    public decimal WaveHeight { get; set; }

    /// <summary>
    /// Wave period (s)
    /// </summary>
    public decimal WavePeriod { get; set; }

    /// <summary>
    /// Wave direction relative to heading (degrees)
    /// 0 = following seas, 90 = beam seas, 180 = head seas
    /// </summary>
    public decimal WaveDirection { get; set; }

    /// <summary>
    /// Wind speed (m/s)
    /// </summary>
    public decimal? WindSpeed { get; set; }

    /// <summary>
    /// Wind direction relative to heading (degrees)
    /// </summary>
    public decimal? WindDirection { get; set; }

    /// <summary>
    /// Water depth (m) - null for deep water
    /// </summary>
    public decimal? WaterDepth { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
