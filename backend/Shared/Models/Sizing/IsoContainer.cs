namespace Shared.Models.Sizing;

/// <summary>
/// ISO standard container types for TEU-based sizing
/// </summary>
public class IsoContainer
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Container type: 20GP, 40GP, 40HC, 45HC
    /// </summary>
    public string ContainerType { get; set; } = null!;

    /// <summary>
    /// External length (mm)
    /// </summary>
    public int LengthMm { get; set; }

    /// <summary>
    /// External width (mm)
    /// </summary>
    public int WidthMm { get; set; }

    /// <summary>
    /// External height (mm)
    /// </summary>
    public int HeightMm { get; set; }

    /// <summary>
    /// Maximum gross weight (kg)
    /// </summary>
    public int MaxGrossKg { get; set; }
}








