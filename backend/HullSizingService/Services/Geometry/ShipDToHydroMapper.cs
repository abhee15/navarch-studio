using System;
using System.Collections.Generic;
using System.Linq;
using Shared.DTOs;

namespace HullSizingService.Services.Geometry;

public class ShipDToHydroMapper : IShipDToHydroMapper
{
    public (List<StationDto> stations, List<WaterlineDto> waterlines, List<OffsetDto> offsets) ConvertSections(HullSectionsDto sections, decimal lpp)
    {
        if (sections == null)
        {
            throw new ArgumentNullException(nameof(sections));
        }

        if (lpp <= 0)
        {
            throw new ArgumentException($"Lpp must be positive, got {lpp}", nameof(lpp));
        }

        if (sections.Stations == null || sections.Stations.Count == 0)
        {
            throw new ArgumentException("Sections must contain at least one station", nameof(sections));
        }

        var stations = BuildStations(sections, lpp);
        var waterlines = BuildWaterlines(sections);
        var offsets = BuildOffsets(sections, stations.Count, waterlines);

        // Validate minimum requirements
        if (stations.Count < 3)
        {
            throw new InvalidOperationException($"Geometry conversion produced only {stations.Count} stations, minimum 3 required.");
        }

        if (waterlines.Count < 2)
        {
            throw new InvalidOperationException($"Geometry conversion produced only {waterlines.Count} waterlines, minimum 2 required.");
        }

        if (offsets.Count == 0)
        {
            throw new InvalidOperationException("Geometry conversion produced no offsets.");
        }

        return (stations, waterlines, offsets);
    }

    private static List<StationDto> BuildStations(HullSectionsDto sections, decimal lpp)
    {
        var result = new List<StationDto>();
        for (var i = 0; i < sections.Stations.Count; i++)
        {
            var station = sections.Stations[i];
            var normalized = sections.StationPositions?.ElementAtOrDefault(i) ?? station.Position;
            var x = Math.Round(normalized * lpp, 6, MidpointRounding.AwayFromZero);

            result.Add(new StationDto
            {
                StationIndex = i,
                X = x
            });
        }

        return result;
    }

    private static List<WaterlineDto> BuildWaterlines(HullSectionsDto sections)
    {
        var heights = new SortedSet<decimal>();
        foreach (var station in sections.Stations)
        {
            foreach (var height in station.Offsets.Keys)
            {
                heights.Add(Math.Round(height, 6, MidpointRounding.AwayFromZero));
            }

            if (station.BulbOffsets != null)
            {
                foreach (var height in station.BulbOffsets.Keys)
                {
                    heights.Add(Math.Round(height, 6, MidpointRounding.AwayFromZero));
                }
            }

            // Include skeg heights (may be negative - skeg extends below keel)
            if (station.SkegOffsets != null)
            {
                foreach (var height in station.SkegOffsets.Keys)
                {
                    heights.Add(Math.Round(height, 6, MidpointRounding.AwayFromZero));
                }
            }
        }

        return heights.Select((z, index) => new WaterlineDto
        {
            WaterlineIndex = index,
            Z = z
        }).ToList();
    }

    private static List<OffsetDto> BuildOffsets(HullSectionsDto sections, int stationCount, List<WaterlineDto> waterlines)
    {
        var offsets = new List<OffsetDto>(stationCount * Math.Max(1, waterlines.Count));

        for (var stationIndex = 0; stationIndex < stationCount; stationIndex++)
        {
            var station = sections.Stations[stationIndex];
            for (var waterlineIndex = 0; waterlineIndex < waterlines.Count; waterlineIndex++)
            {
                var waterline = waterlines[waterlineIndex];
                var halfBreadth = LookupHalfBreadth(station, waterline.Z);

                offsets.Add(new OffsetDto
                {
                    StationIndex = stationIndex,
                    WaterlineIndex = waterlineIndex,
                    HalfBreadthY = halfBreadth
                });
            }
        }

        return offsets;
    }

    private static decimal LookupHalfBreadth(HullStationDto station, decimal height)
    {
        // Check main offsets first (exact match)
        if (station.Offsets.TryGetValue(height, out var value))
        {
            return value;
        }

        // Check skeg offsets (skeg may extend below keel with negative heights)
        // Use maximum of main offset and skeg offset if both exist at same height
        if (station.SkegOffsets != null && station.SkegOffsets.TryGetValue(height, out var skegValue))
        {
            // If main offset exists, use maximum; otherwise use skeg value
            if (station.Offsets.TryGetValue(height, out var mainValue))
            {
                return Math.Max(mainValue, skegValue);
            }
            return skegValue;
        }

        // Check bulb offsets (exact match)
        if (station.BulbOffsets != null && station.BulbOffsets.TryGetValue(height, out var bulbValue))
        {
            return bulbValue;
        }

        // CRITICAL FIX: Use linear interpolation instead of fuzzy lookup
        // This ensures accurate offset values at waterlines, especially critical for bow/stern taper
        // Try interpolation in main offsets first
        var mainInterpolated = InterpolateHalfBreadth(station.Offsets, height);
        if (mainInterpolated.HasValue)
        {
            return mainInterpolated.Value;
        }

        // Try interpolation in bulb offsets
        if (station.BulbOffsets != null)
        {
            var bulbInterpolated = InterpolateHalfBreadth(station.BulbOffsets, height);
            if (bulbInterpolated.HasValue)
            {
                return bulbInterpolated.Value;
            }
        }

        // Try interpolation in skeg offsets
        if (station.SkegOffsets != null)
        {
            var skegInterpolated = InterpolateHalfBreadth(station.SkegOffsets, height);
            if (skegInterpolated.HasValue)
            {
                return skegInterpolated.Value;
            }
        }

        // Fallback: Heights may have rounding differences - attempt fuzzy lookup across all dictionaries
        var candidates = station.Offsets.AsEnumerable();
        if (station.BulbOffsets != null)
        {
            candidates = candidates.Concat(station.BulbOffsets);
        }
        if (station.SkegOffsets != null)
        {
            candidates = candidates.Concat(station.SkegOffsets);
        }

        var match = candidates
            .OrderBy(kvp => Math.Abs(kvp.Key - height))
            .FirstOrDefault();

        if (!match.Equals(default(KeyValuePair<decimal, decimal>)))
        {
            return match.Value;
        }

        return 0m;
    }

    /// <summary>
    /// Interpolates half-breadth at a given height using linear interpolation between adjacent heights
    /// </summary>
    private static decimal? InterpolateHalfBreadth(Dictionary<decimal, decimal> offsets, decimal height)
    {
        if (offsets == null || offsets.Count == 0)
        {
            return null;
        }

        // Get sorted heights
        var sortedHeights = offsets.Keys.OrderBy(h => h).ToList();

        if (sortedHeights.Count < 2)
        {
            // Not enough points for interpolation
            return null;
        }

        // Check if height is below minimum or above maximum
        if (height <= sortedHeights[0])
        {
            // Below minimum - return first value (or 0 if negative)
            return height < 0 ? 0m : offsets[sortedHeights[0]];
        }

        if (height >= sortedHeights[sortedHeights.Count - 1])
        {
            // Above maximum - return last value
            return offsets[sortedHeights[sortedHeights.Count - 1]];
        }

        // Find surrounding heights for interpolation
        for (int i = 0; i < sortedHeights.Count - 1; i++)
        {
            var lowerHeight = sortedHeights[i];
            var upperHeight = sortedHeights[i + 1];

            if (lowerHeight <= height && height <= upperHeight)
            {
                var lowerValue = offsets[lowerHeight];
                var upperValue = offsets[upperHeight];

                // Linear interpolation: value = lower + t * (upper - lower)
                // where t = (height - lowerHeight) / (upperHeight - lowerHeight)
                var heightRange = upperHeight - lowerHeight;
                if (heightRange > 0.000001m) // Avoid division by zero
                {
                    var t = (height - lowerHeight) / heightRange;
                    return lowerValue + t * (upperValue - lowerValue);
                }
                else
                {
                    // Heights are too close - return average
                    return (lowerValue + upperValue) / 2m;
                }
            }
        }

        // Should not reach here, but return null if no match found
        return null;
    }
}
