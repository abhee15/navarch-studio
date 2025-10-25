namespace NavArch.UnitConversion.Models;

/// <summary>
/// Represents a complete unit system (e.g., SI, Imperial)
/// </summary>
public class UnitSystemDefinition
{
    public string Id { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public Dictionary<string, string> Names { get; set; } = new();
    public Dictionary<string, string> Descriptions { get; set; } = new();
    public Dictionary<string, CategoryDefinition> Categories { get; set; } = new();
}

/// <summary>
/// Represents a category of measurements (e.g., Length, Mass)
/// </summary>
public class CategoryDefinition
{
    public string Id { get; set; } = string.Empty;
    public Dictionary<string, string> Names { get; set; } = new();
    public List<UnitDefinition> Units { get; set; } = new();
}

/// <summary>
/// Represents a single unit of measurement
/// </summary>
public class UnitDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public bool IsBase { get; set; }
    public Dictionary<string, string> Names { get; set; } = new();
    public Dictionary<string, string> PluralNames { get; set; } = new();
    public decimal? ConversionFactor { get; set; }
    public string? BaseUnit { get; set; }
}

/// <summary>
/// Represents a conversion factor between two units
/// </summary>
public record ConversionFactor(string From, string To, decimal Factor);

/// <summary>
/// Information about a unit system for display purposes
/// </summary>
public record UnitSystemInfo(
    string Id,
    string Name,
    string Description,
    bool IsDefault,
    List<string> Categories);

/// <summary>
/// Information about a specific unit for display purposes
/// </summary>
public record UnitInfo(
    string Id,
    string Symbol,
    string Name,
    string PluralName,
    string Category,
    bool IsBase);

