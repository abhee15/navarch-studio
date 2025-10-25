using Shared.DTOs;
using Shared.Models;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for managing hull geometry (stations, waterlines, offsets)
/// </summary>
public interface IGeometryService
{
    /// <summary>
    /// Imports stations for a vessel
    /// </summary>
    Task<List<Station>> ImportStationsAsync(Guid vesselId, List<StationDto> stations, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports waterlines for a vessel
    /// </summary>
    Task<List<Waterline>> ImportWaterlinesAsync(Guid vesselId, List<WaterlineDto> waterlines, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports offsets for a vessel
    /// </summary>
    Task<List<Offset>> ImportOffsetsAsync(Guid vesselId, List<OffsetDto> offsets, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the complete offsets grid for a vessel
    /// </summary>
    Task<OffsetsGridDto?> GetOffsetsGridAsync(Guid vesselId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports all geometry from combined CSV data
    /// </summary>
    Task<(int stationsCount, int waterlinesCount, int offsetsCount)> ImportCombinedGeometryAsync(
        Guid vesselId,
        List<StationDto> stations,
        List<WaterlineDto> waterlines,
        List<OffsetDto> offsets,
        CancellationToken cancellationToken = default);
}

