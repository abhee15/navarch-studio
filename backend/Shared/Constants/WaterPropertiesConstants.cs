namespace Shared.Constants;

/// <summary>
/// Extended water property data points for interpolation
/// Source: Experimental data (extended beyond ITTC anchor points)
/// Used to extend temperature range from -2°C to 32°C
///
/// UNITS: All values stored in SI base units (matches database schema)
/// - Temperature: Celsius (°C)
/// - Density: kg/m³
/// - Kinematic Viscosity: m²/s
///
/// Unit conversion is handled at API boundaries via UnitConversionService
/// </summary>
public static class WaterPropertiesConstants
{
    /// <summary>
    /// Extended seawater properties (beyond ITTC 0/15/30°C anchor points)
    /// Medium: Sea, Salinity: 35 PSU
    /// Units: SI (Temperature: °C, Density: kg/m³, Viscosity: m²/s)
    /// </summary>
    public static readonly WaterPropertyDataPoint[] ExtendedSeawaterProperties = new[]
    {
        new WaterPropertyDataPoint
        {
            Temperature_C = -2m,
            Density_kgm3 = 1028.1m,
            KinematicViscosity_m2s = 0.00000188m
        },
        new WaterPropertyDataPoint
        {
            Temperature_C = 3m,
            Density_kgm3 = 1027.5m,
            KinematicViscosity_m2s = 0.00000165m
        },
        new WaterPropertyDataPoint
        {
            Temperature_C = 8m,
            Density_kgm3 = 1026.9m,
            KinematicViscosity_m2s = 0.00000152m
        },
        new WaterPropertyDataPoint
        {
            Temperature_C = 13m,
            Density_kgm3 = 1026.2m,
            KinematicViscosity_m2s = 0.00000143m
        },
        new WaterPropertyDataPoint
        {
            Temperature_C = 18m,
            Density_kgm3 = 1025.6m,
            KinematicViscosity_m2s = 0.00000135m
        },
        new WaterPropertyDataPoint
        {
            Temperature_C = 23m,
            Density_kgm3 = 1024.9m,
            KinematicViscosity_m2s = 0.00000130m
        },
        new WaterPropertyDataPoint
        {
            Temperature_C = 28m,
            Density_kgm3 = 1024.2m,
            KinematicViscosity_m2s = 0.00000125m
        },
        new WaterPropertyDataPoint
        {
            Temperature_C = 32m,
            Density_kgm3 = 1023.5m,
            KinematicViscosity_m2s = 0.00000120m
        }
    };
}

/// <summary>
/// Water property data point for extended temperature range
/// </summary>
public class WaterPropertyDataPoint
{
    public decimal Temperature_C { get; init; }
    public decimal Density_kgm3 { get; init; }
    public decimal KinematicViscosity_m2s { get; init; }
}
