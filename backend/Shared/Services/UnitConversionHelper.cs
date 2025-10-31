using System.Reflection;
using NavArch.UnitConversion.Services;
using Shared.Attributes;
using Shared.DTOs;

namespace Shared.Services;

/// <summary>
/// Helper class for manual unit conversion in service layer
/// </summary>
public static class UnitConversionHelper
{
    /// <summary>
    /// Convert all [Convertible] properties in a DTO from its current units to SI
    /// </summary>
    public static void ConvertToSI(UnitAwareDto dto, IUnitConverter converter)
    {
        if (dto.Units == "SI") return; // Already in SI

        ConvertDto(dto, dto.Units, "SI", converter);
    }

    /// <summary>
    /// Convert all [Convertible] properties in a DTO from SI to target units
    /// </summary>
    public static void ConvertFromSI(UnitAwareDto dto, string targetUnits, IUnitConverter converter)
    {
        if (targetUnits == "SI") return; // Already in SI

        ConvertDto(dto, "SI", targetUnits, converter);
    }

    /// <summary>
    /// Convert all [Convertible] properties in a DTO between unit systems
    /// </summary>
    public static void ConvertDto(UnitAwareDto dto, string fromUnits, string toUnits, IUnitConverter converter)
    {
        if (fromUnits == toUnits) return;

        var properties = dto.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetCustomAttribute<ConvertibleAttribute>() != null);

        foreach (var prop in properties)
        {
            var attr = prop.GetCustomAttribute<ConvertibleAttribute>();
            if (attr == null) continue;

            var value = prop.GetValue(dto);
            if (value == null) continue;

            if (prop.PropertyType == typeof(decimal))
            {
                var decimalValue = (decimal)value;
                var converted = converter.Convert(decimalValue, fromUnits, toUnits, attr.QuantityType);
                prop.SetValue(dto, converted);
            }
            else if (prop.PropertyType == typeof(decimal?))
            {
                var nullableValue = (decimal?)value;
                if (nullableValue.HasValue)
                {
                    var converted = converter.Convert(nullableValue.Value, fromUnits, toUnits, attr.QuantityType);
                    prop.SetValue(dto, converted);
                }
            }
        }

        dto.Units = toUnits;
    }
}

