using MathNet.Numerics.LinearAlgebra;
using Shared.Constants;

namespace Shared.HullGenerators.Fairing;

/// <summary>
/// Cubic spline fairing with clamped end slopes (CSFIT equivalent)
/// Prevents oscillation at bow/stern by specifying end slopes
/// Based on MATLAB CSFIT algorithm from BSRA methodology
/// </summary>
public class CubicSplineFairing
{
    /// <summary>
    /// Fit cubic spline with clamped boundary conditions
    /// </summary>
    /// <param name="x">Independent variable (stations or waterlines)</param>
    /// <param name="y">Dependent variable (half-breadths or areas)</param>
    /// <param name="dx0">Slope at first point (x[0])</param>
    /// <param name="dxn">Slope at last point (x[n-1])</param>
    /// <returns>Spline coefficients: [a, b, c, d] for each interval</returns>
    public static List<SplineCoefficients> Fit(
        List<decimal> x,
        List<decimal> y,
        decimal dx0,
        decimal dxn)
    {
        if (x.Count != y.Count)
            throw new ArgumentException("x and y must have the same length");
        if (x.Count < 2)
            throw new ArgumentException("At least 2 points required for spline fitting");

        int n = x.Count;
        var h = new List<decimal>(); // Interval widths
        for (int i = 0; i < n - 1; i++)
        {
            h.Add(x[i + 1] - x[i]);
            if (h[i] <= 0)
                throw new ArgumentException("x must be strictly increasing");
        }

        // Build tridiagonal system: A * M = b
        // A: Tridiagonal matrix (n×n)
        // M: Second derivatives (unknowns)
        // b: Right-hand side vector
        // Note: MathNet.Numerics uses double, so convert from decimal

        var A = Matrix<double>.Build.Dense(n, n, 0);
        var b = Vector<double>.Build.Dense(n, 0);

        // First row (clamped boundary at x0)
        A[0, 0] = (double)(2 * h[0]);
        A[0, 1] = (double)h[0];
        b[0] = (double)(3 * ((y[1] - y[0]) / h[0] - dx0));

        // Interior rows
        for (int i = 1; i < n - 1; i++)
        {
            A[i, i - 1] = (double)h[i - 1];
            A[i, i] = (double)(2 * (h[i - 1] + h[i]));
            A[i, i + 1] = (double)h[i];
            b[i] = (double)(3 * (((y[i + 1] - y[i]) / h[i]) - ((y[i] - y[i - 1]) / h[i - 1])));
        }

        // Last row (clamped boundary at xn)
        A[n - 1, n - 2] = (double)h[n - 2];
        A[n - 1, n - 1] = (double)(2 * h[n - 2]);
        b[n - 1] = (double)(3 * (dxn - (y[n - 1] - y[n - 2]) / h[n - 2]));

        // Solve for second derivatives: M = A \ b
        var M = A.Solve(b);

        // Convert to piecewise polynomial form: [a, b, c, d] for each interval
        var coefficients = new List<SplineCoefficients>();
        for (int i = 0; i < n - 1; i++)
        {
            var a = (decimal)((M[i + 1] - M[i]) / (6 * (double)h[i]));
            var b_coeff = (decimal)(M[i] / 2);
            var c = (y[i + 1] - y[i]) / h[i] - h[i] * (decimal)(2 * M[i] + M[i + 1]) / 6;
            var d = y[i];

            coefficients.Add(new SplineCoefficients
            {
                A = a,
                B = b_coeff,
                C = c,
                D = d,
                XStart = x[i],
                XEnd = x[i + 1]
            });
        }

        return coefficients;
    }

    /// <summary>
    /// Evaluate spline at a point
    /// </summary>
    public static decimal Evaluate(
        List<SplineCoefficients> coefficients,
        decimal x)
    {
        // Find the interval containing x
        for (int i = 0; i < coefficients.Count; i++)
        {
            var coeff = coefficients[i];
            if (x >= coeff.XStart && x <= coeff.XEnd)
            {
                // Evaluate polynomial: y = a*(x-x0)^3 + b*(x-x0)^2 + c*(x-x0) + d
                decimal dx = x - coeff.XStart;
                return coeff.A * dx * dx * dx +
                       coeff.B * dx * dx +
                       coeff.C * dx +
                       coeff.D;
            }
        }

        // Extrapolate using first or last interval
        if (x < coefficients[0].XStart)
        {
            var coeff = coefficients[0];
            decimal dx = x - coeff.XStart;
            return coeff.A * dx * dx * dx +
                   coeff.B * dx * dx +
                   coeff.C * dx +
                   coeff.D;
        }
        else
        {
            var coeff = coefficients[coefficients.Count - 1];
            decimal dx = x - coeff.XStart;
            return coeff.A * dx * dx * dx +
                   coeff.B * dx * dx +
                   coeff.C * dx +
                   coeff.D;
        }
    }

    /// <summary>
    /// Fair a waterline using cubic spline with end slopes from BSRA constants
    /// </summary>
    public static List<decimal> FairWaterline(
        List<decimal> stations,
        List<decimal> halfBreadths,
        decimal waterlineHeight)
    {
        // Find closest end slope from BSRA constants
        var endSlope = BSRAConstants.EndSlopesCb080
            .OrderBy(s => Math.Abs(s.Waterline - waterlineHeight))
            .FirstOrDefault();

        decimal bowSlope = endSlope?.BowSlope ?? -1.0m;
        decimal sternSlope = endSlope?.SternSlope ?? 1.0m;

        // Fit spline
        var coefficients = Fit(stations, halfBreadths, bowSlope, sternSlope);

        // Resample at original stations
        var faired = new List<decimal>();
        foreach (var station in stations)
        {
            faired.Add(Evaluate(coefficients, station));
        }

        return faired;
    }

    /// <summary>
    /// Fair a section (vertical profile) using cubic spline
    /// </summary>
    public static List<decimal> FairSection(
        List<decimal> waterlines,
        List<decimal> halfBreadths)
    {
        // For sections, estimate end slopes from data
        decimal dz0 = waterlines.Count > 1
            ? (halfBreadths[1] - halfBreadths[0]) / (waterlines[1] - waterlines[0])
            : 0m;

        decimal dzn = waterlines.Count > 1
            ? (halfBreadths[waterlines.Count - 1] - halfBreadths[waterlines.Count - 2]) /
              (waterlines[waterlines.Count - 1] - waterlines[waterlines.Count - 2])
            : 0m;

        // Fit spline
        var coefficients = Fit(waterlines, halfBreadths, dz0, dzn);

        // Resample at original waterlines
        var faired = new List<decimal>();
        foreach (var waterline in waterlines)
        {
            faired.Add(Evaluate(coefficients, waterline));
        }

        return faired;
    }
}

/// <summary>
/// Cubic spline coefficients for one interval
/// Polynomial: y = a*(x-x0)^3 + b*(x-x0)^2 + c*(x-x0) + d
/// </summary>
public class SplineCoefficients
{
    public decimal A { get; set; } // Cubic coefficient
    public decimal B { get; set; } // Quadratic coefficient
    public decimal C { get; set; } // Linear coefficient
    public decimal D { get; set; } // Constant term
    public decimal XStart { get; set; } // Start of interval
    public decimal XEnd { get; set; } // End of interval
}
