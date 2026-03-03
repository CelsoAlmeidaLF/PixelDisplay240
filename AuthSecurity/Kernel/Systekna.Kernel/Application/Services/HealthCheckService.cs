using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Systekna.Kernel.Infrastructure.Data;

namespace Systekna.Kernel.Application.Services;

/// <summary>
/// Serviço de verificação de saúde do sistema centralizado.
/// Verifica banco de dados, serviços externos e recursos do sistema.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AuthDbContext _context;

    public DatabaseHealthCheck(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Tenta executar uma query simples
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);
            
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Não foi possível conectar ao banco de dados.");
            }

            // Verifica se a tabela de usuários está acessível
            var userCount = await _context.Users.CountAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                { "provider", _context.Database.ProviderName ?? "Unknown" },
                { "userCount", userCount }
            };

            return HealthCheckResult.Healthy("Banco de dados está funcionando.", data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Erro ao verificar o banco de dados.",
                exception: ex);
        }
    }
}

/// <summary>
/// Health check para verificar o serviço de detecção de ameaças
/// </summary>
public class SecurityServicesHealthCheck : IHealthCheck
{
    private readonly ILoginRateLimiter _rateLimiter;

    public SecurityServicesHealthCheck(ILoginRateLimiter rateLimiter)
    {
        _rateLimiter = rateLimiter;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verifica se o rate limiter está funcionando
            var testResult = _rateLimiter.IsBlocked("health-check-test");
            
            var data = new Dictionary<string, object>
            {
                { "rateLimiterActive", true }
            };

            return Task.FromResult(HealthCheckResult.Healthy("Serviços de segurança estão funcionando.", data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Erro nos serviços de segurança.",
                exception: ex));
        }
    }
}

/// <summary>
/// Health check para recursos do sistema (memória, etc.)
/// </summary>
public class SystemResourcesHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var memoryMb = process.WorkingSet64 / (1024 * 1024);
            var gcMemory = GC.GetTotalMemory(false) / (1024 * 1024);

            var data = new Dictionary<string, object>
            {
                { "processMemoryMB", memoryMb },
                { "gcMemoryMB", gcMemory },
                { "threadCount", process.Threads.Count },
                { "uptime", DateTime.Now - process.StartTime }
            };

            // Alerta se usar mais de 500MB
            if (memoryMb > 500)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    "Uso de memória elevado.",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                "Recursos do sistema estão normais.",
                data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Erro ao verificar recursos do sistema.",
                exception: ex));
        }
    }
}
