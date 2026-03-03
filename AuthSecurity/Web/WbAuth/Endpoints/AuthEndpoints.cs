using Systekna.Kernel.Application.Services;
using Systekna.Kernel.Application.UseCases;
using Systekna.Kernel.Domain.DTOs;
using Systekna.Kernel.Domain.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace WbAuth.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (RegisterRequest req, RegisterUserUseCase useCase) =>
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { error = "Por favor, preencha todos os campos para o registro." });

            var success = await useCase.ExecuteAsync(req, "Public_Register");
            return success ? Results.Ok(new { message = "Cadastro realizado com sucesso! Agora você pode fazer login." }) 
                           : Results.BadRequest(new { error = "Usuário ou E-mail já existe no sistema." });
        });

        group.MapPost("/login", async (LoginRequest req, LoginUseCase useCase, IAuthService authService, HttpContext context) =>
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return Results.BadRequest(new { error = "Por favor, preencha todos os campos." });

            // WbAuth: Apenas clientes (User) e administradores (Admin) podem acessar
            var users = await authService.GetAllUsers();
            var user = users.FirstOrDefault(u => u.Username == req.Username);
            
            if (user == null || (user.Role != "User" && user.Role != "Admin"))
                return Results.Unauthorized();

            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "";
            var userAgent = context.Request.Headers["User-Agent"].ToString();

            var response = await useCase.ExecuteAsync(req, ip, userAgent);
            return response != null ? Results.Ok(response) : Results.Unauthorized();
        });

        group.MapPost("/refresh", async (RefreshTokenRequest req, IAuthService auth) =>
        {
            var response = await auth.RefreshToken(req);
            return response != null ? Results.Ok(response) : Results.BadRequest(new { error = "Invalid token or refresh token" });
        });

        group.MapPost("/forgot-password", async (ForgotPasswordRequest req, IAuthService auth) =>
        {
            await auth.ForgotPassword(req);
            return Results.Ok(new { message = "If the email exists, a reset link has been sent." });
        });

        group.MapPost("/reset-password", async (ResetPasswordRequest req, IAuthService auth) =>
        {
            var success = await auth.ResetPassword(req);
            return success ? Results.Ok(new { message = "Password reset successfully" }) : Results.BadRequest(new { error = "Invalid or expired token" });
        });

        group.MapGet("/verify-email", async (string token, IAuthService auth) =>
        {
            var success = await auth.VerifyEmail(token);
            return success ? Results.Ok(new { message = "Email verified successfully" }) : Results.BadRequest(new { error = "Invalid token" });
        });

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

        group.MapPost("/logout", () => Results.Ok(new { message = "Logout successful (Token invalidated client-side)" }));

        group.MapGet("/validate", (ClaimsPrincipal user) => 
            Results.Ok(new { message = "Token is valid", user = user.Identity?.Name }))
            .RequireAuthorization();
    }
}
