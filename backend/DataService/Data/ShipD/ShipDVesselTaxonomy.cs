using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataService.Data.ShipD;

/// <summary>
/// Canonical mapping of vessel categories/types to applicable ShipD hull family options.
/// </summary>
[Table("shipd_vessel_taxonomy", Schema = "data")]
public class ShipDVesselTaxonomy
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// High-level category (Commercial, Government, Recreational, Research, etc.).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Canonical vessel type slug (e.g., general_cargo, bulk_carrier).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable type name for UI display.
    /// </summary>
    [Required]
    [MaxLength(150)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Optional description or notes.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// JSON array of bow family identifiers applicable to this vessel type.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string BowFamiliesJson { get; set; } = "[]";

    /// <summary>
    /// JSON array of midship family identifiers.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string MidshipFamiliesJson { get; set; } = "[]";

    /// <summary>
    /// JSON array of stern family identifiers.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string SternFamiliesJson { get; set; } = "[]";

    /// <summary>
    /// Optional JSON describing additional parameter prompts triggered by this combination.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? AdditionalParametersJson { get; set; }

    /// <summary>
    /// Version of the ShipD family mask logic used to derive the arrays.
    /// </summary>
    public int MaskVersion { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}


