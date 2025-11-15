namespace AIAgentService.Services;

public class CostTrackingService
{
    private readonly ILogger<CostTrackingService> _logger;

    public CostTrackingService(ILogger<CostTrackingService> logger)
    {
        _logger = logger;
    }

    public void TrackUsage(string userId, string model, int promptTokens, int completionTokens)
    {
        var cost = CalculateCost(model, promptTokens, completionTokens);

        _logger.LogInformation(
            "AI Usage - User: {UserId}, Model: {Model}, Tokens: {PromptTokens}+{CompletionTokens}, Cost: ${Cost:F6}",
            userId,
            model,
            promptTokens,
            completionTokens,
            cost);
    }

    private decimal CalculateCost(string model, int promptTokens, int completionTokens)
    {
        var (promptCostPer1k, completionCostPer1k) = model.ToLower() switch
        {
            "gpt-4o-mini" => (0.00015m, 0.0006m),
            "gpt-4o" => (0.005m, 0.015m),
            _ => (0.001m, 0.002m)
        };

        var promptCost = (promptTokens / 1000m) * promptCostPer1k;
        var completionCost = (completionTokens / 1000m) * completionCostPer1k;

        return promptCost + completionCost;
    }
}












