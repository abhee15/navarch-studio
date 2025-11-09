using AIAgentService.Models.Responses;

namespace AIAgentService.Services;

public interface INLToMissionService
{
    Task<MissionResult> ConvertAsync(string naturalLanguage, string? preferredUnits = "SI", CancellationToken cancellationToken = default);
    Task<MissionResult> RefineAsync(Shared.DTOs.Sizing.MissionCaseDto currentMission, string userFeedback, CancellationToken cancellationToken = default);
}



