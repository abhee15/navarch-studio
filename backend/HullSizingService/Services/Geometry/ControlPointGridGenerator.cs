using System;
using System.Collections.Generic;
using System.Linq;

namespace HullSizingService.Services.Geometry;

/// <summary>
/// Generates initial Control Point Grid guesses for optimization
/// Creates reasonable starting points based on target form coefficients
/// </summary>
public class ControlPointGridGenerator
{
    /// <summary>
    /// Generates initial Control Point Grid from target form coefficients
    /// Uses parametric formulas to create a reasonable starting guess
    /// </summary>
    public static NurbsSurfaceGenerator.ControlPointGrid GenerateInitialGuess(
        decimal lppM,
        decimal beamM,
        decimal draftM,
        decimal targetCb,
        decimal targetCp,
        decimal targetLcbPercent,
        int numStations = 11,
        int numControlPointsPerStation = 8)
    {
        var grid = new NurbsSurfaceGenerator.ControlPointGrid(numStations, numControlPointsPerStation);

        // Generate station positions (longitudinal, normalized 0-1)
        var stationPositions = new List<decimal>();
        for (int i = 0; i < numStations; i++)
        {
            stationPositions.Add((decimal)i / (numStations - 1));
        }

        // Generate control point heights (vertical, normalized 0-1)
        var controlPointHeights = new List<decimal>();
        for (int j = 0; j < numControlPointsPerStation; j++)
        {
            controlPointHeights.Add((decimal)j / (numControlPointsPerStation - 1));
        }

        // Generate control points for each station
        for (int stationIdx = 0; stationIdx < numStations; stationIdx++)
        {
            decimal u = stationPositions[stationIdx]; // Longitudinal position [0, 1]

            // Determine region: stern, midship, or bow
            string region;
            if (u < 0.3m)
            {
                region = "stern";
            }
            else if (u < 0.7m)
            {
                region = "midship";
            }
            else
            {
                region = "bow";
            }

            for (int cpIdx = 0; cpIdx < numControlPointsPerStation; cpIdx++)
            {
                decimal v = controlPointHeights[cpIdx]; // Vertical position [0, 1]

                // Calculate half-breadth based on region and form coefficients
                decimal halfBreadth = CalculateHalfBreadth(
                    u, v, region, targetCb, targetCp, targetLcbPercent, beamM, draftM);

                // Normalize half-breadth to [0, 1] range (for NURBS surface)
                decimal normalizedY = halfBreadth / (beamM / 2m);
                normalizedY = Math.Clamp(normalizedY, 0m, 1m);

                // Set control point coordinates
                // x = longitudinal (normalized 0-1)
                // y = half-breadth (normalized 0-1)
                // z = height (normalized 0-1)
                grid.Points[stationIdx].Add((u, normalizedY, v));
            }
        }

        return grid;
    }

    /// <summary>
    /// Calculates half-breadth at a given position using parametric formulas
    /// This provides a reasonable initial guess for the optimization
    /// </summary>
    private static decimal CalculateHalfBreadth(
        decimal u, // Longitudinal position [0, 1]
        decimal v, // Vertical position [0, 1]
        string region,
        decimal targetCb,
        decimal targetCp,
        decimal targetLcbPercent,
        decimal beamM,
        decimal draftM)
    {
        // Base half-breadth at waterline (v = 1)
        decimal baseHalfBreadth = beamM / 2m;

        // Apply longitudinal taper (bow/stern narrowing)
        decimal longitudinalScale = 1.0m;
        if (region == "bow")
        {
            // Bow taper: narrow toward tip
            decimal bowPos = (u - 0.7m) / 0.3m; // Position within bow region [0, 1]
            longitudinalScale = (decimal)Math.Pow((double)(1.0m - bowPos), 2.0);
        }
        else if (region == "stern")
        {
            // Stern taper: narrow toward tip
            decimal sternPos = u / 0.3m; // Position within stern region [0, 1]
            longitudinalScale = (decimal)Math.Pow((double)sternPos, 2.0);
        }

        // Apply vertical profile (narrow at keel, wide at waterline)
        decimal verticalScale = (decimal)Math.Pow((double)v, 0.8); // Gentle expansion

        // Apply form coefficient adjustments
        // Higher CB = fuller sections
        decimal cbAdjustment = 0.8m + targetCb * 0.4m; // Range: 0.8-1.2
        verticalScale *= cbAdjustment;

        // Higher CP = fuller ends
        decimal cpAdjustment = 1.0m;
        if (region != "midship")
        {
            cpAdjustment = 0.9m + targetCp * 0.2m; // Range: 0.9-1.1
        }
        longitudinalScale *= cpAdjustment;

        // Apply LCB shift (asymmetry)
        // Positive LCB% = forward shift (fuller bow, finer stern)
        decimal lcbAdjustment = 1.0m;
        if (targetLcbPercent > 0)
        {
            // Forward LCB: fuller bow, finer stern
            if (region == "bow")
            {
                lcbAdjustment = 1.0m + targetLcbPercent / 100m * 0.1m;
            }
            else if (region == "stern")
            {
                lcbAdjustment = 1.0m - targetLcbPercent / 100m * 0.1m;
            }
        }
        else if (targetLcbPercent < 0)
        {
            // Aft LCB: finer bow, fuller stern
            if (region == "bow")
            {
                lcbAdjustment = 1.0m + targetLcbPercent / 100m * 0.1m;
            }
            else if (region == "stern")
            {
                lcbAdjustment = 1.0m - targetLcbPercent / 100m * 0.1m;
            }
        }
        longitudinalScale *= lcbAdjustment;

        // Calculate final half-breadth
        decimal halfBreadth = baseHalfBreadth * longitudinalScale * verticalScale;

        // Ensure non-negative and reasonable bounds
        halfBreadth = Math.Max(0m, Math.Min(halfBreadth, beamM / 2m * 1.1m));

        return halfBreadth;
    }
}
