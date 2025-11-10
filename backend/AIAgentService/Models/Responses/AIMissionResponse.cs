namespace AIAgentService.Models.Responses;

/// <summary>
/// Internal model for AI response before converting to MissionCaseDto
/// </summary>
public class AIMissionResponse
{
    public string Name { get; set; } = string.Empty;
    public string MissionType { get; set; } = string.Empty;
    public string CargoBasis { get; set; } = string.Empty;
    public decimal CargoValue { get; set; }
    public decimal? CargoDensityTPerM3 { get; set; }
    public decimal ServiceSpeedKn { get; set; }
    public decimal? MaxBeamM { get; set; }
    public decimal? MaxDraftM { get; set; }
    public decimal? MaxDisplacementKg { get; set; }
    public string? HullFamily { get; set; }
    public bool LockFroude { get; set; }
    public decimal? TargetFroude { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public double Confidence { get; set; }
}





