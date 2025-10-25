namespace Shared.DTOs;

public record HydroResultDto
{
    public decimal Draft { get; init; }
    public decimal? DispVolume { get; init; }
    public decimal? DispWeight { get; init; }
    public decimal? KBz { get; init; }
    public decimal? LCBx { get; init; }
    public decimal? TCBy { get; init; }
    public decimal? BMt { get; init; }
    public decimal? BMl { get; init; }
    public decimal? GMt { get; init; }
    public decimal? GMl { get; init; }
    public decimal? Awp { get; init; }
    public decimal? Iwp { get; init; }
    public decimal? Cb { get; init; }
    public decimal? Cp { get; init; }
    public decimal? Cm { get; init; }
    public decimal? Cwp { get; init; }
    public decimal? TrimAngle { get; init; }
}

public record HydroTableRequestDto
{
    public Guid LoadcaseId { get; init; }
    public List<decimal> Drafts { get; init; } = new();
}

public record HydroTableResponseDto
{
    public List<HydroResultDto> Results { get; init; } = new();
    public int ComputationTimeMs { get; init; }
}

