namespace Shared.DTOs;

/// <summary>
/// Request DTO for KCS benchmark validation
/// </summary>
public class KcsBenchmarkRequest
{
    public Guid VesselId { get; set; }
    public Guid SpeedGridId { get; set; }

    // KCS vessel particulars (read-only from preset)
    public decimal LWL { get; set; } // Length at waterline (m)
    public decimal B { get; set; }    // Beam (m)
    public decimal T { get; set; }    // Draft (m)
    public decimal? CB { get; set; }   // Block coefficient
    public decimal? CP { get; set; }   // Prismatic coefficient
    public decimal? CM { get; set; }    // Midship coefficient
    public decimal? LCB_pct { get; set; } // LCB as % of LWL
    public decimal? S { get; set; }    // Wetted surface area (m²)

    // Water properties
    public int TempC { get; set; } = 15;
    public decimal SalinityPpt { get; set; } = 35.0m;

    // Reference data points (speed, RT_ref)
    public List<KcsReferencePoint> ReferenceData { get; set; } = new();

    // Tolerances
    public decimal MaeTolerancePercent { get; set; } = 3.0m; // Default 3%
    public decimal MaxTolerancePercent { get; set; } = 5.0m; // Default 5%
}

/// <summary>
/// A reference data point for KCS benchmark
/// </summary>
public class KcsReferencePoint
{
    public decimal Speed { get; set; } // m/s
    public decimal RtReference { get; set; } // N (reference total resistance)
    public string? Source { get; set; } // Optional source annotation
}

/// <summary>
/// Response DTO for KCS benchmark results
/// </summary>
public class KcsBenchmarkResult
{
    public List<decimal> SpeedGrid { get; set; } = new(); // m/s
    public List<decimal> CalculatedResistance { get; set; } = new(); // RT_calc (N)
    public List<decimal> ReferenceResistance { get; set; } = new(); // RT_ref (N)
    public List<decimal> ErrorPercent { get; set; } = new(); // Δ% at each speed
    public decimal MeanAbsoluteError { get; set; } // MAE (%)
    public decimal MaxError { get; set; } // Max error (%)
    public bool Pass { get; set; } // True if MAE≤tolerance and Max≤tolerance
    public decimal MaeTolerance { get; set; }
    public decimal MaxTolerance { get; set; }

    // Calculation details
    public HoltropMennenCalculationResult? CalculationDetails { get; set; }
}
