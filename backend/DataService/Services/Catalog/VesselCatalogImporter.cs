using System.Globalization;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Models;

namespace DataService.Services.Catalog;

public class VesselCatalogImporter
{
    private readonly DataDbContext _context;
    private readonly ILogger<VesselCatalogImporter> _logger;

    public VesselCatalogImporter(DataDbContext context, ILogger<VesselCatalogImporter> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ImportResult> ImportFromCsvAsync(
        string csvContent,
        CancellationToken cancellationToken = default)
    {
        var result = new ImportResult();
        var vesselsToImport = new List<CatalogVesselReal>();

        try
        {
            using var reader = new StringReader(csvContent);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,  // Don't throw on missing headers
                MissingFieldFound = null  // Don't throw on missing fields
            });

            var records = csv.GetRecords<VesselCatalogCsvRow>();

            foreach (var record in records)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // Validate required fields
                    if (!ValidateRequiredFields(record, out var validationError))
                    {
                        result.SkippedRows++;
                        result.Errors.Add($"Row {result.TotalRows + 1}: {validationError}");
                        result.TotalRows++;
                        continue;
                    }

                    // Handle missing optional fields
                    var vessel = new CatalogVesselReal
                    {
                        VesselId = record.VesselID,
                        VesselType = record.VesselType,
                        LppM = record.Lpp_m,
                        BeamM = record.Beam_m,
                        DraftM = record.Draft_m,
                        DepthM = record.Depth_m ?? EstimateDepth(record.Draft_m),
                        DisplacementT = record.Displacement_t,
                        Cb = record.CB,
                        Cp = record.CP,
                        Cm = record.CM ?? EstimateCm(record.CB, record.CP),
                        Cw = record.CW,
                        ServiceSpeedMs = record.ServiceSpeed_ms,
                        DwtT = record.DWT_t,
                        EngineType = record.EngineType,
                        YearBuilt = record.YearBuilt,
                        Source = record.Source,
                        DataQuality = record.DataQuality,
                        HullGeometryFile = string.IsNullOrWhiteSpace(record.HullGeometryFile) ||
                                          record.HullGeometryFile == "None"
                                          ? null : record.HullGeometryFile,
                        ResistanceCurve = record.ResistanceCurve,  // Already JSON string
                        IsSystemData = true,  // These are system-seeded vessels
                        CreatedBy = null,  // System data
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    // Log if we estimated any values
                    if (!record.Depth_m.HasValue)
                    {
                        result.Warnings.Add($"{record.VesselID}: Estimated Depth = {vessel.DepthM:F3}m from Draft");
                    }

                    if (!record.CM.HasValue && vessel.Cm.HasValue)
                    {
                        result.Warnings.Add($"{record.VesselID}: Estimated CM = {vessel.Cm:F4} from CB/CP");
                    }

                    vesselsToImport.Add(vessel);
                }
                catch (Exception ex)
                {
                    result.SkippedRows++;
                    result.Errors.Add($"Row {result.TotalRows + 1} ({record.VesselID}): {ex.Message}");
                }

                result.TotalRows++;
            }

            // Bulk insert to database
            if (vesselsToImport.Any())
            {
                // Check for duplicates
                var existingIds = await _context.CatalogVesselsReal
                    .Where(v => vesselsToImport.Select(n => n.VesselId).Contains(v.VesselId))
                    .Select(v => v.VesselId)
                    .ToListAsync(cancellationToken);

                if (existingIds.Any())
                {
                    _logger.LogWarning(
                        "Found {Count} duplicate vessel IDs. Will skip: {Ids}",
                        existingIds.Count,
                        string.Join(", ", existingIds.Take(10)));

                    vesselsToImport = vesselsToImport
                        .Where(v => !existingIds.Contains(v.VesselId))
                        .ToList();

                    result.SkippedRows += existingIds.Count;
                }

                // Only count vessels that actually get imported (after duplicate check)
                result.ImportedRows = vesselsToImport.Count;

                await _context.CatalogVesselsReal.AddRangeAsync(vesselsToImport, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            result.Success = true;
            _logger.LogInformation(
                "Catalog import complete. Imported: {Imported}, Skipped: {Skipped}, Warnings: {Warnings}",
                result.ImportedRows, result.SkippedRows, result.Warnings.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during catalog import");
            result.Success = false;
            result.Errors.Add($"Fatal error: {ex.Message}");
            return result;
        }
    }

    private bool ValidateRequiredFields(VesselCatalogCsvRow record, out string error)
    {
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(record.VesselID))
        {
            error = "VesselID is required";
            return false;
        }

        if (string.IsNullOrWhiteSpace(record.VesselType))
        {
            error = "VesselType is required";
            return false;
        }

        if (record.Lpp_m <= 0)
        {
            error = "Lpp_m must be positive";
            return false;
        }

        if (record.Beam_m <= 0)
        {
            error = "Beam_m must be positive";
            return false;
        }

        if (record.Draft_m <= 0)
        {
            error = "Draft_m must be positive";
            return false;
        }

        if (record.Displacement_t <= 0)
        {
            error = "Displacement_t must be positive";
            return false;
        }

        if (record.CB < 0.3m || record.CB > 0.95m)
        {
            error = $"CB must be between 0.3 and 0.95, got {record.CB}";
            return false;
        }

        return true;
    }

    private decimal? EstimateDepth(decimal draft)
    {
        // Typical freeboard is 25-50% of draft
        // Depth = Draft + Freeboard
        // Conservative estimate: Depth ≈ 1.5 × Draft
        return draft * 1.5m;
    }

    private decimal? EstimateCm(decimal cb, decimal? cp)
    {
        if (!cp.HasValue)
            return null;

        // Midship coefficient CM = CB / CP
        // But CP is often close to 1.0, so CM ≈ CB / CP
        if (cp.Value > 0)
            return Math.Min(cb / cp.Value, 1.0m);

        return null;
    }
}

/// <summary>
/// CSV row mapping for vessel_catalog_curated_600.csv
/// </summary>
public class VesselCatalogCsvRow
{
    public string VesselID { get; set; } = string.Empty;
    public string VesselType { get; set; } = string.Empty;
    public decimal Lpp_m { get; set; }
    public decimal Beam_m { get; set; }
    public decimal Draft_m { get; set; }
    public decimal? Depth_m { get; set; }
    public decimal Displacement_t { get; set; }
    public decimal CB { get; set; }
    public decimal? CP { get; set; }
    public decimal? CM { get; set; }
    public decimal? CW { get; set; }
    public decimal? ServiceSpeed_ms { get; set; }
    public decimal? DWT_t { get; set; }
    public string? EngineType { get; set; }
    public int? YearBuilt { get; set; }
    public string? Source { get; set; }
    public string? DataQuality { get; set; }
    public string? HullGeometryFile { get; set; }
    public string? ResistanceCurve { get; set; }  // JSON string from CSV
}

/// <summary>
/// Result of catalog import operation
/// </summary>
public class ImportResult
{
    public bool Success { get; set; }
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int SkippedRows { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public string GetSummary()
    {
        return $"Import {(Success ? "succeeded" : "failed")}. " +
               $"Total: {TotalRows}, Imported: {ImportedRows}, Skipped: {SkippedRows}, " +
               $"Errors: {Errors.Count}, Warnings: {Warnings.Count}";
    }
}
