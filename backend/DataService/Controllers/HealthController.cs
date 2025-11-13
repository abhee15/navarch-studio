using DataService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DataService.Controllers;

/// <summary>
/// Enhanced health check with seed data validation
/// </summary>
[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private static readonly DateTime ServiceStartTime = DateTime.UtcNow;
    private static readonly TimeSpan StartupGracePeriod = TimeSpan.FromMinutes(5);

    private readonly DataDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(DataDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Enhanced health check that includes seed data validation
    /// Returns 200 OK if service is healthy and seed data is complete
    /// Returns 200 OK with "Degraded" status during startup grace period (first 5 minutes) if seed data is incomplete
    /// Returns 503 Service Unavailable if seed data is incomplete after grace period
    /// </summary>
    [HttpGet]
    public async Task<ActionResult> GetHealth()
    {
        try
        {
            // Check database connectivity
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                _logger.LogWarning("[HEALTH] Database unreachable");
                return StatusCode(503, new { status = "Unhealthy", error = "Database unreachable" });
            }

            // Check critical seed data
            var waterCount = await _context.CatalogWaterProperties.CountAsync();
            var benchmarkCount = await _context.BenchmarkCases.CountAsync();

            var seedDataComplete = waterCount >= 6 && benchmarkCount >= 6;
            var uptime = DateTime.UtcNow - ServiceStartTime;
            var isInGracePeriod = uptime < StartupGracePeriod;

            if (seedDataComplete)
            {
                return Ok(new
                {
                    status = "Healthy",
                    database = "Connected",
                    seedData = "Complete",
                    waterProperties = waterCount,
                    benchmarkCases = benchmarkCount
                });
            }
            else if (isInGracePeriod)
            {
                // During startup grace period, return 200 OK but with "Degraded" status
                // This allows App Runner to mark the service as healthy while seeding completes
                _logger.LogInformation(
                    "[HEALTH] Service in startup grace period ({Uptime:F1}s). Seed data incomplete (water: {WaterCount}, benchmarks: {BenchmarkCount}) but allowing degraded state",
                    uptime.TotalSeconds, waterCount, benchmarkCount);

                return Ok(new
                {
                    status = "Degraded",
                    database = "Connected",
                    seedData = "Incomplete",
                    waterProperties = waterCount,
                    benchmarkCases = benchmarkCount,
                    uptimeSeconds = uptime.TotalSeconds,
                    gracePeriodRemainingSeconds = (StartupGracePeriod - uptime).TotalSeconds,
                    message = "Service is starting up - seed data is being populated"
                });
            }
            else
            {
                // After grace period, return 503 if seed data is still incomplete
                _logger.LogError(
                    "[HEALTH] Seed data incomplete after grace period (water: {WaterCount}, benchmarks: {BenchmarkCount}). Service may not function properly.",
                    waterCount, benchmarkCount);

                return StatusCode(503, new
                {
                    status = "Degraded",
                    database = "Connected",
                    seedData = "Incomplete",
                    waterProperties = waterCount,
                    benchmarkCases = benchmarkCount,
                    uptimeSeconds = uptime.TotalSeconds,
                    error = "Critical seed data missing - service may not function properly"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HEALTH] Health check failed with exception");
            return StatusCode(503, new { status = "Unhealthy", error = ex.Message });
        }
    }
}
