using Microsoft.EntityFrameworkCore;
using Shared.Models.Sizing;

namespace HullSizingService.Data.Seeds;

/// <summary>
/// Seeds default KPI weights for multi-objective hull sizing scoring
/// These are system-wide defaults (userId = null)
/// </summary>
public static class KpiWeightSeeder
{
    public static async Task SeedAsync(SizingDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[SEED] Loading KPI weight defaults...");

        // Load existing weights (system defaults only)
        var existingWeights = await context.KpiWeights
            .Where(w => w.UserId == null)
            .ToDictionaryAsync(w => w.Metric, w => w, cancellationToken);

        var weights = GetDefaultKpiWeights();

        int added = 0;
        int updated = 0;

        foreach (var weight in weights)
        {
            if (existingWeights.TryGetValue(weight.Metric, out var existing))
            {
                // Update existing
                existing.Weight = weight.Weight;
                updated++;
            }
            else
            {
                // Add new
                context.KpiWeights.Add(weight);
                added++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("[SEED] KPI weights synced: {Added} added, {Updated} updated (Total: {Total})",
            added, updated, weights.Count);
    }

    private static List<KpiWeight> GetDefaultKpiWeights()
    {
        return new List<KpiWeight>
        {
            // Displacement Balance - Highest priority
            // Measures how accurately the sizing algorithm matched the target displacement
            // Within ±1% is excellent
            new KpiWeight
            {
                Id = Guid.NewGuid(),
                UserId = null, // System default
                Metric = "delta_balance",
                Weight = 0.35m
            },

            // Installed Power - Second priority
            // Lower SHP means better fuel efficiency and lower operating costs
            // Normalized across all candidates
            new KpiWeight
            {
                Id = Guid.NewGuid(),
                UserId = null,
                Metric = "installed_power",
                Weight = 0.25m
            },

            // Constraints - Third priority
            // No violations of draft/beam/LOA caps from mission requirements
            // Critical for port/canal access
            new KpiWeight
            {
                Id = Guid.NewGuid(),
                UserId = null,
                Metric = "constraints_ok",
                Weight = 0.20m
            },

            // Stability Screen - Fourth priority
            // GMt within acceptable range (1-3m typical)
            // Too low = unstable, too high = uncomfortable roll
            new KpiWeight
            {
                Id = Guid.NewGuid(),
                UserId = null,
                Metric = "stability_screen",
                Weight = 0.10m
            },

            // Cargo Capacity Fit - Fifth priority
            // How well the hull volume matches TEU or cargo volume requirements
            // Less critical in preliminary sizing (detailed in next phase)
            new KpiWeight
            {
                Id = Guid.NewGuid(),
                UserId = null,
                Metric = "teu_or_volume_fit",
                Weight = 0.10m
            }
        };
    }
}

