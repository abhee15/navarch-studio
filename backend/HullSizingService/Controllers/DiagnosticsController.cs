using HullSizingService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HullSizingService.Controllers;

/// <summary>
/// Diagnostic endpoints for troubleshooting
/// </summary>
[ApiController]
[Route("api/v1/diagnostics")]
public class DiagnosticsController : ControllerBase
{
    private readonly SizingDbContext _context;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(SizingDbContext context, ILogger<DiagnosticsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all hull family presets (for debugging seed data)
    /// </summary>
    [HttpGet("hull-families")]
    public async Task<ActionResult> GetHullFamilies(CancellationToken ct)
    {
        _logger.LogInformation("[DIAGNOSTICS] Checking hull family presets...");

        var families = await _context.HullFamilyPresets
            .OrderBy(f => f.Family)
            .Select(f => new
            {
                f.Id,
                f.Family,
                f.DisplayName,
                f.LOverBMin,
                f.LOverBMax,
                f.BOverTMin,
                f.BOverTMax,
                f.CbMin,
                f.CbMax,
                f.FnMin,
                f.FnMax,
                f.IsActive
            })
            .ToListAsync(ct);

        return Ok(new
        {
            count = families.Count,
            families
        });
    }

    /// <summary>
    /// Get seed data status
    /// </summary>
    [HttpGet("seed-status")]
    public async Task<ActionResult> GetSeedStatus(CancellationToken ct)
    {
        _logger.LogInformation("[DIAGNOSTICS] Checking seed data status...");

        var hullFamiliesCount = await _context.HullFamilyPresets.CountAsync(ct);
        var isoContainersCount = await _context.IsoContainers.CountAsync(ct);
        var kpiWeightsCount = await _context.KpiWeights.CountAsync(ct);

        return Ok(new
        {
            hullFamilies = hullFamiliesCount,
            isoContainers = isoContainersCount,
            kpiWeights = kpiWeightsCount,
            expectedHullFamilies = 5,
            expectedIsoContainers = 8,
            expectedKpiWeights = 5,
            seedDataComplete = hullFamiliesCount >= 5 && isoContainersCount >= 8 && kpiWeightsCount >= 5
        });
    }
}

