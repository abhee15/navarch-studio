namespace HullSizingService.Services.Solver;

/// <summary>
/// Configuration options for displacement closure solver
/// </summary>
public class SolverOptions
{
    /// <summary>
    /// Enable detailed per-iteration logging for debugging
    /// </summary>
    public bool DebugIterations { get; set; } = false;

    /// <summary>
    /// Maximum Newton-Raphson iterations before fallback to Brent
    /// </summary>
    public int NewtonMaxIter { get; set; } = 20;

    /// <summary>
    /// Maximum Brent iterations
    /// </summary>
    public int MaxBrentIter { get; set; } = 100;

    /// <summary>
    /// Convergence tolerance for displacement (relative error)
    /// </summary>
    public double ConvergenceTolerance { get; set; } = 0.01; // ±1%

    /// <summary>
    /// Absolute tolerance for Brent root finder (meters)
    /// </summary>
    public double BrentAbsoluteTolerance { get; set; } = 0.001; // 1mm

    /// <summary>
    /// Relative tolerance for Brent root finder
    /// </summary>
    public double BrentRelativeTolerance { get; set; } = 0.0001; // 0.01%

    /// <summary>
    /// Step size for finite difference derivative (fraction of x)
    /// </summary>
    public double DerivativeStep { get; set; } = 0.001; // 0.1%

    /// <summary>
    /// Minimum derivative magnitude to accept Newton step
    /// </summary>
    public double MinimumDerivative { get; set; } = 1e-8;

    /// <summary>
    /// Maximum draft step per Newton iteration (meters)
    /// </summary>
    public double MaxDraftStep { get; set; } = 5.0; // 5m max step

    /// <summary>
    /// Minimum backtracking alpha before giving up on Newton
    /// </summary>
    public double MinimumAlpha { get; set; } = 0.01; // 1% of full step

    /// <summary>
    /// Backtracking damping factor (multiply alpha by this each iteration)
    /// </summary>
    public double AlphaDampingFactor { get; set; } = 0.5;

    /// <summary>
    /// Minimum draft as fraction of depth (for bracketing)
    /// </summary>
    public double MinDraftFraction { get; set; } = 0.01; // 1% of depth

    /// <summary>
    /// Maximum draft as fraction of depth (for bracketing)
    /// </summary>
    public double MaxDraftFraction { get; set; } = 0.98; // 98% of depth (preserve freeboard)

    /// <summary>
    /// Expand bracket search multiplier if initial bracket fails
    /// </summary>
    public double BracketExpansionFactor { get; set; } = 1.5;

    /// <summary>
    /// Maximum bracket expansion attempts
    /// </summary>
    public int MaxBracketExpansions { get; set; } = 5;
}
