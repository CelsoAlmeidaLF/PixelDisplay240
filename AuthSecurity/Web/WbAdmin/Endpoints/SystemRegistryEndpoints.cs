using Systekna.Kernel.Domain.DTOs;
using Systekna.Kernel.Domain.Interfaces;
using System.Security.Claims;

namespace WbGovAdmin.Endpoints;

/// <summary>
/// Endpoints para gerenciamento de sistemas cadastrados e controle de acesso de usuários.
/// </summary>
public static class SystemRegistryEndpoints
{
    public static void MapSystemRegistryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/systems")
            .WithTags("System Registry")
            .RequireAuthorization("AdminOnly");

        // ========================================
        // CRUD de Sistemas
        // ========================================

        /// <summary>
        /// Lista todos os sistemas cadastrados
        /// </summary>
        group.MapGet("/", async (ISystemRegistryService service) =>
        {
            var systems = await service.GetAllSystemsAsync();
            return Results.Ok(systems);
        })
        .WithName("GetAllSystems")
        .WithSummary("Lista todos os sistemas cadastrados");

        /// <summary>
        /// Obtém um sistema por ID
        /// </summary>
        group.MapGet("/{id:int}", async (int id, ISystemRegistryService service) =>
        {
            var system = await service.GetSystemByIdAsync(id);
            return system != null ? Results.Ok(system) : Results.NotFound();
        })
        .WithName("GetSystemById")
        .WithSummary("Obtém um sistema por ID");

        /// <summary>
        /// Obtém um sistema por código
        /// </summary>
        group.MapGet("/code/{systemCode}", async (string systemCode, ISystemRegistryService service) =>
        {
            var system = await service.GetSystemByCodeAsync(systemCode);
            return system != null ? Results.Ok(system) : Results.NotFound();
        })
        .WithName("GetSystemByCode")
        .WithSummary("Obtém um sistema por código");

        /// <summary>
        /// Cria um novo sistema
        /// </summary>
        group.MapPost("/", async (CreateSystemRequest request, ISystemRegistryService service) =>
        {
            try
            {
                var system = await service.CreateSystemAsync(request);
                return Results.Created($"/api/systems/{system.Id}", system);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("CreateSystem")
        .WithSummary("Cria um novo sistema");

        /// <summary>
        /// Atualiza um sistema existente
        /// </summary>
        group.MapPut("/{id:int}", async (int id, UpdateSystemRequest request, ISystemRegistryService service) =>
        {
            var system = await service.UpdateSystemAsync(id, request);
            return system != null ? Results.Ok(system) : Results.NotFound();
        })
        .WithName("UpdateSystem")
        .WithSummary("Atualiza um sistema existente");

        /// <summary>
        /// Exclui um sistema
        /// </summary>
        group.MapDelete("/{id:int}", async (int id, ISystemRegistryService service) =>
        {
            var result = await service.DeleteSystemAsync(id);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteSystem")
        .WithSummary("Exclui um sistema");

        /// <summary>
        /// Ativa/Desativa um sistema
        /// </summary>
        group.MapPost("/{id:int}/toggle-status", async (int id, ISystemRegistryService service) =>
        {
            var result = await service.ToggleSystemStatusAsync(id);
            return result ? Results.Ok(new { message = "Status alterado com sucesso" }) : Results.NotFound();
        })
        .WithName("ToggleSystemStatus")
        .WithSummary("Ativa ou desativa um sistema");

        /// <summary>
        /// Regenera a chave de API do sistema
        /// </summary>
        group.MapPost("/{id:int}/regenerate-secret", async (int id, ISystemRegistryService service) =>
        {
            try
            {
                var newSecret = await service.RegenerateApiSecretAsync(id);
                return Results.Ok(new { apiSecret = newSecret });
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .WithName("RegenerateApiSecret")
        .WithSummary("Regenera a chave de API do sistema");

        // ========================================
        // Estatísticas de Sistemas
        // ========================================

        /// <summary>
        /// Obtém estatísticas de todos os sistemas
        /// </summary>
        group.MapGet("/stats", async (ISystemRegistryService service) =>
        {
            var stats = await service.GetAllSystemsStatsAsync();
            return Results.Ok(stats);
        })
        .WithName("GetAllSystemsStats")
        .WithSummary("Obtém estatísticas de todos os sistemas");

        /// <summary>
        /// Obtém estatísticas de um sistema específico
        /// </summary>
        group.MapGet("/{id:int}/stats", async (int id, ISystemRegistryService service) =>
        {
            var stats = await service.GetSystemStatsAsync(id);
            return stats != null ? Results.Ok(stats) : Results.NotFound();
        })
        .WithName("GetSystemStats")
        .WithSummary("Obtém estatísticas de um sistema específico");

        // ========================================
        // Gerenciamento de Usuários por Sistema
        // ========================================

        /// <summary>
        /// Lista todos os usuários de um sistema
        /// </summary>
        group.MapGet("/{systemId:int}/users", async (int systemId, ISystemRegistryService service) =>
        {
            var users = await service.GetSystemUsersAsync(systemId);
            return Results.Ok(users);
        })
        .WithName("GetSystemUsers")
        .WithSummary("Lista todos os usuários de um sistema");

        /// <summary>
        /// Lista solicitações de acesso pendentes de um sistema
        /// </summary>
        group.MapGet("/{systemId:int}/pending", async (int systemId, ISystemRegistryService service) =>
        {
            var pending = await service.GetPendingAccessRequestsAsync(systemId);
            return Results.Ok(pending);
        })
        .WithName("GetPendingAccessRequests")
        .WithSummary("Lista solicitações de acesso pendentes de um sistema");

        /// <summary>
        /// Concede acesso a um usuário em um sistema
        /// </summary>
        group.MapPost("/access/grant", async (GrantSystemAccessRequest request, HttpContext http, ISystemRegistryService service) =>
        {
            var adminId = GetUserIdFromClaims(http);
            if (adminId == null) return Results.Unauthorized();

            try
            {
                var access = await service.GrantAccessAsync(request, adminId.Value);
                return Results.Created($"/api/systems/access/{access.Id}", access);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("GrantSystemAccess")
        .WithSummary("Concede acesso a um usuário em um sistema");

        /// <summary>
        /// Atualiza acesso de um usuário
        /// </summary>
        group.MapPut("/access/{accessId:int}", async (int accessId, UpdateUserAccessRequest request, ISystemRegistryService service) =>
        {
            var access = await service.UpdateAccessAsync(accessId, request);
            return access != null ? Results.Ok(access) : Results.NotFound();
        })
        .WithName("UpdateUserAccess")
        .WithSummary("Atualiza acesso de um usuário");

        /// <summary>
        /// Revoga acesso de um usuário
        /// </summary>
        group.MapDelete("/access/{accessId:int}", async (int accessId, ISystemRegistryService service) =>
        {
            var result = await service.RevokeAccessAsync(accessId);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RevokeAccess")
        .WithSummary("Revoga acesso de um usuário");

        /// <summary>
        /// Aprova ou rejeita solicitação de acesso
        /// </summary>
        group.MapPost("/access/{accessId:int}/approve", async (int accessId, ApproveAccessRequest request, HttpContext http, ISystemRegistryService service) =>
        {
            var adminId = GetUserIdFromClaims(http);
            if (adminId == null) return Results.Unauthorized();

            var result = await service.ApproveAccessAsync(accessId, request, adminId.Value);
            return result 
                ? Results.Ok(new { message = request.Approve ? "Acesso aprovado" : "Acesso rejeitado" }) 
                : Results.NotFound();
        })
        .WithName("ApproveAccess")
        .WithSummary("Aprova ou rejeita solicitação de acesso");

        // ========================================
        // Endpoints para o próprio usuário (Self-Service)
        // ========================================

        var userGroup = app.MapGroup("/api/my-systems")
            .WithTags("User Systems")
            .RequireAuthorization();

        /// <summary>
        /// Lista sistemas do usuário atual
        /// </summary>
        userGroup.MapGet("/", async (HttpContext http, ISystemRegistryService service) =>
        {
            var userId = GetUserIdFromClaims(http);
            if (userId == null) return Results.Unauthorized();

            var systems = await service.GetUserSystemsAsync(userId.Value);
            return Results.Ok(systems);
        })
        .WithName("GetMySystems")
        .WithSummary("Lista sistemas do usuário atual");

        /// <summary>
        /// Solicita acesso a um sistema
        /// </summary>
        userGroup.MapPost("/request", async (RequestSystemAccessRequest request, HttpContext http, ISystemRegistryService service) =>
        {
            var userId = GetUserIdFromClaims(http);
            if (userId == null) return Results.Unauthorized();

            try
            {
                var access = await service.RequestAccessAsync(userId.Value, request);
                return Results.Created($"/api/my-systems", access);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        })
        .WithName("RequestSystemAccess")
        .WithSummary("Solicita acesso a um sistema");

        /// <summary>
        /// Verifica se o usuário tem acesso a um sistema
        /// </summary>
        userGroup.MapGet("/check/{systemCode}", async (string systemCode, HttpContext http, ISystemRegistryService service) =>
        {
            var userId = GetUserIdFromClaims(http);
            if (userId == null) return Results.Unauthorized();

            var hasAccess = await service.HasAccessAsync(userId.Value, systemCode);
            return Results.Ok(new { systemCode, hasAccess });
        })
        .WithName("CheckSystemAccess")
        .WithSummary("Verifica se o usuário tem acesso a um sistema");

        /// <summary>
        /// Obtém políticas do usuário em um sistema
        /// </summary>
        userGroup.MapGet("/policies/{systemCode}", async (string systemCode, HttpContext http, ISystemRegistryService service) =>
        {
            var userId = GetUserIdFromClaims(http);
            if (userId == null) return Results.Unauthorized();

            var policies = await service.GetUserPoliciesForSystemAsync(userId.Value, systemCode);
            return policies != null 
                ? Results.Ok(new { systemCode, policies }) 
                : Results.NotFound(new { error = "Acesso não encontrado ou não aprovado" });
        })
        .WithName("GetMyPoliciesForSystem")
        .WithSummary("Obtém políticas do usuário em um sistema");

        /// <summary>
        /// Lista sistemas disponíveis para solicitação
        /// </summary>
        userGroup.MapGet("/available", async (ISystemRegistryService service) =>
        {
            var systems = await service.GetAllSystemsAsync();
            var available = systems.Where(s => s.IsActive).Select(s => new
            {
                s.Id,
                s.SystemCode,
                s.DisplayName,
                s.Description,
                s.IconUrl,
                s.RequiresApproval
            });
            return Results.Ok(available);
        })
        .WithName("GetAvailableSystems")
        .WithSummary("Lista sistemas disponíveis para solicitação");
    }

    private static int? GetUserIdFromClaims(HttpContext http)
    {
        var userIdClaim = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? http.User.FindFirst("sub")?.Value
                       ?? http.User.FindFirst("userId")?.Value;

        return int.TryParse(userIdClaim, out var id) ? id : null;
    }
}
