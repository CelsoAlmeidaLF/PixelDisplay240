using Systekna.Kernel.Domain.Entities;

namespace Systekna.Kernel.Domain.Interfaces;

public interface IUserRepository
{
    Task<UserEntity?> GetByIdAsync(int id);
    Task<UserEntity?> GetByUsernameAsync(string username);
    Task<UserEntity?> GetByEmailAsync(string email);
    Task<UserEntity?> GetByResetTokenAsync(string token);
    Task<UserEntity?> GetByVerificationTokenAsync(string token);
    Task<UserEntity?> GetByRefreshTokenAsync(string token);
    Task<List<UserEntity>> GetAllAsync();
    Task AddAsync(UserEntity user);
    Task UpdateAsync(UserEntity user);
    Task DeleteAsync(UserEntity user);
    Task<bool> ExistsAsync(string username, string email);
}

public interface IAuditRepository
{
    Task AddAsync(AuditLog log);
    Task<List<AuditLog>> GetRecentAsync(int count);
    Task<int> GetCountAsync();
}

public interface ISettingRepository
{
    Task<SystemSetting?> GetByKeyAsync(string key);
    Task<List<SystemSetting>> GetAllAsync();
    Task UpdateAsync(SystemSetting setting);
}

// ========================================
// Repositórios para Sistemas Cadastrados
// ========================================

public interface IRegisteredSystemRepository
{
    Task<RegisteredSystem?> GetByIdAsync(int id);
    Task<RegisteredSystem?> GetByCodeAsync(string systemCode);
    Task<List<RegisteredSystem>> GetAllAsync();
    Task<List<RegisteredSystem>> GetActiveAsync();
    Task AddAsync(RegisteredSystem system);
    Task UpdateAsync(RegisteredSystem system);
    Task DeleteAsync(RegisteredSystem system);
    Task<bool> ExistsAsync(string systemCode);
}

public interface IUserSystemAccessRepository
{
    Task<UserSystemAccess?> GetByIdAsync(int id);
    Task<UserSystemAccess?> GetByUserAndSystemAsync(int userId, int systemId);
    Task<List<UserSystemAccess>> GetByUserIdAsync(int userId);
    Task<List<UserSystemAccess>> GetBySystemIdAsync(int systemId);
    Task<List<UserSystemAccess>> GetPendingBySystemIdAsync(int systemId);
    Task<List<UserSystemAccess>> GetActiveBySystemIdAsync(int systemId);
    Task AddAsync(UserSystemAccess access);
    Task UpdateAsync(UserSystemAccess access);
    Task DeleteAsync(UserSystemAccess access);
    Task<int> CountBySystemIdAsync(int systemId);
    Task<int> CountActiveBySystemIdAsync(int systemId);
    Task<int> CountPendingBySystemIdAsync(int systemId);
}
