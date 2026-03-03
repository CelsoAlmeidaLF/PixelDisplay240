using Microsoft.EntityFrameworkCore;
using Systekna.Kernel.Domain.Entities;
using Systekna.Kernel.Domain.Interfaces;
using Systekna.Kernel.Infrastructure.Data;

namespace Systekna.Kernel.Application.Services;

public class AuditService : IAuditService
{
    private readonly AuthDbContext _context;

    public AuditService(AuthDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string action, string userIdentifier, string details, string ipAddress = "", string userAgent = "")
    {
        var log = new AuditLog
        {
            Action = action,
            UserIdentifier = userIdentifier,
            Details = details,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTime.UtcNow
        };
        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetRecentLogsAsync(int count = 100)
    {
        return await _context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToListAsync();
    }

    public async Task<byte[]> GenerateAuditReportCsvAsync()
    {
        var logs = await _context.AuditLogs.OrderByDescending(a => a.Timestamp).ToListAsync();
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Id,Timestamp,Action,User,Details,IPAddress");
        foreach (var log in logs)
        {
            csv.AppendLine($"{log.Id},{log.Timestamp:yyyy-MM-dd HH:mm:ss},\"{log.Action}\",\"{log.UserIdentifier}\",\"{log.Details.Replace("\"", "\"\"")}\",\"{log.IpAddress}\"");
        }
        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }
}
