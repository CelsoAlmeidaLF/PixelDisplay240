namespace PixelDisplay240Api.Models;

public sealed class AgentConfig
{
    public GeminiConfig Gemini { get; set; } = new();
}

public sealed class GeminiConfig
{
    public string ApiKey { get; set; } = string.Empty;
}
