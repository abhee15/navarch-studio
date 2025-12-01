using System;
using System.Collections.Generic;
using System.Linq;
using HullSizingService.Services.Integration;
using Microsoft.Extensions.Logging;

namespace HullSizingService.Services.Geometry;

/// <summary>
/// Computes hydrostatic properties (CB, CP, LCB) from NURBS-generated offsets
/// Uses Gauss Quadrature for high-accuracy, fast integration in optimization loop
/// Falls back to discrete integration for compatibility
/// </summary>
public class NurbsHydrostaticsCalculator : IHydrostaticsCalculator
{
    private readonly IIntegrationEngine _integrationEngine;
    private readonly ILogger<NurbsHydrostaticsCalculator> _logger;

    public NurbsHydrostaticsCalculator(
        IIntegrationEngine integrationEngine,
        ILogger<NurbsHydrostaticsCalculator> logger)
    {
        _integrationEngine = integrationEngine;
        _logger = logger;
    }

    /// <summary>
    /// Computes hydrostatics directly from NURBS Control Point Grid using Gauss Quadrature
    /// This is the fast path for optimization - evaluates surface directly without generating offsets
    /// </summary>
    public (decimal Cb, decimal Cp, decimal LcbPercent) ComputeFromControlPointGrid(
        NurbsSurfaceGenerator.ControlPointGrid controlPoints,
        decimal lppM,
        decimal beamM,
        decimal draftM,
        bool useGaussQuadrature = true)
    {
        if (useGaussQuadrature)
        {
            return ComputeWithGaussQuadrature(controlPoints, lppM, beamM, draftM);
        }
        else
        {
            // Fallback: generate offsets and use discrete integration
            var stations = GenerateStationPositions(controlPoints.NumStations);
            var waterlines = GenerateWaterlinePositions(controlPoints.NumControlPointsPerStation);
            var offsets = NurbsSurfaceGenerator.GenerateOffsetsFromSurface(
                controlPoints, stations, waterlines, lppM, beamM, draftM);
            return ComputeFromOffsets(stations, waterlines, offsets, lppM, beamM, draftM);
        }
    }

    /// <summary>
    /// Computes hydrostatics using Gauss Quadrature for direct NURBS surface evaluation
    /// Much faster than discrete integration - only evaluates surface at Gauss points
    /// </summary>
    private (decimal Cb, decimal Cp, decimal LcbPercent) ComputeWithGaussQuadrature(
        NurbsSurfaceGenerator.ControlPointGrid controlPoints,
        decimal lppM,
        decimal beamM,
        decimal draftM)
    {
        // 1. Compute sectional area curve A_x(x) using Gauss Quadrature
        // For each station (u), integrate half-breadth y(v) from keel (v=0) to draft (v=1)
        Func<decimal, decimal> sectionalAreaFunc = (decimal u) =>
        {
            // Integrate half-breadth along vertical direction (v) at this station (u)
            return _integrationEngine.GaussQuadrature(
                v =>
                {
                    var point = NurbsSurfaceGenerator.EvaluateSurface(controlPoints, u, v);
                    // Extract half-breadth (y coordinate) and scale to physical units
                    return point.y * beamM / 2m;
                },
                a: 0m,  // keel
                b: 1m,  // draft (normalized)
                use7Point: true
            );
        };

        // 2. Compute volume by integrating sectional area along length
        // Volume = ∫ A_x(x) dx from 0 to Lpp
        decimal volume = _integrationEngine.CompositeGaussQuadrature(
            sectionalAreaFunc,
            a: 0m,  // aft
            b: 1m,  // forward (normalized)
            numSegments: 5,  // Divide into 5 segments for accuracy
            use7Point: true
        ) * 2m; // Mirror to port side

        // 3. Compute LCB moment: ∫ x * A_x(x) dx
        Func<decimal, decimal> lcbMomentFunc = (decimal u) =>
        {
            decimal x = u * lppM; // Physical position
            decimal area = sectionalAreaFunc(u);
            return x * area;
        };

        decimal lcbMoment = _integrationEngine.CompositeGaussQuadrature(
            lcbMomentFunc,
            a: 0m,
            b: 1m,
            numSegments: 5,
            use7Point: true
        ) * 2m; // Mirror to port side

        decimal lcb = volume > 0 ? lcbMoment / volume : lppM / 2m;

        // 4. Compute midship sectional area (for CP calculation)
        decimal midshipU = 0.5m; // Midship at u = 0.5
        decimal midshipArea = sectionalAreaFunc(midshipU) * 2m; // Full section

        // 5. Compute form coefficients
        decimal cb = (lppM * beamM * draftM) > 0
            ? volume / (lppM * beamM * draftM)
            : 0m;

        decimal cp = (midshipArea * lppM) > 0
            ? volume / (midshipArea * lppM)
            : 0m;

        // 6. Compute LCB as percentage from midship
        decimal lcbPercent = lppM > 0
            ? ((lcb / lppM) - 0.5m) * 100m
            : 0m;

        _logger.LogDebug(
            "[NURBS_HYDROSTATICS] Gauss Quadrature: CB={Cb}, CP={Cp}, LCB={Lcb}% (position={LcbPos}m)",
            cb, cp, lcbPercent, lcb);

        return (cb, cp, lcbPercent);
    }

    /// <summary>
    /// Computes hydrostatics from discrete offsets (compatibility method)
    /// </summary>
    public (decimal Cb, decimal Cp, decimal LcbPercent) ComputeFromOffsets(
        List<decimal> stations,
        List<decimal> waterlines,
        Dictionary<(int stationIdx, int waterlineIdx), decimal> offsets,
        decimal lppM,
        decimal beamM,
        decimal draftM)
    {
        // Convert normalized stations/waterlines to physical coordinates
        var stationXPositions = stations.Select(s => s * lppM).ToList();
        var waterlineZPositions = waterlines.Select(w => w * draftM).ToList();

        // 1. Compute sectional areas at each station
        var sectionAreas = new List<decimal>();
        var stationXPhysical = new List<decimal>();

        foreach (var station in stations)
        {
            var stationIdx = stations.IndexOf(station);
            stationXPhysical.Add(station * lppM);

            // Get half-breadths for this station at each waterline
            var halfBreadths = new List<decimal>();
            var waterlineZs = new List<decimal>();

            foreach (var waterline in waterlines)
            {
                var waterlineIdx = waterlines.IndexOf(waterline);
                var offset = offsets.GetValueOrDefault((stationIdx, waterlineIdx), 0m);

                waterlineZs.Add(waterline * draftM);
                halfBreadths.Add(offset);
            }

            // Integrate to get area of this section (half-section * 2)
            var halfSectionArea = _integrationEngine.Integrate(waterlineZs, halfBreadths);
            var fullSectionArea = 2 * halfSectionArea; // Mirror to port side
            sectionAreas.Add(fullSectionArea);
        }

        // 2. Compute volume by integrating sectional areas along length
        var volume = _integrationEngine.Integrate(stationXPhysical, sectionAreas);

        // 3. Compute LCB (longitudinal center of buoyancy)
        var lcbMoment = _integrationEngine.FirstMoment(stationXPhysical, sectionAreas);
        var lcb = volume > 0 ? lcbMoment / volume : lppM / 2m;

        // 4. Compute midship sectional area (for CP calculation)
        var midshipIndex = stations.Count / 2;
        var midshipArea = sectionAreas[midshipIndex];

        // 5. Compute form coefficients
        var cb = (lppM * beamM * draftM) > 0
            ? volume / (lppM * beamM * draftM)
            : 0m;

        var cp = (midshipArea * lppM) > 0
            ? volume / (midshipArea * lppM)
            : 0m;

        // 6. Compute LCB as percentage from midship
        var lcbPercent = lppM > 0
            ? ((lcb / lppM) - 0.5m) * 100m
            : 0m;

        _logger.LogDebug(
            "[NURBS_HYDROSTATICS] Discrete: CB={Cb}, CP={Cp}, LCB={Lcb}% (position={LcbPos}m)",
            cb, cp, lcbPercent, lcb);

        return (cb, cp, lcbPercent);
    }

    private List<decimal> GenerateStationPositions(int numStations)
    {
        var positions = new List<decimal>();
        for (int i = 0; i < numStations; i++)
        {
            positions.Add((decimal)i / (numStations - 1));
        }
        return positions;
    }

    private List<decimal> GenerateWaterlinePositions(int numWaterlines)
    {
        var positions = new List<decimal>();
        for (int i = 0; i < numWaterlines; i++)
        {
            positions.Add((decimal)i / (numWaterlines - 1));
        }
        return positions;
    }
}
