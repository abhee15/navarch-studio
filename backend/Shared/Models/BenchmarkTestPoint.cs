using System;

namespace Shared.Models;

public class BenchmarkTestPoint
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public decimal Fr { get; set; }
    public decimal? Vm { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public BenchmarkCase? Case { get; set; }
}
