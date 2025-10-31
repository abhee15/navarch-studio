namespace DataService.Services.Resistance;

/// <summary>
/// Service for calculating water properties (density, kinematic viscosity) from temperature and salinity
/// </summary>
public class WaterPropertiesService
{
    private readonly ILogger<WaterPropertiesService> _logger;

    // Standard air density at sea level (kg/m³)
    private const decimal RHO_AIR = 1.225m;

    // Standard gravity (m/s²)
    private const decimal G = 9.80665m;

    public WaterPropertiesService(ILogger<WaterPropertiesService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets kinematic viscosity (ν) in m²/s from temperature and salinity
    /// Uses ITTC standard formulas
    /// </summary>
    public decimal GetKinematicViscosity(decimal tempC, decimal salinityPpt = 35.0m)
    {
        // ITTC formula for kinematic viscosity
        // ν = (1.7915 - 0.0352 * T + 0.0004 * T²) × 10^-6
        // Adjusted for salinity using simplified relationship
        decimal tempAdjustment = 1.7915m - 0.0352m * tempC + 0.0004m * tempC * tempC;
        decimal baseNu = tempAdjustment * 1e-6m;

        // Salinity correction (simplified): ν increases slightly with salinity
        decimal salinityCorrection = 1.0m + (salinityPpt - 35.0m) * 0.0001m;

        decimal nu = baseNu * salinityCorrection;

        // Clamp to reasonable range
        if (nu < 0.8e-6m) nu = 0.8e-6m;
        if (nu > 2.0e-6m) nu = 2.0e-6m;

        _logger.LogDebug("Calculated ν = {Nu} m²/s from T = {TempC}°C, S = {Salinity} ppt", nu, tempC, salinityPpt);

        return nu;
    }

    /// <summary>
    /// Gets water density (ρ) in kg/m³ from temperature and salinity
    /// </summary>
    public decimal GetWaterDensity(decimal tempC, decimal salinityPpt = 35.0m)
    {
        // Simplified density formula for seawater
        // Base density at 15°C, 35 ppt: 1025 kg/m³
        decimal baseRho = 1025.0m;

        // Temperature correction: decreases with temperature
        decimal tempCorrection = -0.2m * (tempC - 15.0m);

        // Salinity correction: increases with salinity
        decimal salinityCorrection = 0.7m * (salinityPpt - 35.0m);

        decimal rho = baseRho + tempCorrection + salinityCorrection;

        // Clamp to reasonable range
        if (rho < 995.0m) rho = 995.0m;
        if (rho > 1030.0m) rho = 1030.0m;

        _logger.LogDebug("Calculated ρ = {Rho} kg/m³ from T = {TempC}°C, S = {Salinity} ppt", rho, tempC, salinityPpt);

        return rho;
    }

    /// <summary>
    /// Gets standard air density (kg/m³)
    /// </summary>
    public static decimal GetAirDensity()
    {
        return RHO_AIR;
    }

    /// <summary>
    /// Gets standard gravity (m/s²)
    /// </summary>
    public static decimal GetGravity()
    {
        return G;
    }
}

