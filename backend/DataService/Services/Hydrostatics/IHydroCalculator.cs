using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for hydrostatic calculations
/// </summary>
public interface IHydroCalculator
{
    /// <summary>
    /// Computes hydrostatic properties at a single draft
    /// </summary>
    Task<HydroResultDto> ComputeAtDraftAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal draft,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Computes hydrostatic table for multiple drafts
    /// </summary>
    Task<List<HydroResultDto>> ComputeTableAsync(
        Guid vesselId,
        Guid? loadcaseId,
        List<decimal> drafts,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Hydrostatic calculation result
/// </summary>
public record HydroResultDto
{
    public decimal Draft { get; init; }
    public decimal DispVolume { get; init; }
    public decimal DispWeight { get; init; }
    public decimal KBz { get; init; }
    public decimal LCBx { get; init; }
    public decimal TCBy { get; init; }
    public decimal BMt { get; init; }
    public decimal BMl { get; init; }
    public decimal? GMt { get; init; }
    public decimal? GMl { get; init; }
    public decimal Awp { get; init; }
    public decimal Iwp { get; init; }
    public decimal Cb { get; init; }
    public decimal Cp { get; init; }
    public decimal Cm { get; init; }
    public decimal Cwp { get; init; }
    public decimal? TrimAngle { get; init; }
}

