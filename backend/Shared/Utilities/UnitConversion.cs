namespace Shared.Utilities;

/// <summary>
/// Utility class for unit conversions between SI and Imperial systems
/// </summary>
public static class UnitConversion
{
    // Conversion factors (from SI base units)
    public const decimal METERS_TO_FEET = 3.28084m;
    public const decimal FEET_TO_METERS = 0.3048m;
    
    public const decimal SQUARE_METERS_TO_SQUARE_FEET = 10.7639m;
    public const decimal SQUARE_FEET_TO_SQUARE_METERS = 0.092903m;
    
    public const decimal CUBIC_METERS_TO_CUBIC_FEET = 35.3147m;
    public const decimal CUBIC_FEET_TO_CUBIC_METERS = 0.0283168m;
    
    public const decimal KG_TO_LB = 2.20462m;
    public const decimal LB_TO_KG = 0.453592m;
    
    public const decimal KG_PER_CUBIC_METER_TO_LB_PER_CUBIC_FOOT = 0.062428m;
    public const decimal LB_PER_CUBIC_FOOT_TO_KG_PER_CUBIC_METER = 16.0185m;
    
    public const decimal M4_TO_FT4 = 115.862m;
    public const decimal FT4_TO_M4 = 0.00863097m;

    /// <summary>
    /// Convert length from SI to Imperial or vice versa
    /// </summary>
    public static decimal ConvertLength(decimal value, string fromUnit, string toUnit)
    {
        if (fromUnit == toUnit) return value;
        
        if (fromUnit == "SI" && toUnit == "Imperial")
            return value * METERS_TO_FEET;
        
        if (fromUnit == "Imperial" && toUnit == "SI")
            return value * FEET_TO_METERS;
        
        return value;
    }

    /// <summary>
    /// Convert area from SI to Imperial or vice versa
    /// </summary>
    public static decimal ConvertArea(decimal value, string fromUnit, string toUnit)
    {
        if (fromUnit == toUnit) return value;
        
        if (fromUnit == "SI" && toUnit == "Imperial")
            return value * SQUARE_METERS_TO_SQUARE_FEET;
        
        if (fromUnit == "Imperial" && toUnit == "SI")
            return value * SQUARE_FEET_TO_SQUARE_METERS;
        
        return value;
    }

    /// <summary>
    /// Convert volume from SI to Imperial or vice versa
    /// </summary>
    public static decimal ConvertVolume(decimal value, string fromUnit, string toUnit)
    {
        if (fromUnit == toUnit) return value;
        
        if (fromUnit == "SI" && toUnit == "Imperial")
            return value * CUBIC_METERS_TO_CUBIC_FEET;
        
        if (fromUnit == "Imperial" && toUnit == "SI")
            return value * CUBIC_FEET_TO_CUBIC_METERS;
        
        return value;
    }

    /// <summary>
    /// Convert mass from SI to Imperial or vice versa
    /// </summary>
    public static decimal ConvertMass(decimal value, string fromUnit, string toUnit)
    {
        if (fromUnit == toUnit) return value;
        
        if (fromUnit == "SI" && toUnit == "Imperial")
            return value * KG_TO_LB;
        
        if (fromUnit == "Imperial" && toUnit == "SI")
            return value * LB_TO_KG;
        
        return value;
    }

    /// <summary>
    /// Convert density from SI to Imperial or vice versa
    /// </summary>
    public static decimal ConvertDensity(decimal value, string fromUnit, string toUnit)
    {
        if (fromUnit == toUnit) return value;
        
        if (fromUnit == "SI" && toUnit == "Imperial")
            return value * KG_PER_CUBIC_METER_TO_LB_PER_CUBIC_FOOT;
        
        if (fromUnit == "Imperial" && toUnit == "SI")
            return value * LB_PER_CUBIC_FOOT_TO_KG_PER_CUBIC_METER;
        
        return value;
    }

    /// <summary>
    /// Convert moment of inertia from SI to Imperial or vice versa
    /// </summary>
    public static decimal ConvertMomentOfInertia(decimal value, string fromUnit, string toUnit)
    {
        if (fromUnit == toUnit) return value;
        
        if (fromUnit == "SI" && toUnit == "Imperial")
            return value * M4_TO_FT4;
        
        if (fromUnit == "Imperial" && toUnit == "SI")
            return value * FT4_TO_M4;
        
        return value;
    }

    /// <summary>
    /// Get unit label for length
    /// </summary>
    public static string GetLengthUnit(string unitSystem) => unitSystem == "Imperial" ? "ft" : "m";

    /// <summary>
    /// Get unit label for area
    /// </summary>
    public static string GetAreaUnit(string unitSystem) => unitSystem == "Imperial" ? "ft²" : "m²";

    /// <summary>
    /// Get unit label for volume
    /// </summary>
    public static string GetVolumeUnit(string unitSystem) => unitSystem == "Imperial" ? "ft³" : "m³";

    /// <summary>
    /// Get unit label for mass
    /// </summary>
    public static string GetMassUnit(string unitSystem) => unitSystem == "Imperial" ? "lb" : "kg";

    /// <summary>
    /// Get unit label for density
    /// </summary>
    public static string GetDensityUnit(string unitSystem) => unitSystem == "Imperial" ? "lb/ft³" : "kg/m³";

    /// <summary>
    /// Get unit label for moment of inertia
    /// </summary>
    public static string GetMomentOfInertiaUnit(string unitSystem) => unitSystem == "Imperial" ? "ft⁴" : "m⁴";
}

