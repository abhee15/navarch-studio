namespace Shared.DTOs;

public record OffsetDto
{
    public int StationIndex { get; init; }
    public int WaterlineIndex { get; init; }
    public decimal HalfBreadthY { get; init; }
}

public record OffsetsImportDto
{
    public List<OffsetDto> Offsets { get; init; } = new();
}

public record OffsetsGridDto
{
    /// <summary>
    /// Station X positions
    /// </summary>
    public List<decimal> Stations { get; init; } = new();

    /// <summary>
    /// Waterline Z positions
    /// </summary>
    public List<decimal> Waterlines { get; init; } = new();

    /// <summary>
    /// 2D grid of offsets [stationIndex][waterlineIndex]
    /// </summary>
    public List<List<decimal>> Offsets { get; init; } = new();
}

