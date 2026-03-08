using System.Text.Json;
using Systekna.PixelDisplay.Application.Domain.Entities;
using Systekna.PixelDisplay.Application.Infrastructure.Interfaces;

namespace Systekna.PixelDisplay.Application.Infrastructure.Repositories;

/// <summary>
/// Repositório para configuração de agentes.
/// Implementação baseada em arquivo JSON.
/// </summary>
public class AgentConfigFileRepository : IAgentConfigRepository
{
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public AgentConfigFileRepository(string basePath)
    {
        _configPath = Path.Combine(basePath, "agent-config.json");
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
    }

    public AgentConfig LoadConfig()
    {
        if (!File.Exists(_configPath))
        {
            return new AgentConfig();
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<AgentConfig>(json, _jsonOptions) ?? new AgentConfig();
        }
        catch
        {
            return new AgentConfig();
        }
    }

    public void SaveConfig(AgentConfig config)
    {
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        File.WriteAllText(_configPath, json);
    }
}
