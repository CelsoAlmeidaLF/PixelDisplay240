namespace PixelDisplay240Api.Models
{
    public class AgentConfig
    {
        public GeminiConfig Gemini { get; set; } = new();
    }

    public class GeminiConfig
    {
        public string ApiKey { get; set; } = string.Empty;
    }
}
