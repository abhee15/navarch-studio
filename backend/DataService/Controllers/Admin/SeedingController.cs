using DataService.Data;
using DataService.Data.Seeds;
using Microsoft.AspNetCore.Mvc;

namespace DataService.Controllers.Admin;

/// <summary>
/// Admin endpoints for managing database seeding (TEMPORARY - for debugging)
/// </summary>
[ApiController]
[Route("api/v1/admin/seeding")]
public class SeedingController : ControllerBase
{
    private readonly CatalogSeeder _catalogSeeder;
    private readonly DataDbContext _context;
    private readonly ILogger<SeedingController> _logger;

    public SeedingController(
        CatalogSeeder catalogSeeder,
        DataDbContext context,
        ILogger<SeedingController> logger)
    {
        _catalogSeeder = catalogSeeder;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Check catalog seed status
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult> GetStatus()
    {
        var waterCount = await _context.CatalogWaterProperties.CountAsync();
        var propellerCount = await _context.CatalogPropellerSeries.CountAsync();
        var benchmarkCount = await _context.BenchmarkCases.CountAsync();
        var geometryCount = await _context.BenchmarkGeometries.CountAsync();

        return Ok(new
        {
            waterProperties = waterCount,
            propellerSeries = propellerCount,
            benchmarkCases = benchmarkCount,
            benchmarkGeometries = geometryCount,
            expectedWater = 6,
            expectedPropeller = 1,
            expectedBenchmark = 6, // 3 templates + 3 benchmarks
            isComplete = waterCount >= 6 && propellerCount >= 1 && benchmarkCount >= 6
        });
    }

    /// <summary>
    /// Force re-seed catalog data (WARNING: Use with caution)
    /// </summary>
    [HttpPost("force-reseed")]
    public async Task<ActionResult> ForceReseed()
    {
        _logger.LogWarning("[ADMIN] Force re-seed requested");

        try
        {
            // Delete existing catalog data
            _logger.LogInformation("[ADMIN] Deleting existing catalog data...");
            
            _context.CatalogWaterProperties.RemoveRange(_context.CatalogWaterProperties);
            _context.CatalogPropellerSeries.RemoveRange(_context.CatalogPropellerSeries);
            _context.BenchmarkGeometries.RemoveRange(_context.BenchmarkGeometries);
            _context.BenchmarkCases.RemoveRange(_context.BenchmarkCases);
            
            await _context.SaveChangesAsync();
            _logger.LogInformation("[ADMIN] Existing data deleted");

            // Run seeder
            _logger.LogInformation("[ADMIN] Running catalog seeder...");
            await _catalogSeeder.SeedAllAsync();
            _logger.LogInformation("[ADMIN] Catalog seeder completed");

            // Get new counts
            var waterCount = await _context.CatalogWaterProperties.CountAsync();
            var propellerCount = await _context.CatalogPropellerSeries.CountAsync();
            var benchmarkCount = await _context.BenchmarkCases.CountAsync();
            var geometryCount = await _context.BenchmarkGeometries.CountAsync();

            return Ok(new
            {
                success = true,
                message = "Catalog re-seeded successfully",
                waterProperties = waterCount,
                propellerSeries = propellerCount,
                benchmarkCases = benchmarkCount,
                benchmarkGeometries = geometryCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ADMIN] Force re-seed failed");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }
    }
}

