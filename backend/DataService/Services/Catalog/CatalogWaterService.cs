using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;
using Shared.Models;

namespace DataService.Services.Catalog;

/// <summary>
/// Service for looking up water properties with temperature interpolation
/// Provides ITTC 7.5-02-01-03 based water property data
/// </summary>
public class CatalogWaterService
{
    private readonly DataDbContext _context;
    private readonly ILogger<CatalogWaterService> _logger;

    public CatalogWaterService(DataDbContext context, ILogger<CatalogWaterService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets water properties at a specific temperature and salinity with linear interpolation
    /// </summary>
    /// <param name="temperatureC">Temperature in Celsius (0-30°C range)</param>
    /// <param name="salinityPSU">Salinity in PSU (0 for fresh, 35 for seawater)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Water properties with density and kinematic viscosity</returns>
    /// <exception cref="ArgumentException">Thrown if temperature is outside 0-30°C range</exception>
    public async Task<WaterPropertiesDto> GetWaterPropertiesAsync(
        decimal temperatureC,
        decimal salinityPSU = 35,
        CancellationToken cancellationToken = default)
    {
        // Determine medium based on salinity
        var medium = salinityPSU < 1 ? "Fresh" : "Sea";

        // Validate temperature range
        if (temperatureC < 0 || temperatureC > 30)
        {
            throw new ArgumentException(
                $"Temperature {temperatureC}°C is outside supported range (0-30°C)");
        }

        // Get anchor points for this medium
        var anchorPoints = await _context.CatalogWaterProperties
            .Where(w => w.Medium == medium)
            .OrderBy(w => w.Temperature_C)
            .ToListAsync(cancellationToken);

        if (anchorPoints.Count == 0)
        {
            throw new InvalidOperationException(
                $"No anchor points found for medium: {medium}");
        }

        // Check for exact match
        var exactMatch = anchorPoints.FirstOrDefault(p => p.Temperature_C == temperatureC);
        if (exactMatch != null)
        {
            _logger.LogDebug(
                "Exact match found for {Medium} at {Temp}°C: ρ={Density}, ν={Viscosity}",
                medium, temperatureC, exactMatch.Density_kgm3, exactMatch.KinematicViscosity_m2s);

            return new WaterPropertiesDto
            {
                Medium = exactMatch.Medium,
                Temperature_C = exactMatch.Temperature_C,
                Salinity_PSU = exactMatch.Salinity_PSU,
                Density = exactMatch.Density_kgm3,
                KinematicViscosity_m2s = exactMatch.KinematicViscosity_m2s,
                IsInterpolated = false,
                SourceRef = exactMatch.SourceRef,
                Units = "SI"
            };
        }

        // Linear interpolation
        var lowerPoint = anchorPoints.LastOrDefault(p => p.Temperature_C < temperatureC);
        var upperPoint = anchorPoints.FirstOrDefault(p => p.Temperature_C > temperatureC);

        if (lowerPoint == null || upperPoint == null)
        {
            throw new InvalidOperationException(
                $"Cannot interpolate: missing anchor points for {medium} at {temperatureC}°C");
        }

        // Linear interpolation: y = y1 + (x - x1) * (y2 - y1) / (x2 - x1)
        var x1 = (double)lowerPoint.Temperature_C;
        var x2 = (double)upperPoint.Temperature_C;
        var x = (double)temperatureC;

        var density1 = (double)lowerPoint.Density_kgm3;
        var density2 = (double)upperPoint.Density_kgm3;
        var interpolatedDensity = density1 + (x - x1) * (density2 - density1) / (x2 - x1);

        var viscosity1 = (double)lowerPoint.KinematicViscosity_m2s;
        var viscosity2 = (double)upperPoint.KinematicViscosity_m2s;
        var interpolatedViscosity = viscosity1 + (x - x1) * (viscosity2 - viscosity1) / (x2 - x1);

        _logger.LogDebug(
            "Interpolated {Medium} at {Temp}°C between {Lower}°C and {Upper}°C: ρ={Density}, ν={Viscosity}",
            medium, temperatureC, lowerPoint.Temperature_C, upperPoint.Temperature_C,
            interpolatedDensity, interpolatedViscosity);

        return new WaterPropertiesDto
        {
            Medium = medium,
            Temperature_C = temperatureC,
            Salinity_PSU = salinityPSU,
            Density = (decimal)interpolatedDensity,
            KinematicViscosity_m2s = (decimal)interpolatedViscosity,
            IsInterpolated = true,
            SourceRef = $"ITTC 7.5-02-01-03 (interpolated between {lowerPoint.Temperature_C}°C and {upperPoint.Temperature_C}°C)",
            Units = "SI"
        };
    }

    /// <summary>
    /// Gets all anchor points for a medium
    /// </summary>
    public async Task<List<CatalogWaterProperty>> GetAnchorPointsAsync(
        string medium,
        CancellationToken cancellationToken = default)
    {
        return await _context.CatalogWaterProperties
            .Where(w => w.Medium == medium)
            .OrderBy(w => w.Temperature_C)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all available water properties
    /// </summary>
    public async Task<List<CatalogWaterProperty>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CatalogWaterProperties
            .OrderBy(w => w.Medium)
            .ThenBy(w => w.Temperature_C)
            .ToListAsync(cancellationToken);
    }

}
