using DataService.Data;
using DataService.Services.Hydrostatics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NavArch.Shared.DTOs;
using NavArch.Shared.Models;

namespace DataService.Services.Seakeeping;

/// <summary>
/// Motion analysis service for vessel response in irregular seas.
/// </summary>
public class MotionAnalysisService : IMotionAnalysisService
{
    private readonly DataDbContext _context;
    private readonly IWaveSpectrumService _spectrumService;
    private readonly IIntegrationEngine _integrationEngine;
    private readonly IExceedanceCalculator _exceedanceCalculator;
    private readonly ILogger<MotionAnalysisService> _logger;

    public MotionAnalysisService(
        DataDbContext context,
        IWaveSpectrumService spectrumService,
        IIntegrationEngine integrationEngine,
        IExceedanceCalculator exceedanceCalculator,
        ILogger<MotionAnalysisService> logger)
    {
        _context = context;
        _spectrumService = spectrumService;
        _integrationEngine = integrationEngine;
        _exceedanceCalculator = exceedanceCalculator;
        _logger = logger;
    }

    public async Task<MotionResponseDto> AnalyzeMotionAsync(
        Guid raoResultId,
        SeaStateDto seaState,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Analyzing motion for RAO {RaoId} in sea state Hs={Hs}m, Tp={Tp}s",
            raoResultId, seaState.SignificantHeight, seaState.PeakPeriod
        );

        var raos = await _context.RaoResults
            .FirstOrDefaultAsync(r => r.Id == raoResultId, cancellationToken);

        if (raos == null)
        {
            throw new ArgumentException($"RAO result {raoResultId} not found");
        }

        // 1. Compute wave spectrum S(ω)
        var waveSpectrum = _spectrumService.ComputeSpectrum(seaState, raos.Frequency);

        // 2. Compute response spectra: Sₓ(ω) = |RAO(ω)|² × S(ω)
        var heaveSpectrum = ComputeResponseSpectrum(raos.HeaveRao, waveSpectrum);
        var pitchSpectrum = ComputeResponseSpectrum(raos.PitchRao, waveSpectrum);
        var rollSpectrum = ComputeResponseSpectrum(raos.RollRao, waveSpectrum);

        // 3. Compute spectral moments
        var heaveM0 = ComputeSpectralMoment(heaveSpectrum, raos.Frequency, 0);
        var heaveM2 = ComputeSpectralMoment(heaveSpectrum, raos.Frequency, 2);

        var pitchM0 = ComputeSpectralMoment(pitchSpectrum, raos.Frequency, 0);
        var pitchM2 = ComputeSpectralMoment(pitchSpectrum, raos.Frequency, 2);

        var rollM0 = ComputeSpectralMoment(rollSpectrum, raos.Frequency, 0);
        var rollM2 = ComputeSpectralMoment(rollSpectrum, raos.Frequency, 2);

        // 4. Significant responses (Rayleigh distribution): x₁/₃ = 4√m₀
        var significantHeave = 4.0 * Math.Sqrt(heaveM0);
        var significantPitch = 4.0 * Math.Sqrt(pitchM0) * (180.0 / Math.PI); // Convert to degrees
        var significantRoll = 4.0 * Math.Sqrt(rollM0) * (180.0 / Math.PI);

        // 5. Mean periods: Tₘ = 2π√(m₀/m₂)
        var heaveMeanPeriod = heaveM2 > 0 ? 2.0 * Math.PI * Math.Sqrt(heaveM0 / heaveM2) : 0;
        var pitchMeanPeriod = pitchM2 > 0 ? 2.0 * Math.PI * Math.Sqrt(pitchM0 / pitchM2) : 0;
        var rollMeanPeriod = rollM2 > 0 ? 2.0 * Math.PI * Math.Sqrt(rollM0 / rollM2) : 0;

        // 6. Compute exceedance probabilities for standard thresholds
        var heaveExceedance = _exceedanceCalculator.CalculateExceedanceProbabilities(
            significantHeave,
            new[] { 1.0, 2.0, 3.0 }
        );

        var pitchExceedance = _exceedanceCalculator.CalculateExceedanceProbabilities(
            significantPitch,
            new[] { 3.0, 5.0, 7.0 }
        );

        var rollExceedance = _exceedanceCalculator.CalculateExceedanceProbabilities(
            significantRoll,
            new[] { 5.0, 10.0, 15.0 }
        );

        // Flatten to single dictionary for DTO
        var exceedances = new Dictionary<string, double>
        {
            ["heave1m"] = heaveExceedance[1.0],
            ["heave2m"] = heaveExceedance[2.0],
            ["heave3m"] = heaveExceedance[3.0],
            ["pitch3deg"] = pitchExceedance[3.0],
            ["pitch5deg"] = pitchExceedance[5.0],
            ["pitch7deg"] = pitchExceedance[7.0],
            ["roll5deg"] = rollExceedance[5.0],
            ["roll10deg"] = rollExceedance[10.0],
            ["roll15deg"] = rollExceedance[15.0]
        };

        // 7. Save to database
        var response = new MotionResponse
        {
            Id = Guid.NewGuid(),
            RaoResultId = raoResultId,
            SeaStateHs = seaState.SignificantHeight,
            SeaStateTp = seaState.PeakPeriod,
            SeaStateHeading = seaState.Heading,
            SeaStateSpectrum = seaState.Spectrum,
            SeaStateGamma = seaState.Gamma == 0 ? 3.3 : seaState.Gamma,
            SignificantHeave = significantHeave,
            SignificantPitch = significantPitch,
            SignificantRoll = significantRoll,
            HeaveMeanPeriod = heaveMeanPeriod,
            PitchMeanPeriod = pitchMeanPeriod,
            RollMeanPeriod = rollMeanPeriod,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.MotionResponses.AddAsync(response, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Motion analysis complete: Significant heave={Heave:F2}m, pitch={Pitch:F2}°, roll={Roll:F2}°",
            significantHeave, significantPitch, significantRoll
        );

        return new MotionResponseDto(
            response.Id,
            response.RaoResultId,
            seaState,
            response.SignificantHeave,
            response.SignificantPitch,
            response.SignificantRoll,
            new Dictionary<string, double>
            {
                ["heave"] = heaveMeanPeriod,
                ["pitch"] = pitchMeanPeriod,
                ["roll"] = rollMeanPeriod
            },
            exceedances,
            response.CreatedAt
        );
    }

    /// <summary>
    /// Compute response spectrum: Sₓ(ω) = |RAO(ω)|² × S(ω)
    /// </summary>
    private double[] ComputeResponseSpectrum(double[] rao, double[] waveSpectrum)
    {
        var responseSpectrum = new double[rao.Length];

        for (int i = 0; i < rao.Length; i++)
        {
            responseSpectrum[i] = Math.Pow(rao[i], 2) * waveSpectrum[i];
        }

        return responseSpectrum;
    }

    /// <summary>
    /// Compute spectral moment: mₙ = ∫ ωⁿ S(ω) dω
    /// </summary>
    private double ComputeSpectralMoment(double[] spectrum, double[] frequencies, int n)
    {
        // Prepare data for integration
        var integrand = new double[spectrum.Length];

        for (int i = 0; i < spectrum.Length; i++)
        {
            integrand[i] = Math.Pow(frequencies[i], n) * spectrum[i];
        }

        // Convert to List<decimal> for integration engine
        var freqList = frequencies.Select(f => (decimal)f).ToList();
        var integrandList = integrand.Select(v => (decimal)v).ToList();

        return (double)_integrationEngine.Integrate(freqList, integrandList);
    }
}
