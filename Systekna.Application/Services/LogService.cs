using System.Text.Json;

namespace Systekna.Application.Services;

/// <summary>
/// Interface para o serviço de logs.
/// </summary>
public interface ILogService
{
    void SaveError(string context, string type, string message, string data);
    void SaveInfo(string context, string message);
    void SaveWarning(string context, string message);
}

/// <summary>
/// Serviço de logs para arquivos JSON.
/// </summary>
public class LogService : ILogService
{
    private readonly string _logsPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public LogService(string logsPath)
    {
        _logsPath = logsPath;
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
        SaveLog(context, type, message, data, "ERROR");
    }

    public void SaveInfo(string context, string message)
    {
        SaveLog(context, "INFO", message, null, "INFO");
    }

    public void SaveWarning(string context, string message)
    {
        SaveLog(context, "WARNING", message, null, "WARNING");
    }

    private void SaveLog(string context, string type, string message, string? data, string level)
    {
        try
        {
            var logEntry = new
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Context = context,
                Type = type,
                Message = message,
                Data = data
            };

            var fileName = $"{DateTime.UtcNow:yyyy-MM-dd_HH-mm-ss-fff}_{level}.json";
            var filePath = Path.Combine(_logsPath, fileName);

            var json = JsonSerializer.Serialize(logEntry, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LogService] Failed to save log: {ex.Message}");
        }
    }
}
