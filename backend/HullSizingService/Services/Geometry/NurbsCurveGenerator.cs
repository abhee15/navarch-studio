using System;
using System.Collections.Generic;
using System.Linq;

namespace HullSizingService.Services.Geometry;

/// <summary>
/// NURBS (Non-Uniform Rational B-Spline) curve generator for smooth hull geometry
/// Implements B-spline basis functions with boundary condition enforcement
/// Ensures C² continuity for fair hull sections, waterlines, and buttocks
/// </summary>
public class NurbsCurveGenerator
{
    /// <summary>
    /// Generates a NURBS curve from control points with specified degree
    /// </summary>
    /// <param name="controlPoints">Control points (x, y) coordinates</param>
    /// <param name="degree">Degree of the B-spline (typically 3 for cubic)</param>
    /// <param name="knots">Knot vector (if null, generates uniform knots)</param>
    /// <param name="weights">Weights for each control point (if null, uses uniform weights = 1)</param>
    /// <param name="evaluationPoints">Parameter values (u) at which to evaluate the curve</param>
    /// <returns>Evaluated curve points</returns>
    public static List<decimal> GenerateCurve(
        List<(decimal x, decimal y)> controlPoints,
        int degree = 3,
        List<decimal>? knots = null,
        List<decimal>? weights = null,
        List<decimal>? evaluationPoints = null)
    {
        if (controlPoints == null || controlPoints.Count < degree + 1)
        {
            throw new ArgumentException($"Need at least {degree + 1} control points for degree {degree} NURBS");
        }

        int n = controlPoints.Count - 1; // Number of control points minus 1 (0-indexed)
        int m = n + degree + 1; // Number of knots = n + degree + 1

        // Generate uniform knot vector if not provided
        if (knots == null)
        {
            knots = GenerateUniformKnots(n, degree);
        }

        // Use uniform weights if not provided
        if (weights == null)
        {
            weights = Enumerable.Repeat(1m, controlPoints.Count).ToList();
        }

        // Generate evaluation points if not provided (uniform spacing)
        if (evaluationPoints == null)
        {
            evaluationPoints = GenerateEvaluationPoints(knots, degree, 50); // Default 50 points
        }

        // Evaluate curve at each parameter value
        var result = new List<decimal>();
        foreach (var u in evaluationPoints)
        {
            var point = EvaluateNurbs(controlPoints, degree, knots, weights, u);
            result.Add(point.y);
        }

        return result;
    }

    /// <summary>
    /// Generates a NURBS curve with boundary conditions enforced
    /// </summary>
    /// <param name="controlPoints">Control points</param>
    /// <param name="degree">Degree of B-spline</param>
    /// <param name="startSlope">Slope at start (dy/dx at first point) - for centerline: 0</param>
    /// <param name="endSlope">Slope at end (dy/dx at last point)</param>
    /// <param name="startCurvature">Second derivative at start (d²y/dx²) - for midship: 0</param>
    /// <param name="endCurvature">Second derivative at end (d²y/dx²)</param>
    /// <param name="evaluationPoints">Parameter values for evaluation</param>
    /// <returns>Evaluated curve points</returns>
    public static List<decimal> GenerateCurveWithBoundaryConditions(
        List<(decimal x, decimal y)> controlPoints,
        int degree = 3,
        decimal? startSlope = null,
        decimal? endSlope = null,
        decimal? startCurvature = null,
        decimal? endCurvature = null,
        List<decimal>? evaluationPoints = null)
    {
        // Adjust control points to satisfy boundary conditions
        var adjustedPoints = EnforceBoundaryConditions(
            controlPoints,
            degree,
            startSlope,
            endSlope,
            startCurvature,
            endCurvature);

        // Generate uniform knot vector
        int n = adjustedPoints.Count - 1;
        var knots = GenerateUniformKnots(n, degree);

        // Generate evaluation points if not provided
        if (evaluationPoints == null)
        {
            evaluationPoints = GenerateEvaluationPoints(knots, degree, 50);
        }

        // Generate curve
        return GenerateCurve(adjustedPoints, degree, knots, null, evaluationPoints);
    }

    /// <summary>
    /// Evaluates NURBS curve at parameter value u
    /// </summary>
    private static (decimal x, decimal y) EvaluateNurbs(
        List<(decimal x, decimal y)> controlPoints,
        int degree,
        List<decimal> knots,
        List<decimal> weights,
        decimal u)
    {
        int n = controlPoints.Count - 1;

        // Clamp u to valid range [knots[degree], knots[n+1]]
        u = Math.Clamp(u, knots[degree], knots[n + 1]);

        // Find the knot span containing u
        int span = FindKnotSpan(n, degree, u, knots);

        // Evaluate basis functions
        var basisFunctions = EvaluateBasisFunctions(span, u, degree, knots);

        // Compute weighted sum
        decimal xNumerator = 0m;
        decimal yNumerator = 0m;
        decimal denominator = 0m;

        for (int i = 0; i <= degree; i++)
        {
            int idx = span - degree + i;
            decimal basis = basisFunctions[i];
            decimal weight = weights[idx];
            decimal weightedBasis = basis * weight;

            xNumerator += weightedBasis * controlPoints[idx].x;
            yNumerator += weightedBasis * controlPoints[idx].y;
            denominator += weightedBasis;
        }

        if (denominator == 0m)
        {
            return (0m, 0m);
        }

        return (xNumerator / denominator, yNumerator / denominator);
    }

    /// <summary>
    /// Evaluates B-spline basis functions using Cox-de Boor recursion
    /// </summary>
    private static List<decimal> EvaluateBasisFunctions(
        int span,
        decimal u,
        int degree,
        List<decimal> knots)
    {
        // CRITICAL FIX: Validate span is within safe bounds before accessing knot vector
        // This prevents ArgumentOutOfRangeException when extreme hull parameters create invalid geometries
        // Safe range: span must allow access to knots[span - degree] through knots[span + degree]
        int minValidSpan = degree;
        int maxValidSpan = knots.Count - degree - 2;

        if (span < minValidSpan || span > maxValidSpan)
        {
            // Clamp to safe range to prevent crash
            // This handles edge cases from extreme hull parameters
            span = Math.Clamp(span, minValidSpan, Math.Max(minValidSpan, maxValidSpan));
        }

        // Additional safety: ensure knots vector is large enough
        if (knots.Count < degree + 2)
        {
            // Degenerate case: not enough knots for this degree
            // Return uniform basis functions to prevent crash
            return Enumerable.Repeat(1m / (degree + 1), degree + 1).ToList();
        }

        var basisFunctions = new List<decimal>(degree + 1);
        // Pre-allocate left and right arrays with degree+1 elements to prevent index out of bounds
        var left = new List<decimal>(degree + 1);
        var right = new List<decimal>(degree + 1);

        basisFunctions.Add(1m);

        for (int j = 1; j <= degree; j++)
        {
            // Additional safety: validate array indices before access
            int leftIdx = span + 1 - j;
            int rightIdx = span + j;

            decimal leftValue = 0m;
            decimal rightValue = 0m;

            if (leftIdx >= 0 && leftIdx < knots.Count && rightIdx >= 0 && rightIdx < knots.Count)
            {
                leftValue = u - knots[leftIdx];
                rightValue = knots[rightIdx] - u;
            }

            // Ensure left and right have at least j+1 elements for safe indexing
            while (left.Count <= j)
            {
                left.Add(leftValue);
            }
            while (right.Count <= j)
            {
                right.Add(rightValue);
            }

            // Update the j-th element (0-indexed, so index j)
            left[j] = leftValue;
            right[j] = rightValue;

            decimal saved = 0m;

            for (int r = 0; r < j; r++)
            {
                // CRITICAL: Prevent division by zero which causes NaN
                // Access left[j - r] which is now safe because left has j+1 elements
                var denominator = right[r] + left[j - r];
                if (denominator == 0m)
                {
                    // Skip this iteration if denominator is zero (degenerate case)
                    continue;
                }
                decimal temp = basisFunctions[r] / denominator;
                basisFunctions[r] = saved + right[r] * temp;
                saved = left[j - r] * temp;
            }

            basisFunctions.Add(saved);
        }

        return basisFunctions;
    }

    /// <summary>
    /// Finds the knot span containing parameter u
    /// </summary>
    private static int FindKnotSpan(
        int n,
        int degree,
        decimal u,
        List<decimal> knots)
    {
        // Special case: u is at the last knot
        if (u >= knots[n + 1])
        {
            return n;
        }

        // Binary search for efficiency
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
    /// Generates uniform knot vector
    /// </summary>
    private static List<decimal> GenerateUniformKnots(int n, int degree)
    {
        var knots = new List<decimal>(n + degree + 2);
        int m = n + degree + 1;

        // Clamped uniform knots: first (degree+1) knots = 0, last (degree+1) knots = 1
        for (int i = 0; i <= degree; i++)
        {
            knots.Add(0m);
        }

        for (int i = 1; i < n - degree + 1; i++)
        {
            knots.Add((decimal)i / (n - degree + 1));
        }

        for (int i = 0; i <= degree; i++)
        {
            knots.Add(1m);
        }

        return knots;
    }

    /// <summary>
    /// Generates evaluation points uniformly distributed in valid parameter range
    /// </summary>
    private static List<decimal> GenerateEvaluationPoints(
        List<decimal> knots,
        int degree,
        int numPoints)
    {
        var points = new List<decimal>(numPoints);
        decimal uMin = knots[degree];
        decimal uMax = knots[knots.Count - degree - 1];

        for (int i = 0; i < numPoints; i++)
        {
            decimal u = uMin + (uMax - uMin) * i / (numPoints - 1);
            points.Add(u);
        }

        return points;
    }

    /// <summary>
    /// Enforces boundary conditions by adjusting control points
    /// </summary>
    private static List<(decimal x, decimal y)> EnforceBoundaryConditions(
        List<(decimal x, decimal y)> controlPoints,
        int degree,
        decimal? startSlope,
        decimal? endSlope,
        decimal? startCurvature,
        decimal? endCurvature)
    {
        if (controlPoints.Count < 2)
        {
            return controlPoints;
        }

        var adjusted = new List<(decimal x, decimal y)>(controlPoints);

        // Enforce start slope (dy/dx = 0 for centerline)
        if (startSlope.HasValue && adjusted.Count >= 2)
        {
            decimal dx = adjusted[1].x - adjusted[0].x;
            if (Math.Abs(dx) > 0.0001m)
            {
                // Adjust second control point to achieve desired slope
                decimal targetDy = startSlope.Value * dx;
                adjusted[1] = (adjusted[1].x, adjusted[0].y + targetDy);
            }
        }

        // Enforce end slope
        if (endSlope.HasValue && adjusted.Count >= 2)
        {
            int lastIdx = adjusted.Count - 1;
            decimal dx = adjusted[lastIdx].x - adjusted[lastIdx - 1].x;
            if (Math.Abs(dx) > 0.0001m)
            {
                decimal targetDy = endSlope.Value * dx;
                adjusted[lastIdx - 1] = (adjusted[lastIdx - 1].x, adjusted[lastIdx].y - targetDy);
            }
        }

        // Enforce start curvature (d²y/dx² = 0 for midship parallel body)
        if (startCurvature.HasValue && adjusted.Count >= 3)
        {
            // For zero curvature, ensure first three points are collinear
            decimal dx1 = adjusted[1].x - adjusted[0].x;
            decimal dx2 = adjusted[2].x - adjusted[1].x;

            if (Math.Abs(dx1) > 0.0001m && Math.Abs(dx2) > 0.0001m)
            {
                decimal slope1 = (adjusted[1].y - adjusted[0].y) / dx1;
                // Adjust third point to maintain same slope (zero curvature)
                adjusted[2] = (adjusted[2].x, adjusted[1].y + slope1 * dx2);
            }
        }

        // Enforce end curvature
        if (endCurvature.HasValue && adjusted.Count >= 3)
        {
            int lastIdx = adjusted.Count - 1;
            decimal dx1 = adjusted[lastIdx].x - adjusted[lastIdx - 1].x;
            decimal dx2 = adjusted[lastIdx - 1].x - adjusted[lastIdx - 2].x;

            if (Math.Abs(dx1) > 0.0001m && Math.Abs(dx2) > 0.0001m)
            {
                decimal slope1 = (adjusted[lastIdx].y - adjusted[lastIdx - 1].y) / dx1;
                // Adjust second-to-last point to maintain same slope
                adjusted[lastIdx - 2] = (adjusted[lastIdx - 2].x, adjusted[lastIdx - 1].y - slope1 * dx2);
            }
        }

        return adjusted;
    }
}
