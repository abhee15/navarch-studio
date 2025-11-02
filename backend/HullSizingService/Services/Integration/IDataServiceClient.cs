namespace HullSizingService.Services.Integration;

/// <summary>
/// Client for making resilient HTTP calls to DataService
/// </summary>
public interface IDataServiceClient
{
    /// <summary>
    /// Get water properties for displacement calculations
    /// </summary>
    /// <param name="tempC">Temperature in Celsius</param>
    /// <param name="salinityPsu">Salinity in PSU (Practical Salinity Units)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Water properties (density, kinematic viscosity)</returns>
    Task<WaterPropertiesResponse> GetWaterPropertiesAsync(
        decimal tempC,
        decimal salinityPsu,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a vessel in DataService from a candidate design (Push to Hydrostatics)
    /// </summary>
    /// <param name="vesselDto">Vessel creation DTO with dimensions and geometry</param>
    /// <param name="idempotencyKey">Idempotency key to prevent duplicate creation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created vessel ID</returns>
    Task<Guid> CreateVesselAsync(
        CreateVesselFromCandidateDto vesselDto,
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Water properties response from DataService
/// </summary>
public record WaterPropertiesResponse
{
    /// <summary>
    /// Temperature in Celsius
    /// </summary>
    public decimal TempC { get; init; }

    /// <summary>
    /// Salinity in PSU
    /// </summary>
    public decimal SalinityPsu { get; init; }

    /// <summary>
    /// Density in kg/m³
    /// </summary>
    public decimal RhoKgM3 { get; init; }

    /// <summary>
    /// Kinematic viscosity in m²/s
    /// </summary>
    public decimal NuM2S { get; init; }
}

/// <summary>
/// DTO for creating a vessel from a candidate design
/// </summary>
public record CreateVesselFromCandidateDto
{
    public string Name { get; init; } = string.Empty;
    public string VesselType { get; init; } = string.Empty;
    public decimal Lpp { get; init; }
    public decimal Lwl { get; init; }
    public decimal Beam { get; init; }
    public decimal Draft { get; init; }
    public decimal Depth { get; init; }
    public string? Notes { get; init; }
    // TODO: Add stations/waterlines/offsets when geometry generation is implemented (Phase 2)
}

