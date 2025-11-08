using Microsoft.Extensions.Logging;
using NavArch.Shared.DTOs;

namespace DataService.Services.Seakeeping;

/// <summary>
/// Wave spectrum service implementing JONSWAP and PM spectra.
/// </summary>
public class WaveSpectrumService : IWaveSpectrumService
{
    private readonly ILogger<WaveSpectrumService> _logger;

    public WaveSpectrumService(ILogger<WaveSpectrumService> logger)
    {
        _logger = logger;
    }

    public double[] ComputeSpectrum(SeaStateDto seaState, double[] frequencies)
    {
        _logger.LogInformation(
            "Computing {Spectrum} spectrum for Hs={Hs}m, Tp={Tp}s",
            seaState.Spectrum, seaState.SignificantHeight, seaState.PeakPeriod
        );

        if (seaState.Spectrum.Equals("JONSWAP", StringComparison.OrdinalIgnoreCase))
        {
            return ComputeJonswapSpectrum(seaState, frequencies);
        }
        else if (seaState.Spectrum.Equals("PM", StringComparison.OrdinalIgnoreCase))
        {
            return ComputePiersonMoskowitzSpectrum(seaState, frequencies);
        }
        else
        {
            throw new ArgumentException($"Unknown spectrum type: {seaState.Spectrum}");
        }
    }

    /// <summary>
    /// Compute JONSWAP (Joint North Sea Wave Project) spectrum.
    /// S(ω) = (αg²/ω⁵) × exp(-1.25(ωₚ/ω)⁴) × γ^exp(-(ω-ωₚ)²/(2σ²ωₚ²))
    /// </summary>
    private double[] ComputeJonswapSpectrum(SeaStateDto seaState, double[] frequencies)
    {
        const double g = 9.81; // Gravity (m/s²)
        var spectrum = new double[frequencies.Length];

        var Hs = seaState.SignificantHeight;
        var Tp = seaState.PeakPeriod;
        var gamma = seaState.Gamma == 0 ? 3.3 : seaState.Gamma; // Default γ = 3.3

        // Peak frequency
        var omegaP = 2.0 * Math.PI / Tp;

        // Phillips constant α from significant height
        var alpha = ComputeAlpha(Hs, omegaP, g);

        for (int i = 0; i < frequencies.Length; i++)
        {
            var omega = frequencies[i];

            if (omega <= 0)
            {
                spectrum[i] = 0;
                continue;
            }

            // Pierson-Moskowitz base spectrum
            var ratio = omegaP / omega;
            var S_pm = (alpha * Math.Pow(g, 2) / Math.Pow(omega, 5))
                       * Math.Exp(-1.25 * Math.Pow(ratio, 4));

            // Peak enhancement factor
            var sigma = omega <= omegaP ? 0.07 : 0.09;
            var exponent = -Math.Pow(omega - omegaP, 2) / (2 * Math.Pow(sigma * omegaP, 2));
            var peakFactor = Math.Pow(gamma, Math.Exp(exponent));

            spectrum[i] = S_pm * peakFactor;
        }

        return spectrum;
    }

    /// <summary>
    /// Compute Pierson-Moskowitz spectrum (fully developed sea).
    /// PM is a special case of JONSWAP with γ = 1.
    /// </summary>
    private double[] ComputePiersonMoskowitzSpectrum(SeaStateDto seaState, double[] frequencies)
    {
        const double g = 9.81;
        var spectrum = new double[frequencies.Length];

        var Hs = seaState.SignificantHeight;
        var Tp = seaState.PeakPeriod;

        var omegaP = 2.0 * Math.PI / Tp;
        var alpha = ComputeAlpha(Hs, omegaP, g);

        for (int i = 0; i < frequencies.Length; i++)
        {
            var omega = frequencies[i];

            if (omega <= 0)
            {
                spectrum[i] = 0;
                continue;
            }

            var ratio = omegaP / omega;
            spectrum[i] = (alpha * Math.Pow(g, 2) / Math.Pow(omega, 5))
                         * Math.Exp(-1.25 * Math.Pow(ratio, 4));
        }

        return spectrum;
    }

    /// <summary>
    /// Compute Phillips constant α from significant wave height.
    /// α = (5/16) * Hs² * ωₚ⁴ / g²
    /// </summary>
    private double ComputeAlpha(double Hs, double omegaP, double g)
    {
        return (5.0 / 16.0) * Math.Pow(Hs, 2) * Math.Pow(omegaP, 4) / Math.Pow(g, 2);
    }
}
