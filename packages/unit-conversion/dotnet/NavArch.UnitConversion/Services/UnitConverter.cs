using System.Globalization;
using NavArch.UnitConversion.Models;
using NavArch.UnitConversion.Providers;

namespace NavArch.UnitConversion.Services;

/// <summary>
/// Main unit conversion service
/// </summary>
public class UnitConverter : IUnitConverter
{
    private readonly XmlUnitSystemProvider _provider;
    private readonly Dictionary<string, UnitSystemDefinition> _systems;
    private readonly Dictionary<(string, string), decimal> _conversionMatrix;

    public UnitConverter(string? xmlConfigPath = null)
    {
        _provider = new XmlUnitSystemProvider(xmlConfigPath);
        _systems = _provider.LoadUnitSystems();
        _conversionMatrix = _provider.LoadConversionMatrix();
    }

    public decimal Convert(decimal value, string fromSystem, string toSystem, string category)
    {
        if (fromSystem == toSystem)
            return value;

        // Get unit IDs for the category in each system
        var fromUnit = GetUnitIdForCategory(fromSystem, category);
        var toUnit = GetUnitIdForCategory(toSystem, category);

        if (fromUnit == null || toUnit == null)
            throw new ArgumentException($"Category '{category}' not found in one of the unit systems");

        // Look up conversion factor
        if (_conversionMatrix.TryGetValue((fromUnit, toUnit), out var factor))
        {
            return value * factor;
        }

        throw new InvalidOperationException($"No conversion available from {fromUnit} to {toUnit}");
    }

    public Dictionary<string, decimal> ConvertBatch(
        Dictionary<string, (decimal value, string category)> values,
        string fromSystem,
        string toSystem)
    {
        var results = new Dictionary<string, decimal>();

        foreach (var (key, (value, category)) in values)
        {
            results[key] = Convert(value, fromSystem, toSystem, category);
        }

        return results;
    }

    public string GetUnitSymbol(string unitSystem, string category, string locale = "en")
    {
        var unit = GetUnitDefinition(unitSystem, category);
        return unit?.Symbol ?? string.Empty;
    }

    public string GetUnitName(string unitSystem, string category, string locale = "en", bool plural = false)
    {
        var unit = GetUnitDefinition(unitSystem, category);
        if (unit == null)
            return string.Empty;

        var names = plural ? unit.PluralNames : unit.Names;

        // Try to get localized name, fall back to English
        if (names.TryGetValue(locale, out var name))
            return name;

        if (names.TryGetValue("en", out var englishName))
            return englishName;

        return unit.Id;
    }

    public string GetCategoryName(string category, string locale = "en")
    {
        // Try to find the category in any system (they should have same names)
        foreach (var system in _systems.Values)
        {
            if (system.Categories.TryGetValue(category, out var categoryDef))
            {
                if (categoryDef.Names.TryGetValue(locale, out var name))
                    return name;

                if (categoryDef.Names.TryGetValue("en", out var englishName))
                    return englishName;

                return category;
            }
        }

        return category;
    }

    public List<UnitSystemInfo> GetAvailableUnitSystems(string locale = "en")
    {
        return _systems.Values.Select(system => GetUnitSystemInfo(system.Id, locale)).ToList();
    }

    public UnitSystemInfo GetUnitSystemInfo(string unitSystemId, string locale = "en")
    {
        if (!_systems.TryGetValue(unitSystemId, out var system))
            throw new ArgumentException($"Unit system '{unitSystemId}' not found");

        var name = system.Names.TryGetValue(locale, out var localizedName)
            ? localizedName
            : system.Names.GetValueOrDefault("en", unitSystemId);

        var description = system.Descriptions.TryGetValue(locale, out var localizedDesc)
            ? localizedDesc
            : system.Descriptions.GetValueOrDefault("en", string.Empty);

        return new UnitSystemInfo(
            unitSystemId,
            name,
            description,
            system.IsDefault,
            system.Categories.Keys.ToList());
    }

    public string FormatValue(decimal value, string unitSystem, string category, string locale = "en", int decimals = 2)
    {
        var culture = GetCultureInfo(locale);
        var formattedValue = value.ToString($"N{decimals}", culture);
        var symbol = GetUnitSymbol(unitSystem, category, locale);

        return $"{formattedValue} {symbol}";
    }

    private string? GetUnitIdForCategory(string unitSystem, string category)
    {
        if (!_systems.TryGetValue(unitSystem, out var system))
            return null;

        if (!system.Categories.TryGetValue(category, out var categoryDef))
            return null;

        // Return the first (and typically only) unit in this category
        return categoryDef.Units.FirstOrDefault()?.Id;
    }

    private UnitDefinition? GetUnitDefinition(string unitSystem, string category)
    {
        if (!_systems.TryGetValue(unitSystem, out var system))
            return null;

        if (!system.Categories.TryGetValue(category, out var categoryDef))
            return null;

        return categoryDef.Units.FirstOrDefault();
    }

    private static CultureInfo GetCultureInfo(string locale)
    {
        try
        {
            return CultureInfo.GetCultureInfo(locale);
        }
        catch
        {
            return CultureInfo.InvariantCulture;
        }
    }
}

