using Shared.DTOs.Sizing;

namespace AIAgentService.Models.Responses;

public class MissionSuggestionResponse
{
    public MissionCaseDto MissionCase { get; set; } = new();
    public string Reasoning { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public List<string> AlternativeSuggestions { get; set; } = new();
}
