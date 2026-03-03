using Systekna.Kernel.Domain.DTOs;
using Systekna.Kernel.Domain.Entities;
using System.Security.Claims;

namespace Systekna.Kernel.Domain.Interfaces
{
    public interface IAuthService
    {
        Task<bool> Register(RegisterRequest request);
        Task<AuthResponse?> Login(LoginRequest request);
        Task<AuthResponse?> RefreshToken(RefreshTokenRequest request);
        Task ForgotPassword(ForgotPasswordRequest request);
        Task<bool> ResetPassword(ResetPasswordRequest request);
        Task<bool> VerifyEmail(string token);
        Task<bool> ChangePassword(int userId, ChangePasswordRequest request);
        Task<bool> AdminResetPassword(int userId, string newPassword);
        Task<List<UserDto>> GetAllUsers();
        Task<UserDto?> GetUserById(int userId);
        Task<bool> UpdateUserRole(int userId, string newRole);
        Task<bool> DeleteUser(int userId);
        Task<bool> ApproveUser(int userId);
        Task<List<UserDto>> GetPendingUsers();
        Task<bool> UpdateUserPolicies(int userId, string policies);
    }

    public interface ITokenProvider
    {
        string CreateToken(string userId, string username, string email, string role, string? policies = null);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }

    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string hash);
    }

    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }

    public interface IAuditService
    {
        Task LogAsync(string action, string userIdentifier, string details, string ipAddress = "", string userAgent = "");
        Task<List<AuditLog>> GetRecentLogsAsync(int count = 100);
        Task<byte[]> GenerateAuditReportCsvAsync();
    }

    public interface IGovernanceService
    {
        Task<GovernanceStats> GetStatsAsync();
        Task<bool> ToggleUserStatusAsync(int userId);
        Task<List<SystemSetting>> GetSettingsAsync();
        Task UpdateSettingAsync(string key, string value);
        Task<List<ErrorLog>> GetRecentErrorsAsync(int count = 100);
        Task<byte[]> GenerateErrorReportCsvAsync();
    }

    public interface IVpsManagerService
    {
        Task<HostStats> GetHostStatsAsync();
        Task<bool> RestartServiceAsync(string serviceName);
        Task<List<string>> GetSystemLogsAsync(int lines = 50);
    }
}
