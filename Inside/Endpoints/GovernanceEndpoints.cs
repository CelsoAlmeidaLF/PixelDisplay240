using Systekna.Kernel.Domain.DTOs;
using Systekna.Kernel.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PixelDisplay240Api.Endpoints;

/// <summary>
/// Endpoints de Governança - Aprovação de usuários, gestão de permissões
/// Apenas administradores têm acesso
/// </summary>
public static class GovernanceEndpoints
{
    public static void MapGovernanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/governance")
            .RequireAuthorization(policy => policy.RequireRole("Admin"));

        // ==========================================
        // LISTAR TODOS OS USUÁRIOS
        // ==========================================
        group.MapGet("/users", async (IAuthService authService) =>
        {
            var users = await authService.GetAllUsers();
            return Results.Ok(users.Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.Role,
                u.IsEmailVerified,
                u.IsApproved,
                u.IsActive,
                u.CreatedAt,
                policies = u.Policies.Split(';', StringSplitOptions.RemoveEmptyEntries)
            }));
        }).WithName("GetAllUsers");

        // ==========================================
        // LISTAR USUÁRIOS PENDENTES DE APROVAÇÃO
        // ==========================================
        group.MapGet("/users/pending", async (IAuthService authService) =>
        {
            var pendingUsers = await authService.GetPendingUsers();
            return Results.Ok(new
            {
                count = pendingUsers.Count,
                users = pendingUsers.Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Email,
                    u.CreatedAt,
                    u.IsEmailVerified
                })
            });
        }).WithName("GetPendingUsers");

        // ==========================================
        // APROVAR USUÁRIO
        // ==========================================
        group.MapPost("/users/{userId}/approve", async (
            int userId,
            ClaimsPrincipal adminUser,
            IAuthService authService,
            IAuditService auditService,
            HttpContext context) =>
        {
            var adminName = adminUser.Identity?.Name ?? "Admin";
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Buscar informações do usuário antes de aprovar
            var userInfo = await authService.GetUserById(userId);
            if (userInfo == null)
            {
                return Results.NotFound(new { error = "Usuário não encontrado." });
            }

            if (userInfo.IsApproved)
            {
                return Results.BadRequest(new { error = "Usuário já está aprovado." });
            }

            var success = await authService.ApproveUser(userId);

            if (success)
            {
                await auditService.LogAsync(
                    "USER_APPROVED",
                    adminName,
                    $"Usuário aprovado: {userInfo.Username} (ID: {userId})",
                    ip);

                return Results.Ok(new
                {
                    message = $"Usuário '{userInfo.Username}' aprovado com sucesso!",
                    userId,
                    username = userInfo.Username
                });
            }

            return Results.Problem("Erro ao aprovar usuário.");
        }).WithName("ApproveUser");

        // ==========================================
        // REVOGAR ACESSO DO USUÁRIO (Desativar)
        // ==========================================
        group.MapPost("/users/{userId}/revoke", async (
            int userId,
            ClaimsPrincipal adminUser,
            IAuthService authService,
            IGovernanceService governanceService,
            IAuditService auditService,
            HttpContext context) =>
        {
            var adminName = adminUser.Identity?.Name ?? "Admin";
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Verificar se não está tentando revogar a si mesmo
            var adminIdClaim = adminUser.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                            ?? adminUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(adminIdClaim, out var adminId) && adminId == userId)
            {
                return Results.BadRequest(new { error = "Você não pode revogar seu próprio acesso." });
            }

            var userInfo = await authService.GetUserById(userId);
            if (userInfo == null)
            {
                return Results.NotFound(new { error = "Usuário não encontrado." });
            }

            // Usa ToggleUserStatus para desativar
            var success = await governanceService.ToggleUserStatusAsync(userId);

            if (success)
            {
                var newStatus = !userInfo.IsActive;
                var action = newStatus ? "USER_ACTIVATED" : "USER_DEACTIVATED";
                var message = newStatus
                    ? $"Acesso do usuário '{userInfo.Username}' foi restaurado."
                    : $"Acesso do usuário '{userInfo.Username}' foi revogado.";

                await auditService.LogAsync(action, adminName, $"Status alterado para: {userInfo.Username} (ID: {userId})", ip);

                return Results.Ok(new
                {
                    message,
                    userId,
                    username = userInfo.Username,
                    isActive = newStatus
                });
            }

            return Results.Problem("Erro ao alterar status do usuário.");
        }).WithName("RevokeUserAccess");

        // ==========================================
        // ALTERAR ROLE DO USUÁRIO
        // ==========================================
        group.MapPut("/users/{userId}/role", async (
            int userId,
            UpdateRoleRequest request,
            ClaimsPrincipal adminUser,
            IAuthService authService,
            IAuditService auditService,
            HttpContext context) =>
        {
            var adminName = adminUser.Identity?.Name ?? "Admin";
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Validar role
            var validRoles = new[] { "User", "Admin" };
            if (!validRoles.Contains(request.Role))
            {
                return Results.BadRequest(new { error = $"Role inválido. Use: {string.Join(", ", validRoles)}" });
            }

            var userInfo = await authService.GetUserById(userId);
            if (userInfo == null)
            {
                return Results.NotFound(new { error = "Usuário não encontrado." });
            }

            var oldRole = userInfo.Role;
            var success = await authService.UpdateUserRole(userId, request.Role);

            if (success)
            {
                await auditService.LogAsync(
                    "USER_ROLE_CHANGED",
                    adminName,
                    $"Role alterado: {userInfo.Username} de '{oldRole}' para '{request.Role}'",
                    ip);

                return Results.Ok(new
                {
                    message = $"Role do usuário '{userInfo.Username}' alterado de '{oldRole}' para '{request.Role}'.",
                    userId,
                    username = userInfo.Username,
                    oldRole,
                    newRole = request.Role
                });
            }

            return Results.Problem("Erro ao alterar role do usuário.");
        }).WithName("UpdateUserRole");

        // ==========================================
        // ATUALIZAR POLICIES DO USUÁRIO
        // ==========================================
        group.MapPut("/users/{userId}/policies", async (
            int userId,
            UpdatePoliciesRequest request,
            ClaimsPrincipal adminUser,
            IAuthService authService,
            IAuditService auditService,
            HttpContext context) =>
        {
            var adminName = adminUser.Identity?.Name ?? "Admin";
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            var userInfo = await authService.GetUserById(userId);
            if (userInfo == null)
            {
                return Results.NotFound(new { error = "Usuário não encontrado." });
            }

            var oldPolicies = userInfo.Policies;
            var success = await authService.UpdateUserPolicies(userId, request.Policies);

            if (success)
            {
                await auditService.LogAsync(
                    "USER_POLICIES_CHANGED",
                    adminName,
                    $"Policies alteradas: {userInfo.Username}",
                    ip);

                return Results.Ok(new
                {
                    message = $"Policies do usuário '{userInfo.Username}' atualizadas.",
                    userId,
                    username = userInfo.Username,
                    oldPolicies,
                    newPolicies = request.Policies
                });
            }

            return Results.Problem("Erro ao atualizar policies.");
        }).WithName("UpdateUserPolicies");

        // ==========================================
        // RESETAR SENHA DO USUÁRIO (Admin)
        // ==========================================
        group.MapPost("/users/{userId}/reset-password", async (
            int userId,
            AdminResetPasswordRequest request,
            ClaimsPrincipal adminUser,
            IAuthService authService,
            IAuditService auditService,
            HttpContext context) =>
        {
            var adminName = adminUser.Identity?.Name ?? "Admin";
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return Results.BadRequest(new { error = "Nova senha é obrigatória." });
            }

            var userInfo = await authService.GetUserById(userId);
            if (userInfo == null)
            {
                return Results.NotFound(new { error = "Usuário não encontrado." });
            }

            try
            {
                var success = await authService.AdminResetPassword(userId, request.NewPassword);

                if (success)
                {
                    await auditService.LogAsync(
                        "ADMIN_PASSWORD_RESET",
                        adminName,
                        $"Senha resetada para usuário: {userInfo.Username}",
                        ip);

                    return Results.Ok(new
                    {
                        message = $"Senha do usuário '{userInfo.Username}' foi resetada.",
                        userId,
                        username = userInfo.Username
                    });
                }

                return Results.Problem("Erro ao resetar senha.");
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).WithName("AdminResetPassword");

        // ==========================================
        // EXCLUIR USUÁRIO
        // ==========================================
        group.MapDelete("/users/{userId}", async (
            int userId,
            ClaimsPrincipal adminUser,
            IAuthService authService,
            IAuditService auditService,
            HttpContext context) =>
        {
            var adminName = adminUser.Identity?.Name ?? "Admin";
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Verificar se não está tentando excluir a si mesmo
            var adminIdClaim = adminUser.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                            ?? adminUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(adminIdClaim, out var adminId) && adminId == userId)
            {
                return Results.BadRequest(new { error = "Você não pode excluir sua própria conta." });
            }

            var userInfo = await authService.GetUserById(userId);
            if (userInfo == null)
            {
                return Results.NotFound(new { error = "Usuário não encontrado." });
            }

            var success = await authService.DeleteUser(userId);

            if (success)
            {
                await auditService.LogAsync(
                    "USER_DELETED",
                    adminName,
                    $"Usuário excluído: {userInfo.Username} (ID: {userId})",
                    ip);

                return Results.Ok(new
                {
                    message = $"Usuário '{userInfo.Username}' foi excluído permanentemente.",
                    userId,
                    username = userInfo.Username
                });
            }

            return Results.Problem("Erro ao excluir usuário.");
        }).WithName("DeleteUser");

        // ==========================================
        // ESTATÍSTICAS DE GOVERNANÇA
        // ==========================================
        group.MapGet("/stats", async (IGovernanceService governanceService) =>
        {
            var stats = await governanceService.GetStatsAsync();
            return Results.Ok(stats);
        }).WithName("GetGovernanceStats");

        // ==========================================
        // LOGS DE AUDITORIA
        // ==========================================
        group.MapGet("/audit-logs", async (int? count, IAuditService auditService) =>
        {
            var logs = await auditService.GetRecentLogsAsync(count ?? 100);
            return Results.Ok(new
            {
                totalCount = logs.Count,
                logs = logs.Select(l => new
                {
                    l.Id,
                    l.Action,
                    l.UserIdentifier,
                    l.Details,
                    l.IpAddress,
                    l.Timestamp
                })
            });
        }).WithName("GetAuditLogs");

        // ==========================================
        // EXPORTAR RELATÓRIO DE AUDITORIA
        // ==========================================
        group.MapGet("/audit-logs/export", async (IAuditService auditService) =>
        {
            var csvBytes = await auditService.GenerateAuditReportCsvAsync();
            return Results.File(csvBytes, "text/csv", $"audit_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }).WithName("ExportAuditLogs");
    }
}

/// <summary>
/// DTO para reset de senha por admin
/// </summary>
public record AdminResetPasswordRequest(string NewPassword);
