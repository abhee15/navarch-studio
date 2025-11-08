using NavArch.Shared.DTOs;

namespace DataService.Services.Seakeeping;

/// <summary>
/// RAO (Response Amplitude Operator) calculator.
/// Solves frequency-domain equations of motion to compute vessel responses to regular waves.
/// </summary>
public interface IRaoCalculator
{
    /// <summary>
    /// Calculate RAOs for heave, pitch, and roll motions.
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="request">RAO calculation request parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>RAO results</returns>
    Task<RaoResultDto> CalculateRaosAsync(
        Guid vesselId,
        RaoCalculationRequestDto request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Get RAO results by ID.
    /// </summary>
    /// <param name="raoId">RAO result ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>RAO results or null if not found</returns>
    Task<RaoResultDto?> GetRaoByIdAsync(
        Guid raoId,
        CancellationToken cancellationToken = default
    );
}
