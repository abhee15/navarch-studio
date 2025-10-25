namespace DataService.Services.Hydrostatics;

/// <summary>
/// Service for numerical integration methods
/// </summary>
public interface IIntegrationEngine
{
    /// <summary>
    /// Integrates using Simpson's 1/3 Rule (requires odd number of points, even number of intervals)
    /// </summary>
    /// <param name="x">X coordinates (must be equally spaced)</param>
    /// <param name="y">Y values at each X</param>
    /// <returns>Integral value</returns>
    decimal SimpsonsRule(List<decimal> x, List<decimal> y);

    /// <summary>
    /// Integrates using Composite Simpson's Rule (handles larger datasets)
    /// </summary>
    /// <param name="x">X coordinates</param>
    /// <param name="y">Y values at each X</param>
    /// <returns>Integral value</returns>
    decimal CompositeSimpson(List<decimal> x, List<decimal> y);

    /// <summary>
    /// Integrates using Trapezoidal Rule (works with any number of points and irregular spacing)
    /// </summary>
    /// <param name="x">X coordinates (can be irregularly spaced)</param>
    /// <param name="y">Y values at each X</param>
    /// <returns>Integral value</returns>
    decimal TrapezoidalRule(List<decimal> x, List<decimal> y);

    /// <summary>
    /// Automatically selects best integration method based on data characteristics
    /// </summary>
    /// <param name="x">X coordinates</param>
    /// <param name="y">Y values at each X</param>
    /// <returns>Integral value</returns>
    decimal Integrate(List<decimal> x, List<decimal> y);

    /// <summary>
    /// Computes first moment (for centroid calculation): ∫ x * y dx
    /// </summary>
    decimal FirstMoment(List<decimal> x, List<decimal> y);

    /// <summary>
    /// Computes second moment of area: ∫ x² * y dx
    /// </summary>
    decimal SecondMoment(List<decimal> x, List<decimal> y);
}

