namespace Shared.DTOs;

/// <summary>
/// Request DTO for speed-draft matrix (heatmap) calculation
/// </summary>
public class SpeedDraftMatrixRequest
{
    public Guid VesselId { get; set; }

    // Speed range
    public decimal MinSpeed { get; set; } // m/s
    public decimal MaxSpeed { get; set; } // m/s
    public int SpeedSteps { get; set; } = 20;

    // Draft range
    public decimal MinDraft { get; set; } // m
    public decimal MaxDraft { get; set; } // m
    public int DraftSteps { get; set; } = 15;

    // Calculation parameters (optional - uses vessel defaults if not provided)
    public decimal? LWL { get; set; }
    public decimal? B { get; set; }
    public decimal? CB { get; set; }
    public decimal? CP { get; set; }
    public decimal? CM { get; set; }
    public decimal? LCB_pct { get; set; }
    public decimal? S { get; set; }
    public decimal? AppendageFactor { get; set; }
    public decimal? A_transom { get; set; }
    public decimal? WindageArea { get; set; }
    public decimal? K { get; set; }
    public bool ApplyFormFactor { get; set; } = true;

    // Water properties
    public int? TempC { get; set; } = 15;
    public decimal? SalinityPpt { get; set; } = 35.0m;

    // Design and trial points (optional)
    public List<DesignPoint>? DesignPoints { get; set; }
    public List<TrialPoint>? TrialPoints { get; set; }
}

/// <summary>
/// Design point to overlay on heatmap
/// </summary>
public class DesignPoint
{
    public string Name { get; set; } = string.Empty;
    public decimal Speed { get; set; } // m/s
    public decimal Draft { get; set; } // m
    public decimal? ExpectedPower { get; set; } // kW (optional)
}

/// <summary>
/// Trial/measured point to overlay on heatmap
/// </summary>
public class TrialPoint
{
    public string Name { get; set; } = string.Empty;
    public decimal Speed { get; set; } // m/s
    public decimal Draft { get; set; } // m
    public decimal MeasuredPower { get; set; } // kW
    public string? TrialDate { get; set; }
}

/// <summary>
/// Response DTO for speed-draft matrix calculation
/// </summary>
public class SpeedDraftMatrixResult
{
    public List<decimal> SpeedGrid { get; set; } = new(); // m/s
    public List<decimal> DraftGrid { get; set; } = new(); // m

    // Matrix data: [draftIndex][speedIndex] = value
    public List<List<decimal>> PowerMatrix { get; set; } = new(); // Effective Power (kW)
    public List<List<decimal>> ResistanceMatrix { get; set; } = new(); // Total Resistance (N)
    public List<List<decimal>> FroudeNumberMatrix { get; set; } = new();

    // Detailed breakdown available for each point (flattened for efficiency)
    public List<MatrixPointDetails> PointDetails { get; set; } = new();

    // Design and trial points echoed back
    public List<DesignPoint>? DesignPoints { get; set; }
    public List<TrialPoint>? TrialPoints { get; set; }

    // Calculation metadata
    public int TotalPoints { get; set; }
    public string CalculationMethod { get; set; } = "Holtrop-Mennen 1982";
}

/// <summary>
/// Detailed breakdown for a single point in the matrix
/// </summary>
public class MatrixPointDetails
{
    public int SpeedIndex { get; set; }
    public int DraftIndex { get; set; }
    public decimal Speed { get; set; } // m/s
    public decimal Draft { get; set; } // m

    // Resistance components
    public decimal FrictionResistance { get; set; } // RF (N)
    public decimal ResiduaryResistance { get; set; } // RR (N)
    public decimal AppendageResistance { get; set; } // RA (N)
    public decimal CorrelationAllowance { get; set; } // RCA (N)
    public decimal AirResistance { get; set; } // RAA (N)
    public decimal TotalResistance { get; set; } // RT (N)

    // Power
    public decimal EffectivePower { get; set; } // EHP (kW)

    // Non-dimensional parameters
    public decimal ReynoldsNumber { get; set; }
    public decimal FroudeNumber { get; set; }
    public decimal FrictionCoefficient { get; set; }

    // Form coefficients at this draft
    public decimal CB { get; set; }
    public decimal CP { get; set; }
    public decimal CM { get; set; }
}
