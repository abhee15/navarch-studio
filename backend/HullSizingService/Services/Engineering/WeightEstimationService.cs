using Shared.Models.Engineering;
using Shared.Models.Sizing;

namespace HullSizingService.Services.Engineering;

/// <summary>
/// Service for estimating vessel weight breakdown
/// Uses industry-standard methods (Watson and Gilfillan, SNAME)
/// </summary>
public interface IWeightEstimationService
{
    /// <summary>
    /// Calculate weight breakdown for a candidate design
    /// </summary>
    WeightBreakdown EstimateWeights(
        decimal lppM,
        decimal beamM,
        decimal depthM,
        decimal draftM,
        decimal cb,
        decimal displacementT,
        decimal? shpKw,
        string missionType,
        decimal? enduranceNm = null,
        decimal? serviceSpeedKn = null);
}

public class WeightEstimationService : IWeightEstimationService
{
    private readonly ILogger<WeightEstimationService> _logger;

    public WeightEstimationService(ILogger<WeightEstimationService> logger)
    {
        _logger = logger;
    }

    public WeightBreakdown EstimateWeights(
        decimal lppM,
        decimal beamM,
        decimal depthM,
        decimal draftM,
        decimal cb,
        decimal displacementT,
        decimal? shpKw,
        string missionType,
        decimal? enduranceNm = null,
        decimal? serviceSpeedKn = null)
    {
        var breakdown = new WeightBreakdown
        {
            CalculationMethod = "Watson & Gilfillan (simplified)"
        };

        // 1. Steel Weight (Watson's method)
        breakdown.SteelWeightT = EstimateSteelWeight(lppM, beamM, depthM, cb, missionType);

        // 2. Machinery Weight
        breakdown.MachineryWeightT = EstimateMachineryWeight(shpKw, missionType);

        // 3. Outfit & Equipment
        breakdown.OutfitWeightT = EstimateOutfitWeight(lppM, beamM, depthM, missionType);

        // 4. Lightship = Steel + Machinery + Outfit
        breakdown.LightshipWeightT = breakdown.SteelWeightT + breakdown.MachineryWeightT + breakdown.OutfitWeightT;

        // 5. Weight Margin (5-10% depending on design phase)
        breakdown.WeightMarginPct = GetWeightMargin(missionType);
        var marginT = breakdown.LightshipWeightT * (breakdown.WeightMarginPct / 100m);

        // Adjust lightship for margin
        breakdown.LightshipWeightT += marginT;

        // 6. Deadweight = Displacement - Lightship
        breakdown.DeadweightT = displacementT - breakdown.LightshipWeightT;

        if (breakdown.DeadweightT < 0)
        {
            breakdown.Warnings.Add($"Negative deadweight ({breakdown.DeadweightT:F0}t). Design may be infeasible.");
            breakdown.DeadweightT = 0;
        }

        // 7. Fuel Capacity (if endurance specified)
        if (enduranceNm.HasValue && serviceSpeedKn.HasValue && shpKw.HasValue)
        {
            breakdown.FuelCapacityT = EstimateFuelCapacity(shpKw.Value, enduranceNm.Value, serviceSpeedKn.Value);
        }
        else
        {
            // Default: 15% of deadweight for ocean-going vessels
            breakdown.FuelCapacityT = breakdown.DeadweightT * 0.15m;
        }

        // 8. Fresh Water (typically 2-3% of DWT)
        breakdown.FreshWaterT = breakdown.DeadweightT * 0.025m;

        // 9. Stores & Provisions (typically 1-2% of DWT)
        breakdown.StoresT = breakdown.DeadweightT * 0.015m;

        // 10. Crew & Effects (estimate based on vessel size)
        breakdown.CrewWeightT = EstimateCrewWeight(lppM, missionType);

        // 11. Cargo = DWT - Fuel - FW - Stores - Crew
        breakdown.CargoWeightT = breakdown.DeadweightT
            - breakdown.FuelCapacityT
            - breakdown.FreshWaterT
            - breakdown.StoresT
            - breakdown.CrewWeightT;

        if (breakdown.CargoWeightT < 0)
        {
            breakdown.Warnings.Add("Insufficient deadweight for cargo. Increase displacement or reduce consumables.");
            breakdown.CargoWeightT = 0;
        }

        // 12. VCG Estimate (simplified)
        breakdown.VcgEstimateM = EstimateVCG(lppM, depthM, missionType);

        // Validate and add warnings
        ValidateWeightBreakdown(breakdown, displacementT, missionType);

        _logger.LogDebug(
            "[WEIGHT] L={L}m, Disp={Disp}t → Lightship={LS}t, DWT={DWT}t, Cargo={Cargo}t",
            lppM, displacementT, breakdown.LightshipWeightT, breakdown.DeadweightT, breakdown.CargoWeightT);

        return breakdown;
    }

    /// <summary>
    /// Estimate steel weight using Watson's method
    /// W_steel = C * L^a * B^b * D^c * Cb^d
    /// </summary>
    private decimal EstimateSteelWeight(decimal lppM, decimal beamM, decimal depthM, decimal cb, string missionType)
    {
        // Coefficients vary by vessel type
        var (c, a, b, d_exp, cb_exp) = GetSteelCoefficients(missionType);

        // Watson's formula
        var steelWeight = c
            * (decimal)Math.Pow((double)lppM, a)
            * (decimal)Math.Pow((double)beamM, b)
            * (decimal)Math.Pow((double)depthM, d_exp)
            * (decimal)Math.Pow((double)cb, cb_exp);

        return Math.Max(0, steelWeight);
    }

    /// <summary>
    /// Get steel weight coefficients for different vessel types
    /// </summary>
    private (decimal c, double a, double b, double d, double cb) GetSteelCoefficients(string missionType)
    {
        var type = missionType.ToLowerInvariant();

        return type switch
        {
            "container" => (0.034m, 1.0, 0.7, 0.85, 0.5),      // High-tensile steel, optimized
            "tanker" => (0.040m, 1.0, 0.7, 0.85, 0.5),          // Heavier due to tank structure
            "bulk" => (0.036m, 1.0, 0.7, 0.85, 0.5),            // Moderate weight
            "lng" => (0.045m, 1.0, 0.7, 0.85, 0.5),             // Heavy due to tank systems
            "roro" => (0.030m, 1.0, 0.7, 0.85, 0.5),            // Lighter, large volume
            "cargo" => (0.035m, 1.0, 0.7, 0.85, 0.5),           // General cargo
            "fishing" => (0.038m, 1.0, 0.7, 0.85, 0.5),         // Robust construction
            "yacht_disp" => (0.042m, 1.0, 0.7, 0.85, 0.5),      // Higher quality materials
            _ => (0.035m, 1.0, 0.7, 0.85, 0.5)                  // Default
        };
    }

    /// <summary>
    /// Estimate machinery weight from installed power
    /// W_machinery = k * SHP^0.7
    /// </summary>
    private decimal EstimateMachineryWeight(decimal? shpKw, string missionType)
    {
        if (!shpKw.HasValue || shpKw.Value <= 0)
            return 0;

        // Convert kW to HP for formula (1 HP ≈ 0.746 kW)
        var shpHp = shpKw.Value / 0.746m;

        // Coefficient varies by engine type
        var k = missionType.ToLowerInvariant() switch
        {
            "container" => 0.17m,       // High-speed diesels
            "tanker" => 0.19m,          // Low-speed diesels (heavier)
            "bulk" => 0.18m,            // Medium-speed diesels
            "hsc_planing" => 0.12m,     // Gas turbines (lighter)
            "ferry" => 0.15m,           // High-speed diesels
            _ => 0.17m                  // Default: modern diesel
        };

        return k * (decimal)Math.Pow((double)shpHp, 0.7);
    }

    /// <summary>
    /// Estimate outfit and equipment weight
    /// W_outfit = k * L * (B + D)
    /// </summary>
    private decimal EstimateOutfitWeight(decimal lppM, decimal beamM, decimal depthM, string missionType)
    {
        var k = missionType.ToLowerInvariant() switch
        {
            "container" => 0.12m,       // Minimal accommodation
            "tanker" => 0.14m,          // Cargo handling systems
            "bulk" => 0.13m,            // Cargo handling
            "roro" => 0.15m,            // Ramps and vehicle systems
            "lng" => 0.16m,             // Complex cargo systems
            "fishing" => 0.18m,         // Fish processing equipment
            "yacht_disp" => 0.30m,      // Luxury accommodation
            "ferry" => 0.25m,           // Passenger facilities
            _ => 0.15m                  // Default
        };

        return k * lppM * (beamM + depthM);
    }

    /// <summary>
    /// Estimate fuel capacity from endurance
    /// </summary>
    private decimal EstimateFuelCapacity(decimal shpKw, decimal enduranceNm, decimal serviceSpeedKn)
    {
        // Hours of operation
        var hours = enduranceNm / serviceSpeedKn;

        // SFOC (Specific Fuel Oil Consumption) in g/kWh
        // Modern diesels: 180-220 g/kWh
        var sfoc = 200m; // g/kWh (conservative)

        // Fuel consumption = SHP * SFOC * hours / 1,000,000 (convert g to tonnes)
        var fuelT = shpKw * sfoc * hours / 1_000_000m;

        // Add 10% margin
        return fuelT * 1.10m;
    }

    /// <summary>
    /// Estimate crew weight (personnel + effects)
    /// </summary>
    private decimal EstimateCrewWeight(decimal lppM, string missionType)
    {
        // Crew size estimate based on length and type
        var crewSize = missionType.ToLowerInvariant() switch
        {
            "container" when lppM < 150 => 15,
            "container" => 20,
            "tanker" when lppM < 200 => 20,
            "tanker" => 25,
            "bulk" => 20,
            "ferry" when lppM < 100 => 10,
            "ferry" => 15,
            "fishing" => 15,
            "yacht_disp" => 8,
            _ => (int)(lppM / 10m) // Rule of thumb: 1 crew per 10m
        };

        // Weight per person (including effects, provisions)
        var weightPerPerson = 0.15m; // 150 kg

        return crewSize * weightPerPerson;
    }

    /// <summary>
    /// Estimate vertical center of gravity (VCG/KG)
    /// </summary>
    private decimal EstimateVCG(decimal lppM, decimal depthM, string missionType)
    {
        // Simplified formula: VCG ≈ 0.01*L + D/2
        // This is a rough approximation for preliminary design
        var vcg = 0.01m * lppM + depthM / 2m;

        // Adjust for vessel type
        var adjustment = missionType.ToLowerInvariant() switch
        {
            "container" => 1.1m,        // Higher due to deck containers
            "roro" => 1.15m,            // Higher due to superstructure
            "ferry" => 1.12m,           // Higher due to passenger decks
            "tanker" => 0.95m,          // Lower, most weight in tanks
            "bulk" => 0.97m,            // Lower, cargo in holds
            _ => 1.0m
        };

        return vcg * adjustment;
    }

    /// <summary>
    /// Get weight margin percentage (varies by design phase)
    /// </summary>
    private decimal GetWeightMargin(string missionType)
    {
        // Preliminary design: 5-10%
        // For novel designs or complex vessels: higher margin
        return missionType.ToLowerInvariant() switch
        {
            "lng" => 10m,               // Complex systems
            "ferry" => 8m,              // Many systems
            "yacht_disp" => 10m,        // Custom design
            _ => 7m                     // Standard margin
        };
    }

    /// <summary>
    /// Validate weight breakdown and add warnings
    /// </summary>
    private void ValidateWeightBreakdown(WeightBreakdown breakdown, decimal displacementT, string missionType)
    {
        // Check lightship coefficient (LS/Disp)
        var lsCoeff = breakdown.LightshipWeightT / displacementT;
        if (lsCoeff < 0.30m)
        {
            breakdown.Warnings.Add($"Lightship coefficient very low ({lsCoeff:P0}). Typical: 35-50%.");
        }
        else if (lsCoeff > 0.70m)
        {
            breakdown.Warnings.Add($"Lightship coefficient very high ({lsCoeff:P0}). Limited cargo capacity.");
        }

        // Check deadweight coefficient (DWT/Disp)
        var dwtCoeff = breakdown.DeadweightT / displacementT;
        if (dwtCoeff < 0.30m)
        {
            breakdown.Warnings.Add($"Low deadweight coefficient ({dwtCoeff:P0}). Economic feasibility questionable.");
        }

        // Check cargo fraction of deadweight
        var cargoFraction = breakdown.CargoWeightT / breakdown.DeadweightT;
        if (cargoFraction < 0.50m)
        {
            breakdown.Warnings.Add($"Cargo is only {cargoFraction:P0} of deadweight. High consumables.");
        }
    }
}
