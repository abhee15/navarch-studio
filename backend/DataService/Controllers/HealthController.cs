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
    private readonly DataDbContext _context;

    public HealthController(DataDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Enhanced health check that includes seed data validation
    /// Returns 200 OK if service is healthy and seed data is complete
    /// Returns 503 Service Unavailable if seed data is incomplete
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
                return StatusCode(503, new { status = "Unhealthy", error = "Database unreachable" });
            }

            // Check critical seed data
            var waterCount = await _context.CatalogWaterProperties.CountAsync();
            var benchmarkCount = await _context.BenchmarkCases.CountAsync();

            var seedDataComplete = waterCount >= 6 && benchmarkCount >= 6;

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
            else
            {
                return StatusCode(503, new
                {
                    status = "Degraded",
                    database = "Connected",
                    seedData = "Incomplete",
                    waterProperties = waterCount,
                    benchmarkCases = benchmarkCount,
                    error = "Critical seed data missing - service may not function properly"
                });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { status = "Unhealthy", error = ex.Message });
        }
    }
}
