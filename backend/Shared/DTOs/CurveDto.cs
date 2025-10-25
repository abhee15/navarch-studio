namespace Shared.DTOs;

public record CurvePointDto
{
    public decimal X { get; init; }
    public decimal Y { get; init; }
}

public record CurveDto
{
    public string Type { get; init; } = string.Empty;
    public string? XLabel { get; init; }
    public string? YLabel { get; init; }
    public List<CurvePointDto> Points { get; init; } = new();
}

public record CurveGenerationRequestDto
{
    public Guid LoadcaseId { get; init; }
    public List<string> Types { get; init; } = new();
    public decimal MinDraft { get; init; }
    public decimal MaxDraft { get; init; }
    public int Points { get; init; } = 100;
}

public record CurveGenerationResponseDto
{
    public List<CurveDto> Curves { get; init; } = new();
}

