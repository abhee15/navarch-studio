using Shared.DTOs.Sizing;

namespace AIAgentService.Models.Responses;

public class MissionResult
{
    public MissionCaseDto MissionCase { get; set; } = new();
    public string Reasoning { get; set; } = string.Empty;
    public double Confidence { get; set; }
}
