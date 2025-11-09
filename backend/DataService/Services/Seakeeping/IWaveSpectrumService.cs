using NavArch.Shared.DTOs;

namespace DataService.Services.Seakeeping;

/// <summary>
/// Wave spectrum service for JONSWAP and Pierson-Moskowitz spectra.
/// </summary>
public interface IWaveSpectrumService
{
    /// <summary>
    /// Compute wave energy spectrum for a given sea state.
    /// </summary>
    /// <param name="seaState">Sea state parameters (Hs, Tp, spectrum type)</param>
    /// <param name="frequencies">Frequency array (rad/s)</param>
    /// <returns>Energy spectrum S(ω) in m²s</returns>
    double[] ComputeSpectrum(SeaStateDto seaState, double[] frequencies);
}

