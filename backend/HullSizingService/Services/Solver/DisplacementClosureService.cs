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

        // Step 1: Better initial guess using cube root scaling
        // ∇ = Δ / ρ
        var targetVolumeM3 = req.TargetDisplacementT / (req.WaterDensityKgM3 / 1000m);

        // Volume = L * B * T * Cb
        // Assume: L = L/B * B, T = B / (B/T)
        // Volume = (L/B * B) * B * (B / (B/T)) * Cb
        //        = L/B * B³ / (B/T) * Cb
        // B³ = Volume * (B/T) / (L/B * Cb)
        // B = (Volume * (B/T) / (L/B * Cb))^(1/3)

        var beam = (decimal)Math.Pow((double)(targetVolumeM3 * req.BOverT / (req.LOverB * req.Cb)), 1.0 / 3.0);
        var lpp = beam * req.LOverB;
        var draft = beam / req.BOverT;
        var cb = req.Cb;

        int iteration = 0;
        bool converged = false;
        decimal currentDisplacementT = 0;
        decimal error = 0;

        // Check for over-constrained problem before starting
        if (req.KeepBOverT && req.KeepCb && req.KeepLOverB)
        {
            flags.Add("all_locked_cannot_converge");
            _logger.LogWarning("[CLOSURE] All parameters locked - cannot adjust to meet target displacement");

            // Return initial guess values (no convergence possible)
            var initialVol = lpp * beam * draft * cb;
            currentDisplacementT = initialVol * req.WaterDensityKgM3 / 1000m;
            error = (req.TargetDisplacementT - currentDisplacementT) / req.TargetDisplacementT;

            var initialDepth = draft * req.DOverT;
            return await Task.FromResult(new ClosureResult(
                LppM: lpp,
                BeamM: beam,
                DraftM: draft,
                DepthM: initialDepth,
                Cb: cb,
                DisplacementT: currentDisplacementT,
                DisplacementError: error,
                Converged: false,
                Iterations: 0,
                Flags: flags
            ));
        }

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
            // Use aggressive adjustment factors for faster convergence
            var adjustmentFactor = Math.Abs(error) > 0.1m ? 0.8m : 0.6m; // Larger steps for large errors

            if (!req.KeepBOverT)
            {
                // Adjust beam (most effective for displacement, scales as B³)
                var beamAdjustment = error * beam * adjustmentFactor / 3.0m; // Divide by 3 since volume ∝ B³
                var newBeam = beam + beamAdjustment;

                // Clamp to L/B band
                var minBeam = lpp / req.LOverBMax;
                var maxBeam = lpp / req.LOverBMin;
                newBeam = Math.Clamp(newBeam, minBeam, maxBeam);

                // Check constraint and clamp
                if (req.MaxBeamM.HasValue)
                {
                    if (newBeam > req.MaxBeamM.Value)
                    {
                        newBeam = req.MaxBeamM.Value;
                        if (!flags.Contains("beam_constrained"))
                            flags.Add("beam_constrained");
                    }
                }

                beam = newBeam;

                // Update draft to maintain B/T ratio
                draft = beam / req.BOverT;

                // Check draft constraint
                if (req.MaxDraftM.HasValue && draft > req.MaxDraftM.Value)
                {
                    draft = req.MaxDraftM.Value;
                    if (!flags.Contains("draft_constrained"))
                        flags.Add("draft_constrained");

                    // Recalculate beam from constrained draft
                    beam = draft * req.BOverT;

                    // Re-check beam constraint after recalculating from draft
                    if (req.MaxBeamM.HasValue && beam > req.MaxBeamM.Value)
                    {
                        beam = req.MaxBeamM.Value;
                        if (!flags.Contains("beam_constrained"))
                            flags.Add("beam_constrained");

                        // Both beam and draft are now at max - recalculate draft to match
                        draft = beam / req.BOverT;
                    }
                }
            }
            else if (!req.KeepCb)
            {
                // Adjust Cb if beam/draft are locked
                var cbAdjustment = error * cb * adjustmentFactor;
                var newCb = cb + cbAdjustment;
                cb = Math.Clamp(newCb, req.CbMin, req.CbMax);
            }
            else
            {
                // All parameters locked, can't converge
                flags.Add("all_locked_cannot_converge");
                break;
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

        // Final constraint enforcement (ensure we never exceed hard limits)
        if (req.MaxBeamM.HasValue && beam > req.MaxBeamM.Value)
        {
            beam = req.MaxBeamM.Value;
            if (!flags.Contains("beam_constrained"))
                flags.Add("beam_constrained");
        }

        if (req.MaxDraftM.HasValue && draft > req.MaxDraftM.Value)
        {
            draft = req.MaxDraftM.Value;
            if (!flags.Contains("draft_constrained"))
                flags.Add("draft_constrained");
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

