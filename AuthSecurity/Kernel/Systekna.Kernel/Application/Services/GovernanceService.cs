using Microsoft.EntityFrameworkCore;
using Systekna.Kernel.Domain.DTOs;
using Systekna.Kernel.Domain.Entities;
using Systekna.Kernel.Domain.Interfaces;
using Systekna.Kernel.Infrastructure.Data;

namespace Systekna.Kernel.Application.Services;

public class GovernanceService : IGovernanceService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuditRepository _auditRepository;
    private readonly ISettingRepository _settingRepository;
    private readonly AuthDbContext _context;

    public GovernanceService(IUserRepository userRepository, IAuditRepository auditRepository, ISettingRepository settingRepository, AuthDbContext context)
    {
        _userRepository = userRepository;
        _auditRepository = auditRepository;
        _settingRepository = settingRepository;
        _context = context;
    }

    public async Task<GovernanceStats> GetStatsAsync()
    {
        var users = await _userRepository.GetAllAsync();
        var auditsCount = await _auditRepository.GetCountAsync();
        var recentAudits = await _auditRepository.GetRecentAsync(1000);
        var failedLogins = recentAudits.Count(l => l.Action == "Login_Failed" && l.Timestamp > DateTime.UtcNow.AddDays(-1));
        
        return new GovernanceStats(
            TotalUsers: users.Count,
            ActiveSessions: users.Count(u => u.RefreshTokenExpiryTime > DateTime.UtcNow),
            FailedLoginsLast24h: failedLogins,
            AuditLogsCount: auditsCount
        );
    }

    public async Task<bool> ToggleUserStatusAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return false;

        // Inverter o status de atividade
        user.IsActive = !user.IsActive;

        // Se for bloqueado, limpar tokens para deslogar imediatamente
        if (!user.IsActive)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
        }
        
        await _userRepository.UpdateAsync(user);
        return true;
    }

    public async Task<List<SystemSetting>> GetSettingsAsync()
    {
        return await _settingRepository.GetAllAsync();
    }

    public async Task UpdateSettingAsync(string key, string value)
    {
        var setting = await _settingRepository.GetByKeyAsync(key) ?? new SystemSetting { Key = key };
        setting.Value = value;
        await _settingRepository.UpdateAsync(setting);
    }

    public async Task<List<ErrorLog>> GetRecentErrorsAsync(int count = 100)
    {
        return await _context.ErrorLogs
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task<byte[]> GenerateErrorReportCsvAsync()
    {
        var errors = await _context.ErrorLogs.OrderByDescending(e => e.Timestamp).ToListAsync();
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Id,Timestamp,Message,Endpoint,User,IPAddress");
        foreach (var err in errors)
        {
            csv.AppendLine($"{err.Id},{err.Timestamp:yyyy-MM-dd HH:mm:ss},\"{err.Message.Replace("\"", "\"\"")}\",\"{err.Endpoint}\",\"{err.UserIdentifier}\",\"{err.IpAddress}\"");
        }
        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }
}
