using Microsoft.Extensions.Options;
using PixelDisplay240Api.Models;

namespace PixelDisplay240Api.Endpoints;

/// <summary>
/// Extensões para mapear todos os endpoints da API
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Mapeia todos os endpoints de autenticação, perfil e governança
    /// </summary>
    public static void MapAllAuthEndpoints(this WebApplication app)
    {
        var authOptions = app.Services.GetRequiredService<IOptions<AuthOptions>>().Value;
        
        // Endpoints de Autenticação (Login, Registro, Recuperação de Senha)
        app.MapAuthEndpoints(authOptions);
        
        // Endpoints de Perfil do Usuário (Dados pessoais, alteração de senha)
        app.MapProfileEndpoints();
        
        // Endpoints de Governança (Aprovação/Revogação de usuários - Admin only)
        app.MapGovernanceEndpoints();
    }
}
