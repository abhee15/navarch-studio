namespace HullSizingService.Services.Solver;

/// <summary>
/// Quick stability screening for preliminary design
/// Estimates GMt, KB, BMt based on simplified formulas
/// </summary>
public class StabilityScreenService : IStabilityScreenService
{
    private readonly ILogger<StabilityScreenService> _logger;
    private const decimal G = 9.81m;

    public StabilityScreenService(ILogger<StabilityScreenService> logger)
    {
        _logger = logger;
    }

    public async Task<StabilityResult> ScreenAsync(StabilityRequest req, CancellationToken cancellationToken = default)
    {
        var flags = new List<string>();

        // 1. Vertical center of buoyancy (KB)
        // KB ≈ 0.53 * T for box-like hulls (Cb > 0.7)
        // KB ≈ 0.56 * T for V-shaped hulls (Cb < 0.7)
        var kb = req.Cb > 0.7m ? 0.53m * req.DraftM : 0.56m * req.DraftM;

        // 2. Waterplane inertia: Iwp = (1/12) * Lwl * B³ * Cwp
        var iwp = (1.0m / 12.0m) * req.LwlM * (decimal)Math.Pow((double)req.BeamM, 3.0) * req.Cwp;

        // 3. Displacement volume: ∇ = Δ / ρ
        var volumeM3 = req.DisplacementT / 1.025m; // Assume seawater density 1.025 t/m³

        // 4. Transverse metacentric radius: BMt = Iwp / ∇
        var bmt = iwp / volumeM3;

        // 5. Vertical center of gravity (KG) - Estimate based on vessel type
        // These are rough estimates for preliminary design
        var kg = req.VesselType.ToLower() switch
        {
            "container" => 0.65m * req.DepthM, // High CG due to deck containers
            "tanker" => 0.55m * req.DepthM,    // Low CG (cargo in bottom tanks)
            "bulker" or "bulk" => 0.60m * req.DepthM,
            "fishing" => 0.62m * req.DepthM,
            "general_cargo" => 0.63m * req.DepthM,
            _ => 0.60m * req.DepthM // Default
        };

        // 6. Transverse metacentric height: GMt = KB + BMt - KG
        var gmt = kb + bmt - kg;

        // 7. Roll period estimate: T_roll = 2π * kxx / sqrt(g * GMt)
        // where kxx ≈ 0.35 * B for typical ships
        decimal tRoll;
        if (gmt <= 0.01m)
        {
            // Negative or near-zero GM → unstable, roll period meaningless
            tRoll = 999m; // Sentinel value for unstable
        }
        else
        {
            var kxx = 0.35m * req.BeamM;
            tRoll = 2.0m * (decimal)Math.PI * kxx / (decimal)Math.Sqrt((double)(G * gmt));
        }

        // 8. Flag stability issues
        if (gmt < 0.5m)
        {
            flags.Add("critically_low_gm");
            _logger.LogWarning("[STABILITY] Critically low GMt={Gmt:F2}m (< 0.5m)", gmt);
        }
        else if (gmt < 1.0m)
        {
            flags.Add("low_gm");
        }
        else if (gmt > 3.0m)
        {
            flags.Add("high_gm_stiff");
        }

        if (tRoll < 8.0m)
        {
            flags.Add("fast_roll_period");
        }
        else if (tRoll > 20.0m)
        {
            flags.Add("slow_roll_period");
        }

        _logger.LogDebug("[STABILITY] KB={Kb:F2}m, BMt={Bmt:F2}m, KG(est)={Kg:F2}m, GMt={Gmt:F2}m, T_roll={Troll:F1}s",
            kb, bmt, kg, gmt, tRoll);

        return await Task.FromResult(new StabilityResult(
            KbM: kb,
            BmtM: bmt,
            KgEstM: kg,
            GmEstM: gmt,
            TRollSeconds: tRoll,
            Flags: flags
        ));
    }
}

