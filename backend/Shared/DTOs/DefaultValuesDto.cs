namespace Shared.DTOs;

/// <summary>
/// Request DTO for fetching default/typical values for resistance and powering parameters
/// </summary>
public class DefaultValuesRequestDto
{
    /// <summary>
    /// Vessel type (e.g., "CargoShip", "Tanker", "ContainerShip", "Ferry", "Yacht")
    /// </summary>
    public string? VesselType { get; set; }

    /// <summary>
    /// Block coefficient (CB)
    /// </summary>
    public decimal? CB { get; set; }

    /// <summary>
    /// Length to Beam ratio (L/B)
    /// </summary>
    public decimal? LB_Ratio { get; set; }

    /// <summary>
    /// Beam to Draft ratio (B/T)
    /// </summary>
    public decimal? BT_Ratio { get; set; }

    /// <summary>
    /// Length between perpendiculars (m) - used for additional scaling/validation
    /// </summary>
    public decimal? Lpp { get; set; }

    /// <summary>
    /// Beam (m)
    /// </summary>
    public decimal? Beam { get; set; }

    /// <summary>
    /// Draft (m)
    /// </summary>
    public decimal? Draft { get; set; }
}

/// <summary>
/// Response DTO containing typical/default values with provenance
/// </summary>
public class DefaultValuesResponseDto
{
    /// <summary>
    /// Form factor k (typical value: 0.15 - 0.30)
    /// </summary>
    public DefaultValueItem? FormFactor { get; set; }

    /// <summary>
    /// Appendage area as percentage of wetted surface (typical: 2-10%)
    /// </summary>
    public DefaultValueItem? AppendageAreaPercent { get; set; }

    /// <summary>
    /// Roughness allowance coefficient CA (typical: 0.0004)
    /// </summary>
    public DefaultValueItem? RoughnessAllowance { get; set; }

    /// <summary>
    /// Overall propulsive efficiency ηD (typical: 0.55 - 0.75)
    /// </summary>
    public DefaultValueItem? EtaD { get; set; }

    /// <summary>
    /// Hull efficiency ηH (typical: 0.95 - 1.05)
    /// </summary>
    public DefaultValueItem? EtaH { get; set; }

    /// <summary>
    /// Relative rotative efficiency ηR (typical: 1.0 - 1.05)
    /// </summary>
    public DefaultValueItem? EtaR { get; set; }

    /// <summary>
    /// Open water efficiency ηO (typical: 0.50 - 0.70)
    /// </summary>
    public DefaultValueItem? EtaO { get; set; }

    /// <summary>
    /// Wetted surface area (m²) - estimated based on dimensions
    /// </summary>
    public DefaultValueItem? WettedSurfaceArea { get; set; }

    /// <summary>
    /// Midship coefficient CM (typical: 0.90 - 0.99)
    /// </summary>
    public DefaultValueItem? CM { get; set; }

    /// <summary>
    /// Overall provenance message
    /// </summary>
    public string Provenance { get; set; } = "Default values based on typical vessels";
}

/// <summary>
/// A single default value with its provenance
/// </summary>
public class DefaultValueItem
{
    /// <summary>
    /// The default value
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Provenance/source of this value (e.g., "Default for cargo ships", "Typical for CB=0.70")
    /// </summary>
    public string Provenance { get; set; } = string.Empty;

    /// <summary>
    /// Optional range or guidance (e.g., "Typical range: 0.15-0.25")
    /// </summary>
    public string? Range { get; set; }
}
