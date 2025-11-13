using DataService.Data;
using DataService.Data.ShipD;
using DataService.Services.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataService.Services.Catalog;

/// <summary>
/// Seeds ShipD taxonomy fields for existing catalog vessels
/// Maps catalog vessel types to ShipD taxonomy and infers families from form coefficients
/// </summary>
public class CatalogTaxonomySeeder
{
    private readonly DataDbContext _context;
    private readonly IVesselTypeMapper _vesselTypeMapper;
    private readonly ILogger<CatalogTaxonomySeeder> _logger;

    public CatalogTaxonomySeeder(
        DataDbContext context,
        IVesselTypeMapper vesselTypeMapper,
        ILogger<CatalogTaxonomySeeder> logger)
    {
        _context = context;
        _vesselTypeMapper = vesselTypeMapper;
        _logger = logger;
    }

    /// <summary>
    /// Seeds taxonomy fields for all catalog vessels that don't have them yet
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[CATALOG-TAXONOMY] Starting taxonomy seeding for catalog vessels...");

        // Load ShipD taxonomy entries for reference
        var taxonomyEntries = await _context.ShipDVesselTaxonomies
            .ToListAsync(cancellationToken);

        if (!taxonomyEntries.Any())
        {
            _logger.LogWarning("[CATALOG-TAXONOMY] No ShipD taxonomy entries found. Skipping seeding.");
            return;
        }

        // Get all catalog vessels without taxonomy
        var vesselsWithoutTaxonomy = await _context.CatalogVesselsReal
            .Where(v => v.ShipdVesselType == null || v.VesselCategory == null)
            .ToListAsync(cancellationToken);

        if (!vesselsWithoutTaxonomy.Any())
        {
            _logger.LogInformation("[CATALOG-TAXONOMY] All catalog vessels already have taxonomy. Skipping.");
            return;
        }

        _logger.LogInformation(
            "[CATALOG-TAXONOMY] Found {Count} vessels without taxonomy. Starting mapping...",
            vesselsWithoutTaxonomy.Count);

        int updated = 0;
        int skipped = 0;

        foreach (var vessel in vesselsWithoutTaxonomy)
        {
            try
            {
                // Step 1: Map vessel type to ShipD taxonomy
                var shipdType = _vesselTypeMapper.MapToShipDType(vessel.VesselType);

                if (string.IsNullOrEmpty(shipdType))
                {
                    _logger.LogDebug(
                        "[CATALOG-TAXONOMY] No ShipD mapping for vessel '{VesselId}' (type: '{VesselType}'). Skipping.",
                        vessel.VesselId, vessel.VesselType);
                    skipped++;
                    continue;
                }

                // Step 2: Find matching taxonomy entry
                var taxonomy = taxonomyEntries
                    .FirstOrDefault(t => t.Type.Equals(shipdType, StringComparison.OrdinalIgnoreCase));

                if (taxonomy == null)
                {
                    _logger.LogWarning(
                        "[CATALOG-TAXONOMY] No taxonomy entry found for ShipD type '{ShipDType}' (vessel: '{VesselId}'). Skipping.",
                        shipdType, vessel.VesselId);
                    skipped++;
                    continue;
                }

                // Step 3: Set taxonomy fields
                vessel.ShipdVesselType = shipdType;
                vessel.VesselCategory = taxonomy.Category;
                vessel.FamilyMaskVersion = taxonomy.MaskVersion;

                // Step 4: Infer hull families from form coefficients and vessel type
                InferHullFamilies(vessel, taxonomy);

                updated++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[CATALOG-TAXONOMY] Error processing vessel '{VesselId}'. Skipping.",
                    vessel.VesselId);
                skipped++;
            }
        }

        if (updated > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "[CATALOG-TAXONOMY] ✅ Taxonomy seeding complete. Updated: {Updated}, Skipped: {Skipped}",
                updated, skipped);
        }
        else
        {
            _logger.LogInformation(
                "[CATALOG-TAXONOMY] No vessels updated. Skipped: {Skipped}",
                skipped);
        }
    }

    /// <summary>
    /// Infers hull families from form coefficients and vessel characteristics
    /// Uses heuristics based on Cb, Cp, and vessel type
    /// </summary>
    private void InferHullFamilies(Shared.Models.CatalogVesselReal vessel, ShipDVesselTaxonomy taxonomy)
    {
        // Parse taxonomy families (stored as JSON arrays)
        var bowFamilies = ParseJsonArray(taxonomy.BowFamiliesJson);
        var midshipFamilies = ParseJsonArray(taxonomy.MidshipFamiliesJson);
        var sternFamilies = ParseJsonArray(taxonomy.SternFamiliesJson);

        if (!bowFamilies.Any() || !midshipFamilies.Any() || !sternFamilies.Any())
        {
            _logger.LogDebug(
                "[CATALOG-TAXONOMY] No families defined in taxonomy for type '{Type}'. Skipping family inference.",
                taxonomy.Type);
            return;
        }

        // Infer bow family based on Cb and vessel type
        vessel.BowFamily = InferBowFamily(vessel, bowFamilies);

        // Infer midship family based on Cb and Cm
        vessel.MidshipFamily = InferMidshipFamily(vessel, midshipFamilies);

        // Infer stern family based on vessel type and form coefficients
        vessel.SternFamily = InferSternFamily(vessel, sternFamilies);
    }

    /// <summary>
    /// Infers bow family from vessel characteristics
    /// </summary>
    private string? InferBowFamily(Shared.Models.CatalogVesselReal vessel, List<string> availableFamilies)
    {
        // Heuristics:
        // - High Cb (>0.75) → bulbous_bow (tankers, bulk carriers)
        // - Medium Cb (0.6-0.75) → straight_raked (container ships)
        // - Low Cb (<0.6) → fine_entry (fishing, yachts)

        if (vessel.Cb > 0.75m)
        {
            // Prefer bulbous_bow for full forms
            return availableFamilies.FirstOrDefault(f => f.Contains("bulbous", StringComparison.OrdinalIgnoreCase))
                ?? availableFamilies.FirstOrDefault();
        }
        else if (vessel.Cb > 0.6m)
        {
            // Prefer straight_raked for medium forms
            return availableFamilies.FirstOrDefault(f => f.Contains("straight", StringComparison.OrdinalIgnoreCase) || f.Contains("raked", StringComparison.OrdinalIgnoreCase))
                ?? availableFamilies.FirstOrDefault();
        }
        else
        {
            // Prefer fine_entry for fine forms
            return availableFamilies.FirstOrDefault(f => f.Contains("fine", StringComparison.OrdinalIgnoreCase) || f.Contains("entry", StringComparison.OrdinalIgnoreCase))
                ?? availableFamilies.FirstOrDefault();
        }
    }

    /// <summary>
    /// Infers midship family from form coefficients
    /// </summary>
    private string? InferMidshipFamily(Shared.Models.CatalogVesselReal vessel, List<string> availableFamilies)
    {
        // Heuristics:
        // - High Cb (>0.75) → full_midship
        // - Medium Cb (0.6-0.75) → fine_midship or full_midship
        // - Low Cb (<0.6) → fine_midship or deep_v_midship

        if (vessel.Cb > 0.75m)
        {
            return availableFamilies.FirstOrDefault(f => f.Contains("full", StringComparison.OrdinalIgnoreCase))
                ?? availableFamilies.FirstOrDefault();
        }
        else if (vessel.Cb < 0.6m)
        {
            return availableFamilies.FirstOrDefault(f => f.Contains("fine", StringComparison.OrdinalIgnoreCase) || f.Contains("deep", StringComparison.OrdinalIgnoreCase))
                ?? availableFamilies.FirstOrDefault();
        }
        else
        {
            // Medium Cb - use first available
            return availableFamilies.FirstOrDefault();
        }
    }

    /// <summary>
    /// Infers stern family from vessel type and characteristics
    /// </summary>
    private string? InferSternFamily(Shared.Models.CatalogVesselReal vessel, List<string> availableFamilies)
    {
        // Heuristics:
        // - Commercial vessels → transom_stern (common)
        // - Fishing/yachts → cruiser_stern or canoe_stern
        // - High-speed craft → transom_stern

        var vesselTypeLower = vessel.VesselType.ToLowerInvariant();

        if (vesselTypeLower.Contains("fishing") || vesselTypeLower.Contains("yacht"))
        {
            return availableFamilies.FirstOrDefault(f => f.Contains("cruiser", StringComparison.OrdinalIgnoreCase) || f.Contains("canoe", StringComparison.OrdinalIgnoreCase))
                ?? availableFamilies.FirstOrDefault();
        }
        else
        {
            // Default to transom_stern for commercial vessels
            return availableFamilies.FirstOrDefault(f => f.Contains("transom", StringComparison.OrdinalIgnoreCase))
                ?? availableFamilies.FirstOrDefault();
        }
    }

    /// <summary>
    /// Parses JSON array string into list of strings
    /// </summary>
    private List<string> ParseJsonArray(string? jsonArray)
    {
        if (string.IsNullOrWhiteSpace(jsonArray))
        {
            return new List<string>();
        }

        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(jsonArray);
            return parsed ?? new List<string>();
        }
        catch
        {
            _logger.LogWarning("[CATALOG-TAXONOMY] Failed to parse JSON array: {Json}", jsonArray);
            return new List<string>();
        }
    }
}
