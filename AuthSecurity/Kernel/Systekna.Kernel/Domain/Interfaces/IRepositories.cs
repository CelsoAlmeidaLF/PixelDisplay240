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
