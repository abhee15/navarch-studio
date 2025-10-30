using System;

namespace Shared.Models;

public class BenchmarkValidationRun
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public decimal? Fr { get; set; }
    public string? Metrics { get; set; } // JSON
    public required string Status { get; set; }
    public DateTime CreatedAt { get; set; }

    public BenchmarkCase? Case { get; set; }
}
