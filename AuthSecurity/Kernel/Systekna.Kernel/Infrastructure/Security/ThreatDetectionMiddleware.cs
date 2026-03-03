using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace Systekna.Kernel.Infrastructure.Security;

/// <summary>
/// Middleware para detecção de atividades suspeitas e proteção contra ataques.
/// Monitora padrões de requisições e bloqueia comportamentos anômalos.
/// </summary>
public class ThreatDetectionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ThreatDetectionMiddleware> _logger;
    private readonly IThreatDetectionService _threatService;

    public ThreatDetectionMiddleware(
        RequestDelegate next,
        ILogger<ThreatDetectionMiddleware> logger,
        IThreatDetectionService threatService)
    {
        _next = next;
        _logger = logger;
        _threatService = threatService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientIp = GetClientIp(context);
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Verificar se o IP está na lista de bloqueio
        if (_threatService.IsBlocked(clientIp))
        {
            _logger.LogWarning("Blocked request from banned IP: {IP}", clientIp);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Access denied", code = "BLOCKED" });
            return;
        }

        // Detectar padrões de ataque
        var threatLevel = _threatService.AnalyzeRequest(clientIp, path, method, context.Request.Headers);

        if (threatLevel >= ThreatLevel.High)
        {
            _logger.LogWarning("High threat level detected from IP: {IP}, Path: {Path}, Level: {Level}", 
                clientIp, path, threatLevel);

            if (threatLevel == ThreatLevel.Critical)
            {
                _threatService.BlockIp(clientIp, TimeSpan.FromHours(24));
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Access denied", code = "THREAT_DETECTED" });
                return;
            }
        }

        // Registrar requisição para análise de padrões
        _threatService.RecordRequest(clientIp, path, method, userId);

        // Adicionar headers de rastreamento
        context.Response.Headers["X-Request-Id"] = context.TraceIdentifier;

        await _next(context);

        // Registrar resposta para detecção de anomalias
        _threatService.RecordResponse(clientIp, context.Response.StatusCode);
    }

    private static string GetClientIp(HttpContext context)
    {
        // Verificar headers de proxy
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

/// <summary>
/// Serviço de detecção de ameaças
/// </summary>
public interface IThreatDetectionService
{
    bool IsBlocked(string identifier);
    void BlockIp(string ip, TimeSpan duration);
    ThreatLevel AnalyzeRequest(string ip, string path, string method, IHeaderDictionary headers);
    void RecordRequest(string ip, string path, string method, string? userId);
    void RecordResponse(string ip, int statusCode);
    ThreatStats GetStats();
}

public class ThreatDetectionService : IThreatDetectionService
{
    private readonly ConcurrentDictionary<string, BlockInfo> _blockedIps = new();
    private readonly ConcurrentDictionary<string, RequestPattern> _requestPatterns = new();
    private readonly ConcurrentDictionary<string, int> _failedRequests = new();
    private readonly ThreatDetectionOptions _options;
    private readonly Timer _cleanupTimer;

    // Padrões de ataque conhecidos
    private static readonly string[] SqlInjectionPatterns = {
        "union select", "' or '1'='1", "'; drop", "exec(", "execute(",
        "xp_cmdshell", "sp_executesql", "--", "/*", "*/", "@@version"
    };

    private static readonly string[] XssPatterns = {
        "<script", "javascript:", "onerror=", "onload=", "onclick=",
        "onmouseover=", "onfocus=", "onblur=", "eval(", "document.cookie"
    };

    private static readonly string[] PathTraversalPatterns = {
        "../", "..\\", "%2e%2e", "%252e", "/etc/passwd", "/proc/self",
        "\\windows\\", "\\system32\\", "file://", "php://", "data://"
    };

    private static readonly string[] SuspiciousUserAgents = {
        "sqlmap", "nikto", "nmap", "masscan", "dirbuster", "gobuster",
        "nuclei", "wfuzz", "ffuf", "burp", "zaproxy", "havij"
    };

    public ThreatDetectionService(ThreatDetectionOptions? options = null)
    {
        _options = options ?? new ThreatDetectionOptions();
        _cleanupTimer = new Timer(Cleanup, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public bool IsBlocked(string identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return false;

        if (_blockedIps.TryGetValue(identifier, out var blockInfo))
        {
            if (blockInfo.ExpiresAt > DateTime.UtcNow)
                return true;

            _blockedIps.TryRemove(identifier, out _);
        }

        return false;
    }

    public void BlockIp(string ip, TimeSpan duration)
    {
        _blockedIps[ip] = new BlockInfo
        {
            Ip = ip,
            BlockedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(duration),
            Reason = "Threat detected"
        };
    }

    public ThreatLevel AnalyzeRequest(string ip, string path, string method, IHeaderDictionary headers)
    {
        var score = 0;

        // Verificar User-Agent suspeito
        var userAgent = headers.UserAgent.ToString().ToLowerInvariant();
        if (SuspiciousUserAgents.Any(ua => userAgent.Contains(ua)))
        {
            score += 50;
        }

        // Verificar path para padrões de ataque
        var pathLower = path.ToLowerInvariant();
        
        if (SqlInjectionPatterns.Any(p => pathLower.Contains(p)))
            score += 80;

        if (XssPatterns.Any(p => pathLower.Contains(p)))
            score += 70;

        if (PathTraversalPatterns.Any(p => pathLower.Contains(p)))
            score += 90;

        // Verificar query string
        var queryString = headers.TryGetValue("Query", out var queryValues) 
            ? queryValues.ToString().ToLowerInvariant() 
            : "";
        if (SqlInjectionPatterns.Any(p => queryString.Contains(p)))
            score += 80;

        // Verificar taxa de requisições
        if (_requestPatterns.TryGetValue(ip, out var pattern))
        {
            var recentRequests = pattern.RequestsInWindow;
            if (recentRequests > _options.MaxRequestsPerMinute)
            {
                score += 30;
            }
            if (recentRequests > _options.MaxRequestsPerMinute * 3)
            {
                score += 50;
            }
        }

        // Verificar taxa de erros
        if (_failedRequests.TryGetValue(ip, out var failures) && failures > _options.MaxFailuresBeforeBlock)
        {
            score += 40;
        }

        // Headers ausentes ou suspeitos
        if (string.IsNullOrEmpty(userAgent))
            score += 20;

        if (headers.ContainsKey("X-Forwarded-For") && headers["X-Forwarded-For"].Count > 5)
            score += 30; // Possível header spoofing

        return score switch
        {
            >= 100 => ThreatLevel.Critical,
            >= 70 => ThreatLevel.High,
            >= 40 => ThreatLevel.Medium,
            >= 20 => ThreatLevel.Low,
            _ => ThreatLevel.None
        };
    }

    public void RecordRequest(string ip, string path, string method, string? userId)
    {
        var now = DateTime.UtcNow;

        _requestPatterns.AddOrUpdate(ip,
            _ => new RequestPattern
            {
                Ip = ip,
                FirstRequest = now,
                LastRequest = now,
                RequestsInWindow = 1,
                UniquePathsAccessed = new HashSet<string> { path }
            },
            (_, existing) =>
            {
                // Reset se passou da janela de tempo
                if (now - existing.FirstRequest > TimeSpan.FromMinutes(1))
                {
                    return new RequestPattern
                    {
                        Ip = ip,
                        FirstRequest = now,
                        LastRequest = now,
                        RequestsInWindow = 1,
                        UniquePathsAccessed = new HashSet<string> { path }
                    };
                }

                existing.LastRequest = now;
                existing.RequestsInWindow++;
                existing.UniquePathsAccessed.Add(path);
                return existing;
            });
    }

    public void RecordResponse(string ip, int statusCode)
    {
        // Rastrear erros 4xx e 5xx
        if (statusCode >= 400)
        {
            _failedRequests.AddOrUpdate(ip, 1, (_, count) => count + 1);
        }
        else
        {
            // Reset em caso de sucesso
            _failedRequests.TryRemove(ip, out _);
        }
    }

    public ThreatStats GetStats()
    {
        return new ThreatStats
        {
            BlockedIps = _blockedIps.Count,
            TrackedIps = _requestPatterns.Count,
            TotalBlockedToday = _blockedIps.Count(b => b.Value.BlockedAt.Date == DateTime.UtcNow.Date)
        };
    }

    private void Cleanup(object? state)
    {
        var now = DateTime.UtcNow;

        // Limpar bloqueios expirados
        foreach (var key in _blockedIps.Keys.ToList())
        {
            if (_blockedIps.TryGetValue(key, out var info) && info.ExpiresAt < now)
            {
                _blockedIps.TryRemove(key, out _);
            }
        }

        // Limpar padrões antigos
        foreach (var key in _requestPatterns.Keys.ToList())
        {
            if (_requestPatterns.TryGetValue(key, out var pattern) && 
                now - pattern.LastRequest > TimeSpan.FromMinutes(15))
            {
                _requestPatterns.TryRemove(key, out _);
            }
        }

        // Limpar contagem de falhas
        _failedRequests.Clear();
    }
}

public enum ThreatLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public class BlockInfo
{
    public string Ip { get; set; } = string.Empty;
    public DateTime BlockedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class RequestPattern
{
    public string Ip { get; set; } = string.Empty;
    public DateTime FirstRequest { get; set; }
    public DateTime LastRequest { get; set; }
    public int RequestsInWindow { get; set; }
    public HashSet<string> UniquePathsAccessed { get; set; } = new();
}

public class ThreatStats
{
    public int BlockedIps { get; set; }
    public int TrackedIps { get; set; }
    public int TotalBlockedToday { get; set; }
}

public class ThreatDetectionOptions
{
    public int MaxRequestsPerMinute { get; set; } = 120;
    public int MaxFailuresBeforeBlock { get; set; } = 10;
    public bool EnableSqlInjectionDetection { get; set; } = true;
    public bool EnableXssDetection { get; set; } = true;
    public bool EnablePathTraversalDetection { get; set; } = true;
    public bool EnableRateLimitDetection { get; set; } = true;

    public static ThreatDetectionOptions Default => new();

    public static ThreatDetectionOptions Strict => new()
    {
        MaxRequestsPerMinute = 60,
        MaxFailuresBeforeBlock = 5
    };

    public static ThreatDetectionOptions Development => new()
    {
        MaxRequestsPerMinute = 1000,
        MaxFailuresBeforeBlock = 100
    };
}
