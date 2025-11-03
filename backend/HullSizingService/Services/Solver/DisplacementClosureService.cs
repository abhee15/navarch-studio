namespace HullSizingService.Services.Solver;

/// <summary>
/// Displacement closure using Newton-Raphson iteration
/// Solves for dimensions (Lpp, B, T, Cb) that match target displacement
/// </summary>
public class DisplacementClosureService : IDisplacementClosureService
{
    private readonly ILogger<DisplacementClosureService> _logger;
    private const decimal CONVERGENCE_TOLERANCE = 0.01m; // ±1%
    private const int MAX_ITERATIONS = 50;
    private const decimal G = 9.81m; // Gravity (m/s²)

    public DisplacementClosureService(ILogger<DisplacementClosureService> logger)
    {
        _logger = logger;
    }

    public async Task<ClosureResult> SolveAsync(ClosureRequest req, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("[CLOSURE] Starting displacement closure for target Δ={Target}t", req.TargetDisplacementT);

        var flags = new List<string>();

        // Step 1: Calculate Lwl from Froude number
        // Fn = V / sqrt(g * Lwl) → Lwl = V² / (g * Fn²)
        // But we don't have V yet. We'll estimate Lwl from L/B and target displacement
        // Start with an initial guess based on cube root of volume
        var targetVolumeM3 = req.TargetDisplacementT / req.WaterDensityKgM3 * 1000m; // kg/m³ → t/m³
        var estimatedLwl = (decimal)Math.Pow((double)(targetVolumeM3 / req.Cb), 1.0 / 3.0) * 2.5m; // Initial guess

        // Apply L/B ratio to get initial beam
        var lpp = estimatedLwl; // Lpp ≈ Lwl for initial guess
        var beam = lpp / req.LOverB;
        var draft = beam / req.BOverT;
        var cb = req.Cb;

        int iteration = 0;
        bool converged = false;
        decimal currentDisplacementT = 0;
        decimal error = 0;

        // Newton-Raphson loop
        while (iteration < MAX_ITERATIONS && !converged)
        {
            iteration++;

            // Calculate current displacement: Δ = ρ · Lpp · B · T · Cb
            var volumeM3 = lpp * beam * draft * cb;
            currentDisplacementT = volumeM3 * req.WaterDensityKgM3 / 1000m; // Convert kg to tonnes

            // Calculate error
            error = (req.TargetDisplacementT - currentDisplacementT) / req.TargetDisplacementT;

            if (Math.Abs(error) < CONVERGENCE_TOLERANCE)
            {
                converged = true;
                break;
            }

            _logger.LogTrace("[CLOSURE] Iteration {Iter}: Lpp={Lpp:F2}m, B={B:F2}m, T={T:F2}m, Cb={Cb:F3}, Δ={Disp:F1}t, Error={Error:P2}",
                iteration, lpp, beam, draft, cb, currentDisplacementT, error);

            // Adjust dimensions to reduce error
            // Priority: Beam → Draft → Cb (keep Lpp from Fn if possible)

            if (!req.KeepBOverT && beam < (req.MaxBeamM ?? decimal.MaxValue))
            {
                // Adjust beam (most effective for displacement)
                var beamAdjustment = error * beam * 0.5m; // 50% of error proportional adjustment
                var newBeam = beam + beamAdjustment;

                // Clamp to L/B band
                var minBeam = lpp / req.LOverBMax;
                var maxBeam = lpp / req.LOverBMin;
                newBeam = Math.Clamp(newBeam, minBeam, maxBeam);

                // Check constraint
                if (req.MaxBeamM.HasValue && newBeam > req.MaxBeamM.Value)
                {
                    newBeam = req.MaxBeamM.Value;
                    if (!flags.Contains("beam_constrained"))
                        flags.Add("beam_constrained");
                }

                beam = newBeam;
            }
            else if (!req.KeepBOverT)
            {
                // Adjust draft if beam is locked/constrained
                var draftAdjustment = error * draft * 0.5m;
                var newDraft = draft + draftAdjustment;

                // Clamp to B/T band
                var minDraft = beam / req.BOverTMax;
                var maxDraft = beam / req.BOverTMin;
                newDraft = Math.Clamp(newDraft, minDraft, maxDraft);

                // Check constraint
                if (req.MaxDraftM.HasValue && newDraft > req.MaxDraftM.Value)
                {
                    newDraft = req.MaxDraftM.Value;
                    if (!flags.Contains("draft_constrained"))
                        flags.Add("draft_constrained");
                }

                draft = newDraft;
            }
            else if (!req.KeepCb)
            {
                // Last resort: adjust Cb
                var cbAdjustment = error * cb * 0.3m; // Smaller adjustment for Cb
                var newCb = cb + cbAdjustment;
                newCb = Math.Clamp(newCb, req.CbMin, req.CbMax);
                cb = newCb;
            }
            else
            {
                // All parameters locked, can't converge
                flags.Add("all_locked_cannot_converge");
                break;
            }

            // Recalculate B/T ratio after adjustments
            if (!req.KeepBOverT)
            {
                draft = beam / req.BOverT;

                if (req.MaxDraftM.HasValue && draft > req.MaxDraftM.Value)
                {
                    draft = req.MaxDraftM.Value;
                    if (!flags.Contains("draft_constrained"))
                        flags.Add("draft_constrained");
                }
            }
        }

        if (!converged && iteration >= MAX_ITERATIONS)
        {
            flags.Add("no_convergence");
            _logger.LogWarning("[CLOSURE] Failed to converge after {MaxIter} iterations, final error={Error:P2}",
                MAX_ITERATIONS, error);
        }
        else
        {
            _logger.LogInformation("[CLOSURE] Converged in {Iter} iterations: Lpp={Lpp:F2}m, B={B:F2}m, T={T:F2}m, Cb={Cb:F3}, Δ={Disp:F1}t",
                iteration, lpp, beam, draft, cb, currentDisplacementT);
        }

        // Calculate depth from D/T ratio
        var depth = draft * req.DOverT;

        // Check LOA constraint (Lpp * 1.05 for typical bow/stern overhangs)
        var estimatedLoa = lpp * 1.05m;
        if (req.MaxLoaM.HasValue && estimatedLoa > req.MaxLoaM.Value)
        {
            flags.Add("loa_exceeded");
        }

        return await Task.FromResult(new ClosureResult(
            LppM: lpp,
            BeamM: beam,
            DraftM: draft,
            DepthM: depth,
            Cb: cb,
            DisplacementT: currentDisplacementT,
            DisplacementError: error,
            Converged: converged,
            Iterations: iteration,
            Flags: flags
        ));
    }
}

