using System.Text.Json;
using Systekna.PixelDisplay.Application.Infrastructure.Interfaces;

namespace Systekna.PixelDisplay.Application.Infrastructure.Repositories;

/// <summary>
/// Repositório para logs de erro.
/// Implementação baseada em arquivos JSON.
/// </summary>
public class LogFileRepository : ILogRepository
{
    private readonly string _logsPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public LogFileRepository(string basePath)
    {
        _logsPath = Path.Combine(basePath, "logs");
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        if (!Directory.Exists(_logsPath))
        {
            Directory.CreateDirectory(_logsPath);
        }
    }

    public void SaveError(string context, string type, string message, string data)
    {
        var logEntry = new
        {
            Timestamp = DateTime.UtcNow,
            Context = context,
            Type = type,
            Message = message,
            Data = data
        };

        var fileName = $"{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss-fff}_{type}.json";
        var filePath = Path.Combine(_logsPath, fileName);

        var json = JsonSerializer.Serialize(logEntry, _jsonOptions);
        File.WriteAllText(filePath, json);
    }
}
