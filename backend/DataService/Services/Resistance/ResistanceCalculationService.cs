namespace DataService.Services.Resistance;

/// <summary>
/// Implementation of resistance calculation service using ITTC-57 and Holtrop-Mennen methods
/// </summary>
public class ResistanceCalculationService : IResistanceCalculationService
{
    private readonly ILogger<ResistanceCalculationService> _logger;
    private readonly WaterPropertiesService _waterProperties;

    // Minimum Reynolds number threshold for warning
    private const decimal RE_THRESHOLD = 2e6m;

    public ResistanceCalculationService(
        ILogger<ResistanceCalculationService> logger,
        WaterPropertiesService waterProperties)
    {
        _logger = logger;
        _waterProperties = waterProperties;
    }

    /// <summary>
    /// Calculates ITTC-57 friction coefficient
    /// Formula: CF = 0.075 / (log10(Re) - 2)^2
    /// </summary>
    public decimal CalculateIttc57Cf(decimal re)
    {
        if (re <= 0)
        {
            throw new ArgumentException("Reynolds number must be positive", nameof(re));
        }

        // Domain check: ITTC-57 is valid for Re > 10^5
        if (re < 1e5m)
        {
            _logger.LogWarning("Reynolds number {Re} is below ITTC-57 valid range (Re > 10^5)", re);
        }

        // Calculate log10(Re)
        double reDouble = (double)re;
        double log10Re = Math.Log10(reDouble);

        // Check for invalid domain (log10(Re) - 2) should not be zero or negative
        double denominator = log10Re - 2.0;
        if (denominator <= 0)
        {
            throw new ArgumentException($"ITTC-57 formula invalid for Re = {re}: log10(Re) - 2 <= 0", nameof(re));
        }

        // CF = 0.075 / (log10(Re) - 2)^2
        double cfDouble = 0.075 / (denominator * denominator);
        decimal cf = (decimal)cfDouble;

        _logger.LogDebug("ITTC-57: Re = {Re}, CF = {Cf}", re, cf);

        return cf;
    }

    /// <summary>
    /// Calculates effective friction coefficient with form factor (1+k)
    /// </summary>
    public decimal CalculateEffectiveCf(decimal cf, decimal k = 0.20m, bool applyFormFactor = true)
    {
        if (!applyFormFactor)
        {
            return cf;
        }

        decimal cfEff = (1.0m + k) * cf;

        _logger.LogDebug("Form factor applied: CF = {Cf}, k = {K}, CF_eff = {CfEff}", cf, k, cfEff);

        return cfEff;
    }

    /// <summary>
    /// Calculates Reynolds number: Re = V * L / ν
    /// </summary>
    public decimal CalculateReynoldsNumber(decimal speed, decimal lwl, decimal nu)
    {
        if (speed < 0 || lwl <= 0 || nu <= 0)
        {
            throw new ArgumentException("Speed, LWL, and kinematic viscosity must be positive");
        }

        decimal re = speed * lwl / nu;

        // Warning for low Reynolds number (scale effects)
        if (re < RE_THRESHOLD)
        {
            _logger.LogWarning(
                "Reynolds number {Re} is below threshold {Threshold}. Check scale effects.",
                re, RE_THRESHOLD);
        }

        return re;
    }

    /// <summary>
    /// Calculates Froude number: Fn = V / sqrt(g * L)
    /// </summary>
    public decimal CalculateFroudeNumber(decimal speed, decimal lwl, decimal g = 9.80665m)
    {
        if (speed < 0 || lwl <= 0 || g <= 0)
        {
            throw new ArgumentException("Speed, LWL, and gravity must be positive");
        }

        decimal fn = speed / (decimal)Math.Sqrt((double)(g * lwl));

        return fn;
    }
}

