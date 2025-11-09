namespace NavArch.Shared.DTOs;

/// <summary>
/// Sea state parameters for motion response analysis.
/// </summary>
public record SeaStateDto(
    double SignificantHeight,  // Hs (m)
    double PeakPeriod,         // Tp (s)
    double Heading,            // degrees (0=following, 180=head seas)
    string Spectrum,           // "JONSWAP" or "PM"
    double Gamma = 3.3         // Peak enhancement factor (default 3.3 for JONSWAP)
);

