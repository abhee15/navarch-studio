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

    // Catalog/Hull-specific fields (for use as catalog entries)
    public string? HullType { get; set; }  // "Container" | "Tanker" | "Naval" | "Template"
    public decimal? Lpp_m { get; set; }  // Length between perpendiculars
    public decimal? B_m { get; set; }  // Beam
    public decimal? T_m { get; set; }  // Draft
    public decimal? Cb { get; set; }  // Block coefficient
    public decimal? Cp { get; set; }  // Prismatic coefficient
    public decimal? LCB_pctLpp { get; set; }  // Longitudinal center of buoyancy (% Lpp)
    public decimal? LCF_pctLpp { get; set; }  // Longitudinal center of flotation (% Lpp)
    public bool GeometryMissing { get; set; } = false;  // Flag for entries without full geometry

    // Future-proofing: optional reference to parent catalog entry
    public Guid? CatalogHullId { get; set; }
    public BenchmarkCase? CatalogHull { get; set; }
    public ICollection<BenchmarkCase> ChildBenchmarks { get; set; } = new List<BenchmarkCase>();

    public ICollection<BenchmarkGeometry> Geometries { get; set; } = new List<BenchmarkGeometry>();
    public ICollection<BenchmarkTestPoint> TestPoints { get; set; } = new List<BenchmarkTestPoint>();
    public ICollection<BenchmarkMetricRef> MetricRefs { get; set; } = new List<BenchmarkMetricRef>();
    public ICollection<BenchmarkAsset> Assets { get; set; } = new List<BenchmarkAsset>();
    public ICollection<BenchmarkValidationRun> ValidationRuns { get; set; } = new List<BenchmarkValidationRun>();
}
