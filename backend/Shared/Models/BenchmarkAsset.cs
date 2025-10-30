using System;

namespace Shared.Models;

public class BenchmarkAsset
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public required string Kind { get; set; } // plot | table | grid | wake_profile
    public required string S3Key { get; set; }
    public string? Caption { get; set; }
    public string? FigureRef { get; set; }
    public string? SourceUrl { get; set; }

    public BenchmarkCase? Case { get; set; }
}
