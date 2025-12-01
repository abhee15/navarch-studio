using System;
using System.Collections.Generic;
using System.Linq;

namespace HullSizingService.Services.Integration;

/// <summary>
/// Integration engine for optimization service
/// Uses Gauss Quadrature for high accuracy with minimal function evaluations
/// Falls back to Simpson's/Trapezoidal for discrete data
/// </summary>
public class SimpleIntegrationEngine : IIntegrationEngine
{
    /// <summary>
    /// 7-point Gauss-Legendre quadrature weights and points
    /// Provides high accuracy (exact for polynomials up to degree 13)
    /// Points and weights are for interval [-1, 1], will be transformed to [a, b]
    /// </summary>
    private static readonly (decimal point, decimal weight)[] Gauss7Point = new[]
    {
        (-0.949107912342758m, 0.129484966168870m),
        (-0.741531185599394m, 0.279705391489277m),
        (-0.405845151377397m, 0.381830050505119m),
        (0.000000000000000m, 0.417959183673469m),
        (0.405845151377397m, 0.381830050505119m),
        (0.741531185599394m, 0.279705391489277m),
        (0.949107912342758m, 0.129484966168870m)
    };

    /// <summary>
    /// 5-point Gauss-Legendre quadrature (faster, still very accurate)
    /// Exact for polynomials up to degree 9
    /// </summary>
    private static readonly (decimal point, decimal weight)[] Gauss5Point = new[]
    {
        (-0.906179845938664m, 0.236926885056189m),
        (-0.538469310105683m, 0.478628670499366m),
        (0.000000000000000m, 0.568888888888889m),
        (0.538469310105683m, 0.478628670499366m),
        (0.906179845938664m, 0.236926885056189m)
    };

    public decimal Integrate(List<decimal> x, List<decimal> y)
    {
        if (x.Count != y.Count || x.Count < 2)
        {
            return 0m;
        }

        // For discrete data, use traditional methods
        // Use Simpson's rule for equally spaced data with odd number of points
        if (IsEquallySpaced(x) && x.Count % 2 == 1 && x.Count >= 3)
        {
            return SimpsonsRule(x, y);
        }

        // Use trapezoidal rule otherwise
        return TrapezoidalRule(x, y);
    }

    /// <summary>
    /// Integrates a function using Gauss-Legendre quadrature
    /// Transforms the integration interval [a, b] to [-1, 1] and applies weights
    /// </summary>
    /// <param name="func">Function to integrate: f(x) -> y</param>
    /// <param name="a">Lower bound</param>
    /// <param name="b">Upper bound</param>
    /// <param name="use7Point">If true, use 7-point rule (more accurate), else 5-point (faster)</param>
    /// <returns>Integral value</returns>
    public decimal GaussQuadrature(Func<decimal, decimal> func, decimal a, decimal b, bool use7Point = true)
    {
        var points = use7Point ? Gauss7Point : Gauss5Point;
        decimal sum = 0m;

        // Transform from [-1, 1] to [a, b]
        decimal scale = (b - a) / 2m;
        decimal offset = (a + b) / 2m;

        foreach (var (point, weight) in points)
        {
            decimal x = offset + scale * point;
            decimal y = func(x);
            sum += weight * y;
        }

        return scale * sum;
    }

    /// <summary>
    /// Integrates a function over multiple segments using composite Gauss quadrature
    /// Splits [a, b] into segments and applies Gauss quadrature to each
    /// </summary>
    /// <param name="func">Function to integrate</param>
    /// <param name="a">Lower bound</param>
    /// <param name="b">Upper bound</param>
    /// <param name="numSegments">Number of segments to divide interval into</param>
    /// <param name="use7Point">If true, use 7-point rule, else 5-point</param>
    /// <returns>Integral value</returns>
    public decimal CompositeGaussQuadrature(
        Func<decimal, decimal> func,
        decimal a,
        decimal b,
        int numSegments = 1,
        bool use7Point = true)
    {
        if (numSegments < 1)
        {
            numSegments = 1;
        }

        decimal segmentWidth = (b - a) / numSegments;
        decimal sum = 0m;

        for (int i = 0; i < numSegments; i++)
        {
            decimal segmentA = a + i * segmentWidth;
            decimal segmentB = a + (i + 1) * segmentWidth;
            sum += GaussQuadrature(func, segmentA, segmentB, use7Point);
        }

        return sum;
    }

    public decimal SimpsonsRule(List<decimal> x, List<decimal> y)
    {
        if (x.Count < 3 || x.Count % 2 == 0)
        {
            return TrapezoidalRule(x, y);
        }

        decimal h = x[1] - x[0];
        decimal sum = y[0] + y[x.Count - 1];

        for (int i = 1; i < x.Count - 1; i++)
        {
            if (i % 2 == 1)
            {
                sum += 4m * y[i];
            }
            else
            {
                sum += 2m * y[i];
            }
        }

        return (h / 3m) * sum;
    }

    public decimal CompositeSimpson(List<decimal> x, List<decimal> y)
    {
        // For even number of points, use Simpson's 3/8 rule for last segment
        if (x.Count < 4)
        {
            return TrapezoidalRule(x, y);
        }

        decimal h = x[1] - x[0];
        decimal sum = 0m;

        // Use Simpson's 1/3 for all but last 3 points
        int n = x.Count - 1;
        if (n % 2 == 0)
        {
            // Even number of intervals - use 3/8 rule for last segment
            for (int i = 0; i < n - 3; i += 2)
            {
                sum += (h / 3m) * (y[i] + 4m * y[i + 1] + y[i + 2]);
            }
            // Last 3 intervals using 3/8 rule
            sum += (3m * h / 8m) * (y[n - 3] + 3m * y[n - 2] + 3m * y[n - 1] + y[n]);
        }
        else
        {
            // Odd number of intervals - all Simpson's 1/3
            return SimpsonsRule(x, y);
        }

        return sum;
    }

    public decimal TrapezoidalRule(List<decimal> x, List<decimal> y)
    {
        if (x.Count < 2)
        {
            return 0m;
        }

        decimal sum = 0m;
        for (int i = 0; i < x.Count - 1; i++)
        {
            decimal dx = x[i + 1] - x[i];
            decimal avg = (y[i] + y[i + 1]) / 2m;
            sum += dx * avg;
        }

        return sum;
    }

    public decimal FirstMoment(List<decimal> x, List<decimal> y)
    {
        if (x.Count != y.Count || x.Count < 2)
        {
            return 0m;
        }

        // Compute ∫ x * y dx using trapezoidal rule
        decimal sum = 0m;
        for (int i = 0; i < x.Count - 1; i++)
        {
            decimal dx = x[i + 1] - x[i];
            decimal avg = (x[i] * y[i] + x[i + 1] * y[i + 1]) / 2m;
            sum += dx * avg;
        }

        return sum;
    }

    public decimal SecondMoment(List<decimal> x, List<decimal> y)
    {
        if (x.Count != y.Count || x.Count < 2)
        {
            return 0m;
        }

        // Compute ∫ x² * y dx using trapezoidal rule
        decimal sum = 0m;
        for (int i = 0; i < x.Count - 1; i++)
        {
            decimal dx = x[i + 1] - x[i];
            decimal avg = (x[i] * x[i] * y[i] + x[i + 1] * x[i + 1] * y[i + 1]) / 2m;
            sum += dx * avg;
        }

        return sum;
    }

    private bool IsEquallySpaced(List<decimal> x)
    {
        if (x.Count < 2)
        {
            return true;
        }

        decimal spacing = x[1] - x[0];
        decimal tolerance = Math.Abs(spacing) * 0.0001m; // 0.01% tolerance

        for (int i = 1; i < x.Count - 1; i++)
        {
            decimal currentSpacing = x[i + 1] - x[i];
            if (Math.Abs(currentSpacing - spacing) > tolerance)
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// Integration engine interface for optimization service
/// </summary>
public interface IIntegrationEngine
{
    decimal Integrate(List<decimal> x, List<decimal> y);
    decimal SimpsonsRule(List<decimal> x, List<decimal> y);
    decimal CompositeSimpson(List<decimal> x, List<decimal> y);
    decimal TrapezoidalRule(List<decimal> x, List<decimal> y);
    decimal FirstMoment(List<decimal> x, List<decimal> y);
    decimal SecondMoment(List<decimal> x, List<decimal> y);

    /// <summary>
    /// Integrates a function using Gauss-Legendre quadrature
    /// High accuracy with minimal function evaluations
    /// </summary>
    decimal GaussQuadrature(Func<decimal, decimal> func, decimal a, decimal b, bool use7Point = true);

    /// <summary>
    /// Composite Gauss quadrature over multiple segments
    /// </summary>
    decimal CompositeGaussQuadrature(Func<decimal, decimal> func, decimal a, decimal b, int numSegments = 1, bool use7Point = true);
}
