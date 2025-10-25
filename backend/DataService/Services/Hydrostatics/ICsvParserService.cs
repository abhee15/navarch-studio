using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for parsing CSV files containing hull geometry
/// </summary>
public interface ICsvParserService
{
    /// <summary>
    /// Parses combined offsets CSV (station_index, station_x, waterline_index, waterline_z, half_breadth_y)
    /// </summary>
    Task<CombinedGeometryDto> ParseCombinedOffsetsAsync(Stream csvStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses stations CSV (station_index, x)
    /// </summary>
    Task<List<StationDto>> ParseStationsAsync(Stream csvStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses waterlines CSV (waterline_index, z)
    /// </summary>
    Task<List<WaterlineDto>> ParseWaterlinesAsync(Stream csvStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses offsets CSV (station_index, waterline_index, half_breadth_y)
    /// </summary>
    Task<List<OffsetDto>> ParseOffsetsAsync(Stream csvStream, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of parsing combined geometry CSV
/// </summary>
public record CombinedGeometryDto
{
    public List<StationDto> Stations { get; init; } = new();
    public List<WaterlineDto> Waterlines { get; init; } = new();
    public List<OffsetDto> Offsets { get; init; } = new();
}

