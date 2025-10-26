namespace Shared.Services;

/// <summary>
/// Service for converting DTOs between unit systems
/// </summary>
public interface IUnitConversionService
{
    /// <summary>
    /// Convert a DTO from source unit system to target unit system
    /// </summary>
    T ConvertDto<T>(T dto, string fromUnits, string toUnits) where T : class;

    /// <summary>
    /// Get the user's preferred unit system from request headers
    /// Default to the source unit system if not specified
    /// </summary>
    string GetPreferredUnits(string? headerValue, string defaultUnits = "SI");
}

