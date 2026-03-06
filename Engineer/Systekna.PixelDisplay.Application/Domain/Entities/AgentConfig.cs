namespace Systekna.PixelDisplay.Application.Domain.Entities;

/// <summary>
/// Configuração dos agentes de IA.
/// </summary>
public sealed class AgentConfig
{
    public GeminiConfig Gemini { get; set; } = new();
}

/// <summary>
/// Configuração específica do Gemini.
/// </summary>
public sealed class GeminiConfig
{
    public string ApiKey { get; set; } = string.Empty;
}
