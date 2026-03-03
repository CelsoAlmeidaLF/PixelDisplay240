using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Systekna.Kernel.Infrastructure.Security;

/// <summary>
/// Middleware que adiciona headers de segurança HTTP recomendados.
/// Implementa as melhores práticas da OWASP para proteção contra ataques comuns.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        SecurityHeadersOptions? options = null)
    {
        _next = next;
        _logger = logger;
        _options = options ?? new SecurityHeadersOptions();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Headers de segurança aplicados ANTES da resposta
        var headers = context.Response.Headers;

        // Previne clickjacking (ataques de iframe)
        if (_options.EnableFrameOptions)
        {
            headers["X-Frame-Options"] = _options.FrameOptions;
        }

        // Previne MIME-type sniffing
        if (_options.EnableContentTypeOptions)
        {
            headers["X-Content-Type-Options"] = "nosniff";
        }

        // Habilita proteção XSS do navegador
        if (_options.EnableXssProtection)
        {
            headers["X-XSS-Protection"] = "1; mode=block";
        }

        // Política de referrer
        if (_options.EnableReferrerPolicy)
        {
            headers["Referrer-Policy"] = _options.ReferrerPolicy;
        }

        // Content Security Policy
        if (_options.EnableContentSecurityPolicy && !string.IsNullOrEmpty(_options.ContentSecurityPolicy))
        {
            headers["Content-Security-Policy"] = _options.ContentSecurityPolicy;
        }

        // Permissions Policy (antigo Feature-Policy)
        if (_options.EnablePermissionsPolicy && !string.IsNullOrEmpty(_options.PermissionsPolicy))
        {
            headers["Permissions-Policy"] = _options.PermissionsPolicy;
        }

        // Strict Transport Security (HSTS) - apenas em HTTPS
        if (_options.EnableHsts && context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = $"max-age={_options.HstsMaxAge}; includeSubDomains; preload";
        }

        // Cache-Control para respostas sensíveis
        if (_options.EnableCacheControl && IsSensitivePath(context.Request.Path))
        {
            headers["Cache-Control"] = "no-store, no-cache, must-revalidate, proxy-revalidate";
            headers["Pragma"] = "no-cache";
            headers["Expires"] = "0";
        }

        // Remove headers que expõem informações do servidor
        if (_options.RemoveServerHeader)
        {
            headers.Remove("Server");
            headers.Remove("X-Powered-By");
            headers.Remove("X-AspNet-Version");
            headers.Remove("X-AspNetMvc-Version");
        }

        await _next(context);
    }

    private bool IsSensitivePath(PathString path)
    {
        var sensitivePaths = new[] { "/api/auth", "/api/admin", "/api/users", "/api/systems", "/Account" };
        return sensitivePaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Opções de configuração para o middleware de headers de segurança.
/// </summary>
public class SecurityHeadersOptions
{
    // Frame Options
    public bool EnableFrameOptions { get; set; } = true;
    public string FrameOptions { get; set; } = "DENY"; // DENY, SAMEORIGIN, ALLOW-FROM uri

    // Content Type Options
    public bool EnableContentTypeOptions { get; set; } = true;

    // XSS Protection
    public bool EnableXssProtection { get; set; } = true;

    // Referrer Policy
    public bool EnableReferrerPolicy { get; set; } = true;
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    // Content Security Policy
    public bool EnableContentSecurityPolicy { get; set; } = true;
    public string ContentSecurityPolicy { get; set; } = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdnjs.cloudflare.com https://unpkg.com; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com https://cdnjs.cloudflare.com; " +
        "font-src 'self' https://fonts.gstatic.com data:; " +
        "img-src 'self' data: blob: https:; " +
        "connect-src 'self' https://generativelanguage.googleapis.com; " +
        "frame-ancestors 'none'; " +
        "form-action 'self'; " +
        "base-uri 'self'";

    // Permissions Policy
    public bool EnablePermissionsPolicy { get; set; } = true;
    public string PermissionsPolicy { get; set; } = 
        "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()";

    // HSTS
    public bool EnableHsts { get; set; } = true;
    public int HstsMaxAge { get; set; } = 31536000; // 1 ano em segundos

    // Cache Control
    public bool EnableCacheControl { get; set; } = true;

    // Remove Server Headers
    public bool RemoveServerHeader { get; set; } = true;

    /// <summary>
    /// Configuração padrão para produção (mais restritiva)
    /// </summary>
    public static SecurityHeadersOptions Production => new()
    {
        FrameOptions = "DENY",
        EnableHsts = true,
        HstsMaxAge = 63072000, // 2 anos
        RemoveServerHeader = true
    };

    /// <summary>
    /// Configuração para desenvolvimento (menos restritiva)
    /// </summary>
    public static SecurityHeadersOptions Development => new()
    {
        EnableContentSecurityPolicy = false, // CSP pode atrapalhar hot-reload
        EnableHsts = false, // Não força HTTPS em dev
        FrameOptions = "SAMEORIGIN"
    };
}
