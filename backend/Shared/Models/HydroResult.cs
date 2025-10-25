namespace Shared.Models;

/// <summary>
/// Hydrostatic calculation result for a specific draft and loadcase
/// </summary>
public class HydroResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public Guid VesselId { get; set; }
    public Vessel Vessel { get; set; } = null!;
    
    public Guid? LoadcaseId { get; set; }
    public Loadcase? Loadcase { get; set; }
    
    /// <summary>
    /// Draft (m)
    /// </summary>
    public decimal Draft { get; set; }
    
    /// <summary>
    /// Displacement volume (m³)
    /// </summary>
    public decimal? DispVolume { get; set; }
    
    /// <summary>
    /// Displacement weight (kg)
    /// </summary>
    public decimal? DispWeight { get; set; }
    
    /// <summary>
    /// Vertical center of buoyancy from keel (m)
    /// </summary>
    public decimal? KBz { get; set; }
    
    /// <summary>
    /// Longitudinal center of buoyancy from aft perpendicular (m)
    /// </summary>
    public decimal? LCBx { get; set; }
    
    /// <summary>
    /// Transverse center of buoyancy from centerline (m)
    /// </summary>
    public decimal? TCBy { get; set; }
    
    /// <summary>
    /// Transverse metacentric radius (m)
    /// </summary>
    public decimal? BMt { get; set; }
    
    /// <summary>
    /// Longitudinal metacentric radius (m)
    /// </summary>
    public decimal? BMl { get; set; }
    
    /// <summary>
    /// Transverse metacentric height (m)
    /// </summary>
    public decimal? GMt { get; set; }
    
    /// <summary>
    /// Longitudinal metacentric height (m)
    /// </summary>
    public decimal? GMl { get; set; }
    
    /// <summary>
    /// Waterplane area (m²)
    /// </summary>
    public decimal? Awp { get; set; }
    
    /// <summary>
    /// Waterplane second moment of area (m⁴)
    /// </summary>
    public decimal? Iwp { get; set; }
    
    /// <summary>
    /// Block coefficient
    /// </summary>
    public decimal? Cb { get; set; }
    
    /// <summary>
    /// Prismatic coefficient
    /// </summary>
    public decimal? Cp { get; set; }
    
    /// <summary>
    /// Midship coefficient
    /// </summary>
    public decimal? Cm { get; set; }
    
    /// <summary>
    /// Waterplane coefficient
    /// </summary>
    public decimal? Cwp { get; set; }
    
    /// <summary>
    /// Trim angle (degrees, positive = bow up)
    /// </summary>
    public decimal? TrimAngle { get; set; }
    
    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    public string? Meta { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

