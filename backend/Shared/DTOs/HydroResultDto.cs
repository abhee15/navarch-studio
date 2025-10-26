using Shared.Attributes;

namespace Shared.DTOs;

public class HydroResultDto
{
    [Convertible("Length")]
    public decimal Draft { get; set; }
    
    [Convertible("Volume")]
    public decimal? DispVolume { get; set; }
    
    [Convertible("Mass")]
    public decimal? DispWeight { get; set; }
    
    [Convertible("Length")]
    public decimal? KBz { get; set; }
    
    [Convertible("Length")]
    public decimal? LCBx { get; set; }
    
    [Convertible("Length")]
    public decimal? TCBy { get; set; }
    
    [Convertible("Length")]
    public decimal? BMt { get; set; }
    
    [Convertible("Length")]
    public decimal? BMl { get; set; }
    
    [Convertible("Length")]
    public decimal? GMt { get; set; }
    
    [Convertible("Length")]
    public decimal? GMl { get; set; }
    
    [Convertible("Area")]
    public decimal? Awp { get; set; }
    
    [Convertible("Inertia")]
    public decimal? Iwp { get; set; }
    
    public decimal? Cb { get; set; }  // Dimensionless
    public decimal? Cp { get; set; }  // Dimensionless
    public decimal? Cm { get; set; }  // Dimensionless
    public decimal? Cwp { get; set; } // Dimensionless
    public decimal? TrimAngle { get; set; }  // Degrees (no conversion needed)
}

public class HydroTableRequestDto
{
    public Guid LoadcaseId { get; set; }
    
    [Convertible("Length")]
    public List<decimal> Drafts { get; set; } = new();
}

public class HydroTableResponseDto
{
    public List<HydroResultDto> Results { get; set; } = new();
    public int ComputationTimeMs { get; set; }
    public string UnitsSystem { get; set; } = "SI";  // Track source units
}

