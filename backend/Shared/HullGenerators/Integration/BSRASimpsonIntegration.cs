using Shared.Constants;

namespace Shared.HullGenerators.Integration;

/// <summary>
/// BSRA Simpson's rule integration with standard 23-station multipliers
/// Uses BSRA standard station layout and multipliers for accurate volume/LCB calculation
/// </summary>
public static class BSRASimpsonIntegration
{
    /// <summary>
    /// Calculate volume using BSRA Simpson's rule
    /// V = (h/3) * Σ(areas[i] * multipliers[i])
    /// where h = Lbp / 10 (common interval)
    /// </summary>
    public static decimal CalculateVolume(
        List<decimal> stations,
        List<decimal> sectionalAreas,
        decimal length)
    {
        if (stations.Count != sectionalAreas.Count)
            throw new ArgumentException("Stations and sectional areas must have same length");

        if (stations.Count != 23)
            throw new ArgumentException("BSRA integration requires exactly 23 stations");

        // Common interval: h = Lbp / 10
        decimal h = length / 10.0m;

        // Get multipliers for each station
        decimal sum = 0;
        for (int i = 0; i < stations.Count; i++)
        {
            var multiplier = GetMultiplierForStation(stations[i]);
            sum += sectionalAreas[i] * multiplier;
        }

        return (h / 3.0m) * sum;
    }

    /// <summary>
    /// Calculate LCB (longitudinal center of buoyancy) from midship
    /// LCB = Σ(moments) / Σ(volumes)
    /// where moment = area * multiplier * lever_from_midship
    /// </summary>
    public static decimal CalculateLCB(
        List<decimal> stations,
        List<decimal> sectionalAreas,
        decimal length)
    {
        if (stations.Count != sectionalAreas.Count)
            throw new ArgumentException("Stations and sectional areas must have same length");

        if (stations.Count != 23)
            throw new ArgumentException("BSRA integration requires exactly 23 stations");

        decimal h = length / 10.0m;
        decimal midshipStation = length / 2.0m;

        decimal sumMoments = 0;
        decimal sumVolumes = 0;

        for (int i = 0; i < stations.Count; i++)
        {
            var multiplier = GetMultiplierForStation(stations[i]);
            decimal lever = stations[i] - midshipStation; // Lever from midship
            decimal volumeElement = sectionalAreas[i] * multiplier;

            sumMoments += volumeElement * lever;
            sumVolumes += volumeElement;
        }

        if (sumVolumes == 0)
            return 0;

        return (h / 3.0m) * sumMoments / ((h / 3.0m) * sumVolumes);
    }

    /// <summary>
    /// Calculate vertical moment for KB (vertical center of buoyancy) calculation
    /// Vertical moment = Σ(area * multiplier * z_centroid)
    /// </summary>
    public static decimal CalculateVerticalMoment(
        List<decimal> stations,
        List<decimal> sectionalAreas,
        List<decimal> sectionCentroids, // Z-centroids of each section
        decimal length)
    {
        if (stations.Count != sectionalAreas.Count || stations.Count != sectionCentroids.Count)
            throw new ArgumentException("All lists must have same length");

        if (stations.Count != 23)
            throw new ArgumentException("BSRA integration requires exactly 23 stations");

        decimal h = length / 10.0m;

        decimal sum = 0;
        for (int i = 0; i < stations.Count; i++)
        {
            var multiplier = GetMultiplierForStation(stations[i]);
            sum += sectionalAreas[i] * multiplier * sectionCentroids[i];
        }

        return (h / 3.0m) * sum;
    }

    /// <summary>
    /// Calculate waterplane area using BSRA Simpson's rule
    /// Aw = (2 * h / 3) * Σ(half_breadths[i] * multipliers[i])
    /// Factor of 2 accounts for port and starboard
    /// </summary>
    public static decimal CalculateWaterplaneArea(
        List<decimal> stations,
        List<decimal> halfBreadths,
        decimal length)
    {
        if (stations.Count != halfBreadths.Count)
            throw new ArgumentException("Stations and half-breadths must have same length");

        if (stations.Count != 23)
            throw new ArgumentException("BSRA integration requires exactly 23 stations");

        decimal h = length / 10.0m;

        decimal sum = 0;
        for (int i = 0; i < stations.Count; i++)
        {
            var multiplier = GetMultiplierForStation(stations[i]);
            sum += halfBreadths[i] * multiplier;
        }

        return (2 * h / 3.0m) * sum; // Factor of 2 for port + starboard
    }

    /// <summary>
    /// Get Simpson multiplier for a given station position
    /// </summary>
    private static decimal GetMultiplierForStation(decimal station)
    {
        // Normalize station to 0-10 range (BSRA standard)
        // Find closest match in constants
        var multiplier = BSRAConstants.SimpsonMultipliers
            .OrderBy(m => Math.Abs(m.Station - station))
            .FirstOrDefault();

        return multiplier?.Multiplier ?? 1.0m;
    }

    /// <summary>
    /// Get multiplier for station by index (0-22 for 23 stations)
    /// </summary>
    public static decimal GetMultiplierByIndex(int index)
    {
        if (index < 0 || index >= BSRAConstants.SimpsonMultipliers.Length)
            throw new ArgumentOutOfRangeException(nameof(index), "Index must be 0-22");

        return BSRAConstants.SimpsonMultipliers[index].Multiplier;
    }
}
