using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for generating hydrostatic curves
/// </summary>
public interface ICurvesGenerator
{
    /// <summary>
    /// Generates displacement curve: ∆(T)
    /// </summary>
    Task<CurveDataDto> GenerateDisplacementCurveAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal minDraft,
        decimal maxDraft,
        int points = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates KB curve: KB(T)
    /// </summary>
    Task<CurveDataDto> GenerateKBCurveAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal minDraft,
        decimal maxDraft,
        int points = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates LCB curve: LCB(T)
    /// </summary>
    Task<CurveDataDto> GenerateLCBCurveAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal minDraft,
        decimal maxDraft,
        int points = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates GMt curve: GMt(T)
    /// </summary>
    Task<CurveDataDto> GenerateGMtCurveAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal minDraft,
        decimal maxDraft,
        int points = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates waterplane area curve: Awp(T)
    /// </summary>
    Task<CurveDataDto> GenerateAwpCurveAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal minDraft,
        decimal maxDraft,
        int points = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates Bonjean curves: sectional area vs draft per station
    /// </summary>
    Task<List<BonjeanCurveDto>> GenerateBonjeanCurvesAsync(
        Guid vesselId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates multiple curves at once
    /// </summary>
    Task<Dictionary<string, CurveDataDto>> GenerateMultipleCurvesAsync(
        Guid vesselId,
        Guid? loadcaseId,
        List<string> curveTypes, // "displacement", "kb", "lcb", "gmt", "awp"
        decimal minDraft,
        decimal maxDraft,
        int points = 100,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Curve data with X/Y points
/// </summary>
public record CurveDataDto
{
    public string Type { get; init; } = string.Empty;
    public string XLabel { get; init; } = string.Empty;
    public string YLabel { get; init; } = string.Empty;
    public List<CurvePointDto> Points { get; init; } = new();
}

/// <summary>
/// Single point on a curve
/// </summary>
public record CurvePointDto
{
    public decimal X { get; init; }
    public decimal Y { get; init; }
}

/// <summary>
/// Bonjean curve (sectional area vs draft) for a single station
/// </summary>
public record BonjeanCurveDto
{
    public int StationIndex { get; init; }
    public decimal StationX { get; init; }
    public List<CurvePointDto> Points { get; init; } = new(); // X=draft, Y=sectional area
}

