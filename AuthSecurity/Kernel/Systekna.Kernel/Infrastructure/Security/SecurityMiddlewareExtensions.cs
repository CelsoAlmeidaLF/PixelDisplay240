using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Systekna.Kernel.Infrastructure.Security;

/// <summary>
/// Extensões para configurar os middlewares de segurança do Systekna.
/// </summary>
public static class SecurityMiddlewareExtensions
{
    /// <summary>
    /// Adiciona os serviços de segurança necessários ao container de DI.
    /// </summary>
    public static IServiceCollection AddSysteknaSecurityServices(
        this IServiceCollection services,
        bool isDevelopment = false)
    {
        // Serviço de detecção de ameaças
        var threatOptions = isDevelopment 
            ? ThreatDetectionOptions.Development 
            : ThreatDetectionOptions.Default;
        
        services.AddSingleton<IThreatDetectionService>(new ThreatDetectionService(threatOptions));

        return services;
    }

    /// <summary>
    /// Configura todos os middlewares de segurança recomendados na ordem correta.
    /// Deve ser chamado ANTES de UseAuthentication e UseAuthorization.
    /// </summary>
    /// <param name="app">Application builder</param>
    /// <param name="environment">Ambiente de execução</param>
    /// <param name="enableCsrf">Habilitar proteção CSRF (recomendado para APIs com cookies)</param>
    /// <param name="enableThreatDetection">Habilitar detecção de ameaças</param>
    public static IApplicationBuilder UseSysteknaSecurityMiddlewares(
        this IApplicationBuilder app,
        IHostEnvironment environment,
        bool enableCsrf = false,
        bool enableThreatDetection = true)
    {
        // 1. Headers de segurança (sempre primeiro)
        var headerOptions = environment.IsDevelopment()
            ? SecurityHeadersOptions.Development
            : SecurityHeadersOptions.Production;
        
        app.UseMiddleware<SecurityHeadersMiddleware>(headerOptions);

        // 2. Detecção de ameaças
        if (enableThreatDetection)
        {
            app.UseMiddleware<ThreatDetectionMiddleware>();
        }

        // 3. Proteção CSRF (opcional, para APIs que usam cookies de sessão)
        if (enableCsrf)
        {
            app.UseMiddleware<ApiCsrfMiddleware>();
        }

        // 4. Tratamento global de exceções
        app.UseMiddleware<GlobalExceptionMiddleware>();

        return app;
    }

    /// <summary>
    /// Versão simplificada que aplica segurança padrão baseada no ambiente.
    /// </summary>
    public static IApplicationBuilder UseSysteknaSecurityDefaults(
        this IApplicationBuilder app,
        IHostEnvironment environment)
    {
        return app.UseSysteknaSecurityMiddlewares(
            environment,
            enableCsrf: false, // CSRF geralmente não é necessário para APIs JWT puras
            enableThreatDetection: !environment.IsDevelopment()
        );
    }

    /// <summary>
    /// Aplica configuração de segurança estrita para produção.
    /// </summary>
    public static IApplicationBuilder UseSysteknaSecurityStrict(
        this IApplicationBuilder app,
        IHostEnvironment environment)
    {
        return app.UseSysteknaSecurityMiddlewares(
            environment,
            enableCsrf: true,
            enableThreatDetection: true
        );
    }
}
