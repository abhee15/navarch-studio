namespace DataService.Services.Hydrostatics;

/// <summary>
/// Implementation of numerical integration methods
/// </summary>
public class IntegrationEngine : IIntegrationEngine
{
    private readonly ILogger<IntegrationEngine> _logger;
    private const decimal SPACING_TOLERANCE = 0.001m; // 1mm tolerance for equal spacing check

    public IntegrationEngine(ILogger<IntegrationEngine> logger)
    {
        _logger = logger;
    }

    public decimal SimpsonsRule(List<decimal> x, List<decimal> y)
    {
        if (x.Count != y.Count)
        {
            throw new ArgumentException("X and Y arrays must have the same length");
        }

        if (x.Count < 3)
        {
            throw new ArgumentException("At least 3 points required for Simpson's Rule");
        }

        if (x.Count % 2 == 0)
        {
            throw new ArgumentException("Simpson's Rule requires odd number of points (even number of intervals)");
        }

        // Check for equal spacing
        decimal dx = x[1] - x[0];
        for (int i = 1; i < x.Count - 1; i++)
        {
            decimal spacing = x[i + 1] - x[i];
            if (Math.Abs(spacing - dx) > SPACING_TOLERANCE)
            {
                throw new ArgumentException("Simpson's Rule requires equally spaced points");
            }
        }

        // Simpson's 1/3 Rule: ∫f(x)dx ≈ (h/3)[f(x0) + 4f(x1) + 2f(x2) + 4f(x3) + ... + 4f(xn-1) + f(xn)]
        decimal sum = y[0] + y[^1]; // First and last terms

        // Middle terms with alternating 4 and 2 coefficients
        for (int i = 1; i < x.Count - 1; i++)
        {
            sum += (i % 2 == 1 ? 4 : 2) * y[i];
        }

        return (dx / 3) * sum;
    }

    public decimal CompositeSimpson(List<decimal> x, List<decimal> y)
    {
        if (x.Count != y.Count)
        {
            throw new ArgumentException("X and Y arrays must have the same length");
        }

        if (x.Count < 3)
        {
            throw new ArgumentException("At least 3 points required for Composite Simpson's Rule");
        }

        // If odd number of points, use Simpson directly
        if (x.Count % 2 == 1)
        {
            return SimpsonsRule(x, y);
        }

        // If even number of points, apply Simpson to first n-1 points and Trapezoid to last segment
        var xSubset = x.Take(x.Count - 1).ToList();
        var ySubset = y.Take(y.Count - 1).ToList();

        decimal simpsonPart = SimpsonsRule(xSubset, ySubset);

        // Trapezoidal rule for last segment
        int n = x.Count - 1;
        decimal lastSegment = (x[n] - x[n - 1]) * (y[n] + y[n - 1]) / 2;

        return simpsonPart + lastSegment;
    }

    public decimal TrapezoidalRule(List<decimal> x, List<decimal> y)
    {
        if (x.Count != y.Count)
        {
            throw new ArgumentException("X and Y arrays must have the same length");
        }

        if (x.Count < 2)
        {
            throw new ArgumentException("At least 2 points required for Trapezoidal Rule");
        }

        decimal sum = 0;
        for (int i = 0; i < x.Count - 1; i++)
        {
            decimal dx = x[i + 1] - x[i];
            if (dx < 0)
            {
                throw new ArgumentException("X values must be monotonically increasing");
            }
            sum += dx * (y[i] + y[i + 1]) / 2;
        }

        return sum;
    }

    public decimal Integrate(List<decimal> x, List<decimal> y)
    {
        if (x.Count != y.Count)
        {
            throw new ArgumentException("X and Y arrays must have the same length");
        }

        if (x.Count < 2)
        {
            return 0;
        }

        // Check if equally spaced
        bool equallySpaced = IsEquallySpaced(x);

        // Prefer Simpson's for equally spaced data with odd number of points
        if (equallySpaced && x.Count % 2 == 1 && x.Count >= 3)
        {
            _logger.LogDebug("Using Simpson's Rule for integration ({Count} points)", x.Count);
            return SimpsonsRule(x, y);
        }

        // Use Composite Simpson for equally spaced data with even number of points
        if (equallySpaced && x.Count >= 3)
        {
            _logger.LogDebug("Using Composite Simpson's Rule for integration ({Count} points)", x.Count);
            return CompositeSimpson(x, y);
        }

        // Fallback to Trapezoidal for irregular spacing
        _logger.LogDebug("Using Trapezoidal Rule for integration ({Count} points)", x.Count);
        return TrapezoidalRule(x, y);
    }

    public decimal FirstMoment(List<decimal> x, List<decimal> y)
    {
        if (x.Count != y.Count)
        {
            throw new ArgumentException("X and Y arrays must have the same length");
        }

        // Compute ∫ x * y dx using trapezoidal rule
        decimal sum = 0;
        for (int i = 0; i < x.Count - 1; i++)
        {
            decimal dx = x[i + 1] - x[i];
            decimal avg = (x[i] * y[i] + x[i + 1] * y[i + 1]) / 2;
            sum += dx * avg;
        }

        return sum;
    }

    public decimal SecondMoment(List<decimal> x, List<decimal> y)
    {
        if (x.Count != y.Count)
        {
            throw new ArgumentException("X and Y arrays must have the same length");
        }

        // Compute ∫ x² * y dx using trapezoidal rule
        decimal sum = 0;
        for (int i = 0; i < x.Count - 1; i++)
        {
            decimal dx = x[i + 1] - x[i];
            decimal avg = (x[i] * x[i] * y[i] + x[i + 1] * x[i + 1] * y[i + 1]) / 2;
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

        decimal dx = x[1] - x[0];
        for (int i = 1; i < x.Count - 1; i++)
        {
            decimal spacing = x[i + 1] - x[i];
            if (Math.Abs(spacing - dx) > SPACING_TOLERANCE)
            {
                return false;
            }
        }

        return true;
    }
}

