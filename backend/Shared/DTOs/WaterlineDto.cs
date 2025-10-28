using Shared.Attributes;

namespace Shared.DTOs;

public record WaterlineDto
{
    public int WaterlineIndex { get; init; }

    [Convertible("Length")]
    public decimal Z { get; init; }
}

public record WaterlinesImportDto
{
    public List<WaterlineDto> Waterlines { get; init; } = new();
}

