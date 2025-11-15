namespace Shared.Constants;

/// <summary>
/// Parent hull offset table constants
/// Contains offset data for parent hulls used in offset generation
/// Converted from CSV files for better maintainability and testability
/// </summary>
public static class ParentHullOffsetConstants
{
    /// <summary>
    /// Get offset data for product carrier with Cb=0.80
    /// Returns stations, waterlines, and offsets as a tuple
    /// Stations are normalized 0-10 (BSRA standard)
    /// </summary>
    public static (List<decimal> Stations, List<decimal> Waterlines, List<List<decimal>> Offsets)
        GetProductCarrierCb080Offsets()
    {
        // Waterlines (from header: wl_1, wl_2, ..., wl_16.4)
        var waterlines = new List<decimal>
        {
            1.0m, 2.0m, 3.0m, 4.0m, 5.0m, 6.0m, 7.0m, 8.0m, 9.0m, 10.0m,
            11.0m, 12.0m, 13.0m, 14.0m, 15.0m, 16.0m, 16.4m
        };

        // Stations (normalized 0-10, BSRA standard)
        var stations = new List<decimal>
        {
            0.0m, 0.25m, 0.5m, 0.75m, 1.0m, 1.5m, 2.0m, 2.5m, 3.0m, 3.5m,
            4.0m, 5.0m, 6.0m, 6.5m, 7.0m, 7.5m, 8.0m, 8.5m, 9.0m, 9.25m,
            9.5m, 9.75m, 10.0m
        };

        // Offsets (half-breadths in meters)
        // Each row corresponds to a station, each column to a waterline
        var offsets = new List<List<decimal>>
        {
            // Station 0 (AP)
            new() { 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0m, 0.3m, 2.1m, 3.05m, 3.8m, 4.35m, 4.9m, 5.35m, 5.5m },
            // Station 0.25
            new() { 0.2m, 0.3m, 0.35m, 0.39m, 0.41m, 0.47m, 0.6m, 0.95m, 1.65m, 2.9m, 4m, 4.95m, 5.8m, 6.45m, 7m, 7.4m, 7.55m },
            // Station 0.5
            new() { 0.8m, 1.15m, 1.4m, 1.7m, 1.95m, 2.3m, 2.7m, 3.25m, 4m, 4.85m, 5.9m, 6.8m, 7.55m, 8.15m, 8.7m, 9.15m, 9.35m },
            // Station 0.75
            new() { 1.8m, 2.4m, 2.95m, 3.4m, 3.85m, 4.35m, 4.85m, 5.45m, 6.1m, 6.9m, 7.7m, 8.4m, 9.05m, 9.6m, 10.05m, 10.5m, 10.7m },
            // Station 1.0
            new() { 2.6m, 3.65m, 4.45m, 5.05m, 5.65m, 6.15m, 6.7m, 7.3m, 7.95m, 8.6m, 9.25m, 9.8m, 10.35m, 10.85m, 11.25m, 11.65m, 11.8m },
            // Station 1.5
            new() { 5.25m, 6.5m, 7.4m, 8.1m, 8.75m, 9.35m, 9.85m, 10.3m, 10.8m, 11.2m, 11.6m, 12m, 12.3m, 12.6m, 12.85m, 13.1m, 13.2m },
            // Station 2.0
            new() { 8.15m, 9.4m, 10.25m, 10.85m, 11.35m, 11.75m, 12.1m, 12.4m, 12.75m, 13m, 13.2m, 13.3m, 13.45m, 13.55m, 13.65m, 13.75m, 13.8m },
            // Station 2.5
            new() { 10.6m, 11.65m, 12.3m, 12.75m, 13.05m, 13.3m, 13.45m, 13.6m, 13.75m, 13.85m, 13.9m, 13.95m, 14m, 14m, 14m, 14m, 14m },
            // Station 3.0
            new() { 12.25m, 13m, 13.45m, 13.7m, 13.85m, 13.95m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m },
            // Station 3.5
            new() { 13.15m, 13.7m, 13.95m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m },
            // Station 4.0
            new() { 13.45m, 13.85m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m },
            // Station 5.0 (Midship)
            new() { 13.6m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m },
            // Station 6.0
            new() { 13.6m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m },
            // Station 6.5
            new() { 13.55m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m },
            // Station 7.0
            new() { 13.45m, 13.9m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m },
            // Station 7.5
            new() { 13.25m, 13.7m, 13.9m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m },
            // Station 8.0
            new() { 12.4m, 13m, 13.35m, 13.55m, 13.65m, 13.8m, 13.87m, 13.95m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m, 14m },
            // Station 8.5
            new() { 10.5m, 11.35m, 11.85m, 12.25m, 12.5m, 12.7m, 12.85m, 12.95m, 13m, 13.1m, 13.2m, 13.27m, 13.4m, 13.5m, 13.65m, 13.75m, 13.8m },
            // Station 9.0
            new() { 7.45m, 8.5m, 9.15m, 9.6m, 9.9m, 10.2m, 10.4m, 10.5m, 10.6m, 10.7m, 10.85m, 11m, 11.2m, 11.5m, 11.8m, 12.1m, 12.25m },
            // Station 9.25
            new() { 5.55m, 6.5m, 7.1m, 7.6m, 7.9m, 8.15m, 8.35m, 8.5m, 8.6m, 8.75m, 8.95m, 9.15m, 9.45m, 9.75m, 10.1m, 10.45m, 10.6m },
            // Station 9.5
            new() { 3.6m, 4.5m, 5.05m, 5.45m, 5.75m, 5.85m, 5.95m, 6.05m, 6.1m, 6.2m, 6.4m, 6.65m, 6.9m, 7.25m, 7.6m, 8m, 8.15m },
            // Station 9.75
            new() { 2.25m, 2.9m, 3.25m, 3.35m, 3.42m, 3.45m, 3.48m, 3.5m, 3.57m, 3.62m, 3.67m, 3.75m, 3.8m, 4m, 4.25m, 4.55m, 4.65m },
            // Station 10.0 (FP)
            new() { 1.4m, 1.9m, 2.05m, 1.95m, 1.7m, 1.35m, 1.05m, 0.75m, 0.5m, 0.25m, 0.1m, 0.05m, 0m, 0.1m, 0.25m, 0.55m, 0.7m }
        };

        return (stations, waterlines, offsets);
    }

    /// <summary>
    /// Get offset data for a specific vessel type and Cb
    /// Currently only supports product_carrier with Cb=0.80
    /// </summary>
    public static (List<decimal> Stations, List<decimal> Waterlines, List<List<decimal>> Offsets)?
        GetOffsets(string vesselType, decimal cb)
    {
        // Normalize Cb to integer for comparison (e.g., 0.80 -> 80)
        var cbInt = (int)(cb * 100);

        if (string.Equals(vesselType, "product_carrier", StringComparison.OrdinalIgnoreCase) && cbInt == 80)
        {
            return GetProductCarrierCb080Offsets();
        }

        // Other vessel types/Cb combinations can be added here as needed
        return null;
    }
}

