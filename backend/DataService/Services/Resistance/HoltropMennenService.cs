using DataService.Services.Resistance;

namespace DataService.Services.Resistance;

/// <summary>
/// Service for calculating total resistance using Holtrop-Mennen 1982 method
/// </summary>
public class HoltropMennenService
{
    private readonly IResistanceCalculationService _resistanceCalc;
    private readonly WaterPropertiesService _waterProperties;
    private readonly ILogger<HoltropMennenService> _logger;
    private readonly HmMethodConfig _config;

    // Standard air density (kg/m³)
    private const decimal RHO_AIR = 1.225m;

    public HoltropMennenService(
        IResistanceCalculationService resistanceCalc,
        WaterPropertiesService waterProperties,
        ILogger<HoltropMennenService> logger)
    {
        _resistanceCalc = resistanceCalc;
        _waterProperties = waterProperties;
        _logger = logger;
        _config = HmMethodConfig.CreateDefault();
    }

    /// <summary>
    /// Calculates total resistance components using Holtrop-Mennen 1982 method
    /// </summary>
    public HoltropMennenResult CalculateResistance(
        HoltropMennenInputs inputs,
        List<decimal> speedGrid, // m/s
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Computing HM resistance for LWL={LWL}m, B={B}m, T={T}m at {Count} speeds",
            inputs.LWL, inputs.B, inputs.T, speedGrid.Count);

        var result = new HoltropMennenResult
        {
            SpeedGrid = speedGrid.ToList(),
            ReynoldsNumbers = new List<decimal>(),
            FroudeNumbers = new List<decimal>(),
            FrictionCoefficients = new List<decimal>(),
            EffectiveFrictionCoefficients = new List<decimal>(),
            FrictionResistance = new List<decimal>(),
            ResiduaryResistance = new List<decimal>(),
            AppendageResistance = new List<decimal>(),
            CorrelationAllowance = new List<decimal>(),
            AirResistance = new List<decimal>(),
            TotalResistance = new List<decimal>(),
            EffectivePower = new List<decimal>()
        };

        // Get water properties
        decimal nu = inputs.Nu ?? _waterProperties.GetKinematicViscosity(inputs.TempC, inputs.SalinityPpt ?? 35.0m);
        decimal rho = inputs.Rho ?? _waterProperties.GetWaterDensity(inputs.TempC, inputs.SalinityPpt ?? 35.0m);

        // Get form factor
        decimal k = inputs.K ?? _config.Friction.KValue;
        bool applyFormFactor = inputs.ApplyFormFactor;

        foreach (var speed in speedGrid)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Calculate Re and Fn
            decimal re = _resistanceCalc.CalculateReynoldsNumber(speed, inputs.LWL, nu);
            decimal fn = _resistanceCalc.CalculateFroudeNumber(speed, inputs.LWL);

            result.ReynoldsNumbers.Add(re);
            result.FroudeNumbers.Add(fn);

            // 1. Friction resistance (RF)
            decimal cf = _resistanceCalc.CalculateIttc57Cf(re);
            decimal cfEff = _resistanceCalc.CalculateEffectiveCf(cf, k, applyFormFactor);

            result.FrictionCoefficients.Add(cf);
            result.EffectiveFrictionCoefficients.Add(cfEff);

            // RF = 0.5 * ρ * V² * S * CF_eff
            decimal s = inputs.S ?? EstimateWettedSurfaceArea(inputs.LWL, inputs.B, inputs.T, inputs.CM ?? 0.98m);
            decimal rf = 0.5m * rho * speed * speed * s * cfEff;
            result.FrictionResistance.Add(rf);

            // 2. Residuary resistance (RR) - simplified HM 1982
            decimal rr = CalculateResiduaryResistance(inputs, fn, rho, speed, s);
            result.ResiduaryResistance.Add(rr);

            // 3. Appendage resistance (RA)
            decimal ra = CalculateAppendageResistance(rf, inputs);
            result.AppendageResistance.Add(ra);

            // 4. Correlation allowance (RCA)
            decimal rca = CalculateCorrelationAllowance(inputs, rho, speed, s);
            result.CorrelationAllowance.Add(rca);

            // 5. Air resistance (RAA)
            decimal raa = CalculateAirResistance(inputs, speed);
            result.AirResistance.Add(raa);

            // Total resistance
            decimal rt = rf + rr + ra + rca + raa;
            result.TotalResistance.Add(rt);

            // Effective Power: EHP = RT * V
            decimal ehp = rt * speed / 1000m; // Convert to kW
            result.EffectivePower.Add(ehp);
        }

        _logger.LogInformation(
            "HM calculation complete: RT range {MinRT}-{MaxRT} N, EHP range {MinEHP}-{MaxEHP} kW",
            result.TotalResistance.Min(),
            result.TotalResistance.Max(),
            result.EffectivePower.Min(),
            result.EffectivePower.Max());

        return result;
    }

    /// <summary>
    /// Estimates wetted surface area using ITTC formula
    /// S ≈ LWL * (2*T + B) * sqrt(CM)
    /// </summary>
    private decimal EstimateWettedSurfaceArea(decimal lwl, decimal b, decimal t, decimal cm)
    {
        return lwl * (2m * t + b) * (decimal)Math.Sqrt((double)cm);
    }

    /// <summary>
    /// Calculates residuary resistance (simplified HM 1982)
    /// This is a placeholder - full HM requires complex empirical formulas
    /// </summary>
    private decimal CalculateResiduaryResistance(
        HoltropMennenInputs inputs,
        decimal fn,
        decimal rho,
        decimal speed,
        decimal s)
    {
        // Simplified residuary resistance based on Froude number
        // Full HM method requires complex calculations based on vessel form
        // For MVP: use simplified empirical relationship
        decimal cb = inputs.CB ?? 0.65m;
        decimal cp = inputs.CP ?? 0.70m;

        // Simplified formula: RR ≈ f(Fn, CB, CP) * 0.5 * ρ * V² * S
        // This is a placeholder - should be replaced with full HM residuary formulas
        decimal residuaryCoefficient = 0.002m * (1m + 2m * fn) * (1m - cb + 0.1m * cp);
        decimal rr = residuaryCoefficient * 0.5m * rho * speed * speed * s;

        return rr;
    }

    /// <summary>
    /// Calculates appendage resistance using generic factor
    /// RA = RF * (generic_factor - 1.0)
    /// </summary>
    private decimal CalculateAppendageResistance(decimal rf, HoltropMennenInputs inputs)
    {
        if (_config.Appendages.Mode != "generic_factor")
        {
            // Detailed list mode not implemented yet
            return 0m;
        }

        decimal genericFactor = inputs.AppendageFactor ?? _config.Appendages.GenericFactor;
        decimal ra = rf * (genericFactor - 1.0m);

        return ra;
    }

    /// <summary>
    /// Calculates correlation allowance (ITTC-1978 model)
    /// RCA = CA * 0.5 * ρ * V² * S
    /// </summary>
    private decimal CalculateCorrelationAllowance(
        HoltropMennenInputs inputs,
        decimal rho,
        decimal speed,
        decimal s)
    {
        // ITTC-1978 correlation allowance
        // CA ≈ 0.0004 (typical for smooth ships)
        decimal ca = 0.0004m;
        decimal rca = ca * 0.5m * rho * speed * speed * s;

        return rca;
    }

    /// <summary>
    /// Calculates air resistance
    /// RAA = 0.5 * ρ_air * CD * A_windage * V²
    /// </summary>
    private decimal CalculateAirResistance(HoltropMennenInputs inputs, decimal speed)
    {
        if (inputs.WindageArea <= 0)
        {
            return 0m;
        }

        decimal cd = _config.AirResistance.CdDefault;
        decimal raa = 0.5m * RHO_AIR * cd * inputs.WindageArea * speed * speed;

        return raa;
    }
}

/// <summary>
/// Inputs for Holtrop-Mennen calculation
/// </summary>
public class HoltropMennenInputs
{
    // Geometry
    public decimal LWL { get; set; } // Length at waterline (m)
    public decimal B { get; set; }    // Beam (m)
    public decimal T { get; set; }    // Draft (m)
    public decimal? CB { get; set; }  // Block coefficient
    public decimal? CP { get; set; }  // Prismatic coefficient
    public decimal? CM { get; set; }   // Midship coefficient
    public decimal? LCB_pct { get; set; } // LCB as % of LWL

    // Wetted surface
    public decimal? S { get; set; }   // Wetted surface area (m²)

    // Appendages
    public decimal? AppendageFactor { get; set; } // Generic appendage factor
    public decimal? A_transom { get; set; }      // Transom area (m²)

    // Air resistance
    public decimal WindageArea { get; set; }      // Windage area (m²)

    // Water properties
    public decimal? Rho { get; set; }             // Water density (kg/m³)
    public decimal? Nu { get; set; }              // Kinematic viscosity (m²/s)
    public decimal TempC { get; set; } = 15.0m;   // Temperature (°C)
    public decimal? SalinityPpt { get; set; }     // Salinity (ppt)

    // Form factor
    public decimal? K { get; set; }               // Form factor (1+k)
    public bool ApplyFormFactor { get; set; } = true;
}

/// <summary>
/// Results from Holtrop-Mennen calculation
/// </summary>
public class HoltropMennenResult
{
    public List<decimal> SpeedGrid { get; set; } = new();
    public List<decimal> ReynoldsNumbers { get; set; } = new();
    public List<decimal> FroudeNumbers { get; set; } = new();
    public List<decimal> FrictionCoefficients { get; set; } = new();
    public List<decimal> EffectiveFrictionCoefficients { get; set; } = new();
    public List<decimal> FrictionResistance { get; set; } = new();
    public List<decimal> ResiduaryResistance { get; set; } = new();
    public List<decimal> AppendageResistance { get; set; } = new();
    public List<decimal> CorrelationAllowance { get; set; } = new();
    public List<decimal> AirResistance { get; set; } = new();
    public List<decimal> TotalResistance { get; set; } = new();
    public List<decimal> EffectivePower { get; set; } = new();
}
