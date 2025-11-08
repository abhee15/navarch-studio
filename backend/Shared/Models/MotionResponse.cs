using NavArch.Shared.Models;

namespace NavArch.Shared.Models;

/// <summary>
/// Represents vessel motion response analysis in irregular seas (JONSWAP/PM spectrum).
/// </summary>
public class MotionResponse
{
    public Guid Id { get; set; }
    public Guid RaoResultId { get; set; }

    // Sea state parameters
    public double SeaStateHs { get; set; } // Significant wave height (m)
    public double SeaStateTp { get; set; } // Peak period (s)
    public double SeaStateHeading { get; set; } // Heading angle (degrees, 0=head seas)
    public string SeaStateSpectrum { get; set; } = "JONSWAP"; // "JONSWAP" or "PM"
    public double SeaStateGamma { get; set; } = 3.3; // Peak enhancement factor

    // Significant motion responses (1/3 highest)
    public double SignificantHeave { get; set; } // m
    public double SignificantPitch { get; set; } // degrees
    public double SignificantRoll { get; set; } // degrees

    // Mean periods
    public double HeaveMeanPeriod { get; set; } // s
    public double PitchMeanPeriod { get; set; } // s
    public double RollMeanPeriod { get; set; } // s

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; } // Soft delete

    // Navigation properties
    public RaoResult RaoResult { get; set; } = null!;
}
