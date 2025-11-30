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
        // Only apply if the stern has unreasonably large half-breadths (likely a scaling artifact)
        // Many vessel types have wider sterns (transoms), so be conservative with this fix
        if (scaledOffsets.Count > 0 && scaledWaterlines.Count > 0)
        {
            var maxDraft = scaledWaterlines[scaledWaterlines.Count - 1];
            if (maxDraft <= 0) maxDraft = 1m;

            // Fix the first station (aft perpendicular) only if it has unreasonably large offsets
            var aftStationOffsets = scaledOffsets[0];
            if (aftStationOffsets.Count > 0)
            {
                // Check if stern has unreasonably large half-breadths (more than 50% of beam)
                // This indicates a scaling artifact that needs correction
                var maxSternHalfBreadth = aftStationOffsets.Max();
                var reasonableSternMax = beamTarget * 0.5m; // Stern can be up to 50% of beam (transom)

                // Only apply fix if stern is unreasonably wide
                if (maxSternHalfBreadth > reasonableSternMax)
                {
                    // Cap at reasonable maximum (50% of beam for transom stern)
                    var maxAllowedSternHalfBreadth = reasonableSternMax;

                    // Ensure keel (waterline 0) has zero or very small half-breadth
                    var keelHalfBreadth = aftStationOffsets[0];
                    var maxKeelHalfBreadth = beamTarget * 0.05m; // Max 5% of beam at keel (more lenient than bow)

                    if (keelHalfBreadth > maxKeelHalfBreadth)
                    {
                        aftStationOffsets[0] = Math.Min(keelHalfBreadth, maxKeelHalfBreadth);
                    }

                    // Ensure the stern tapers properly from keel to deck, but allow wider transom
                    for (int wlIdx = 1; wlIdx < aftStationOffsets.Count && wlIdx < scaledWaterlines.Count; wlIdx++)
                    {
                        var currentHalfBreadth = aftStationOffsets[wlIdx];
                        var prevHalfBreadth = aftStationOffsets[wlIdx - 1];
                        var waterlineZ = scaledWaterlines[wlIdx];
                        var waterlineNorm = waterlineZ / maxDraft;

                        // Cap only if unreasonably large, allow wider transom at deck
                        if (currentHalfBreadth > maxAllowedSternHalfBreadth)
                        {
                            // Allow transom to be wider at deck (up to 50% of beam)
                            var maxAllowed = maxAllowedSternHalfBreadth * (0.4m + 0.6m * waterlineNorm);
                            aftStationOffsets[wlIdx] = Math.Min(currentHalfBreadth, maxAllowed);
                        }

                        // Ensure smooth tapering (less aggressive than bow)
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
