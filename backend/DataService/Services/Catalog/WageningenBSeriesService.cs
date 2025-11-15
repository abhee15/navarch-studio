using Microsoft.Extensions.Logging;
using Shared.Constants;

namespace DataService.Services.Catalog;

/// <summary>
/// Wageningen B-Series propeller performance calculator
/// Uses 33-term polynomial regression from MARIN systematic series (Oosterveld and van Oossanen, 1975)
/// Coefficients are hardcoded constants (published reference data that never changes)
/// </summary>
public class WageningenBSeriesService
{
    private readonly ILogger<WageningenBSeriesService> _logger;

    public WageningenBSeriesService(ILogger<WageningenBSeriesService> logger)
    {
        _logger = logger;
        _logger.LogInformation("[WAGENINGEN] Service initialized with {Count} hardcoded coefficients",
            WageningenConstants.Coefficients.Length);
    }

    /// <summary>
    /// Calculate propeller performance at given operating point
    /// </summary>
    /// <param name="J">Advance coefficient (0-1.5)</param>
    /// <param name="Z">Number of blades (2-7)</param>
    /// <param name="AeA0">Blade area ratio (0.3-1.05)</param>
    /// <param name="PD">Pitch/diameter ratio (0.5-1.4)</param>
    /// <returns>Thrust coefficient, torque coefficient, efficiency</returns>
    public PropellerPerformance CalculatePerformance(
        double J,
        int Z,
        double AeA0,
        double PD)
    {
        // Validate inputs
        if (J < WageningenConstants.ParameterRanges.J_Min || J > WageningenConstants.ParameterRanges.J_Max)
            throw new ArgumentOutOfRangeException(nameof(J), $"Advance coefficient must be {WageningenConstants.ParameterRanges.J_Min}-{WageningenConstants.ParameterRanges.J_Max}");

        if (Z < WageningenConstants.ParameterRanges.Z_Min || Z > WageningenConstants.ParameterRanges.Z_Max)
            throw new ArgumentOutOfRangeException(nameof(Z), $"Number of blades must be {WageningenConstants.ParameterRanges.Z_Min}-{WageningenConstants.ParameterRanges.Z_Max}");

        if (AeA0 < WageningenConstants.ParameterRanges.AeA0_Min || AeA0 > WageningenConstants.ParameterRanges.AeA0_Max)
            throw new ArgumentOutOfRangeException(nameof(AeA0), $"Blade area ratio must be {WageningenConstants.ParameterRanges.AeA0_Min}-{WageningenConstants.ParameterRanges.AeA0_Max}");

        if (PD < WageningenConstants.ParameterRanges.PD_Min || PD > WageningenConstants.ParameterRanges.PD_Max)
            throw new ArgumentOutOfRangeException(nameof(PD), $"Pitch/diameter ratio must be {WageningenConstants.ParameterRanges.PD_Min}-{WageningenConstants.ParameterRanges.PD_Max}");

        double KT = 0, KQ = 0;

        // Calculate using polynomial regression
        // KT = Σ C_KT * J^s * (AE/A0)^t * (P/D)^u * Z^v
        // KQ = Σ C_KQ * J^s * (AE/A0)^t * (P/D)^u * Z^v
        foreach (var coeff in WageningenConstants.Coefficients)
        {
            var term_KT = coeff.C_KT * Math.Pow(J, coeff.s) * Math.Pow(AeA0, coeff.t)
                         * Math.Pow(PD, coeff.u) * Math.Pow(Z, coeff.v);
            KT += term_KT;

            var term_KQ = coeff.C_KQ * Math.Pow(J, coeff.s) * Math.Pow(AeA0, coeff.t)
                         * Math.Pow(PD, coeff.u) * Math.Pow(Z, coeff.v);
            KQ += term_KQ;
        }

        // Calculate efficiency: η = (J / 2π) * (KT / KQ)
        double efficiency = 0;
        if (Math.Abs(KQ) > 1e-10) // Avoid division by zero
        {
            efficiency = (J / (2 * Math.PI)) * (KT / KQ);
        }

        // Clamp efficiency to valid range
        efficiency = Math.Clamp(efficiency, 0, 1);

        _logger.LogDebug(
            "[WAGENINGEN] J={J:F3}, Z={Z}, AE/A0={AeA0:F2}, P/D={PD:F2} → KT={KT:F4}, KQ={KQ:F5}, η={Eta:F3}",
            J, Z, AeA0, PD, KT, KQ, efficiency);

        return new PropellerPerformance
        {
            AdvanceCoefficient = J,
            ThrustCoefficient = KT,
            TorqueCoefficient = KQ,
            Efficiency = efficiency,
            NumberOfBlades = Z,
            BladeAreaRatio = AeA0,
            PitchDiameterRatio = PD
        };
    }

    /// <summary>
    /// Find optimal propeller for required thrust
    /// </summary>
    /// <param name="requiredThrustN">Required thrust in Newtons</param>
    /// <param name="speedMs">Speed in m/s</param>
    /// <param name="diameterM">Propeller diameter in meters</param>
    /// <param name="rpmRange">RPM search range (min, max)</param>
    /// <param name="Z">Number of blades (default: 4)</param>
    /// <param name="AeA0">Expanded area ratio (default: 0.55)</param>
    /// <param name="PD">Pitch-diameter ratio (default: 1.0)</param>
    /// <returns>Optimal operating point</returns>
    public PropellerOperatingPoint FindOptimalPoint(
        double requiredThrustN,
        double speedMs,
        double diameterM,
        (double min, double max) rpmRange,
        int Z = 4,
        double AeA0 = 0.55,
        double PD = 1.0)
    {
        const int searchSteps = 50;
        const double seawaterDensity = 1025; // kg/m³

        PropellerOperatingPoint? bestPoint = null;
        double bestEfficiency = 0;

        for (int i = 0; i <= searchSteps; i++)
        {
            var rpm = rpmRange.min + (rpmRange.max - rpmRange.min) * i / searchSteps;
            var n = rpm / 60.0; // rev/s

            // Calculate advance coefficient: J = V_a / (n * D)
            var J = speedMs / (n * diameterM);

            if (J < 0 || J > 1.5) continue; // Out of valid range

            // Calculate performance
            var perf = CalculatePerformance(J, Z, AeA0, PD);

            // Calculate actual thrust: T = KT * ρ * n² * D⁴
            var thrustN = perf.ThrustCoefficient * seawaterDensity * Math.Pow(n, 2) * Math.Pow(diameterM, 4);

            // Check if this meets thrust requirement (within 5%)
            var thrustError = Math.Abs(thrustN - requiredThrustN) / requiredThrustN;

            if (thrustError < 0.05 && perf.Efficiency > bestEfficiency)
            {
                bestEfficiency = perf.Efficiency;
                bestPoint = new PropellerOperatingPoint
                {
                    RPM = rpm,
                    ThrustN = thrustN,
                    Performance = perf,
                    DiameterM = diameterM
                };
            }
        }

        if (bestPoint == null)
        {
            throw new InvalidOperationException(
                $"Could not find valid operating point for thrust={requiredThrustN}N, speed={speedMs}m/s, D={diameterM}m");
        }

        return bestPoint;
    }
}

/// <summary>
/// Propeller performance at given operating point
/// </summary>
public class PropellerPerformance
{
    public double AdvanceCoefficient { get; set; }
    public double ThrustCoefficient { get; set; }
    public double TorqueCoefficient { get; set; }
    public double Efficiency { get; set; }
    public int NumberOfBlades { get; set; }
    public double BladeAreaRatio { get; set; }
    public double PitchDiameterRatio { get; set; }
}

/// <summary>
/// Complete propeller operating point
/// </summary>
public class PropellerOperatingPoint
{
    public double RPM { get; set; }
    public double ThrustN { get; set; }
    public double DiameterM { get; set; }
    public PropellerPerformance Performance { get; set; } = new();
}
