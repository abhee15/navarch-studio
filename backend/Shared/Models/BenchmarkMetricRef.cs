using System;

namespace Shared.Models;

public class BenchmarkMetricRef
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public decimal? Fr { get; set; }
    public required string Metric { get; set; } // Ct | sinkage | trim | Cw | wake_profile | wave_pattern | pow_curve
    public decimal? ValueNum { get; set; }
    public string? Unit { get; set; }
    public decimal? TolRel { get; set; }
    public string? FigureRef { get; set; }
    public string? SourceUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public BenchmarkCase? Case { get; set; }
}
