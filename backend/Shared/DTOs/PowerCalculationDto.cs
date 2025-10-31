namespace Shared.DTOs;

/// <summary>
/// Request DTO for power curve calculation from EHP
/// </summary>
public class PowerCurveRequest
{
    public List<decimal> EffectivePower { get; set; } = new(); // EHP in kW
    public List<decimal> SpeedGrid { get; set; } = new(); // m/s
    
    // Efficiency options - either single etaD or decomposed
    public decimal? EtaD { get; set; } // Overall propulsive efficiency (0.6-0.7 typical)
    public decimal? EtaH { get; set; } // Hull efficiency (~0.98-1.02)
    public decimal? EtaR { get; set; } // Relative rotative efficiency (~1.0-1.05)
    public decimal? EtaO { get; set; } // Open water efficiency (~0.5-0.7)
    
    public decimal ServiceMargin { get; set; } = 15.0m; // Percentage (0-30%)
}

/// <summary>
/// Response DTO for power curve results
/// </summary>
public class PowerCurveResult
{
    public List<decimal> SpeedGrid { get; set; } = new(); // m/s
    public List<decimal> EffectivePower { get; set; } = new(); // EHP (kW)
    public List<decimal> DeliveredPower { get; set; } = new(); // DHP (kW)
    public List<decimal> InstalledPower { get; set; } = new(); // P_inst (kW)
    public decimal ServiceMargin { get; set; } // Percentage used
    public decimal? EtaD { get; set; } // Overall efficiency used
}

