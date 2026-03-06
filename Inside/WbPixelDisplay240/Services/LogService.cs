
using System.Text.Json;

namespace PixelDisplay240Api.Services;

public class LogService
{
    private readonly string _logsPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public LogService(IWebHostEnvironment env)
    {
        _logsPath = Path.Combine(env.ContentRootPath, "logs");
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
