using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        // Serviços de domínio
        services.AddSingleton<Services.IPrototypeService, Services.PrototypeService>();
        services.AddSingleton<Services.IHardwareExportService, Services.HardwareExportService>();

        // Serviços de configuração
        services.AddSingleton<Services.IAgentConfigService>(sp =>
            new Services.AgentConfigService(configPath));

        // Serviços de log
        services.AddSingleton<Services.ILogService>(sp =>
            new Services.LogService(logsPath));

        // Serviço de IA (requer HttpClient)
        services.AddHttpClient<Services.IAIService, Services.AIService>(client =>
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
