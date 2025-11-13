using Shared.DTOs.Sizing;

namespace AIAgentService.Services;

public interface IPromptTemplateService
{
    string GetSystemPrompt(string? preferredUnits = "SI");
    string GetRefinementPrompt(MissionCaseDto currentMission, string userFeedback);
}








