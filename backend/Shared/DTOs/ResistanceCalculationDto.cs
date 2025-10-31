namespace Shared.DTOs;

/// <summary>
/// Request DTO for ITTC-57 friction calculation
/// </summary>
public class Ittc57CalculationRequest
{
    public Guid VesselId { get; set; }
    public Guid SpeedGridId { get; set; }
    public decimal? FormFactor { get; set; } // (1+k) - optional, uses config default if not provided
    public bool ApplyFormFactor { get; set; } = true;
    public int? TempC { get; set; } = 15;
    public decimal? SalinityPpt { get; set; } = 35.0m;
}

/// <summary>
/// Response DTO for ITTC-57 friction calculation
/// </summary>
public class Ittc57CalculationResult
{
    public List<decimal> SpeedGrid { get; set; } = new(); // m/s
    public List<decimal> ReynoldsNumbers { get; set; } = new();
    public List<decimal> FroudeNumbers { get; set; } = new();
    public List<decimal> FrictionCoefficients { get; set; } = new(); // CF (ITTC-57)
    public List<decimal> EffectiveFrictionCoefficients { get; set; } = new(); // CF_eff = (1+k)*CF
    public decimal? FormFactor { get; set; } // (1+k) used in calculation
}

/// <summary>
/// Request DTO for Holtrop-Mennen resistance calculation
/// </summary>
public class HoltropMennenCalculationRequest
{
    public Guid VesselId { get; set; }
    public Guid SpeedGridId { get; set; }

    // Geometry (will use vessel defaults if not provided)
    public decimal? LWL { get; set; } // Length at waterline (m)
    public decimal? B { get; set; }    // Beam (m)
    public decimal? T { get; set; }    // Draft (m)
    public decimal? CB { get; set; }   // Block coefficient
    public decimal? CP { get; set; }   // Prismatic coefficient
    public decimal? CM { get; set; }    // Midship coefficient
    public decimal? LCB_pct { get; set; } // LCB as % of LWL

    // Wetted surface
    public decimal? S { get; set; }    // Wetted surface area (m²) - will estimate if not provided

    // Appendages
    public decimal? AppendageFactor { get; set; } // Generic appendage factor (default from config)
    public decimal? A_transom { get; set; }       // Transom area (m²)

    // Air resistance
    public decimal? WindageArea { get; set; }    // Windage area (m²)

    // Form factor
    public decimal? K { get; set; }               // Form factor (1+k) - default from config
    public bool ApplyFormFactor { get; set; } = true;

    // Water properties
    public int? TempC { get; set; } = 15;
    public decimal? SalinityPpt { get; set; } = 35.0m;
}

/// <summary>
/// Response DTO for Holtrop-Mennen resistance calculation
/// </summary>
public class HoltropMennenCalculationResult
{
    public List<decimal> SpeedGrid { get; set; } = new(); // m/s
    public List<decimal> ReynoldsNumbers { get; set; } = new();
    public List<decimal> FroudeNumbers { get; set; } = new();
    public List<decimal> FrictionCoefficients { get; set; } = new();
    public List<decimal> EffectiveFrictionCoefficients { get; set; } = new();
    public List<decimal> FrictionResistance { get; set; } = new(); // RF (N)
    public List<decimal> ResiduaryResistance { get; set; } = new(); // RR (N)
    public List<decimal> AppendageResistance { get; set; } = new(); // RA (N)
    public List<decimal> CorrelationAllowance { get; set; } = new(); // RCA (N)
    public List<decimal> AirResistance { get; set; } = new(); // RAA (N)
    public List<decimal> TotalResistance { get; set; } = new(); // RT (N)
    public List<decimal> EffectivePower { get; set; } = new(); // EHP (kW)
}

