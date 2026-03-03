using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Systekna.Application.Services;

namespace Systekna.Application.Extensions;

/// <summary>
/// Extensões para configurar os serviços do PixelDisplay240 na aplicação.
/// </summary>
public static class PixelDisplayServiceExtensions
{
    /// <summary>
    /// Adiciona todos os serviços de aplicação do PixelDisplay240.
    /// </summary>
    /// <param name="services">Container de DI</param>
    /// <param name="configPath">Caminho para o arquivo de configuração de agentes</param>
    /// <param name="logsPath">Caminho para a pasta de logs</param>
    /// <returns>IServiceCollection para encadeamento</returns>
    public static IServiceCollection AddPixelDisplayServices(
        this IServiceCollection services,
        string configPath,
        string logsPath)
    {
        // Serviços de domínio (agora com suporte a ILogger via DI)
        services.AddSingleton<IPrototypeService, PrototypeService>();
        services.AddSingleton<IHardwareExportService, HardwareExportService>();

        // Serviços de configuração
        services.AddSingleton<IAgentConfigService>(sp =>
            new AgentConfigService(configPath));

        // Serviços de log
        services.AddSingleton<ILogService>(sp =>
            new LogService(logsPath));

        // Serviço de persistência de projetos
        services.AddSingleton<IProjectPersistenceService, FileProjectPersistenceService>();

        // Serviço de IA (requer HttpClient e ILogger)
        services.AddHttpClient<IAIService, AIService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    /// <summary>
    /// Adiciona os serviços do PixelDisplay240 usando caminhos padrão baseados no ambiente.
    /// </summary>
    public static IServiceCollection AddPixelDisplayServices(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        var contentRoot = environment.ContentRootPath;
        var configPath = Path.Combine(contentRoot, "agent-config.json");
        var logsPath = Path.Combine(contentRoot, "logs");

        return services.AddPixelDisplayServices(configPath, logsPath);
    }
}
