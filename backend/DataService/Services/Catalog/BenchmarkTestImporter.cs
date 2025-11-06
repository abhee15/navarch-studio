using System.Globalization;
using CsvHelper;
using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shared.Models;

namespace DataService.Services.Catalog;

/// <summary>
/// Imports benchmark test conditions from SIMMAN/ITTC datasets
/// Source: .plan/app-docs/templates/MLData/benchmark_test_conditions.txt
/// </summary>
public class BenchmarkTestImporter
{
    private readonly DataDbContext _context;
    private readonly ILogger<BenchmarkTestImporter> _logger;
    private readonly string _dataPath;

    public BenchmarkTestImporter(
        DataDbContext context,
        IConfiguration configuration,
        ILogger<BenchmarkTestImporter> logger)
    {
        _context = context;
        _logger = logger;
        _dataPath = configuration["DataPath"] ?? "Data";
    }

    /// <summary>
    /// Import benchmark test conditions from CSV
    /// Returns count of newly imported test scenarios
    /// </summary>
    public async Task<int> ImportAsync(CancellationToken cancellationToken = default)
    {
        // DataPath points to hull-sizing/data, go up to app-docs then to templates
        var csvPath = Path.Combine(_dataPath, "..", "..", "templates", "MLData", "benchmark_test_conditions.txt");

        if (!File.Exists(csvPath))
        {
            _logger.LogWarning("[BENCHMARK_TEST] Test conditions file not found: {Path}", csvPath);
            return 0;
        }

        _logger.LogInformation("[BENCHMARK_TEST] Importing test conditions from {Path}", csvPath);

        var startTime = DateTime.UtcNow;

        try
        {
            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var records = csv.GetRecords<BenchmarkTestCsvRow>().ToList();
            var imported = 0;

            // Clear existing test conditions (they're reference data, safe to replace)
            var existing = await _context.BenchmarkTestConditions.ToListAsync(cancellationToken);
            if (existing.Any())
            {
                _context.BenchmarkTestConditions.RemoveRange(existing);
                _logger.LogInformation("[BENCHMARK_TEST] Removed {Count} existing test conditions", existing.Count);
            }

            foreach (var record in records)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var test = new BenchmarkTestCondition
                {
                    Id = Guid.NewGuid(),
                    TestType = record.Test_Type,
                    HullName = record.Hull,
                    SpeedKnots = record.Speed_knots,
                    FroudeNumber = record.Froude_Number,
                    ReynoldsNumber = record.Reynolds_Number,
                    WaveHeightM = record.Wave_Height_m,
                    WavePeriodS = record.Wave_Period_s,
                    HeadingDeg = record.Heading_deg,
                    Description = record.Description,
                    Standard = record.Standard,
                    CreatedAt = DateTime.UtcNow
                };

                await _context.BenchmarkTestConditions.AddAsync(test, cancellationToken);
                imported++;

                _logger.LogDebug("[BENCHMARK_TEST] Queued: {Type} - {Hull} @ Fn={Fn:F3}",
                    test.TestType, test.HullName, test.FroudeNumber);
            }

            if (imported > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            var elapsedMs = (DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "[BENCHMARK_TEST] ✅ Test conditions import complete. Imported: {Imported}, Time: {Time}ms",
                imported, elapsedMs);

            return imported;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BENCHMARK_TEST] Error importing test conditions");
            throw;
        }
    }
}

/// <summary>
/// CSV row model for benchmark_test_conditions.txt
/// </summary>
public class BenchmarkTestCsvRow
{
    public string Test_Type { get; set; } = string.Empty;
    public string Hull { get; set; } = string.Empty;
    public decimal Speed_knots { get; set; }
    public decimal Froude_Number { get; set; }
    public decimal Reynolds_Number { get; set; }
    public decimal Wave_Height_m { get; set; }
    public decimal Wave_Period_s { get; set; }
    public decimal Heading_deg { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Standard { get; set; } = string.Empty;
}
