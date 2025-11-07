using System.ComponentModel.DataAnnotations;

namespace AIAgentService.Models.Requests;

public class NLMissionRequest
{
    [Required]
    [MaxLength(1000)]
    public string NaturalLanguage { get; set; } = string.Empty;

    public string? PreferredUnits { get; set; } = "SI"; // SI or Imperial
}

