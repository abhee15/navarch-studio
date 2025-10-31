using System.Collections;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using NavArch.UnitConversion.Services;
using Shared.Attributes;
using Shared.DTOs;

namespace Shared.Filters;

/// <summary>
/// Filter that automatically converts unit-aware DTOs from SI to user's preferred units
/// </summary>
public class UnitConversionFilter : IAsyncActionFilter
{
    private readonly IUnitConverter _converter;
    private readonly ILogger<UnitConversionFilter> _logger;

    public UnitConversionFilter(IUnitConverter converter, ILogger<UnitConversionFilter> logger)
    {
        _converter = converter;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Execute the action
        var resultContext = await next();

        // Only convert on successful responses
        if (resultContext.Result is ObjectResult objectResult && objectResult.StatusCode is >= 200 and < 300)
        {
            var preferredUnits = context.HttpContext.Items["PreferredUnits"]?.ToString() ?? "SI";

            // Convert the response if it's a UnitAwareDto
            if (objectResult.Value != null)
            {
                ConvertResponseToPreferredUnits(objectResult.Value, preferredUnits);
            }
        }
    }

    private void ConvertResponseToPreferredUnits(object obj, string targetUnits)
    {
        if (obj == null) return;

        var objType = obj.GetType();

        // Handle single UnitAwareDto
        if (obj is UnitAwareDto dto)
        {
            ConvertDto(dto, targetUnits);
            return;
        }

        // Handle collections of UnitAwareDto
        if (obj is IEnumerable enumerable && objType.IsGenericType)
        {
            var genericArg = objType.GetGenericArguments().FirstOrDefault();
            if (genericArg != null && typeof(UnitAwareDto).IsAssignableFrom(genericArg))
            {
                foreach (var item in enumerable)
                {
                    if (item is UnitAwareDto itemDto)
                    {
                        ConvertDto(itemDto, targetUnits);
                    }
                }
                return;
            }
        }

        // Handle anonymous objects or objects with nested UnitAwareDto properties
        // Check all properties recursively for UnitAwareDto instances
        try
        {
            var properties = objType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (!prop.CanRead) continue;

                var value = prop.GetValue(obj);
                if (value == null) continue;

                // If property is a UnitAwareDto, convert it
                if (value is UnitAwareDto propDto)
                {
                    ConvertDto(propDto, targetUnits);
                }
                // If property is a collection of UnitAwareDto, convert each item
                else if (value is IEnumerable propEnumerable)
                {
                    foreach (var item in propEnumerable)
                    {
                        if (item is UnitAwareDto itemDto)
                        {
                            ConvertDto(itemDto, targetUnits);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - conversion is best-effort
            _logger.LogWarning(ex, "Error converting nested properties in response object of type {Type}", objType.Name);
        }
    }

    private void ConvertDto(UnitAwareDto dto, string targetUnits)
    {
        // Default to SI if Units is null or empty
        if (string.IsNullOrWhiteSpace(dto.Units))
        {
            dto.Units = "SI";
        }

        if (dto.Units == targetUnits) return;

        var sourceUnits = dto.Units;

        // Convert all properties with [Convertible] attribute
        var properties = dto.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetCustomAttribute<ConvertibleAttribute>() != null);

        foreach (var prop in properties)
        {
            var attr = prop.GetCustomAttribute<ConvertibleAttribute>();
            if (attr == null) continue;

            var value = prop.GetValue(dto);
            if (value == null) continue;

            try
            {
                if (prop.PropertyType == typeof(decimal))
                {
                    var decimalValue = (decimal)value;
                    var converted = _converter.Convert(decimalValue, sourceUnits, targetUnits, attr.QuantityType);
                    prop.SetValue(dto, converted);
                }
                else if (prop.PropertyType == typeof(decimal?))
                {
                    var nullableValue = (decimal?)value;
                    if (nullableValue.HasValue)
                    {
                        var converted = _converter.Convert(nullableValue.Value, sourceUnits, targetUnits, attr.QuantityType);
                        prop.SetValue(dto, converted);
                    }
                }
                else if (prop.PropertyType == typeof(List<decimal>))
                {
                    var list = (List<decimal>)value;
                    var convertedList = list.Select(v => _converter.Convert(v, sourceUnits, targetUnits, attr.QuantityType)).ToList();
                    prop.SetValue(dto, convertedList);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert property {PropertyName} from {SourceUnits} to {TargetUnits}",
                    prop.Name, sourceUnits, targetUnits);
            }
        }

        // Update the Units property to reflect the conversion
        dto.Units = targetUnits;
    }
}
