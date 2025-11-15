using Shared.HullGenerators.Fairing;

namespace Shared.HullGenerators.ParentHull;

/// <summary>
/// Adjusts LCB by "swinging" sections fore/aft
/// Uses cubic spline interpolation to reinterpolate onto original station grid
/// Preserves volume during LCB adjustment
/// </summary>
public static class LCBSectionSwing
{
    /// <summary>
    /// Adjust LCB by swinging sections
    /// </summary>
    /// <param name="scaledHull">Scaled hull data</param>
    /// <param name="targetLcbPercent">Target LCB (% Lbp forward of midship)</param>
    /// <param name="currentLcbPercent">Current LCB (% Lbp forward of midship)</param>
    /// <param name="length">Vessel length (m)</param>
    /// <returns>Adjusted offsets with target LCB</returns>
    public static List<List<decimal>> AdjustLCB(
        ScaledHullData scaledHull,
        decimal targetLcbPercent,
        decimal currentLcbPercent,
        decimal length)
    {
        decimal lcbDiff = targetLcbPercent - currentLcbPercent;

        // If already close enough, return original
        if (Math.Abs(lcbDiff) < 0.1m)
        {
            return scaledHull.Offsets;
        }

        // Convert LCB% to target position
        decimal targetLCB = length * (0.5m + targetLcbPercent / 100m);
        decimal midshipPosition = length / 2.0m;

        // Initialize result with same structure: [station][waterline]
        var result = new List<List<decimal>>();
        for (int stIdx = 0; stIdx < scaledHull.Stations.Count; stIdx++)
        {
            result.Add(new List<decimal>());
        }

        // Calculate shift for each station (more shift at ends, less at midship)
        var shifts = new List<decimal>();
        int midIdx = scaledHull.Stations.Count / 2;

        for (int i = 0; i < scaledHull.Stations.Count; i++)
        {
            decimal factor;
            if (i < midIdx)
            {
                // Forward section
                factor = (decimal)(midIdx - i) / midIdx;
            }
            else
            {
                // Aft section
                factor = (decimal)(i - midIdx) / midIdx;
            }

            // Scale shift by LCB difference
            decimal shift = lcbDiff * factor * 0.1m * (length / 100m);
            shifts.Add(shift);
        }

        // For each waterline, adjust by shifting stations
        for (int wlIdx = 0; wlIdx < scaledHull.Waterlines.Count; wlIdx++)
        {
            var halfBreadths = scaledHull.Offsets.Select(stationOffsets => stationOffsets[wlIdx]).ToList();

            // Apply shifts to station positions
            var shiftedStations = scaledHull.Stations
                .Select((s, i) => s + shifts[i])
                .ToList();

            // Reinterpolate onto original station grid using cubic spline
            // Estimate end slopes from data
            decimal dx0 = halfBreadths.Count > 1
                ? (halfBreadths[1] - halfBreadths[0]) / (shiftedStations[1] - shiftedStations[0])
                : 0m;

            decimal dxn = halfBreadths.Count > 1
                ? (halfBreadths[halfBreadths.Count - 1] - halfBreadths[halfBreadths.Count - 2]) /
                  (shiftedStations[shiftedStations.Count - 1] - shiftedStations[shiftedStations.Count - 2])
                : 0m;

            // Fit spline through shifted stations
            var coefficients = CubicSplineFairing.Fit(shiftedStations, halfBreadths, dx0, dxn);

            // Resample at original stations and store in result
            for (int stIdx = 0; stIdx < scaledHull.Stations.Count; stIdx++)
            {
                decimal adjustedHb = CubicSplineFairing.Evaluate(coefficients, scaledHull.Stations[stIdx]);
                result[stIdx].Add(Math.Max(0m, adjustedHb)); // Ensure non-negative
            }
        }

        return result;
    }
}
