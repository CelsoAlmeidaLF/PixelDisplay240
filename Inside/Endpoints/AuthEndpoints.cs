using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Systekna.Application.DTOs;
using Systekna.Kernel.Application.Services;
using Systekna.Kernel.Domain.DTOs;
using Systekna.Kernel.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PixelDisplay240Api.Endpoints;

/// <summary>
/// Endpoints de Autenticação - Login, Registro, Recuperação de Senha
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app, AuthOptions authOptions)
    {
        var group = app.MapGroup("/api/auth");

        // ==========================================
        // LOGIN
        // ==========================================
        group.MapPost("/login", async (
            LoginRequest request, 
            IAuthService authService,
            ILoginRateLimiter rateLimiter,
            IAuditService auditService,
            HttpContext context) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { error = "Por favor, preencha todos os campos." });

            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = context.Request.Headers.UserAgent.ToString();

            try
            {
                var response = await authService.Login(request);
                if (response == null)
                {
                    await auditService.LogAsync("LOGIN_FAILED", request.Username, "Credenciais inválidas", ip, userAgent);
                    return Results.Unauthorized();
                }

                // Verifica se o role é permitido para PixelDisplay240
                if (response.Role != "User" && response.Role != "Admin")
                {
                    await auditService.LogAsync("LOGIN_DENIED", request.Username, "Role não permitido no PixelDisplay240", ip, userAgent);
                    return Results.Json(
                        new { error = "Seu perfil não tem permissão para acessar o PixelDisplay240." },
                        statusCode: StatusCodes.Status403Forbidden);
                }

                // === CRIAR COOKIE DE AUTENTICAÇÃO ===
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, response.Username),
                    new Claim(ClaimTypes.Email, response.Email),
                    new Claim(ClaimTypes.Role, response.Role),
                    new Claim(JwtRegisteredClaimNames.UniqueName, response.Username)
                };
                
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
                    AllowRefresh = true
                };
                
                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // Também salvar o JWT em um cookie HttpOnly para chamadas de API
                context.Response.Cookies.Append("PixelDisplay240.Token", response.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = context.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

                await auditService.LogAsync("LOGIN_SUCCESS", request.Username, $"Login bem-sucedido. Role: {response.Role}", ip, userAgent);

                return Results.Ok(new
                {
                    token = response.Token,
                    refreshToken = response.RefreshToken,
                    expiresAt = DateTime.UtcNow.AddHours(authOptions.TokenExpirationHours),
                    username = response.Username,
                    email = response.Email,
                    role = response.Role,
                    message = response.Message
                });
            }
            catch (Exception ex)
            {
                // Rate limit ou conta bloqueada
                return Results.BadRequest(new { error = ex.Message });
            }
        }).WithName("Login");

        // ==========================================
        // REGISTRO DE NOVO USUÁRIO
        // ==========================================
        group.MapPost("/register", async (
            RegisterRequest request,
            IAuthService authService,
            IPasswordValidationService passwordValidator,
            IAuditService auditService,
            HttpContext context) =>
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { error = "Por favor, preencha todos os campos." });
            }

            // Validar e-mail
            if (!IsValidEmail(request.Email))
            {
                return Results.BadRequest(new { error = "Por favor, informe um e-mail válido." });
            }

            // Validar username
            if (request.Username.Length < 3 || request.Username.Length > 50)
            {
                return Results.BadRequest(new { error = "O nome de usuário deve ter entre 3 e 50 caracteres." });
            }

            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            try
            {
                var success = await authService.Register(request);

                if (!success)
                {
                    await auditService.LogAsync("REGISTER_FAILED", request.Username, "Usuário ou e-mail já existe", ip);
                    return Results.BadRequest(new { error = "Usuário ou E-mail já existe no sistema." });
                }

                await auditService.LogAsync("REGISTER_SUCCESS", request.Username, $"Novo usuário registrado: {request.Email}", ip);

#if DEBUG
                return Results.Ok(new
                {
                    message = "Conta criada com sucesso! Em modo DEBUG, você já pode fazer login.",
                    requiresApproval = false
                });
#else
                return Results.Ok(new
                {
                    message = "Conta criada com sucesso! Verifique seu e-mail e aguarde aprovação da governança.",
                    requiresApproval = true
                });
#endif
            }
            catch (Exception ex)
            {
                // Erro de validação de senha
                return Results.BadRequest(new { error = ex.Message });
            }
        }).WithName("Register");

        // ==========================================
        // ESQUECI MINHA SENHA
        // ==========================================
        group.MapPost("/forgot-password", async (
            ForgotPasswordRequest request,
            IAuthService authService,
            IAuditService auditService,
            HttpContext context) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Results.BadRequest(new { error = "Por favor, informe seu e-mail." });
            }

            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            await authService.ForgotPassword(request);
            await auditService.LogAsync("PASSWORD_RESET_REQUESTED", request.Email, "Solicitação de recuperação de senha", ip);

            // Sempre retorna sucesso para não revelar se o e-mail existe
            return Results.Ok(new { message = "Se o e-mail estiver cadastrado, você receberá um link de recuperação." });
        }).WithName("ForgotPassword");

        // ==========================================
        // REDEFINIR SENHA (via token do e-mail)
        // ==========================================
        group.MapPost("/reset-password", async (
            ResetPasswordRequest request,
            IAuthService authService,
            IAuditService auditService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return Results.BadRequest(new { error = "Token e nova senha são obrigatórios." });
            }

            try
            {
                var success = await authService.ResetPassword(request);

                if (success)
                {
                    await auditService.LogAsync("PASSWORD_RESET_SUCCESS", "token", "Senha redefinida com sucesso");
                    return Results.Ok(new { message = "Senha alterada com sucesso! Você já pode fazer login." });
                }

                return Results.BadRequest(new { error = "Token inválido ou expirado." });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).WithName("ResetPassword");

        // ==========================================
        // REFRESH TOKEN
        // ==========================================
        group.MapPost("/refresh", async (RefreshTokenRequest request, IAuthService authService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Results.BadRequest(new { error = "Token e refresh token são obrigatórios." });
            }

            var response = await authService.RefreshToken(request);
            return response != null
                ? Results.Ok(response)
                : Results.Unauthorized();
        }).WithName("RefreshToken");

        // ==========================================
        // VERIFICAÇÃO DE E-MAIL (link enviado por email)
        // ==========================================
        group.MapGet("/verify-email", async (string token, IAuthService authService) =>
        {
            if (string.IsNullOrWhiteSpace(token))
                return Results.BadRequest(new { error = "Token inválido." });

            var success = await authService.VerifyEmail(token);

            if (success)
            {
                // Redireciona para página de login com mensagem de sucesso
                return Results.Redirect("/Account/Login?verified=true");
            }

            return Results.BadRequest(new { error = "Token inválido ou expirado." });
        }).WithName("VerifyEmail");

        // ==========================================
        // LOGOUT
        // ==========================================
        group.MapPost("/logout", async (HttpContext context) =>
        {
            // Remover cookie de autenticação
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            // Remover cookie do JWT
            context.Response.Cookies.Delete("PixelDisplay240.Token");
            context.Response.Cookies.Delete("PixelDisplay240.Auth");
            
            return Results.Ok(new { message = "Logout realizado com sucesso." });
        }).WithName("Logout");

        // ==========================================
        // VALIDAR TOKEN / SESSÃO
        // ==========================================
        group.MapGet("/validate", (ClaimsPrincipal user) =>
        {
            if (user.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }
            
            var username = user.Identity?.Name ?? user.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;
            var role = user.FindFirst(ClaimTypes.Role)?.Value;

            return Results.Ok(new
            {
                valid = true,
                username,
                role,
                message = "Sessão válida"
            });
        }).RequireAuthorization().WithName("ValidateToken");
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
