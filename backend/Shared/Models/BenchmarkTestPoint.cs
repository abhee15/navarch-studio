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

    // Water properties for this test condition
    public string? Medium { get; set; }  // "Fresh" | "Sea"
    public decimal? Temperature_C { get; set; }
    public decimal? Salinity_PSU { get; set; }
    public decimal? Density_kgm3 { get; set; }
    public decimal? KinematicViscosity_m2s { get; set; }

    public BenchmarkCase? Case { get; set; }
}
