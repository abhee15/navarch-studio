using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataService.Services.Catalog;

public class CatalogVesselSeeder
{
    private readonly DataDbContext _context;
    private readonly VesselCatalogImporter _importer;
    private readonly ILogger<CatalogVesselSeeder> _logger;

    public CatalogVesselSeeder(
        DataDbContext context,
        VesselCatalogImporter importer,
        ILogger<CatalogVesselSeeder> logger)
    {
        _context = context;
        _importer = importer;
        _logger = logger;
    }

    public async Task SeedRealWorldCatalogAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if already seeded
            var existingCount = await _context.CatalogVesselsReal
                .Where(v => v.IsSystemData)
                .CountAsync(cancellationToken);

            if (existingCount > 0)
            {
                _logger.LogInformation(
                    "Real-world vessel catalog already seeded with {Count} vessels. Skipping.",
                    existingCount);
                return;
            }

            _logger.LogInformation("Starting real-world vessel catalog import...");

            // Read CSV from embedded resource or file path
            var csvPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data",
                "Seeds",
                "vessel_catalog_curated_600.csv");

            if (!File.Exists(csvPath))
            {
                _logger.LogWarning(
                    "Catalog CSV not found at {Path}. Catalog seeding skipped.",
                    csvPath);
                return;
            }

            var csvContent = await File.ReadAllTextAsync(csvPath, cancellationToken);

            // Import using VesselCatalogImporter
            var result = await _importer.ImportFromCsvAsync(csvContent, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "✅ Real-world catalog import successful. {Summary}",
                    result.GetSummary());

                // Log warnings if any
                if (result.Warnings.Any())
                {
                    _logger.LogWarning(
                        "Import completed with {Count} warnings:\n{Warnings}",
                        result.Warnings.Count,
                        string.Join("\n", result.Warnings.Take(10)));
                }
            }
            else
            {
                _logger.LogError(
                    "❌ Real-world catalog import failed. {Summary}\nErrors:\n{Errors}",
                    result.GetSummary(),
                    string.Join("\n", result.Errors.Take(20)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error during real-world catalog seeding");
        }
    }
}

