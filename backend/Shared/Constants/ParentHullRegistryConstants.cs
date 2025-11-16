namespace Shared.Constants;

/// <summary>
/// Parent hull registry constants
/// Contains reference data for parent hulls used in offset generation
/// Converted from parent_hulls_registry.csv for better maintainability and testability
/// </summary>
public static class ParentHullRegistryConstants
{
    /// <summary>
    /// Get all parent hull registry entries
    /// </summary>
    public static List<ParentHullRegistryEntry> GetAllEntries()
    {
        return new List<ParentHullRegistryEntry>
        {
            // Product carriers
            new ParentHullRegistryEntry
            {
                VesselType = "product_carrier",
                Cb = 0.792m,
                Lbp = 185m,
                B = 28m,
                D = 16.4m,
                T = 12.87m,
                Cm = 0.98m,
                Cw = 0.88m,
                LcbPercent = 2.08m,
                Source = "BSRA_Fig54",
                Notes = "40000DWT_14kts"
            },
            new ParentHullRegistryEntry
            {
                VesselType = "product_carrier",
                Cb = 0.80m,
                Lbp = 185m,
                B = 28m,
                D = 16.4m,
                T = 12.87m,
                Cm = 0.99m,
                Cw = 0.87m,
                LcbPercent = 2.08m,
                Source = "Document_Table_VI",
                Notes = "Standard_form"
            },
            new ParentHullRegistryEntry
            {
                VesselType = "product_carrier",
                Cb = 0.75m,
                Lbp = 170m,
                B = 26m,
                D = 15.5m,
                T = 11.5m,
                Cm = 0.98m,
                Cw = 0.86m,
                LcbPercent = 2.0m,
                Source = "BSRA_Interpolated",
                Notes = "Lower_Cb_variant"
            },
            new ParentHullRegistryEntry
            {
                VesselType = "product_carrier",
                Cb = 0.85m,
                Lbp = 200m,
                B = 30m,
                D = 17.5m,
                T = 14.0m,
                Cm = 0.99m,
                Cw = 0.89m,
                LcbPercent = 2.2m,
                Source = "BSRA_Interpolated",
                Notes = "Higher_Cb_variant"
            },
            // Bulk carriers
            new ParentHullRegistryEntry
            {
                VesselType = "bulk_carrier",
                Cb = 0.80m,
                Lbp = 200m,
                B = 30m,
                D = 17.0m,
                T = 13.0m,
                Cm = 0.99m,
                Cw = 0.88m,
                LcbPercent = 1.5m,
                Source = "Series_60",
                Notes = "Standard"
            },
            new ParentHullRegistryEntry
            {
                VesselType = "bulk_carrier",
                Cb = 0.85m,
                Lbp = 220m,
                B = 32m,
                D = 18.5m,
                T = 14.2m,
                Cm = 0.995m,
                Cw = 0.90m,
                LcbPercent = 1.5m,
                Source = "Series_60",
                Notes = "Modified"
            },
            new ParentHullRegistryEntry
            {
                VesselType = "bulk_carrier",
                Cb = 0.88m,
                Lbp = 240m,
                B = 34m,
                D = 19.5m,
                T = 15.0m,
                Cm = 0.996m,
                Cw = 0.91m,
                LcbPercent = 1.3m,
                Source = "Series_60",
                Notes = "High_Cb"
            },
            // Tankers
            new ParentHullRegistryEntry
            {
                VesselType = "tanker",
                Cb = 0.78m,
                Lbp = 220m,
                B = 40m,
                D = 20.0m,
                T = 15.0m,
                Cm = 0.99m,
                Cw = 0.88m,
                LcbPercent = 2.0m,
                Source = "BSRA_Tanker",
                Notes = "Medium_size"
            },
            new ParentHullRegistryEntry
            {
                VesselType = "tanker",
                Cb = 0.82m,
                Lbp = 240m,
                B = 42m,
                D = 21.0m,
                T = 16.5m,
                Cm = 0.99m,
                Cw = 0.89m,
                LcbPercent = 2.0m,
                Source = "BSRA_Tanker",
                Notes = "VLCC_type"
            },
            new ParentHullRegistryEntry
            {
                VesselType = "tanker",
                Cb = 0.86m,
                Lbp = 260m,
                B = 44m,
                D = 22.0m,
                T = 17.5m,
                Cm = 0.995m,
                Cw = 0.90m,
                LcbPercent = 1.8m,
                Source = "BSRA_Tanker",
                Notes = "ULCC_type"
            },
            // Container ships
            new ParentHullRegistryEntry
            {
                VesselType = "container",
                Cb = 0.55m,
                Lbp = 250m,
                B = 38m,
                D = 22.0m,
                T = 12.0m,
                Cm = 0.97m,
                Cw = 0.73m,
                LcbPercent = 4.0m,
                Source = "Series_60",
                Notes = "Very_high_speed"
            },
            new ParentHullRegistryEntry
            {
                VesselType = "container",
                Cb = 0.60m,
                Lbp = 280m,
                B = 40m,
                D = 24.0m,
                T = 13.5m,
                Cm = 0.98m,
                Cw = 0.75m,
                LcbPercent = 3.5m,
                Source = "Series_60",
                Notes = "High_speed"
            },
            new ParentHullRegistryEntry
            {
                VesselType = "container",
                Cb = 0.65m,
                Lbp = 300m,
                B = 42m,
                D = 25.0m,
                T = 14.5m,
                Cm = 0.98m,
                Cw = 0.77m,
                LcbPercent = 3.0m,
                Source = "Series_60",
                Notes = "Standard"
            },
            // General cargo
            new ParentHullRegistryEntry
            {
                VesselType = "general_cargo",
                Cb = 0.68m,
                Lbp = 140m,
                B = 22m,
                D = 13.0m,
                T = 9.0m,
                Cm = 0.96m,
                Cw = 0.81m,
                LcbPercent = 2.5m,
                Source = "NPL_Series",
                Notes = "Small"
            },
            new ParentHullRegistryEntry
            {
                VesselType = "general_cargo",
                Cb = 0.70m,
                Lbp = 150m,
                B = 23m,
                D = 13.5m,
                T = 9.8m,
                Cm = 0.96m,
                Cw = 0.82m,
                LcbPercent = 2.5m,
                Source = "NPL_Series",
                Notes = "Multi_purpose"
            },
            new ParentHullRegistryEntry
            {
                VesselType = "general_cargo",
                Cb = 0.72m,
                Lbp = 160m,
                B = 24m,
                D = 14.0m,
                T = 10.5m,
                Cm = 0.97m,
                Cw = 0.83m,
                LcbPercent = 2.3m,
                Source = "NPL_Series",
                Notes = "Large"
            },
            // RoRo
            new ParentHullRegistryEntry
            {
                VesselType = "roro",
                Cb = 0.65m,
                Lbp = 180m,
                B = 28m,
                D = 20.0m,
                T = 7.5m,
                Cm = 0.97m,
                Cw = 0.78m,
                LcbPercent = 2.0m,
                Source = "Series_60",
                Notes = "Standard"
            },
            new ParentHullRegistryEntry
            {
                VesselType = "roro",
                Cb = 0.70m,
                Lbp = 200m,
                B = 30m,
                D = 22.0m,
                T = 8.5m,
                Cm = 0.98m,
                Cw = 0.80m,
                LcbPercent = 1.8m,
                Source = "Series_60",
                Notes = "Large"
            },
            // Fast ferry
            new ParentHullRegistryEntry
            {
                VesselType = "fast_ferry",
                Cb = 0.50m,
                Lbp = 80m,
                B = 12m,
                D = 8.0m,
                T = 3.5m,
                Cm = 0.95m,
                Cw = 0.70m,
                LcbPercent = 5.0m,
                Source = "High_Speed_Series",
                Notes = "Small"
            },
            new ParentHullRegistryEntry
            {
                VesselType = "fast_ferry",
                Cb = 0.55m,
                Lbp = 100m,
                B = 14m,
                D = 9.0m,
                T = 4.0m,
                Cm = 0.96m,
                Cw = 0.72m,
                LcbPercent = 4.5m,
                Source = "High_Speed_Series",
                Notes = "Medium"
            },
            // Yacht
            new ParentHullRegistryEntry
            {
                VesselType = "yacht",
                Cb = 0.45m,
                Lbp = 50m,
                B = 10m,
                D = 6.0m,
                T = 2.5m,
                Cm = 0.94m,
                Cw = 0.68m,
                LcbPercent = 6.0m,
                Source = "Yacht_Series",
                Notes = "Small"
            },
            new ParentHullRegistryEntry
            {
                VesselType = "yacht",
                Cb = 0.50m,
                Lbp = 60m,
                B = 12m,
                D = 7.0m,
                T = 3.0m,
                Cm = 0.95m,
                Cw = 0.70m,
                LcbPercent = 5.5m,
                Source = "Yacht_Series",
                Notes = "Medium"
            },
            // Cruise
            new ParentHullRegistryEntry
            {
                VesselType = "cruise",
                Cb = 0.65m,
                Lbp = 300m,
                B = 40m,
                D = 25.0m,
                T = 8.5m,
                Cm = 0.98m,
                Cw = 0.77m,
                LcbPercent = 3.0m,
                Source = "Cruise_Series",
                Notes = "Standard"
            },
            new ParentHullRegistryEntry
            {
                VesselType = "cruise",
                Cb = 0.70m,
                Lbp = 320m,
                B = 42m,
                D = 26.0m,
                T = 9.0m,
                Cm = 0.98m,
                Cw = 0.79m,
                LcbPercent = 2.8m,
                Source = "Cruise_Series",
                Notes = "Large"
            }
        };
    }
}

/// <summary>
/// Parent hull registry entry matching the CSV structure
/// </summary>
public class ParentHullRegistryEntry
{
    public string VesselType { get; init; } = string.Empty;
    public decimal Cb { get; init; }
    public decimal Lbp { get; init; }
    public decimal B { get; init; }
    public decimal D { get; init; }
    public decimal T { get; init; }
    public decimal Cm { get; init; }
    public decimal Cw { get; init; }
    public decimal LcbPercent { get; init; }
    public string Source { get; init; } = string.Empty;
    public string? Notes { get; init; }
}
