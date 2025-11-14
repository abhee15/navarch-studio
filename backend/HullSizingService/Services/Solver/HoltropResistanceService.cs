namespace HullSizingService.Services.Solver;

/// <summary>
/// Simplified Holtrop-Mennen resistance calculation for preliminary design
/// TODO: CUSTOM_ALGO - Replace with custom algorithm in Phase 3 (reference: SPS SName paper 2014)
/// </summary>
public class HoltropResistanceService : IResistanceService
{
    private readonly ILogger<HoltropResistanceService> _logger;
    private const decimal G = 9.81m; // Gravity (m/s²)
    private const decimal PROPULSIVE_EFFICIENCY = 0.60m; // Simplified assumption for MVP
    private const decimal SEA_MARGIN = 0.15m; // 15%
    private const decimal SERVICE_MARGIN = 0.10m; // 10%

    public HoltropResistanceService(ILogger<HoltropResistanceService> logger)
    {
        _logger = logger;
    }

    public async Task<ResistanceResult> CalculateAsync(ResistanceRequest req, CancellationToken cancellationToken = default)
    {
        // Convert speed from knots to m/s
        var speedMs = req.SpeedKn * 0.5144m;

        // Froude number: Fn = V / sqrt(g * Lwl)
        var fn = speedMs / (decimal)Math.Sqrt((double)(G * req.LwlM));

        // Reynolds number: Rn = V * Lwl / ν
        var rn = speedMs * req.LwlM / req.KinematicViscosityM2S;

        // ITTC-57 friction coefficient: Cf = 0.075 / (log₁₀(Rn) - 2)²
        var logRn = (decimal)Math.Log10((double)rn);
        var cf = 0.075m / (decimal)Math.Pow((double)(logRn - 2), 2);

        // Form factor (1+k₁) - Simplified estimate based on L/B and B/T
        // TODO: CUSTOM_ALGO - Use full Holtrop formula in Phase 3
        var lOverB = req.LppM / req.BeamM;
        var bOverT = req.BeamM / req.DraftM;
        var formFactor = 1.0m + (0.93m + 0.487118m * (decimal)Math.Pow((double)(req.Cb * bOverT / lOverB), 1.06806)) / 100m;
        formFactor = Math.Max(1.0m, formFactor); // Ensure >= 1.0

        // Wetted surface (Simplified Holtrop formula)
        var s = req.LwlM * (2 * req.DraftM + req.BeamM) * (decimal)Math.Sqrt((double)req.Cm) * (0.453m + 0.4425m * req.Cb - 0.2862m * req.Cm - 0.003467m * bOverT + 0.3696m * req.Cwp);
        if (s < 0) s = req.LwlM * req.BeamM * 2.5m; // Fallback to simple box approximation

        // Frictional resistance: Rf = 0.5 * ρ * V² * S * Cf * (1+k₁)
        var rf = 0.5m * req.WaterDensityKgM3 * speedMs * speedMs * s * cf * formFactor / 1000m; // Convert to kN

        // Wave resistance (Simplified polynomial approximation)
        // TODO: CUSTOM_ALGO - Use full Holtrop wave resistance in Phase 3
        // Reference: SPS SName paper 2014, Section 4.2
        var rw = CalculateSimplifiedWaveResistance(fn, req.Cb, req.LppM, req.BeamM, req.DraftM, req.WaterDensityKgM3);

        // Total resistance
        var totalR = rf + rw;

        // Effective horsepower: EHP = R * V (kW)
        var ehp = totalR * speedMs;

        // Shaft horsepower: SHP = EHP / η_propulsive
        var shpBase = ehp / PROPULSIVE_EFFICIENCY;

        // Apply margins
        var shpWithMargins = shpBase * (1 + SEA_MARGIN) * (1 + SERVICE_MARGIN);

        _logger.LogDebug("[RESISTANCE] Fn={Fn:F3}, Rn={Rn:E2}, Cf={Cf:F5}, Rf={Rf:F1}kN, Rw={Rw:F1}kN, EHP={Ehp:F1}kW, SHP={Shp:F1}kW",
            fn, rn, cf, rf, rw, ehp, shpWithMargins);

        return await Task.FromResult(new ResistanceResult(
            Fn: fn,
            Rn: rn,
            Cf: cf,
            FormFactor: formFactor,
            FrictionalResistanceKn: rf,
            WaveResistanceKn: rw,
            TotalResistanceKn: totalR,
            EhpKw: ehp,
            ShpKw: shpWithMargins
        ));
    }

    /// <summary>
    /// Simplified wave resistance calculation (polynomial approximation)
    /// TODO: CUSTOM_ALGO - Replace with full Holtrop-Mennen in Phase 3
    /// </summary>
    private decimal CalculateSimplifiedWaveResistance(decimal fn, decimal cb, decimal lpp, decimal beam, decimal draft, decimal rho)
    {
        // Very simplified wave resistance based on Froude number and block coefficient
        // This is NOT accurate for production - placeholder for MVP

        if (fn < 0.15m)
        {
            // Low speed regime - minimal wave resistance
            return 0.1m * lpp * beam * fn * fn;
        }
        else if (fn > 0.40m)
        {
            // High speed regime - significant wave resistance
            return 2.0m * lpp * beam * (decimal)Math.Pow((double)fn, 4.0) * (1.0m + cb);
        }
        else
        {
            // Moderate speed - polynomial approximation
            var fnFactor = (decimal)Math.Pow((double)fn, 3.5);
            var cbFactor = 1.0m + (cb - 0.65m) * 0.5m;
            return 0.8m * lpp * beam * fnFactor * cbFactor;
        }
    }
}













