namespace Shared.DTOs;

/// <summary>
/// Base class for DTOs that support unit conversion
/// </summary>
public abstract class UnitAwareDto
{
    /// <summary>
    /// Unit system for all values in this DTO (e.g., "SI", "Imperial")
    /// Indicates what units the values are currently in
    /// </summary>
    public string Units { get; set; } = "SI";
}

