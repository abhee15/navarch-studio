using NavArch.Shared.DTOs;

namespace DataService.Services.Seakeeping;

/// <summary>
/// Motion analysis service for computing vessel response in irregular seas.
/// </summary>
public interface IMotionAnalysisService
{
    /// <summary>
    /// Analyze vessel motion response in a given sea state.
    /// </summary>
    /// <param name="raoResultId">RAO results ID</param>
    /// <param name="seaState">Sea state parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Motion response (significant motions, mean periods, exceedances)</returns>
    Task<MotionResponseDto> AnalyzeMotionAsync(
        Guid raoResultId,
        SeaStateDto seaState,
        CancellationToken cancellationToken = default
    );
}

