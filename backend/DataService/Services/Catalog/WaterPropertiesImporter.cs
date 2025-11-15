using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DataService.Data;
using Microsoft.Extensions.Logging;
using Shared.Models;

namespace DataService.Services.Catalog;

/// <summary>
/// Service for importing water properties from CSV files
/// </summary>
public class WaterPropertiesImporter
{
    private readonly ILogger<WaterPropertiesImporter> _logger;

    public WaterPropertiesImporter(ILogger<WaterPropertiesImporter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parses water properties CSV and returns list of CatalogWaterProperty entities
    /// CSV format: temp_C, rho_kg_per_m3, nu_m2_per_s
    /// </summary>
    public async Task<List<CatalogWaterProperty>> ParseFromCsvAsync(Stream csvStream, string medium = "Sea", decimal salinityPSU = 35m, string sourceRef = "CSV Import", CancellationToken cancellationToken = default)
    {
        try
        {
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null,
                MissingFieldFound = null,
                IgnoreBlankLines = true
            });

            csv.Context.RegisterClassMap<WaterPropertyCsvRecordMap>();

            var properties = new List<CatalogWaterProperty>();
            var rowNumber = 1; // Start at 1 to account for header

            await foreach (var record in csv.GetRecordsAsync<WaterPropertyCsvRecord>(cancellationToken))
            {
                rowNumber++;

                // Validate temperature range
                if (record.TempC < -5 || record.TempC > 35)
                {
                    _logger.LogWarning("Skipping row {Row}: Temperature {Temp}°C out of valid range (-5 to 35°C)",
                        rowNumber, record.TempC);
                    continue;
                }

                // Validate density range
                if (record.RhoKgPerM3 < 995 || record.RhoKgPerM3 > 1030)
                {
                    _logger.LogWarning("Skipping row {Row}: Density {Density} kg/m³ out of valid range (995 to 1030 kg/m³)",
                        rowNumber, record.RhoKgPerM3);
                    continue;
                }

                // Validate viscosity range
                if (record.NuM2PerS < 0.7e-6m || record.NuM2PerS > 2.0e-6m)
                {
                    _logger.LogWarning("Skipping row {Row}: Viscosity {Viscosity} m²/s out of valid range (0.7e-6 to 2.0e-6 m²/s)",
                        rowNumber, record.NuM2PerS);
                    continue;
                }

                // Determine medium from density if not specified
                // Seawater typically > 1020 kg/m³, freshwater < 1010 kg/m³
                var actualMedium = medium;
                var actualSalinity = salinityPSU;
                if (record.RhoKgPerM3 > 1020)
                {
                    actualMedium = "Sea";
                    actualSalinity = 35m;
                }
                else if (record.RhoKgPerM3 < 1010)
                {
                    actualMedium = "Fresh";
                    actualSalinity = 0m;
                }

                var property = new CatalogWaterProperty
                {
                    Medium = actualMedium,
                    Temperature_C = record.TempC,
                    Salinity_PSU = actualSalinity,
                    Density_kgm3 = record.RhoKgPerM3,
                    KinematicViscosity_m2s = record.NuM2PerS,
                    SourceRef = sourceRef
                };

                properties.Add(property);
            }

            _logger.LogInformation("Parsed {Count} water property records from CSV (medium: {Medium})",
                properties.Count, medium);

            return properties;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing water properties CSV");
            throw new InvalidOperationException($"Failed to parse water properties CSV: {ex.Message}", ex);
        }
    }
}

// CSV record class
internal class WaterPropertyCsvRecord
{
    public decimal TempC { get; set; }
    public decimal RhoKgPerM3 { get; set; }
    public decimal NuM2PerS { get; set; }
}

// CSV Class Map for snake_case headers
internal sealed class WaterPropertyCsvRecordMap : ClassMap<WaterPropertyCsvRecord>
{
    public WaterPropertyCsvRecordMap()
    {
        Map(m => m.TempC).Name("temp_C");
        Map(m => m.RhoKgPerM3).Name("rho_kg_per_m3");
        Map(m => m.NuM2PerS).Name("nu_m2_per_s");
    }
}
