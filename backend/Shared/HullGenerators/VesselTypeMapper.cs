using Shared.Models;

namespace Shared.HullGenerators;

/// <summary>
/// Maps vessel type identifiers (ShipdType, VesselType) to parent hull registry types
/// </summary>
public static class VesselTypeMapper
{
    /// <summary>
    /// Map ShipdType or VesselType to parent hull registry vessel type
    /// </summary>
    public static string? MapToRegistryType(string? shipdType, string? vesselType)
    {
        // Try ShipdType first, then VesselType
        var type = shipdType ?? vesselType;
        if (string.IsNullOrWhiteSpace(type))
            return null;

        // Normalize to lowercase for comparison
        var normalized = type.ToLowerInvariant().Trim();

        // Map common vessel type identifiers to registry types
        // Product Carrier / Tanker variations
        if (normalized.Contains("product") || normalized.Contains("product_carrier"))
            return "product_carrier";
        if (normalized.Contains("tanker") || normalized.Contains("oil_tanker") || normalized.Contains("crude"))
            return "tanker";

        // Bulk Carrier
        if (normalized.Contains("bulk") || normalized.Contains("bulk_carrier"))
            return "bulk_carrier";

        // Container
        if (normalized.Contains("container") || normalized.Contains("container_ship"))
            return "container";

        // General Cargo
        if (normalized.Contains("cargo") || normalized.Contains("general_cargo") || normalized.Contains("multi"))
            return "general_cargo";

        // RoRo
        if (normalized.Contains("roro") || normalized.Contains("ro-ro") || normalized.Contains("roll"))
            return "roro";

        // Fast Ferry
        if (normalized.Contains("ferry") && (normalized.Contains("fast") || normalized.Contains("high-speed")))
            return "fast_ferry";

        // Yacht
        if (normalized.Contains("yacht") || normalized.Contains("sailing"))
            return "yacht";

        // Cruise
        if (normalized.Contains("cruise") || normalized.Contains("passenger"))
            return "cruise";

        // If no match, return null (will use parametric fallback)
        return null;
    }

    /// <summary>
    /// Get vessel type from VesselMetadata or Vessel model
    /// </summary>
    public static string? GetVesselType(Vessel? vessel, VesselMetadata? metadata)
    {
        if (vessel == null && metadata == null)
            return null;

        // Try metadata first (more specific)
        if (metadata?.VesselType != null)
            return MapToRegistryType(null, metadata.VesselType);

        // Try ShipdType from vessel
        if (vessel?.ShipdType != null)
            return MapToRegistryType(vessel.ShipdType, null);

        return null;
    }
}
