using Shared.DTOs.Catalog;
using Shared.DTOs.ShipD;

namespace HullSizingService.Services.Integration;

/// <summary>
/// Client for communicating with the DataService (Holtrop-Mennen resistance, water properties, etc.)
/// </summary>
public interface IDataServiceClient
{
    /// <summary>
    /// Get water properties (density, viscosity) for a given temperature and salinity
    /// </summary>
    Task<WaterPropertiesResponse?> GetWaterPropertiesAsync(decimal temperatureCelsius, decimal salinityPpt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate resistance using Holtrop-Mennen method (Phase 2 - integration with DataService)
    /// </summary>
    Task<HoltropResponse?> CalculateResistanceAsync(HoltropRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for similar vessels in the real-world catalog using KNN
    /// </summary>
    Task<KnnSearchResponse> SearchSimilarVesselsAsync(KnnSearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for similar parametric hulls in the ML catalog using KNN
    /// </summary>
    Task<ParametricSearchResponse> SearchSimilarParametricHullsAsync(ParametricSearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch ShipD parameter metadata (45 canonical parameters).
    /// </summary>
    Task<IReadOnlyList<ShipDParameterMetadataDto>> GetShipDParameterMetadataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch ShipD vessel taxonomy mappings.
    /// </summary>
    Task<IReadOnlyList<ShipDVesselTaxonomyDto>> GetShipDVesselTaxonomyAsync(CancellationToken cancellationToken = default);
}

public record WaterPropertiesResponse(
    decimal DensityKgM3,
    decimal KinematicViscosityM2S,
    decimal TemperatureCelsius,
    decimal SalinityPpt
);

public record HoltropRequest(
    decimal LppM,
    decimal LwlM,
    decimal BeamM,
    decimal DraftM,
    decimal Cb,
    decimal Cp,
    decimal Cwp,
    decimal Cm,
    decimal SpeedKn
);

public record HoltropResponse(
    decimal EhpKw,
    decimal ShpKw,
    decimal ResistanceKn,
    decimal FroudeNumber
);
