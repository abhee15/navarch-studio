using AIAgentService.Models.Requests;
using AIAgentService.Models.Responses;
using AIAgentService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIAgentService.Controllers;

[ApiController]
[Route("api/v1/ai-agent")]
public class DesignAssistantController : ControllerBase
{
    private readonly INLToMissionService _nlService;
    private readonly ILogger<DesignAssistantController> _logger;

    public DesignAssistantController(
        INLToMissionService nlService,
        ILogger<DesignAssistantController> logger)
    {
        _nlService = nlService;
        _logger = logger;
    }

    /// <summary>
    /// Convert natural language to mission parameters
    /// </summary>
    [HttpPost("suggest/mission")]
    [ProducesResponseType(typeof(MissionSuggestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SuggestMission(
        [FromBody] NLMissionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Processing mission suggestion for: {Input}", request.NaturalLanguage);

            var result = await _nlService.ConvertAsync(
                request.NaturalLanguage,
                request.PreferredUnits,
                cancellationToken);

            var response = new MissionSuggestionResponse
            {
                MissionCase = result.MissionCase,
                Reasoning = result.Reasoning,
                Confidence = result.Confidence,
                AlternativeSuggestions = new List<string>()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing mission suggestion");
            return StatusCode(500, new { error = "Failed to process mission request" });
        }
    }

    /// <summary>
    /// Refine existing mission based on user feedback
    /// </summary>
    [HttpPost("refine/mission")]
    [ProducesResponseType(typeof(MissionSuggestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefineMission(
        [FromBody] RefineMissionRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Refining mission based on feedback");

            var result = await _nlService.RefineAsync(
                request.CurrentMission,
                request.UserFeedback,
                cancellationToken);

            var response = new MissionSuggestionResponse
            {
                MissionCase = result.MissionCase,
                Reasoning = result.Reasoning,
                Confidence = result.Confidence,
                AlternativeSuggestions = new List<string>()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refining mission");
            return StatusCode(500, new { error = "Failed to refine mission" });
        }
    }
}
