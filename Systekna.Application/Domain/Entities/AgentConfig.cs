namespace Systekna.Application.Domain.Entities;

/// <summary>
/// Configuração de agentes de IA e preferências do sistema.
/// </summary>
public class AgentConfig
{
    /// <summary>
    /// Configurações do Gemini AI.
    /// </summary>
    public GeminiConfig Gemini { get; set; } = new();
    
    /// <summary>
    /// Tema da interface (dark, light).
    /// </summary>
    public string Theme { get; set; } = "dark";
    
    /// <summary>
    /// Idioma da interface (pt-BR, en-US, es-ES).
    /// </summary>
    public string Language { get; set; } = "pt-BR";
    
    /// <summary>
    /// Habilita salvamento automático do projeto.
    /// </summary>
    public bool AutoSave { get; set; } = true;
}

/// <summary>
/// Configurações específicas do Gemini AI.
/// </summary>
public class GeminiConfig
{
    /// <summary>
    /// Chave de API do Google Gemini.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Modelo a ser usado para geração de imagens.
    /// </summary>
    public string ImageModel { get; set; } = "gemini-2.5-flash-image";
    
    /// <summary>
    /// Modelo a ser usado para geração de texto.
    /// </summary>
    public string TextModel { get; set; } = "gemini-1.5-flash";
}
