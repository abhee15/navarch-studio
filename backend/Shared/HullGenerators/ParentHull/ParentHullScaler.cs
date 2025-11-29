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
        decimal lengthTarget,
        decimal beamTarget)
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

        // Ensure forward perpendicular station (last station) is exactly at Lpp
        // This ensures proper bow positioning
        if (scaledStations.Count > 0)
        {
            var forwardStationIdx = scaledStations.Count - 1;
            scaledStations[forwardStationIdx] = lengthTarget;
        }

        // Scale waterlines (draft)
        var scaledWaterlines = parent.Waterlines.Select(wl =>
            wl * scale.Draft
        ).ToList();

        // Scale half-breadths (breadth)
        var scaledOffsets = parent.Offsets.Select(stationOffsets =>
            stationOffsets.Select(hb => hb * scale.Breadth).ToList()
        ).ToList();

        // Ensure forward perpendicular station (last station) closes properly
        // The bow should taper from keel to deck, with keel at zero or very small value
        // This prevents the flared, wing-like appearance at the front
        if (scaledOffsets.Count > 0 && scaledWaterlines.Count > 0)
        {
            var forwardStationIdx = scaledOffsets.Count - 1;
            var forwardStationOffsets = scaledOffsets[forwardStationIdx];

            if (forwardStationOffsets.Count > 0 && scaledWaterlines.Count > 0)
            {
                // Get the maximum draft to normalize waterline positions
                var maxDraft = scaledWaterlines[scaledWaterlines.Count - 1];
                if (maxDraft <= 0) maxDraft = 1m; // Safety check

                // Ensure keel (waterline 0) has zero or very small half-breadth
                var keelHalfBreadth = forwardStationOffsets[0];
                var maxKeelHalfBreadth = beamTarget * 0.1m; // 10% of beam is maximum reasonable

                if (keelHalfBreadth > maxKeelHalfBreadth)
                {
                    // If keel half-breadth is more than 10% of beam, it's likely incorrect
                    // Reduce to a small fraction (2% of beam max) to ensure proper bow closure
                    forwardStationOffsets[0] = Math.Min(keelHalfBreadth, beamTarget * 0.02m);
                }

                // Ensure the forward station tapers properly from keel to deck
                // The half-breadth should increase gradually with waterline height
                // But should never exceed a reasonable maximum for a bow station
                var maxBowHalfBreadth = beamTarget * 0.3m; // Bow should be much narrower than beam

                for (int wlIdx = 1; wlIdx < forwardStationOffsets.Count && wlIdx < scaledWaterlines.Count; wlIdx++)
                {
                    var currentHalfBreadth = forwardStationOffsets[wlIdx];
                    var prevHalfBreadth = forwardStationOffsets[wlIdx - 1];
                    var waterlineZ = scaledWaterlines[wlIdx];
                    var waterlineNorm = waterlineZ / maxDraft; // Normalize to 0-1

                    // Cap the maximum half-breadth at the bow (should be much less than beam/2)
                    if (currentHalfBreadth > maxBowHalfBreadth)
                    {
                        // Reduce to a reasonable maximum, but allow some increase with height
                        var maxAllowed = maxBowHalfBreadth * (0.5m + 0.5m * waterlineNorm); // 50-100% of max based on height
                        forwardStationOffsets[wlIdx] = Math.Min(currentHalfBreadth, maxAllowed);
                    }

                    // Ensure monotonic increase (each waterline should be >= previous)
                    // But don't force it if it would create an unnatural shape
                    if (currentHalfBreadth < prevHalfBreadth * 0.8m)
                    {
                        // If current is significantly less than previous, increase it slightly
                        // This ensures smooth tapering from keel to deck
                        forwardStationOffsets[wlIdx] = Math.Max(currentHalfBreadth, prevHalfBreadth * 0.9m);
                    }
                }
            }
        }

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
