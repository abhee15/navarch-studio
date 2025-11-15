namespace Shared.Models.Sizing;

/// <summary>
/// Scoring weights for multi-objective candidate ranking
/// </summary>
public class KpiWeight
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User ID (NULL = system default)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Metric name: delta_balance, installed_power, constraints_ok, stability_screen, teu_or_volume_fit
    /// </summary>
    public string Metric { get; set; } = null!;

    /// <summary>
    /// Weight value (0-1, sum of all weights should equal 1)
    /// </summary>
    public decimal Weight { get; set; }
}














