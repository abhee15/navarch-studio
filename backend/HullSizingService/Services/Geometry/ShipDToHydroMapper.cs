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

        var stations = BuildStations(sections, lpp);
        var waterlines = BuildWaterlines(sections);
        var offsets = BuildOffsets(sections, stations.Count, waterlines);

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
        if (station.Offsets.TryGetValue(height, out var value))
        {
            return value;
        }

        if (station.BulbOffsets != null && station.BulbOffsets.TryGetValue(height, out var bulbValue))
        {
            return bulbValue;
        }

        // heights may have rounding differences - attempt fuzzy lookup across both dictionaries
        var candidates = station.Offsets.AsEnumerable();
        if (station.BulbOffsets != null)
        {
            candidates = candidates.Concat(station.BulbOffsets);
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
}
