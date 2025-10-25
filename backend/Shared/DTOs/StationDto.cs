namespace Shared.DTOs;

public record StationDto
{
    public int StationIndex { get; init; }
    public decimal X { get; init; }
}

public record StationsImportDto
{
    public List<StationDto> Stations { get; init; } = new();
}

