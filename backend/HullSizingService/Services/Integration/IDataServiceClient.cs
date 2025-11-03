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
