namespace Shared.Models.Engineering;

/// <summary>
/// Weight breakdown for a vessel design
/// Based on naval architecture principles (Watson & Gilfillan)
/// </summary>
public class WeightBreakdown
{
    /// <summary>
    /// Steel weight (hull structure) in tonnes
    /// </summary>
    public decimal SteelWeightT { get; set; }

    /// <summary>
    /// Machinery weight (engines, gearboxes, shafting, auxiliaries) in tonnes
    /// </summary>
    public decimal MachineryWeightT { get; set; }

    /// <summary>
    /// Outfit and equipment weight (piping, electrical, HVAC, accommodation) in tonnes
    /// </summary>
    public decimal OutfitWeightT { get; set; }

    /// <summary>
    /// Lightship weight (steel + machinery + outfit) in tonnes
    /// </summary>
    public decimal LightshipWeightT { get; set; }

    /// <summary>
    /// Fuel capacity in tonnes
    /// </summary>
    public decimal FuelCapacityT { get; set; }

    /// <summary>
    /// Fresh water capacity in tonnes
    /// </summary>
    public decimal FreshWaterT { get; set; }

    /// <summary>
    /// Stores and provisions in tonnes
    /// </summary>
    public decimal StoresT { get; set; }

    /// <summary>
    /// Crew and effects weight in tonnes
    /// </summary>
    public decimal CrewWeightT { get; set; }

    /// <summary>
    /// Cargo weight (deadweight - consumables) in tonnes
    /// </summary>
    public decimal CargoWeightT { get; set; }

    /// <summary>
    /// Total deadweight (displacement - lightship) in tonnes
    /// </summary>
    public decimal DeadweightT { get; set; }

    /// <summary>
    /// Vertical center of gravity (VCG/KG) estimate in meters
    /// </summary>
    public decimal VcgEstimateM { get; set; }

    /// <summary>
    /// Weight margin as percentage of lightship (typically 5-10%)
    /// </summary>
    public decimal WeightMarginPct { get; set; }

    /// <summary>
    /// Calculation method used
    /// </summary>
    public string? CalculationMethod { get; set; }

    /// <summary>
    /// Warnings or notes about the weight estimate
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}
