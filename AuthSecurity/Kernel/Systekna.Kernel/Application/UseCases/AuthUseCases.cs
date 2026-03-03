using Systekna.Kernel.Domain.DTOs;
using Systekna.Kernel.Domain.Interfaces;

namespace Systekna.Kernel.Application.UseCases;

public class LoginUseCase
{
    private readonly IAuthService _authService;
    private readonly IAuditService _auditService;

    public LoginUseCase(IAuthService authService, IAuditService auditService)
    {
        _authService = authService;
        _auditService = auditService;
    }

    public async Task<AuthResponse?> ExecuteAsync(LoginRequest request, string ipAddress = "", string userAgent = "")
    {
        var response = await _authService.Login(request);
        if (response != null)
        {
            await _auditService.LogAsync("Login_Success", request.Username, "Usuário logou no sistema", ipAddress, userAgent);
        }
        else
        {
            await _auditService.LogAsync("Login_Failure", request.Username, "Tentativa de login falhou", ipAddress, userAgent);
        }
        return response;
    }
}

public class RegisterUserUseCase
{
    private readonly IAuthService _authService;
    private readonly IAuditService _auditService;

    public RegisterUserUseCase(IAuthService authService, IAuditService auditService)
    {
        _authService = authService;
        _auditService = auditService;
    }

    public async Task<bool> ExecuteAsync(RegisterRequest request, string performer)
    {
        var success = await _authService.Register(request);
        if (success)
        {
            await _auditService.LogAsync("User_Registered", performer, $"Novo usuário registrado: {request.Username}");
        }
        return success;
    }
}
