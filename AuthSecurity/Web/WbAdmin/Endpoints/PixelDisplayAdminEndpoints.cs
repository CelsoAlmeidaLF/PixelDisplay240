using Systekna.Kernel.Domain.DTOs;
using Systekna.Kernel.Domain.Interfaces;
using System.Security.Claims;

namespace WbAdmin.Endpoints;

public static class PixelDisplayAdminEndpoints
{
    public static void MapPixelDisplayAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/pixeldisplay").RequireAuthorization(p => p.RequireRole("Admin"));

        // Lista todos os usuários com acesso ao PixelDisplay
        group.MapGet("/users", async (IAuthService auth) =>
        {
            var allUsers = await auth.GetAllUsers();
            var pixelDisplayUsers = allUsers
                .Where(u => u.Policies.Contains("PixelDisplay") || u.Policies.Contains("BaseAccess") || u.Role == "Admin")
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.Role,
                    u.IsActive,
                    u.CreatedAt,
                    Policies = u.Policies.Split(';', StringSplitOptions.RemoveEmptyEntries),
                    Features = new
                    {
                        CanExport = u.Policies.Contains("PixelDisplay.Export") || u.Role == "Admin",
                        CanUseAI = u.Policies.Contains("PixelDisplay.AI") || u.Role == "Admin",
                        CanManageProjects = u.Policies.Contains("PixelDisplay.Projects") || u.Role == "Admin"
                    }
                })
                .ToList();

            return Results.Ok(pixelDisplayUsers);
        });

        // Concede acesso ao PixelDisplay para um usuário
        group.MapPost("/users/{id}/grant-access", async (int id, IAuthService auth, IAuditService audit, ClaimsPrincipal user) =>
        {
            var targetUser = await auth.GetUserById(id);
            if (targetUser == null) return Results.NotFound(new { error = "Usuário não encontrado" });

            var currentPolicies = targetUser.Policies.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            
            if (!currentPolicies.Contains("PixelDisplay"))
            {
                currentPolicies.Add("PixelDisplay");
            }

            var success = await auth.UpdateUserPolicies(id, string.Join(";", currentPolicies));
            if (success)
            {
                await audit.LogAsync("PixelDisplay_GrantAccess", user.Identity?.Name ?? "Admin", 
                    $"Concedido acesso ao PixelDisplay para usuário ID {id}");
            }

            return success 
                ? Results.Ok(new { message = "Acesso ao PixelDisplay concedido com sucesso" }) 
                : Results.BadRequest(new { error = "Falha ao conceder acesso" });
        });

        // Revoga acesso ao PixelDisplay de um usuário
        group.MapPost("/users/{id}/revoke-access", async (int id, IAuthService auth, IAuditService audit, ClaimsPrincipal user) =>
        {
            var targetUser = await auth.GetUserById(id);
            if (targetUser == null) return Results.NotFound(new { error = "Usuário não encontrado" });

            var currentPolicies = targetUser.Policies.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            
            // Remove todas as políticas relacionadas ao PixelDisplay
            currentPolicies.RemoveAll(p => p.StartsWith("PixelDisplay"));

            var success = await auth.UpdateUserPolicies(id, string.Join(";", currentPolicies));
            if (success)
            {
                await audit.LogAsync("PixelDisplay_RevokeAccess", user.Identity?.Name ?? "Admin", 
                    $"Revogado acesso ao PixelDisplay do usuário ID {id}");
            }

            return success 
                ? Results.Ok(new { message = "Acesso ao PixelDisplay revogado" }) 
                : Results.BadRequest(new { error = "Falha ao revogar acesso" });
        });

        // Atualiza features específicas do PixelDisplay para um usuário
        group.MapPut("/users/{id}/features", async (int id, PixelDisplayFeaturesRequest req, IAuthService auth, IAuditService audit, ClaimsPrincipal user) =>
        {
            var targetUser = await auth.GetUserById(id);
            if (targetUser == null) return Results.NotFound(new { error = "Usuário não encontrado" });

            var currentPolicies = targetUser.Policies.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            
            // Remove políticas existentes do PixelDisplay para reconstruir
            currentPolicies.RemoveAll(p => p.StartsWith("PixelDisplay."));

            // Garante acesso base ao PixelDisplay
            if (!currentPolicies.Contains("PixelDisplay"))
            {
                currentPolicies.Add("PixelDisplay");
            }

            // Adiciona features solicitadas
            if (req.CanExport) currentPolicies.Add("PixelDisplay.Export");
            if (req.CanUseAI) currentPolicies.Add("PixelDisplay.AI");
            if (req.CanManageProjects) currentPolicies.Add("PixelDisplay.Projects");

            var success = await auth.UpdateUserPolicies(id, string.Join(";", currentPolicies));
            if (success)
            {
                await audit.LogAsync("PixelDisplay_UpdateFeatures", user.Identity?.Name ?? "Admin", 
                    $"Atualizadas features do PixelDisplay para usuário ID {id}: Export={req.CanExport}, AI={req.CanUseAI}, Projects={req.CanManageProjects}");
            }

            return success 
                ? Results.Ok(new { message = "Features atualizadas com sucesso" }) 
                : Results.BadRequest(new { error = "Falha ao atualizar features" });
        });

        // Estatísticas do PixelDisplay
        group.MapGet("/stats", async (IAuthService auth) =>
        {
            var allUsers = await auth.GetAllUsers();
            
            var stats = new
            {
                TotalUsersWithAccess = allUsers.Count(u => u.Policies.Contains("PixelDisplay") || u.Role == "Admin"),
                UsersWithExport = allUsers.Count(u => u.Policies.Contains("PixelDisplay.Export") || u.Role == "Admin"),
                UsersWithAI = allUsers.Count(u => u.Policies.Contains("PixelDisplay.AI") || u.Role == "Admin"),
                UsersWithProjects = allUsers.Count(u => u.Policies.Contains("PixelDisplay.Projects") || u.Role == "Admin"),
                ActiveUsers = allUsers.Count(u => u.IsActive && (u.Policies.Contains("PixelDisplay") || u.Role == "Admin"))
            };

            return Results.Ok(stats);
        });
    }
}

public record PixelDisplayFeaturesRequest(bool CanExport, bool CanUseAI, bool CanManageProjects);
