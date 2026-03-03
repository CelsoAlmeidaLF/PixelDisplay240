using Microsoft.EntityFrameworkCore;
using Systekna.Kernel.Domain.DTOs;
using Systekna.Kernel.Domain.Entities;
using Systekna.Kernel.Domain.Interfaces;
using Systekna.Kernel.Infrastructure.Data;
using System.Security.Cryptography;

namespace Systekna.Kernel.Application.Services;

/// <summary>
/// Serviço de gerenciamento de sistemas cadastrados e controle de acesso de usuários.
/// Implementa a relação muitos-para-muitos entre usuários e sistemas.
/// </summary>
public class SystemRegistryService : ISystemRegistryService
{
    private readonly IRegisteredSystemRepository _systemRepo;
    private readonly IUserSystemAccessRepository _accessRepo;
    private readonly IUserRepository _userRepo;
    private readonly IAuditService _auditService;
    private readonly AuthDbContext _context;

    public SystemRegistryService(
        IRegisteredSystemRepository systemRepo,
        IUserSystemAccessRepository accessRepo,
        IUserRepository userRepo,
        IAuditService auditService,
        AuthDbContext context)
    {
        _systemRepo = systemRepo;
        _accessRepo = accessRepo;
        _userRepo = userRepo;
        _auditService = auditService;
        _context = context;
    }

    // ========================================
    // CRUD de Sistemas
    // ========================================

    public async Task<List<RegisteredSystemDto>> GetAllSystemsAsync()
    {
        var systems = await _systemRepo.GetAllAsync();
        var result = new List<RegisteredSystemDto>();

        foreach (var system in systems)
        {
            var totalUsers = await _accessRepo.CountBySystemIdAsync(system.Id);
            var activeUsers = await _accessRepo.CountActiveBySystemIdAsync(system.Id);

            result.Add(new RegisteredSystemDto(
                system.Id,
                system.SystemCode,
                system.DisplayName,
                system.Description,
                system.BaseUrl,
                system.IconUrl,
                system.AvailablePolicies,
                system.IsActive,
                system.RequiresApproval,
                system.CreatedAt,
                totalUsers,
                activeUsers
            ));
        }

        return result;
    }

    public async Task<RegisteredSystemDto?> GetSystemByIdAsync(int id)
    {
        var system = await _systemRepo.GetByIdAsync(id);
        if (system == null) return null;

        var totalUsers = await _accessRepo.CountBySystemIdAsync(system.Id);
        var activeUsers = await _accessRepo.CountActiveBySystemIdAsync(system.Id);

        return new RegisteredSystemDto(
            system.Id,
            system.SystemCode,
            system.DisplayName,
            system.Description,
            system.BaseUrl,
            system.IconUrl,
            system.AvailablePolicies,
            system.IsActive,
            system.RequiresApproval,
            system.CreatedAt,
            totalUsers,
            activeUsers
        );
    }

    public async Task<RegisteredSystemDto?> GetSystemByCodeAsync(string systemCode)
    {
        var system = await _systemRepo.GetByCodeAsync(systemCode);
        if (system == null) return null;

        var totalUsers = await _accessRepo.CountBySystemIdAsync(system.Id);
        var activeUsers = await _accessRepo.CountActiveBySystemIdAsync(system.Id);

        return new RegisteredSystemDto(
            system.Id,
            system.SystemCode,
            system.DisplayName,
            system.Description,
            system.BaseUrl,
            system.IconUrl,
            system.AvailablePolicies,
            system.IsActive,
            system.RequiresApproval,
            system.CreatedAt,
            totalUsers,
            activeUsers
        );
    }

    public async Task<RegisteredSystemDto> CreateSystemAsync(CreateSystemRequest request)
    {
        if (await _systemRepo.ExistsAsync(request.SystemCode))
            throw new InvalidOperationException($"Sistema com código '{request.SystemCode}' já existe.");

        var system = new RegisteredSystem
        {
            SystemCode = request.SystemCode.ToLowerInvariant().Trim(),
            DisplayName = request.DisplayName,
            Description = request.Description,
            BaseUrl = request.BaseUrl,
            IconUrl = request.IconUrl,
            AvailablePolicies = request.AvailablePolicies ?? string.Empty,
            RequiresApproval = request.RequiresApproval,
            ApiSecret = GenerateApiSecret(),
            CreatedAt = DateTime.UtcNow
        };

        await _systemRepo.AddAsync(system);

        await _auditService.LogAsync(
            "System_Created",
            "Admin",
            $"Sistema '{system.DisplayName}' ({system.SystemCode}) criado."
        );

        return new RegisteredSystemDto(
            system.Id,
            system.SystemCode,
            system.DisplayName,
            system.Description,
            system.BaseUrl,
            system.IconUrl,
            system.AvailablePolicies,
            system.IsActive,
            system.RequiresApproval,
            system.CreatedAt,
            0, 0
        );
    }

    public async Task<RegisteredSystemDto?> UpdateSystemAsync(int id, UpdateSystemRequest request)
    {
        var system = await _systemRepo.GetByIdAsync(id);
        if (system == null) return null;

        if (request.DisplayName != null) system.DisplayName = request.DisplayName;
        if (request.Description != null) system.Description = request.Description;
        if (request.BaseUrl != null) system.BaseUrl = request.BaseUrl;
        if (request.IconUrl != null) system.IconUrl = request.IconUrl;
        if (request.AvailablePolicies != null) system.AvailablePolicies = request.AvailablePolicies;
        if (request.IsActive.HasValue) system.IsActive = request.IsActive.Value;
        if (request.RequiresApproval.HasValue) system.RequiresApproval = request.RequiresApproval.Value;

        await _systemRepo.UpdateAsync(system);

        await _auditService.LogAsync(
            "System_Updated",
            "Admin",
            $"Sistema '{system.DisplayName}' atualizado."
        );

        return await GetSystemByIdAsync(id);
    }

    public async Task<bool> DeleteSystemAsync(int id)
    {
        var system = await _systemRepo.GetByIdAsync(id);
        if (system == null) return false;

        await _auditService.LogAsync(
            "System_Deleted",
            "Admin",
            $"Sistema '{system.DisplayName}' ({system.SystemCode}) excluído."
        );

        await _systemRepo.DeleteAsync(system);
        return true;
    }

    public async Task<bool> ToggleSystemStatusAsync(int id)
    {
        var system = await _systemRepo.GetByIdAsync(id);
        if (system == null) return false;

        system.IsActive = !system.IsActive;
        await _systemRepo.UpdateAsync(system);

        await _auditService.LogAsync(
            system.IsActive ? "System_Activated" : "System_Deactivated",
            "Admin",
            $"Sistema '{system.DisplayName}' {(system.IsActive ? "ativado" : "desativado")}."
        );

        return true;
    }

    public async Task<string> RegenerateApiSecretAsync(int id)
    {
        var system = await _systemRepo.GetByIdAsync(id);
        if (system == null)
            throw new InvalidOperationException("Sistema não encontrado.");

        system.ApiSecret = GenerateApiSecret();
        await _systemRepo.UpdateAsync(system);

        await _auditService.LogAsync(
            "System_ApiSecret_Regenerated",
            "Admin",
            $"API Secret regenerado para sistema '{system.DisplayName}'."
        );

        return system.ApiSecret;
    }

    // ========================================
    // Gerenciamento de Acesso de Usuários
    // ========================================

    public async Task<List<UserSystemAccessDto>> GetSystemUsersAsync(int systemId)
    {
        var accesses = await _accessRepo.GetBySystemIdAsync(systemId);
        return accesses.Select(MapToDto).ToList();
    }

    public async Task<List<UserSystemAccessDto>> GetPendingAccessRequestsAsync(int systemId)
    {
        var accesses = await _accessRepo.GetPendingBySystemIdAsync(systemId);
        return accesses.Select(MapToDto).ToList();
    }

    public async Task<UserSystemAccessDto> GrantAccessAsync(GrantSystemAccessRequest request, int approverUserId)
    {
        var existing = await _accessRepo.GetByUserAndSystemAsync(request.UserId, request.SystemId);
        if (existing != null)
            throw new InvalidOperationException("Usuário já possui acesso a este sistema.");

        var user = await _userRepo.GetByIdAsync(request.UserId);
        var system = await _systemRepo.GetByIdAsync(request.SystemId);

        if (user == null || system == null)
            throw new InvalidOperationException("Usuário ou sistema não encontrado.");

        var access = new UserSystemAccess
        {
            UserId = request.UserId,
            SystemId = request.SystemId,
            SystemRole = request.SystemRole,
            GrantedPolicies = request.GrantedPolicies ?? string.Empty,
            IsActive = true,
            IsApproved = true, // Concedido diretamente por admin
            RequestedAt = DateTime.UtcNow,
            ApprovedAt = DateTime.UtcNow,
            ApprovedByUserId = approverUserId
        };

        await _accessRepo.AddAsync(access);

        await _auditService.LogAsync(
            "Access_Granted",
            $"Admin:{approverUserId}",
            $"Acesso ao sistema '{system.SystemCode}' concedido para usuário '{user.Username}' com role '{request.SystemRole}'."
        );

        // Recarregar com includes
        var result = await _accessRepo.GetByIdAsync(access.Id);
        return MapToDto(result!);
    }

    public async Task<UserSystemAccessDto?> UpdateAccessAsync(int accessId, UpdateUserAccessRequest request)
    {
        var access = await _accessRepo.GetByIdAsync(accessId);
        if (access == null) return null;

        if (request.SystemRole != null) access.SystemRole = request.SystemRole;
        if (request.GrantedPolicies != null) access.GrantedPolicies = request.GrantedPolicies;
        if (request.IsActive.HasValue) access.IsActive = request.IsActive.Value;
        if (request.AdminNotes != null) access.AdminNotes = request.AdminNotes;

        await _accessRepo.UpdateAsync(access);

        await _auditService.LogAsync(
            "Access_Updated",
            "Admin",
            $"Acesso #{accessId} atualizado: Role={access.SystemRole}, Políticas={access.GrantedPolicies}."
        );

        return MapToDto(access);
    }

    public async Task<bool> RevokeAccessAsync(int accessId)
    {
        var access = await _accessRepo.GetByIdAsync(accessId);
        if (access == null) return false;

        var username = access.User?.Username ?? $"User:{access.UserId}";
        var systemCode = access.System?.SystemCode ?? $"System:{access.SystemId}";

        await _accessRepo.DeleteAsync(access);

        await _auditService.LogAsync(
            "Access_Revoked",
            "Admin",
            $"Acesso de '{username}' ao sistema '{systemCode}' revogado."
        );

        return true;
    }

    public async Task<bool> ApproveAccessAsync(int accessId, ApproveAccessRequest request, int approverUserId)
    {
        var access = await _accessRepo.GetByIdAsync(accessId);
        if (access == null) return false;

        if (request.Approve)
        {
            access.IsApproved = true;
            access.ApprovedAt = DateTime.UtcNow;
            access.ApprovedByUserId = approverUserId;
            if (request.SystemRole != null) access.SystemRole = request.SystemRole;
            if (request.GrantedPolicies != null) access.GrantedPolicies = request.GrantedPolicies;
            if (request.AdminNotes != null) access.AdminNotes = request.AdminNotes;

            await _accessRepo.UpdateAsync(access);

            await _auditService.LogAsync(
                "Access_Approved",
                $"Admin:{approverUserId}",
                $"Solicitação de acesso #{accessId} aprovada para '{access.User?.Username}'."
            );
        }
        else
        {
            // Rejeitar = remover
            await _auditService.LogAsync(
                "Access_Rejected",
                $"Admin:{approverUserId}",
                $"Solicitação de acesso #{accessId} rejeitada para '{access.User?.Username}'. Motivo: {request.AdminNotes}"
            );

            await _accessRepo.DeleteAsync(access);
        }

        return true;
    }

    // ========================================
    // Acesso do próprio usuário
    // ========================================

    public async Task<List<UserSystemSummaryDto>> GetUserSystemsAsync(int userId)
    {
        var accesses = await _accessRepo.GetByUserIdAsync(userId);

        return accesses
            .Where(a => a.IsActive && a.IsApproved && a.System != null && a.System.IsActive)
            .Select(a => new UserSystemSummaryDto(
                a.SystemId,
                a.System!.SystemCode,
                a.System.DisplayName,
                a.System.IconUrl,
                a.System.BaseUrl,
                a.SystemRole,
                a.GrantedPolicies,
                a.IsApproved,
                a.LastAccessAt
            ))
            .ToList();
    }

    public async Task<UserSystemAccessDto?> RequestAccessAsync(int userId, RequestSystemAccessRequest request)
    {
        var existing = await _accessRepo.GetByUserAndSystemAsync(userId, request.SystemId);
        if (existing != null)
            throw new InvalidOperationException("Você já possui ou solicitou acesso a este sistema.");

        var system = await _systemRepo.GetByIdAsync(request.SystemId);
        if (system == null || !system.IsActive)
            throw new InvalidOperationException("Sistema não encontrado ou indisponível.");

        var access = new UserSystemAccess
        {
            UserId = userId,
            SystemId = request.SystemId,
            SystemRole = request.RequestedRole ?? "User",
            IsActive = true,
            IsApproved = !system.RequiresApproval, // Auto-aprova se não requer aprovação
            RequestedAt = DateTime.UtcNow,
            ApprovedAt = system.RequiresApproval ? null : DateTime.UtcNow,
            AdminNotes = request.Message
        };

        await _accessRepo.AddAsync(access);

        var user = await _userRepo.GetByIdAsync(userId);
        await _auditService.LogAsync(
            system.RequiresApproval ? "Access_Requested" : "Access_AutoApproved",
            user?.Username ?? $"User:{userId}",
            $"Solicitação de acesso ao sistema '{system.SystemCode}'."
        );

        var result = await _accessRepo.GetByIdAsync(access.Id);
        return MapToDto(result!);
    }

    public async Task<bool> HasAccessAsync(int userId, string systemCode)
    {
        var system = await _systemRepo.GetByCodeAsync(systemCode);
        if (system == null || !system.IsActive) return false;

        var access = await _accessRepo.GetByUserAndSystemAsync(userId, system.Id);
        return access != null && access.IsActive && access.IsApproved;
    }

    public async Task<string?> GetUserPoliciesForSystemAsync(int userId, string systemCode)
    {
        var system = await _systemRepo.GetByCodeAsync(systemCode);
        if (system == null) return null;

        var access = await _accessRepo.GetByUserAndSystemAsync(userId, system.Id);
        if (access == null || !access.IsActive || !access.IsApproved) return null;

        return access.GrantedPolicies;
    }

    public async Task UpdateLastAccessAsync(int userId, int systemId)
    {
        var access = await _accessRepo.GetByUserAndSystemAsync(userId, systemId);
        if (access != null)
        {
            access.LastAccessAt = DateTime.UtcNow;
            await _accessRepo.UpdateAsync(access);
        }
    }

    // ========================================
    // Estatísticas
    // ========================================

    public async Task<SystemStatsDto?> GetSystemStatsAsync(int systemId)
    {
        var system = await _systemRepo.GetByIdAsync(systemId);
        if (system == null) return null;

        var totalUsers = await _accessRepo.CountBySystemIdAsync(systemId);
        var activeUsers = await _accessRepo.CountActiveBySystemIdAsync(systemId);
        var pendingApprovals = await _accessRepo.CountPendingBySystemIdAsync(systemId);

        var adminCount = await _context.UserSystemAccesses
            .CountAsync(a => a.SystemId == systemId && a.IsActive && a.IsApproved && a.SystemRole == "Admin");

        var lastAccess = await _context.UserSystemAccesses
            .Where(a => a.SystemId == systemId && a.LastAccessAt != null)
            .MaxAsync(a => (DateTime?)a.LastAccessAt);

        return new SystemStatsDto(
            systemId,
            system.SystemCode,
            system.DisplayName,
            totalUsers,
            activeUsers,
            pendingApprovals,
            adminCount,
            lastAccess
        );
    }

    public async Task<List<SystemStatsDto>> GetAllSystemsStatsAsync()
    {
        var systems = await _systemRepo.GetAllAsync();
        var result = new List<SystemStatsDto>();

        foreach (var system in systems)
        {
            var stats = await GetSystemStatsAsync(system.Id);
            if (stats != null) result.Add(stats);
        }

        return result;
    }

    // ========================================
    // Helpers
    // ========================================

    private static UserSystemAccessDto MapToDto(UserSystemAccess access)
    {
        return new UserSystemAccessDto(
            access.Id,
            access.UserId,
            access.User?.Username ?? string.Empty,
            access.User?.Email ?? string.Empty,
            access.SystemId,
            access.System?.SystemCode ?? string.Empty,
            access.System?.DisplayName ?? string.Empty,
            access.SystemRole,
            access.GrantedPolicies,
            access.IsActive,
            access.IsApproved,
            access.RequestedAt,
            access.ApprovedAt,
            access.LastAccessAt
        );
    }

    private static string GenerateApiSecret()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
