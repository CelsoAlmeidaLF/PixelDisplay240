using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Systekna.Kernel.Domain.DTOs;
using Systekna.Kernel.Domain.Entities;
using Systekna.Kernel.Domain.Interfaces;
using Systekna.Kernel.Infrastructure.Data;

namespace Systekna.Kernel.Application.Services;

public class AuthService : IAuthService
{
    private readonly AuthDbContext _context;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenProvider _tokenProvider;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILoginRateLimiter _rateLimiter;
    private readonly IPasswordValidationService _passwordValidator;
    private readonly IAuditService? _auditService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AuthDbContext context, 
        IPasswordHasher hasher, 
        ITokenProvider tokenProvider, 
        IEmailService emailService,
        IConfiguration configuration,
        ILoginRateLimiter rateLimiter,
        IPasswordValidationService passwordValidator,
        ILogger<AuthService> logger,
        IAuditService? auditService = null)
    {
        _context = context;
        _hasher = hasher;
        _tokenProvider = tokenProvider;
        _emailService = emailService;
        _configuration = configuration;
        _rateLimiter = rateLimiter;
        _passwordValidator = passwordValidator;
        _logger = logger;
        _auditService = auditService;
    }

    public async Task<AuthResponse?> Login(LoginRequest request)
    {
        var identifier = request.Username.ToLowerInvariant();
        
        // Verifica rate limiting antes de qualquer operacao
        if (_rateLimiter.IsBlocked(identifier))
        {
            var info = _rateLimiter.GetAttemptInfo(identifier);
            var minutes = (int)Math.Ceiling(info.TimeUntilUnblock?.TotalMinutes ?? 1);
            await LogAuditAsync("LOGIN_BLOCKED", identifier, $"Tentativa bloqueada. Aguarde {minutes} minuto(s).");
            throw new Exception($"Muitas tentativas de login. Aguarde {minutes} minuto(s) e tente novamente.");
        }
        
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        
        // Valida senha ANTES de verificar qualquer outro estado
        // Isso evita revelar se o usuário existe ou não
        if (user == null || !_hasher.Verify(request.Password, user.PasswordHash))
        {
            _rateLimiter.RecordFailedAttempt(identifier);
            await LogAuditAsync("LOGIN_FAILED", identifier, "Credenciais inválidas");
            return null;
        }

        if (!user.IsActive)
        {
            await LogAuditAsync("LOGIN_FAILED", identifier, "Conta desativada");
            throw new Exception("Sua conta está desativada. Entre em contato com o administrador.");
        }

        if (!user.IsApproved)
        {
            await LogAuditAsync("LOGIN_FAILED", identifier, "Conta não aprovada");
            throw new Exception("Seu acesso ainda não foi aprovado pela equipe de governança.");
        }

#if !DEBUG
        if (!user.IsEmailVerified)
        {
            await LogAuditAsync("LOGIN_FAILED", identifier, "E-mail não verificado");
            throw new Exception("Por favor, verifique seu e-mail antes de fazer login.");
        }
#endif

        // Login bem-sucedido - limpa tentativas
        _rateLimiter.RecordSuccessfulLogin(identifier);

        // Inclui policies do usuário no token JWT para verificação granular
        var token = _tokenProvider.CreateToken(
            user.Id.ToString(), 
            user.Username, 
            user.Email, 
            user.Role,
            user.Policies);
            
        var refreshToken = _tokenProvider.GenerateRefreshToken();
        
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();
        
        await LogAuditAsync("LOGIN_SUCCESS", identifier, $"Login bem-sucedido. Role: {user.Role}");

        return new AuthResponse(token, refreshToken, user.Username, user.Email, user.Role, "/admin.html", "Login realizado com sucesso!");
    }

    public async Task<bool> Register(RegisterRequest request)
    {
        // Validar força da senha
        var passwordValidation = _passwordValidator.Validate(request.Password);
        if (!passwordValidation.IsValid)
        {
            throw new Exception($"Senha inválida: {passwordValidation.ErrorMessage}");
        }
        
        if (await _context.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email))
            return false;

        var confirmationToken = Guid.NewGuid().ToString();
        
        var user = new UserEntity
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _hasher.Hash(request.Password),
            EmailConfirmationToken = confirmationToken,
            Role = "User",
#if DEBUG
            // Em DEBUG, auto-aprovar e verificar email para facilitar testes
            IsApproved = true,
            IsEmailVerified = true
#else
            IsApproved = false,
            IsEmailVerified = false
#endif
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        
        await LogAuditAsync("USER_REGISTERED", request.Username, $"Novo usuário registrado: {request.Email}");

#if !DEBUG
        // Apenas em Producao/Homologacao envia email real
        var baseUrl = _configuration["App:BaseUrl"] ?? "https://localhost";
        var verifyUrl = $"{baseUrl}/api/auth/verify-email?token={confirmationToken}";
        
        var emailBody = $@"
            <h2>Bem-vindo ao sistema!</h2>
            <p>Olá {request.Username},</p>
            <p>Sua conta foi criada com sucesso. Clique no link abaixo para verificar seu e-mail:</p>
            <p><a href='{verifyUrl}'>Verificar E-mail</a></p>
            <p>Ou copie e cole este link no navegador: {verifyUrl}</p>
            <p>Após verificar seu e-mail, aguarde a aprovação da equipe de governança.</p>
        ";
        
        await _emailService.SendEmailAsync(user.Email, "Verifique seu E-mail - Sistema Systekna", emailBody);
#else
        // Em DEBUG, apenas loga
        _logger.LogDebug("[DEBUG] Usuário {Username} criado e auto-aprovado", request.Username);
        _logger.LogDebug("[DEBUG] Token de verificação (não necessário em DEBUG): {Token}", confirmationToken);
#endif

        return true;
    }

    public async Task<AuthResponse?> RefreshToken(RefreshTokenRequest request)
    {
        var principal = _tokenProvider.GetPrincipalFromExpiredToken(request.Token);
        if (principal == null) return null;

        var username = principal.Identity?.Name;
        
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return null;

        // Inclui policies atualizadas do usuário no novo token
        var newToken = _tokenProvider.CreateToken(
            user.Id.ToString(), 
            user.Username, 
            user.Email, 
            user.Role,
            user.Policies);
        var newRefreshToken = _tokenProvider.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        await _context.SaveChangesAsync();

        return new AuthResponse(newToken, newRefreshToken, user.Username, user.Email, user.Role, "/admin.html", "Sessão renovada.");
    }

    public async Task ForgotPassword(ForgotPasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null) return; // Nao revelar se o email existe ou nao

        var resetToken = Guid.NewGuid().ToString();
        user.PasswordResetToken = resetToken;
        user.ResetTokenExpires = DateTime.UtcNow.AddHours(1);
        await _context.SaveChangesAsync();

#if DEBUG
        // Em DEBUG, apenas loga o token
        _logger.LogDebug("[DEBUG] Token de recuperação para {Email}: {Token}", request.Email, resetToken);
        _logger.LogDebug("[DEBUG] Use POST /api/auth/reset-password com token: {Token}", resetToken);
#else
        // Em Producao, envia email real
        var baseUrl = _configuration["App:BaseUrl"] ?? "https://localhost";
        var resetUrl = $"{baseUrl}/reset-password?token={resetToken}";
        
        var emailBody = $@"
            <h2>Recuperação de Senha</h2>
            <p>Olá {user.Username},</p>
            <p>Recebemos uma solicitação para redefinir sua senha. Clique no link abaixo:</p>
            <p><a href='{resetUrl}'>Redefinir Senha</a></p>
            <p>Ou copie e cole este link no navegador: {resetUrl}</p>
            <p>Este link expira em 1 hora.</p>
            <p>Se você não solicitou esta alteração, ignore este e-mail.</p>
        ";
        
        await _emailService.SendEmailAsync(user.Email, "Recuperação de Senha - Sistema Systekna", emailBody);
#endif
    }

    public async Task<bool> ResetPassword(ResetPasswordRequest request)
    {
        // Validar força da nova senha
        var passwordValidation = _passwordValidator.Validate(request.NewPassword);
        if (!passwordValidation.IsValid)
        {
            throw new Exception($"Senha inválida: {passwordValidation.ErrorMessage}");
        }
        
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token && u.ResetTokenExpires > DateTime.UtcNow);
        if (user == null) return false;

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        user.PasswordResetToken = null;
        user.ResetTokenExpires = null;
        
        // Invalida refresh tokens existentes (força re-login)
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        
        await _context.SaveChangesAsync();
        await LogAuditAsync("PASSWORD_RESET", user.Username, "Senha redefinida via token de recuperação");
        return true;
    }

    public async Task<bool> ChangePassword(int userId, ChangePasswordRequest request)
    {
        // Validar força da nova senha
        var passwordValidation = _passwordValidator.Validate(request.NewPassword);
        if (!passwordValidation.IsValid)
        {
            throw new Exception($"Senha inválida: {passwordValidation.ErrorMessage}");
        }
        
        var user = await _context.Users.FindAsync(userId);
        if (user == null || !_hasher.Verify(request.OldPassword, user.PasswordHash))
            return false;

        user.PasswordHash = _hasher.Hash(request.NewPassword);
        
        // Invalida refresh tokens existentes (força re-login em outros dispositivos)
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        
        await _context.SaveChangesAsync();
        await LogAuditAsync("PASSWORD_CHANGED", user.Username, "Senha alterada pelo usuário");
        return true;
    }

    public async Task<bool> AdminResetPassword(int userId, string newPassword)
    {
        // Validar força da nova senha
        var passwordValidation = _passwordValidator.Validate(newPassword);
        if (!passwordValidation.IsValid)
        {
            throw new Exception($"Senha inválida: {passwordValidation.ErrorMessage}");
        }
        
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.PasswordHash = _hasher.Hash(newPassword);
        user.PasswordResetToken = null;
        user.ResetTokenExpires = null;
        
        // Invalida refresh tokens existentes (força re-login)
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        
        await _context.SaveChangesAsync();
        await LogAuditAsync("ADMIN_PASSWORD_RESET", user.Username, $"Senha redefinida por administrador");
        return true;
    }

    public async Task<bool> VerifyEmail(string token)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailConfirmationToken == token);
        if (user == null) return false;

        user.IsEmailVerified = true;
        user.EmailConfirmationToken = null;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserDto>> GetAllUsers()
    {
        return await _context.Users
            .Select(u => new UserDto(u.Id, u.Username, u.Email, u.Role, u.IsEmailVerified, u.CreatedAt, u.IsApproved, u.Policies, u.IsActive))
            .ToListAsync();
    }

    public async Task<UserDto?> GetUserById(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        return new UserDto(user.Id, user.Username, user.Email, user.Role, user.IsEmailVerified, user.CreatedAt, user.IsApproved, user.Policies, user.IsActive);
    }

    public async Task<bool> ApproveUser(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;
        user.IsApproved = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserDto>> GetPendingUsers()
    {
        return await _context.Users
            .Where(u => !u.IsApproved && u.IsActive)
            .Select(u => new UserDto(u.Id, u.Username, u.Email, u.Role, u.IsEmailVerified, u.CreatedAt, u.IsApproved, u.Policies, u.IsActive))
            .ToListAsync();
    }

    public async Task<bool> UpdateUserRole(int userId, string newRole)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.Role = newRole;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteUser(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateUserPolicies(int userId, string policies)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.Policies = policies;
        await _context.SaveChangesAsync();
        return true;
    }
    
    /// <summary>
    /// Registra evento de auditoria de forma segura (não falha se o serviço não estiver disponível)
    /// </summary>
    private async Task LogAuditAsync(string action, string userIdentifier, string details)
    {
        try
        {
            if (_auditService != null)
            {
                await _auditService.LogAsync(action, userIdentifier, details);
            }
        }
        catch
        {
            // Silenciosamente ignora erros de auditoria para não afetar o fluxo principal
        }
    }
}
