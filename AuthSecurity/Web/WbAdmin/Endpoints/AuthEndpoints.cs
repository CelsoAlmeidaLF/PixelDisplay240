using Systekna.Kernel.Application.UseCases;
using Systekna.Kernel.Domain.DTOs;
using Systekna.Kernel.Domain.Interfaces;
using System.Security.Claims;

namespace WbAdmin.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        // === ENDPOINTS PÚBLICOS (sem autenticação) ===
        
        group.MapPost("/register", async (RegisterRequest req, RegisterUserUseCase useCase, ClaimsPrincipal user) =>
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { error = "Por favor, preencha todos os campos para o registro." });

            var success = await useCase.ExecuteAsync(req, user.Identity?.Name ?? "Admin_Portal");
            return success ? Results.Ok(new { message = "Cadastro administrativo realizado com sucesso!" }) 
                           : Results.BadRequest(new { error = "Admin ou E-mail já existe." });
        }).AllowAnonymous();

        group.MapPost("/login", async (LoginRequest req, LoginUseCase useCase, HttpContext context) =>
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { error = "Por favor, preencha todos os campos." });

            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "";
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            // Primeiro autentica (valida senha)
            var response = await useCase.ExecuteAsync(req, ip, userAgent);
            if (response == null)
                return Results.Unauthorized();

            // Depois verifica se o role é Admin (WbGovAdmin: Apenas administradores)
            if (response.Role != "Admin")
            {
                return Results.Json(
                    new { error = "Acesso negado. Este sistema é restrito a administradores." },
                    statusCode: StatusCodes.Status403Forbidden);
            }

            return Results.Ok(response);
        }).AllowAnonymous();

        group.MapPost("/refresh", async (RefreshTokenRequest req, IAuthService auth) =>
        {
            var response = await auth.RefreshToken(req);
            return response != null ? Results.Ok(response) : Results.BadRequest(new { error = "Invalid token or refresh token" });
        }).AllowAnonymous();

        group.MapPost("/forgot-password", async (ForgotPasswordRequest req, IAuthService auth) =>
        {
            await auth.ForgotPassword(req);
            return Results.Ok(new { message = "If the email exists, a reset link has been sent." });
        }).AllowAnonymous();

        group.MapPost("/reset-password", async (ResetPasswordRequest req, IAuthService auth) =>
        {
            var success = await auth.ResetPassword(req);
            return success ? Results.Ok(new { message = "Password reset successfully" }) : Results.BadRequest(new { error = "Invalid or expired token" });
        }).AllowAnonymous();

        group.MapGet("/verify-email", async (string token, IAuthService auth) =>
        {
            var success = await auth.VerifyEmail(token);
            return success ? Results.Ok(new { message = "Email verified successfully" }) : Results.BadRequest(new { error = "Invalid token" });
        }).AllowAnonymous();

        group.MapPost("/logout", () => Results.Ok(new { message = "Logout successful (Token invalidated client-side)" }))
            .AllowAnonymous();

        // === ENDPOINTS PROTEGIDOS (requerem autenticação) ===

        group.MapPost("/change-password", async (ChangePasswordRequest req, ClaimsPrincipal user, IAuthService auth) =>
        {
            if (string.IsNullOrWhiteSpace(req.OldPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
                return Results.BadRequest(new { error = "Preencha todos os campos da senha." });

            if (req.NewPassword.Length < 8)
                return Results.BadRequest(new { error = "A nova senha deve ter pelo menos 8 caracteres." });

            var userIdValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                              ?? user.FindFirst("sub")?.Value 
                              ?? "0";
            var userId = int.Parse(userIdValue);
            
            var success = await auth.ChangePassword(userId, req);
            return success ? Results.Ok(new { message = "Senha alterada com sucesso!" }) 
                           : Results.BadRequest(new { error = "Senha atual incorreta." });
        }).RequireAuthorization();

        group.MapGet("/validate", (ClaimsPrincipal user) => 
            Results.Ok(new { message = "Token is valid", user = user.Identity?.Name }))
            .RequireAuthorization();
    }
}
