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
    /// Get seed data status with detailed validation
    /// CRITICAL: If hull families = 0, first-principles solver will generate ZERO candidates
    /// </summary>
    [HttpGet("seed-status")]
    public async Task<ActionResult> GetSeedStatus(CancellationToken ct)
    {
        _logger.LogInformation("[DIAGNOSTICS] Checking seed data status...");

        var hullFamiliesCount = await _context.HullFamilyPresets.CountAsync(ct);
        var activeFamiliesCount = await _context.HullFamilyPresets.Where(f => f.IsActive).CountAsync(ct);
        var isoContainersCount = await _context.IsoContainers.CountAsync(ct);
        var kpiWeightsCount = await _context.KpiWeights.CountAsync(ct);

        var errors = new List<string>();
        if (hullFamiliesCount < 5) errors.Add($"Hull families: {hullFamiliesCount}/5 (CRITICAL - solver will generate 0 candidates!)");
        if (activeFamiliesCount < 5) errors.Add($"Active hull families: {activeFamiliesCount}/5 (CRITICAL - solver needs active families!)");
        if (isoContainersCount < 8) errors.Add($"ISO containers: {isoContainersCount}/8 (missing {8 - isoContainersCount})");
        if (kpiWeightsCount < 5) errors.Add($"KPI weights: {kpiWeightsCount}/5 (CRITICAL - candidate scoring will fail!)");

        var isComplete = hullFamiliesCount >= 5 && activeFamiliesCount >= 5 &&
                        isoContainersCount >= 8 && kpiWeightsCount >= 5;

        return Ok(new
        {
            hullFamilies = hullFamiliesCount,
            activeFamilies = activeFamiliesCount,
            isoContainers = isoContainersCount,
            kpiWeights = kpiWeightsCount,
            expected = new
            {
                hullFamilies = 5,
                activeFamilies = 5,
                isoContainers = 8,
                kpiWeights = 5
            },
            seedDataComplete = isComplete,
            errors = errors.Any() ? errors : null,
            severity = !isComplete ? "CRITICAL" : "OK",
            impact = !isComplete ? "First-principles solver may generate ZERO candidates" : null
        });
    }
}
