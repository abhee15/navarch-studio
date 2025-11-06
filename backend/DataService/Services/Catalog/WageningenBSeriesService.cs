using CsvHelper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace DataService.Services.Catalog;

/// <summary>
/// Wageningen B-Series propeller performance calculator
/// Uses 33-term polynomial regression from MARIN systematic series
/// Source: .plan/app-docs/templates/MLData/wageningen_coefficients.txt
/// </summary>
public class WageningenBSeriesService
{
    private List<WageningenCoefficient>? _coefficients;
    private readonly ILogger<WageningenBSeriesService> _logger;
    private readonly string _dataPath;

    public WageningenBSeriesService(
        IConfiguration configuration,
        ILogger<WageningenBSeriesService> logger)
    {
        _logger = logger;
        _dataPath = configuration["DataPath"] ?? "Data";
    }

    /// <summary>
    /// Load coefficients from CSV (call once at startup)
    /// </summary>
    public async Task LoadCoefficientsAsync(CancellationToken cancellationToken = default)
    {
        // DataPath points to hull-sizing/data, go up to app-docs then to templates
        var csvPath = Path.Combine(_dataPath, "..", "..", "templates", "MLData", "wageningen_coefficients.txt");

        if (!File.Exists(csvPath))
        {
            _logger.LogWarning("[WAGENINGEN] Coefficients file not found: {Path}", csvPath);
            return;
        }

        _logger.LogInformation("[WAGENINGEN] Loading B-series coefficients from {Path}", csvPath);

        try
        {
            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            _coefficients = csv.GetRecords<WageningenCoefficient>().ToList();

            _logger.LogInformation("[WAGENINGEN] ✅ Loaded {Count} coefficient terms", _coefficients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WAGENINGEN] Error loading coefficients");
            throw;
        }
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
        if (_coefficients == null || !_coefficients.Any())
            throw new InvalidOperationException("Wageningen coefficients not loaded. Call LoadCoefficientsAsync() first.");

        // Validate inputs
        if (J < 0 || J > 1.5)
            throw new ArgumentOutOfRangeException(nameof(J), "Advance coefficient must be 0-1.5");

        if (Z < 2 || Z > 7)
            throw new ArgumentOutOfRangeException(nameof(Z), "Number of blades must be 2-7");

        if (AeA0 < 0.3 || AeA0 > 1.05)
            throw new ArgumentOutOfRangeException(nameof(AeA0), "Blade area ratio must be 0.3-1.05");

        if (PD < 0.5 || PD > 1.4)
            throw new ArgumentOutOfRangeException(nameof(PD), "Pitch/diameter ratio must be 0.5-1.4");

        double KT = 0, KQ = 0;

        // Calculate using polynomial regression
        // KT = Σ C_KT * J^s * (AE/A0)^t * (P/D)^u * Z^v
        // KQ = Σ C_KQ * J^s * (AE/A0)^t * (P/D)^u * Z^v
        foreach (var coeff in _coefficients)
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
    /// <param name="speedMs">Ship speed in m/s</param>
    /// <param name="diameterM">Propeller diameter in meters</param>
    /// <param name="rpmRange">RPM range to search</param>
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
/// Wageningen polynomial coefficient (one term)
/// </summary>
public class WageningenCoefficient
{
    public int Term { get; set; }
    public int s { get; set; } // J exponent
    public int t { get; set; } // AE/A0 exponent
    public int u { get; set; } // P/D exponent
    public int v { get; set; } // Z exponent
    public double C_KT { get; set; } // KT coefficient
    public double C_KQ { get; set; } // KQ coefficient
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
