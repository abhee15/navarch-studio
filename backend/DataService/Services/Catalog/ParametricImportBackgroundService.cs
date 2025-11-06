using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataService.Services.Catalog;

/// <summary>
/// Background service to import parametric hulls from ShipD dataset
/// Runs after startup to import 30K (Phase 2B) or 82K (Phase 2C) hulls
/// </summary>
public class ParametricImportBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ParametricImportBackgroundService> _logger;

    public ParametricImportBackgroundService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<ParametricImportBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Wait for app startup to complete (don't block startup)
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

            // Check if background import is enabled
            var enabledPhase = _configuration.GetValue<string>("CatalogSettings:BackgroundImportPhase", "none");

            if (enabledPhase == "none")
            {
                _logger.LogInformation("[BG_IMPORT] Background import disabled. Skipping.");
                return;
            }

            _logger.LogInformation("[BG_IMPORT] Starting background parametric catalog import. Phase: {Phase}", enabledPhase);

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataDbContext>();
            var importer = scope.ServiceProvider.GetRequiredService<ParametricCatalogImporter>();

            // Check current catalog size
            var existingCount = await context.ParametricHulls.CountAsync(stoppingToken);
            _logger.LogInformation("[BG_IMPORT] Current catalog size: {Count} hulls", existingCount);

            var dataPath = _configuration["DataPath"] ?? "Data";
            var shipDPath = Path.Combine(dataPath, "Ship_D_Dataset");

            if (enabledPhase == "Phase2B")
            {
                // Import all 3 Constrained sets (~30K total)
                await ImportConstrainedSetsAsync(importer, shipDPath, existingCount, stoppingToken);
            }
            else if (enabledPhase == "Phase2C")
            {
                // Import all 5 datasets (82K total)
                await ImportAllDatasetsAsync(importer, shipDPath, existingCount, stoppingToken);
            }

            _logger.LogInformation("[BG_IMPORT] ✅ Background import complete!");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[BG_IMPORT] Background import cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BG_IMPORT] Background import failed");
        }
    }

    /// <summary>
    /// Import all 3 Constrained datasets (~30K hulls)
    /// </summary>
    private async Task ImportConstrainedSetsAsync(
        ParametricCatalogImporter importer,
        string shipDPath,
        int existingCount,
        CancellationToken stoppingToken)
    {
        var datasets = new[]
        {
            "Constrained_Randomized_Set_1",
            "Constrained_Randomized_Set_2",
            "Constrained_Randomized_Set_3"
        };

        int totalImported = 0;

        foreach (var dataset in datasets)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var datasetPath = Path.Combine(shipDPath, dataset);
            if (!Directory.Exists(datasetPath))
            {
                _logger.LogWarning("[BG_IMPORT] Dataset not found: {Dataset}", dataset);
                continue;
            }

            _logger.LogInformation("[BG_IMPORT] Importing {Dataset}...", dataset);

            try
            {
                var result = await importer.ImportFromShipDFolderAsync(
                    folderPath: datasetPath,
                    datasetName: dataset,
                    maxRows: null,  // Import all (~10K per dataset)
                    skipRows: 1,     // Import every row
                    cancellationToken: stoppingToken);

                if (result.Success)
                {
                    totalImported += result.ImportedRows;
                    _logger.LogInformation(
                        "[BG_IMPORT] ✅ {Dataset} complete: {Count} hulls, {Time}ms",
                        dataset, result.ImportedRows, result.ElapsedMs);
                }
                else
                {
                    _logger.LogError(
                        "[BG_IMPORT] ❌ {Dataset} failed: {Errors}",
                        dataset, string.Join("; ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BG_IMPORT] Error importing {Dataset}", dataset);
            }
        }

        _logger.LogInformation(
            "[BG_IMPORT] Phase 2B complete. Total imported: {Total} hulls. Grand total: {GrandTotal}",
            totalImported, existingCount + totalImported);
    }

    /// <summary>
    /// Import all 5 datasets (82K hulls) with parallel processing
    /// </summary>
    private async Task ImportAllDatasetsAsync(
        ParametricCatalogImporter importer,
        string shipDPath,
        int existingCount,
        CancellationToken stoppingToken)
    {
        var datasets = new[]
        {
            "Constrained_Randomized_Set_1",  // ~10K
            "Constrained_Randomized_Set_2",  // ~10K
            "Constrained_Randomized_Set_3",  // ~10K
            "Diffusion_Aug_Set_1",           // ~26K
            "Diffusion_Aug_Set_2"            // ~26K
        };

        _logger.LogInformation("[BG_IMPORT] Phase 2C: Importing all {Count} datasets (~82K hulls)...", datasets.Length);

        // Parallel import (3 threads max to avoid DB contention)
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 3,
            CancellationToken = stoppingToken
        };

        int totalImported = 0;
        var importLock = new object();

        await Parallel.ForEachAsync(datasets, options, async (dataset, ct) =>
        {
            var datasetPath = Path.Combine(shipDPath, dataset);
            if (!Directory.Exists(datasetPath))
            {
                _logger.LogWarning("[BG_IMPORT] Dataset not found: {Dataset}", dataset);
                return;
            }

            _logger.LogInformation("[BG_IMPORT] [Thread {Thread}] Starting {Dataset}...",
                Environment.CurrentManagedThreadId, dataset);

            try
            {
                // Create new scope for thread-safe DbContext
                using var scope = _serviceProvider.CreateScope();
                var threadImporter = scope.ServiceProvider.GetRequiredService<ParametricCatalogImporter>();

                var result = await threadImporter.ImportFromShipDFolderAsync(
                    folderPath: datasetPath,
                    datasetName: dataset,
                    maxRows: null,
                    skipRows: 1,
                    cancellationToken: ct);

                lock (importLock)
                {
                    totalImported += result.ImportedRows;
                }

                _logger.LogInformation(
                    "[BG_IMPORT] ✅ {Dataset} complete: {Count} hulls, {Time}ms (Thread {Thread})",
                    dataset, result.ImportedRows, result.ElapsedMs, Environment.CurrentManagedThreadId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BG_IMPORT] Error importing {Dataset}", dataset);
            }
        });

        _logger.LogInformation(
            "[BG_IMPORT] 🎉 Phase 2C complete! Total imported: {Total} hulls. Grand total: {GrandTotal}",
            totalImported, existingCount + totalImported);
    }
}
