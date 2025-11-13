using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataService.Data.ShipD;

/// <summary>
/// Metadata for ShipD parameterization vectors (45 canonical parameters).
/// Stored in DataService to provide frontend/backends with human-readable labels, groupings, and statistical ranges.
/// </summary>
[Table("shipd_parameter_metadata", Schema = "data")]
public class ShipDParameterMetadata
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Zero-based index within the canonical ShipD parameter vector.
    /// </summary>
    [Range(0, 255)]
    public int ParameterIndex { get; set; }

    /// <summary>
    /// Canonical label string (e.g., LOA, Lb, Beta).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Logical grouping (principal_dimensions, bow, midship, stern, appendage, etc.).
    /// </summary>
    [MaxLength(100)]
    public string? Group { get; set; }

    /// <summary>
    /// Optional description or tooltip text.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Engineering unit or dimensionless flag.
    /// </summary>
    [MaxLength(50)]
    public string? Unit { get; set; }

    /// <summary>
    /// Observed minimum value (from dataset) for quick validation.
    /// </summary>
    public decimal? Min { get; set; }

    /// <summary>
    /// Observed maximum value.
    /// </summary>
    public decimal? Max { get; set; }

    /// <summary>
    /// Observed mean value.
    /// </summary>
    public decimal? Mean { get; set; }

    /// <summary>
    /// Observed standard deviation.
    /// </summary>
    public decimal? StdDev { get; set; }

    /// <summary>
    /// Optional JSON metadata (e.g., display hints, constraints).
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? MetadataJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}


