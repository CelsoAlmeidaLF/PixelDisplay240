using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Systekna.Kernel.Domain.Entities;
using Systekna.Kernel.Infrastructure.Data;
using System.Net;
using System.Text.Json;

namespace Systekna.Kernel.Infrastructure.Security;

/// <summary>
/// Middleware global para tratamento de exceções.
/// Em produção, oculta detalhes sensíveis das respostas de erro.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next, 
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context, AuthDbContext dbContext)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex, dbContext);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, AuthDbContext dbContext)
    {
        var userIdentifier = context.User?.Identity?.Name ?? "Anonymous";
        var endpoint = $"{context.Request.Method} {context.Request.Path}";
        var ip = GetClientIp(context);
        var traceId = context.TraceIdentifier;

        // Determinar status code baseado no tipo de exceção
        var (statusCode, publicMessage) = CategorizeException(exception);
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        // Logar erro no banco de dados
        await LogErrorToDatabase(dbContext, exception, endpoint, userIdentifier, ip, traceId);

        // Construir resposta segura
        object response;
        
        if (_environment.IsDevelopment())
        {
            // Em desenvolvimento, mostra detalhes completos
            response = new
            {
                error = publicMessage,
                details = exception.Message,
                type = exception.GetType().Name,
                stackTrace = exception.StackTrace,
                traceId = traceId,
                innerException = exception.InnerException?.Message
            };
        }
        else
        {
            // Em produção, oculta detalhes sensíveis
            response = new
            {
                error = publicMessage,
                traceId = traceId,
                support = "Se o problema persistir, entre em contato com o suporte informando o traceId."
            };
        }

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static (int StatusCode, string Message) CategorizeException(Exception exception)
    {
        return exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Acesso não autorizado."),
            ArgumentException => (StatusCodes.Status400BadRequest, "Requisição inválida."),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado."),
            InvalidOperationException => (StatusCodes.Status409Conflict, "Operação não permitida no estado atual."),
            TimeoutException => (StatusCodes.Status504GatewayTimeout, "A operação excedeu o tempo limite."),
            NotImplementedException => (StatusCodes.Status501NotImplemented, "Funcionalidade não implementada."),
            _ => (StatusCodes.Status500InternalServerError, "Ocorreu um erro interno no servidor.")
        };
    }

    private async Task LogErrorToDatabase(
        AuthDbContext dbContext, 
        Exception exception, 
        string endpoint, 
        string userIdentifier, 
        string? ip,
        string traceId)
    {
        try
        {
            var errorLog = new ErrorLog
            {
                Message = SanitizeForLog(exception.Message),
                StackTrace = exception.StackTrace,
                Endpoint = endpoint,
                Source = exception.Source ?? "Unknown",
                UserIdentifier = userIdentifier,
                Timestamp = DateTime.UtcNow,
                IpAddress = ip,
                TraceId = traceId
            };

            dbContext.ErrorLogs.Add(errorLog);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception dbEx)
        {
            _logger.LogCritical(dbEx, "Failed to log error to database. Original error: {Message}", exception.Message);
        }
    }

    private static string GetClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',').First().Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string SanitizeForLog(string message)
    {
        // Remove possíveis dados sensíveis do log
        var sanitized = message;
        
        // Remove possíveis passwords
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized, 
            @"(password|senha|pwd|secret|token|key|apikey|api_key)[\s]*[=:]+[\s]*[^\s,;]+",
            "$1=[REDACTED]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove possíveis connection strings
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"(Server|Data Source|Initial Catalog|User Id|Password)=[^;]+",
            "$1=[REDACTED]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return sanitized;
    }
}
