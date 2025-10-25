namespace Shared.DTOs;

public record WaterlineDto
{
    public int WaterlineIndex { get; init; }
    public decimal Z { get; init; }
}

public record WaterlinesImportDto
{
    public List<WaterlineDto> Waterlines { get; init; } = new();
}

