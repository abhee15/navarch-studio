using System;
using System.Collections.Generic;

namespace Shared.Models;

public class BenchmarkCase
{
    public Guid Id { get; set; }
    public required string Slug { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? CanonicalRefs { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<BenchmarkGeometry> Geometries { get; set; } = new List<BenchmarkGeometry>();
    public ICollection<BenchmarkTestPoint> TestPoints { get; set; } = new List<BenchmarkTestPoint>();
    public ICollection<BenchmarkMetricRef> MetricRefs { get; set; } = new List<BenchmarkMetricRef>();
    public ICollection<BenchmarkAsset> Assets { get; set; } = new List<BenchmarkAsset>();
    public ICollection<BenchmarkValidationRun> ValidationRuns { get; set; } = new List<BenchmarkValidationRun>();
}
