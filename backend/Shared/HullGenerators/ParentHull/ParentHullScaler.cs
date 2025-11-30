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

        // Ensure forward perpendicular station (last station) closes properly to form a fine bow
        // The bow should taper from keel to deck, with keel at zero or very small value
        // This prevents the flared, wing-like appearance at the front
        // Only apply to the forward region (last 20% of stations, minimum 1, maximum 4)
        if (scaledOffsets.Count > 0 && scaledWaterlines.Count > 0)
        {
            // Get the maximum draft to normalize waterline positions
            var maxDraft = scaledWaterlines[scaledWaterlines.Count - 1];
            if (maxDraft <= 0) maxDraft = 1m; // Safety check

            // Only fix forward stations: last 20% of stations, but at least 1 and at most 4
            // For 23-station setup: 20% = 4.6 → 4 stations (19, 20, 21, 22)
            // For 3-station setup: 20% = 0.6 → 1 station (just the last one)
            int numBowStations = Math.Max(1, Math.Min(4, (int)Math.Ceiling(scaledOffsets.Count * 0.2m)));
            int firstBowStationIdx = scaledOffsets.Count - numBowStations;

            // Only apply fix if we have enough stations to distinguish bow from midship
            // For very small hulls (< 10 stations), only fix the very last station
            if (scaledOffsets.Count < 10)
            {
                numBowStations = 1;
                firstBowStationIdx = scaledOffsets.Count - 1;
            }

            for (int stIdx = firstBowStationIdx; stIdx < scaledOffsets.Count; stIdx++)
            {
                var stationOffsets = scaledOffsets[stIdx];
                if (stationOffsets.Count == 0) continue;

                // Calculate how far forward this station is (0 = start of bow region, 1 = forward perpendicular)
                decimal forwardness = numBowStations > 1
                    ? (decimal)(stIdx - firstBowStationIdx) / (numBowStations - 1)
                    : 1m; // If only one station, it's fully forward

                // Maximum allowed half-breadth decreases as we go forward
                // Forward perpendicular: max 15% of beam
                // Start of bow region: max 45% of beam
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

        // Ensure aft perpendicular station (first station, index 0) closes properly to form a fine stern
        // POST-PROCESSING ONLY: This does NOT change offset generation, only corrects scaling artifacts
        // Check if stern is wider than adjacent station (indicates scaling artifact)
        // The "V pattern" in top view is preserved - we only cap unreasonable values
        if (scaledOffsets.Count > 1 && scaledWaterlines.Count > 0)
        {
            var maxDraft = scaledWaterlines[scaledWaterlines.Count - 1];
            if (maxDraft <= 0) maxDraft = 1m;

            // Fix the first station (aft perpendicular) if it's wider than the adjacent station
            // This indicates a scaling artifact, not a natural transom stern
            var aftStationOffsets = scaledOffsets[0];
            var adjacentStationOffsets = scaledOffsets[1]; // Station 1 (next forward)

            if (aftStationOffsets.Count > 0 && adjacentStationOffsets.Count > 0)
            {
                // Check if stern is significantly wider than adjacent station at any waterline
                // This is a better indicator of scaling artifacts than absolute threshold
                bool sternNeedsFix = false;
                for (int wlIdx = 0; wlIdx < Math.Min(aftStationOffsets.Count, adjacentStationOffsets.Count); wlIdx++)
                {
                    // If stern is more than 20% wider than adjacent station, it's likely a scaling artifact
                    if (aftStationOffsets[wlIdx] > adjacentStationOffsets[wlIdx] * 1.2m)
                    {
                        sternNeedsFix = true;
                        break;
                    }
                }

                // Also check absolute threshold as fallback (more than 40% of beam is definitely wrong)
                var maxSternHalfBreadth = aftStationOffsets.Max();
                if (maxSternHalfBreadth > beamTarget * 0.4m)
                {
                    sternNeedsFix = true;
                }

                // Only apply fix if stern is wider than it should be
                if (sternNeedsFix)
                {
                    // Cap stern to be no wider than adjacent station + 10% (slight taper is OK)
                    var maxAllowedSternHalfBreadth = adjacentStationOffsets.Max() * 1.1m;
                    // But also cap at absolute maximum (40% of beam)
                    maxAllowedSternHalfBreadth = Math.Min(maxAllowedSternHalfBreadth, beamTarget * 0.4m);

                    // Ensure keel (waterline 0) has zero or very small half-breadth
                    var keelHalfBreadth = aftStationOffsets[0];
                    var maxKeelHalfBreadth = beamTarget * 0.05m; // Max 5% of beam at keel

                    if (keelHalfBreadth > maxKeelHalfBreadth)
                    {
                        aftStationOffsets[0] = Math.Min(keelHalfBreadth, maxKeelHalfBreadth);
                    }

                    // Ensure the stern tapers properly from keel to deck
                    for (int wlIdx = 1; wlIdx < aftStationOffsets.Count && wlIdx < scaledWaterlines.Count; wlIdx++)
                    {
                        var currentHalfBreadth = aftStationOffsets[wlIdx];
                        var prevHalfBreadth = aftStationOffsets[wlIdx - 1];
                        var adjacentHalfBreadth = wlIdx < adjacentStationOffsets.Count
                            ? adjacentStationOffsets[wlIdx]
                            : adjacentStationOffsets[adjacentStationOffsets.Count - 1];
                        var waterlineZ = scaledWaterlines[wlIdx];
                        var waterlineNorm = waterlineZ / maxDraft;

                        // Cap stern to be no wider than adjacent station + small margin
                        var maxAllowedForThisWaterline = Math.Min(
                            adjacentHalfBreadth * 1.1m, // 10% wider than adjacent
                            maxAllowedSternHalfBreadth * (0.3m + 0.7m * waterlineNorm) // Allow some increase with height
                        );

                        if (currentHalfBreadth > maxAllowedForThisWaterline)
                        {
                            aftStationOffsets[wlIdx] = Math.Min(currentHalfBreadth, maxAllowedForThisWaterline);
                        }

                        // Ensure smooth tapering
                        if (currentHalfBreadth < prevHalfBreadth * 0.8m)
                        {
                            aftStationOffsets[wlIdx] = Math.Max(currentHalfBreadth, prevHalfBreadth * 0.85m);
                        }
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
