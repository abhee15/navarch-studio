namespace HullSizingService.Services.Solver;

/// <summary>
/// Diagnostics information collected during solver execution
/// Used to provide actionable feedback when 0 candidates are generated
/// </summary>
public class SolverDiagnostics
{
    public int TotalFamiliesConsidered { get; set; }
    public int FamiliesAfterFnFiltering { get; set; }
    public int FamiliesAfterHintsFiltering { get; set; }
    public int FamiliesFailedClosure { get; set; }
    public List<string> FailureReasons { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public decimal TargetDisplacementT { get; set; }
    public decimal? EstimatedFroudeNumber { get; set; }
    public string? MissionType { get; set; }
    public Dictionary<string, string> FailedFamilies { get; set; } = new(); // Family name -> failure reason

    /// <summary>
    /// Generate user-friendly suggestions based on failure patterns
    /// </summary>
    public void GenerateSuggestions(Shared.Models.Sizing.MissionCase mission)
    {
        // No families found at all
        if (TotalFamiliesConsidered == 0)
        {
            Suggestions.Add("No hull families are available in the system. Please contact support.");
            return;
        }

        // Filtered out by hints
        if (FamiliesAfterHintsFiltering == 0 && TotalFamiliesConsidered > 0)
        {
            Suggestions.Add("Your hull family hints filtered out all available families.");
            Suggestions.Add("Try removing family hints to allow the solver to explore all options.");
            return;
        }

        // All families failed closure
        if (FamiliesFailedClosure == FamiliesAfterHintsFiltering && FamiliesFailedClosure > 0)
        {
            Suggestions.Add($"All {FamiliesFailedClosure} hull families failed to converge to a valid design.");

            // Analyze failure reasons
            var hasBeamConstraint = FailureReasons.Any(r => r.Contains("beam_constrained"));
            var hasDraftConstraint = FailureReasons.Any(r => r.Contains("draft_constrained"));
            var hasLoaConstraint = FailureReasons.Any(r => r.Contains("loa_exceeded"));
            var hasNoConvergence = FailureReasons.Any(r => r.Contains("no_convergence"));

            // Constraint-related failures
            if (hasBeamConstraint && hasDraftConstraint)
            {
                Suggestions.Add($"Your beam limit ({mission.CapBeamM:F1}m) and draft limit ({mission.CapDraftM:F1}m) are too restrictive together.");
                if (mission.CapBeamM.HasValue)
                    Suggestions.Add($"Consider increasing max beam to {mission.CapBeamM.Value * 1.2m:F1}m.");
                if (mission.CapDraftM.HasValue)
                    Suggestions.Add($"Consider increasing max draft to {mission.CapDraftM.Value * 1.15m:F1}m.");
            }
            else if (hasBeamConstraint)
            {
                Suggestions.Add($"Maximum beam constraint ({mission.CapBeamM:F1}m) is too restrictive for this displacement.");
                if (mission.CapBeamM.HasValue)
                    Suggestions.Add($"Try increasing max beam to {mission.CapBeamM.Value * 1.3m:F1}m.");
            }
            else if (hasDraftConstraint)
            {
                Suggestions.Add($"Maximum draft constraint ({mission.CapDraftM:F1}m) is too restrictive for this displacement.");
                if (mission.CapDraftM.HasValue)
                    Suggestions.Add($"Try increasing max draft to {mission.CapDraftM.Value * 1.2m:F1}m.");
            }

            if (hasLoaConstraint)
            {
                Suggestions.Add($"Length overall constraint ({mission.CapLoaM:F1}m) may be too restrictive.");
                if (mission.CapLoaM.HasValue)
                    Suggestions.Add($"Consider increasing max LOA to {mission.CapLoaM.Value * 1.15m:F1}m.");
            }

            // Speed-related failures
            if (EstimatedFroudeNumber.HasValue && EstimatedFroudeNumber > 0.30m)
            {
                Suggestions.Add($"Service speed ({mission.ServiceSpeedKn:F1} knots) is very high for the target displacement.");
                Suggestions.Add($"Try reducing speed to {mission.ServiceSpeedKn * 0.85m:F1} knots or increasing cargo capacity.");
            }
            else if (EstimatedFroudeNumber.HasValue && EstimatedFroudeNumber < 0.10m)
            {
                Suggestions.Add($"Service speed ({mission.ServiceSpeedKn:F1} knots) is very low for the target displacement.");
                Suggestions.Add($"Consider increasing speed to {mission.ServiceSpeedKn * 1.3m:F1} knots for better hull efficiency.");
            }

            // Displacement-related failures
            if (TargetDisplacementT < 500m)
            {
                Suggestions.Add($"Very small displacement ({TargetDisplacementT:F0} tonnes).");
                Suggestions.Add("Consider using yacht or fishing vessel families, or increase cargo capacity.");
            }
            else if (TargetDisplacementT > 200000m)
            {
                Suggestions.Add($"Very large displacement ({TargetDisplacementT:F0} tonnes).");
                Suggestions.Add("Ensure constraints allow for a large vessel (beam >40m, draft >15m).");
            }

            // Generic convergence failures
            if (hasNoConvergence && !hasBeamConstraint && !hasDraftConstraint)
            {
                Suggestions.Add("The solver could not find a valid combination of dimensions.");
                Suggestions.Add("Try relaxing some constraints or adjusting speed/cargo parameters.");
            }
        }

        // Some families worked, but not enough candidates
        if (FamiliesFailedClosure < FamiliesAfterHintsFiltering && FamiliesFailedClosure > 0)
        {
            var successfulFamilies = FamiliesAfterHintsFiltering - FamiliesFailedClosure;
            Suggestions.Add($"Only {successfulFamilies} out of {FamiliesAfterHintsFiltering} families produced valid designs, but none survived filtering.");
            Suggestions.Add("This is unusual. Check mission parameters for extreme values.");
        }

        // Default fallback
        if (Suggestions.Count == 0)
        {
            Suggestions.Add("The solver could not generate designs with the current parameters.");
            Suggestions.Add("Try adjusting service speed, cargo capacity, or relaxing physical constraints.");
        }
    }

    /// <summary>
    /// Get a user-friendly summary of what went wrong
    /// </summary>
    public string GetSummary()
    {
        if (TotalFamiliesConsidered == 0)
            return "No hull families available in the system.";

        if (FamiliesAfterHintsFiltering == 0)
            return "Hull family filters excluded all available families.";

        if (FamiliesFailedClosure == FamiliesAfterHintsFiltering && FamiliesFailedClosure > 0)
            return "All hull families failed to converge due to restrictive constraints or incompatible parameters.";

        if (FamiliesFailedClosure > 0)
            return $"{FamiliesFailedClosure} of {FamiliesAfterHintsFiltering} families failed to converge.";

        return "The solver completed but generated no valid designs.";
    }
}








