using Microsoft.Extensions.Logging;
using Shared.HullGenerators.ParentHull;

namespace Shared.HullGenerators;

/// <summary>
/// Factory for selecting appropriate hull generator
/// Tries parent hull first, falls back to parametric method
/// </summary>
public class HullGeneratorFactory
{
    private readonly ILogger<HullGeneratorFactory>? _logger;
    private readonly ILogger<ParentHullHullGenerator>? _parentHullLogger;
    private readonly ILogger<FormCoefficientHullGenerator>? _parametricLogger;

    public HullGeneratorFactory(
        ILogger<HullGeneratorFactory>? logger = null,
        ILogger<ParentHullHullGenerator>? parentHullLogger = null,
        ILogger<FormCoefficientHullGenerator>? parametricLogger = null)
    {
        _logger = logger;
        _parentHullLogger = parentHullLogger;
        _parametricLogger = parametricLogger;
    }

    /// <summary>
    /// Get appropriate hull generator for vessel type and Cb
    /// </summary>
    /// <param name="vesselType">Vessel type (e.g., "product_carrier", "container", "tanker")
    ///                          Can be ShipdType, VesselType, or registry type</param>
    /// <param name="cb">Block coefficient</param>
    /// <returns>Hull generator (parent hull if available, otherwise parametric)</returns>
    public IHullGenerator GetGenerator(string? vesselType, decimal cb)
    {
        // Map to registry type if needed
        var registryType = VesselTypeMapper.MapToRegistryType(vesselType, vesselType);
        return GetGeneratorInternal(registryType, cb);
    }

    /// <summary>
    /// Get generator from ShipdType and VesselType
    /// </summary>
    public IHullGenerator GetGenerator(string? shipdType, string? vesselType, decimal cb)
    {
        var registryType = VesselTypeMapper.MapToRegistryType(shipdType, vesselType);
        return GetGeneratorInternal(registryType, cb);
    }

    /// <summary>
    /// Internal method to get generator
    /// </summary>
    private IHullGenerator GetGeneratorInternal(string? registryType, decimal cb)
    {
        // Try parent hull first
        if (ParentHullLoader.HasParentHull(registryType, cb))
        {
            _logger?.LogInformation(
                "Using parent hull generator for {VesselType}, Cb={Cb}",
                registryType ?? "unknown", cb);

            // Use vessel-type-specific generator if available, otherwise generic parent hull generator
            return CreateVesselTypeSpecificGenerator(registryType)
                ?? new ParentHullHullGenerator(_parentHullLogger, registryType);
        }

        // Fallback to parametric
        _logger?.LogInformation(
            "Parent hull not available for {VesselType}, Cb={Cb}. Using parametric generator.",
            registryType ?? "unknown", cb);

        return new FormCoefficientHullGenerator(_parametricLogger);
    }

    /// <summary>
    /// Get generator with explicit preference
    /// </summary>
    public IHullGenerator GetGenerator(
        string? vesselType,
        decimal cb,
        bool preferParentHull = true)
    {
        var registryType = VesselTypeMapper.MapToRegistryType(vesselType, vesselType);
        return GetGeneratorInternal(registryType, cb, preferParentHull);
    }

    /// <summary>
    /// Internal method with preference flag
    /// </summary>
    private IHullGenerator GetGeneratorInternal(string? registryType, decimal cb, bool preferParentHull = true)
    {
        if (preferParentHull && ParentHullLoader.HasParentHull(registryType, cb))
        {
            _logger?.LogInformation(
                "Using parent hull generator for {VesselType}, Cb={Cb}",
                registryType ?? "unknown", cb);

            // Use vessel-type-specific generator if available, otherwise generic parent hull generator
            return CreateVesselTypeSpecificGenerator(registryType)
                ?? new ParentHullHullGenerator(_parentHullLogger, registryType);
        }

        // Fallback to parametric
        _logger?.LogInformation(
            "Parent hull not available for {VesselType}, Cb={Cb}. Using parametric generator.",
            registryType ?? "unknown", cb);

        return new FormCoefficientHullGenerator(_parametricLogger);
    }

    /// <summary>
    /// Create vessel-type-specific generator if available
    /// </summary>
    private IHullGenerator? CreateVesselTypeSpecificGenerator(string? registryType)
    {
        if (string.IsNullOrWhiteSpace(registryType))
            return null;

        // Map to specific generator classes
        // Note: Each generator type has its own logger type, so we pass null and let them create their own
        return registryType.ToLowerInvariant() switch
        {
            "product_carrier" => new VesselTypeSpecific.ProductCarrierHullGenerator(null),
            "container" => new VesselTypeSpecific.ContainerShipHullGenerator(null),
            "tanker" => new VesselTypeSpecific.TankerHullGenerator(null),
            "bulk_carrier" => new VesselTypeSpecific.BulkCarrierHullGenerator(null),
            "general_cargo" => new VesselTypeSpecific.GeneralCargoHullGenerator(null),
            // Additional types can be added as parent hulls become available
            // "roro" => new RoRoHullGenerator(null),
            // "fast_ferry" => new FastFerryHullGenerator(null),
            // "yacht" => new YachtHullGenerator(null),
            // "cruise" => new CruiseHullGenerator(null),
            _ => null
        };
    }
}
