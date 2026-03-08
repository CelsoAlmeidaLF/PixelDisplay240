using Systekna.PixelDisplay.Application.Domain.Entities;

namespace Systekna.PixelDisplay.Application.Infrastructure.Interfaces;

/// <summary>
/// Interface para serviço de configuração de agentes.
/// </summary>
public interface IAgentConfigRepository
{
    /// <summary>
    /// Carrega a configuração dos agentes.
    /// </summary>
    AgentConfig LoadConfig();
    
    /// <summary>
    /// Salva a configuração dos agentes.
    /// </summary>
    void SaveConfig(AgentConfig config);
}
