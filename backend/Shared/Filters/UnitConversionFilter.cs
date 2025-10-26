using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Shared.Services;
using System.Text.Json;

namespace Shared.Filters;

/// <summary>
/// Action filter that automatically converts response DTOs based on X-Preferred-Units header
/// </summary>
public class UnitConversionFilter : IAsyncActionFilter
{
    private readonly IUnitConversionService _conversionService;
    private readonly ILogger<UnitConversionFilter> _logger;

    public UnitConversionFilter(
        IUnitConversionService conversionService,
        ILogger<UnitConversionFilter> logger)
    {
        _conversionService = conversionService;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Execute the action
        var resultContext = await next();

        if (resultContext.Result is ObjectResult objectResult && objectResult.Value != null)
        {
            // Get preferred units from header
            var preferredUnits = context.HttpContext.Request.Headers["X-Preferred-Units"].FirstOrDefault();
            
            if (string.IsNullOrEmpty(preferredUnits))
            {
                // No preference specified, return as-is
                return;
            }

            try
            {
                // Get the source unit system from the response object
                var sourceUnits = GetSourceUnits(objectResult.Value);
                
                if (sourceUnits == null)
                {
                    _logger.LogDebug("No source unit system found in response, skipping conversion");
                    return;
                }

                // Validate preferred units
                preferredUnits = _conversionService.GetPreferredUnits(preferredUnits, sourceUnits);

                // If same units, no conversion needed
                if (string.Equals(sourceUnits, preferredUnits, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // Convert the response
                _logger.LogDebug("Converting response from {SourceUnits} to {PreferredUnits}", sourceUnits, preferredUnits);
                
                // For records/immutable DTOs, we need to serialize and deserialize with converted values
                var json = JsonSerializer.Serialize(objectResult.Value);
                var converted = JsonSerializer.Deserialize(json, objectResult.Value.GetType());
                
                if (converted != null)
                {
                    _conversionService.ConvertDto(converted, sourceUnits, preferredUnits);
                    
                    // Update the result
                    resultContext.Result = new ObjectResult(converted)
                    {
                        StatusCode = objectResult.StatusCode,
                        DeclaredType = objectResult.DeclaredType
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting units in response");
                // Don't fail the request, just return unconverted data
            }
        }
    }

    private string? GetSourceUnits(object obj)
    {
        if (obj == null) return null;

        // Try to get UnitsSystem property using reflection
        var type = obj.GetType();
        
        // Handle collections - get units from first item
        if (obj is System.Collections.IEnumerable enumerable and not string)
        {
            foreach (var item in enumerable)
            {
                var units = GetSourceUnits(item);
                if (units != null) return units;
                break; // Just check first item
            }
            return null;
        }

        // Try common property names
        var unitsProperty = type.GetProperty("UnitsSystem") ?? type.GetProperty("UnitSystem");
        if (unitsProperty != null && unitsProperty.PropertyType == typeof(string))
        {
            return unitsProperty.GetValue(obj) as string;
        }

        return null;
    }
}

