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
