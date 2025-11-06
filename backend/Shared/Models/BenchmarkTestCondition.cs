using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models;

/// <summary>
/// Benchmark test conditions from SIMMAN/ITTC workshops
/// Used for validation of resistance, seakeeping, and maneuvering predictions
/// </summary>
[Table("benchmark_test_conditions", Schema = "catalog_real")]
public class BenchmarkTestCondition
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Type of test (Resistance, Self_Propulsion, Turning_Circle, Zigzag, Seakeeping, etc.)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string TestType { get; set; } = string.Empty;

    /// <summary>
    /// Hull name (must match catalog_vessels.name)
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string HullName { get; set; } = string.Empty;

    /// <summary>
    /// Test speed in knots
    /// </summary>
    public decimal SpeedKnots { get; set; }

    /// <summary>
    /// Froude number (dimensionless speed)
    /// Fn = V / sqrt(g * L)
    /// </summary>
    public decimal FroudeNumber { get; set; }

    /// <summary>
    /// Reynolds number (flow regime indicator)
    /// </summary>
    public decimal ReynoldsNumber { get; set; }

    /// <summary>
    /// Wave height in meters (0 for calm water)
    /// </summary>
    public decimal WaveHeightM { get; set; }

    /// <summary>
    /// Wave period in seconds (0 for calm water)
    /// </summary>
    public decimal WavePeriodS { get; set; }

    /// <summary>
    /// Wave heading in degrees (0=following, 90=beam, 180=head seas)
    /// </summary>
    public decimal HeadingDeg { get; set; }

    /// <summary>
    /// Test description
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Standard/source (ITTC, SIMMAN, etc.)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Standard { get; set; } = string.Empty;

    /// <summary>
    /// When this test condition was added
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

