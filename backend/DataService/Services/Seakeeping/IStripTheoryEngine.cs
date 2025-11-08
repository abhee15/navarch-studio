namespace DataService.Services.Seakeeping;

/// <summary>
/// Strip theory engine for computing hydrodynamic coefficients.
/// Calculates 2D added mass and damping coefficients at each station,
/// then integrates along ship length.
/// </summary>
public interface IStripTheoryEngine
{
    /// <summary>
    /// Compute hydrodynamic coefficients (added mass and damping) for a vessel
    /// across a range of frequencies.
    /// </summary>
    /// <param name="vesselId">Vessel ID</param>
    /// <param name="draft">Draft at which to compute coefficients (m)</param>
    /// <param name="frequencyRange">Array of frequencies (rad/s)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Hydrodynamic coefficients for each frequency</returns>
    Task<HydrodynamicCoefficients> ComputeCoefficientsAsync(
        Guid vesselId,
        double draft,
        double[] frequencyRange,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// Hydrodynamic coefficients from strip theory
/// </summary>
public class HydrodynamicCoefficients
{
    public double[] Frequency { get; set; } = Array.Empty<double>();

    // Added mass matrices (3x3) for each frequency
    public double[][][] AddedMass { get; set; } = Array.Empty<double[][]>();

    // Damping matrices (3x3) for each frequency
    public double[][][] Damping { get; set; } = Array.Empty<double[][]>();

    // Wave excitation forces (3x1) for each frequency
    public double[][] ExcitationForce { get; set; } = Array.Empty<double[]>();
}
