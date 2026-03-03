namespace Systekna.Application.Domain.Entities;

/// <summary>
/// Configuração de agentes de IA do sistema.
/// </summary>
public class AgentConfig
{
    public GeminiConfig Gemini { get; set; } = new();
}

/// <summary>
/// Configurações específicas do Gemini AI.
/// </summary>
public class GeminiConfig
{
    public string ApiKey { get; set; } = string.Empty;
}
