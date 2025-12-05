using Shared.DTOs.ShipD;

namespace HullSizingService.Services.ShipD;

/// <summary>
/// Service for mapping vessel types to default hull shape parameters.
/// Provides automatic selection of hull families and geometry parameters
/// based on vessel category and type.
/// </summary>
public interface IVesselTypeHullMappingService
{
    /// <summary>
    /// Gets default hull families and parameters for a vessel type.
    /// </summary>
    /// <param name="category">Vessel category (e.g., "commercial", "recreational")</param>
    /// <param name="type">Vessel type (e.g., "container", "yacht")</param>
    /// <returns>Default hull families and parameters, or null if no mapping exists</returns>
    VesselHullDefaults? GetDefaultsForVesselType(string category, string type);
}

