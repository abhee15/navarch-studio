namespace NavArch.Shared.DTOs;

/// <summary>
/// Response Amplitude Operator (RAO) calculation results.
/// </summary>
public record RaoResultDto(
    Guid RaoId,
    Guid VesselId,
    Guid LoadcaseId,
    double[] Frequency,     // rad/s
    double[] HeaveRao,      // m/m
    double[] PitchRao,      // rad/m
    double[] RollRao,       // rad/m
    DateTime CreatedAt
);

