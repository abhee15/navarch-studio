using HullSizingService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Models.Sizing;

namespace HullSizingService.Services;

/// <summary>
/// Service for querying and filtering hull family presets
/// </summary>
public class HullFamilyService : IHullFamilyService
{
    private readonly SizingDbContext _context;
    private readonly ILogger<HullFamilyService> _logger;

    public HullFamilyService(SizingDbContext context, ILogger<HullFamilyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<HullFamilyPreset>> GetApplicableFamiliesAsync(
        string missionType,
        decimal? targetFn,
        decimal? maxDraft,
        decimal? maxBeam,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[HULL_FAMILY] Finding families for mission={Mission}, Fn={Fn}", missionType, targetFn);

        var query = _context.HullFamilyPresets
            .Where(f => f.IsActive);

        // Filter by Froude number range if specified
        if (targetFn.HasValue)
        {
            query = query.Where(f =>
                (f.FnMin == null || targetFn >= f.FnMin) &&
                (f.FnMax == null || targetFn <= f.FnMax));
        }

        var families = await query
            .OrderBy(f => f.Family)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("[HULL_FAMILY] Found {Count} applicable families", families.Count);

        return families;
    }

    public async Task<HullFamilyPreset?> GetByFamilyAsync(string family, CancellationToken cancellationToken = default)
    {
        return await _context.HullFamilyPresets
            .Where(f => f.Family == family && f.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }
}







