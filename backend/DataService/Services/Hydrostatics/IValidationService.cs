using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for validating hydrostatics input data
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates that station X values are monotonically increasing
    /// </summary>
    ValidationResult ValidateStations(List<StationDto> stations);

    /// <summary>
    /// Validates that waterline Z values are monotonically increasing
    /// </summary>
    ValidationResult ValidateWaterlines(List<WaterlineDto> waterlines);

    /// <summary>
    /// Validates offsets data (non-negative, all intersections present)
    /// </summary>
    ValidationResult ValidateOffsets(List<OffsetDto> offsets, int stationCount, int waterlineCount);

    /// <summary>
    /// Validates vessel principal particulars
    /// </summary>
    ValidationResult ValidateVessel(VesselDto vessel);
}

/// <summary>
/// Result of a validation operation
/// </summary>
public record ValidationResult
{
    public bool IsValid { get; init; }
    public List<ValidationError> Errors { get; init; } = new();

    public static ValidationResult Success() => new() { IsValid = true };

    public static ValidationResult Failure(params ValidationError[] errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
}

/// <summary>
/// Individual validation error
/// </summary>
public record ValidationError
{
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public int? Row { get; init; }
    public int? Column { get; init; }
}

