using System.Xml.Linq;
using NavArch.UnitConversion.Models;

namespace NavArch.UnitConversion.Providers;

/// <summary>
/// Loads unit system definitions from XML configuration
/// </summary>
public class XmlUnitSystemProvider
{
    private readonly string _xmlPath;
    private Dictionary<string, UnitSystemDefinition>? _cachedSystems;
    private Dictionary<(string, string), decimal>? _cachedConversions;

    public XmlUnitSystemProvider(string? xmlPath = null)
    {
        _xmlPath = xmlPath ?? Path.Combine(AppContext.BaseDirectory, "config", "unit-systems.xml");
    }

    /// <summary>
    /// Load all unit systems from XML
    /// </summary>
    public Dictionary<string, UnitSystemDefinition> LoadUnitSystems()
    {
        if (_cachedSystems != null)
            return _cachedSystems;

        var systems = new Dictionary<string, UnitSystemDefinition>();
        var doc = XDocument.Load(_xmlPath);

        foreach (var systemElement in doc.Root?.Elements("UnitSystem") ?? Enumerable.Empty<XElement>())
        {
            var system = ParseUnitSystem(systemElement);
            systems[system.Id] = system;
        }

        _cachedSystems = systems;
        return systems;
    }

    /// <summary>
    /// Load conversion matrix from XML
    /// </summary>
    public Dictionary<(string From, string To), decimal> LoadConversionMatrix()
    {
        if (_cachedConversions != null)
            return _cachedConversions;

        var conversions = new Dictionary<(string, string), decimal>();
        var doc = XDocument.Load(_xmlPath);

        var matrixElement = doc.Root?.Element("ConversionMatrix");
        if (matrixElement != null)
        {
            foreach (var conversion in matrixElement.Elements("Conversion"))
            {
                var from = conversion.Attribute("from")?.Value ?? string.Empty;
                var to = conversion.Attribute("to")?.Value ?? string.Empty;
                var factor = decimal.Parse(conversion.Attribute("factor")?.Value ?? "1");

                conversions[(from, to)] = factor;
            }
        }

        _cachedConversions = conversions;
        return conversions;
    }

    private UnitSystemDefinition ParseUnitSystem(XElement element)
    {
        var system = new UnitSystemDefinition
        {
            Id = element.Attribute("id")?.Value ?? string.Empty,
            IsDefault = bool.Parse(element.Attribute("isDefault")?.Value ?? "false")
        };

        // Parse names
        var namesElement = element.Element("Names");
        if (namesElement != null)
        {
            foreach (var name in namesElement.Elements("Name"))
            {
                var locale = name.Attribute("locale")?.Value ?? "en";
                system.Names[locale] = name.Value;
            }
        }

        // Parse descriptions
        var descriptionsElement = element.Element("Descriptions");
        if (descriptionsElement != null)
        {
            foreach (var desc in descriptionsElement.Elements("Description"))
            {
                var locale = desc.Attribute("locale")?.Value ?? "en";
                system.Descriptions[locale] = desc.Value;
            }
        }

        // Parse categories
        var categoriesElement = element.Element("Categories");
        if (categoriesElement != null)
        {
            foreach (var categoryElement in categoriesElement.Elements("Category"))
            {
                var category = ParseCategory(categoryElement);
                system.Categories[category.Id] = category;
            }
        }

        return system;
    }

    private CategoryDefinition ParseCategory(XElement element)
    {
        var category = new CategoryDefinition
        {
            Id = element.Attribute("id")?.Value ?? string.Empty
        };

        // Parse category names
        var namesElement = element.Element("Names");
        if (namesElement != null)
        {
            foreach (var name in namesElement.Elements("Name"))
            {
                var locale = name.Attribute("locale")?.Value ?? "en";
                category.Names[locale] = name.Value;
            }
        }

        // Parse units in category
        foreach (var unitElement in element.Elements("Unit"))
        {
            var unit = ParseUnit(unitElement);
            category.Units.Add(unit);
        }

        return category;
    }

    private UnitDefinition ParseUnit(XElement element)
    {
        var unit = new UnitDefinition
        {
            Id = element.Attribute("id")?.Value ?? string.Empty,
            Symbol = element.Attribute("symbol")?.Value ?? string.Empty,
            IsBase = bool.Parse(element.Attribute("isBase")?.Value ?? "false")
        };

        // Parse unit names
        var namesElement = element.Element("Names");
        if (namesElement != null)
        {
            foreach (var name in namesElement.Elements("Name"))
            {
                var locale = name.Attribute("locale")?.Value ?? "en";
                unit.Names[locale] = name.Value;
            }
        }

        // Parse plural names
        var pluralElement = element.Element("PluralNames");
        if (pluralElement != null)
        {
            foreach (var name in pluralElement.Elements("Name"))
            {
                var locale = name.Attribute("locale")?.Value ?? "en";
                unit.PluralNames[locale] = name.Value;
            }
        }

        // Parse conversion info
        var conversionElement = element.Element("ConversionToSI");
        if (conversionElement != null)
        {
            unit.ConversionFactor = decimal.Parse(conversionElement.Attribute("factor")?.Value ?? "1");
            unit.BaseUnit = conversionElement.Attribute("baseUnit")?.Value;
        }

        return unit;
    }

    /// <summary>
    /// Clear cached data (useful for testing)
    /// </summary>
    public void ClearCache()
    {
        _cachedSystems = null;
        _cachedConversions = null;
    }
}

