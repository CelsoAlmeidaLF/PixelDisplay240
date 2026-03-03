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

// ========================================
// DTOs para Sistemas Cadastrados
// ========================================

/// <summary>
/// DTO para exibição de sistema cadastrado
/// </summary>
public record RegisteredSystemDto(
    int Id,
    string SystemCode,
    string DisplayName,
    string? Description,
    string? BaseUrl,
    string? IconUrl,
    string AvailablePolicies,
    bool IsActive,
    bool RequiresApproval,
    DateTime CreatedAt,
    int TotalUsers,
    int ActiveUsers
);

/// <summary>
/// DTO para criação/atualização de sistema
/// </summary>
public record CreateSystemRequest(
    string SystemCode,
    string DisplayName,
    string? Description,
    string? BaseUrl,
    string? IconUrl,
    string? AvailablePolicies,
    bool RequiresApproval = true
);

/// <summary>
/// DTO para atualização de sistema
/// </summary>
public record UpdateSystemRequest(
    string? DisplayName,
    string? Description,
    string? BaseUrl,
    string? IconUrl,
    string? AvailablePolicies,
    bool? IsActive,
    bool? RequiresApproval
);

// ========================================
// DTOs para Acesso Usuário-Sistema
// ========================================

/// <summary>
/// DTO para exibição de acesso de usuário a sistema
/// </summary>
public record UserSystemAccessDto(
    int Id,
    int UserId,
    string Username,
    string UserEmail,
    int SystemId,
    string SystemCode,
    string SystemDisplayName,
    string SystemRole,
    string GrantedPolicies,
    bool IsActive,
    bool IsApproved,
    DateTime RequestedAt,
    DateTime? ApprovedAt,
    DateTime? LastAccessAt
);

/// <summary>
/// DTO para solicitar acesso a um sistema
/// </summary>
public record RequestSystemAccessRequest(
    int SystemId,
    string? RequestedRole,
    string? Message
);

/// <summary>
/// DTO para conceder acesso a um usuário
/// </summary>
public record GrantSystemAccessRequest(
    int UserId,
    int SystemId,
    string SystemRole,
    string? GrantedPolicies
);

/// <summary>
/// DTO para atualizar acesso de usuário
/// </summary>
public record UpdateUserAccessRequest(
    string? SystemRole,
    string? GrantedPolicies,
    bool? IsActive,
    string? AdminNotes
);

/// <summary>
/// DTO para aprovar/rejeitar solicitação de acesso
/// </summary>
public record ApproveAccessRequest(
    bool Approve,
    string? SystemRole,
    string? GrantedPolicies,
    string? AdminNotes
);

/// <summary>
/// DTO para listar sistemas do usuário
/// </summary>
public record UserSystemSummaryDto(
    int SystemId,
    string SystemCode,
    string DisplayName,
    string? IconUrl,
    string? BaseUrl,
    string Role,
    string Policies,
    bool IsApproved,
    DateTime? LastAccessAt
);

/// <summary>
/// DTO para estatísticas de sistema
/// </summary>
public record SystemStatsDto(
    int SystemId,
    string SystemCode,
    string DisplayName,
    int TotalUsers,
    int ActiveUsers,
    int PendingApprovals,
    int AdminCount,
    DateTime? LastAccessAt
);
