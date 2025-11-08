namespace NavArch.Shared.DTOs;

/// <summary>
/// Request for RAO (Response Amplitude Operator) calculation.
/// </summary>
public record RaoCalculationRequestDto(
    Guid LoadcaseId,
    FrequencyRangeDto FrequencyRange,
    string[] MotionModes // e.g., ["heave", "pitch", "roll"]
);

/// <summary>
/// Defines the frequency range for RAO calculations.
/// </summary>
public record FrequencyRangeDto(
    double Min,  // Minimum frequency (rad/s)
    double Max,  // Maximum frequency (rad/s)
    double Step  // Frequency step (rad/s)
);
