using Microsoft.Extensions.DependencyInjection;
using Systekna.PixelDisplay.Application.Infrastructure.Interfaces;
using Systekna.PixelDisplay.Application.Infrastructure.Repositories;
using Systekna.PixelDisplay.Application.Services;

namespace Systekna.PixelDisplay.Application;

/// <summary>
/// Extensões para configuração de Dependency Injection.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adiciona os serviços da camada Application ao container de DI.
    /// </summary>
    /// <param name="services">Coleção de serviços</param>
    /// <param name="basePath">Caminho base para arquivos de configuração e logs</param>
    public static IServiceCollection AddPixelDisplayApplication(this IServiceCollection services, string basePath)
    {
        // Serviços de Aplicação
        services.AddSingleton<PrototypeApplicationService>();
        services.AddSingleton<HardwareExportApplicationService>();
        
        // Serviço de IA (requer HttpClient)
        services.AddHttpClient<IAIService, AIApplicationService>();
        
        // Repositórios
        services.AddSingleton<IAgentConfigRepository>(sp => new AgentConfigFileRepository(basePath));
        services.AddSingleton<ILogRepository>(sp => new LogFileRepository(basePath));

        return services;
    }
}
