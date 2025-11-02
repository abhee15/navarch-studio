namespace Shared.Models.Sizing;

/// <summary>
/// Hull type presets with geometric ranges
/// </summary>
public class HullFamilyPreset
{
    public Guid Id { get; set; }
    public string Family { get; set; } = null!;
    public string? DisplayName { get; set; }

    // Ratios
    public decimal LOverBMin { get; set; }
    public decimal LOverBMax { get; set; }
    public decimal BOverTMin { get; set; }
    public decimal BOverTMax { get; set; }
    public decimal DOverTMin { get; set; }
    public decimal DOverTMax { get; set; }

    // Coefficients
    public decimal CbMin { get; set; }
    public decimal CbMax { get; set; }
    public decimal? CpMin { get; set; }
    public decimal? CpMax { get; set; }
    public decimal? CwpMin { get; set; }
    public decimal? CwpMax { get; set; }

    // Froude
    public decimal? FnMin { get; set; }
    public decimal? FnMax { get; set; }

    public string? GeneratorType { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
}

