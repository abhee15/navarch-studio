using NavArch.UnitConversion.Models;

namespace NavArch.UnitConversion.Services;

/// <summary>
/// Interface for unit conversion operations
/// </summary>
public interface IUnitConverter
{
    /// <summary>
    /// Convert a value between unit systems for a specific category
    /// </summary>
    /// <param name="value">Value to convert</param>
    /// <param name="fromSystem">Source unit system (e.g., "SI")</param>
    /// <param name="toSystem">Target unit system (e.g., "Imperial")</param>
    /// <param name="category">Category of measurement (e.g., "Length")</param>
    /// <returns>Converted value</returns>
    decimal Convert(decimal value, string fromSystem, string toSystem, string category);

    /// <summary>
    /// Convert multiple values in batch
    /// </summary>
    Dictionary<string, decimal> ConvertBatch(
        Dictionary<string, (decimal value, string category)> values,
        string fromSystem,
        string toSystem);

    /// <summary>
    /// Get unit symbol for a category in a specific unit system
    /// </summary>
    string GetUnitSymbol(string unitSystem, string category, string locale = "en");

    /// <summary>
    /// Get unit name for a category in a specific unit system
    /// </summary>
    string GetUnitName(string unitSystem, string category, string locale = "en", bool plural = false);

    /// <summary>
    /// Get category name
    /// </summary>
    string GetCategoryName(string category, string locale = "en");

    /// <summary>
    /// Get all available unit systems
    /// </summary>
    List<UnitSystemInfo> GetAvailableUnitSystems(string locale = "en");

    /// <summary>
    /// Get unit system information
    /// </summary>
    UnitSystemInfo GetUnitSystemInfo(string unitSystemId, string locale = "en");

    /// <summary>
    /// Format a value with localized unit
    /// </summary>
    string FormatValue(decimal value, string unitSystem, string category, string locale = "en", int decimals = 2);
}

