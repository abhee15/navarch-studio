using System.ComponentModel.DataAnnotations;
using Shared.DTOs.Sizing;

namespace AIAgentService.Models.Requests;

public class RefineMissionRequest
{
    [Required]
    public MissionCaseDto CurrentMission { get; set; } = new();

    [Required]
    [MaxLength(500)]
    public string UserFeedback { get; set; } = string.Empty;
}
