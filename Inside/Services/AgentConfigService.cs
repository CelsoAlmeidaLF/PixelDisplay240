using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using PixelDisplay240Api.Models;

namespace PixelDisplay240Api.Services;

public class AgentConfigService
{
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AgentConfigService(IWebHostEnvironment env)
    {
        _configPath = Path.Combine(env.ContentRootPath, "config.json");
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        // Chave de criptografia fixa para o sistema
        string secret = "PixelDisplay240_Main_Key_2026";
        using var sha256 = SHA256.Create();
        _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(secret));
        _iv = new byte[16];
        Array.Copy(_key, _iv, 16);
    }

    public AgentConfig LoadConfig()
    {
        if (!File.Exists(_configPath)) return new AgentConfig();

        var json = File.ReadAllText(_configPath);
        var config = JsonSerializer.Deserialize<AgentConfig>(json, _jsonOptions) ?? new AgentConfig();
        
        if (!string.IsNullOrEmpty(config.Gemini.ApiKey))
        {
            config.Gemini.ApiKey = Decrypt(config.Gemini.ApiKey);
        }
        
        return config;
    }

    public void SaveConfig(AgentConfig config)
    {
        var clone = new AgentConfig { Gemini = new GeminiConfig { ApiKey = config.Gemini.ApiKey } };
        if (!string.IsNullOrEmpty(clone.Gemini.ApiKey))
        {
            clone.Gemini.ApiKey = Encrypt(clone.Gemini.ApiKey);
        }

        var json = JsonSerializer.Serialize(clone, _jsonOptions);
        File.WriteAllText(_configPath, json);
    }

    private string Encrypt(string text)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        {
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(text);
            }
        }
        return Convert.ToBase64String(ms.ToArray());
    }

    private string Decrypt(string cipher)
    {
        if (string.IsNullOrWhiteSpace(cipher)) return string.Empty;
        try 
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;
            
            using var ms = new MemoryStream(Convert.FromBase64String(cipher));
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        } 
        catch 
        { 
            return string.Empty; // Return empty instead of "[Criptografado]" to avoid UI showing garbage
        }
    }
}
