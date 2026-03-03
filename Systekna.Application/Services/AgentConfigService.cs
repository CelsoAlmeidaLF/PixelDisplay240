using System.Text.Json;
using Systekna.Application.Domain.Entities;

namespace Systekna.Application.Services;

/// <summary>
/// Interface para o serviço de configuração de agentes.
/// </summary>
public interface IAgentConfigService
{
    AgentConfig LoadConfig();
    void SaveConfig(AgentConfig config);
    string? GetApiKey(string provider = "gemini");
    void SetApiKey(string provider, string apiKey);
}

/// <summary>
/// Serviço de configuração de agentes de IA.
/// Gerencia chaves de API e configurações de provedores.
/// </summary>
public class AgentConfigService : IAgentConfigService
{
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private AgentConfig? _cachedConfig;

    public AgentConfigService(string configPath)
    {
        _configPath = configPath;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };
    }

    public AgentConfig LoadConfig()
    {
        if (_cachedConfig != null) return _cachedConfig;

        if (!File.Exists(_configPath))
        {
            _cachedConfig = new AgentConfig();
            return _cachedConfig;
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            _cachedConfig = JsonSerializer.Deserialize<AgentConfig>(json, _jsonOptions) ?? new AgentConfig();
            return _cachedConfig;
        }
        catch
        {
            _cachedConfig = new AgentConfig();
            return _cachedConfig;
        }
    }

    public void SaveConfig(AgentConfig config)
    {
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        File.WriteAllText(_configPath, json);
        _cachedConfig = config;
    }

    public string? GetApiKey(string provider = "gemini")
    {
        var config = LoadConfig();
        return provider.ToLower() switch
        {
            "gemini" => config.Gemini.ApiKey,
            _ => null
        };
    }

    public void SetApiKey(string provider, string apiKey)
    {
        var config = LoadConfig();
        switch (provider.ToLower())
        {
            case "gemini":
                config.Gemini.ApiKey = apiKey;
                break;
        }
        SaveConfig(config);
    }
}
