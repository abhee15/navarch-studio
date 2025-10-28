using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using NavArch.UnitConversion.Services;
using Shared.Attributes;
using Shared.DTOs;
using System.Collections;
using System.Reflection;

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
            }
        }
    }

    private void ConvertDto(UnitAwareDto dto, string targetUnits)
    {
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
