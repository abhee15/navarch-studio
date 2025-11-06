using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shared.Models;

[Table("vessels_real", Schema = "catalog_user")]
public class CatalogVesselReal
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string VesselId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string VesselType { get; set; } = string.Empty;

    // Principal Dimensions
    [Required]
    [Column(TypeName = "decimal(10,3)")]
    public decimal LppM { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,3)")]
    public decimal BeamM { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,3)")]
    public decimal DraftM { get; set; }

    [Column(TypeName = "decimal(10,3)")]
    public decimal? DepthM { get; set; }

    [Required]
    [Column(TypeName = "decimal(12,2)")]
    public decimal DisplacementT { get; set; }

    // Form Coefficients
    [Required]
    [Column(TypeName = "decimal(5,4)")]
    public decimal Cb { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal? Cp { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal? Cm { get; set; }

    [Column(TypeName = "decimal(5,4)")]
    public decimal? Cw { get; set; }

    // Performance
    [Column(TypeName = "decimal(6,3)")]
    public decimal? ServiceSpeedMs { get; set; }

    [Column(TypeName = "decimal(12,2)")]
    public decimal? DwtT { get; set; }

    // Additional Data
    [MaxLength(100)]
    public string? EngineType { get; set; }

    public int? YearBuilt { get; set; }

    [MaxLength(200)]
    public string? Source { get; set; }

    [MaxLength(100)]
    public string? DataQuality { get; set; }

    // Geometry & Performance Data (JSONB)
    [Column(TypeName = "jsonb")]
    public string? ResistanceCurve { get; set; }  // JSON string: {"Fn": [...], "Resistance_N": [...]}

    [MaxLength(500)]
    public string? HullGeometryFile { get; set; }

    // Permissions & Tracking
    [Required]
    public bool IsSystemData { get; set; } = true;  // Default to system data

    public Guid? CreatedBy { get; set; }  // Foreign key to users (not enforced for flexibility)

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    // None for now - catalog is independent
}

