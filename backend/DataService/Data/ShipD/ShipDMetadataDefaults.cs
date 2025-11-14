using System.Collections.Generic;

namespace DataService.Data.ShipD;

internal static class ShipDMetadataDefaults
{
    private const string SharedAdditionalParametersJson = "{\"conditionalInputs\":{\"bow\":{\"bulbous_bow\":[\"bit_BB\",\"Lbb\",\"Hbb\",\"Bbb\",\"Lbbm\",\"Rbb\"],\"wave_piercing\":[\"Beta\",\"Rc\",\"Rk\"],\"fine_entry\":[\"Beta\",\"Rc\"],\"axe_bow\":[\"Beta\",\"Rk\"]},\"midship\":{\"deep_v_midship\":[\"Adrft\",\"Bdrft\",\"Cdrft\"],\"barge_midship\":[\"bit_EP_S\"],\"fine_midship\":[\"bit_EP_T\"]},\"stern\":{\"transom_stern\":[\"Atrans\",\"Beta_trans\",\"Bc_trans\",\"Rc_trans\",\"Rk_trans\"],\"twin_skeg\":[\"SK_z\",\"Kappa_stern\",\"Lsb\",\"Hsb\",\"Bsb\"],\"skeg_stern\":[\"SK_z\",\"Kappa_stern\",\"Lsb\",\"HSBOA\"],\"canoe_stern\":[\"Adel_stern\",\"Bdel_stern\"]}},\"familyDefaults\":{\"bulbous_bow\":{\"bit_BB\":1},\"wave_piercing\":{\"bit_BB\":0,\"Beta\":5},\"transom_stern\":{\"Atrans\":0.5},\"twin_skeg\":{\"bit_SB\":1}}}";

    // Vessel-type-specific default length ratios (bow, stern, midship = 1 - bow - stern)
    private const string ContainerDefaults = "{\"bowLengthRatio\":0.30,\"sternLengthRatio\":0.30}";
    private const string YachtDefaults = "{\"bowLengthRatio\":0.45,\"sternLengthRatio\":0.35}";
    private const string TankerDefaults = "{\"bowLengthRatio\":0.30,\"sternLengthRatio\":0.30}";
    private const string FishingDefaults = "{\"bowLengthRatio\":0.35,\"sternLengthRatio\":0.35}";
    private const string GeneralCargoDefaults = "{\"bowLengthRatio\":0.30,\"sternLengthRatio\":0.30}";
    private const string DefaultFallback = "{\"bowLengthRatio\":0.30,\"sternLengthRatio\":0.30}";

    private static string MergeAdditionalParameters(string baseJson, string defaultsJson)
    {
        try
        {
            var baseObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(baseJson) ?? new Dictionary<string, object>();
            var defaultsObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(defaultsJson) ?? new Dictionary<string, object>();

            foreach (var kvp in defaultsObj)
            {
                baseObj[kvp.Key] = kvp.Value;
            }

            return System.Text.Json.JsonSerializer.Serialize(baseObj);
        }
        catch
        {
            return baseJson;
        }
    }

    public static readonly ShipDParameterMetadataSeed[] ParameterMetadata =
    {
        new ShipDParameterMetadataSeed(0, "LOA", "principal_dimensions", "Baseline length parameter used for ShipD normalization", null, 10.0m, 10.0m, 10.0m, 0.0m, "{\"normalized\":false,\"constant\":true}"),
        new ShipDParameterMetadataSeed(1, "Lb", "principal_dimensions", "Bow length ratio relative to LOA", null, 0.050034m, 0.899679m, 0.31607m, 0.19205m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(2, "Ls", "principal_dimensions", "Stern length ratio relative to LOA", null, 0.003731m, 0.89978m, 0.419778m, 0.207796m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(3, "Bd", "principal_dimensions", "Design beam ratio at midship", null, 0.083351m, 0.332988m, 0.224154m, 0.06966m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(4, "Dd", "principal_dimensions", "Design draft ratio at midship", null, 0.050003m, 0.249991m, 0.159692m, 0.05439m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(5, "Bs", "principal_dimensions", "Bulb scaling coefficient controlling bow volume", null, 0.000091m, 0.999992m, 0.476249m, 0.283354m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(6, "WL", "principal_dimensions", "Waterline length ratio", null, 0.050025m, 0.79995m, 0.393125m, 0.214108m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(7, "Bc", "principal_dimensions", "Beam at chine relative to design beam", null, 0.050002m, 0.49999m, 0.261348m, 0.119467m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(8, "Beta", "bow", "Bow flare angle measured in degrees", "deg", 0.0m, 44.994361m, 14.307295m, 14.490381m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(9, "Rc", "bow", "Bow curvature coefficient", null, 0.00003m, 0.999894m, 0.410454m, 0.27158m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(10, "Rk", "bow", "Bow knuckle curvature coefficient", null, -0.99944m, 0.999886m, 0.34541m, 0.417268m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(11, "Abow", "bow", "Primary bow area spline coefficient", null, -3.99983m, 3.999316m, 0.272184m, 2.21203m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(12, "Bbow", "bow", "Secondary bow area spline coefficient", null, -3.999356m, 3.999777m, -0.237424m, 2.264825m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(13, "BK_z", "bow", "Bow knuckle vertical location control", null, 0.000009m, 0.999991m, 0.515702m, 0.2911m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(14, "Kappa_bow", "bow", "Bow sectional curvature parameter", null, 0.000004m, 0.999922m, 0.474204m, 0.252446m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(15, "Adel_bow", "bow", "Forward sheer/delta coefficient A", null, -3.999979m, 3.999643m, 0.38857m, 2.203062m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(16, "Bdel_bow", "bow", "Forward sheer/delta coefficient B", null, -3.999733m, 3.999983m, -0.45257m, 2.240023m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(17, "Adrft", "bow", "Bow draft rocker coefficient A", null, -3.999915m, 3.986352m, -0.624908m, 1.953151m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(18, "Bdrft", "bow", "Bow draft rocker coefficient B", null, -3.981324m, 3.999895m, 0.733312m, 1.935015m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(19, "Cdrft", "bow", "Forward deadrise / flare control angle", "deg", 0.010776m, 59.999478m, 32.641421m, 16.44855m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(20, "bit_EP_S", "midship", "Midship sheer extrusion toggle", null, 0.0m, 1.0m, 0.497867m, 0.499995m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(21, "bit_EP_T", "midship", "Midship tumblehome toggle", null, 0.0m, 1.0m, 0.496933m, 0.499991m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(22, "Atrans", "stern", "Transom area coefficient", null, -2.999687m, 4.997124m, -0.040188m, 1.77236m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(23, "SK_z", "stern", "Skeg vertical offset control", null, 0.000017m, 0.997469m, 0.477592m, 0.247824m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(24, "Kappa_stern", "stern", "Stern sectional curvature parameter", null, 0.000057m, 0.999919m, 0.512123m, 0.259396m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(25, "Adel_stern", "stern", "Aft sheer/delta coefficient A", null, -3.999967m, 3.99891m, -0.49521m, 2.160727m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(26, "Bdel_stern", "stern", "Aft sheer/delta coefficient B", null, -3.999205m, 3.999957m, 0.560869m, 2.186946m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(27, "Beta_trans", "stern", "Transom rake angle in degrees", "deg", 0.000238m, 59.999538m, 23.547834m, 16.242139m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(28, "Bc_trans", "stern", "Transom beam coefficient", null, 0.000255m, 0.463976m, 0.109606m, 0.067919m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(29, "Rc_trans", "stern", "Transom curvature coefficient", null, 0.000006m, 0.499987m, 0.236096m, 0.14342m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(30, "Rk_trans", "stern", "Transom knuckle curvature coefficient", null, -0.999742m, 0.499976m, -0.062163m, 0.351683m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(31, "bit_BB", "appendages", "Bulbous bow activation toggle", null, 0.0m, 1.0m, 0.214133m, 0.41022m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(32, "bit_SB", "appendages", "Skeg/bilge keel activation toggle", null, 0.0m, 1.0m, 0.218933m, 0.413523m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(33, "Lbb", "appendages", "Bulb length ratio", null, 0.000001m, 0.199996m, 0.099315m, 0.057773m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(34, "Hbb", "appendages", "Bulb height ratio", null, 0.000027m, 0.999998m, 0.499516m, 0.288899m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(35, "Bbb", "appendages", "Bulb beam ratio", null, 0.000004m, 0.999994m, 0.496382m, 0.287775m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(36, "Lbbm", "appendages", "Bulb longitudinal moment coefficient", null, -0.999783m, 0.999864m, 0.013882m, 0.575103m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(37, "Rbb", "appendages", "Bulb radius coefficient", null, 0.050022m, 0.329993m, 0.190013m, 0.080697m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(38, "Kappa_SB", "appendages", "Skeg/bilge keel curvature parameter", null, 0.000033m, 1.0m, 0.517176m, 0.284656m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(39, "Lsb", "appendages", "Skeg length ratio", null, 0.00001m, 0.199998m, 0.100181m, 0.057544m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(40, "HSBOA", "appendages", "Skeg height to breadth ratio", null, 0.000002m, 0.999988m, 0.487799m, 0.28993m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(41, "Hsb", "appendages", "Skeg height ratio", null, 0.000093m, 0.999904m, 0.496874m, 0.289452m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(42, "Bsb", "appendages", "Skeg breadth ratio", null, 0.000007m, 0.999929m, 0.496778m, 0.28748m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(43, "Lsbm", "appendages", "Skeg longitudinal moment coefficient", null, -0.999968m, 0.999922m, 0.028226m, 0.576118m, "{\"normalized\":true}"),
        new ShipDParameterMetadataSeed(44, "Rsb", "appendages", "Skeg radius coefficient", null, 0.050005m, 0.329999m, 0.190094m, 0.080647m, "{\"normalized\":true}")
    };

    public static readonly ShipDVesselTaxonomySeed[] VesselTaxonomy =
    {
        new ShipDVesselTaxonomySeed(
            "Commercial",
            "general_cargo",
            "Commercial – General Cargo",
            null,
            new[] { "bulbous_bow", "straight_raked", "fine_entry" },
            new[] { "full_midship", "fine_midship" },
            new[] { "transom_stern", "cruiser_stern" },
            1,
            MergeAdditionalParameters(SharedAdditionalParametersJson, GeneralCargoDefaults)),
        new ShipDVesselTaxonomySeed(
            "Commercial",
            "bulk_carrier",
            "Commercial – Bulk Carrier",
            null,
            new[] { "bulbous_bow", "straight_raked" },
            new[] { "full_midship" },
            new[] { "transom_stern", "cruiser_stern" },
            1,
            MergeAdditionalParameters(SharedAdditionalParametersJson, GeneralCargoDefaults)),
        new ShipDVesselTaxonomySeed(
            "Commercial",
            "container",
            "Commercial – Container Ship",
            null,
            new[] { "bulbous_bow", "axe_bow" },
            new[] { "fine_midship" },
            new[] { "transom_stern", "twin_skeg" },
            1,
            MergeAdditionalParameters(SharedAdditionalParametersJson, ContainerDefaults)),
        new ShipDVesselTaxonomySeed(
            "Commercial",
            "fishing",
            "Commercial – Fishing Vessel",
            null,
            new[] { "fine_entry", "wave_piercing" },
            new[] { "deep_v_midship", "fine_midship" },
            new[] { "canoe_stern", "wedge_stern" },
            1,
            MergeAdditionalParameters(SharedAdditionalParametersJson, FishingDefaults)),
        new ShipDVesselTaxonomySeed(
            "Commercial",
            "tanker",
            "Commercial – Tanker",
            null,
            new[] { "bulbous_bow", "straight_raked" },
            new[] { "full_midship" },
            new[] { "transom_stern", "cruiser_stern" },
            1,
            MergeAdditionalParameters(SharedAdditionalParametersJson, TankerDefaults)),
        new ShipDVesselTaxonomySeed(
            "Commercial",
            "lng_carrier",
            "Commercial – LNG Carrier",
            null,
            new[] { "bulbous_bow", "wave_piercing" },
            new[] { "full_midship", "fine_midship" },
            new[] { "transom_stern", "twin_skeg" },
            1,
            MergeAdditionalParameters(SharedAdditionalParametersJson, ContainerDefaults)),
        new ShipDVesselTaxonomySeed(
            "Commercial",
            "cruise_vessel",
            "Commercial – Cruise Vessel",
            null,
            new[] { "bulbous_bow", "fine_entry" },
            new[] { "full_midship" },
            new[] { "cruiser_stern", "transom_stern" },
            1,
            MergeAdditionalParameters(SharedAdditionalParametersJson, GeneralCargoDefaults)),
        new ShipDVesselTaxonomySeed(
            "Commercial",
            "passenger_vessel",
            "Commercial – Passenger Vessel",
            null,
            new[] { "bulbous_bow", "fine_entry" },
            new[] { "full_midship", "fine_midship" },
            new[] { "cruiser_stern", "transom_stern" },
            1,
            MergeAdditionalParameters(SharedAdditionalParametersJson, GeneralCargoDefaults)),
        new ShipDVesselTaxonomySeed(
            "Government",
            "cutters",
            "Government – Cutter",
            null,
            new[] { "fine_entry", "axe_bow" },
            new[] { "deep_v_midship", "fine_midship" },
            new[] { "transom_stern", "canoe_stern" },
            1,
            MergeAdditionalParameters(SharedAdditionalParametersJson, DefaultFallback)),
        new ShipDVesselTaxonomySeed(
            "Government",
            "medical_ship",
            "Government – Medical Ship",
            null,
            new[] { "bulbous_bow", "straight_raked" },
            new[] { "full_midship" },
            new[] { "cruiser_stern", "transom_stern" },
            1,
            MergeAdditionalParameters(SharedAdditionalParametersJson, GeneralCargoDefaults)),
        new ShipDVesselTaxonomySeed(
            "Government",
            "general_military",
            "Government – General Military",
            null,
            new[] { "fine_entry", "axe_bow" },
            new[] { "deep_v_midship" },
            new[] { "transom_stern", "skeg_stern" },
            1,
            MergeAdditionalParameters(SharedAdditionalParametersJson, DefaultFallback)),
        new ShipDVesselTaxonomySeed(
            "Recreational",
            "yacht",
            "Recreational – Yacht",
            null,
            new[] { "wave_piercing", "fine_entry" },
            new[] { "deep_v_midship" },
            new[] { "transom_stern", "canoe_stern" },
            1,
            MergeAdditionalParameters(SharedAdditionalParametersJson, YachtDefaults)),
        new ShipDVesselTaxonomySeed(
            "Recreational",
            "fishing_recreational",
            "Recreational – Fishing",
            null,
            new[] { "fine_entry", "straight_raked" },
            new[] { "deep_v_midship", "barge_midship" },
            new[] { "wedge_stern", "transom_stern" },
            1,
            MergeAdditionalParameters(SharedAdditionalParametersJson, FishingDefaults)),
        new ShipDVesselTaxonomySeed(
            "Recreational",
            "high_speed_craft",
            "Recreational – High Speed Craft",
            null,
            new[] { "axe_bow", "wave_piercing" },
            new[] { "deep_v_midship" },
            new[] { "transom_stern", "twin_skeg" },
            1,
            MergeAdditionalParameters(SharedAdditionalParametersJson, DefaultFallback)),
        new ShipDVesselTaxonomySeed(
            "Research",
            "research_vessel",
            "Research – Oceanographic Research Vessel",
            null,
            new[] { "bulbous_bow", "wave_piercing" },
            new[] { "fine_midship", "full_midship" },
            new[] { "cruiser_stern", "transom_stern" },
            1,
            MergeAdditionalParameters(SharedAdditionalParametersJson, GeneralCargoDefaults))
    };

    internal sealed record ShipDParameterMetadataSeed(
        int ParameterIndex,
        string Label,
        string Group,
        string? Description,
        string? Unit,
        decimal Min,
        decimal Max,
        decimal Mean,
        decimal StdDev,
        string? MetadataJson);

    internal sealed record ShipDVesselTaxonomySeed(
        string Category,
        string Type,
        string DisplayName,
        string? Description,
        IReadOnlyList<string> BowFamilies,
        IReadOnlyList<string> MidshipFamilies,
        IReadOnlyList<string> SternFamilies,
        int MaskVersion,
        string? AdditionalParametersJson);
}
