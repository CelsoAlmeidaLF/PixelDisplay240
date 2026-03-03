using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Systekna.Kernel.Infrastructure.Security;

/// <summary>
/// Middleware para proteção CSRF usando tokens de dupla submissão (Double Submit Cookie Pattern).
/// Mais adequado para APIs stateless do que o padrão anti-forgery do ASP.NET.
/// </summary>
public class ApiCsrfMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiCsrfMiddleware> _logger;
    private readonly CsrfOptions _options;

    private static readonly string[] SafeMethods = { "GET", "HEAD", "OPTIONS", "TRACE" };

    public ApiCsrfMiddleware(
        RequestDelegate next,
        ILogger<ApiCsrfMiddleware> logger,
        CsrfOptions? options = null)
    {
        _next = next;
        _logger = logger;
        _options = options ?? new CsrfOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Ignora métodos seguros (leitura)
        if (SafeMethods.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase))
        {
            // Garante que o cookie CSRF existe
            EnsureCsrfCookie(context);
            await _next(context);
            return;
        }

        // Ignora paths excluídos
        var path = context.Request.Path.Value ?? "";
        if (_options.ExcludedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Valida CSRF para métodos que modificam estado
        if (!ValidateCsrfToken(context))
        {
            _logger.LogWarning("CSRF validation failed for {Method} {Path}", 
                context.Request.Method, path);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new 
            { 
                error = "CSRF validation failed",
                code = "CSRF_INVALID",
                hint = $"Include the '{_options.HeaderName}' header with the value from '{_options.CookieName}' cookie"
            });
            return;
        }

        await _next(context);
    }

    private void EnsureCsrfCookie(HttpContext context)
    {
        if (!context.Request.Cookies.ContainsKey(_options.CookieName))
        {
            var token = GenerateCsrfToken();
            context.Response.Cookies.Append(_options.CookieName, token, new CookieOptions
            {
                HttpOnly = false, // JavaScript precisa ler para enviar no header
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                MaxAge = TimeSpan.FromHours(24),
                Path = "/"
            });
        }
    }

    private bool ValidateCsrfToken(HttpContext context)
    {
        // Obtém token do cookie
        if (!context.Request.Cookies.TryGetValue(_options.CookieName, out var cookieToken) ||
            string.IsNullOrEmpty(cookieToken))
        {
            return false;
        }

        // Obtém token do header
        if (!context.Request.Headers.TryGetValue(_options.HeaderName, out var headerToken) ||
            string.IsNullOrEmpty(headerToken))
        {
            return false;
        }

        // Compara tokens de forma segura (timing-safe)
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(cookieToken),
            Encoding.UTF8.GetBytes(headerToken.ToString())
        );
    }

    private static string GenerateCsrfToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_");
    }
}

/// <summary>
/// Opções de configuração para proteção CSRF
/// </summary>
public class CsrfOptions
{
    /// <summary>
    /// Nome do cookie que armazena o token CSRF
    /// </summary>
    public string CookieName { get; set; } = "X-CSRF-TOKEN";

    /// <summary>
    /// Nome do header que deve conter o token CSRF
    /// </summary>
    public string HeaderName { get; set; } = "X-CSRF-TOKEN";

    /// <summary>
    /// Paths excluídos da validação CSRF (ex: webhooks)
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/refresh",
        "/api/auth/forgot-password",
        "/api/auth/reset-password",
        "/api/public/",
        "/health",
        "/webhook"
    };

    /// <summary>
    /// Se deve exigir HTTPS para o cookie
    /// </summary>
    public bool RequireHttps { get; set; } = true;
}
