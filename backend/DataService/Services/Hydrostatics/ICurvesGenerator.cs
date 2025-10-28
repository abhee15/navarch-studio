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
    Task<CurveDto> GenerateDisplacementCurveAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal minDraft,
        decimal maxDraft,
        int points = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates KB curve: KB(T)
    /// </summary>
    Task<CurveDto> GenerateKBCurveAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal minDraft,
        decimal maxDraft,
        int points = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates LCB curve: LCB(T)
    /// </summary>
    Task<CurveDto> GenerateLCBCurveAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal minDraft,
        decimal maxDraft,
        int points = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates GMt curve: GMt(T)
    /// </summary>
    Task<CurveDto> GenerateGMtCurveAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal minDraft,
        decimal maxDraft,
        int points = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates waterplane area curve: Awp(T)
    /// </summary>
    Task<CurveDto> GenerateAwpCurveAsync(
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
    Task<Dictionary<string, CurveDto>> GenerateMultipleCurvesAsync(
        Guid vesselId,
        Guid? loadcaseId,
        List<string> curveTypes, // "displacement", "kb", "lcb", "gmt", "awp"
        decimal minDraft,
        decimal maxDraft,
        int points = 100,
        CancellationToken cancellationToken = default);
}

