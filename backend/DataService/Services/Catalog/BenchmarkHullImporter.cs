using CsvHelper;
using CsvHelper.Configuration;
using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Models;
using System.Globalization;

namespace DataService.Services.Catalog;

/// <summary>
/// Imports benchmark hull data from SIMMAN/ITTC datasets
/// Source: .plan/app-docs/templates/MLData/benchmark_hulls.txt
/// </summary>
public class BenchmarkHullImporter
{
    private readonly DataDbContext _context;
    private readonly ILogger<BenchmarkHullImporter> _logger;
    private readonly string _dataPath;

    public BenchmarkHullImporter(
        DataDbContext context,
        IConfiguration configuration,
        ILogger<BenchmarkHullImporter> logger)
    {
        _context = context;
        _logger = logger;
        _dataPath = configuration["DataPath"] ?? "Data";
    }

    /// <summary>
    /// Import benchmark hulls from CSV
    /// Returns count of newly imported hulls
    /// </summary>
    public async Task<int> ImportAsync(CancellationToken cancellationToken = default)
    {
        var csvPath = Path.Combine(_dataPath, "templates/MLData/benchmark_hulls.txt");
        
        if (!File.Exists(csvPath))
        {
            _logger.LogWarning("[BENCHMARK] Benchmark hulls file not found: {Path}", csvPath);
            return 0;
        }

        _logger.LogInformation("[BENCHMARK] Importing benchmark hulls from {Path}", csvPath);

        var startTime = DateTime.UtcNow;

        try
        {
            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            
            var records = csv.GetRecords<BenchmarkHullCsvRow>().ToList();
            var imported = 0;
            var skipped = 0;

            foreach (var record in records)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Check if already exists
                var existing = await _context.CatalogVesselsReal
                    .FirstOrDefaultAsync(v => v.VesselId == record.Hull_Name, cancellationToken);

                if (existing != null)
                {
                    _logger.LogDebug("[BENCHMARK] Hull already exists, skipping: {Name}", record.Hull_Name);
                    skipped++;
                    continue;
                }

                // Derive additional properties
                var depth = EstimateDepth(record.Draft_m, record.Type);
                var cp = EstimateCp(record.Block_Coefficient);
                var cm = record.Block_Coefficient / cp;

                // Calculate displacement in tonnes (from m³)
                var displacementT = record.Displacement_m3 * 1.025m; // Seawater density

                var vessel = new CatalogVesselReal
                {
                    Id = Guid.NewGuid(),
                    VesselId = record.Hull_Name,
                    VesselType = record.Type,
                    LppM = record.Length_PP_m,
                    BeamM = record.Beam_m,
                    DraftM = record.Draft_m,
                    DepthM = depth,
                    DisplacementT = displacementT,
                    Cb = record.Block_Coefficient,
                    Cp = cp,
                    Cm = cm,
                    Source = record.Data_Source,
                    IsSystemData = true, // Benchmark data is system data
                    DataQuality = "Reference", // Mark as reference quality
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _context.CatalogVesselsReal.AddAsync(vessel, cancellationToken);
                imported++;

                _logger.LogDebug("[BENCHMARK] Queued import: {Name} ({Type}, Cb={Cb:F3})", 
                    vessel.VesselId, vessel.VesselType, vessel.Cb);
            }

            if (imported > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "[BENCHMARK] ✅ Benchmark hulls import complete. Imported: {Imported}, Skipped: {Skipped}, Time: {Time}ms",
                imported, skipped, elapsedMs);

            return imported;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BENCHMARK] Error importing benchmark hulls");
            throw;
        }
    }

    /// <summary>
    /// Estimate depth from draft and vessel type
    /// Typical D/T ratios: Tanker ~1.15, Container ~1.25, Combatant ~1.35
    /// </summary>
    private decimal EstimateDepth(decimal draft, string type)
    {
        var ratio = type.ToLower() switch
        {
            "tanker" => 1.15m,
            "container" => 1.25m,
            "combatant" => 1.35m,
            "submarine" => 1.0m, // Circular cross-section
            _ => 1.2m
        };

        return draft * ratio;
    }

    /// <summary>
    /// Estimate prismatic coefficient from block coefficient
    /// Typical relationship: Cp = Cb + (0.05 to 0.10)
    /// </summary>
    private decimal EstimateCp(decimal cb)
    {
        // Fuller hulls (high Cb) have Cp closer to Cb
        // Finer hulls (low Cb) have larger difference
        var delta = cb > 0.70m ? 0.05m : 0.08m;
        var cp = cb + delta;

        // Clamp to valid range
        return Math.Clamp(cp, 0.50m, 1.0m);
    }
}

/// <summary>
/// CSV row model for benchmark_hulls.txt
/// </summary>
public class BenchmarkHullCsvRow
{
    public string Hull_Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Length_PP_m { get; set; }
    public decimal Beam_m { get; set; }
    public decimal Draft_m { get; set; }
    public decimal Displacement_m3 { get; set; }
    public decimal Block_Coefficient { get; set; }
    public decimal LCB_from_midship_m { get; set; }
    public string Scale { get; set; } = string.Empty;
    public decimal Full_Scale_Length_m { get; set; }
    public string Data_Source { get; set; } = string.Empty;
}

