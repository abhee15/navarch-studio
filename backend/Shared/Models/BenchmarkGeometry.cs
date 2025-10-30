using System;

namespace Shared.Models;

public class BenchmarkGeometry
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public required string Type { get; set; } // offsets | iges | analytic
    public string? SourceUrl { get; set; }
    public string? S3Key { get; set; }
    public string? Checksum { get; set; }
    public string? ScaleNote { get; set; }
    public DateTime CreatedAt { get; set; }

    public BenchmarkCase? Case { get; set; }
}
