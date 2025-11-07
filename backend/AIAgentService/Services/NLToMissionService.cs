using System.Text.Json;
using AIAgentService.Configuration;
using AIAgentService.Models.Responses;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using Shared.DTOs.Sizing;

namespace AIAgentService.Services;

public class NLToMissionService : INLToMissionService
{
    private readonly IPromptTemplateService _promptService;
    private readonly ICachingService _cachingService;
    private readonly ChatClient _chatClient;
    private readonly ILogger<NLToMissionService> _logger;
    private readonly OpenAISettings _settings;

    public NLToMissionService(
        IPromptTemplateService promptService,
        ICachingService cachingService,
        IOptions<OpenAISettings> settings,
        ILogger<NLToMissionService> logger)
    {
        _promptService = promptService;
        _cachingService = cachingService;
        _logger = logger;
        _settings = settings.Value;

        // Initialize OpenAI chat client
        _chatClient = new ChatClient(
            model: _settings.Model,
            apiKey: _settings.ApiKey);
    }

    public async Task<MissionResult> ConvertAsync(
        string naturalLanguage,
        string? preferredUnits = "SI",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Converting NL to mission: {Input}", naturalLanguage);

        // Check cache first
        var cached = await _cachingService.GetCachedMissionAsync(naturalLanguage);
        if (cached != null)
        {
            _logger.LogInformation("Returning cached mission result");
            return cached;
        }

        try
        {
            var systemPrompt = _promptService.GetSystemPrompt(preferredUnits);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(naturalLanguage)
            };

            var chatOptions = new ChatCompletionOptions
            {
                Temperature = (float)_settings.Temperature,
                MaxOutputTokenCount = _settings.MaxTokens,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            var response = await _chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken);

            var content = response.Value.Content[0].Text;
            _logger.LogDebug("OpenAI Response: {Response}", content);

            // Parse AI response
            var aiResponse = JsonSerializer.Deserialize<AIMissionResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (aiResponse == null)
            {
                throw new InvalidOperationException("Failed to parse AI response");
            }

            // Convert to MissionCaseDto
            var missionCase = MapToMissionCase(aiResponse);

            var result = new MissionResult
            {
                MissionCase = missionCase,
                Reasoning = aiResponse.Reasoning,
                Confidence = aiResponse.Confidence
            };

            // Cache the result
            await _cachingService.CacheMissionAsync(naturalLanguage, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting NL to mission");
            throw;
        }
    }

    public async Task<MissionResult> RefineAsync(
        MissionCaseDto currentMission,
        string userFeedback,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refining mission based on feedback: {Feedback}", userFeedback);

        try
        {
            var prompt = _promptService.GetRefinementPrompt(currentMission, userFeedback);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a naval architecture expert helping refine ship design parameters."),
                new UserChatMessage(prompt)
            };

            var chatOptions = new ChatCompletionOptions
            {
                Temperature = (float)_settings.Temperature,
                MaxOutputTokenCount = _settings.MaxTokens,
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
            };

            var response = await _chatClient.CompleteChatAsync(messages, chatOptions, cancellationToken);

            var content = response.Value.Content[0].Text;

            var aiResponse = JsonSerializer.Deserialize<AIMissionResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (aiResponse == null)
            {
                throw new InvalidOperationException("Failed to parse AI response");
            }

            var missionCase = MapToMissionCase(aiResponse);

            return new MissionResult
            {
                MissionCase = missionCase,
                Reasoning = aiResponse.Reasoning,
                Confidence = aiResponse.Confidence
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refining mission");
            throw;
        }
    }

    private MissionCaseDto MapToMissionCase(AIMissionResponse aiResponse)
    {
        return new MissionCaseDto
        {
            Id = Guid.NewGuid(),
            Name = aiResponse.Name,
            MissionType = aiResponse.MissionType,
            CargoBasis = aiResponse.CargoBasis,
            CargoValue = aiResponse.CargoValue,
            CargoDensityTPerM3 = aiResponse.CargoDensityTPerM3,
            ServiceSpeedKn = aiResponse.ServiceSpeedKn,
            SeaMarginPct = 15.0m, // Default
            CapBeamM = aiResponse.MaxBeamM,
            CapDraftM = aiResponse.MaxDraftM,
            Notes = $"AI Generated - {aiResponse.HullFamily ?? "Unknown"} hull family. Confidence: {aiResponse.Confidence:P0}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
