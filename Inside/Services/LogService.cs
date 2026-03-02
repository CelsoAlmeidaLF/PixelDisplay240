using System;
using System.IO;

namespace PixelDisplay240Api.Services;

public class LogService
{
    private readonly string _logPath;
    private readonly object _lock = new();

    public LogService(IWebHostEnvironment env)
    {
        var logDir = Path.Combine(env.ContentRootPath, "log");
        if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
        _logPath = Path.Combine(logDir, "error.log");
    }

    public void SaveError(string context, string type, string message, string data)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var logLine = $"[{timestamp}] [{context}] {type}: {message}\nData: {data}\n{new string('-', 40)}\n";

        lock (_lock)
        {
            File.AppendAllText(_logPath, logLine);
        }
    }
}
