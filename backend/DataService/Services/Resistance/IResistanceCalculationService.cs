namespace DataService.Services.Resistance;

/// <summary>
/// Service for calculating resistance using ITTC-57 and Holtrop-Mennen methods
/// </summary>
public interface IResistanceCalculationService
{
    /// <summary>
    /// Calculates ITTC-57 friction coefficient CF from Reynolds number
    /// </summary>
    /// <param name="re">Reynolds number</param>
    /// <returns>Friction coefficient CF</returns>
    decimal CalculateIttc57Cf(decimal re);

    /// <summary>
    /// Calculates effective friction coefficient with form factor (1+k)
    /// </summary>
    /// <param name="cf">Base ITTC-57 friction coefficient</param>
    /// <param name="k">Form factor (default 0.20)</param>
    /// <param name="applyFormFactor">Whether to apply form factor</param>
    /// <returns>Effective friction coefficient CF_eff</returns>
    decimal CalculateEffectiveCf(decimal cf, decimal k = 0.20m, bool applyFormFactor = true);

    /// <summary>
    /// Calculates Reynolds number from speed, length, and kinematic viscosity
    /// </summary>
    /// <param name="speed">Speed in m/s</param>
    /// <param name="lwl">Length at waterline in m</param>
    /// <param name="nu">Kinematic viscosity in m²/s</param>
    /// <returns>Reynolds number</returns>
    decimal CalculateReynoldsNumber(decimal speed, decimal lwl, decimal nu);

    /// <summary>
    /// Calculates Froude number from speed, length, and gravity
    /// </summary>
    /// <param name="speed">Speed in m/s</param>
    /// <param name="lwl">Length at waterline in m</param>
    /// <param name="g">Gravity in m/s²</param>
    /// <returns>Froude number</returns>
    decimal CalculateFroudeNumber(decimal speed, decimal lwl, decimal g = 9.80665m);
}

