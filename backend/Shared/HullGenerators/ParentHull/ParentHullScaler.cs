namespace Shared.HullGenerators.ParentHull;

/// <summary>
/// Scales parent hull to target dimensions using affine transformation
/// Maintains geometric relationships while adjusting L, B, T
/// </summary>
public static class ParentHullScaler
{
    /// <summary>
    /// Calculate scale factors for affine transformation
    /// </summary>
    public static ScaleFactors CalculateScaleFactors(
        ParentHullData parent,
        decimal lengthTarget,
        decimal beamTarget,
        decimal draftTarget)
    {
        return new ScaleFactors
        {
            Length = lengthTarget / parent.Lbp,
            Breadth = beamTarget / parent.B,
            Draft = draftTarget / parent.T
        };
    }

    /// <summary>
    /// Scale parent hull offsets to target dimensions
    /// </summary>
    public static ScaledHullData ScaleOffsets(
        ParentHullData parent,
        ScaleFactors scale,
        decimal lengthTarget)
    {
        // Scale stations (length)
        // Stations in CSV are normalized 0-10 (BSRA standard)
        // Convert to actual positions: station_actual = (station_normalized / 10) * Lbp_target
        // Simplified: normalizedStation * lengthTarget (instead of normalizedStation * scale.Length * parent.Lbp)
        var scaledStations = parent.Stations.Select(s =>
        {
            // s is normalized 0-10, convert to actual length for target vessel
            decimal normalizedStation = s / 10.0m; // Normalize to 0-1
            return normalizedStation * lengthTarget;
        }).ToList();

        // Scale waterlines (draft)
        var scaledWaterlines = parent.Waterlines.Select(wl =>
            wl * scale.Draft
        ).ToList();

        // Scale half-breadths (breadth)
        var scaledOffsets = parent.Offsets.Select(stationOffsets =>
            stationOffsets.Select(hb => hb * scale.Breadth).ToList()
        ).ToList();

        return new ScaledHullData
        {
            Stations = scaledStations,
            Waterlines = scaledWaterlines,
            Offsets = scaledOffsets,
            ScaleFactors = scale
        };
    }
}

/// <summary>
/// Scale factors for affine transformation
/// </summary>
public class ScaleFactors
{
    public decimal Length { get; set; }
    public decimal Breadth { get; set; }
    public decimal Draft { get; set; }
}

/// <summary>
/// Scaled hull data after affine transformation
/// </summary>
public class ScaledHullData
{
    public List<decimal> Stations { get; set; } = new();
    public List<decimal> Waterlines { get; set; } = new();
    public List<List<decimal>> Offsets { get; set; } = new();
    public ScaleFactors ScaleFactors { get; set; } = new();
}
