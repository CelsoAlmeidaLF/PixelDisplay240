using Microsoft.EntityFrameworkCore;
using Systekna.Kernel.Domain.Entities;
using Systekna.Kernel.Domain.Interfaces;
using Systekna.Kernel.Infrastructure.Data;

namespace Systekna.Kernel.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuthDbContext _context;
    public UserRepository(AuthDbContext context) => _context = context;

    public async Task<UserEntity?> GetByIdAsync(int id) 
        => await _context.Users.FindAsync(id);
    public async Task<UserEntity?> GetByUsernameAsync(string username) 
        => await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    public async Task<UserEntity?> GetByEmailAsync(string email)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    public async Task<UserEntity?> GetByResetTokenAsync(string token)
        => await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);
    public async Task<UserEntity?> GetByVerificationTokenAsync(string token) 
        => await _context.Users.FirstOrDefaultAsync(u => u.EmailConfirmationToken == token);
    public async Task<UserEntity?> GetByRefreshTokenAsync(string token) 
        => await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == token);
    public async Task<List<UserEntity>> GetAllAsync() 
        => await _context.Users.ToListAsync();
    public async Task AddAsync(UserEntity user) { 
        _context.Users.Add(user); await _context.SaveChangesAsync(); }
    public async Task UpdateAsync(UserEntity user) {
        _context.Users.Update(user); await _context.SaveChangesAsync(); }
    public async Task DeleteAsync(UserEntity user) { 
        _context.Users.Remove(user); await _context.SaveChangesAsync(); }
    public async Task<bool> ExistsAsync(string username, string email) 
        => await _context.Users.AnyAsync(u => u.Username == username || u.Email == email);
}

public class AuditRepository : IAuditRepository
{
    private readonly AuthDbContext _context;
    public AuditRepository(AuthDbContext context) => _context = context;

    public async Task AddAsync(AuditLog log) { _context.AuditLogs.Add(log); await _context.SaveChangesAsync(); }
    public async Task<List<AuditLog>> GetRecentAsync(int count) => await _context.AuditLogs.OrderByDescending(a => a.Timestamp).Take(count).ToListAsync();
    public async Task<int> GetCountAsync() => await _context.AuditLogs.CountAsync();
}

public class SettingRepository : ISettingRepository
{
    private readonly AuthDbContext _context;
    public SettingRepository(AuthDbContext context) => _context = context;

    public async Task<SystemSetting?> GetByKeyAsync(string key) => await _context.SystemSettings.FindAsync(key);
    public async Task<List<SystemSetting>> GetAllAsync() => await _context.SystemSettings.ToListAsync();
    public async Task UpdateAsync(SystemSetting setting)
    {
        var existing = await _context.SystemSettings.FindAsync(setting.Key);
        if (existing == null) _context.SystemSettings.Add(setting);
        else { existing.Value = setting.Value; existing.LastUpdated = DateTime.UtcNow; }
        await _context.SaveChangesAsync();
    }
}

// ========================================
// Repositório de Sistemas Cadastrados
// ========================================

public class RegisteredSystemRepository : IRegisteredSystemRepository
{
    private readonly AuthDbContext _context;
    public RegisteredSystemRepository(AuthDbContext context) => _context = context;

    public async Task<RegisteredSystem?> GetByIdAsync(int id)
        => await _context.RegisteredSystems
            .Include(s => s.UserAccesses)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<RegisteredSystem?> GetByCodeAsync(string systemCode)
        => await _context.RegisteredSystems
            .Include(s => s.UserAccesses)
            .FirstOrDefaultAsync(s => s.SystemCode == systemCode);

    public async Task<List<RegisteredSystem>> GetAllAsync()
        => await _context.RegisteredSystems
            .Include(s => s.UserAccesses)
            .OrderBy(s => s.DisplayName)
            .ToListAsync();

    public async Task<List<RegisteredSystem>> GetActiveAsync()
        => await _context.RegisteredSystems
            .Include(s => s.UserAccesses)
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayName)
            .ToListAsync();

    public async Task AddAsync(RegisteredSystem system)
    {
        _context.RegisteredSystems.Add(system);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(RegisteredSystem system)
    {
        system.UpdatedAt = DateTime.UtcNow;
        _context.RegisteredSystems.Update(system);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(RegisteredSystem system)
    {
        _context.RegisteredSystems.Remove(system);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string systemCode)
        => await _context.RegisteredSystems.AnyAsync(s => s.SystemCode == systemCode);
}

// ========================================
// Repositório de Acesso Usuário-Sistema
// ========================================

public class UserSystemAccessRepository : IUserSystemAccessRepository
{
    private readonly AuthDbContext _context;
    public UserSystemAccessRepository(AuthDbContext context) => _context = context;

    public async Task<UserSystemAccess?> GetByIdAsync(int id)
        => await _context.UserSystemAccesses
            .Include(a => a.User)
            .Include(a => a.System)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<UserSystemAccess?> GetByUserAndSystemAsync(int userId, int systemId)
        => await _context.UserSystemAccesses
            .Include(a => a.User)
            .Include(a => a.System)
            .FirstOrDefaultAsync(a => a.UserId == userId && a.SystemId == systemId);

    public async Task<List<UserSystemAccess>> GetByUserIdAsync(int userId)
        => await _context.UserSystemAccesses
            .Include(a => a.System)
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.System!.DisplayName)
            .ToListAsync();

    public async Task<List<UserSystemAccess>> GetBySystemIdAsync(int systemId)
        => await _context.UserSystemAccesses
            .Include(a => a.User)
            .Where(a => a.SystemId == systemId)
            .OrderBy(a => a.User!.Username)
            .ToListAsync();

    public async Task<List<UserSystemAccess>> GetPendingBySystemIdAsync(int systemId)
        => await _context.UserSystemAccesses
            .Include(a => a.User)
            .Where(a => a.SystemId == systemId && !a.IsApproved && a.IsActive)
            .OrderBy(a => a.RequestedAt)
            .ToListAsync();

    public async Task<List<UserSystemAccess>> GetActiveBySystemIdAsync(int systemId)
        => await _context.UserSystemAccesses
            .Include(a => a.User)
            .Where(a => a.SystemId == systemId && a.IsActive && a.IsApproved)
            .OrderBy(a => a.User!.Username)
            .ToListAsync();

    public async Task AddAsync(UserSystemAccess access)
    {
        _context.UserSystemAccesses.Add(access);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserSystemAccess access)
    {
        _context.UserSystemAccesses.Update(access);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(UserSystemAccess access)
    {
        _context.UserSystemAccesses.Remove(access);
        await _context.SaveChangesAsync();
    }

    public async Task<int> CountBySystemIdAsync(int systemId)
        => await _context.UserSystemAccesses.CountAsync(a => a.SystemId == systemId);

    public async Task<int> CountActiveBySystemIdAsync(int systemId)
        => await _context.UserSystemAccesses.CountAsync(a => a.SystemId == systemId && a.IsActive && a.IsApproved);

    public async Task<int> CountPendingBySystemIdAsync(int systemId)
        => await _context.UserSystemAccesses.CountAsync(a => a.SystemId == systemId && !a.IsApproved && a.IsActive);
}
