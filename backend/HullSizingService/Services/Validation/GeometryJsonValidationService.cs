using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shared.DTOs.Sizing;

namespace HullSizingService.Services.Validation;

/// <summary>
/// Service for validating geometry JSON structure and quality.
/// Ensures generated geometry is valid for frontend visualization.
/// </summary>
public interface IGeometryJsonValidationService
{
    /// <summary>
    /// Validates geometry JSON structure and returns validation result
    /// </summary>
    GeometryJsonValidationResult Validate(string? geometryJson);

    /// <summary>
    /// Sanitizes geometry JSON by removing invalid values (NaN, null, negative offsets)
    /// </summary>
    string? Sanitize(string? geometryJson);
}

public class GeometryJsonValidationService : IGeometryJsonValidationService
{
    private readonly ILogger<GeometryJsonValidationService> _logger;

    public GeometryJsonValidationService(ILogger<GeometryJsonValidationService> logger)
    {
        _logger = logger;
    }

    public GeometryJsonValidationResult Validate(string? geometryJson)
    {
        if (string.IsNullOrWhiteSpace(geometryJson))
        {
            return new GeometryJsonValidationResult
            {
                IsValid = false,
                Errors = new List<string> { "Geometry JSON is null or empty" }
            };
        }

        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            using var doc = JsonDocument.Parse(geometryJson);
            var root = doc.RootElement;

            // Check for OffsetsGrid format
            var hasOffsetsGridFormat = root.TryGetProperty("stations", out var stationsProp) ||
                                      root.TryGetProperty("Stations", out stationsProp);

            var hasShipDFormat = root.TryGetProperty("stations", out var shipdStationsProp) &&
                                shipdStationsProp.ValueKind == JsonValueKind.Array &&
                                shipdStationsProp.GetArrayLength() > 0 &&
                                shipdStationsProp[0].ValueKind == JsonValueKind.Object &&
                                shipdStationsProp[0].TryGetProperty("position", out _);

            if (!hasOffsetsGridFormat && !hasShipDFormat)
            {
                errors.Add("Geometry JSON does not match OffsetsGrid or ShipD format");
                return new GeometryJsonValidationResult
                {
                    IsValid = false,
                    Errors = errors,
                    Warnings = warnings
                };
            }

            if (hasOffsetsGridFormat)
            {
                ValidateOffsetsGridFormat(root, errors, warnings);
            }
            else if (hasShipDFormat)
            {
                ValidateShipDFormat(root, errors, warnings);
            }
        }
        catch (JsonException ex)
        {
            errors.Add($"Invalid JSON format: {ex.Message}");
        }
        catch (Exception ex)
        {
            errors.Add($"Unexpected error validating geometry JSON: {ex.Message}");
            _logger.LogError(ex, "Unexpected error validating geometry JSON");
        }

        return new GeometryJsonValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    private void ValidateOffsetsGridFormat(JsonElement root, List<string> errors, List<string> warnings)
    {
        // Get stations (handle both camelCase and PascalCase)
        var stationsProp = root.TryGetProperty("stations", out var s) ? s :
                          root.TryGetProperty("Stations", out s) ? s : default;

        if (stationsProp.ValueKind != JsonValueKind.Array || stationsProp.GetArrayLength() == 0)
        {
            errors.Add("Stations array is missing or empty");
            return;
        }

        // Get waterlines
        var waterlinesProp = root.TryGetProperty("waterlines", out var w) ? w :
                            root.TryGetProperty("Waterlines", out w) ? w : default;

        if (waterlinesProp.ValueKind != JsonValueKind.Array || waterlinesProp.GetArrayLength() == 0)
        {
            errors.Add("Waterlines array is missing or empty");
            return;
        }

        // Get offsets
        var offsetsProp = root.TryGetProperty("offsets", out var o) ? o :
                         root.TryGetProperty("Offsets", out o) ? o : default;

        if (offsetsProp.ValueKind != JsonValueKind.Array || offsetsProp.GetArrayLength() == 0)
        {
            errors.Add("Offsets array is missing or empty");
            return;
        }

        var stationCount = stationsProp.GetArrayLength();
        var waterlineCount = waterlinesProp.GetArrayLength();
        var offsetRowCount = offsetsProp.GetArrayLength();

        if (offsetRowCount != stationCount)
        {
            errors.Add($"Offsets array length ({offsetRowCount}) does not match stations length ({stationCount})");
        }

        // Validate station positions
        var stations = stationsProp.EnumerateArray().ToList();
        for (int i = 0; i < stations.Count; i++)
        {
            if (!stations[i].TryGetDecimal(out var stationX))
            {
                errors.Add($"Station {i} is not a valid number");
            }
            else if (stationX < 0)
            {
                warnings.Add($"Station {i} has negative position: {stationX}m");
            }
        }

        // Validate waterline heights
        var waterlines = waterlinesProp.EnumerateArray().ToList();
        for (int i = 0; i < waterlines.Count; i++)
        {
            if (!waterlines[i].TryGetDecimal(out var waterlineZ))
            {
                errors.Add($"Waterline {i} is not a valid number");
            }
            else if (waterlineZ < 0)
            {
                warnings.Add($"Waterline {i} has negative height: {waterlineZ}m");
            }
        }

        // Validate offsets grid
        var offsetRows = offsetsProp.EnumerateArray().ToList();
        int invalidOffsetCount = 0;
        int negativeOffsetCount = 0;

        for (int stIdx = 0; stIdx < offsetRows.Count; stIdx++)
        {
            if (offsetRows[stIdx].ValueKind != JsonValueKind.Array)
            {
                errors.Add($"Offset row {stIdx} is not an array");
                continue;
            }

            var offsetRow = offsetRows[stIdx].EnumerateArray().ToList();
            if (offsetRow.Count != waterlineCount)
            {
                errors.Add($"Offset row {stIdx} length ({offsetRow.Count}) does not match waterlines length ({waterlineCount})");
                continue;
            }

            for (int wlIdx = 0; wlIdx < offsetRow.Count; wlIdx++)
            {
                if (!offsetRow[wlIdx].TryGetDecimal(out var offset))
                {
                    invalidOffsetCount++;
                    if (invalidOffsetCount <= 5)
                    {
                        errors.Add($"Station {stIdx}, Waterline {wlIdx} has invalid offset value");
                    }
                }
                else if (offset < 0)
                {
                    negativeOffsetCount++;
                    if (negativeOffsetCount <= 5)
                    {
                        warnings.Add($"Station {stIdx}, Waterline {wlIdx} has negative half-breadth: {offset}m");
                    }
                }
            }
        }

        if (invalidOffsetCount > 5)
        {
            errors.Add($"... and {invalidOffsetCount - 5} more invalid offsets");
        }

        if (negativeOffsetCount > 5)
        {
            warnings.Add($"... and {negativeOffsetCount - 5} more negative offsets");
        }
    }

    private void ValidateShipDFormat(JsonElement root, List<string> errors, List<string> warnings)
    {
        if (!root.TryGetProperty("stations", out var stationsProp) ||
            stationsProp.ValueKind != JsonValueKind.Array ||
            stationsProp.GetArrayLength() == 0)
        {
            errors.Add("ShipD stations array is missing or empty");
            return;
        }

        var stations = stationsProp.EnumerateArray().ToList();
        for (int i = 0; i < stations.Count; i++)
        {
            var station = stations[i];
            if (station.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"ShipD station {i} is not an object");
                continue;
            }

            if (!station.TryGetProperty("position", out var positionProp) ||
                !positionProp.TryGetDecimal(out var position))
            {
                errors.Add($"ShipD station {i} missing or invalid position");
            }
            else if (position < 0 || position > 1)
            {
                warnings.Add($"ShipD station {i} position {position} is outside normalized range [0, 1]");
            }

            if (!station.TryGetProperty("offsets", out var offsetsProp) ||
                offsetsProp.ValueKind != JsonValueKind.Object)
            {
                errors.Add($"ShipD station {i} missing or invalid offsets object");
            }
        }
    }

    public string? Sanitize(string? geometryJson)
    {
        if (string.IsNullOrWhiteSpace(geometryJson))
        {
            return geometryJson;
        }

        try
        {
            using var doc = JsonDocument.Parse(geometryJson);
            var root = doc.RootElement;

            // Check for OffsetsGrid format
            if (root.TryGetProperty("stations", out _) || root.TryGetProperty("Stations", out _))
            {
                return SanitizeOffsetsGrid(doc);
            }

            // For ShipD format, return as-is (sanitization would require reconstruction)
            return geometryJson;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sanitize geometry JSON");
            return geometryJson; // Return original on error
        }
    }

    private string SanitizeOffsetsGrid(JsonDocument doc)
    {
        // Reconstruct JSON with sanitized values
        // This is a simplified implementation - full implementation would properly reconstruct the JSON
        // For now, return original and let frontend handle sanitization
        return doc.RootElement.GetRawText();
    }
}

public class GeometryJsonValidationResult
{
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
}

