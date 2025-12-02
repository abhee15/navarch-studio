using Shared.Models.Sizing;

namespace HullSizingService.Services.Validation;

/// <summary>
/// Validates if dimensional constraints are feasible given the mission requirements
/// Provides early feedback before attempting hull generation
/// </summary>
public interface IConstraintFeasibilityValidator
{
    /// <summary>
    /// Check if constraints are feasible for the given mission
    /// </summary>
    Task<FeasibilityCheckResult> CheckAsync(MissionCase mission, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of constraint feasibility check
/// </summary>
public record FeasibilityCheckResult(
    bool IsFeasible,
    List<string> Warnings,
    List<string> Errors,
    Dictionary<string, ConstraintEstimate>? Estimates = null
);

/// <summary>
/// Estimated dimension vs constraint
/// </summary>
public record ConstraintEstimate(
    string Dimension,
    decimal EstimatedValue,
    decimal? ConstraintValue,
    bool IsViolated,
    string? Guidance = null
);

public class ConstraintFeasibilityValidator : IConstraintFeasibilityValidator
{
    private readonly ILogger<ConstraintFeasibilityValidator> _logger;

    public ConstraintFeasibilityValidator(ILogger<ConstraintFeasibilityValidator> logger)
    {
        _logger = logger;
    }

    public async Task<FeasibilityCheckResult> CheckAsync(MissionCase mission, CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        var errors = new List<string>();
        var estimates = new Dictionary<string, ConstraintEstimate>();

        // Step 1: Estimate required dimensions from cargo/speed
        var (estimatedLpp, estimatedBeam, estimatedDraft) = EstimateDimensionsFromMission(mission);

        // Step 2: Check each constraint
        if (mission.CapLoaM.HasValue)
        {
            var estimatedLoa = estimatedLpp * 1.05m; // LOA typically 5% more than Lpp
            var isViolated = estimatedLoa > mission.CapLoaM.Value;

            estimates["loa"] = new ConstraintEstimate(
                "Length Overall",
                estimatedLoa,
                mission.CapLoaM.Value,
                isViolated,
                isViolated ? $"Estimated LOA ({estimatedLoa:F1}m) exceeds constraint ({mission.CapLoaM.Value:F1}m). Consider reducing cargo capacity or increasing constraint." : null
            );

            if (isViolated)
            {
                errors.Add($"Max LOA constraint ({mission.CapLoaM.Value:F1}m) is too restrictive. Estimated LOA: {estimatedLoa:F1}m based on {GetCargoDescription(mission)}.");
            }
        }

        if (mission.CapBeamM.HasValue)
        {
            var isViolated = estimatedBeam > mission.CapBeamM.Value;
            var margin = mission.CapBeamM.Value - estimatedBeam;
            var marginPercent = (margin / estimatedBeam) * 100m;

            estimates["beam"] = new ConstraintEstimate(
                "Beam",
                estimatedBeam,
                mission.CapBeamM.Value,
                isViolated,
                isViolated ? $"Estimated beam ({estimatedBeam:F1}m) exceeds constraint ({mission.CapBeamM.Value:F1}m). Typical {mission.MissionType} at this capacity: {estimatedBeam:F1}m ± 10%." : null
            );

            if (isViolated)
            {
                errors.Add($"Max beam constraint ({mission.CapBeamM.Value:F1}m) is too restrictive. Estimated beam: {estimatedBeam:F1}m.");

                // Suggest alternatives
                if (mission.CapBeamM.Value <= 32.3m && estimatedBeam > 35m)
                {
                    errors.Add("Panamax beam (32.3m) too restrictive for this vessel. Try Neo-Panamax (49.0m) or remove beam constraint.");
                }
            }
            else if (marginPercent < 5m)
            {
                warnings.Add($"Beam constraint ({mission.CapBeamM.Value:F1}m) is tight (only {marginPercent:F1}% margin). Solver may struggle to find solutions.");
            }
        }

        if (mission.CapDraftM.HasValue)
        {
            var isViolated = estimatedDraft > mission.CapDraftM.Value;
            var margin = mission.CapDraftM.Value - estimatedDraft;
            var marginPercent = (margin / estimatedDraft) * 100m;

            estimates["draft"] = new ConstraintEstimate(
                "Draft",
                estimatedDraft,
                mission.CapDraftM.Value,
                isViolated,
                isViolated ? $"Estimated draft ({estimatedDraft:F1}m) exceeds constraint ({mission.CapDraftM.Value:F1}m). Reduce cargo or increase constraint." : null
            );

            if (isViolated)
            {
                errors.Add($"Max draft constraint ({mission.CapDraftM.Value:F1}m) is too restrictive. Estimated draft: {estimatedDraft:F1}m.");
            }
            else if (marginPercent < 5m)
            {
                warnings.Add($"Draft constraint ({mission.CapDraftM.Value:F1}m) is tight (only {marginPercent:F1}% margin).");
            }
        }

        // Step 3: Cross-check multiple constraints
        if (errors.Count > 1)
        {
            errors.Add("Multiple constraints are too restrictive. Consider relaxing constraints or reducing vessel size requirements.");
        }

        var isFeasible = errors.Count == 0;

        if (!isFeasible)
        {
            _logger.LogWarning(
                "[PREFLIGHT_CHECK] Constraints infeasible for mission {MissionId}. Errors: {Errors}",
                mission.Id, string.Join("; ", errors));
        }
        else if (warnings.Count > 0)
        {
            _logger.LogInformation(
                "[PREFLIGHT_CHECK] Constraints feasible but tight for mission {MissionId}. Warnings: {Warnings}",
                mission.Id, string.Join("; ", warnings));
        }

        return await Task.FromResult(new FeasibilityCheckResult(isFeasible, warnings, errors, estimates));
    }

    private (decimal Lpp, decimal Beam, decimal Draft) EstimateDimensionsFromMission(MissionCase mission)
    {
        // Rough estimation formulas based on vessel type and cargo
        decimal cargoWeight = 0m;

        // Determine cargo weight based on basis
        if (mission.CargoBasis == "teu" && mission.TeuCount.HasValue)
        {
            cargoWeight = mission.TeuCount.Value * 14m; // Assume 14t per TEU (typical for containers)
        }
        else if (mission.CargoBasis == "weight" && mission.CargoValue.HasValue)
        {
            cargoWeight = mission.CargoValue.Value;
        }
        else if (mission.CargoBasis == "volume" && mission.CargoVolumeM3.HasValue && mission.CargoDensityTPerM3.HasValue)
        {
            cargoWeight = mission.CargoVolumeM3.Value * mission.CargoDensityTPerM3.Value;
        }

        // Estimate displacement (cargo + lightship + consumables)
        // Lightship typically 30-40% of displacement for cargo ships
        decimal estimatedDisplacement = cargoWeight * 2.0m; // Rough multiplier

        // Estimate Lpp from displacement (Watson approximation)
        // Lpp ≈ K * Displacement^(1/3) where K varies by type
        decimal k = GetLppCoefficientForType(mission.MissionType);
        decimal estimatedLpp = k * (decimal)Math.Pow((double)estimatedDisplacement, 1.0 / 3.0);

        // Estimate beam from typical L/B ratio
        decimal lOverB = GetTypicalLOverBForType(mission.MissionType);
        decimal estimatedBeam = estimatedLpp / lOverB;

        // Estimate draft from typical B/T ratio
        decimal bOverT = GetTypicalBOverTForType(mission.MissionType);
        decimal estimatedDraft = estimatedBeam / bOverT;

        _logger.LogDebug(
            "[PREFLIGHT_CHECK] Estimated dimensions for {Cargo}: Lpp={Lpp:F1}m, B={Beam:F1}m, T={Draft:F1}m",
            GetCargoDescription(mission), estimatedLpp, estimatedBeam, estimatedDraft);

        return (estimatedLpp, estimatedBeam, estimatedDraft);
    }

    private decimal GetLppCoefficientForType(string? vesselType)
    {
        return vesselType?.ToLowerInvariant() switch
        {
            "container" => 7.0m,
            "bulk_carrier" => 7.5m,
            "tanker" => 7.2m,
            "general_cargo" => 6.8m,
            "cruise_vessel" => 8.5m,
            "fishing_vessel" => 5.5m,
            _ => 7.0m
        };
    }

    private decimal GetTypicalLOverBForType(string? vesselType)
    {
        return vesselType?.ToLowerInvariant() switch
        {
            "container" => 7.0m,
            "bulk_carrier" => 6.5m,
            "tanker" => 6.0m,
            "general_cargo" => 6.5m,
            "cruise_vessel" => 8.0m,
            "fishing_vessel" => 5.0m,
            _ => 6.5m
        };
    }

    private decimal GetTypicalBOverTForType(string? vesselType)
    {
        return vesselType?.ToLowerInvariant() switch
        {
            "container" => 3.2m,
            "bulk_carrier" => 2.5m,
            "tanker" => 2.2m,
            "general_cargo" => 2.8m,
            "cruise_vessel" => 3.5m,
            "fishing_vessel" => 3.0m,
            _ => 2.8m
        };
    }

    private string GetCargoDescription(MissionCase mission)
    {
        if (mission.CargoBasis == "teu" && mission.TeuCount.HasValue)
            return $"{mission.TeuCount} TEU";
        if (mission.CargoBasis == "weight" && mission.CargoValue.HasValue)
            return $"{mission.CargoValue:F0}t cargo";
        if (mission.CargoBasis == "volume" && mission.CargoVolumeM3.HasValue)
            return $"{mission.CargoVolumeM3:F0}m³ cargo";
        return "unknown cargo";
    }
}
