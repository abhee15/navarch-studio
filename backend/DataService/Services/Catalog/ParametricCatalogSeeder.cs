using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DataService.Services.Catalog;

/// <summary>
/// Seeds parametric hull catalog from ShipD dataset on startup
/// </summary>
public class ParametricCatalogSeeder
{
    private readonly DataDbContext _context;
    private readonly ParametricCatalogImporter _importer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ParametricCatalogSeeder> _logger;

    public ParametricCatalogSeeder(
        DataDbContext context,
        ParametricCatalogImporter importer,
        IConfiguration configuration,
        ILogger<ParametricCatalogSeeder> logger)
    {
        _context = context;
        _importer = importer;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Seed parametric catalog if empty
    /// Starts with 5K from Constrained_Set_1 for Phase 2A
    /// </summary>
    public async Task SeedParametricCatalogAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if already seeded
            var existingCount = await _context.ParametricHulls.CountAsync(cancellationToken);
            if (existingCount > 0)
            {
                _logger.LogInformation(
                    "[SEED] Parametric catalog already contains {Count} hulls. Skipping import.",
                    existingCount);
                return;
            }

            _logger.LogInformation("[SEED] Parametric catalog is empty. Starting import...");

            // Get dataset path from configuration
            var dataPath = _configuration["DataPath"] ?? "Data";
            var shipDPath = Path.Combine(dataPath, "Ship_D_Dataset");

            // Phase 2A: Import 5K from Constrained_Set_1 (every 2nd row)
            var dataset1Path = Path.Combine(shipDPath, "Constrained_Randomized_Set_1");
            
            if (!Directory.Exists(dataset1Path))
            {
                _logger.LogWarning(
                    "[SEED] ShipD dataset not found at {Path}. Skipping parametric catalog seeding.",
                    dataset1Path);
                return;
            }

            _logger.LogInformation("[SEED] Importing 5K hulls from Constrained_Set_1 (Phase 2A prototype)...");

            var result = await _importer.ImportFromShipDFolderAsync(
                folderPath: dataset1Path,
                datasetName: "Constrained_Randomized_Set_1",
                maxRows: 5000,    // Phase 2A: 5K prototype
                skipRows: 2,      // Every 2nd row = 5K from 10K
                cancellationToken: cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "[SEED] ✅ Parametric catalog seeded successfully!\n" +
                    "       Dataset: {Dataset}\n" +
                    "       Imported: {Imported} hulls\n" +
                    "       Skipped: {Skipped} rows\n" +
                    "       Time: {Time}ms\n" +
                    "       Ready for ML/Parametric solver!",
                    result.DatasetName,
                    result.ImportedRows,
                    result.SkippedRows,
                    result.ElapsedMs);
            }
            else
            {
                _logger.LogError(
                    "[SEED] ❌ Parametric catalog import failed!\n" +
                    "       Errors: {Errors}",
                    string.Join("; ", result.Errors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SEED] Failed to seed parametric catalog");
        }
    }

    /// <summary>
    /// Get catalog statistics for monitoring
    /// </summary>
    public async Task<CatalogStats> GetCatalogStatsAsync()
    {
        var stats = new CatalogStats
        {
            TotalHulls = await _context.ParametricHulls.CountAsync(),
            ByDataset = await _context.ParametricHulls
                .GroupBy(h => h.DatasetSource)
                .Select(g => new { Source = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Source, x => x.Count),
            AvgCb = await _context.ParametricHulls.AverageAsync(h => (double)h.CbDerived),
            CbRange = new
            {
                Min = await _context.ParametricHulls.MinAsync(h => h.CbDerived),
                Max = await _context.ParametricHulls.MaxAsync(h => h.CbDerived)
            }
        };

        return stats;
    }
}

public class CatalogStats
{
    public int TotalHulls { get; set; }
    public Dictionary<string, int> ByDataset { get; set; } = new();
    public double AvgCb { get; set; }
    public object? CbRange { get; set; }
}

