using Systekna.Kernel.Domain.DTOs;
using Systekna.Kernel.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PixelDisplay240Api.Endpoints;

/// <summary>
/// Endpoints de Perfil do Usuário - Dados pessoais, alteração de senha, preferências
/// </summary>
public static class ProfileEndpoints
{
    public static void MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profile")
            .RequireAuthorization("ClienteOuAdmin");

        // ==========================================
        // OBTER DADOS DO USUÁRIO LOGADO
        // ==========================================
        group.MapGet("/me", async (ClaimsPrincipal user, IAuthService authService) =>
        {
            var userId = GetUserId(user);
            if (userId == 0) return Results.Unauthorized();

            var userInfo = await authService.GetUserById(userId);
            if (userInfo == null) return Results.NotFound(new { error = "Usuário não encontrado." });

            return Results.Ok(new
            {
                userInfo.Id,
                userInfo.Username,
                userInfo.Email,
                userInfo.Role,
                userInfo.CreatedAt,
                userInfo.IsEmailVerified,
                userInfo.IsApproved,
                userInfo.IsActive,
                policies = userInfo.Policies.Split(';', StringSplitOptions.RemoveEmptyEntries)
            });
        }).WithName("GetCurrentUser");

        // ==========================================
        // OBTER RECURSOS DISPONÍVEIS PARA O USUÁRIO
        // ==========================================
        group.MapGet("/features", async (ClaimsPrincipal user, IAuthService authService) =>
        {
            var userId = GetUserId(user);
            if (userId == 0) return Results.Unauthorized();

            var userInfo = await authService.GetUserById(userId);
            if (userInfo == null) return Results.NotFound();

            var policies = userInfo.Policies.Split(';', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
            var isAdmin = userInfo.Role == "Admin";

            return Results.Ok(new
            {
                canExport = isAdmin || policies.Contains("PixelDisplay.Export"),
                canUseAI = isAdmin || policies.Contains("PixelDisplay.AI"),
                canManageProjects = isAdmin || policies.Contains("PixelDisplay.Projects"),
                maxProjects = isAdmin ? -1 : 10,
                hasFullAccess = isAdmin,
                availablePolicies = policies
            });
        }).WithName("GetUserFeatures");

        // ==========================================
        // VERIFICAR ACESSO AO PIXELDISPLAY
        // ==========================================
        group.MapGet("/access", async (ClaimsPrincipal user, IAuthService authService) =>
        {
            var userId = GetUserId(user);
            if (userId == 0) return Results.Unauthorized();

            var userInfo = await authService.GetUserById(userId);
            if (userInfo == null) return Results.NotFound();

            var policies = userInfo.Policies.Split(';', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

            var hasAccess = policies.Contains("PixelDisplay") ||
                           policies.Contains("BaseAccess") ||
                           userInfo.Role == "Admin";

            if (!hasAccess)
            {
                return Results.Json(new
                {
                    hasAccess = false,
                    message = "Você não tem permissão para acessar o PixelDisplay240."
                }, statusCode: StatusCodes.Status403Forbidden);
            }

            return Results.Ok(new
            {
                hasAccess = true,
                userInfo.Username,
                userInfo.Email,
                userInfo.Role,
                policies = policies.ToArray()
            });
        }).WithName("CheckAccess");

        // ==========================================
        // ALTERAR SENHA DO USUÁRIO LOGADO
        // ==========================================
        group.MapPost("/change-password", async (
            ChangePasswordRequest request,
            ClaimsPrincipal user,
            IAuthService authService,
            IAuditService auditService,
            HttpContext context) =>
        {
            if (string.IsNullOrWhiteSpace(request.OldPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return Results.BadRequest(new { error = "Por favor, preencha todos os campos." });
            }

            var userId = GetUserId(user);
            if (userId == 0) return Results.Unauthorized();

            var username = user.Identity?.Name ?? "unknown";
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            try
            {
                var success = await authService.ChangePassword(userId, request);

                if (success)
                {
                    await auditService.LogAsync("PASSWORD_CHANGED", username, "Senha alterada pelo próprio usuário", ip);
                    return Results.Ok(new
                    {
                        message = "Senha alterada com sucesso!",
                        requiresRelogin = true // Front-end deve forçar novo login
                    });
                }

                await auditService.LogAsync("PASSWORD_CHANGE_FAILED", username, "Tentativa de alteração de senha falhou", ip);
                return Results.BadRequest(new { error = "Senha atual incorreta." });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).WithName("ChangePassword");

        // ==========================================
        // SALVAR PREFERÊNCIAS DO USUÁRIO
        // ==========================================
        group.MapPost("/preferences", async (
            UserPreferencesRequest request,
            ClaimsPrincipal user,
            IAuthService authService) =>
        {
            var userId = GetUserId(user);
            if (userId == 0) return Results.Unauthorized();

            // TODO: Implementar salvamento de preferências em tabela separada
            // Por enquanto, apenas retorna sucesso
            return Results.Ok(new { message = "Preferências salvas com sucesso." });
        }).WithName("SavePreferences");

        // ==========================================
        // OBTER PREFERÊNCIAS DO USUÁRIO
        // ==========================================
        group.MapGet("/preferences", async (ClaimsPrincipal user, IAuthService authService) =>
        {
            var userId = GetUserId(user);
            if (userId == 0) return Results.Unauthorized();

            // TODO: Implementar leitura de preferências de tabela separada
            return Results.Ok(new
            {
                theme = "default",
                language = "pt-BR",
                gridEnabled = true,
                autoSave = true
            });
        }).WithName("GetPreferences");
    }

    private static int GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                       ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(userIdClaim, out var id) ? id : 0;
    }
}

/// <summary>
/// DTO para preferências do usuário
/// </summary>
public record UserPreferencesRequest(
    string? Theme,
    string? Language,
    bool? GridEnabled,
    bool? AutoSave
);
