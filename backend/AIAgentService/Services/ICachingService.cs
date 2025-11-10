using AIAgentService.Models.Responses;

namespace AIAgentService.Services;

public interface ICachingService
{
    Task<MissionResult?> GetCachedMissionAsync(string input);
    Task CacheMissionAsync(string input, MissionResult result);
}





