using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NavArch.Shared.DTOs;
using NavArch.Shared.Models;

namespace DataService.Services.Seakeeping;

/// <summary>
/// RAO calculator using frequency-domain equations of motion.
/// </summary>
public class RaoCalculator : IRaoCalculator
{
    private readonly DataDbContext _context;
    private readonly IStripTheoryEngine _stripTheory;
    private readonly ILogger<RaoCalculator> _logger;

    public RaoCalculator(
        DataDbContext context,
        IStripTheoryEngine stripTheory,
        ILogger<RaoCalculator> logger)
    {
        _context = context;
        _stripTheory = stripTheory;
        _logger = logger;
    }

    public async Task<RaoResultDto> CalculateRaosAsync(
        Guid vesselId,
        RaoCalculationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Calculating RAOs for vessel {VesselId}, loadcase {LoadcaseId}",
            vesselId, request.LoadcaseId
        );

        // Get vessel and loadcase
        var vessel = await _context.Vessels.FindAsync(new object[] { vesselId }, cancellationToken);
        var loadcase = await _context.Loadcases.FindAsync(new object[] { request.LoadcaseId }, cancellationToken);

        if (vessel == null) throw new ArgumentException($"Vessel {vesselId} not found");
        if (loadcase == null) throw new ArgumentException($"Loadcase {request.LoadcaseId} not found");

        // Generate frequency array
        var frequencies = GenerateFrequencyArray(request.FrequencyRange);

        // Use vessel's design draft for RAO calculation
        var draft = (double)vessel.DesignDraft;

        // Get hydrodynamic coefficients from strip theory
        var coeffs = await _stripTheory.ComputeCoefficientsAsync(
            vesselId,
            draft,
            frequencies,
            cancellationToken
        );

        // Build mass and restoring matrices
        var displacement = (double)(vessel.Lpp * vessel.Beam * vessel.DesignDraft * 0.7m * loadcase.Rho); // Approximate
        var M = BuildMassMatrix(displacement, (double)vessel.Lpp, (double)vessel.Beam);
        var C = BuildRestoringMatrix((double)vessel.Beam, draft, (double)loadcase.Rho);

        // Solve for RAOs at each frequency
        var heaveRao = new double[frequencies.Length];
        var pitchRao = new double[frequencies.Length];
        var rollRao = new double[frequencies.Length];

        const double waveAmplitude = 1.0; // Unit wave amplitude for RAO

        for (int i = 0; i < frequencies.Length; i++)
        {
            var omega = frequencies[i];
            var A = coeffs.AddedMass[i];
            var B = coeffs.Damping[i];
            var F = coeffs.ExcitationForce[i];

            // Solve: [-ω²(M + A) + iωB + C] X = F
            var response = SolveFrequencyDomain(omega, M, A, B, C, F);

            heaveRao[i] = Math.Abs(response[0]) / waveAmplitude;
            pitchRao[i] = Math.Abs(response[1]) / waveAmplitude;
            rollRao[i] = Math.Abs(response[2]) / waveAmplitude;
        }

        // Save to database
        var raoResult = new RaoResult
        {
            Id = Guid.NewGuid(),
            VesselId = vesselId,
            LoadcaseId = request.LoadcaseId,
            Frequency = frequencies,
            HeaveRao = heaveRao,
            PitchRao = pitchRao,
            RollRao = rollRao,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.RaoResults.AddAsync(raoResult, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "RAO calculation complete: {FreqCount} frequencies, peak heave RAO = {PeakHeave:F3}",
            frequencies.Length, heaveRao.Max()
        );

        return new RaoResultDto(
            raoResult.Id,
            raoResult.VesselId,
            raoResult.LoadcaseId,
            raoResult.Frequency,
            raoResult.HeaveRao,
            raoResult.PitchRao,
            raoResult.RollRao,
            raoResult.CreatedAt
        );
    }

    /// <summary>
    /// Build mass matrix [M] for equations of motion.
    /// </summary>
    private double[][] BuildMassMatrix(double displacement, double lpp, double beam)
    {
        var M = new double[3][];
        for (int i = 0; i < 3; i++)
        {
            M[i] = new double[3];
        }

        M[0][0] = displacement; // Heave mass

        // Pitch moment of inertia: I_yy ≈ m * L² / 12 (approximation)
        M[1][1] = displacement * Math.Pow(lpp, 2) / 12.0;

        // Roll moment of inertia: I_xx ≈ m * B² / 12
        M[2][2] = displacement * Math.Pow(beam, 2) / 12.0;

        return M;
    }

    /// <summary>
    /// Build restoring force matrix [C] for hydrostatic stiffness.
    /// </summary>
    private double[][] BuildRestoringMatrix(double beam, double draft, double rho)
    {
        const double g = 9.81;

        var C = new double[3][];
        for (int i = 0; i < 3; i++)
        {
            C[i] = new double[3];
        }

        // Heave restoring: C33 = ρgAwp (waterplane area approximation)
        var awp = beam * draft * 0.85; // Approximate waterplane area
        C[0][0] = rho * g * awp;

        // Pitch restoring: C55 = ρg∇GM_l (approximate)
        var displacement = beam * draft * draft * 0.7 * rho;
        var gm = draft * 0.5; // Rough estimate
        C[1][1] = rho * g * displacement * gm;

        // Roll restoring: C44 = ρg∇GM_t
        C[2][2] = rho * g * displacement * gm * 1.2;

        return C;
    }

    /// <summary>
    /// Solve frequency-domain equation: [-ω²(M+A) + iωB + C]X = F
    /// Returns magnitude of complex response.
    /// </summary>
    private double[] SolveFrequencyDomain(
        double omega,
        double[][] M,
        double[][] A,
        double[][] B,
        double[][] C,
        double[] F)
    {
        var response = new double[3];

        // For each DOF (heave, pitch, roll), solve separately (uncoupled approximation)
        for (int dof = 0; dof < 3; dof++)
        {
            // Real part: -ω²(M + A) + C
            var realPart = -Math.Pow(omega, 2) * (M[dof][dof] + A[dof][dof]) + C[dof][dof];

            // Imaginary part: ωB
            var imagPart = omega * B[dof][dof];

            // Magnitude of complex impedance
            var impedanceMag = Math.Sqrt(realPart * realPart + imagPart * imagPart);

            // Response magnitude: |X| = |F| / |impedance|
            if (impedanceMag > 1e-10)
            {
                response[dof] = Math.Abs(F[dof]) / impedanceMag;
            }
        }

        return response;
    }

    public async Task<RaoResultDto?> GetRaoByIdAsync(
        Guid raoId,
        CancellationToken cancellationToken = default)
    {
        var result = await _context.RaoResults
            .Where(r => r.Id == raoId && r.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
        {
            return null;
        }

        return new RaoResultDto(
            result.Id,
            result.VesselId,
            result.LoadcaseId,
            result.Frequency,
            result.HeaveRao,
            result.PitchRao,
            result.RollRao,
            result.CreatedAt
        );
    }

    private double[] GenerateFrequencyArray(FrequencyRangeDto range)
    {
        var count = (int)Math.Ceiling((range.Max - range.Min) / range.Step) + 1;
        var frequencies = new double[count];

        for (int i = 0; i < count; i++)
        {
            frequencies[i] = range.Min + i * range.Step;
        }

        return frequencies;
    }
}
