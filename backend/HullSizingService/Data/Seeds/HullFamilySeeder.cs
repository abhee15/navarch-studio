using Microsoft.EntityFrameworkCore;
using Shared.Models.Sizing;

namespace HullSizingService.Data.Seeds;

/// <summary>
/// Seeds hull family presets with industry-standard geometric ranges
/// </summary>
public static class HullFamilySeeder
{
    public static async Task SeedAsync(SizingDbContext context, ILogger logger, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[SEED] Loading hull families...");

        // Load existing families
        var existingFamilies = await context.HullFamilyPresets
            .ToDictionaryAsync(f => f.Family, f => f, cancellationToken);

        var families = GetHullFamilies();

        int added = 0;
        int updated = 0;

        foreach (var family in families)
        {
            if (existingFamilies.TryGetValue(family.Family, out var existing))
            {
                // Update existing
                existing.DisplayName = family.DisplayName;
                existing.LOverBMin = family.LOverBMin;
                existing.LOverBMax = family.LOverBMax;
                existing.BOverTMin = family.BOverTMin;
                existing.BOverTMax = family.BOverTMax;
                existing.DOverTMin = family.DOverTMin;
                existing.DOverTMax = family.DOverTMax;
                existing.CbMin = family.CbMin;
                existing.CbMax = family.CbMax;
                existing.CpMin = family.CpMin;
                existing.CpMax = family.CpMax;
                existing.CwpMin = family.CwpMin;
                existing.CwpMax = family.CwpMax;
                existing.FnMin = family.FnMin;
                existing.FnMax = family.FnMax;
                existing.GeneratorType = family.GeneratorType;
                existing.Notes = family.Notes;
                existing.IsActive = family.IsActive;
                updated++;
            }
            else
            {
                // Add new
                context.HullFamilyPresets.Add(family);
                added++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("[SEED] Hull families synced: {Added} added, {Updated} updated (Total: {Total})",
            added, updated, families.Count);
    }

    private static List<HullFamilyPreset> GetHullFamilies()
    {
        return new List<HullFamilyPreset>
        {
            // Container Ship - High-speed, slender hulls
            new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = "container",
                DisplayName = "Container Ship",
                LOverBMin = 6.50m, LOverBMax = 8.00m,
                BOverTMin = 2.80m, BOverTMax = 3.50m,
                DOverTMin = 1.30m, DOverTMax = 1.50m,
                CbMin = 0.60m, CbMax = 0.70m,
                CpMin = 0.70m, CpMax = 0.75m,
                CwpMin = 0.75m, CwpMax = 0.85m,
                FnMin = 0.22m, FnMax = 0.28m,
                GeneratorType = "wigley",
                IsActive = true,
                Notes = "High-speed container vessels with slender hulls optimized for Fn 0.22-0.28"
            },

            // Tanker - Full-form, slow displacement hulls
            new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = "tanker",
                DisplayName = "Tanker",
                LOverBMin = 5.00m, LOverBMax = 6.50m,
                BOverTMin = 2.00m, BOverTMax = 2.50m,
                DOverTMin = 1.20m, DOverTMax = 1.40m,
                CbMin = 0.78m, CbMax = 0.85m,
                CpMin = 0.82m, CpMax = 0.88m,
                CwpMin = 0.85m, CwpMax = 0.92m,
                FnMin = 0.12m, FnMax = 0.18m,
                GeneratorType = "series60",
                IsActive = true,
                Notes = "VLCC/Suezmax/Aframax tankers with full-form displacement hulls"
            },

            // Bulk Carrier - Moderate fullness and speed
            new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = "bulk",
                DisplayName = "Bulk Carrier",
                LOverBMin = 5.50m, LOverBMax = 7.00m,
                BOverTMin = 2.20m, BOverTMax = 2.80m,
                DOverTMin = 1.25m, DOverTMax = 1.45m,
                CbMin = 0.72m, CbMax = 0.80m,
                CpMin = 0.78m, CpMax = 0.84m,
                CwpMin = 0.80m, CwpMax = 0.88m,
                FnMin = 0.14m, FnMax = 0.20m,
                GeneratorType = "series60",
                IsActive = true,
                Notes = "Capesize/Panamax/Handymax bulk carriers with moderate speed"
            },

            // General Cargo - Multipurpose vessels
            new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = "cargo",
                DisplayName = "General Cargo",
                LOverBMin = 5.50m, LOverBMax = 7.00m,
                BOverTMin = 2.50m, BOverTMax = 3.50m,
                DOverTMin = 1.30m, DOverTMax = 1.50m,
                CbMin = 0.62m, CbMax = 0.73m,
                CpMin = 0.72m, CpMax = 0.78m,
                CwpMin = 0.75m, CwpMax = 0.85m,
                FnMin = 0.16m, FnMax = 0.24m,
                GeneratorType = "wigley",
                IsActive = true,
                Notes = "Multipurpose cargo vessels with moderate speed and capacity"
            },

            // RoRo / Car Carrier - High D/T for vehicle decks
            new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = "roro",
                DisplayName = "RoRo / Car Carrier",
                LOverBMin = 6.00m, LOverBMax = 7.50m,
                BOverTMin = 3.50m, BOverTMax = 4.50m,
                DOverTMin = 1.60m, DOverTMax = 2.00m,
                CbMin = 0.55m, CbMax = 0.65m,
                CpMin = 0.65m, CpMax = 0.75m,
                CwpMin = 0.70m, CwpMax = 0.80m,
                FnMin = 0.20m, FnMax = 0.26m,
                GeneratorType = "wigley",
                IsActive = true,
                Notes = "Roll-on/Roll-off vessels with high D/T for multiple car decks"
            },

            // LNG Carrier - Specialized gas carriers
            new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = "lng",
                DisplayName = "LNG Carrier",
                LOverBMin = 5.50m, LOverBMax = 6.50m,
                BOverTMin = 2.20m, BOverTMax = 2.80m,
                DOverTMin = 1.30m, DOverTMax = 1.50m,
                CbMin = 0.70m, CbMax = 0.78m,
                CpMin = 0.75m, CpMax = 0.82m,
                CwpMin = 0.78m, CwpMax = 0.85m,
                FnMin = 0.18m, FnMax = 0.24m,
                GeneratorType = "series60",
                IsActive = true,
                Notes = "Liquefied Natural Gas carriers with membrane or spherical tank systems"
            },

            // Offshore Supply Vessel
            new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = "osv",
                DisplayName = "Offshore Supply",
                LOverBMin = 4.00m, LOverBMax = 5.50m,
                BOverTMin = 2.50m, BOverTMax = 3.50m,
                DOverTMin = 1.40m, DOverTMax = 1.80m,
                CbMin = 0.55m, CbMax = 0.70m,
                CpMin = 0.65m, CpMax = 0.75m,
                CwpMin = 0.70m, CwpMax = 0.82m,
                FnMin = 0.20m, FnMax = 0.30m,
                GeneratorType = "wigley",
                IsActive = true,
                Notes = "Platform supply vessels for offshore operations with moderate to high speed"
            },

            // Fishing Vessel
            new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = "fishing",
                DisplayName = "Fishing Vessel",
                LOverBMin = 3.50m, LOverBMax = 5.00m,
                BOverTMin = 2.80m, BOverTMax = 3.80m,
                DOverTMin = 1.40m, DOverTMax = 1.70m,
                CbMin = 0.50m, CbMax = 0.65m,
                CpMin = 0.60m, CpMax = 0.72m,
                CwpMin = 0.70m, CwpMax = 0.82m,
                FnMin = 0.22m, FnMax = 0.32m,
                GeneratorType = "wigley",
                IsActive = true,
                Notes = "Fishing trawlers and longliners with seakeeping as priority"
            },

            // Tugboat
            new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = "tug",
                DisplayName = "Tugboat",
                LOverBMin = 3.00m, LOverBMax = 4.50m,
                BOverTMin = 2.50m, BOverTMax = 3.80m,
                DOverTMin = 1.30m, DOverTMax = 1.60m,
                CbMin = 0.48m, CbMax = 0.62m,
                CpMin = 0.58m, CpMax = 0.70m,
                CwpMin = 0.68m, CwpMax = 0.80m,
                FnMin = 0.15m, FnMax = 0.28m,
                GeneratorType = "wigley",
                IsActive = true,
                Notes = "Harbor and ocean-going tugs optimized for bollard pull"
            },

            // Displacement Yacht
            new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = "yacht_disp",
                DisplayName = "Displacement Yacht",
                LOverBMin = 5.00m, LOverBMax = 7.00m,
                BOverTMin = 3.00m, BOverTMax = 4.50m,
                DOverTMin = 1.40m, DOverTMax = 1.80m,
                CbMin = 0.42m, CbMax = 0.58m,
                CpMin = 0.55m, CpMax = 0.68m,
                CwpMin = 0.65m, CwpMax = 0.78m,
                FnMin = 0.25m, FnMax = 0.38m,
                GeneratorType = "wigley",
                IsActive = true,
                Notes = "Motor yachts and sailboats in displacement mode prioritizing comfort and range"
            },

            // Fast Ferry
            new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = "ferry_fast",
                DisplayName = "Fast Ferry",
                LOverBMin = 7.00m, LOverBMax = 9.00m,
                BOverTMin = 3.50m, BOverTMax = 5.00m,
                DOverTMin = 1.50m, DOverTMax = 2.00m,
                CbMin = 0.38m, CbMax = 0.52m,
                CpMin = 0.52m, CpMax = 0.65m,
                CwpMin = 0.60m, CwpMax = 0.75m,
                FnMin = 0.35m, FnMax = 0.50m,
                GeneratorType = "wigley",
                IsActive = true,
                Notes = "High-speed passenger ferries with semi-displacement hulls"
            },

            // Conventional Ferry
            new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = "ferry_conv",
                DisplayName = "Conventional Ferry",
                LOverBMin = 5.50m, LOverBMax = 7.00m,
                BOverTMin = 3.00m, BOverTMax = 4.00m,
                DOverTMin = 1.50m, DOverTMax = 1.90m,
                CbMin = 0.55m, CbMax = 0.68m,
                CpMin = 0.65m, CpMax = 0.75m,
                CwpMin = 0.72m, CwpMax = 0.82m,
                FnMin = 0.22m, FnMax = 0.30m,
                GeneratorType = "wigley",
                IsActive = true,
                Notes = "Conventional displacement ferries including Ro-Pax vessels"
            },

            // Research Vessel
            new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = "research",
                DisplayName = "Research Vessel",
                LOverBMin = 5.00m, LOverBMax = 6.50m,
                BOverTMin = 2.80m, BOverTMax = 3.80m,
                DOverTMin = 1.40m, DOverTMax = 1.70m,
                CbMin = 0.52m, CbMax = 0.68m,
                CpMin = 0.62m, CpMax = 0.75m,
                CwpMin = 0.70m, CwpMax = 0.82m,
                FnMin = 0.18m, FnMax = 0.26m,
                GeneratorType = "wigley",
                IsActive = true,
                Notes = "Oceanographic and scientific research vessels with laboratory space"
            },

            // Patrol Boat
            new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = "patrol",
                DisplayName = "Patrol Boat",
                LOverBMin = 5.50m, LOverBMax = 7.50m,
                BOverTMin = 3.00m, BOverTMax = 4.50m,
                DOverTMin = 1.40m, DOverTMax = 1.80m,
                CbMin = 0.45m, CbMax = 0.62m,
                CpMin = 0.58m, CpMax = 0.72m,
                CwpMin = 0.65m, CwpMax = 0.78m,
                FnMin = 0.28m, FnMax = 0.42m,
                GeneratorType = "wigley",
                IsActive = true,
                Notes = "Military and coast guard patrol craft balancing seakeeping and speed"
            },

            // Barge
            new HullFamilyPreset
            {
                Id = Guid.NewGuid(),
                Family = "barge",
                DisplayName = "Barge",
                LOverBMin = 5.00m, LOverBMax = 8.00m,
                BOverTMin = 2.00m, BOverTMax = 3.50m,
                DOverTMin = 1.15m, DOverTMax = 1.35m,
                CbMin = 0.85m, CbMax = 0.95m,
                CpMin = 0.90m, CpMax = 0.98m,
                CwpMin = 0.88m, CwpMax = 0.95m,
                FnMin = 0.08m, FnMax = 0.15m,
                GeneratorType = "series60",
                IsActive = true,
                Notes = "Pushed or towed barges with very full forms and slow speeds"
            }
        };
    }
}











