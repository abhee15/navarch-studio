using System.Reflection;
using Shared.Attributes;

namespace Shared.Services;

/// <summary>
/// Service for converting DTOs between unit systems using reflection
/// </summary>
public class UnitConversionService : IUnitConversionService
{
    private readonly ILogger<UnitConversionService> _logger;

    public UnitConversionService(ILogger<UnitConversionService> logger)
    {
        _logger = logger;
    }

    public T ConvertDto<T>(T dto, string fromUnits, string toUnits) where T : class
    {
        if (dto == null) return dto;
        
        // If same unit system, no conversion needed
        if (string.Equals(fromUnits, toUnits, StringComparison.OrdinalIgnoreCase))
        {
            return dto;
        }

        try
        {
            ConvertObject(dto, fromUnits, toUnits);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting DTO from {FromUnits} to {ToUnits}", fromUnits, toUnits);
            return dto; // Return original on error
        }
    }

    public string GetPreferredUnits(string? headerValue, string defaultUnits = "SI")
    {
        if (string.IsNullOrWhiteSpace(headerValue))
            return defaultUnits;

        // Validate the unit system
        var units = headerValue.Trim();
        if (units.Equals("SI", StringComparison.OrdinalIgnoreCase) ||
            units.Equals("Imperial", StringComparison.OrdinalIgnoreCase))
        {
            return units;
        }

        _logger.LogWarning("Invalid unit system '{Units}' specified, using default '{Default}'", units, defaultUnits);
        return defaultUnits;
    }

    private void ConvertObject(object obj, string fromUnits, string toUnits)
    {
        if (obj == null) return;

        var type = obj.GetType();

        // Handle collections
        if (obj is System.Collections.IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
            {
                if (item != null && !item.GetType().IsPrimitive)
                {
                    ConvertObject(item, fromUnits, toUnits);
                }
            }
            return;
        }

        // Convert properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            if (!prop.CanRead || !prop.CanWrite) continue;

            var attribute = prop.GetCustomAttribute<ConvertibleAttribute>();
            if (attribute != null)
            {
                // Convert this property
                var value = prop.GetValue(obj);
                if (value is double doubleValue)
                {
                    var converted = ConvertValue(doubleValue, attribute.QuantityType, fromUnits, toUnits);
                    prop.SetValue(obj, converted);
                }
                else if (value is decimal decimalValue)
                {
                    var converted = ConvertValue((double)decimalValue, attribute.QuantityType, fromUnits, toUnits);
                    prop.SetValue(obj, (decimal)converted);
                }
                else if (value is double? nullableDouble && nullableDouble.HasValue)
                {
                    var converted = ConvertValue(nullableDouble.Value, attribute.QuantityType, fromUnits, toUnits);
                    prop.SetValue(obj, (double?)converted);
                }
            }
            else if (!prop.PropertyType.IsPrimitive && prop.PropertyType != typeof(string) && prop.PropertyType != typeof(Guid))
            {
                // Recursively convert nested objects
                var nestedValue = prop.GetValue(obj);
                if (nestedValue != null)
                {
                    ConvertObject(nestedValue, fromUnits, toUnits);
                }
            }
        }
    }

    private double ConvertValue(double value, string quantityType, string fromUnits, string toUnits)
    {
        // Conversion factors from SI to Imperial
        var conversionFactors = new Dictionary<string, double>
        {
            ["Length"] = 3.28084,        // m to ft
            ["Area"] = 10.7639,          // m² to ft²
            ["Volume"] = 35.3147,        // m³ to ft³
            ["Mass"] = 2.20462,          // kg to lb
            ["Density"] = 0.062428,      // kg/m³ to lb/ft³
            ["Inertia"] = 115.86,        // m⁴ to ft⁴
            ["Force"] = 0.224809,        // N to lbf
        };

        if (!conversionFactors.ContainsKey(quantityType))
        {
            _logger.LogWarning("Unknown quantity type '{QuantityType}', returning original value", quantityType);
            return value;
        }

        var factor = conversionFactors[quantityType];

        // Determine conversion direction
        bool siToImperial = fromUnits.Equals("SI", StringComparison.OrdinalIgnoreCase) &&
                           toUnits.Equals("Imperial", StringComparison.OrdinalIgnoreCase);
        bool imperialToSi = fromUnits.Equals("Imperial", StringComparison.OrdinalIgnoreCase) &&
                           toUnits.Equals("SI", StringComparison.OrdinalIgnoreCase);

        if (siToImperial)
        {
            return value * factor;
        }
        else if (imperialToSi)
        {
            return value / factor;
        }

        return value;
    }
}

