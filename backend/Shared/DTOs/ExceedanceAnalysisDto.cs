namespace NavArch.Shared.DTOs;

/// <summary>
/// Exceedance probability analysis for motion thresholds.
/// </summary>
public record ExceedanceAnalysisDto(
    Guid MotionResponseId,
    Dictionary<string, Dictionary<double, double>> Exceedances
// e.g., { "heave": { 1.0: 0.42, 2.0: 0.15 }, "pitch": { 3.0: 0.38 }, ... }
// Outer key: motion type, Inner: threshold -> probability
);
