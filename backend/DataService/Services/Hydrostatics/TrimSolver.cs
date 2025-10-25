using DataService.Data;
using Microsoft.EntityFrameworkCore;
using Shared.DTOs;

namespace DataService.Services.Hydrostatics;

/// <summary>
/// Implementation of trim solver using Newton-Raphson method
/// Solves for equilibrium trim condition (T_AP, T_FP) given target displacement
/// </summary>
public class TrimSolver : ITrimSolver
{
    private readonly IHydroCalculator _hydroCalculator;
    private readonly DataDbContext _context;
    private readonly ILogger<TrimSolver> _logger;

    public TrimSolver(
        IHydroCalculator hydroCalculator,
        DataDbContext context,
        ILogger<TrimSolver> logger)
    {
        _hydroCalculator = hydroCalculator;
        _context = context;
        _logger = logger;
    }

    public async Task<TrimSolutionDto> SolveForDisplacementAsync(
        Guid vesselId,
        Guid? loadcaseId,
        decimal targetDisplacement,
        decimal initialDraftFwd,
        decimal initialDraftAft,
        int maxIterations = 20,
        decimal tolerance = 100m,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting trim solver for vessel {VesselId}, target displacement {Target} kg",
            vesselId, targetDisplacement);

        // Get vessel for validation
        var vessel = await _context.Vessels
            .FirstOrDefaultAsync(v => v.Id == vesselId && v.DeletedAt == null, cancellationToken);

        if (vessel == null)
        {
            throw new KeyNotFoundException($"Vessel {vesselId} not found");
        }

        // Validate initial drafts
        if (initialDraftFwd <= 0 || initialDraftAft <= 0)
        {
            throw new ArgumentException("Initial drafts must be positive");
        }

        if (targetDisplacement <= 0)
        {
            throw new ArgumentException("Target displacement must be positive");
        }

        // Newton-Raphson iteration variables
        decimal draftFP = initialDraftFwd;  // Forward Perpendicular
        decimal draftAP = initialDraftAft;  // Aft Perpendicular
        int iteration = 0;
        bool converged = false;
        decimal error = 0;

        while (iteration < maxIterations && !converged)
        {
            iteration++;

            // Compute displacement at current drafts (mean draft for simplified approach)
            decimal meanDraft = (draftFP + draftAP) / 2.0m;
            var hydroResult = await _hydroCalculator.ComputeAtDraftAsync(
                vesselId, loadcaseId, meanDraft, cancellationToken);

            // Calculate error (difference from target)
            error = hydroResult.DispWeight - targetDisplacement;

            // Log iteration
            _logger.LogDebug(
                "Iteration {Iter}: T_FP={TFP:F3}m, T_AP={TAP:F3}m, T_mean={Tmean:F3}m, Disp={Disp:F0}kg, Error={Error:F0}kg",
                iteration, draftFP, draftAP, meanDraft, hydroResult.DispWeight, error);

            // Check convergence
            if (Math.Abs(error) < tolerance)
            {
                converged = true;
                _logger.LogInformation(
                    "Converged in {Iter} iterations: T_FP={TFP:F3}m, T_AP={TAP:F3}m, Error={Error:F0}kg",
                    iteration, draftFP, draftAP, error);

                // Calculate trim angle (in meters)
                decimal trimAngle = draftAP - draftFP;

                // Calculate LCF (Longitudinal Center of Floatation) from hydro result
                // For simplified approach, use LCB as approximation
                decimal lcf = hydroResult.LCBx;

                // Calculate MTC (Moment to Change Trim by 1cm)
                // MTC = (rho * g * I_l) / (100 * Lpp)
                // Using approximate formula: MTC ≈ (Disp * BML) / (100 * Lpp)
                decimal mtc = (hydroResult.DispWeight * hydroResult.BMl) / (100m * vessel.Lpp);

                return new TrimSolutionDto
                {
                    TargetDisplacement = targetDisplacement,
                    DraftAP = draftAP,
                    DraftFP = draftFP,
                    MeanDraft = meanDraft,
                    TrimAngle = trimAngle,
                    LCF = lcf,
                    MTC = mtc,
                    Converged = true,
                    Iterations = iteration
                };
            }

            // Newton-Raphson update: compute derivative (finite difference)
            decimal delta = 0.01m; // 1cm perturbation
            var hydroResultPerturbed = await _hydroCalculator.ComputeAtDraftAsync(
                vesselId, loadcaseId, meanDraft + delta, cancellationToken);

            decimal derivative = (hydroResultPerturbed.DispWeight - hydroResult.DispWeight) / delta;

            if (Math.Abs(derivative) < 0.01m)
            {
                _logger.LogWarning("Derivative too small, may not converge");
                derivative = 1000m; // Fallback to avoid division by zero
            }

            // Update mean draft using Newton-Raphson: x_new = x_old - f(x) / f'(x)
            decimal draftCorrection = error / derivative;
            meanDraft -= draftCorrection;

            // Keep trim angle approximately constant (simplified approach)
            // In reality, we'd solve a 2D system for both T_FP and T_AP
            decimal trim = draftAP - draftFP;
            draftFP = meanDraft - trim / 2.0m;
            draftAP = meanDraft + trim / 2.0m;

            // Clamp to reasonable bounds
            draftFP = Math.Max(0.1m, Math.Min(draftFP, vessel.DesignDraft * 2m));
            draftAP = Math.Max(0.1m, Math.Min(draftAP, vessel.DesignDraft * 2m));
        }

        // Failed to converge
        _logger.LogWarning(
            "Failed to converge after {Iter} iterations. Error: {Error:F0}kg",
            iteration, error);

        decimal finalMeanDraft = (draftFP + draftAP) / 2.0m;
        var finalHydroResult = await _hydroCalculator.ComputeAtDraftAsync(
            vesselId, loadcaseId, finalMeanDraft, cancellationToken);

        decimal finalTrimAngle = draftAP - draftFP;
        decimal finalLcf = finalHydroResult.LCBx;
        decimal finalMtc = (finalHydroResult.DispWeight * finalHydroResult.BMl) / (100m * vessel.Lpp);

        return new TrimSolutionDto
        {
            TargetDisplacement = targetDisplacement,
            DraftAP = draftAP,
            DraftFP = draftFP,
            MeanDraft = finalMeanDraft,
            TrimAngle = finalTrimAngle,
            LCF = finalLcf,
            MTC = finalMtc,
            Converged = false,
            Iterations = iteration
        };
    }

    public async Task<bool> IsDisplacementAchievableAsync(
        Guid vesselId,
        decimal targetDisplacement,
        CancellationToken cancellationToken = default)
    {
        // Get vessel
        var vessel = await _context.Vessels
            .FirstOrDefaultAsync(v => v.Id == vesselId && v.DeletedAt == null, cancellationToken);

        if (vessel == null)
        {
            return false;
        }

        // Check maximum displacement at design draft
        var maxHydro = await _hydroCalculator.ComputeAtDraftAsync(
            vesselId, null, vessel.DesignDraft, cancellationToken);

        // Target should be less than max displacement
        return targetDisplacement <= maxHydro.DispWeight;
    }
}

