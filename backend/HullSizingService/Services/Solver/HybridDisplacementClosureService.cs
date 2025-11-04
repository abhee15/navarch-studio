using MathNet.Numerics.RootFinding;

namespace HullSizingService.Services.Solver;

/// <summary>
/// Robust hybrid displacement closure solver using damped Newton + Brent fallback
/// Based on research-backed numerical methods for naval architecture applications
/// </summary>
/// <remarks>
/// Algorithm: Attempt damped Newton-Raphson with backtracking line search first for speed.
/// If Newton fails (poor derivative, residual increases, or no convergence), fall back to
/// Brent's method which guarantees convergence given a valid bracket.
/// 
/// References:
/// - MathNet.Numerics Brent implementation
/// - Burke: Backtracking line search for Newton methods
/// - Numerical Recipes: Robust root finding
/// </remarks>
public class HybridDisplacementClosureService : IDisplacementClosureService
{
    private readonly ILogger<HybridDisplacementClosureService> _logger;
    private readonly SolverOptions _options;
    private const double G = 9.81; // Gravity (m/s²)

    public HybridDisplacementClosureService(
        ILogger<HybridDisplacementClosureService> logger,
        SolverOptions? options = null)
    {
        _logger = logger;
        _options = options ?? new SolverOptions();
    }

    public async Task<ClosureResult> SolveAsync(ClosureRequest req, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[HYBRID-CLOSURE] Starting displacement closure for target Δ={Target}t", req.TargetDisplacementT);
        
        var flags = new List<string>();

        // Check for over-constrained problem before starting
        if (req.KeepBOverT && req.KeepCb && req.KeepLOverB)
        {
            flags.Add("all_locked_cannot_converge");
            _logger.LogWarning("[HYBRID-CLOSURE] All parameters locked - cannot adjust to meet target displacement");
            return await ReturnInitialGuess(req, flags);
        }

        // Convert target displacement to volume
        double targetVolumeM3 = (double)(req.TargetDisplacementT / (req.WaterDensityKgM3 / 1000m));
        double waterDensity = (double)req.WaterDensityKgM3;

        // For this problem, we're primarily solving for draft given other parameters
        // f(draft) = computedVolume(draft) - targetVolume = 0
        
        // Initial physics-based guess
        double initialDraft = PhysicsBasedInitialGuess(req, targetVolumeM3);
        
        if (_options.DebugIterations)
        {
            _logger.LogDebug("[HYBRID-CLOSURE] Initial guess: draft={Draft:F3}m", initialDraft);
        }

        // Define the root-finding problem
        Func<double, double> volumeFunction = draft => ComputeVolume(draft, req) - targetVolumeM3;

        // Define draft bounds for bracketing
        double depth = initialDraft * (double)req.DOverT;
        double minDraft = Math.Max(0.001, depth * _options.MinDraftFraction); // Avoid zero
        double maxDraft = depth * _options.MaxDraftFraction;

        // Try damped Newton first (fast path)
        var newtonResult = TryDampedNewton(volumeFunction, initialDraft, minDraft, maxDraft, targetVolumeM3);

        double convergedDraft;
        bool usedBrent = false;
        int totalIterations;

        if (newtonResult.Converged)
        {
            convergedDraft = newtonResult.Draft;
            totalIterations = newtonResult.Iterations;
            _logger.LogInformation("[HYBRID-CLOSURE] Newton converged in {Iter} iterations, draft={Draft:F3}m", 
                totalIterations, convergedDraft);
        }
        else
        {
            // Newton failed, fall back to Brent (robust path)
            _logger.LogInformation("[HYBRID-CLOSURE] Newton failed, falling back to Brent method");
            usedBrent = true;

            try
            {
                // Ensure we have a valid bracket
                var bracket = EnsureBracket(volumeFunction, minDraft, maxDraft);
                if (!bracket.HasValue)
                {
                    flags.Add("no_bracket_found");
                    _logger.LogError("[HYBRID-CLOSURE] Could not find valid bracket for root");
                    return await ReturnInitialGuess(req, flags);
                }

                // Use Brent's method
                convergedDraft = Brent.FindRoot(
                    volumeFunction,
                    bracket.Value.lower,
                    bracket.Value.upper,
                    accuracy: _options.BrentAbsoluteTolerance,
                    maxIterations: _options.MaxBrentIter
                );

                totalIterations = newtonResult.Iterations + _options.MaxBrentIter; // Approximate
                _logger.LogInformation("[HYBRID-CLOSURE] Brent converged, draft={Draft:F3}m", convergedDraft);
            }
            catch (Exception ex)
            {
                flags.Add("brent_failed");
                _logger.LogError(ex, "[HYBRID-CLOSURE] Brent method failed");
                return await ReturnInitialGuess(req, flags);
            }
        }

        // Build result from converged draft
        return await BuildResult(convergedDraft, req, waterDensity, totalIterations, usedBrent, flags);
    }

    /// <summary>
    /// Physics-based initial guess using cube-root scaling
    /// </summary>
    private double PhysicsBasedInitialGuess(ClosureRequest req, double targetVolumeM3)
    {
        // Volume = L * B * T * Cb = (L/B * B) * B * T * Cb
        // If we maintain ratios: L = L/B * B, T = B / (B/T)
        // Volume = L/B * B * B * (B / (B/T)) * Cb = L/B * B³ / (B/T) * Cb
        // B³ = Volume * (B/T) / (L/B * Cb)
        // B = (Volume * (B/T) / (L/B * Cb))^(1/3)

        double BOverT = (double)req.BOverT;
        double LOverB = (double)req.LOverB;
        double Cb = (double)req.Cb;

        double beam = Math.Pow(targetVolumeM3 * BOverT / (LOverB * Cb), 1.0 / 3.0);
        double draft = beam / BOverT;

        return draft;
    }

    /// <summary>
    /// Compute submerged volume given draft and request parameters
    /// </summary>
    private double ComputeVolume(double draft, ClosureRequest req)
    {
        // For displacement closure, we're using prismatic approximation
        // Volume = Lpp * Beam * Draft * Cb
        
        double beam = draft * (double)req.BOverT;
        double lpp = beam * (double)req.LOverB;
        double cb = (double)req.Cb;

        // Apply beam constraint if specified
        if (req.MaxBeamM.HasValue && beam > (double)req.MaxBeamM.Value)
        {
            beam = (double)req.MaxBeamM.Value;
            lpp = beam * (double)req.LOverB; // Recalculate to maintain ratio
        }

        double volume = lpp * beam * draft * cb;
        return volume;
    }

    /// <summary>
    /// Try damped Newton-Raphson with backtracking line search
    /// </summary>
    private (bool Converged, double Draft, int Iterations) TryDampedNewton(
        Func<double, double> f, 
        double x0, 
        double minDraft, 
        double maxDraft,
        double targetVolume)
    {
        double x = x0;
        int iter = 0;

        for (iter = 0; iter < _options.NewtonMaxIter; iter++)
        {
            double fx = f(x);
            double relativeError = Math.Abs(fx / targetVolume);

            // Check convergence
            if (relativeError < _options.ConvergenceTolerance)
            {
                if (_options.DebugIterations)
                {
                    _logger.LogDebug("[NEWTON] Converged at iter={Iter}, x={X:F3}, f(x)={Fx:F2}, error={Error:P2}",
                        iter, x, fx, relativeError);
                }
                return (true, x, iter);
            }

            // Compute derivative using central finite difference
            double h = x * _options.DerivativeStep;
            double fxPlus = f(x + h);
            double fxMinus = f(x - h);
            double dfdx = (fxPlus - fxMinus) / (2 * h);

            if (_options.DebugIterations)
            {
                _logger.LogDebug("[NEWTON] Iter={Iter}: x={X:F3}, f(x)={Fx:F2}, df/dx={Deriv:F2}, error={Error:P2}",
                    iter, x, fx, dfdx, relativeError);
            }

            // Check derivative magnitude
            if (Math.Abs(dfdx) < _options.MinimumDerivative)
            {
                _logger.LogDebug("[NEWTON] Derivative too small ({Deriv}), abandoning Newton", dfdx);
                return (false, x, iter);
            }

            // Newton step
            double dx = -fx / dfdx;

            // Limit step size
            double maxStep = _options.MaxDraftStep;
            if (Math.Abs(dx) > maxStep)
            {
                dx = Math.Sign(dx) * maxStep;
            }

            // Backtracking line search
            double alpha = 1.0;
            bool accepted = false;

            while (alpha >= _options.MinimumAlpha)
            {
                double xTrial = x + alpha * dx;

                // Check physical bounds
                if (xTrial < minDraft || xTrial > maxDraft)
                {
                    alpha *= _options.AlphaDampingFactor;
                    continue;
                }

                double fxTrial = f(xTrial);

                // Armijo condition: accept if residual decreases
                if (Math.Abs(fxTrial) < Math.Abs(fx))
                {
                    x = xTrial;
                    accepted = true;

                    if (_options.DebugIterations)
                    {
                        _logger.LogDebug("[NEWTON] Accepted step: alpha={Alpha:F3}, new x={X:F3}, new f(x)={Fx:F2}",
                            alpha, x, fxTrial);
                    }
                    break;
                }

                alpha *= _options.AlphaDampingFactor;
            }

            if (!accepted)
            {
                _logger.LogDebug("[NEWTON] Line search failed, abandoning Newton");
                return (false, x, iter);
            }
        }

        _logger.LogDebug("[NEWTON] Max iterations reached without convergence");
        return (false, x, iter);
    }

    /// <summary>
    /// Ensure we have a valid bracket [a, b] where f(a) * f(b) < 0
    /// </summary>
    private (double lower, double upper)? EnsureBracket(Func<double, double> f, double minDraft, double maxDraft)
    {
        double a = minDraft;
        double b = maxDraft;
        double fa = f(a);
        double fb = f(b);

        if (_options.DebugIterations)
        {
            _logger.LogDebug("[BRACKET] Initial: a={A:F3}, f(a)={Fa:F2}, b={B:F3}, f(b)={Fb:F2}",
                a, fa, b, fb);
        }

        // Check if we already have a bracket
        if (fa * fb < 0)
        {
            return (a, b);
        }

        // Try expanding the bracket
        for (int i = 0; i < _options.MaxBracketExpansions; i++)
        {
            // Expand outward
            double newA = a / _options.BracketExpansionFactor;
            double newB = b * _options.BracketExpansionFactor;

            // Keep physical bounds reasonable
            newA = Math.Max(0.001, newA);
            newB = Math.Min(maxDraft * 2.0, newB); // Allow some expansion beyond initial guess

            fa = f(newA);
            fb = f(newB);

            if (_options.DebugIterations)
            {
                _logger.LogDebug("[BRACKET] Expansion {Iter}: a={A:F3}, f(a)={Fa:F2}, b={B:F3}, f(b)={Fb:F2}",
                    i + 1, newA, fa, newB, fb);
            }

            if (fa * fb < 0)
            {
                return (newA, newB);
            }

            a = newA;
            b = newB;
        }

        // Could not find bracket
        return null;
    }

    /// <summary>
    /// Build final result from converged draft
    /// </summary>
    private async Task<ClosureResult> BuildResult(
        double convergedDraft,
        ClosureRequest req,
        double waterDensity,
        int iterations,
        bool usedBrent,
        List<string> flags)
    {
        // Calculate final dimensions
        double draft = convergedDraft;
        double beam = draft * (double)req.BOverT;
        double lpp = beam * (double)req.LOverB;
        double cb = (double)req.Cb;

        // Apply constraints
        if (req.MaxBeamM.HasValue && beam > (double)req.MaxBeamM.Value)
        {
            beam = (double)req.MaxBeamM.Value;
            lpp = beam * (double)req.LOverB;
            flags.Add("beam_constrained");
        }

        if (req.MaxDraftM.HasValue && draft > (double)req.MaxDraftM.Value)
        {
            draft = (double)req.MaxDraftM.Value;
            flags.Add("draft_constrained");
        }

        double depth = draft * (double)req.DOverT;

        // Calculate final displacement
        double volumeM3 = lpp * beam * draft * cb;
        double displacementT = volumeM3 * waterDensity / 1000.0;
        double error = ((double)req.TargetDisplacementT - displacementT) / (double)req.TargetDisplacementT;

        // Check LOA constraint
        double estimatedLoa = lpp * 1.05;
        if (req.MaxLoaM.HasValue && estimatedLoa > (double)req.MaxLoaM.Value)
        {
            flags.Add("loa_exceeded");
        }

        // Add method used flag
        if (usedBrent)
        {
            flags.Add("used_brent_fallback");
        }

        _logger.LogInformation(
            "[HYBRID-CLOSURE] Converged: Lpp={Lpp:F2}m, B={B:F2}m, T={T:F2}m, Cb={Cb:F3}, Δ={Disp:F1}t, Error={Error:P2}, Method={Method}",
            lpp, beam, draft, cb, displacementT, error, usedBrent ? "Brent" : "Newton");

        return await Task.FromResult(new ClosureResult(
            LppM: (decimal)lpp,
            BeamM: (decimal)beam,
            DraftM: (decimal)draft,
            DepthM: (decimal)depth,
            Cb: (decimal)cb,
            DisplacementT: (decimal)displacementT,
            DisplacementError: (decimal)error,
            Converged: Math.Abs(error) < _options.ConvergenceTolerance,
            Iterations: iterations,
            Flags: flags
        ));
    }

    /// <summary>
    /// Return initial guess when solver cannot proceed
    /// </summary>
    private async Task<ClosureResult> ReturnInitialGuess(ClosureRequest req, List<string> flags)
    {
        double targetVolumeM3 = (double)(req.TargetDisplacementT / (req.WaterDensityKgM3 / 1000m));
        double draft = PhysicsBasedInitialGuess(req, targetVolumeM3);
        double beam = draft * (double)req.BOverT;
        double lpp = beam * (double)req.LOverB;
        double cb = (double)req.Cb;
        double depth = draft * (double)req.DOverT;

        double volumeM3 = lpp * beam * draft * cb;
        double displacementT = volumeM3 * (double)req.WaterDensityKgM3 / 1000.0;
        double error = ((double)req.TargetDisplacementT - displacementT) / (double)req.TargetDisplacementT;

        return await Task.FromResult(new ClosureResult(
            LppM: (decimal)lpp,
            BeamM: (decimal)beam,
            DraftM: (decimal)draft,
            DepthM: (decimal)depth,
            Cb: (decimal)cb,
            DisplacementT: (decimal)displacementT,
            DisplacementError: (decimal)error,
            Converged: false,
            Iterations: 0,
            Flags: flags
        ));
    }
}

