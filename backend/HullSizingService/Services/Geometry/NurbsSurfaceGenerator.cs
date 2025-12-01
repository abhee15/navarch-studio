using System;
using System.Collections.Generic;
using System.Linq;

namespace HullSizingService.Services.Geometry;

/// <summary>
/// NURBS Surface Generator for 2D hull surfaces S(u,v)
/// Extends the 1D NURBS curve generator to create smooth 2D surfaces
/// Used for inverse design optimization where Control Points directly define hull shape
/// </summary>
public class NurbsSurfaceGenerator
{
    /// <summary>
    /// Control Point Grid: 2D array of control points defining the hull surface
    /// Grid dimensions: [numStations, numControlPointsPerStation]
    /// Each control point is (x, y, z) where:
    ///   x = longitudinal position (station)
    ///   y = half-breadth (transverse)
    ///   z = height (vertical)
    /// </summary>
    public class ControlPointGrid
    {
        public List<List<(decimal x, decimal y, decimal z)>> Points { get; set; }
        public int NumStations => Points?.Count ?? 0;
        public int NumControlPointsPerStation => Points?.FirstOrDefault()?.Count ?? 0;

        public ControlPointGrid()
        {
            Points = new List<List<(decimal x, decimal y, decimal z)>>();
        }

        public ControlPointGrid(int numStations, int numControlPointsPerStation)
        {
            Points = new List<List<(decimal x, decimal y, decimal z)>>();
            for (int i = 0; i < numStations; i++)
            {
                Points.Add(new List<(decimal x, decimal y, decimal z)>(numControlPointsPerStation));
            }
        }
    }

    /// <summary>
    /// Evaluates NURBS surface S(u,v) at parameter values (u, v)
    /// u: longitudinal parameter (0 = aft, 1 = forward)
    /// v: vertical parameter (0 = keel, 1 = deck)
    /// </summary>
    /// <param name="controlPointGrid">2D grid of control points</param>
    /// <param name="u">Longitudinal parameter [0, 1]</param>
    /// <param name="v">Vertical parameter [0, 1]</param>
    /// <param name="degreeU">Degree in longitudinal direction (typically 3 for cubic)</param>
    /// <param name="degreeV">Degree in vertical direction (typically 3 for cubic)</param>
    /// <param name="knotsU">Knot vector for u direction (if null, generates clamped uniform)</param>
    /// <param name="knotsV">Knot vector for v direction (if null, generates clamped uniform)</param>
    /// <returns>Evaluated point (x, y, z) on the surface</returns>
    public static (decimal x, decimal y, decimal z) EvaluateSurface(
        ControlPointGrid controlPointGrid,
        decimal u,
        decimal v,
        int degreeU = 3,
        int degreeV = 3,
        List<decimal>? knotsU = null,
        List<decimal>? knotsV = null)
    {
        if (controlPointGrid.NumStations < degreeU + 1 || controlPointGrid.NumControlPointsPerStation < degreeV + 1)
        {
            throw new ArgumentException(
                $"Need at least {degreeU + 1} stations and {degreeV + 1} control points per station");
        }

        // Generate knot vectors if not provided
        if (knotsU == null)
        {
            knotsU = GenerateClampedUniformKnots(controlPointGrid.NumStations - 1, degreeU);
        }

        if (knotsV == null)
        {
            knotsV = GenerateClampedUniformKnots(controlPointGrid.NumControlPointsPerStation - 1, degreeV);
        }

        // Clamp parameters to valid range
        u = Math.Clamp(u, knotsU[degreeU], knotsU[knotsU.Count - degreeU - 1]);
        v = Math.Clamp(v, knotsV[degreeV], knotsV[knotsV.Count - degreeV - 1]);

        // Find knot spans
        int spanU = FindKnotSpan(controlPointGrid.NumStations - 1, degreeU, u, knotsU);
        int spanV = FindKnotSpan(controlPointGrid.NumControlPointsPerStation - 1, degreeV, v, knotsV);

        // Evaluate basis functions
        var basisU = EvaluateBasisFunctions(spanU, u, degreeU, knotsU);
        var basisV = EvaluateBasisFunctions(spanV, v, degreeV, knotsV);

        // Compute surface point using tensor product
        decimal x = 0m, y = 0m, z = 0m;

        for (int i = 0; i <= degreeU; i++)
        {
            int idxU = spanU - degreeU + i;
            decimal basisUVal = basisU[i];

            for (int j = 0; j <= degreeV; j++)
            {
                int idxV = spanV - degreeV + j;
                decimal basisVVal = basisV[j];
                decimal basisProduct = basisUVal * basisVVal;

                var cp = controlPointGrid.Points[idxU][idxV];
                x += basisProduct * cp.x;
                y += basisProduct * cp.y;
                z += basisProduct * cp.z;
            }
        }

        return (x, y, z);
    }

    /// <summary>
    /// Generates hull offsets from NURBS surface by evaluating at discrete stations and waterlines
    /// This converts the continuous NURBS surface to the discrete offset format used by HydroCalculator
    /// </summary>
    /// <param name="controlPointGrid">Control point grid defining the surface</param>
    /// <param name="stations">List of station positions (longitudinal, 0-1)</param>
    /// <param name="waterlines">List of waterline heights (vertical, 0-1)</param>
    /// <param name="lppM">Length between perpendiculars (m) for scaling</param>
    /// <param name="beamM">Maximum beam (m) for scaling</param>
    /// <param name="draftM">Design draft (m) for scaling</param>
    /// <returns>Dictionary: (stationIndex, waterlineIndex) -> halfBreadth</returns>
    public static Dictionary<(int stationIdx, int waterlineIdx), decimal> GenerateOffsetsFromSurface(
        ControlPointGrid controlPointGrid,
        List<decimal> stations,
        List<decimal> waterlines,
        decimal lppM,
        decimal beamM,
        decimal draftM)
    {
        var offsets = new Dictionary<(int stationIdx, int waterlineIdx), decimal>();

        for (int i = 0; i < stations.Count; i++)
        {
            decimal u = stations[i]; // Longitudinal parameter [0, 1]

            for (int j = 0; j < waterlines.Count; j++)
            {
                decimal v = waterlines[j]; // Vertical parameter [0, 1]

                // Evaluate surface at (u, v)
                var point = EvaluateSurface(controlPointGrid, u, v);

                // Extract half-breadth (y coordinate)
                // Scale from normalized [0, 1] to physical units
                decimal halfBreadth = point.y * beamM / 2m;

                offsets[(i, j)] = Math.Max(0m, halfBreadth);
            }
        }

        return offsets;
    }

    /// <summary>
    /// Generates clamped uniform knot vector for NURBS
    /// Clamped means first (degree+1) knots = 0, last (degree+1) knots = 1
    /// This ensures the curve passes through the first and last control points
    /// </summary>
    private static List<decimal> GenerateClampedUniformKnots(int n, int degree)
    {
        var knots = new List<decimal>(n + degree + 2);
        int m = n + degree + 1;

        // Clamp start: first (degree+1) knots = 0
        for (int i = 0; i <= degree; i++)
        {
            knots.Add(0m);
        }

        // Uniform interior knots
        if (n > degree)
        {
            for (int i = 1; i <= n - degree; i++)
            {
                knots.Add((decimal)i / (n - degree + 1));
            }
        }

        // Clamp end: last (degree+1) knots = 1
        for (int i = 0; i <= degree; i++)
        {
            knots.Add(1m);
        }

        return knots;
    }

    /// <summary>
    /// Finds the knot span containing parameter u using binary search
    /// </summary>
    private static int FindKnotSpan(int n, int degree, decimal u, List<decimal> knots)
    {
        // Special case: u is at the last knot
        if (u >= knots[n + 1])
        {
            return n;
        }

        // Binary search
        int low = degree;
        int high = n + 1;
        int mid = (low + high) / 2;

        while (u < knots[mid] || u >= knots[mid + 1])
        {
            if (u < knots[mid])
            {
                high = mid;
            }
            else
            {
                low = mid;
            }
            mid = (low + high) / 2;
        }

        return mid;
    }

    /// <summary>
    /// Evaluates B-spline basis functions using Cox-de Boor recursion
    /// Returns the (degree+1) non-zero basis functions at parameter u
    /// </summary>
    private static List<decimal> EvaluateBasisFunctions(
        int span,
        decimal u,
        int degree,
        List<decimal> knots)
    {
        var basisFunctions = new List<decimal>(degree + 1);
        var left = new List<decimal>(degree + 1);
        var right = new List<decimal>(degree + 1);

        basisFunctions.Add(1m);

        for (int j = 1; j <= degree; j++)
        {
            left.Add(u - knots[span + 1 - j]);
            right.Add(knots[span + j] - u);

            decimal saved = 0m;

            for (int r = 0; r < j; r++)
            {
                decimal temp = basisFunctions[r] / (right[r] + left[j - r]);
                basisFunctions[r] = saved + right[r] * temp;
                saved = left[j - r] * temp;
            }

            basisFunctions.Add(saved);
        }

        return basisFunctions;
    }
}
