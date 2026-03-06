using PixelDisplay240Api.Models;
using System.Text.Json;

namespace PixelDisplay240Api.Services;

public class AgentConfigService
{
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public AgentConfigService(IWebHostEnvironment env)
    {
        _configPath = Path.Combine(env.ContentRootPath, "agent-config.json");
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