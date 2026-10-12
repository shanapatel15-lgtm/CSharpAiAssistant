namespace CSharpAiAssistant.Services;

public sealed class AiOptions
{
    public string Provider { get; set; } = "OpenAI";
    public string Model { get; set; } = "gpt-4o-mini";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = "You are a helpful AI assistant.";
    public bool UseDemoModeWhenNoApiKey { get; set; } = true;
}
