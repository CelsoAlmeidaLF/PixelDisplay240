namespace Systekna.Kernel.Domain.DTOs;

public record LoginRequest(string Username, string Password);
public record RegisterRequest(string Username, string Email, string Password);
public record AuthResponse(string Token, string RefreshToken, string Username, string Email, string Role, string RedirectUrl, string Message = "");

public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
public record ChangePasswordRequest(string OldPassword, string NewPassword);
public record RefreshTokenRequest(string Token, string RefreshToken);
public record UpdateRoleRequest(string Role);
public record UpdatePoliciesRequest(string Policies);

public record UserDto(int Id, string Username, string Email, string Role, bool IsEmailVerified, DateTime CreatedAt, bool IsApproved, string Policies, bool IsActive);
public record GovernanceStats(int TotalUsers, int ActiveSessions, int FailedLoginsLast24h, int AuditLogsCount);
