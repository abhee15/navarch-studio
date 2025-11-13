namespace AIAgentService.Configuration;

public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public int MaxTokens { get; set; } = 2000;
    public double Temperature { get; set; } = 0.3;
    public int TimeoutSeconds { get; set; } = 30;
}








