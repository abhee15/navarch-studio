namespace NavArch.Shared.DTOs;

/// <summary>
/// Vessel motion response in irregular seas.
/// </summary>
public record MotionResponseDto(
    Guid MotionResponseId,
    Guid RaoResultId,
    SeaStateDto SeaState,
    double SignificantHeave,      // m
    double SignificantPitch,      // degrees
    double SignificantRoll,       // degrees
    Dictionary<string, double> MeanPeriods,  // seconds { "heave": 8.2, "pitch": 8.5, "roll": 9.1 }
    Dictionary<string, double> ExceedanceProbabilities,  // { "heave2m": 0.15, "pitch5deg": 0.22, ... }
    DateTime CreatedAt
);
