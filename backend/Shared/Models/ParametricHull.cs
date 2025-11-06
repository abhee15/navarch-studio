using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models;

/// <summary>
/// Parametric hull from MIT ShipD dataset (82K synthetic hulls)
/// Each hull defined by 45 parameters + pre-computed geometric measures
/// </summary>
[Table("parametric_hulls", Schema = "catalog_ml")]
public class ParametricHull
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string HullId { get; set; } = string.Empty;  // "CS1_00001", "DA1_12345"

    [Required]
    [MaxLength(50)]
    public string DatasetSource { get; set; } = string.Empty;  // "Constrained_Set_1", etc.

    public int RowIndex { get; set; }  // Original row in source CSV

    // 45 Parametric Vector (JSONB)
    [Required]
    [Column(TypeName = "jsonb")]
    public string ParametricVector { get; set; } = string.Empty;

    // Key Parameters (Extracted for Fast Querying)
    [Required]
    [Column(TypeName = "decimal(10,3)")]
    public decimal LoaM { get; set; } = 10.0m;  // Always 10m in ShipD

    [Required]
    [Column(TypeName = "decimal(6,4)")]
    public decimal LbRatio { get; set; }  // Bow length / LOA

    [Required]
    [Column(TypeName = "decimal(6,4)")]
    public decimal LsRatio { get; set; }  // Stern length / LOA

    [Required]
    [Column(TypeName = "decimal(8,6)")]
    public decimal BdRatio { get; set; }  // (Beam_deck / 2) / LOA

    [Required]
    [Column(TypeName = "decimal(8,6)")]
    public decimal DdRatio { get; set; }  // Depth / LOA

    [Required]
    [Column(TypeName = "decimal(6,4)")]
    public decimal BsRatio { get; set; }  // Stern beam / Bow beam

    // Geometric Measures @ Design Draft (T/Dd = 0.5, array index 4)
    [Required]
    [Column(TypeName = "decimal(12,8)")]
    public decimal VolumeNorm { get; set; }  // Volume/LOA^3

    [Required]
    [Column(TypeName = "decimal(6,4)")]
    public decimal LcbNorm { get; set; }  // LCB/LOA

    [Column(TypeName = "decimal(6,4)")]
    public decimal? VcbNorm { get; set; }  // VCB/Dd

    [Required]
    [Column(TypeName = "decimal(10,8)")]
    public decimal AreaWpNorm { get; set; }  // Area_WP/LOA^2

    [Required]
    [Column(TypeName = "decimal(5,4)")]
    public decimal CwCoeff { get; set; }  // Waterplane coefficient

    [Column(TypeName = "decimal(10,8)")]
    public decimal? AreaWsNorm { get; set; }  // Wetted surface / LOA^2

    [Column(TypeName = "decimal(12,8)")]
    public decimal? IxxNorm { get; set; }  // Moment of inertia X

    [Column(TypeName = "decimal(12,8)")]
    public decimal? IyyNorm { get; set; }  // Moment of inertia Y

    // All Geometric Measures (JSONB - Full Arrays, 10 draft ratios each)
    [Required]
    [Column(TypeName = "jsonb")]
    public string GeometricMeasures { get; set; } = string.Empty;

    // Derived Principal Dimensions @ Design Draft (T/Dd = 0.5)
    [Required]
    [Column(TypeName = "decimal(10,3)")]
    public decimal LppMDerived { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,3)")]
    public decimal BeamMDerived { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,3)")]
    public decimal DraftMDerived { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,3)")]
    public decimal DepthMDerived { get; set; }

    // Derived Form Coefficients
    [Required]
    [Column(TypeName = "decimal(5,4)")]
    public decimal CbDerived { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal? CpDerived { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal? CmDerived { get; set; }

    // Quality Metrics
    [MaxLength(20)]
    public string? ConversionQuality { get; set; }  // "Excellent", "Good", "Fair", "Poor"

    public bool HasValidCoefficients { get; set; } = true;

    [Column(TypeName = "decimal(5,4)")]
    public decimal? DistortionScore { get; set; }

    // Metadata
    [Required]
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    public int DataVersion { get; set; } = 1;

    public bool IsActive { get; set; } = true;  // For soft-delete or quality filtering
}

