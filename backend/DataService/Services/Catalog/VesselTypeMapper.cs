using Microsoft.Extensions.Logging;

namespace DataService.Services.Catalog;

/// <summary>
/// Maps between ShipD taxonomy vessel types and catalog vessel types
/// Handles normalization (spaces, underscores, case) and fuzzy matching
/// </summary>
public class VesselTypeMapper : IVesselTypeMapper
{
    private readonly ILogger<VesselTypeMapper> _logger;

    /// <summary>
    /// Mapping from ShipD taxonomy vessel type to catalog vessel types
    /// Multiple catalog types can map to the same ShipD type (handles variations)
    /// </summary>
    private static readonly Dictionary<string, List<string>> ShipDToCatalogMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Commercial vessel types
        { "container", new() { "Container", "Container ship", "Container vessel" } },
        { "bulk_carrier", new() { "Bulk carrier", "Bulk", "Bulkcarrier", "Bulk Carrier" } },
        { "tanker", new() { "Tanker", "Crude oil tanker", "Product tanker", "Oil tanker", "Tank ship" } },
        { "general_cargo", new() { "General cargo", "Cargo", "Cargo ship", "General cargo ship" } },
        { "fishing", new() { "Fishing", "Fishing vessel", "Trawler", "Fishing boat" } },
        { "l7g", new() { "LNG", "LNG carrier", "Liquefied natural gas carrier" } },
        { "cruise_vessel", new() { "Cruise ship", "Cruise", "Passenger ship", "Cruise liner" } },
        { "passenger_vessel", new() { "Passenger ship", "Ferry", "Passenger", "Passenger ferry" } },

        // Government vessel types
        { "cutters", new() { "Naval combatant", "Cutter", "Coast guard", "Coast guard cutter", "Patrol vessel" } },
        { "medical_ship", new() { "Medical ship", "Hospital ship", "Medical vessel" } },
        { "general_military", new() { "Naval combatant", "Warship", "Military", "Naval vessel", "Combatant" } },

        // Recreational vessel types
        { "yacht", new() { "Yacht", "Sailing yacht", "Motor yacht", "Pleasure craft" } },
        { "high_speed_craft", new() { "High speed craft", "HSC", "Fast ferry", "High-speed vessel" } },

        // Research vessel types
        { "research_vessel", new() { "Research vessel", "Research", "Survey ship", "Research ship", "Oceanographic vessel" } },
    };

    /// <summary>
    /// Reverse mapping: catalog vessel type -> ShipD taxonomy type
    /// Built from ShipDToCatalogMap for fast lookup
    /// </summary>
    private static readonly Dictionary<string, string> CatalogToShipDMap;

    static VesselTypeMapper()
    {
        // Build reverse mapping
        CatalogToShipDMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (shipdType, catalogTypes) in ShipDToCatalogMap)
        {
            foreach (var catalogType in catalogTypes)
            {
                // Use first mapping if multiple ShipD types map to same catalog type
                if (!CatalogToShipDMap.ContainsKey(catalogType))
                {
                    CatalogToShipDMap[catalogType] = shipdType;
                }
            }
        }
    }

    public VesselTypeMapper(ILogger<VesselTypeMapper> logger)
    {
        _logger = logger;
    }

    public List<string> MapToCatalogTypes(string shipdVesselType)
    {
        if (string.IsNullOrWhiteSpace(shipdVesselType))
        {
            return new List<string>();
        }

        // Direct lookup first
        if (ShipDToCatalogMap.TryGetValue(shipdVesselType, out var catalogTypes))
        {
            return catalogTypes;
        }

        // Try normalized lookup (handles case differences)
        var normalized = NormalizeVesselType(shipdVesselType);
        var matchingKey = ShipDToCatalogMap.Keys
            .FirstOrDefault(k => NormalizeVesselType(k) == normalized);

        if (matchingKey != null && ShipDToCatalogMap.TryGetValue(matchingKey, out catalogTypes))
        {
            _logger.LogDebug(
                "Mapped ShipD type '{ShipDType}' (normalized: '{Normalized}') to catalog types via fuzzy match",
                shipdVesselType, normalized);
            return catalogTypes;
        }

        // No mapping found - log for manual review
        _logger.LogWarning(
            "No catalog mapping found for ShipD vessel type '{ShipDType}'. " +
            "Consider adding mapping or using fuzzy matching.",
            shipdVesselType);

        return new List<string>();
    }

    public string? MapToShipDType(string catalogVesselType)
    {
        if (string.IsNullOrWhiteSpace(catalogVesselType))
        {
            return null;
        }

        // Direct lookup first
        if (CatalogToShipDMap.TryGetValue(catalogVesselType, out var shipdType))
        {
            return shipdType;
        }

        // Try normalized lookup
        var normalized = NormalizeVesselType(catalogVesselType);
        var matchingKey = CatalogToShipDMap.Keys
            .FirstOrDefault(k => NormalizeVesselType(k) == normalized);

        if (matchingKey != null && CatalogToShipDMap.TryGetValue(matchingKey, out shipdType))
        {
            _logger.LogDebug(
                "Mapped catalog type '{CatalogType}' (normalized: '{Normalized}') to ShipD type '{ShipDType}' via fuzzy match",
                catalogVesselType, normalized, shipdType);
            return shipdType;
        }

        // No mapping found
        _logger.LogDebug(
            "No ShipD mapping found for catalog vessel type '{CatalogType}'",
            catalogVesselType);

        return null!;
    }

    public string NormalizeVesselType(string vesselType)
    {
        if (string.IsNullOrWhiteSpace(vesselType))
        {
            return string.Empty;
        }

        // Convert to lowercase, remove spaces, underscores, hyphens, and special characters
        return vesselType
            .ToLowerInvariant()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Trim();
    }
}
