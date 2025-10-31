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

    // Normalized JSON storage for fast access (derived from IGES/offsets)
    public string? StationsJson { get; set; }  // JSON array of station positions
    public string? WaterlinesJson { get; set; }  // JSON array of waterline elevations
    public string? OffsetsJson { get; set; }  // JSON grid of half-breadths at [station, waterline]

    public BenchmarkCase? Case { get; set; }
}
