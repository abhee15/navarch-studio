using Microsoft.Extensions.Logging;

namespace HullSizingService.Services.ShipD;

/// <summary>
/// Service for mapping vessel types to default hull shape parameters.
/// Implements automatic selection of hull families and geometry parameters
/// based on vessel category and type, using research-based mappings.
/// </summary>
public class VesselTypeHullMappingService : IVesselTypeHullMappingService
{
    private readonly ILogger<VesselTypeHullMappingService> _logger;
    private static readonly Dictionary<string, VesselHullDefaults> Mappings = new(StringComparer.OrdinalIgnoreCase);

    static VesselTypeHullMappingService()
    {
        InitializeMappings();
    }

    public VesselTypeHullMappingService(ILogger<VesselTypeHullMappingService> logger)
    {
        _logger = logger;
    }

    public VesselHullDefaults? GetDefaultsForVesselType(string category, string type)
    {
        if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(type))
        {
            return null;
        }

        var key = $"{category}:{type}";
        if (Mappings.TryGetValue(key, out var defaults))
        {
            _logger.LogDebug("[VESSEL_MAPPING] Found defaults for {Category}:{Type}: Bow={Bow}, Mid={Mid}, Stern={Stern}, Chine={Chine}",
                category, type, defaults.BowFamily, defaults.MidshipFamily, defaults.SternFamily, defaults.ChineType);
            return defaults;
        }

        _logger.LogWarning("[VESSEL_MAPPING] No mapping found for {Category}:{Type}", category, type);
        return null;
    }

    private static void InitializeMappings()
    {
        // ====================================================================
        // COMMERCIAL VESSELS
        // ====================================================================

        // General Cargo - Similar to Container Ship (soft chine + convex)
        Mappings["commercial:general_cargo"] = new VesselHullDefaults
        {
            BowFamily = "bulbous_bow",
            MidshipFamily = "barge_type", // or "fine_midship"
            SternFamily = "transom_stern",
            ChineType = "soft",
            CurvatureType = "convex", // U-shaped bottom
            DeadriseAngleDeg = 7.5m, // 5-10° range
            FlareAngleDeg = 5m
        };

        // Bulk Carrier - Similar to Container Ship (soft chine + convex)
        Mappings["commercial:bulk_carrier"] = new VesselHullDefaults
        {
            BowFamily = "bulbous_bow",
            MidshipFamily = "barge_type",
            SternFamily = "transom_stern",
            ChineType = "soft",
            CurvatureType = "convex", // U-shaped bottom
            DeadriseAngleDeg = 7.5m, // 5-10° range
            FlareAngleDeg = 5m
        };

        // Container Ship - Soft chine + Convex (matches visual reference)
        Mappings["commercial:container"] = new VesselHullDefaults
        {
            BowFamily = "bulbous_bow",
            MidshipFamily = "barge_type", // or "fine_midship"
            SternFamily = "transom_stern",
            ChineType = "soft",
            CurvatureType = "convex", // U-shaped bottom
            DeadriseAngleDeg = 10m, // 5-15° range
            FlareAngleDeg = 5m
        };

        // Tanker - Soft chine + Convex (standard)
        Mappings["commercial:tanker"] = new VesselHullDefaults
        {
            BowFamily = "bulbous_bow",
            MidshipFamily = "barge_type",
            SternFamily = "transom_stern",
            ChineType = "soft",
            CurvatureType = "convex", // U-shaped bottom
            DeadriseAngleDeg = 7.5m, // 5-10° range
            FlareAngleDeg = 5m
        };

        // LNG Carrier - Similar to Tanker (soft chine + convex)
        Mappings["commercial:lng_carrier"] = new VesselHullDefaults
        {
            BowFamily = "bulbous_bow",
            MidshipFamily = "barge_type",
            SternFamily = "transom_stern",
            ChineType = "soft",
            CurvatureType = "convex", // U-shaped bottom
            DeadriseAngleDeg = 7.5m, // 5-10° range
            FlareAngleDeg = 5m
        };

        // Cruise Vessel - Soft chine + Convex (comfort priority)
        Mappings["commercial:cruise_vessel"] = new VesselHullDefaults
        {
            BowFamily = "bulbous_bow",
            MidshipFamily = "barge_type",
            SternFamily = "transom_stern",
            ChineType = "soft",
            CurvatureType = "convex", // U-shaped bottom, smooth ride
            DeadriseAngleDeg = 10m, // 5-15° range
            FlareAngleDeg = 5m
        };

        // Passenger Vessel - Similar to Cruise Vessel (soft chine + convex)
        Mappings["commercial:passenger_vessel"] = new VesselHullDefaults
        {
            BowFamily = "bulbous_bow",
            MidshipFamily = "barge_type",
            SternFamily = "transom_stern",
            ChineType = "soft",
            CurvatureType = "convex", // U-shaped bottom
            DeadriseAngleDeg = 10m, // 5-15° range
            FlareAngleDeg = 5m
        };

        // Fishing Vessel - Soft chine (traditional) or Hard chine (modern)
        // Default to soft chine for traditional fishing
        Mappings["commercial:fishing"] = new VesselHullDefaults
        {
            BowFamily = "straight_raked", // or "bulbous_bow"
            MidshipFamily = "fine_midship", // or "barge_type"
            SternFamily = "transom_stern",
            ChineType = "soft", // Traditional (can override to hard chine for modern designs)
            CurvatureType = null, // Neutral
            DeadriseAngleDeg = 12m, // 5-25° varies widely
            FlareAngleDeg = 5m
        };

        // ====================================================================
        // RECREATIONAL VESSELS
        // ====================================================================

        // Yacht - Sailing Yacht (soft chine + convex) - Matches visual reference
        // Note: Power yachts would use hard chine, but default to sailing
        Mappings["recreational:yacht"] = new VesselHullDefaults
        {
            BowFamily = "straight_raked", // No bulbous bow for sailing
            MidshipFamily = "fine_midship", // or "deep_v" for power yacht
            SternFamily = "cruiser_stern", // or "canoe_stern"
            ChineType = "soft", // Sailing yacht (power yacht would be "hard")
            CurvatureType = "convex", // U-shaped bottom with sharp keel
            DeadriseAngleDeg = 15m, // 10-20° for sailing
            FlareAngleDeg = 8m
        };

        // Recreational Fishing - Similar to Sailing Yacht (soft chine + convex)
        Mappings["recreational:fishing_recreational"] = new VesselHullDefaults
        {
            BowFamily = "straight_raked",
            MidshipFamily = "fine_midship",
            SternFamily = "cruiser_stern",
            ChineType = "soft",
            CurvatureType = "convex", // U-shaped bottom
            DeadriseAngleDeg = 12m, // 10-20° range
            FlareAngleDeg = 8m
        };

        // ====================================================================
        // GOVERNMENT/MILITARY VESSELS
        // ====================================================================

        // General Military - Tumblehome/Stealth (matches DDG-1000 visual reference)
        Mappings["government:general_military"] = new VesselHullDefaults
        {
            BowFamily = "straight_raked", // Wave-piercing
            MidshipFamily = "fine_midship", // For tumblehome
            SternFamily = "transom_stern",
            ChineType = "soft", // Lower sections
            CurvatureType = "concave", // Upper sections (tumblehome)
            DeadriseAngleDeg = 10m,
            FlareAngleDeg = -15m, // Negative = tumblehome (inward slope)
            TumblehomeEnabled = true
        };

        // Cutters - Hard Chine (traditional military)
        Mappings["government:cutters"] = new VesselHullDefaults
        {
            BowFamily = "straight_raked",
            MidshipFamily = "fine_midship",
            SternFamily = "transom_stern",
            ChineType = "hard", // Traditional military
            CurvatureType = null, // Neutral
            DeadriseAngleDeg = 15m,
            FlareAngleDeg = 8m
        };

        // Medical Ship - Similar to Commercial (soft chine + convex)
        Mappings["government:medical_ship"] = new VesselHullDefaults
        {
            BowFamily = "bulbous_bow",
            MidshipFamily = "barge_type",
            SternFamily = "transom_stern",
            ChineType = "soft",
            CurvatureType = "convex", // U-shaped bottom
            DeadriseAngleDeg = 10m, // 5-15° range
            FlareAngleDeg = 5m
        };

        // ====================================================================
        // SPECIALIZED VESSELS
        // ====================================================================

        // High-Speed Craft - Hard Chine (planing hull) - Matches visual reference
        Mappings["specialized:high_speed_craft"] = new VesselHullDefaults
        {
            BowFamily = "straight_raked",
            MidshipFamily = "deep_v",
            SternFamily = "transom_stern",
            ChineType = "hard", // Planing hull
            CurvatureType = null, // Neutral (straight lines)
            DeadriseAngleDeg = 10m, // 5-15° (low, flat/shallow V)
            FlareAngleDeg = 5m
        };

        // Research Vessel - Soft Chine (standard)
        Mappings["specialized:research_vessel"] = new VesselHullDefaults
        {
            BowFamily = "bulbous_bow",
            MidshipFamily = "barge_type", // or "fine_midship"
            SternFamily = "transom_stern",
            ChineType = "soft",
            CurvatureType = "convex", // U-shaped bottom
            DeadriseAngleDeg = 10m, // 5-15° range
            FlareAngleDeg = 5m
        };
    }
}

