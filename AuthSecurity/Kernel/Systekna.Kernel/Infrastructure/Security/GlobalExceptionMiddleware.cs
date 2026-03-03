using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Systekna.Kernel.Domain.Entities;
using Systekna.Kernel.Infrastructure.Data;
using System.Net;
using System.Text.Json;

namespace Systekna.Kernel.Infrastructure.Security;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AuthDbContext dbContext)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception has occurred.");
            await HandleExceptionAsync(context, ex, dbContext);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, AuthDbContext dbContext)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var userIdentifier = context.User?.Identity?.Name ?? "Anonymous";
        var endpoint = $"{context.Request.Method} {context.Request.Path}";
        var ip = context.Connection.RemoteIpAddress?.ToString();

        var errorLog = new ErrorLog
        {
            Message = exception.Message,
            StackTrace = exception.StackTrace,
            Endpoint = endpoint,
            Source = exception.Source,
            UserIdentifier = userIdentifier,
            Timestamp = DateTime.UtcNow,
            IpAddress = ip
        };

        try
        {
            dbContext.ErrorLogs.Add(errorLog);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception dbEx)
        {
            _logger.LogCritical(dbEx, "Failed to log error to database.");
        }

        var response = new
        {
            error = "Ocorreu um erro interno no servidor.",
            details = exception.Message, // Em produção, talvez queira esconder isso
            traceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(response);
        await context.Response.WriteAsync(json);
    }
}
