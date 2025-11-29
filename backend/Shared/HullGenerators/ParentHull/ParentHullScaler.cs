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

        // Ensure forward stations (last 3-4 stations) close properly to form a fine bow
        // The bow should taper from keel to deck, with keel at zero or very small value
        // This prevents the flared, wing-like appearance at the front
        if (scaledOffsets.Count > 0 && scaledWaterlines.Count > 0)
        {
            // Get the maximum draft to normalize waterline positions
            var maxDraft = scaledWaterlines[scaledWaterlines.Count - 1];
            if (maxDraft <= 0) maxDraft = 1m; // Safety check

            // Fix the last 4 stations (bow region) - stations 19, 20, 21, 22 for 23-station setup
            // This ensures the entire bow region tapers properly
            int numBowStations = Math.Min(4, scaledOffsets.Count);
            int firstBowStationIdx = scaledOffsets.Count - numBowStations;

            for (int stIdx = firstBowStationIdx; stIdx < scaledOffsets.Count; stIdx++)
            {
                var stationOffsets = scaledOffsets[stIdx];
                if (stationOffsets.Count == 0) continue;

                // Calculate how far forward this station is (0 = aft, 1 = forward)
                // More forward stations need more aggressive correction
                decimal forwardness = (decimal)(stIdx - firstBowStationIdx) / numBowStations; // 0 to 1

                // Maximum allowed half-breadth decreases as we go forward
                // Station 22 (forward perpendicular): max 15% of beam
                // Station 21: max 25% of beam
                // Station 20: max 35% of beam
                // Station 19: max 45% of beam
                var maxStationHalfBreadth = beamTarget * (0.15m + 0.3m * (1m - forwardness));

                // Ensure keel (waterline 0) has zero or very small half-breadth
                var keelHalfBreadth = stationOffsets[0];
                var maxKeelHalfBreadth = beamTarget * (0.02m + 0.03m * (1m - forwardness)); // 2-5% of beam

                if (keelHalfBreadth > maxKeelHalfBreadth)
                {
                    stationOffsets[0] = Math.Min(keelHalfBreadth, maxKeelHalfBreadth);
                }

                // Ensure the station tapers properly from keel to deck
                for (int wlIdx = 1; wlIdx < stationOffsets.Count && wlIdx < scaledWaterlines.Count; wlIdx++)
                {
                    var currentHalfBreadth = stationOffsets[wlIdx];
                    var prevHalfBreadth = stationOffsets[wlIdx - 1];
                    var waterlineZ = scaledWaterlines[wlIdx];
                    var waterlineNorm = waterlineZ / maxDraft; // Normalize to 0-1

                    // Cap the maximum half-breadth based on forwardness and waterline height
                    if (currentHalfBreadth > maxStationHalfBreadth)
                    {
                        // Allow some increase with height, but cap based on forwardness
                        var maxAllowed = maxStationHalfBreadth * (0.3m + 0.7m * waterlineNorm);
                        stationOffsets[wlIdx] = Math.Min(currentHalfBreadth, maxAllowed);
                    }

                    // Ensure smooth tapering - each waterline should be >= previous
                    if (currentHalfBreadth < prevHalfBreadth * 0.85m)
                    {
                        // If current is significantly less than previous, increase it slightly
                        stationOffsets[wlIdx] = Math.Max(currentHalfBreadth, prevHalfBreadth * 0.9m);
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
