namespace DataService.Services.Catalog;

/// <summary>
/// Maps between ShipD taxonomy vessel types and catalog vessel types
/// Handles normalization (spaces, underscores, case) and fuzzy matching
/// </summary>
public interface IVesselTypeMapper
{
    /// <summary>
    /// Maps ShipD taxonomy vessel type to one or more catalog vessel type(s)
    /// Returns empty list if no mapping found
    /// </summary>
    /// <param name="shipdVesselType">ShipD taxonomy vessel type (e.g., "bulk_carrier", "container")</param>
    /// <returns>List of catalog vessel types that match (e.g., ["Bulk carrier", "Bulk"])</returns>
    List<string> MapToCatalogTypes(string shipdVesselType);

    /// <summary>
    /// Maps catalog vessel type to ShipD taxonomy vessel type
    /// Returns null if no mapping found
    /// </summary>
    /// <param name="catalogVesselType">Catalog vessel type (e.g., "Bulk carrier", "Container")</param>
    /// <returns>ShipD taxonomy vessel type (e.g., "bulk_carrier", "container") or null</returns>
    string? MapToShipDType(string catalogVesselType);

    /// <summary>
    /// Normalizes vessel type for comparison (handles spaces, underscores, case)
    /// Example: "Bulk carrier" -> "bulkcarrier", "bulk_carrier" -> "bulkcarrier"
    /// </summary>
    /// <param name="vesselType">Vessel type to normalize</param>
    /// <returns>Normalized string (lowercase, no spaces/underscores)</returns>
    string NormalizeVesselType(string vesselType);
}

