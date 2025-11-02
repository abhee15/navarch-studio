namespace Shared.Models.Sizing;

public class KpiWeight
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Metric { get; set; } = null!;
    public decimal Weight { get; set; }
}

