using Systekna.Kernel.Domain.DTOs;
using Systekna.Kernel.Domain.Entities;
using Systekna.Kernel.Domain.Interfaces;
using System.Security.Claims;

namespace WbAdmin.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var adminGroup = app.MapGroup("/api/admin").RequireAuthorization(p => p.RequireRole("Admin"));

        // ========== USER MANAGEMENT ==========
        
        // Listar todos os usuários
        adminGroup.MapGet("/users", async (IAuthService auth) => Results.Ok(await auth.GetAllUsers()));
        
        // Listar usuários pendentes de aprovação
        adminGroup.MapGet("/users/pending", async (IAuthService auth) => Results.Ok(await auth.GetPendingUsers()));
        
        // Obter usuário por ID
        adminGroup.MapGet("/users/{id}", async (int id, IAuthService auth) =>
        {
            var user = await auth.GetUserById(id);
            return user != null ? Results.Ok(user) : Results.NotFound(new { error = "Usuário não encontrado" });
        });

        // Criar novo usuário (pelo Admin)
        adminGroup.MapPost("/users", async (CreateUserRequest req, IAuthService auth, IAuditService audit, ClaimsPrincipal user) =>
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { error = "Preencha todos os campos obrigatórios." });

            if (req.Password.Length < 8)
                return Results.BadRequest(new { error = "A senha deve ter pelo menos 8 caracteres." });

            var registerReq = new RegisterRequest(req.Username, req.Email, req.Password);
            var success = await auth.Register(registerReq);
            
            if (!success)
                return Results.BadRequest(new { error = "Usuário ou E-mail já existe no sistema." });

            // Se foi criado, busca para atualizar role e aprovar automaticamente
            var users = await auth.GetAllUsers();
            var newUser = users.FirstOrDefault(u => u.Username == req.Username);
            
            if (newUser != null)
            {
                // Atualiza role se especificado
                if (!string.IsNullOrEmpty(req.Role) && req.Role != "User")
                {
                    await auth.UpdateUserRole(newUser.Id, req.Role);
                }
                
                // Atualiza policies se especificado
                if (!string.IsNullOrEmpty(req.Policies))
                {
                    await auth.UpdateUserPolicies(newUser.Id, req.Policies);
                }
                
                // Aprova automaticamente usuário criado pelo admin
                await auth.ApproveUser(newUser.Id);
            }

            await audit.LogAsync("Admin_CreateUser", user.Identity?.Name ?? "Admin", $"Criado usuário: {req.Username} com role {req.Role ?? "User"}");
            
            return Results.Ok(new { message = "Usuário criado com sucesso!", userId = newUser?.Id });
        });

        // Aprovar usuário pendente
        adminGroup.MapPost("/users/{id}/approve", async (int id, IAuthService auth, IAuditService audit, ClaimsPrincipal user) =>
        {
            var success = await auth.ApproveUser(id);
            if (success) await audit.LogAsync("Admin_ApproveUser", user.Identity?.Name ?? "Admin", $"Aprovado acesso para o usuário ID {id}");
            return success ? Results.Ok(new { message = "Usuário aprovado com sucesso" }) : Results.NotFound(new { error = "Usuário não encontrado" });
        });

        // Atualizar role do usuário
        adminGroup.MapPut("/users/{id}/role", async (int id, UpdateRoleRequest req, IAuthService auth, IAuditService audit, ClaimsPrincipal user) =>
        {
            var success = await auth.UpdateUserRole(id, req.Role);
            if (success) await audit.LogAsync("Admin_UpdateRole", user.Identity?.Name ?? "Admin", $"Alterado cargo do usuário ID {id} para {req.Role}");
            return success ? Results.Ok(new { message = "Cargo atualizado com sucesso" }) : Results.NotFound(new { error = "Usuário não encontrado" });
        });

        // Atualizar policies do usuário
        adminGroup.MapPut("/users/{id}/policies", async (int id, UpdatePoliciesRequest req, IAuthService auth, IAuditService audit, ClaimsPrincipal user) =>
        {
            var success = await auth.UpdateUserPolicies(id, req.Policies);
            if (success) await audit.LogAsync("Admin_UpdatePolicies", user.Identity?.Name ?? "Admin", $"Alteradas políticas do usuário ID {id}");
            return success ? Results.Ok(new { message = "Políticas atualizadas com sucesso" }) : Results.NotFound(new { error = "Usuário não encontrado" });
        });

        // Resetar senha do usuário (pelo Admin)
        adminGroup.MapPost("/users/{id}/reset-password", async (int id, AdminResetPasswordRequest req, IAuthService auth, IPasswordHasher hasher, IAuditService audit, ClaimsPrincipal user) =>
        {
            if (string.IsNullOrWhiteSpace(req.NewPassword))
                return Results.BadRequest(new { error = "Nova senha é obrigatória." });

            if (req.NewPassword.Length < 8)
                return Results.BadRequest(new { error = "A senha deve ter pelo menos 8 caracteres." });

            var targetUser = await auth.GetUserById(id);
            if (targetUser == null)
                return Results.NotFound(new { error = "Usuário não encontrado" });

            // Usar método interno para resetar senha sem precisar da senha antiga
            var success = await auth.AdminResetPassword(id, req.NewPassword);
            
            if (success)
            {
                await audit.LogAsync("Admin_ResetPassword", user.Identity?.Name ?? "Admin", $"Senha resetada para usuário: {targetUser.Username} (ID {id})");
                return Results.Ok(new { message = "Senha alterada com sucesso" });
            }
            
            return Results.BadRequest(new { error = "Erro ao resetar senha" });
        });

        // Alternar status ativo/bloqueado do usuário
        adminGroup.MapPost("/users/{id}/toggle-status", async (int id, IGovernanceService gov, IAuditService audit, ClaimsPrincipal user) =>
        {
            var success = await gov.ToggleUserStatusAsync(id);
            if (success) await audit.LogAsync("Admin_ToggleStatus", user.Identity?.Name ?? "Admin", $"Alterado status (bloqueio/desbloqueio) do usuário ID {id}");
            return success ? Results.Ok(new { message = "Status atualizado com sucesso" }) : Results.NotFound(new { error = "Usuário não encontrado" });
        });

        // Deletar usuário
        adminGroup.MapDelete("/users/{id}", async (int id, IAuthService auth, IAuditService audit, ClaimsPrincipal user) =>
        {
            var targetUser = await auth.GetUserById(id);
            if (targetUser == null)
                return Results.NotFound(new { error = "Usuário não encontrado" });

            var success = await auth.DeleteUser(id);
            if (success) await audit.LogAsync("Admin_DeleteUser", user.Identity?.Name ?? "Admin", $"Deletado usuário: {targetUser.Username} (ID {id})");
            return success ? Results.Ok(new { message = "Usuário removido com sucesso" }) : Results.BadRequest(new { error = "Erro ao remover usuário" });
        });

        // ========== SYSTEM GOVERNANCE ==========
        
        adminGroup.MapGet("/stats", async (IGovernanceService gov) => Results.Ok(await gov.GetStatsAsync()));

        adminGroup.MapGet("/logs", async (IAuditService audit) => Results.Ok(await audit.GetRecentLogsAsync()));

        adminGroup.MapGet("/settings", async (IGovernanceService gov) => Results.Ok(await gov.GetSettingsAsync()));

        adminGroup.MapPost("/settings", async (SystemSetting setting, IGovernanceService gov, IAuditService audit, ClaimsPrincipal user) =>
        {
            await gov.UpdateSettingAsync(setting.Key, setting.Value);
            await audit.LogAsync("Admin_UpdateSetting", user.Identity?.Name ?? "Admin", $"Alterada configuração {setting.Key}");
            return Results.Ok(new { message = "Configuração salva com sucesso" });
        });
    }
}

// DTOs para criação e reset de senha pelo Admin
public record CreateUserRequest(string Username, string Email, string Password, string? Role = "User", string? Policies = null);
public record AdminResetPasswordRequest(string NewPassword);
