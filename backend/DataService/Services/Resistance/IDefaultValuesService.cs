using Shared.DTOs;

namespace DataService.Services.Resistance;

/// <summary>
/// Service for providing typical/default values for resistance and powering calculations
/// based on vessel characteristics
/// </summary>
public interface IDefaultValuesService
{
    /// <summary>
    /// Gets default values based on vessel characteristics
    /// </summary>
    DefaultValuesResponseDto GetDefaultValues(DefaultValuesRequestDto request);
}
