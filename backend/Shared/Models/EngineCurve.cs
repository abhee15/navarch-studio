namespace Shared.Models;

/// <summary>
/// Represents an engine performance curve
/// </summary>
public class EngineCurve
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid VesselId { get; set; }
    public Vessel Vessel { get; set; } = null!;

    /// <summary>
    /// Engine name/type (e.g., "Main Engine - MTU 4000")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Engine description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Number of engines (for power calculation)
    /// </summary>
    public int Quantity { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<EnginePoint> EnginePoints { get; set; } = new List<EnginePoint>();
}

/// <summary>
/// Represents a point on the engine curve
/// </summary>
public class EnginePoint
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid EngineCurveId { get; set; }
    public EngineCurve EngineCurve { get; set; } = null!;

    /// <summary>
    /// Engine RPM
    /// </summary>
    public decimal Rpm { get; set; }

    /// <summary>
    /// Engine power (kW)
    /// </summary>
    public decimal PowerKw { get; set; }

    /// <summary>
    /// Torque (Nm)
    /// </summary>
    public decimal? Torque { get; set; }

    /// <summary>
    /// Fuel consumption (L/h)
    /// </summary>
    public decimal? FuelConsumption { get; set; }

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; }
}
